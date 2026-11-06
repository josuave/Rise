using System.Text.Json;
using System.Text.Json.Nodes;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for backups. Export reads the effective state —
/// through the services where they normalize (links, custom themes,
/// layouts, todos, notes) and through the ports where no service
/// getter exists (theme choice, widget configs) — and writes the
/// current-version envelope. Import validates a file completely
/// before saving anything, migrates it to the current version first,
/// then saves through the stores; the caller reloads the page after
/// import so every widget re-reads the restored state. The migrate
/// button only converts a file (see <see cref="BackupFormat.Migrate"/>)
/// and never touches state.
/// </summary>
public sealed class BackupService(
    StartPageService pageService,
    CustomThemeService themeService,
    LayoutService layoutService,
    TodoService todoService,
    NotesService notesService,
    ILinkStore linkStore,
    IThemeStore themeStore,
    ICustomThemeStore customThemeStore,
    ILayoutCollectionStore layoutStore,
    IWidgetConfigStore widgetConfigStore,
    ITodoStore todoStore,
    INoteStore noteStore,
    IModuleStore moduleStore,
    IHotkeyStore hotkeyStore,
    IPreviewFeatureStore previewFeatureStore,
    IBackgroundStore backgroundStore)
{
    public async Task<string> ExportAsync(CancellationToken cancellationToken = default)
    {
        StartPage page = await pageService.LoadAsync(cancellationToken);
        IReadOnlyList<CustomTheme> themes = await themeService.LoadAsync(cancellationToken);
        LayoutCollection layouts = await layoutService.LoadCollectionAsync(cancellationToken);
        TodoList todos = await todoService.LoadAsync(cancellationToken);
        NoteList notes = await notesService.LoadAsync(cancellationToken);
        string theme = await themeStore.GetStoredThemeAsync(cancellationToken) ?? "system";
        Dictionary<string, string> widgetConfigs = await widgetConfigStore.GetAllAsync(cancellationToken);
        Dictionary<string, bool> modules = await moduleStore.GetAllAsync(cancellationToken);
        Dictionary<string, HotkeyBinding> hotkeys = await hotkeyStore.GetAllAsync(cancellationToken);
        Dictionary<string, bool> previewFeatures = await previewFeatureStore.GetAllAsync(cancellationToken);
        BackgroundChoice? background = await backgroundStore.GetAsync(cancellationToken);

        var state = new BackupStateDto(
            page.Links.Select(link => new BackupLinkDto(link.Id, link.Name, link.Url.Value)).ToList(),
            theme,
            themes.Select(ToTheme).ToList(),
            new BackupLayoutsDto(
                layouts.ActiveId,
                layouts.Layouts.Select(layout => new BackupLayoutDto(
                    layout.Id,
                    layout.Name,
                    layout.Layout.Widgets.Select(widget => new BackupWidgetDto(widget.Id, widget.Visible)).ToList(),
                    layout.Layout.GroupChildren.ToList())).ToList()),
            widgetConfigs,
            todos.Items.Select(item => new BackupTodoDto(item.Id, item.Text, item.Done)).ToList(),
            notes.Items.Select(note => new BackupNoteDto(note.Id, note.Text)).ToList(),
            modules,
            hotkeys,
            previewFeatures,
            background is null ? null : new BackupBackgroundDto(background.PresetId, background.CustomUrl, background.Upload));
        JsonObject stateJson = JsonSerializer.SerializeToNode(state)!.AsObject();
        return BackupFormat.Serialize(new BackupEnvelope(
            BackupFormat.CurrentVersion,
            DateTimeOffset.UtcNow.ToString("o"),
            stateJson));
    }

    public async Task<Result<BackupEnvelope>> ImportAsync(string json, CancellationToken cancellationToken = default)
    {
        Result<BackupEnvelope> parsed = BackupFormat.Parse(json);
        if (!parsed.IsSuccess || parsed.Value is null)
        {
            return Result<BackupEnvelope>.Failure(parsed.Error ?? "That backup couldn't be read.");
        }

        BackupEnvelope envelope = parsed.Value;
        if (envelope.FormatVersion < BackupFormat.CurrentVersion)
        {
            Result<BackupEnvelope> migrated = BackupFormat.Migrate(envelope, BackupFormat.CurrentVersion);
            if (!migrated.IsSuccess || migrated.Value is null)
            {
                return Result<BackupEnvelope>.Failure(migrated.Error ?? "That backup couldn't be migrated.");
            }

            envelope = migrated.Value;
        }

        Result<ImportedState> mapped = MapToDomain(envelope);
        if (!mapped.IsSuccess || mapped.Value is null)
        {
            return Result<BackupEnvelope>.Failure(mapped.Error ?? "That backup's saved state couldn't be read.");
        }

        ImportedState state = mapped.Value;
        await linkStore.SaveAsync(state.Page, cancellationToken);
        await themeStore.SetStoredThemeAsync(state.Theme, cancellationToken);
        await customThemeStore.SaveAsync(state.Themes, cancellationToken);
        await layoutStore.SaveAsync(state.Layouts, cancellationToken);
        await todoStore.SaveAsync(state.Todos, cancellationToken);
        await noteStore.SaveAsync(state.Notes, cancellationToken);
        if (state.Modules is not null)
        {
            await moduleStore.SaveAllAsync(state.Modules, cancellationToken);
        }

        if (state.Hotkeys is not null)
        {
            await hotkeyStore.SaveAllAsync(state.Hotkeys, cancellationToken);
        }

        if (state.PreviewFeatures is not null)
        {
            await previewFeatureStore.SaveAllAsync(state.PreviewFeatures, cancellationToken);
        }

        if (state.Background is not null)
        {
            await backgroundStore.SaveAsync(state.Background, cancellationToken);
        }
        foreach (KeyValuePair<string, string> pair in state.WidgetConfigs)
        {
            await widgetConfigStore.SetAsync(pair.Key, pair.Value, cancellationToken);
        }

        return Result<BackupEnvelope>.Success(envelope);
    }

    private static Result<ImportedState> MapToDomain(BackupEnvelope envelope)
    {
        BackupStateDto? state;
        try
        {
            state = JsonSerializer.Deserialize<BackupStateDto>(envelope.State.ToJsonString());
        }
        catch (JsonException)
        {
            return Result<ImportedState>.Failure("That backup's saved state couldn't be read.");
        }

        if (state is null || state.Links is null || state.Themes is null || state.Layouts is null
            || state.Layouts.Layouts is null || state.WidgetConfigs is null || state.Theme is null)
        {
            return Result<ImportedState>.Failure("That backup is missing part of its saved state.");
        }

        var links = new List<Link>();
        foreach (BackupLinkDto dto in state.Links)
        {
            if (dto.Id == Guid.Empty || string.IsNullOrWhiteSpace(dto.Name)
                || !LinkUrl.TryCreate(dto.Url, out LinkUrl? url, out _) || url is null)
            {
                return Result<ImportedState>.Failure("That backup has a link without a valid name or web address.");
            }

            links.Add(new Link(dto.Id, dto.Name.Trim(), url));
        }

        string theme = state.Theme.Trim();
        if (CustomThemeService.TryParseChoice(theme, out string customId))
        {
            // Orphaned custom choice (the theme was deleted after export) repairs to system, like the toggle does.
            if (state.Themes.All(t => t.Id != customId))
            {
                theme = "system";
            }
        }
        else if (theme is not ("light" or "dark" or "system"))
        {
            return Result<ImportedState>.Failure("That backup has a theme choice this app doesn't know.");
        }

        var themes = new List<CustomTheme>();
        foreach (BackupThemeDto dto in state.Themes)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                continue;
            }

            Result<ThemePalette> light = ToPalette(dto.Light);
            Result<ThemePalette> dark = ToPalette(dto.Dark);
            if (!light.IsSuccess || light.Value is null || !dark.IsSuccess || dark.Value is null)
            {
                return Result<ImportedState>.Failure("That backup has a custom theme with a missing palette.");
            }

            themes.Add(new CustomTheme(
                dto.Id,
                string.IsNullOrWhiteSpace(dto.Name) ? "Untitled" : dto.Name,
                light.Value,
                dark.Value));
        }

        if (string.IsNullOrWhiteSpace(state.Layouts.ActiveId))
        {
            return Result<ImportedState>.Failure("That backup's layouts have no active layout.");
        }

        var layouts = new List<NamedLayout>();
        foreach (BackupLayoutDto dto in state.Layouts.Layouts)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                return Result<ImportedState>.Failure("That backup has a layout without an id.");
            }

            if (dto.Widgets is null)
            {
                return Result<ImportedState>.Failure("That backup has a layout without widgets.");
            }

            var widgets = new List<WidgetPlacement>();
            foreach (BackupWidgetDto widget in dto.Widgets)
            {
                if (string.IsNullOrWhiteSpace(widget.Id))
                {
                    return Result<ImportedState>.Failure("That backup has a layout widget without an id.");
                }

                widgets.Add(new WidgetPlacement(widget.Id, WidgetZone.Main, widget.Visible));
            }

            // Group members are an overlay, not structure: blank ids
            // are dropped, and the layout service reconciles the rest
            // against the registered widgets on load.
            List<string> groupChildren = dto.GroupChildren?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList() ?? new List<string>();

            layouts.Add(new NamedLayout(dto.Id, dto.Name ?? string.Empty, new PageLayout(widgets, groupChildren)));
        }

        if (state.WidgetConfigs.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
        {
            return Result<ImportedState>.Failure("That backup has a widget setting without a name.");
        }

        // Todos and notes are a compatible v1 extension: a file that
        // predates them has no sections, which read as empty, and
        // blank text is an import error (never silently dropped),
        // mirroring links.
        var todos = new List<TodoItem>();
        foreach (BackupTodoDto dto in state.Todos ?? new List<BackupTodoDto>())
        {
            if (dto.Id == Guid.Empty || string.IsNullOrWhiteSpace(dto.Text))
            {
                return Result<ImportedState>.Failure("That backup has a todo without valid text.");
            }

            todos.Add(new TodoItem(dto.Id, dto.Text.Trim(), dto.Done));
        }

        var notes = new List<Note>();
        foreach (BackupNoteDto dto in state.Notes ?? new List<BackupNoteDto>())
        {
            if (dto.Id == Guid.Empty || string.IsNullOrWhiteSpace(dto.Text))
            {
                return Result<ImportedState>.Failure("That backup has a note without valid text.");
            }

            notes.Add(new Note(dto.Id, dto.Text.Trim()));
        }

        // Module choices are a compatible v1 extension: a file that
        // predates them has no section, which leaves current choices
        // untouched; a blank module id is an import error, mirroring
        // widget configs. Unknown ids are kept: they are harmless and
        // forward-compatible with modules added later.
        if (state.Modules is not null && state.Modules.Any(pair => string.IsNullOrWhiteSpace(pair.Key)))
        {
            return Result<ImportedState>.Failure("That backup has a module choice without a name.");
        }

        // Hotkey bindings are a compatible v1 extension like modules:
        // absent leaves current bindings untouched; a blank action id
        // or key is an import error. Unknown ids are kept: harmless
        // and forward-compatible with actions added later.
        if (state.Hotkeys is not null && state.Hotkeys.Any(pair =>
            string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value.Key)))
        {
            return Result<ImportedState>.Failure("That backup has a hotkey binding without a name or key.");
        }

        // Preview-feature choices are a compatible v1 extension like
        // modules: absent leaves current choices untouched; a blank
        // feature id is an import error. Unknown ids are kept:
        // harmless and forward-compatible with features added later.
        if (state.PreviewFeatures is not null && state.PreviewFeatures.Any(pair => string.IsNullOrWhiteSpace(pair.Key)))
        {
            return Result<ImportedState>.Failure("That backup has a preview feature choice without a name.");
        }

        // The background is a compatible v1 extension like modules:
        // absent leaves the current choice untouched. An unknown
        // preset repairs to none (the orphaned-custom-theme
        // precedent); a custom choice without a valid image address
        // is an import error.
        BackgroundChoice? background = null;
        if (state.Background is not null)
        {
            if (state.Background.PresetId == BackgroundPresets.Custom)
            {
                string? url = state.Background.CustomUrl?.Trim();
                if (string.IsNullOrWhiteSpace(url)
                    || !url.Contains("://", StringComparison.Ordinal)
                    || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                    || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                {
                    return Result<ImportedState>.Failure("That backup has a background image without a valid address.");
                }

                background = new BackgroundChoice(BackgroundPresets.Custom, url);
            }
            else if (state.Background.PresetId == BackgroundPresets.Upload)
            {
                UploadedImage? upload = state.Background.Upload;
                if (upload is null
                    || upload.Hash.Length != 64
                    || string.IsNullOrWhiteSpace(upload.FileName)
                    || upload.Size <= 0
                    || !upload.MimeType.StartsWith("image/", StringComparison.Ordinal))
                {
                    return Result<ImportedState>.Failure("That backup has a background upload without valid details.");
                }

                background = new BackgroundChoice(BackgroundPresets.Upload, null, upload);
            }
            else
            {
                background = BackgroundPresets.All.Any(p => p.Id == state.Background.PresetId)
                    ? new BackgroundChoice(state.Background.PresetId, null)
                    : BackgroundChoice.None;
            }
        }

        return Result<ImportedState>.Success(new ImportedState(
            new StartPage(links),
            theme,
            themes,
            new LayoutCollection(state.Layouts.ActiveId, layouts),
            state.WidgetConfigs,
            new TodoList(todos),
            new NoteList(notes),
            state.Modules,
            state.Hotkeys,
            state.PreviewFeatures,
            background));
    }

    private static BackupThemeDto ToTheme(CustomTheme theme) => new(
        theme.Id,
        theme.Name,
        ToPalette(theme.Light),
        ToPalette(theme.Dark));

    private static BackupPaletteDto ToPalette(ThemePalette palette) => new(
        palette.Background,
        palette.Text,
        palette.Muted,
        palette.Faint,
        palette.Surface,
        palette.Border,
        palette.BorderSoft,
        palette.TileBackground,
        palette.Error);

    private static Result<ThemePalette> ToPalette(BackupPaletteDto? dto)
    {
        if (dto is null || dto.Background is null || dto.Text is null || dto.Muted is null || dto.Faint is null
            || dto.Surface is null || dto.Border is null || dto.BorderSoft is null || dto.TileBackground is null
            || dto.Error is null)
        {
            return Result<ThemePalette>.Failure("Missing palette.");
        }

        return Result<ThemePalette>.Success(new ThemePalette(
            dto.Background,
            dto.Text,
            dto.Muted,
            dto.Faint,
            dto.Surface,
            dto.Border,
            dto.BorderSoft,
            dto.TileBackground,
            dto.Error));
    }

    private sealed record ImportedState(
        StartPage Page,
        string Theme,
        List<CustomTheme> Themes,
        LayoutCollection Layouts,
        Dictionary<string, string> WidgetConfigs,
        TodoList Todos,
        NoteList Notes,
        Dictionary<string, bool>? Modules,
        Dictionary<string, HotkeyBinding>? Hotkeys,
        Dictionary<string, bool>? PreviewFeatures,
        BackgroundChoice? Background);
}

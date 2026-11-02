# Rise architecture

Rise is a modular monolith: one Blazor WebAssembly app, published as one site
to GitHub Pages, built from a hexagonal clean core plus vertical slices.

## The core: hexagonal clean architecture

```
                ┌─────────────────────────────────────────┐
                │  Imperative shell (effects only)         │
                │  Blazor components, timers, JS interop,  │
                │  localStorage, browser tabs              │
                └───────────────┬─────────────────────────┘
                                │ calls ports, renders results
                ┌───────────────▼─────────────────────────┐
                │  Application (ports + orchestration)     │
                │  ILinkStore, ISearchLauncher, IClock     │
                │  StartPageService, SearchService         │
                └───────────────┬─────────────────────────┘
                                │ calls pure functions
                ┌───────────────▼─────────────────────────┐
                │  Functional core (pure, no I/O)          │
                │  Link, LinkUrl, StartPage, SearchQuery,  │
                │  Greeting, Result                        │
                └─────────────────────────────────────────┘
                                ▲ implemented by
                ┌───────────────┴─────────────────────────┐
                │  Infrastructure (adapters)               │
                │  LocalStorageLinkStore,                  │
                │  BrowserSearchLauncher, SystemClock      │
                └─────────────────────────────────────────┘
```

Dependency rule: core never imports a browser, storage, or clock API.
Application depends only on the domain. Infrastructure depends on
Application (to implement its ports). The shell (Rise.Wasm) is the only
place that wires services to adapters, via `AddRiseCore()` in Program.cs.

### Functional core, imperative shell

The split is deliberately Gary Bernhardt's: the core is pure functions
over immutable data, the shell is thin and does all the effects.

- Pure core: `StartPage.AddLink` / `RemoveLink` take a page and return a
  new page (or a `Result` failure) — they never save anything.
  `LinkUrl.TryCreate` validates and normalizes without touching a browser.
  `SearchQuery.Build` turns text into a URI. `Greeting.For` turns a
  `DateTime` into words. These are trivially testable with no mocks,
  because they have no dependencies at all.
- Imperative shell: application services load a page through a port,
  hand it to the core, and persist whatever the core returns.
  Components render, collect input, and call services. Adapters do the
  actual JS interop / localStorage / tab opening / clock reading.
  The only place `DateTime.Now` is read is `SystemClock`.

If a bug is a wrong rule (duplicate link allowed, bad URL accepted),
it is in the core. If a bug is a failed save or a blocked popup, it is
in the shell. That separation is the whole point.

### What lives where

| Project | Role | May depend on |
|---|---|---|
| `src/Rise.Domain` | Functional core: entities, value objects, aggregates, pure functions | Nothing |
| `src/Rise.Application` | Ports (interfaces) + shell orchestration services | Domain |
| `src/Rise.Infrastructure` | Adapters implementing the ports + DI wiring (`AddRiseCore`) | Application |
| `src/Rise.Wasm` | Shell / composition root: pages, layout, theme, Program.cs | Core + slices |
| `src/Slices/*` | Vertical slices (below) | Application only |

## Slices: `src/Slices/`

A slice is one user-visible feature, end to end, as a Razor class
library (RCL) at a minimum:

```
src/Slices/
  Rise.Slices.Links/    QuickLinks.razor (+ code-behind shell)
  Rise.Slices.Search/   Search.razor (+ code-behind shell)
  Rise.Slices.Settings/ SettingsGear.razor (+ code-behind shell)
  Rise.Slices.Layout/   LayoutEditor.razor (+ code-behind shell)
```

Rules for a slice:

1. **An RCL is the floor.** A slice ships as its own RCL project so it
   can be referenced, tested, reused, or lazy-loaded independently.
   Its components are its public surface.
2. **A slice depends only on the core's ports/services**, never on
   Infrastructure and never on another slice's internals. Slices
   communicate, if they ever must, through core ports or domain
   events — not by importing each other.
3. **All-in-one or split out is the implementer's choice.** A small
   slice may instead live as a folder inside a combined `Rise.Slices`
   RCL (e.g. `Slices/Clock/`), because the dependency rule is the
   same either way: folder or project, it still talks only to core
   ports. Split a slice into its own RCL when it grows its own
   dependencies, its own routes, or becomes a lazy-loading candidate.
4. **A slice may itself be clean/hexagonal, naturally.** Nothing in
   the outer architecture prevents a slice from organizing its
   internals as `Domain/` / `Ports/` / `Adapters/` subfolders inside
   its RCL — its public surface stays the same (components + optional
   DI registration), and its internals stay private to the slice.
   Use that when a slice gets complex enough to earn it (its own
   rules, its own storage, its own tests); don't pay for it on a
   slice that is one component and one service call.
5. **The shell owns the theme.** The calm CSS variables and the
   classes shared by more than one component live in the Wasm app's
   global `app.css`; each component's own styles live in a colocated
   `.razor.css` file next to it (Blazor scopes them to that component
   and bundles RCL styles into the app's stylesheet automatically).
   Rules that style markup rendered by another component stay global:
   the settings dialog's buttons belong to sections from other slices,
   and scoped CSS cannot reach into a child component's markup. The
   theme is never duplicated per slice: scoped files reference the
   shared variables.

Current slices: Links (tiles, persisted via `ILinkStore`), Search
(query box, launched via `ISearchLauncher`), Settings (gear button
and a Settings dialog: a built-in Theme section — Light / Dark /
System, persisted via the `IThemeStore` port — plus sections
contributed by other slices), and Layout (the layout editor).

## Widgets and layout: the page is a list, not a template

The start page is no longer a hardcoded template. It is a `PageLayout`
(Domain): an ordered list of `WidgetPlacement`s — an opaque string
id, a zone, and a visible flag. The core knows no concrete widgets:
which ids exist, what they are called, and their default order and
visibility all come from the registered widget definitions.

A widget definition is the `IWidgetDefinition` contract (Application
port): Id, Title, Zone, DefaultVisible, Order, ComponentType. Each
slice owns its definitions and ships a DI extension for them
(`AddSearchWidget`, `AddLinksWidgets`, `AddSettingsWidgets`), wired
once in the shell's Program.cs — the same shape as the Settings
contract, but as a core port, because the layout service genuinely
needs the metadata (unlike a dialog section, which is purely UI).
ComponentType is System.Type only, so Application stays free of any
Blazor reference; the shell renders definitions via DynamicComponent.

Widgets live in two zones: Theme and Settings render in the
top-right cluster, everything else in the main column, each in
layout order. Moves only swap within a zone, so a widget can never
wander into the other zone, and hiding a widget never loses its
position. The layout service reconciles a stored layout against the
definitions on load: unknown ids are dropped, zones always come from
the definition, visibility is kept, and widgets the stored layout
predates slot in by definition Order, ahead of every widget ordered
after them — so a new widget lands where its Order puts it instead
of last, and the stored relative order of existing widgets is never
disturbed. The default layout is just the definitions in Order —
it reproduces the old page (Header, Search, Links, Theme, Settings
visible; Link editor hidden).

The header itself is a widget: the Header slice renders date, clock,
and greeting and owns the refresh timer, so the shell re-renders
only when the layout changes instead of on every clock tick. The
"Edit layout" button and the editor stay shell-rendered, not widgets:
an editor you could hide would strand the user with no way back.

Adding a widget is a new definition plus one line in Program.cs:
no change to Domain, the layout service, the editor, or the shell.
The shell (`Index`) renders widgets from the layout instead of naming
components directly, and re-renders whenever the layout service's
`Changed` event fires. The link service has the same event, so tiles
stay fresh when the editor (a separate widget) saves. Editing is a
widget too: Links' "Edit links" button shows the Link editor widget,
whose Done button hides it again. The layout editor itself is a
widget of last resort in the shell: move up/down, hide/show, reset to
defaults, all buttons (drag-and-drop is a later step, not v1), with
titles taken from the definitions. Like the Settings contract, this
is an extension point for first-party slices, not a plugin loader:
DynamicComponent only renders components from assemblies the app
already loads.

Named layouts: the user can save more than one arrangement. The
stored shape is a `LayoutCollection` (Domain): an active id plus a
list of `NamedLayout`s (id, name, `PageLayout`). The default layout
always exists, can be renamed but never deleted, and is the fallback
when the active layout is deleted. Names are unique (a clash gets
" 2", " 3", ...), saving the current arrangement activates the copy,
and every layout is reconciled against the definitions on load the
same way a single layout was — per layout, so an old layout gains
new widgets with defaults just like the default one does. The
single-layout operations (move, hide, reset, show/hide widget) always
act on the active layout, so the shell and the other widgets did not
change: they render the active layout and re-render on `Changed`.
The layout editor adds a layouts section above the widget rows:
switch, save the current arrangement as new, rename the active
layout, delete (never the default).

Persistence is the same localStorage pattern as links: `LayoutService`
(Application) over the `ILayoutCollectionStore` port,
`LocalStorageLayoutCollectionStore` (Infrastructure) under
`rise.layouts.v1`, storing only layout ids, names, widget ids, and
visibility (zones come from the definitions), corrupt or missing data
falling back to the default collection. Per-page layouts (a layout
per route) are a later step: Rise has one page today, so named
layouts on that page come first. The theme editor is Phase 2, built
on the same shape: a custom theme is a name over a light palette
and a dark palette (the nine calm CSS variables; the built-in
defaults mirror app.css), created as a copy of the built-ins and
tweaked with color pickers in the Settings dialog. Definitions live
under `rise.themes.v1` via `ICustomThemeStore`; the active choice
stays in `rise.theme.v1` as "custom:&lt;id&gt;", so the picker,
the quick toggle, and the before-first-paint script all keep
working: built-in choices use the data-theme attribute, custom
choices apply one palette half as inline variables (dark half when
the device prefers dark, re-applied on OS-preference change), and
switching to a built-in clears the inline variables. Deleting the
active custom theme repairs the choice to "system"; an orphaned
custom choice repairs the same way on startup. Generating a theme
from a seed color or image is a later step, not this one. The
shell's index.html provides the `window.riseTheme` script block the
theme adapter calls (localStorage, the page's data-theme attribute,
custom inline variables, matchMedia), the same way it provides
`window.riseStorage` for links,
plus a small head script that applies an explicit stored theme before
first paint. The same head order gives the loading screen its theme
for free: `#app` holds a minimal centered "Rise" wordmark and sliding
bar (styled from app.css's calm variables), shown until Blazor
starts and clears it — so the loading screen already matches
light/dark/system/custom with no extra code. The diagnostics/maintenance dialog that lived here while
the dead-clicks bug was being chased was removed once that bug was
fixed (PR #9); its `IDiagnosticsProbe` port and adapter went with it,
and can be restored from git history if a future bug needs them again.

## Settings contract: how a slice injects into Settings

Settings is its own slice and its own library — `Rise.Slices.Settings`,
a Razor class library in `src/Slices/` — not part of the core. The core
is Domain / Application / Infrastructure; Settings depends on the core's
services, never the other way around. And its contract lives in its own
abstractions library, separate from the core: Settings is not the core,
so its contract does not belong in a core abstractions library either.

So `ISettingsSection` (`Title`, `Order`, `Description`,
`ComponentType`) lives in `src/Slices/Rise.Slices.Settings.Abstractions`,
borrowing the Microsoft.Extensions.*.Abstractions pattern: interfaces
only, no implementations, no dependencies. A future core abstractions
library, if one ever earns its existence, would hold only contracts
shared across slices — not settings-specific ones — and would stay
separate from this one:

1. **Contribute.** A slice implements `ISettingsSection` and ships its
   own Razor component for the section. The component is the section's
   behavior and UI: it injects core services and does the work, exactly
   like any slice component. Example: `LinksSettingsSection`
   (`Order = 100`, component `LinksSettings`, which lists, removes,
   and resets links) in `Rise.Slices.Links`.
2. **Register.** The slice exposes a small DI extension (e.g.
   `AddLinksSettings()` registering
   `ISettingsSection -> LinksSettingsSection`) and the shell calls it
   in `Program.cs`. Registration is the shell's job, not the Settings
   slice's, so slices stay independent of each other.
3. **Render.** The Settings dialog injects
   `IEnumerable<ISettingsSection>`, orders by `Order`, and renders each
   section with Blazor's `DynamicComponent` using its `ComponentType`.
   It knows nothing about the contributing slice's internals, and a
   section's component is only ever rendered from an assembly the app
   already loads — this is an extension point for first-party slices,
   not a plugin loader for arbitrary assemblies.

To add a section to a new slice: implement the interface, add the
component, expose the `Add…Settings()` extension, and call it from
the shell. No change to the Settings slice is needed. The home
page in Rise.Wasm composes them; it contains no feature logic of
its own beyond the clock/greeting shell.

## Future options this structure keeps open

- **Lazy loading:** slices already publish as separate assemblies
  (`Rise.Slices.*.wasm` under `_framework`). Marking one with
  `BlazorWebAssemblyLazyLoad` and loading it on its route via
  `LazyAssemblyLoader` is a small change, worth it only for a heavy
  slice — not for Links/Search today.
- **Tests:** pure core tests need no browser; service tests need only
  a fake port; component tests need only a fake service. That last
  one is now real: `tests/Rise.Slices.Tests` (bUnit + xUnit) renders
  slice components, clicks their buttons, and asserts the result —
  including the Edit-links regression test that would have caught
  the PR #9 dead-clicks bug. Slice tests use real application
  services with fake ports (in-memory link store, recording search
  launcher, recording theme store, in-memory layout store), never a
  browser, JS, or Infrastructure. The widget phase added bUnit tests
  for the editor widget, the split-out link editor, and the split-out
  theme toggle; the registry rework added core layout tests (zones,
  in-zone moves, hide-keeps-position) and layout service tests
  (defaults and reconciliation against definitions); named layouts
  added collection tests (name uniqueness, delete fallback, rename)
  and service tests (save/switch/rename/delete, active-only edits)
  plus editor tests for the save-as/switch/delete/rename UI; the
  header widget added a component test (fixed clock) and a reconcile
  test proving new widgets slot in by Order. Both test projects carry `coverlet.collector`;
  CI collects Cobertura coverage via `coverage.runsettings`
  (test assemblies excluded) and uploads it to Codecov.
- **New slice:** copy a slice project, depend on Application, add its
  component to the shell page, wire any new port in Infrastructure.
  No core or existing slice changes required.

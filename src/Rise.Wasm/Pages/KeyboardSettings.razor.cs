using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Wasm.Pages;

/// <summary>
/// Shell-rendered manager for the hotkey system (not a slice section
/// component owned by a slice: the actions are app-wide, so the shell
/// owns this too, like the module switchboard). Rebinding captures
/// the next real keystroke in a readonly input — a plain button would
/// re-fire its click on Space, and an input never does.
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class KeyboardSettings
{
    [Inject]
    private HotkeyService HotkeyService { get; set; } = null!;

    private List<(HotkeyAction Action, HotkeyBinding Binding)> Rows { get; set; } = new();

    private string? CapturingActionId { get; set; }

    private string? Status { get; set; }

    protected override async Task OnInitializedAsync() => await RefreshAsync();

    private async Task RefreshAsync() =>
        Rows = (await HotkeyService.GetAllAsync()).ToList();

    private void CancelCapture() => CapturingActionId = null;

    private async Task OnCaptureKeyDown(KeyboardEventArgs args)
    {
        if (CapturingActionId is null)
        {
            return;
        }

        if (args.Key == "Escape")
        {
            CapturingActionId = null;
            return;
        }

        // A lone modifier is not a shortcut yet; wait for the real key.
        if (args.Key is "Control" or "Shift" or "Alt" or "Meta")
        {
            return;
        }

        string actionId = CapturingActionId;
        CapturingActionId = null;
        HotkeyBinding binding = new(args.Key, args.CtrlKey || args.MetaKey, args.AltKey, args.ShiftKey);
        Result<HotkeyBinding> saved = await HotkeyService.SetAsync(actionId, binding);
        Status = saved.IsSuccess ? $"Saved {binding.Display}." : saved.Error;
        await RefreshAsync();
    }

    private async Task ResetAsync()
    {
        await HotkeyService.ResetAsync();
        Status = "Defaults restored.";
        await RefreshAsync();
    }
}

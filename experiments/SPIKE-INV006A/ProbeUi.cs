using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv006A;

internal static class ProbeUi
{
    private static Label? _status;
    private static Button? _verify;

    internal static void Attach(NMainMenu menu)
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        var panel = new PanelContainer
        {
            Name = "SpikeInv006AScopeSafeGatePanel",
            AnchorLeft = 0.58f,
            AnchorTop = 0.04f,
            AnchorRight = 0.97f,
            AnchorBottom = 0.30f
        };
        var box = new VBoxContainer();
        panel.AddChild(box);
        _status = new Label
        {
            Text = "SPIKE-INV006A\nWAITING_EVENTS\nRemain on the main menu. Verify is disabled.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        box.AddChild(_status);
        _verify = new Button { Text = "Verify Stable Gate", Disabled = true };
        _verify.Pressed += VerifyPressed;
        box.AddChild(_verify);
        menu.AddChild(panel);
    }

    private static void VerifyPressed()
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        ProbeController.VerifyFromUi();
    }

    internal static void QueueGateReady()
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        Callable.From(PresentGateReady).CallDeferred();
    }

    private static void PresentGateReady()
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        if (_status is not null && GodotObject.IsInstanceValid(_status))
            _status.Text = "Gate Ready\nClick Verify Stable Gate exactly once.";
        if (_verify is not null && GodotObject.IsInstanceValid(_verify)) _verify.Disabled = false;
    }

    internal static void ShowTerminalFixed(string classification)
    {
        _status?.CallDeferred("set_text", classification + "\nStop all interaction. Keep the main menu open.");
        _verify?.CallDeferred("set_disabled", true);
    }
}

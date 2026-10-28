using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv006B;

internal static class ProbeUi
{
    private static Label? _status;
    private static Button? _run;

    internal static void Attach(NMainMenu menu)
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        var panel = new PanelContainer
        {
            Name = "SpikeInv006BScopeSafeGatePanel",
            AnchorLeft = 0.58f,
            AnchorTop = 0.04f,
            AnchorRight = 0.97f,
            AnchorBottom = 0.30f
        };
        var box = new VBoxContainer();
        panel.AddChild(box);
        _status = new Label
        {
            Text = "SPIKE-INV006B\nWAITING_EVENTS\nRemain on the main menu. Run is disabled.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        box.AddChild(_status);
        _run = new Button { Text = "Run Direct Write Sentinel", Disabled = true };
        _run.Pressed += RunPressed;
        box.AddChild(_run);
        menu.AddChild(panel);
    }

    private static void RunPressed()
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        ProbeController.RunFromUi();
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
            _status.Text = "Direct Write Ready\nClick Run Direct Write Sentinel exactly once.";
        if (_run is not null && GodotObject.IsInstanceValid(_run)) _run.Disabled = false;
    }

    internal static void DisableActions()
    {
        if (_run is not null && GodotObject.IsInstanceValid(_run)) _run.Disabled = true;
        if (_status is not null && GodotObject.IsInstanceValid(_status)) _status.Text = "DIRECT WRITE RUNNING\nDo not provide any input.";
    }

    internal static void AttachRecovery(NMainMenu menu)
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        var panel = new PanelContainer { Name = "SpikeInv006BRecoveryPanel", AnchorLeft = 0.58f, AnchorTop = 0.04f, AnchorRight = 0.97f, AnchorBottom = 0.24f };
        _status = new Label { Text = "FINALIZING SAFETY CHECKS\nDo not provide any input.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        panel.AddChild(_status); menu.AddChild(panel); _run = null;
    }

    internal static void ShowTerminalFixed(string classification)
    {
        _status?.CallDeferred("set_text", classification + "\nStop all interaction. Keep the main menu open.");
        _run?.CallDeferred("set_disabled", true);
    }
}

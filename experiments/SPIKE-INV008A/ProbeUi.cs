using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv008A;

internal static class ProbeUi
{
    private static NMainMenu? _menu;
    private static NMainMenuTextButton? _entry;
    private static Control? _buttonGroup;
    private static Control? _page;
    private static Label? _status;
    private static Button? _run;
    private static Callable _entryReleased;
    private static Callable _entryFocused;
    private static Callable _entryUnfocused;
    private static ProbePhase _phase;
    private static bool _ready;

    internal static void Attach(NMainMenu menu, ProbePhase phase)
    {
        Detach();
        _menu = menu;
        _phase = phase;
        _ready = false;
        _buttonGroup = menu.GetNode<Control>("MainMenuTextButtons");
        var template = menu.GetNode<NMainMenuTextButton>("MainMenuTextButtons/QuitButton");
        _entry = template.Duplicate(14) as NMainMenuTextButton
            ?? throw new InvalidOperationException("Could not duplicate the native main-menu button template.");
        _entry.Name = "SpikeInv008AOwnerFaultProbeButton";
        _buttonGroup.AddChild(_entry);
        _buttonGroup.MoveChild(_entry, template.GetIndex());
        _entry.label!.Text = "SPIKE-INV008A";

        _entryReleased = Callable.From<NButton>(_ => OpenPage());
        _entryFocused = Callable.From<NMainMenuTextButton>(button => InvokeMenuFocus("MainMenuButtonFocused", button));
        _entryUnfocused = Callable.From<NMainMenuTextButton>(button => InvokeMenuFocus("MainMenuButtonUnfocused", button));
        _entry.Connect(NClickableControl.SignalName.Released, _entryReleased);
        _entry.Connect(NClickableControl.SignalName.Focused, _entryFocused);
        _entry.Connect(NClickableControl.SignalName.Unfocused, _entryUnfocused);
    }

    private static void OpenPage()
    {
        if (_menu is null || _entry is null || _buttonGroup is null || _page is not null) return;
        var panel = new PanelContainer
        {
            Name = "SpikeInv008AOwnerFaultPage",
            AnchorLeft = 0.25f,
            AnchorTop = 0.2f,
            AnchorRight = 0.75f,
            AnchorBottom = 0.8f,
        };
        var box = new VBoxContainer();
        panel.AddChild(box);
        _status = new Label
        {
            Text = StatusText(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        box.AddChild(_status);
        if (PhasePolicy.IsFault(_phase) && ProbeController.IsObserverRunningFirst())
        {
            _run = new Button
            {
                Text = _phase == ProbePhase.SetupOwnerFault ? "Run Setup Fault" : "Run Post-launch Fault",
                Disabled = !_ready,
            };
            _run.Pressed += ProbeController.RunFromUi;
            box.AddChild(_run);
        }
        var close = new Button { Text = "Close" };
        close.Pressed += ClosePage;
        box.AddChild(close);
        _menu.AddChild(panel);
        _page = panel;
        _buttonGroup.Visible = false;
        (_ready && _run is not null ? _run : close).GrabFocus();
    }

    private static string StatusText() => ComposeStatus(_phase, _ready, ProbeController.TerminalTextForUi());

    internal static string ComposeStatus(ProbePhase phase, bool ready, string terminal)
    {
        if (!string.IsNullOrEmpty(terminal)) return terminal + "\nStop all interaction and exit the game normally. Do not retry.";
        var label = PhaseLabel(phase);
        return !ready
            ? $"SPIKE-INV008A — {label}\n\nWAITING_EVENTS\nRemain on the main menu. Do not click the probe action yet."
            : PhasePolicy.IsRestart(phase)
                ? $"SPIKE-INV008A — {label} READY\n\nAutomatic read-only verification is starting. Do not click any game UI."
                : $"SPIKE-INV008A — {label} READY\n\nPrior integrity and fresh Continue verified. Click the single enabled probe button once; no timeout while waiting.";
    }

    internal static string PhaseLabel(ProbePhase phase) => phase switch
    {
        ProbePhase.SetupOwnerFault => "SETUP_OWNER_FAULT_R6",
        ProbePhase.RestartAfterSetup => "RESTART_AFTER_SETUP_R6",
        ProbePhase.PostLaunchOwnerFault => "POST_LAUNCH_OWNER_FAULT_R6",
        ProbePhase.RestartAfterPostLaunch => "RESTART_AFTER_POST_LAUNCH_R6",
        _ => phase.ToString()
    };

    private static void ClosePage()
    {
        if (_buttonGroup is not null && GodotObject.IsInstanceValid(_buttonGroup)) _buttonGroup.Visible = true;
        if (_page is not null && GodotObject.IsInstanceValid(_page)) _page.QueueFree();
        _page = null;
        _status = null;
        _run = null;
        if (_entry is not null && GodotObject.IsInstanceValid(_entry)) _entry.GrabFocus();
    }

    internal static void QueueGateReady(ProbePhase phase)
    {
        if (ProbeController.IsObserverRunningFirst()) Callable.From(() => PresentReady(phase)).CallDeferred();
    }

    private static void PresentReady(ProbePhase phase)
    {
        if (!ProbeController.IsObserverRunningFirst() || phase != _phase) return;
        _ready = true;
        if (_status is not null && GodotObject.IsInstanceValid(_status)) _status.Text = StatusText();
        if (_run is not null && GodotObject.IsInstanceValid(_run)) _run.Disabled = false;
    }

    internal static void DisableActions()
    {
        if (_run is not null && GodotObject.IsInstanceValid(_run)) _run.Disabled = true;
        if (_status is not null && GodotObject.IsInstanceValid(_status)) _status.Text = "PHASE RUNNING\nDo not provide any input.";
    }

    internal static void AttachRecovery(NMainMenu menu)
    {
        var panel = new PanelContainer { Name = "SpikeInv008ARecoveryPanel", AnchorLeft = 0.55f, AnchorTop = 0.04f, AnchorRight = 0.97f, AnchorBottom = 0.24f };
        _status = new Label { Text = "FINALIZING ORDERED TEARDOWN\nDo not provide any input.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        panel.AddChild(_status); menu.AddChild(panel); _run = null;
    }

    internal static void ShowTerminalFixed(string classification)
    {
        _status?.CallDeferred("set_text", classification + "\nStop all interaction and exit the game normally. Do not retry.");
        _run?.CallDeferred("set_disabled", true);
    }

    private static void InvokeMenuFocus(string methodName, NMainMenuTextButton button)
    {
        typeof(NMainMenu).GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(_menu, new object[] { button });
    }

    private static void Detach()
    {
        if (_page is not null && GodotObject.IsInstanceValid(_page)) _page.QueueFree();
        _page = null;
        if (_entry is not null && GodotObject.IsInstanceValid(_entry))
        {
            if (_entry.IsConnected(NClickableControl.SignalName.Released, _entryReleased)) _entry.Disconnect(NClickableControl.SignalName.Released, _entryReleased);
            if (_entry.IsConnected(NClickableControl.SignalName.Focused, _entryFocused)) _entry.Disconnect(NClickableControl.SignalName.Focused, _entryFocused);
            if (_entry.IsConnected(NClickableControl.SignalName.Unfocused, _entryUnfocused)) _entry.Disconnect(NClickableControl.SignalName.Unfocused, _entryUnfocused);
            _entry.QueueFree();
        }
        _entry = null;
        _buttonGroup = null;
        _menu = null;
        _status = null;
        _run = null;
    }
}

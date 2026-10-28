using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv004A;

internal static class ProbeUi
{
    private static NMainMenu? _menu;
    private static NMainMenuTextButton? _entry;
    private static Control? _page;
    private static Control? _buttonGroup;
    private static Callable _entryReleased;
    private static Callable _entryFocused;
    private static Callable _entryUnfocused;

    internal static void Attach(NMainMenu menu)
    {
        Detach();
        _menu = menu;
        _buttonGroup = menu.GetNode<Control>("MainMenuTextButtons");
        var template = menu.GetNode<NMainMenuTextButton>("MainMenuTextButtons/QuitButton");
        _entry = template.Duplicate(14) as NMainMenuTextButton
            ?? throw new InvalidOperationException("Could not duplicate the native main-menu button template.");
        _entry.Name = "SpikeInv004ADirectWriteSentinelButton";
        _buttonGroup.AddChild(_entry);
        _buttonGroup.MoveChild(_entry, template.GetIndex());
        _entry.label!.Text = "Direct Write Sentinel";

        _entryReleased = Callable.From<NButton>(_ => OpenPage());
        _entryFocused = Callable.From<NMainMenuTextButton>(button => InvokeMenuFocus("MainMenuButtonFocused", button));
        _entryUnfocused = Callable.From<NMainMenuTextButton>(button => InvokeMenuFocus("MainMenuButtonUnfocused", button));
        _entry.Connect(NClickableControl.SignalName.Released, _entryReleased);
        _entry.Connect(NClickableControl.SignalName.Focused, _entryFocused);
        _entry.Connect(NClickableControl.SignalName.Unfocused, _entryUnfocused);
    }

    private static void OpenPage()
    {
        if (_menu is null || _entry is null || _buttonGroup is null || _page is not null)
            return;

        var unlocked = string.Equals(ReplayTransaction.ReadControlPhase(), "DIRECT_WRITE", StringComparison.Ordinal);
        var panel = new PanelContainer
        {
            Name = "SpikeInv004ADirectWritePage",
            AnchorLeft = 0.25f,
            AnchorTop = 0.2f,
            AnchorRight = 0.75f,
            AnchorBottom = 0.8f,
        };
        var box = new VBoxContainer();
        panel.AddChild(box);
        box.AddChild(new Label
        {
            Text = unlocked
                ? "SPIKE-INV004A — Direct progress-write sentinel\n\nNo replay events are dispatched. Only UpdateProgressAfterCombatWon and SaveProgressFile are called on the isolated manager, followed by immediate teardown."
                : "SPIKE-INV004A — Phase 0 locked\n\nRun F8 and F9 at the main menu and return the evidence. The direct-write runtime is not unlocked yet.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (unlocked)
        {
            var start = new Button { Text = "Run Direct Write Sentinel" };
            start.Pressed += StartTransaction;
            box.AddChild(start);
        }
        var close = new Button { Text = "Close" };
        close.Pressed += ClosePage;
        box.AddChild(close);
        _menu.AddChild(panel);
        _page = panel;
        _buttonGroup.Visible = false;
        close.GrabFocus();
    }

    private static void ClosePage()
    {
        if (_buttonGroup is not null && GodotObject.IsInstanceValid(_buttonGroup))
            _buttonGroup.Visible = true;
        if (_page is not null && GodotObject.IsInstanceValid(_page))
            _page.QueueFree();
        _page = null;
        if (_entry is not null && GodotObject.IsInstanceValid(_entry))
            _entry.GrabFocus();
    }

    private static void StartTransaction()
    {
        ClosePage();
        ReplayTransaction.StartFromUi();
    }

    private static void InvokeMenuFocus(string methodName, NMainMenuTextButton button)
    {
        typeof(NMainMenu).GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(_menu, new object[] { button });
    }

    private static void Detach()
    {
        if (_page is not null && GodotObject.IsInstanceValid(_page))
            _page.QueueFree();
        _page = null;
        if (_entry is not null && GodotObject.IsInstanceValid(_entry))
        {
            if (_entry.IsConnected(NClickableControl.SignalName.Released, _entryReleased))
                _entry.Disconnect(NClickableControl.SignalName.Released, _entryReleased);
            if (_entry.IsConnected(NClickableControl.SignalName.Focused, _entryFocused))
                _entry.Disconnect(NClickableControl.SignalName.Focused, _entryFocused);
            if (_entry.IsConnected(NClickableControl.SignalName.Unfocused, _entryUnfocused))
                _entry.Disconnect(NClickableControl.SignalName.Unfocused, _entryUnfocused);
            _entry.QueueFree();
        }
        _entry = null;
        _buttonGroup = null;
        _menu = null;
    }
}

using Godot;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv004B;

internal static class ProbeUi
{
    private static NMainMenu? _menu;
    private static NMainMenuTextButton? _entry;
    private static Control? _page;
    private static Control? _buttonGroup;
    private static Label? _status;
    private static Callable _released;
    private static Callable _focused;
    private static Callable _unfocused;

    internal static void Attach(NMainMenu menu)
    {
        Detach();
        _menu = menu;
        _buttonGroup = menu.GetNode<Control>("MainMenuTextButtons");
        var template = menu.GetNode<NMainMenuTextButton>("MainMenuTextButtons/QuitButton");
        _entry = template.Duplicate(14) as NMainMenuTextButton ?? throw new InvalidOperationException("Could not duplicate main-menu button.");
        _entry.Name = "SpikeInv004BDeepCopyDiagnosticButton";
        _buttonGroup.AddChild(_entry);
        _buttonGroup.MoveChild(_entry, template.GetIndex());
        _entry.label!.Text = "Replay Copy Diagnostic";
        _released = Callable.From<NButton>(_ => Open());
        _focused = Callable.From<NMainMenuTextButton>(button => InvokeFocus("MainMenuButtonFocused", button));
        _unfocused = Callable.From<NMainMenuTextButton>(button => InvokeFocus("MainMenuButtonUnfocused", button));
        _entry.Connect(NClickableControl.SignalName.Released, _released);
        _entry.Connect(NClickableControl.SignalName.Focused, _focused);
        _entry.Connect(NClickableControl.SignalName.Unfocused, _unfocused);
    }

    private static void Open()
    {
        if (_menu is null || _buttonGroup is null || _page is not null) return;
        var panel = new PanelContainer { Name = "SpikeInv004BPage", AnchorLeft = .2f, AnchorTop = .15f, AnchorRight = .8f, AnchorBottom = .85f };
        var box = new VBoxContainer(); panel.AddChild(box);
        _status = new Label { Text = "SPIKE-INV004B — pre-override deep-copy diagnostic\n\nThis runs once at the main menu. It constructs disk-rehydrated and live-seeded managers only in separate memory stores. It does not install an override, start a run, enter a scene, or call progress mutation/save methods.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        box.AddChild(_status);
        var run = new Button { Text = "Run Deep-Copy Diagnostic" };
        run.Pressed += ProbeController.StartFromUi;
        box.AddChild(run);
        var close = new Button { Text = "Close" }; close.Pressed += Close; box.AddChild(close);
        _menu.AddChild(panel); _page = panel; _buttonGroup.Visible = false; run.GrabFocus();
    }

    internal static void ShowStatus(string text) { if (_status is not null && GodotObject.IsInstanceValid(_status)) _status.Text = text; }

    private static void Close()
    {
        if (_buttonGroup is not null && GodotObject.IsInstanceValid(_buttonGroup)) _buttonGroup.Visible = true;
        if (_page is not null && GodotObject.IsInstanceValid(_page)) _page.QueueFree();
        _page = null; _status = null;
        if (_entry is not null && GodotObject.IsInstanceValid(_entry)) _entry.GrabFocus();
    }

    private static void InvokeFocus(string name, NMainMenuTextButton button) => typeof(NMainMenu).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(_menu, new object[] { button });
    private static void Detach()
    {
        if (_page is not null && GodotObject.IsInstanceValid(_page)) _page.QueueFree();
        if (_entry is not null && GodotObject.IsInstanceValid(_entry)) _entry.QueueFree();
        _menu = null; _entry = null; _page = null; _buttonGroup = null; _status = null;
    }
}

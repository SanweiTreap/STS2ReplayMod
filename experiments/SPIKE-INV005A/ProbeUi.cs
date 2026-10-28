using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv005A;

internal static class ProbeUi
{
    private static PanelContainer? _panel;
    private static Label? _status;
    private static Button? _verify;
    private static string? _terminalText;

    internal static void Attach(NMainMenu menu)
    {
        if (_panel is not null && GodotObject.IsInstanceValid(_panel)) _panel.QueueFree();
        _panel = new PanelContainer
        {
            Name = "SpikeInv005AStartupBarrierPanel",
            AnchorLeft = 0.58f,
            AnchorTop = 0.04f,
            AnchorRight = 0.97f,
            AnchorBottom = 0.34f
        };
        var box = new VBoxContainer();
        _panel.AddChild(box);
        _status = new Label
        {
            Text = _terminalText ?? "SPIKE-INV005A\nWAITING_EVENTS\nDo not enter any menu or game mode. Verify remains disabled until explicit startup events and writer/task gates pass.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        box.AddChild(_status);
        _verify = new Button { Text = "Verify Stable Gate", Disabled = true };
        _verify.Pressed += ProbeController.VerifyFromUi;
        box.AddChild(_verify);
        menu.AddChild(_panel);
    }

    internal static void SetGateReady()
    {
        ShowStatus("Gate Ready\nRemain on the main menu. Click Verify Stable Gate exactly once.");
        if (_verify is not null && GodotObject.IsInstanceValid(_verify)) _verify.Disabled = false;
    }

    internal static void SetTerminal(string classification, string artifactPath)
    {
        _terminalText = classification + "\nStop all interaction now. Keep the main menu open for evidence collection.\nEvidence: " + artifactPath;
        ShowStatus(_terminalText);
        if (_verify is not null && GodotObject.IsInstanceValid(_verify)) _verify.Disabled = true;
    }

    internal static void ShowStatus(string text)
    {
        if (_status is not null && GodotObject.IsInstanceValid(_status)) _status.Text = text;
    }
}

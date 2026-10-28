using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace Spike001A2R2;

internal static class ProbeUi
{
    private static CanvasLayer? _layer; private static Label? _label;
    internal static void Show(string text)
    {
        var game = NGame.Instance; if (game is null) { GD.Print("[SPIKE-001A2-R2] " + text.Replace('\n', ' ')); return; }
        if (_layer is null || !GodotObject.IsInstanceValid(_layer))
        {
            _layer = new CanvasLayer { Name = "Spike001A2R2StatusLayer", Layer = 1000 };
            var panel = new PanelContainer { OffsetLeft = 24, OffsetTop = 24, OffsetRight = 720, OffsetBottom = 190 };
            _label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            panel.AddChild(_label); _layer.AddChild(panel); game.AddChild(_layer);
        }
        if (_label is not null) _label.Text = text;
        GD.Print("[SPIKE-001A2-R2] " + text.Replace('\n', ' '));
    }
}

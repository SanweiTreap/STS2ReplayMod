using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace Spike001A4;

internal static class ProbeUi
{
    private const string LabelName = "Spike001A4Status";
    internal const int OverlayZIndex = 4096;
    internal static void Show(string text)
    {
        GD.Print("[SPIKE-001A4] " + text.Replace('\n', ' '));
        var root = NGame.Instance;
        if (root is null) return;
        var label = root.GetNodeOrNull<Label>(LabelName);
        if (label is null)
        {
            label = new Label { Name = LabelName, Position = new Vector2(16, 16), ZIndex = OverlayZIndex,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            root.AddChild(label);
        }
        label.Text = text;
    }
}

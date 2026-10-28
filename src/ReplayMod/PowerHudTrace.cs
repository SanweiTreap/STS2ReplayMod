using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.addons.mega_text;

namespace ReplayMod;

// Small read-only power endpoints follow the source clock while real combat data stays at
// its semantic checkpoint. These controls have no model subscriptions or input handlers.
internal static class PowerHudTrace
{
    internal sealed record IconLayout(TextureRect.ExpandModeEnum Expand,TextureRect.StretchModeEnum Stretch,Vector2 Minimum,Vector2 Pivot,float Rotation);
    internal sealed record Value(Creature Creature,PowerModel Power,Vector2 Position,Vector2 Scale,Color Color,bool Visible,
        Texture2D? Icon,Vector2 IconPosition,Vector2 IconSize,Vector2 IconScale,Color IconColor,
        string Amount,Vector2 LabelPosition,Vector2 LabelSize,Vector2 LabelScale,Vector2 LabelPivot,Color LabelColor,bool LabelVisible,
        Font Font,int FontSize,Color FontColor,Color Outline,int OutlineSize,HorizontalAlignment Horizontal,VerticalAlignment Vertical,IconLayout Layout);
    internal sealed record Sample(Value[] Values);
    private static object Get(object owner,string field)=>NativeDisplayTrace.ReadField(owner,field)!;
    private static NPowerContainer? Container(Creature creature)
    {
        var node=NCombatRoom.Instance?.GetCreatureNode(creature);
        return node is not null&&GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion()?
            (NPowerContainer)Get(Get(node,"_stateDisplay"),"_powerContainer"):null;
    }
    internal static Sample Capture(IEnumerable<Creature> creatures)
    {
        var values=new List<Value>();
        foreach(var creature in creatures)
        {
            if(Container(creature) is not {} container)continue;
            foreach(var power in container.GetChildren().OfType<NPower>().Where(p=>p.IsNodeReady()&&p.IsVisibleInTree()))
            {
                var icon=(TextureRect)Get(power,"_icon");var label=(MegaLabel)Get(power,"_amountLabel");
                values.Add(new(creature,power.Model,power.Position,power.Scale,power.Modulate,power.Visible,
                    icon.Texture,icon.Position,icon.Size,icon.Scale,icon.Modulate,label.Text,label.Position,label.Size,label.Scale,label.PivotOffset,label.Modulate,label.Visible,
                    label.GetThemeFont(NativeDisplayTrace.Name("font")),label.GetThemeFontSize(NativeDisplayTrace.Name("font_size")),label.GetThemeColor(NativeDisplayTrace.Name("font_color")),label.GetThemeColor(NativeDisplayTrace.Name("font_outline_color")),label.GetThemeConstant(NativeDisplayTrace.Name("outline_size")),label.HorizontalAlignment,label.VerticalAlignment,
                    new(icon.ExpandMode,icon.StretchMode,icon.CustomMinimumSize,icon.PivotOffset,icon.Rotation)));
            }
        }
        return new(values.ToArray());
    }
    internal sealed class View : IDisposable
    {
        private sealed record Badge(Control Root,TextureRect Icon,Label Label);
        private readonly Dictionary<Creature,Control> _roots=[];
        private readonly Dictionary<(Creature,PowerModel),Badge> _badges=[];
        private readonly Dictionary<Control,bool> _hidden=[];
        private readonly Dictionary<Creature,NPowerContainer> _containers=[];
        internal void Prepare(IEnumerable<Creature> creatures)
        {
            foreach(var creature in creatures)
            {
                if(Container(creature) is not {} container)continue;
                if(!_roots.TryGetValue(creature,out var root)||!GodotObject.IsInstanceValid(root)||root.IsQueuedForDeletion())
                {
                    root=new(){Name="ReplayPowerDisplay",MouseFilter=Control.MouseFilterEnum.Ignore};
                    var parent=container.GetParent();parent.AddChild(root);parent.MoveChild(root,container.GetIndex()+1);root.ZIndex=container.ZIndex;
                    _roots[creature]=root;_containers[creature]=container;
                }
                root.Position=container.Position;root.Scale=container.Scale;root.Modulate=container.Modulate;
                if(!_hidden.ContainsKey(container))_hidden.Add(container,container.Visible);container.Visible=false;
            }
        }
        private Badge BadgeFor(Value value)
        {
            var key=(value.Creature,value.Power);if(_badges.TryGetValue(key,out var badge))return badge;
            var root=new Control{MouseFilter=Control.MouseFilterEnum.Ignore};_roots[value.Creature].AddChild(root);
            var icon=new TextureRect{MouseFilter=Control.MouseFilterEnum.Ignore};var label=new Label{MouseFilter=Control.MouseFilterEnum.Ignore};root.AddChild(icon);root.AddChild(label);
            label.AddThemeFontOverride(NativeDisplayTrace.Name("font"),value.Font);label.AddThemeFontSizeOverride(NativeDisplayTrace.Name("font_size"),value.FontSize);
            label.AddThemeColorOverride(NativeDisplayTrace.Name("font_color"),value.FontColor);label.AddThemeColorOverride(NativeDisplayTrace.Name("font_outline_color"),value.Outline);label.AddThemeConstantOverride(NativeDisplayTrace.Name("outline_size"),value.OutlineSize);
            badge=new(root,icon,label);_badges.Add(key,badge);return badge;
        }
        internal void Apply(Sample from,Sample to,float blend)
        {
            var visible=from.Values.Select(v=>(v.Creature,v.Power)).ToHashSet();
            foreach(var (key,badge) in _badges)if(!visible.Contains(key)&&GodotObject.IsInstanceValid(badge.Root))badge.Root.Visible=false;
            foreach(var value in from.Values)
            {
                if(!_roots.TryGetValue(value.Creature,out var root)||!GodotObject.IsInstanceValid(root)||root.IsQueuedForDeletion())continue;
                var container=_containers[value.Creature];if(GodotObject.IsInstanceValid(container)&&!container.IsQueuedForDeletion())
                {root.Position=container.Position;root.Scale=container.Scale;root.Modulate=container.Modulate;}
                var next=to.Values.FirstOrDefault(v=>ReferenceEquals(v.Creature,value.Creature)&&ReferenceEquals(v.Power,value.Power))??value;
                var badge=BadgeFor(value);Apply(value,next,blend,badge.Root,badge.Icon,badge.Label);
            }
        }
        private static void Apply(Value value,Value next,float blend,Control root,TextureRect icon,Label label)
        {
            root.Visible=value.Visible;root.Position=value.Position.Lerp(next.Position,blend);root.Scale=value.Scale.Lerp(next.Scale,blend);root.Modulate=value.Color.Lerp(next.Color,blend);
            // TextureRect's default KeepSize can clamp a small replay icon to its full texture
            // resolution. Reproduce the native sizing policy before assigning texture or size.
            icon.ExpandMode=value.Layout.Expand;icon.StretchMode=value.Layout.Stretch;icon.CustomMinimumSize=value.Layout.Minimum;
            icon.PivotOffset=value.Layout.Pivot;icon.Rotation=value.Layout.Rotation;
            icon.Texture=value.Icon;icon.Position=value.IconPosition.Lerp(next.IconPosition,blend);icon.Size=value.IconSize;icon.Scale=value.IconScale.Lerp(next.IconScale,blend);icon.Modulate=value.IconColor.Lerp(next.IconColor,blend);
            if(label.Text!=value.Amount)label.Text=value.Amount;
            label.HorizontalAlignment=value.Horizontal;label.VerticalAlignment=value.Vertical;
            label.Position=value.LabelPosition.Lerp(next.LabelPosition,blend);label.Size=value.LabelSize;label.Scale=value.LabelScale.Lerp(next.LabelScale,blend);
            label.PivotOffset=value.LabelPivot;label.Modulate=value.LabelColor.Lerp(next.LabelColor,blend);label.Visible=value.LabelVisible;
        }
        internal void Handoff(Sample sample)
        {
            var count=0;
            foreach(var value in sample.Values)
            {
                var native=Container(value.Creature)?.GetChildren().OfType<NPower>().FirstOrDefault(p=>ReferenceEquals(p.Model,value.Power));
                if(native is null)continue;
                if(Get(native,"_animInTween") is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
                Apply(value,value,0,native,(TextureRect)Get(native,"_icon"),(MegaLabel)Get(native,"_amountLabel"));count++;
                // Its original container remains masked until all native HUD handoffs complete.
            }
            Runtime.Log("native-power-handoff",new{Powers=count,PowerBusinessRedispatched=false,NativeIconSizingPreserved=true,
                Icons=sample.Values.Select(v=>new{Model=v.Power.Id.ToString(),Width=v.IconSize.X,Height=v.IconSize.Y,Expand=v.Layout.Expand.ToString(),Stretch=v.Layout.Stretch.ToString()}).Distinct().ToArray()});
        }
        public void Dispose()
        {
            foreach(var root in _roots.Values)if(GodotObject.IsInstanceValid(root)&&!root.IsQueuedForDeletion()){root.GetParent()?.RemoveChild(root);root.QueueFree();}
            foreach(var (node,visible) in _hidden)if(GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion())node.Visible=visible;
        }
    }
}

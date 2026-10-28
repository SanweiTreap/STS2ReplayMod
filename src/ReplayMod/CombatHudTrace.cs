using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.addons.mega_text;

namespace ReplayMod;

// Stable data may be prepared early, but these live HUD endpoints follow the visible clock.
// Energy effects themselves use the audited native EnergyChanged display handler for every source.
internal static class CombatHudTrace
{
    private static readonly Dictionary<Node,Control[]> ControlCache=[];
    internal static void ClearControls()=>ControlCache.Clear();
    internal sealed record Value(string Root,string Path,Vector2 Position,Vector2 Size,Vector2 Scale,float Rotation,Vector2 Pivot,
        Color Color,Color SelfColor,bool Visible,Material? Material,Texture2D? Texture,string? Text,Font? Font,int FontSize,Color FontColor,Color Outline,bool Rich=false);
    internal sealed record Motion(bool Entering,double Elapsed);
    internal sealed record Sample(int Turn,Value[] Values,float? StarV=null,float? StarS=null,float? StarLerp=null,float? StarVelocity=null,int? StarDisplayed=null,Motion? EnergyMotion=null);
    private static object? Get(object owner,string field)=>NativeDisplayTrace.ReadField(owner,field);
    private static Motion? EnergyMotion()
    {
        if(Roots().FirstOrDefault(e=>e.Name=="energy").Root is not {} energy)return null;
        foreach(var entering in new[]{true,false})if(Get(energy,entering?"_animInTween":"_animOutTween") is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning())
            return new(entering,tween.GetTotalElapsedTime());
        return null;
    }
    private static void ContinueEnergy(Motion? motion)
    {
        if(motion is null||Roots().FirstOrDefault(e=>e.Name=="energy").Root is not NEnergyCounter energy)return;
        if(motion.Entering)energy.AnimIn();else energy.AnimOut();
        ((Tween)Get(energy,motion.Entering?"_animInTween":"_animOutTween")!).CustomStep(motion.Elapsed);
    }
    internal static void RestoreEnergy(Sample sample)
    {
        if(NativeDisplayTrace.Replaying||Roots().FirstOrDefault(e=>e.Name=="energy").Root is not {} energy)return;
        foreach(var field in new[]{"_animInTween","_animOutTween"})if(Get(energy,field) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
        if(sample.Values.FirstOrDefault(v=>v.Root=="energy"&&v.Path==".") is {} value)
        {energy.Position=value.Position;energy.Visible=value.Visible;energy.Scale=value.Scale;energy.Modulate=value.Color;}
        ContinueEnergy(sample.EnergyMotion);
    }
    private static IEnumerable<(string Name,Control Root)> Roots()
    {
        var room=NCombatRoom.Instance;
        if(room is null||!GodotObject.IsInstanceValid(room)||room.IsQueuedForDeletion()||room.Ui is not {} ui||!GodotObject.IsInstanceValid(ui)||!ui.IsNodeReady())yield break;
        if(Get(ui,"_energyCounter") is Control energy&&energy.IsNodeReady())yield return ("energy",energy);
        if(Get(ui,"_starCounter") is Control stars&&stars.IsNodeReady())yield return ("stars",stars);
        if(ui.EndTurnButton is {} end&&end.IsNodeReady())yield return ("end-turn",end);
        yield return ("draw-pile",ui.DrawPile);yield return ("discard-pile",ui.DiscardPile);yield return ("exhaust-pile",ui.ExhaustPile);
    }
    internal static bool ExitAnimating()
    {
        bool Running(object node,string field)=>Get(node,field) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning();
        if(NPlayerHand.Instance is {} hand&&Running(hand,"_animOutTween"))return true;
        return Roots().Any(e=>e.Name=="energy"?Running(e.Root,"_animOutTween"):e.Name.EndsWith("-pile",StringComparison.Ordinal)&&Running(e.Root,"_positionTween"));
    }
    private static IEnumerable<Control> Values(string name,Control root)=>name.EndsWith("-pile",StringComparison.Ordinal)?[root]:Controls(root);
    private static Control[] Controls(Node root)
    {
        if(ControlCache.TryGetValue(root,out var controls))return controls;
        var list=new List<Control>();
        void Visit(Node node){if(node is Control control)list.Add(control);foreach(var child in node.GetChildren())Visit(child);}
        Visit(root);controls=list.ToArray();ControlCache.Add(root,controls);return controls;
    }
    internal static Sample Capture()=>new(LiveState.Turn,Roots().SelectMany(entry=>Values(entry.Name,entry.Root).Select(control=>
    {
        var label=control as Label;var rich=control as RichTextLabel;
        return new Value(entry.Name,NativeDisplayTrace.RelativePath(entry.Root,control),control.Position,control.Size,control.Scale,control.Rotation,control.PivotOffset,
            control.Modulate,control.SelfModulate,control.Visible,entry.Name=="energy"?control.Material:null,(control as TextureRect)?.Texture,
            label?.Text??rich?.Text,label?.GetThemeFont(NativeDisplayTrace.Name("font"))??rich?.GetThemeFont(NativeDisplayTrace.Name("normal_font")),label?.GetThemeFontSize(NativeDisplayTrace.Name("font_size"))??rich?.GetThemeFontSize(NativeDisplayTrace.Name("normal_font_size"))??0,
            label?.GetThemeColor(NativeDisplayTrace.Name("font_color"))??rich?.GetThemeColor(NativeDisplayTrace.Name("default_color"))??Colors.White,
            label?.GetThemeColor(NativeDisplayTrace.Name("font_outline_color"))??rich?.GetThemeColor(NativeDisplayTrace.Name("font_outline_color"))??Colors.White,rich is not null);
    })).ToArray(),StarShader("v"),StarShader("s"),StarField<float>("_lerpingStarCount"),StarField<float>("_velocity"),StarField<int>("_displayedStarCount"),EnergyMotion());
    private static T? StarField<T>(string name) where T:struct=>Roots().FirstOrDefault(e=>e.Name=="stars").Root is {} stars? (T)Get(stars,name)!:null;
    private static float? StarShader(string name)=>Roots().FirstOrDefault(e=>e.Name=="stars").Root is {} stars&&Get(stars,"_hsv") is ShaderMaterial material?
        material.GetShaderParameter(NativeDisplayTrace.Name(name)).AsSingle():null;
    internal static void StarIncrease(int oldStars,int newStars)
    {
        if(Roots().FirstOrDefault(e=>e.Name=="stars").Root is not {} stars)return;
        // This handler is display only. Historical frames own the actual count, not player data.
        AccessTools.Method(typeof(NStarCounter),"UpdateStarCount").Invoke(stars,[oldStars,newStars]);
    }
    internal static void RestoreStars()
    {
        if(Roots().FirstOrDefault(e=>e.Name=="stars").Root is not {} stars)return;
        if(NativeDisplayTrace.Replaying)return;
        if(Get(stars,"_hsvTween") is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
        AccessTools.Field(typeof(NStarCounter),"_lerpingStarCount").SetValue(stars,(float)LiveState.Player.PlayerCombatState!.Stars);
        AccessTools.Field(typeof(NStarCounter),"_velocity").SetValue(stars,0f);
        AccessTools.Method(typeof(NStarCounter),"SetStarCountText").Invoke(stars,[LiveState.Player.PlayerCombatState.Stars]);
        AccessTools.Method(typeof(NStarCounter),"RefreshVisibility").Invoke(stars,null);
    }
    internal static void EnergyIncrease(int oldEnergy,int newEnergy)
    {
        var energy=Roots().First(e=>e.Name=="energy").Root;
        AccessTools.Method(typeof(NEnergyCounter),"OnEnergyChanged").Invoke(energy,[oldEnergy,newEnergy]);
    }
    internal static void RetireEnergyEffects()
    {
        foreach(var (_,energy) in Roots().Where(e=>e.Name=="energy"))
            foreach(var field in new[]{"_backVfx","_frontVfx"})if(Get(energy,field) is NParticlesContainer particles)
            {particles.Restart();particles.SetEmitting(false);}
    }
    internal static void RefreshTurnCaption()
    {
        if(NCombatRoom.Instance?.Ui?.EndTurnButton is not {} button||!button.IsNodeReady())return;
        var loc=new LocString("gameplay_ui","END_TURN_BUTTON");loc.Add("turnNumber",LiveState.Turn);
        ((MegaLabel)Get(button,"_label")!).SetTextAutoSize(loc.GetFormattedText());
    }
    internal sealed class View : IDisposable
    {
        private readonly Dictionary<(string,string),Control> _nodes=[];
        private readonly List<(Control Node,bool Processing)> _processing=[];
        internal void Prepare()
        {
            _nodes.Clear();
            foreach(var (name,root) in Roots())
            {
                var owned=_processing.Any(p=>ReferenceEquals(p.Node,root));
                if(name is "energy" or "stars"&&!owned)
                {_processing.Add((root,root.IsProcessing()));root.SetProcess(false);}
                foreach(var node in Values(name,root))_nodes[(name,NativeDisplayTrace.RelativePath(root,node))]=node;
                // Position/fade tweens are presentation only. They must not overwrite this clock.
                var fields=name=="energy"?new[]{"_animInTween","_animOutTween"}:name=="stars"?new[]{"_hsvTween"}:name.EndsWith("-pile",StringComparison.Ordinal)?new[]{"_positionTween"}:new[]{"_positionTween","_hoverTween","_glowEnableTween"};
                foreach(var field in fields)if(!(name=="stars"&&owned)&&Get(root,field) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
            }
        }
        internal void Apply(Sample from,Sample to,float blend)
        {
            if(_nodes.TryGetValue(("stars","."),out var stars)&&Get(stars,"_hsv") is ShaderMaterial hsv)
            {
                if(from.StarV is {} v)hsv.SetShaderParameter(NativeDisplayTrace.Name("v"),Mathf.Lerp(v,to.StarV??v,blend));
                if(from.StarS is {} s)hsv.SetShaderParameter(NativeDisplayTrace.Name("s"),s);
            }
            var next=to.Values.ToDictionary(v=>(v.Root,v.Path));
            foreach(var value in from.Values)
            {
                if(!_nodes.TryGetValue((value.Root,value.Path),out var node)||!GodotObject.IsInstanceValid(node)||node.IsQueuedForDeletion())continue;
                var b=next.GetValueOrDefault((value.Root,value.Path),value);
                node.Visible=value.Visible;node.PivotOffset=value.Pivot;node.Size=value.Size;
                node.Position=value.Position.Lerp(b.Position,blend);node.Scale=value.Scale.Lerp(b.Scale,blend);node.Rotation=Mathf.LerpAngle(value.Rotation,b.Rotation,blend);
                node.Modulate=value.Color.Lerp(b.Color,blend);node.SelfModulate=value.SelfColor.Lerp(b.SelfColor,blend);
                if(value.Root=="energy")node.Material=value.Material;
                if(node is TextureRect texture&&texture.Texture!=value.Texture)texture.Texture=value.Texture;
                if(node is Label label&&value.Text is not null)
                {
                    if(label.Text!=value.Text)label.Text=value.Text;
                    if(value.Font is not null&&label.GetThemeFont(NativeDisplayTrace.Name("font"))!=value.Font)label.AddThemeFontOverride(NativeDisplayTrace.Name("font"),value.Font);
                    if(label.GetThemeFontSize(NativeDisplayTrace.Name("font_size"))!=value.FontSize)label.AddThemeFontSizeOverride(NativeDisplayTrace.Name("font_size"),value.FontSize);
                    if(label.GetThemeColor(NativeDisplayTrace.Name("font_color"))!=value.FontColor)label.AddThemeColorOverride(NativeDisplayTrace.Name("font_color"),value.FontColor);
                    if(label.GetThemeColor(NativeDisplayTrace.Name("font_outline_color"))!=value.Outline)label.AddThemeColorOverride(NativeDisplayTrace.Name("font_outline_color"),value.Outline);
                }
                else if(node is RichTextLabel rich&&value.Text is not null)
                {
                    if(rich.Text!=value.Text)rich.Text=value.Text;
                    if(value.Font is not null&&rich.GetThemeFont(NativeDisplayTrace.Name("normal_font"))!=value.Font)rich.AddThemeFontOverride(NativeDisplayTrace.Name("normal_font"),value.Font);
                    if(rich.GetThemeFontSize(NativeDisplayTrace.Name("normal_font_size"))!=value.FontSize)rich.AddThemeFontSizeOverride(NativeDisplayTrace.Name("normal_font_size"),value.FontSize);
                    if(rich.GetThemeColor(NativeDisplayTrace.Name("default_color"))!=value.FontColor)rich.AddThemeColorOverride(NativeDisplayTrace.Name("default_color"),value.FontColor);
                    if(rich.GetThemeColor(NativeDisplayTrace.Name("font_outline_color"))!=value.Outline)rich.AddThemeColorOverride(NativeDisplayTrace.Name("font_outline_color"),value.Outline);
                }
            }
        }
        public void Dispose()
        {
            foreach(var (node,processing) in _processing)if(GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion())node.SetProcess(processing);
        }
        internal void Handoff(Sample sample)
        {
            Apply(sample,sample,0);
            ContinueEnergy(sample.EnergyMotion);
            if(_nodes.TryGetValue(("stars","."),out var stars)&&sample.StarLerp is {} lerp)
            {
                AccessTools.Field(typeof(NStarCounter),"_lerpingStarCount").SetValue(stars,lerp);
                AccessTools.Field(typeof(NStarCounter),"_velocity").SetValue(stars,sample.StarVelocity??0f);
                if(sample.StarDisplayed is {} displayed)AccessTools.Field(typeof(NStarCounter),"_displayedStarCount").SetValue(stars,displayed);
                Runtime.Log("native-star-handoff",new{SourceLerp=lerp,SourceDisplayed=sample.StarDisplayed,ActualStars=LiveState.Player.PlayerCombatState!.Stars,
                    ContinuedNativeCounter=true,ResourcePaymentRedispatched=false});
            }
            Runtime.Log("native-energy-motion-handoff",new{Motion=sample.EnergyMotion,Position=sample.Values.FirstOrDefault(v=>v.Root=="energy"&&v.Path==".")?.Position});
        }
    }
}

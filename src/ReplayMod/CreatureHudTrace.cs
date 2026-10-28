using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ReplayMod;

// The visible death/intent timeline is independent of when the real roster is restored.
// Only the small native HUD endpoints are sampled; no monster scene or gameplay task is cloned.
internal static class CreatureHudTrace
{
    private static readonly Dictionary<NIntent,Node[]> IntentControls=[];
    internal static void ClearControls()=>IntentControls.Clear();
    internal sealed record State(Creature Creature,Vector2 Position,Vector2 Scale,Color Color,Color SelfColor,bool Visible);
    internal sealed record Glyph(Creature Creature,string Path,Texture2D? Texture,Vector2 Position,Vector2 Scale,float Rotation,Color Color,bool Centered,Vector2 Offset);
    internal sealed record Text(Creature Creature,string Path,string Value,Vector2 Position,Vector2 Scale,float Rotation,Vector2 Size,Vector2 Pivot,Color Color,
        Font Font,Font Bold,Font Italic,Font BoldItalic,int FontSize,Color FontColor,Color Outline,int OutlineSize,bool Bbcode,HorizontalAlignment Alignment);
    internal sealed record Clock(Creature Creature,int Slot,string? Animation,float Time,float BobPhase,bool Frozen,Vector2 Position,Color Color,Color HolderColor,Color ContainerColor,bool Visible);
    internal sealed record Sample(State[] States,Glyph[] Glyphs,Text[] Texts,Clock[] Clocks);
    private static object Get(object owner,string name)=>NativeDisplayTrace.ReadField(owner,name)!;
    private static Color ColorOf(CanvasItem node)
    {
        var value=node.Modulate*node.SelfModulate;
        for(var parent=node.GetParent();parent is not null;parent=parent.GetParent())if(parent is CanvasItem canvas)value*=canvas.Modulate;
        return value;
    }
    private static IEnumerable<Node> Children(Node root)
    {
        yield return root;foreach(var child in root.GetChildren())foreach(var node in Children(child))yield return node;
    }
    internal static Sample Capture(IEnumerable<Creature> known)
    {
        var states=new List<State>();var glyphs=new List<Glyph>();var texts=new List<Text>();var clocks=new List<Clock>();
        foreach(var creature in known)
        {
            var node=NCombatRoom.Instance?.GetCreatureNode(creature);
            if(node is null||!GodotObject.IsInstanceValid(node)||!node.IsInsideTree())
            {states.Add(new(creature,Vector2.Zero,Vector2.One,Colors.White,Colors.White,false));continue;}
            var display=(Control)Get(node,"_stateDisplay");
            states.Add(new(creature,display.Position,display.Scale,display.Modulate,display.SelfModulate,display.IsVisibleInTree()));
            var root=node.IntentContainer;
            var intents=root.GetChildren().OfType<NIntent>().ToArray();
            for(var i=0;i<intents.Length;i++)clocks.Add(new(creature,i,Get(intents[i],"_animationName") as string,(float)Get(intents[i],"_timeAccumulator"),
                (float)(Godot.Time.GetTicksMsec()*.001*Math.PI)+(float)Get(intents[i],"_timeOffset"),(bool)Get(intents[i],"_isFrozen"),intents[i].Position,intents[i].Modulate,
                ((Control)Get(intents[i],"_intentHolder")).Modulate,root.Modulate,intents[i].Visible));
            foreach(var native in intents)
            {
                if(!IntentControls.TryGetValue(native,out var children)){children=Children(native).ToArray();IntentControls.Add(native,children);}
                var slot=Array.IndexOf(intents,native);
                foreach(var item in children)
                {
                var path=slot+"/"+NativeDisplayTrace.RelativePath(native,item);
                if(item is Sprite2D sprite&&sprite.IsVisibleInTree())
                {
                    glyphs.Add(new(creature,path,sprite.Texture,sprite.Position,sprite.Scale,sprite.Rotation,
                        sprite.Modulate*sprite.SelfModulate,sprite.Centered,sprite.Offset));
                }
                else if(item is RichTextLabel label&&label.IsVisibleInTree())
                {
                    texts.Add(new(creature,path,label.Text,label.Position,label.Scale,label.Rotation,label.Size,label.PivotOffset,label.Modulate*label.SelfModulate,
                        label.GetThemeFont(NativeDisplayTrace.Name("normal_font")),label.GetThemeFont(NativeDisplayTrace.Name("bold_font")),label.GetThemeFont(NativeDisplayTrace.Name("italics_font")),label.GetThemeFont(NativeDisplayTrace.Name("bold_italics_font")),
                        label.GetThemeFontSize(NativeDisplayTrace.Name("normal_font_size")),label.GetThemeColor(NativeDisplayTrace.Name("default_color")),label.GetThemeColor(NativeDisplayTrace.Name("font_outline_color")),label.GetThemeConstant(NativeDisplayTrace.Name("outline_size")),label.BbcodeEnabled,label.HorizontalAlignment));
                }
                }
            }
        }
        return new(states.ToArray(),glyphs.ToArray(),texts.ToArray(),clocks.ToArray());
    }
    private static readonly HashSet<NIntent> Owned=[];
    internal static void Install(Harmony harmony)=>harmony.Patch(AccessTools.DeclaredMethod(typeof(NIntent),"UpdateVisuals"),
        prefix:new HarmonyMethod(typeof(CreatureHudTrace),nameof(AllowRefresh)));
    private static bool AllowRefresh(NIntent __instance)=>!Owned.Contains(__instance);
    internal sealed class View(IEnumerable<Creature> creatures) : IDisposable
    {
        private readonly Dictionary<NIntent,bool> _processing=[];
        private readonly Dictionary<(Creature,int),NIntent> _intents=[];
        private readonly Dictionary<Creature,Control> _states=[];
        private static void Kill(object owner,string field)
        {
            if(Get(owner,field) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
        }
        internal void MaskIntents()
        {
            foreach(var creature in creatures)
            {
                var node=NCombatRoom.Instance?.GetCreatureNode(creature);if(node is null)continue;
                Kill(node,"_intentFadeTween");
                var state=(Control)Get(node,"_stateDisplay");_states[creature]=state;
                foreach(var field in new[]{"_blockTween","_hpLabelFadeTween","_middlegroundTween"})Kill(Get(state,"_healthBar"),field);
                var intents=node.IntentContainer.GetChildren().OfType<NIntent>().ToArray();
                for(var i=0;i<intents.Length;i++)
                {
                    var intent=intents[i];_intents[(creature,i)]=intent;
                    if(!_processing.ContainsKey(intent)){_processing.Add(intent,intent.IsProcessing());Owned.Add(intent);intent.SetProcess(false);}
                }
            }
        }
        private Node? Resolve(Creature creature,string path)
        {
            var split=path.IndexOf('/');if(split<0||!int.TryParse(path[..split],out var slot))return null;
            return _intents.TryGetValue((creature,slot),out var intent)&&GodotObject.IsInstanceValid(intent)&&!intent.IsQueuedForDeletion()
                ?intent.GetNodeOrNull(path[(split+1)..]):null;
        }
        internal void Apply(Sample from,Sample to,float blend)
        {
            foreach(var (key,intent) in _intents.Where(p=>GodotObject.IsInstanceValid(p.Value)&&!p.Value.IsQueuedForDeletion()))
            {
                var prefix=key.Item2+"/";
                if(!from.Glyphs.Any(g=>ReferenceEquals(g.Creature,key.Item1)&&g.Path.StartsWith(prefix,StringComparison.Ordinal)))((Sprite2D)Get(intent,"_intentSprite")).Visible=false;
                if(!from.Texts.Any(t=>ReferenceEquals(t.Creature,key.Item1)&&t.Path.StartsWith(prefix,StringComparison.Ordinal)))((Control)Get(intent,"_valueLabel")).Visible=false;
            }
            foreach(var state in from.States)
            {
                if(!_states.TryGetValue(state.Creature,out var display)||!GodotObject.IsInstanceValid(display)||display.IsQueuedForDeletion())continue;
                display.Visible=state.Visible;if(!state.Visible)continue;
                var b=to.States.FirstOrDefault(s=>ReferenceEquals(s.Creature,state.Creature)&&s.Visible)??state;
                display.Position=state.Position.Lerp(b.Position,blend);display.Scale=state.Scale.Lerp(b.Scale,blend);
                display.Modulate=state.Color.Lerp(b.Color,blend);display.SelfModulate=state.SelfColor.Lerp(b.SelfColor,blend);
            }
            foreach(var clock in from.Clocks)
            {
                if(!_intents.TryGetValue((clock.Creature,clock.Slot),out var intent)||!GodotObject.IsInstanceValid(intent)||intent.IsQueuedForDeletion())continue;
                var b=to.Clocks.FirstOrDefault(c=>ReferenceEquals(c.Creature,clock.Creature)&&c.Slot==clock.Slot)??clock;
                intent.Position=clock.Position;intent.Modulate=clock.Color;intent.Visible=clock.Visible;
                var holder=(Control)Get(intent,"_intentHolder");holder.Modulate=clock.HolderColor;
                holder.Position=Vector2.Up*(Mathf.Sin(Mathf.Lerp(clock.BobPhase,b.BobPhase,blend))*10+8);
                if(NCombatRoom.Instance?.GetCreatureNode(clock.Creature) is {} node)node.IntentContainer.Modulate=clock.ContainerColor;
            }
            foreach(var glyph in from.Glyphs)if(Resolve(glyph.Creature,glyph.Path) is Sprite2D sprite)
            {
                var b=to.Glyphs.FirstOrDefault(g=>ReferenceEquals(g.Creature,glyph.Creature)&&g.Path==glyph.Path)??glyph;
                sprite.Visible=true;sprite.Texture=glyph.Texture;sprite.Centered=glyph.Centered;sprite.Offset=glyph.Offset;
                sprite.Position=glyph.Position.Lerp(b.Position,blend);sprite.Scale=glyph.Scale.Lerp(b.Scale,blend);
                sprite.Rotation=Mathf.LerpAngle(glyph.Rotation,b.Rotation,blend);sprite.Modulate=glyph.Color.Lerp(b.Color,blend);
            }
            foreach(var text in from.Texts)if(Resolve(text.Creature,text.Path) is RichTextLabel label)
            {
                var b=to.Texts.FirstOrDefault(t=>ReferenceEquals(t.Creature,text.Creature)&&t.Path==text.Path)??text;
                label.Visible=true;label.BbcodeEnabled=text.Bbcode;label.HorizontalAlignment=text.Alignment;
                if(label.Text!=text.Value)label.Text=text.Value;
                label.Size=text.Size;label.PivotOffset=text.Pivot;label.Scale=text.Scale.Lerp(b.Scale,blend);
                label.Rotation=Mathf.LerpAngle(text.Rotation,b.Rotation,blend);label.Position=text.Position.Lerp(b.Position,blend);label.Modulate=text.Color.Lerp(b.Color,blend);
            }
        }
        internal void Handoff(Sample sample)
        {
            var transferred=0;var mismatches=new List<object>();
            foreach(var clock in sample.Clocks)
            {
                if(!_intents.TryGetValue((clock.Creature,clock.Slot),out var intent)||!GodotObject.IsInstanceValid(intent)||intent.IsQueuedForDeletion())continue;
                // Reconcile its native display model once, then retain this very control and phase.
                Owned.Remove(intent);try{AccessTools.Method(typeof(NIntent),"UpdateVisuals").Invoke(intent,null);}finally{Owned.Add(intent);}
                if((string?)Get(intent,"_animationName")!=clock.Animation)
                {mismatches.Add(new{Model=clock.Creature.ModelId.ToString(),clock.Slot,Expected=clock.Animation,Actual=Get(intent,"_animationName")});continue;}
                AccessTools.Field(typeof(NIntent),"_timeAccumulator").SetValue(intent,clock.Time);
                AccessTools.Field(typeof(NIntent),"_timeOffset").SetValue(intent,clock.BobPhase-(float)(Godot.Time.GetTicksMsec()*.001*Math.PI));
                intent.SetFrozen(clock.Frozen);
                var frames=(List<Texture2D>)Get(intent,"_animationFrames");
                if(frames.Count>0)
                {
                    var frame=(int)(clock.Time*15)%frames.Count;((Sprite2D)Get(intent,"_intentSprite")).Texture=frames[frame];
                    AccessTools.Field(typeof(NIntent),"_animationFrame").SetValue(intent,(int?)frame);
                }
                transferred++;
            }
            Apply(sample,sample,0);
            Runtime.Log("native-intent-handoff",new{Clocks=transferred,SameNativeControls=true,ShadowIntentViews=0,AnimationRestarted=false,Mismatches=mismatches});
        }
        public void Dispose()
        {
            foreach(var (intent,processing) in _processing)
            {
                Owned.Remove(intent);
                if(GodotObject.IsInstanceValid(intent)&&!intent.IsQueuedForDeletion())intent.SetProcess(processing);
            }
        }
    }
}

using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace ReplayMod;

// Presentation values are bound to the original creature identity, not to a disposable enemy scene.
internal sealed class CreaturePresentation
{
    private readonly Creature _creature;
    private NCreature _node;
    private NCreatureVisuals _visuals;
    private Node2D _body;
    private readonly float _defaultScale,_temporaryScale,_hue;
    private readonly Vector2 _scale,_position;
    private readonly string? _animation;
    private readonly (int Respawns,bool Dead,Color Tint,(int Revision,float Progress) Music)? _testSubject;
    private CreatureAnimator? _restoreAnimator;
    private AnimState? _restoreLoop;
    private bool _continuedAnimation;
    private static readonly System.Reflection.FieldInfo TemporaryScale=AccessTools.Field(typeof(NCreature),"_tempScale");
    private static readonly System.Reflection.FieldInfo Animator=AccessTools.Field(typeof(NCreature),"_spineAnimator");
    private static readonly System.Reflection.FieldInfo Hue=AccessTools.Field(typeof(NCreatureVisuals),"_hue");
    private const string TestSubjectMusicParameter="test_subject_progress";
    private static CombatState? _musicCombat;
    private static (int Revision,float Progress) _music;

    internal static void ClearMusic(){_musicCombat=null;_music=default;}
    internal static void MusicUpdated(string __0,float __1)
    {
        if(!NativeSeek.Enabled||__0!=TestSubjectMusicParameter||
            CombatManager.Instance.DebugOnlyGetState() is not {Encounter:TestSubjectBoss} combat)return;
        _music=(TestSubjectMusic(combat).Revision+1,__1);
        _musicCombat=combat;
        Runtime.Log("test-subject-music-parameter",new{Progress=__1,_music.Revision,NativeSeek.Restoring,
            Retiring=NativePhaseRetirement.Active});
    }
    private static (int Revision,float Progress) TestSubjectMusic(ICombatState? combat)=>ReferenceEquals(combat,_musicCombat)?_music:default;

    internal static void TestSubjectReviveStarted(NCreature node)
    {
        if(!NativeSeek.Enabled||node.Entity.Monster is not TestSubject||!node.Entity.IsAlive||
            !node.Entity.HasPower<AdaptablePower>()||node.DeathAnimationTask is not {} previous)return;
        // The retained knockout's AnimDie(false) has no post-wait removal
        // work for Test Subject. It must not gate the next native knockout.
        node.DeathAnimationTask=null;
        Runtime.Log("test-subject-knockout-animation-released",new{PreviousStillRunning=!previous.IsCompleted});
    }
    internal static void TestSubjectDeathSound(MonsterModel? __0)
    {
        if(NativeSeek.Enabled&&__0 is TestSubject subject)
            Runtime.Log("test-subject-native-death-sound",new{Sound=subject.DeathSfx,Retiring=NativePhaseRetirement.Active});
    }

    internal CreaturePresentation(Creature creature)
    {
        _creature=creature;
        _node=NCombatRoom.Instance?.GetCreatureNode(creature)??throw new InvalidDataException("checkpoint-creature-visual-missing");
        _visuals=_node.Visuals;_body=_node.Body;_defaultScale=_visuals.DefaultScale;_temporaryScale=(float)TemporaryScale.GetValue(_node)!;
        _position=_node.Position;_hue=(float)Hue.GetValue(_visuals)!;
        if(creature.Monster is TestSubject subject)
            _testSubject=(TestSubjectRespawns(subject),creature.IsDead,_node.GetSpecialNode<CanvasGroup>("%CanvasGroup")!.SelfModulate,
                TestSubjectMusic(creature.CombatState));
        if(Animator.GetValue(_node) is CreatureAnimator animator)
        {
            var state=(AnimState)AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator)!;
            // Gameplay can finish during hurt/attack/devour_start. Capture its stable loop destination.
            for(var i=0;i<16&&!state.IsLooping&&state.GetNextState() is {} next;i++)state=next;
            Codec.Require(state.IsLooping,"checkpoint-no-stable-creature-animation");_animation=state.Id;_restoreAnimator=animator;_restoreLoop=state;
        }
        var tween=AccessTools.Field(typeof(NCreature),"_scaleTween").GetValue(_node) as Tween;
        // ScaleTo returns a completed gameplay task while its visual tween may still be running.
        // Store its stable destination rather than one intermediate rendered size.
        _scale=tween is not null&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning()?
            Vector2.One*_defaultScale*_temporaryScale:_visuals.Scale;
        if(creature.Monster is Osty)
        {
            var size=Mathf.Lerp(Osty.ScaleRange.X,Osty.ScaleRange.Y,Mathf.Clamp((creature.IsAlive?creature.MaxHp:0)/150f,0,1));
            _scale=Vector2.One*size*_defaultScale;
            if(!NCombatRoom.Instance!.SceneContainer.IsAncestorOf(_visuals))_scale*=creature.CombatState!.Encounter!.GetCameraScaling();
            _position=NCombatRoom.Instance.GetCreatureNode(creature.PetOwner!.Creature)!.Position+NCreature.GetOstyOffsetFromPlayer(creature);
        }
    }
    internal bool SameNodes=>GodotObject.IsInstanceValid(_node)&&!_node.IsQueuedForDeletion()&&
        ReferenceEquals(NCombatRoom.Instance?.GetCreatureNode(_creature),_node)&&ReferenceEquals(_node.Visuals,_visuals)&&ReferenceEquals(_node.Body,_body);
    private static int TestSubjectRespawns(TestSubject subject)=>(int)AccessTools.Field(typeof(TestSubject),"_respawns").GetValue(subject)!;
    internal bool TestSubjectPhaseChanged=>_testSubject is {} phase&&_creature.Monster is TestSubject subject&&
        (phase.Dead!=_creature.IsDead||phase.Respawns!=TestSubjectRespawns(subject));
    internal void BindEnemyDisplay()
    {
        Codec.Require(!_creature.IsPlayer,"direct-back-cannot-rebind-player");
        _node=NCombatRoom.Instance!.GetCreatureNode(_creature)??throw new InvalidDataException("direct-back-restored-enemy-node-missing");
        _visuals=_node.Visuals;_body=_node.Body;
    }
    internal void Restore(bool continueAnimation=false)
    {
        Codec.Require(SameNodes,"direct-back-creature-visual-replaced");
        if(AccessTools.Field(typeof(NCreature),"_scaleTween").GetValue(_node) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
        _node.Position=_position;
        _visuals.SetScaleAndHue(_defaultScale,_hue);TemporaryScale.SetValue(_node,_temporaryScale);
        AccessTools.Method(typeof(NCreature),"DoScaleTween").Invoke(_node,[_scale]);
        if(_animation is not null&&Animator.GetValue(_node) is CreatureAnimator animator)
        {
            var state=ReferenceEquals(animator,_restoreAnimator)?_restoreLoop:null;
            state??=AnimationStates(animator).FirstOrDefault(s=>s.Id==_animation&&s.IsLooping)
                ??throw new InvalidDataException("direct-back-animation-state-missing");
            _restoreAnimator=animator;_restoreLoop=state;
            _continuedAnimation=continueAnimation&&Destination(animator)==_animation;
            if(!_continuedAnimation)AccessTools.Method(typeof(CreatureAnimator),"SetNextState").Invoke(animator,[state]);
        }
        var display=(Control)AccessTools.Field(typeof(NCreature),"_stateDisplay").GetValue(_node)!;
        if(AccessTools.Field(display.GetType(),"_showHideTween").GetValue(display) is Tween entrance&&GodotObject.IsInstanceValid(entrance))entrance.Kill();
        display.Position=(Vector2)AccessTools.Field(display.GetType(),"_originalPosition").GetValue(display)!;
        display.Modulate=Colors.White;display.Visible=true;
        if(_creature.Monster is Osty||_testSubject.HasValue)
        {
            // Native death fades this existing display, while StartReviveAnim only
            // restores alpha. A node rebuilt while dead must therefore remain Visible;
            // ToggleIsInteractable(false) would leave it hidden after native revival.
            _node.ToggleIsInteractable(true);
            if(_creature.IsDead)
            {
                display.Modulate=new Color(1,1,1,0);
                _node.DisableInteractionForDeath();
                AccessTools.Method(display.GetType(),"OnCreatureDied").Invoke(display,[_creature]);
            }
            if(_testSubject is {} subject)
            {
                _node.GetSpecialNode<CanvasGroup>("%CanvasGroup")!.SelfModulate=subject.Tint;
                var previous=TestSubjectMusic(_creature.CombatState);
                if(previous!=subject.Music&&_creature.CombatState is CombatState {Encounter:TestSubjectBoss encounter} combat&&
                    NRunMusicController.Instance is {} music)
                {
                    // Setting the old value on an already advanced event does
                    // not rewind its timeline. Start the native event again.
                    music.PlayCustomMusic(encounter.CustomBgm);
                    music.UpdateMusicParameter(TestSubjectMusicParameter,subject.Music.Progress);
                    _musicCombat=combat;_music=subject.Music;
                    Runtime.Log("test-subject-music-restored",new{From=previous.Progress,To=subject.Music.Progress,
                        FromRevision=previous.Revision,ToRevision=subject.Music.Revision,NativeEventRestarted=true});
                }
            }
            else Runtime.Log("osty-hud-restored",new{_creature.CombatId,Alive=_creature.IsAlive,display.Visible,
                Alpha=display.Modulate.A,NativeReviveCanFadeIn=true,ReviveAnimationRun=false});
        }
        AccessTools.Method(display.GetType(),"RefreshValues").Invoke(display,null);
        var bounds=_testSubject is {Respawns:>0} phase?_visuals.GetNode<Control>($"RespawnBounds{phase.Respawns}"):(Node)_visuals;
        AccessTools.Method(typeof(NCreature),"UpdateBounds",[typeof(Node)]).Invoke(_node,[bounds]);
    }
    private static string Destination(CreatureAnimator animator)
    {
        var state=(AnimState)AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator)!;
        for(var i=0;i<16&&!state.IsLooping&&state.GetNextState() is {} next;i++)state=next;
        return state.Id;
    }
    internal static void VerifyNativeOstyRevive(NCreature node)
    {
        if(!NativeSeek.Enabled||node.Entity.Monster is not Osty)return;
        var display=(Control)AccessTools.Field(typeof(NCreature),"_stateDisplay").GetValue(node)!;
        Codec.Require(NCombatUi.IsDebugHidingHpBar||display.Visible,"osty-native-revive-health-display-hidden");
        Runtime.Log("osty-native-revive-hud",new{node.Entity.CombatId,Alive=node.Entity.IsAlive,
            display.Visible,Alpha=display.Modulate.A,NativeFadeOwnsAlpha=true,VisibilityChangedByObserver=false});
    }
    internal static string? CurrentState(NCreature node)=>Animator.GetValue(node) is CreatureAnimator animator?
        ((AnimState)AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator)!).Id:null;
    internal static void PlayRecorded(NCreature node,string trigger,string? animation)
    {
        if(animation is not null&&Animator.GetValue(node) is CreatureAnimator animator)
        {
            var state=AnimationStates(animator).FirstOrDefault(s=>s.Id==animation)
                ??throw new InvalidDataException("recorded-creature-animation-state-missing");
            AccessTools.Method(typeof(CreatureAnimator),"SetNextState").Invoke(animator,[state]);
        }
        else node.SetAnimationTrigger(trigger);
        Runtime.Log("native-creature-animation-replayed",new{Model=node.Entity.ModelId.ToString(),Trigger=trigger,Recorded=animation,Actual=CurrentState(node),BusinessConditionsReevaluated=false});
    }
    private static IEnumerable<AnimState> AnimationStates(CreatureAnimator animator)
    {
        var seen=new HashSet<object>(ReferenceEqualityComparer.Instance);var states=new List<AnimState>();
        void Visit(object? item)
        {
            if(item is null||item is string||item is Delegate||!seen.Add(item))return;
            if(item is AnimState state)
            {
                states.Add(state);foreach(var f in IdentityMemento.Fields(typeof(AnimState)))Visit(f.GetValue(state));
            }
            else if(item is IDictionary dict){foreach(var entry in dict.Values)Visit(entry);}
            else if(item is IEnumerable list){foreach(var entry in list)Visit(entry);}
            else if(item.GetType().DeclaringType==typeof(AnimState))foreach(var f in IdentityMemento.Fields(item.GetType()))Visit(f.GetValue(item));
        }
        Visit(AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator));
        Visit(AccessTools.Field(typeof(CreatureAnimator),"_anyState").GetValue(animator));return states;
    }
    internal bool Matched=>SameNodes&&_visuals.Scale.IsEqualApprox(_scale)&&Mathf.IsEqualApprox(_visuals.DefaultScale,_defaultScale)&&
        Mathf.IsEqualApprox((float)TemporaryScale.GetValue(_node)!,_temporaryScale)&&_node.Position.IsEqualApprox(_position)&&
        Mathf.IsEqualApprox((float)Hue.GetValue(_visuals)!,_hue)&&(_animation is null||_node.SpineAnimation.GetCurrentAnimationName()==_animation||
            _continuedAnimation&&Animator.GetValue(_node) is CreatureAnimator animator&&Destination(animator)==_animation);
    internal object Summary()=>new{Role=_creature.IsPlayer?"player":_creature.PetOwner is not null?"pet":"enemy",Model=_creature.ModelId.ToString(),_creature.CombatId,
        DefaultScale=_defaultScale,TemporaryScale=_temporaryScale,ScaleX=_scale.X,ScaleY=_scale.Y,PositionX=_position.X,PositionY=_position.Y,Hue=_hue,Animation=_animation};
}

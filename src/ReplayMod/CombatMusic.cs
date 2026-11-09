using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class CombatMusic
{
    internal sealed record Point(CombatState State,bool EliteSecondPhase,KeyValuePair<string,CombatMusicCue>[] Parameters,BossScoreSection? Section);
    private const string CeremonialProgress="ceremonial_beast_progress";
    private const string SubjectProgress="test_subject_progress";
    private static readonly System.Reflection.FieldInfo SubjectRespawns=AccessTools.Field(typeof(TestSubject),"_respawns");
    private static readonly int[] VictorySampleMilliseconds=[0,250,1000,2500,5000,8000,12000,15000];
    private sealed record VictorySample(double ElapsedMilliseconds,int? TimelineMilliseconds,bool Playing);
    private static readonly HashSet<string> CombatParameters=new(StringComparer.Ordinal)
    {
        "queen_progress","kaiser_crab_progress","kaiser_crab_direction","the_kin_progress",
        "knowledge_demon_progress","soulfysh_progress","beckon",SubjectProgress,
        "insatiable_progress","vantom_progress","waterfall_giant_progress",
        CeremonialProgress,"ringing"
    };
    private static readonly Dictionary<string,CombatMusicCue> Values=new(StringComparer.Ordinal);
    private static CombatState? _combat;
    private static bool _eliteSecondPhase,_ceremonialSecondPhase;
    private static bool _hiddenVictoryProgress,_writingPoint;

    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(NRunMusicController),nameof(NRunMusicController.UpdateTrack),[]),
            prefix:new HarmonyMethod(typeof(CombatMusic),nameof(AllowProgressUpdate)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NRunMusicController),nameof(NRunMusicController.UpdateMusicParameter)),
            prefix:new HarmonyMethod(typeof(CombatMusic),nameof(EnsureVisibleSubjectTransition)),
            postfix:new HarmonyMethod(typeof(CombatMusic),nameof(ParameterChanged)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NRunMusicController),nameof(NRunMusicController.TriggerEliteSecondPhase),[]),
            postfix:new HarmonyMethod(typeof(CombatMusic),nameof(EliteSecondPhaseStarted)));
    }

    internal static void Clear(){_combat=null;_eliteSecondPhase=false;_ceremonialSecondPhase=false;_hiddenVictoryProgress=false;Values.Clear();}
    private static void Bind(CombatState combat)
    {
        if(ReferenceEquals(combat,_combat))return;
        Clear();_combat=combat;
    }

    private static void ParameterChanged(string __0,float __1)
    {
        if(_writingPoint||!NativeSeek.Enabled||!CombatParameters.Contains(__0)||CombatManager.Instance.DebugOnlyGetState() is not {} combat)return;
        Bind(combat);Values[__0]=Values.GetValueOrDefault(__0).Written(__0,__1);
        if(__0==CeremonialProgress&&__1==1)_ceremonialSecondPhase=true;
    }

    private static void EnsureVisibleSubjectTransition(NRunMusicController __instance,string __0,float __1)
    {
        if(_writingPoint||__0!=SubjectProgress||!NativeSeek.Enabled||NativeSeek.RetiringPresentation||NativeSeek.RebuildingTurn||RunPlayback.RebuildingRoom||
            CombatManager.Instance.DebugOnlyGetState()?.Enemies.Select(enemy=>enemy.Monster).OfType<TestSubject>().FirstOrDefault() is not {} subject||
            MusicEvent(__instance) is not {} track||Timeline(track) is not {} position)return;
        var respawns=(int)SubjectRespawns.GetValue(subject)!;
        var (source,target)=__1 switch
        {
            1 when respawns<2=>(BossScoreSection.Subject(respawns,false),BossScoreSection.Subject(respawns,true)),
            2 when respawns is 1 or 2=>(BossScoreSection.Subject(respawns-1,true),BossScoreSection.Subject(respawns,false)),
            5 when respawns==2=>(BossScoreSection.Subject(2,false),BossScoreSection.Subject(2,false)),
            _=>((BossScoreSection?)null,(BossScoreSection?)null)
        };
        if(source is {} prior&&target is {} next&&!prior.Contains(position)&&!next.Contains(position))
            Align(track,next,"visible-subject-cue");
    }

    private static bool AllowProgressUpdate()
    {
        if(!NativeSeek.RetiringPresentation||!BattleEnd.WonEvent||CombatManager.Instance.IsInProgress||
            CombatManager.Instance.DebugOnlyGetState() is not {} state||!ReferenceEquals(BattleEnd.Room?.CombatState,state))
            return true;
        // The discarded action still completes its real victory under cover.
        // Keep the music on the currently audible combat segment.
        Bind(state);_hiddenVictoryProgress=true;
        Runtime.Log("native-retired-victory-music-suppressed",new{NativeSeek.Generation,NativeSeek.Host});
        return false;
    }

    private static void EliteSecondPhaseStarted()
    {
        if(!NativeSeek.Enabled||LiveState.Run.CurrentRoom is not CombatRoom {RoomType:RoomType.Elite} room||
            !ReferenceEquals(CombatManager.Instance.DebugOnlyGetState(),room.CombatState))return;
        Bind(room.CombatState);_eliteSecondPhase=true;
        Runtime.Log("native-elite-music-second-phase",new{Encounter=room.Encounter.Id.ToString(),NativeSeek.Host,NativeSeek.Restoring,Retiring=NativePhaseRetirement.Active});
    }

    internal static void AtVictory(CombatRoom room)
    {
        if(!NativeSeek.Enabled||NativeSeek.RetiringPresentation||RunPlayback.RebuildingRoom||
            room.Encounter is not CeremonialBeastBoss||NRunMusicController.Instance is not {} music||
            RunPatches.Field(music,"_proxy") is not Node proxy||ActiveEvent(proxy) is not {} track||
            !track.HasMethod("get_playback_state"))return;
        if(_ceremonialSecondPhase)Align(track,BossScoreSection.Beast(second:true),"beast-victory-after-phase-two");
        TaskHelper.RunSafely(KeepCeremonialVictoryCue(room,music,proxy,track));
    }

    private static GodotObject? ActiveEvent(Node proxy)
    {
        using var value=proxy.Get("_musicEv");
        return value.AsGodotObject();
    }
    private static GodotObject? MusicEvent(NRunMusicController music)=>
        RunPatches.Field(music,"_proxy") is Node proxy&&GodotObject.IsInstanceValid(proxy)?ActiveEvent(proxy):null;
    private static int? Timeline(GodotObject track)=>GodotObject.IsInstanceValid(track)&&track.HasMethod("get_timeline_position")?
        track.Call("get_timeline_position").AsInt32():null;
    private static void Align(GodotObject track,BossScoreSection section,string source)
    {
        if(Timeline(track) is not {} position||section.Contains(position)||!track.HasMethod("set_timeline_position"))return;
        track.Call("set_timeline_position",section.Entry);
        Runtime.Log("native-boss-score-section-aligned",new{Source=source,PreviousTimelineMs=position,EntryTimelineMs=section.Entry,
            NativeSeek.Host,NativeSeek.Generation});
    }

    internal static void AlignComputedTurn(Point point)
    {
        if(point.Section is not {} section||NRunMusicController.Instance is not {} music||MusicEvent(music) is not {} track||
            !ReferenceEquals(point.State,CombatManager.Instance.DebugOnlyGetState())||
            Timeline(track) is not {} position||section.Contains(position))return;
        Align(track,section,"computed-turn");
        WritePoint(music,point);
    }

    private static string? SectionParameter(CombatState state)=>state.Encounter switch
    {
        TestSubjectBoss=>SubjectProgress,
        CeremonialBeastBoss=>CeremonialProgress,
        _=>null
    };

    private static void WritePoint(NRunMusicController music,Point point)
    {
        var phaseParameter=SectionParameter(point.State);
        _writingPoint=true;
        try
        {
            foreach(var (parameter,cue) in point.Parameters)
                if(parameter!=phaseParameter||point.Section is null)music.UpdateMusicParameter(parameter,cue.Value);
            if(phaseParameter is not null&&point.Section is {} section)music.UpdateMusicParameter(phaseParameter,section.Cue);
        }
        finally{_writingPoint=false;}
        if(point.State.Encounter is CeremonialBeastBoss)_ceremonialSecondPhase=point.Section is {Cue:1};
    }

    private static async Task KeepCeremonialVictoryCue(CombatRoom room,NRunMusicController music,Node proxy,GodotObject track)
    {
        var started=Stopwatch.GetTimestamp();
        var lastWrite=started;
        var writes=0;
        var samples=new List<VictorySample>();
        // The phase transition can reset the event's effective parameter after
        // the native background has already sent 5. Resend on the same event
        // while it settles. The death one-shot is already owned by the hit.
        while(PlayerLoop.Active&&!PlayerLoop.TearingDown&&!NativeSeek.RetiringPresentation&&!NativeSeek.Stopping&&
            (!RunPlayback.Active||!RunPlayback.Stopping&&!RunPlayback.RoomJumping)&&room.IsPreFinished&&
            ReferenceEquals(RunManager.Instance.DebugOnlyGetState()?.CurrentRoom,room)&&
            GodotObject.IsInstanceValid(proxy)&&ReferenceEquals(ActiveEvent(proxy),track))
        {
            var elapsed=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var playing=NativePhaseRetirement.FmodSoundPlaying(track);
            if(!playing||samples.Count<VictorySampleMilliseconds.Length&&elapsed>=VictorySampleMilliseconds[samples.Count])
                samples.Add(new(elapsed,Timeline(track),playing));
            if(!playing||elapsed>=VictorySampleMilliseconds[^1])break;
            if(elapsed<5000&&(writes==0||Stopwatch.GetElapsedTime(lastWrite).TotalMilliseconds>=200))
            {
                music.UpdateMusicParameter(CeremonialProgress,5f);
                lastWrite=Stopwatch.GetTimestamp();writes++;
            }
            await Runtime.NextFrame();
        }
        var sameTrack=GodotObject.IsInstanceValid(proxy)&&ReferenceEquals(ActiveEvent(proxy),track);
        Runtime.Log("native-ceremonial-victory-timeline",new{Writes=writes,SameTrack=sameTrack,Samples=samples,
            ElapsedMilliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            TrackStillPlaying=sameTrack&&NativePhaseRetirement.FmodSoundPlaying(track),RepeatedDeathSound=false});
    }

    internal static Point Capture(CombatState combat)
    {
        Bind(combat);
        BossScoreSection? section=combat.Enemies.Select(enemy=>enemy.Monster).FirstOrDefault() switch
        {
            TestSubject subject=>BossScoreSection.Subject((int)SubjectRespawns.GetValue(subject)!,subject.Creature.IsDead),
            CeremonialBeast beast=>BossScoreSection.Beast(beast.IsInSecondPhase),
            _=>null
        };
        return new(combat,_eliteSecondPhase||NativeSpawnPresentation.PendingElitePhase(combat),Values.ToArray(),section);
    }

    internal static void Restore(Point point,bool fromVictory)
    {
        if(!ReferenceEquals(point.State,CombatManager.Instance.DebugOnlyGetState())||
            NRunMusicController.Instance is not {} music||point.State.Encounter is not {} encounter)return;
        Bind(point.State);
        var restoreVictoryProgress=fromVictory&&!_hiddenVictoryProgress;
        _hiddenVictoryProgress=false;
        var phaseChanged=_eliteSecondPhase!=point.EliteSecondPhase;
        var parametersChanged=Values.Count!=point.Parameters.Length||point.Parameters.Any(pair=>!Values.TryGetValue(pair.Key,out var value)||value!=pair.Value);
        if(!restoreVictoryProgress&&!phaseChanged&&!parametersChanged)
        {
            AlignComputedTurn(point);
            if(fromVictory)Runtime.Log("native-combat-music-kept",new{Encounter=encounter.Id.ToString(),HiddenVictory=true});
            return;
        }

        var track=encounter.HasBgm?encounter.CustomBgm:null;
        var rewindActElite=track is null&&_eliteSecondPhase&&!point.EliteSecondPhase;
        if(rewindActElite)
            track=AccessTools.Field(typeof(NRunMusicController),"_currentTrack").GetValue(music) as string;
        var sameSection=point.Section is {} section&&MusicEvent(music) is {} active&&Timeline(active) is {} pos&&section.Contains(pos);
        var phaseParameter=SectionParameter(point.State);
        var localCueChanged=phaseParameter is not null&&Values.GetValueOrDefault(phaseParameter)!=
            point.Parameters.FirstOrDefault(pair=>pair.Key==phaseParameter).Value;
        var restart=track is {Length:>0}&&(restoreVictoryProgress||!sameSection||localCueChanged);
        var refreshProgress=restoreVictoryProgress||phaseChanged||restart;
        // The act event reads global Progress when it starts. Seed phase one
        // before reopening it so the next native wake can reach its phase-two cue.
        if(rewindActElite)music.UpdateTrack();
        if(restart)music.PlayCustomMusic(track!);
        if(refreshProgress&&!rewindActElite)music.UpdateTrack();
        if(point.EliteSecondPhase&&refreshProgress)music.TriggerEliteSecondPhase();
        if(point.Section is {} target&&MusicEvent(music) is {} eventTrack)Align(eventTrack,target,"restored-point");
        WritePoint(music,point);
        Values.Clear();
        foreach(var (parameter,cue) in point.Parameters)Values[parameter]=cue;
        _eliteSecondPhase=point.EliteSecondPhase;
        Runtime.Log("native-combat-music-restored",new{Encounter=encounter.Id.ToString(),FromVictory=fromVictory,
            point.EliteSecondPhase,Parameters=point.Parameters,NativeEventRestarted=restart,
            NativeProgressRefreshed=refreshProgress,ProgressSetBeforeRestart=rewindActElite&&restart});
    }
}

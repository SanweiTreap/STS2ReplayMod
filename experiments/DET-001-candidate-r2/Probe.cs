using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using Det001.Core;
using Json=Det001.Core.Json;
using Projection=Det001.Core.Projection;

namespace Det001;

[ModInitializer(nameof(Initialize))]
public static class Probe
{
    public const string Owner="STS2ReplayMod.DET001.FirstReshuffle.candidate-r2";
    private const string FixedHash="0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    private static readonly string Process=System.Environment.ProcessId+":"+System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
    private static PrivateStore? _store;
    private static Flow? _flow;
    private static string? _attempt,_native;
    private static ArmData? _arm;
    private static SealData? _seal;
    private static string? _a;
    private static string[]? _aStates;
    private static AcceptedAction[]? _aActions;
    private static Trajectory? _trajectory;
    private static readonly List<PointData> _points=[];
    private static readonly List<ActionData> _actions=[];
    private static ShuffleData? _shuffle,_aShuffle;
    private static BoundaryLedger _boundaries=new();
    private static int _dispatchIntents;
    internal static Binding Binding=>_seal?.Binding??throw new InvalidDataException("shuffle-seal-missing");
    internal static string InitialEncounter=>Projection.Thaw(_trajectory?.States.FirstOrDefault()??throw new InvalidDataException("shuffle-initial-missing")).Position.Encounter;
    private static bool _dispatching;
    internal static bool Dispatching=>_dispatching;
    internal static Flow? CurrentFlow=>_flow;
    internal static Trajectory? CurrentTrajectory=>_trajectory;
    internal static bool IsB=>_b;
    private static int _eventIndex,_announcedTerminal;
    private static int _pointIndex;
    private static bool _b,_opened;
    private static string? _candidateHash;
    private static System.Threading.Timer? _timer;
    public static void Initialize()
    {
        Harmony? harmony=null;
        try
        {
            var dll=typeof(NGame).Assembly.Location;
            Contract.Require(new FileInfo(dll).Length==9757184 && Json.Hash(File.ReadAllBytes(dll))==FixedHash,"game-identity");
            var releasePath=Path.Combine(Directory.GetParent(Path.GetDirectoryName(dll)!)!.FullName,"release_info.json");
            using var release=JsonDocument.Parse(File.ReadAllText(releasePath));
            Contract.Require(release.RootElement.GetProperty("version").GetString()=="v0.111.0" &&
                release.RootElement.GetProperty("commit").GetString()=="41cef1ea" && typeof(Harmony).Assembly.GetName().Version?.ToString()=="2.4.2.0","release-identity");
            _candidateHash=Json.Hash(File.ReadAllBytes(typeof(Probe).Assembly.Location));
            harmony=new(Owner); harmony.PatchAll(typeof(Probe).Assembly);
        }
        catch { try { harmony?.UnpatchAll(Owner); } catch {} }
    }
    internal static void Safe(Action action)
    {
        try { action(); }
        catch(PreparationException ex) { try { Fail("FIXTURE_NOT_ELIGIBLE-"+ex.Code); } catch {} }
        catch(Exception ex) { try { Fail("observer-"+ex.GetType().Name); } catch {} }
    }
    internal static bool Identity()
    {
        var ids=ModManager.GetLoadedMods().Select(x=>x.manifest?.id).ToArray();
        return ids.Length==1 && ids[0]=="DET001" && SaveManager.Instance?.CurrentProfileId==1 &&
            UserDataPathProvider.IsRunningModded && UserDataPathProvider.GetProfileDir(1).Replace('\\','/')=="modded/profile1";
    }
    internal static void MainMenu()
    {
        if(_opened)return; _opened=true;
        if(!Identity()) { Marker("DET001 PRECHECK_BLOCKED"); return; }
        var userRoot=ProjectSettings.GlobalizePath("user://"); _store=new(userRoot);
        _native=Path.GetFullPath(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedPath(1,"saves/"+RunSaveManager.runSaveFileName)));
        var profile=Path.GetFullPath(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedBasePath(1,null,null)));
        Contract.Require(_native==Path.Combine(profile,"saves",RunSaveManager.runSaveFileName),"native-path-scope");
        PrivateStore.NoLinks(_native);
        var previous=_store.Active();
        _b=previous is not null && AttemptValidation.ReadyB(_store,previous);
        _attempt=_b?previous:Guid.NewGuid().ToString("N");
        if(_b)
        {
            Contract.Require(!_store.Exists(_attempt!,"terminal.json") && !_store.Exists(_attempt!,"compare.json"),"attempt-ended");
            _arm=_store.Read<ArmData>(_attempt!,"arm.json"); _seal=_store.Read<SealData>(_attempt!,"seal.json");
            _aStates=Enumerable.Range(0,5).Select(i=>Encoding.UTF8.GetString(_store.ReadBytes(_attempt!,"A.point-"+i+".semantic.json"))).ToArray();
            _a=_aStates[0];
            var cap=Enumerable.Range(0,5).Select(i=>_store.Read<PointData>(_attempt!,"A.point-"+i+".capture.json")).ToArray();
            var actions=Enumerable.Range(1,4).Select(i=>_store.Read<ActionData>(_attempt!,"A.action-"+i+".json")).ToArray();
            _aActions=actions.Select(a=>a.Action).ToArray();
            var complete=_store.Read<CompleteData>(_attempt!,"A.complete.json");
            var ready=_store.Read<ReadyData>(_attempt!,"A.ready-B.json");
            var original=_store.ReadBytes(_attempt!,"checkpoint.private.json",PrivateStore.NativeCap);
            var current=NativeCheckpoint.StableRead(_native);
            _aShuffle=_store.Read<ShuffleData>(_attempt!,"A.shuffle.json");
            AttemptValidation.BInput(_arm,_seal,cap,actions,complete,ready,_attempt!,_candidateHash!,Process,_aStates,original,current.Bytes,_aShuffle);
            _boundaries=new(_aShuffle.Trace);
            _trajectory=new(_seal.Binding,_aStates,_aActions);
            _store.Write(_attempt!,"B.claim.json",new { Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Process });
        }
        else if(previous is not null)_store.Write(_attempt!,"fresh-A.json",new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Previous=previous,Classification="PREVIOUS_INCOMPLETE_OR_ENDED_RETAINED",Process});
        _flow=new(new MonotonicClock(),e=>_store.WriteBytes(_attempt!,(_b?"B":"A")+".event-"+Interlocked.Increment(ref _eventIndex).ToString("D3")+".json",
            Encoding.UTF8.GetBytes(Json.Serialize(new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Attempt=_attempt,Process,Point=_pointIndex,ExpectedRound=Stages.RoundAt(_pointIndex),Event=e})),Flow.MaxRecordBytes));
        ShuffleBridge.Reset();
        _timer=new System.Threading.Timer(_=>Safe(()=> { _flow.Poll(); AnnounceTerminal(); }),null,100,100);
        var game=NGame.Instance;
        Contract.Require(game is not null && GodotObject.IsInstanceValid(game) && game.IsInsideTree(),"ticker-owner");
        game!.GetTree().ProcessFrame+=Tick;
    }
    private static void Tick()=>Safe(()=>
    {
        if(_flow is null)return;
        ShuffleBridge.Tick();
        if(_flow.Stage==Stage.Startup)
        {
            if(_flow.FirstFrame(_b)) Marker(_b?"DET001 READY_B_CONTINUE":"DET001 PRECHECK_PASS READY_A_ARM");
            AnnounceTerminal(); return;
        }
        if(_flow.Stage is not (Stage.CaptureA or Stage.CaptureB))return;
        Contract.Require(Identity(),"environment-changed");
        var expected=_b?Projection.Thaw(_a!):_trajectory!.States.Length>0?Projection.Thaw(_trajectory.States[0]):null;
        var step=_trajectory!.Point;
        var readiness=LiveRead.Read(_seal!.Binding,expected?.Position.RoomModel,expected?.Position.Encounter,_trajectory.Round);
        var frame=checked((long)Engine.GetProcessFrames());
        if(!_flow.Tick(frame,readiness.Fields,readiness.Identity)) { AnnounceTerminal(); return; }
        var semantic=LiveRead.Capture();
        Contract.Playable(_seal.Binding,Projection.Thaw(semantic),expected?.Position.RoomModel,expected?.Position.Encounter,_trajectory.Round);
        var state=RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("eligibility-state-missing");
        var qualification=EligibilityRead.Point(state);Eligibility.Point(qualification,_arm!.Eligibility,Projection.Thaw(semantic),(int)step);
        if(step==Step.Round3 && (_shuffle is null || _boundaries.Count!=5)){Fail("TARGET_NOT_REACHED-natural-reshuffle");return;}
        if(!_flow.Poll() || _flow.Stage is not (Stage.CaptureA or Stage.CaptureB))return;
        var phase=_b?"B":"A";
        var differences=_trajectory.Capture(semantic);
        _store!.WriteBytes(_attempt!,phase+".point-"+(int)step+".semantic.json",Encoding.UTF8.GetBytes(semantic));
        var capture=new PointData(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,phase,step,Json.Hash(semantic),_flow.Window,qualification);
        AttemptValidation.Window(capture.Window); _store.Write(_attempt!,phase+".point-"+(int)step+".capture.json",capture); _points.Add(capture);
        if(_b)
        {
            _store.Write(_attempt!,"compare-point-"+(int)step+".json",new { Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Attempt=_attempt,Step=step,
                AProcess=_arm!.Process,BProcess=Process,CheckpointHash=_seal.Binding.Checkpoint.Hash,AHash=Json.Hash(_aStates![(int)step]),BHash=Json.Hash(semantic),
                Result=differences.Length==0?"SEMANTIC_EQUIVALENT":"SEMANTIC_DIFFERENT",Differences=differences });
        }
        if(!_flow.Captured()) { AnnounceTerminal(); return; }
        if(_b && differences.Length>0)
        {
            _store.Write(_attempt!,"B.stable-difference.json",new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Process,Step=step,Differences=differences,StablePoints=_points.Count,DispatchIntents=_dispatchIntents,AcceptedCompleted=_actions.Count,ObservedBoundaries=_boundaries.Count,OriginalStatus="Not sampled at stable difference; no cancellation"});
            Fail("stable-semantic-difference");return;
        }
        if(_trajectory.Done)
        {
            AttemptValidation.Shuffle(_shuffle!.Trace,_seal.Binding,_trajectory.States);
            Contract.Require(_boundaries.Difference is null && (!_b || TraceContract.Normalize(_shuffle.Trace)==TraceContract.Normalize(_aShuffle!.Trace)),"shuffle-internal-semantic-difference");
            if(_flow.Complete(()=>
            {
                if(!_b)_store.Write(_attempt!,"A.complete.json",new CompleteData(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,_attempt!,_seal.Binding.Checkpoint.Hash,
                    _trajectory.States.Select(Json.Hash).ToArray(),_points.Select(p=>Json.Hash(Json.Serialize(p))).ToArray(),_actions.Select(a=>Json.Hash(Json.Serialize(a))).ToArray(),Json.Hash(Json.Serialize(_shuffle))));
                else _store.Write(_attempt!,"B.complete.json",new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Attempt=_attempt,Process,
                    CheckpointHash=_seal.Binding.Checkpoint.Hash,ComparedPoints=_points.Count,DispatchIntents=_dispatchIntents,AcceptedCompleted=_actions.Count,ObservedBoundaries=_boundaries.Count,ShuffleHash=Json.Hash(Json.Serialize(_shuffle)),
                    SnapshotHashes=_trajectory.States.Select(Json.Hash).ToArray(),CaptureHashes=_points.Select(p=>Json.Hash(Json.Serialize(p))).ToArray(),ActionHashes=_actions.Select(a=>Json.Hash(Json.Serialize(a))).ToArray(),
                    Result=differences.Length==0?"SEMANTIC_EQUIVALENT":"SEMANTIC_DIFFERENT"});
            },()=>
            {
                if(!_b)
                {
                    var complete=_store.Read<CompleteData>(_attempt!,"A.complete.json");
                    _store.Write(_attempt!,"A.ready-B.json",new ReadyData(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,_attempt!,Json.Hash(Json.Serialize(complete))));
                }
            }))Marker(_b?"DET001 B_COMPLETE_STOP_INPUT "+(differences.Length==0?"SEMANTIC_EQUIVALENT":"SEMANTIC_DIFFERENT"):"DET001 A_COMPLETE_STOP_INPUT");
            AnnounceTerminal(); return;
        }
        if(_b)DispatchNext();
        else if(step is Step.Initial or Step.Round2)Marker("DET001 READY_A_CARD sequence="+(_actions.Count+1)+" hand_index_0based="+_trajectory.Selection!.Index+" model="+_trajectory.Selection.Model+" target=Self:null; play exactly this card then stop");
        else Marker("DET001 READY_A_ENDTURN sequence="+(_actions.Count+1)+"; EndTurn once then stop until next round capture");
    });
    internal static void Arm()
    {
        if(_flow?.Stage!=Stage.ReadyArm)return;
        Contract.Require(Identity(),"arm-environment");
        var state=RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("arm-state");
        Contract.Require(state.Players.Count==1 && state.Players[0].Character.Id.ToString()=="CHARACTER.IRONCLAD" && state.AscensionLevel==0 && state.CurrentActIndex==0 && state.GameMode.ToString()=="Standard" &&
            CombatManager.Instance?.IsInProgress!=true && state.CurrentMapPoint is not null && state.CurrentMapCoord.HasValue &&
            !RunManager.Instance.IsPaused && NMapScreen.Instance is {IsOpen:true,IsTraveling:false},"arm-scene");
        Contract.Require(SaveManager.Instance.CurrentRunSaveTask is null || SaveManager.Instance.CurrentRunSaveTask.IsCompletedSuccessfully,"predecessor-save-still-pending");
        // Actual eligibility is checked before native input/Arm activation. No fabricated defaults.
        var qualification=EligibilityRead.Arm(state);
        // Do not consume an Arm or read another save slot when the predecessor is unsuitable.
        var before=NativeCheckpoint.StableRead(_native!);
        var predecessor=state.CurrentMapCoord!.Value;
        var targetPoint=state.CurrentMapPoint!.Children.Where(x=>x.PointType==MapPointType.Monster)
            .Select(x=>(Point:x,Coord:FindCoord(state.Map,x))).OrderBy(x=>x.Coord.row).ThenBy(x=>x.Coord.col).FirstOrDefault();
        Contract.Require(targetPoint.Point is not null,"no-direct-monster-target");
        var target=new Target(Contract.Text(state.Rng.ToSerializable().Seed,"arm-seed"),state.Players[0].Character.Id.ToString(),0,
            targetPoint.Coord.row,targetPoint.Coord.col,predecessor.row,predecessor.col,state.TotalFloor);
        Contract.Arm(target,state.TotalFloor,before.Facts);
        _arm=new(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,target,before.Facts,qualification);
        _store!.Write(_attempt!,"native-path.private.json",new {Schema=PrivateStore.Schema,Profile="modded/profile1",NativePath=_native});
        _store!.Write(_attempt!,"arm.json",_arm); _store.Activate(_attempt!);
        if(_flow.Arm()) Marker($"DET001 READY_A_NODE row={target.Row} col={target.Col} floor={target.ArmFloor}; click only this Monster then stop input");
    }
    private static MapCoord FindCoord(ActMap map,MapPoint point)
    {
        for(var row=0;row<map.GetRowCount();row++)for(var col=0;col<map.GetColumnCount();col++)
            if(ReferenceEquals(map.GetPoint(col,row),point))return new(col,row);
        throw new InvalidDataException("map-coordinate-missing");
    }
    internal static Task ObserveSave(Task original,AbstractRoom? room,bool saveProgress)
    {
        try
        {
            if(_seal is not null && _flow?.Stage is Stage.CaptureA or Stage.CaptureB or Stage.AComplete or Stage.Action)
            { Fail("unexpected-native-save-during-trajectory"); return original; }
            if(_flow?.Stage!=Stage.AwaitSave || room is not null || !saveProgress)return original;
            var coord=RunManager.Instance.DebugOnlyGetState()?.CurrentMapCoord;
            if(coord?.row!=_arm!.Target.Row || coord?.col!=_arm.Target.Col) { Fail("wrong-map-action"); return original; }
            if(!_flow.SaveStarted())return original;
            Marker("DET001 MATCHED_SAVE_TASK_WRAPPED");
            return ProductionObserver.Wrap(original,()=>
            {
                if(!_flow.BeginSeal())return Task.CompletedTask;
                Contract.Require(Identity(),"seal-environment");
                var native=NativeCheckpoint.StableRead(_native!);
                NativeCheckpoint.MatchArm(NativeCheckpoint.Parse(native.Bytes).Save,_arm!.Eligibility);
                var binding=Contract.Seal(_arm.Target,_arm.Predecessor,native.Facts);
                _store!.WriteBytes(_attempt!,"checkpoint.private.json",native.Bytes,PrivateStore.NativeCap);
                _seal=new(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,binding);
                _store.Write(_attempt!,"seal.json",_seal);
                _trajectory=new(binding);
                LiveRead.ResetIdentity(); _flow.Sealed(); AnnounceTerminal(); return Task.CompletedTask;
            },Fail);
        }
        catch(Exception ex) { try { Fail("save-observer-"+ex.GetType().Name); } catch {} return original; }
    }
    internal static void Continue(RunState state,SerializableRun save)
    {
        if(_flow?.Stage!=Stage.ReadyB) { if(_flow is not null && _flow.Stage is not (Stage.Terminal or Stage.Complete))Fail("unexpected-continue"); return; }
        Contract.Require(Identity(),"continue-environment");
        if(!_flow.ContinueStarted())return;
        var native=NativeCheckpoint.StableRead(_native!);
        var privateBytes=_store!.ReadBytes(_attempt!,"checkpoint.private.json",PrivateStore.NativeCap);
        var parsed=NativeCheckpoint.Parse(privateBytes).Save;
        Contract.Require(Json.Canonical(JsonSerializationUtility.ToJson(save))==Json.Canonical(JsonSerializationUtility.ToJson(parsed)),"continue-loaded-payload-mismatch");
        Contract.Require(state.Players.Count==1,"continue-players");
        Contract.Continue(_seal!.Binding,native.Facts,_arm!.Process,Process,
            Contract.Text(state.Rng.ToSerializable().Seed,"continue-seed"),state.Players[0].Character.Id.ToString(),state.CurrentActIndex,
            state.TotalFloor,state.CurrentMapCoord?.row,state.CurrentMapCoord?.col);
        LiveRead.ResetIdentity();
    }
    internal static void ActionSucceeded(AcceptedAction accepted,int turn)
    {
        Contract.Require(_flow?.Stage==Stage.Action && _trajectory is not null,"action-completion-phase");
        _trajectory!.Succeeded(accepted,true,true,false,false,turn);
        var phase=_b?"B":"A";
        var data=new ActionData(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,phase,accepted);
        _store!.Write(_attempt!,phase+".action-"+accepted.Sequence+".json",data); _actions.Add(data);
        _pointIndex=accepted.Sequence;
        LiveRead.ResetIdentity(); _flow!.ActionSucceeded(_b); AnnounceTerminal();
    }
    private static void DispatchNext()
    {
        Contract.Require(_b && _flow?.Stage==Stage.AComplete && _trajectory is not null,"dispatch-not-ready");
        var state=RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("dispatch-state");
        Contract.Require(Identity() && state.Players.Count==1,"dispatch-environment");
        var player=state.Players[0]; var turn=player.PlayerCombatState?.TurnNumber??throw new InvalidDataException("turn-missing");
        var action=_trajectory!.Planned(turn);
        var snapshot=Projection.Thaw(LiveRead.Capture()); Actions.Validate(action,snapshot,turn);
        _flow!.RunDispatch(()=>
        {
            _trajectory.Dispatch(action,turn);
            Contract.Require(_dispatchIntents<4,"four-operation-hard-limit");_dispatchIntents++;
            _store!.Write(_attempt!,"B.dispatch-"+action.Sequence+".json",new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Process,Action=action});
            _dispatching=true;
            try
            {
                if(action.Type==Operation.CardPlay)
                {
                    var index=Actions.Resolve(action.Card!,snapshot); var card=player.PlayerCombatState!.Hand.Cards[index];
                    Contract.Require(card is MegaCrit.Sts2.Core.Models.Cards.DefendIronclad && card.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.Self,"dispatch-card-content");
                    Contract.Require(card.TryManualPlay(null),"dispatch-card-rejected");
                }
                else
                {
                    Contract.Require(!CombatManager.Instance.IsPlayerReadyToEndTurn(player),"end-already-ready");
                    RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new MegaCrit.Sts2.Core.GameActions.EndPlayerTurnAction(player,turn));
                }
            }
            finally { _dispatching=false; }
        });
    }
    internal static void ObserveBoundary<T>(string name,string normalized,EventStamp stamp,(TaskStatus? Shuffle,TaskStatus? Draw) status,T raw)
    {
        if(_flow?.Stage is Stage.Terminal or Stage.Complete)return;
        _store!.Write(_attempt!,(_b?"B":"A")+".boundary-"+name+".json",new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Attempt=_attempt,Process,Stamp=stamp,Raw=raw,Normalized=normalized,OriginalShuffleStatus=status.Shuffle?.ToString()??"UnknownNotReturned",OriginalDrawStatus=status.Draw?.ToString()??"UnknownNotReturned"});
        if(_boundaries.Observe(name,normalized,stamp,_points.Count,_dispatchIntents,_actions.Count,status))return;
        // Freeze before closing the observation. Never cancel or replace in-flight game work.
        try { _store!.Write(_attempt!,"B.internal-difference.json",new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Attempt=_attempt,Process,Answer=_boundaries.Difference,ObservedBoundaries=_boundaries.Count}); }
        finally { Fail("internal-semantic-difference"); }
    }
    internal static void CommitShuffle(ShuffleTrace trace)
    {
        Contract.Require(_shuffle is null && _boundaries.Count==5 && _boundaries.Difference is null && _flow?.Stage is Stage.CaptureA or Stage.CaptureB,"shuffle-commit-phase");
        _shuffle=new(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,_attempt!,Binding.Checkpoint.Hash,trace);
        _store!.Write(_attempt!,(_b?"B":"A")+".shuffle.json",_shuffle);
    }
    internal static void Fail(string reason)
    {
        _flow?.Fail(reason);
        if(_flow is null)
        {
            try { if(_store is not null && _attempt is not null && !_store.Exists(_attempt,"terminal.json"))
                _store.WriteBytes(_attempt,"terminal.json",Encoding.UTF8.GetBytes(Json.Serialize(new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Process,Reason=reason})),Flow.MaxRecordBytes); } catch{}
            if(Interlocked.Exchange(ref _announcedTerminal,1)==0)Marker("DET001 PRECHECK_BLOCKED "+reason);
        }
        else AnnounceTerminal();
    }
    private static void AnnounceTerminal()
    {
        if(_flow?.Stage!=Stage.Terminal || Interlocked.Exchange(ref _announcedTerminal,1)!=0)return;
        try { _store!.WriteBytes(_attempt!,"terminal.json",Encoding.UTF8.GetBytes(Json.Serialize(new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Process,Reason=_flow.TerminalReason,Diagnostics=_flow.Diagnostics,Action=_flow.ActionDiagnostics})),Flow.MaxRecordBytes); } catch{}
        Marker("DET001 TERMINAL_STOP_INPUT "+_flow.TerminalReason);
    }
    private static void Marker(string value) { try { GD.Print(value); } catch { _flow?.Fail("marker-failure"); } }
}

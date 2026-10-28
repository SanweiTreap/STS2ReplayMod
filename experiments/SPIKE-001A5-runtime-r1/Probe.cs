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
using Spike001A5.Core;
using Json=Spike001A5.Core.Json;
using Projection=Spike001A5.Core.Projection;

namespace Spike001A5;

[ModInitializer(nameof(Initialize))]
public static class Probe
{
    public const string Owner="STS2ReplayMod.SPIKE001A5.RoomBefore.runtime-r1";
    private const string FixedHash="0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    private static readonly string Process=System.Environment.ProcessId+":"+System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
    private static PrivateStore? _store;
    private static Flow? _flow;
    private static string? _attempt,_native;
    private static ArmData? _arm;
    private static SealData? _seal;
    private static string? _a;
    private static int _eventIndex,_announcedTerminal;
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
        catch(Exception ex) { try { Fail("observer-"+ex.GetType().Name); } catch {} }
    }
    internal static bool Identity()
    {
        var ids=ModManager.GetLoadedMods().Select(x=>x.manifest?.id).ToArray();
        return ids.Length==1 && ids[0]=="SPIKE001A5" && SaveManager.Instance?.CurrentProfileId==1 &&
            UserDataPathProvider.IsRunningModded && UserDataPathProvider.GetProfileDir(1).Replace('\\','/')=="modded/profile1";
    }
    internal static void MainMenu()
    {
        if(_opened)return; _opened=true;
        if(!Identity()) { Marker("SPIKE001A5 PRECHECK_BLOCKED"); return; }
        var userRoot=ProjectSettings.GlobalizePath("user://"); _store=new(userRoot);
        _native=Path.GetFullPath(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedPath(1,"saves/"+RunSaveManager.runSaveFileName)));
        var profile=Path.GetFullPath(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedBasePath(1,null,null)));
        Contract.Require(_native==Path.Combine(profile,"saves",RunSaveManager.runSaveFileName),"native-path-scope");
        PrivateStore.NoLinks(_native);
        _attempt=_store.Active(); _b=_attempt is not null;
        if(_b)
        {
            Contract.Require(!_store.Exists(_attempt!,"terminal.json") && !_store.Exists(_attempt!,"compare.json"),"attempt-ended");
            _arm=_store.Read<ArmData>(_attempt!,"arm.json"); _seal=_store.Read<SealData>(_attempt!,"seal.json");
            _a=Encoding.UTF8.GetString(_store.ReadBytes(_attempt!,"A.semantic.json"));
            var cap=_store.Read<CaptureData>(_attempt!,"A.capture.json");
            var complete=_store.Read<CaptureData>(_attempt!,"A.complete.json");
            var original=_store.ReadBytes(_attempt!,"checkpoint.private.json",PrivateStore.NativeCap);
            var current=NativeCheckpoint.StableRead(_native);
            AttemptValidation.BInput(_arm,_seal,cap,complete,_candidateHash!,Process,_a,original,current.Bytes);
            _store.Write(_attempt!,"B.claim.json",new { Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Process });
        }
        else _attempt=Guid.NewGuid().ToString("N");
        _flow=new(new MonotonicClock(),e=>_store.WriteBytes(_attempt!,(_b?"B":"A")+".event-"+Interlocked.Increment(ref _eventIndex).ToString("D3")+".json",
            Encoding.UTF8.GetBytes(Json.Serialize(new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Attempt=_attempt,Process,Event=e})),Flow.MaxRecordBytes));
        _timer=new System.Threading.Timer(_=>Safe(()=> { _flow.Poll(); AnnounceTerminal(); }),null,100,100);
        var game=NGame.Instance;
        Contract.Require(game is not null && GodotObject.IsInstanceValid(game) && game.IsInsideTree(),"ticker-owner");
        game!.GetTree().ProcessFrame+=Tick;
    }
    private static void Tick()=>Safe(()=>
    {
        if(_flow is null)return;
        if(_flow.Stage==Stage.Startup)
        {
            if(_flow.FirstFrame(_b)) Marker(_b?"SPIKE001A5 READY_B_CONTINUE":"SPIKE001A5 PRECHECK_PASS READY_A_ARM");
            AnnounceTerminal(); return;
        }
        if(_flow.Stage is not (Stage.CaptureA or Stage.CaptureB))return;
        Contract.Require(Identity(),"environment-changed");
        var expected=_b?Projection.Thaw(_a!):null;
        var readiness=LiveRead.Read(_seal!.Binding,expected?.Position.RoomModel,expected?.Position.Encounter);
        var frame=checked((long)Engine.GetProcessFrames());
        if(!_flow.Tick(frame,readiness.Fields,readiness.Identity)) { AnnounceTerminal(); return; }
        var semantic=LiveRead.Capture();
        Contract.Playable(_seal.Binding,Projection.Thaw(semantic),expected?.Position.RoomModel,expected?.Position.Encounter);
        if(!_flow.Poll() || _flow.Stage is not (Stage.CaptureA or Stage.CaptureB))return;
        var phase=_b?"B":"A";
        _store!.WriteBytes(_attempt!,phase+".semantic.json",Encoding.UTF8.GetBytes(semantic));
        var capture=new CaptureData(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,phase,Json.Hash(semantic),_flow.Window);
        AttemptValidation.Window(capture.Window); _store.Write(_attempt!,phase+".capture.json",capture);
        string? compareResult=null;
        if(_b)
        {
            var differences=Projection.Compare(_a!,semantic);
            _store.Write(_attempt!,"compare.json",new { Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,CandidateHash=_candidateHash,Attempt=_attempt,
                AProcess=_arm!.Process,BProcess=Process,CheckpointHash=_seal.Binding.Checkpoint.Hash,AHash=Json.Hash(_a!),BHash=Json.Hash(semantic),
                Result=differences.Length==0?"SEMANTIC_EQUIVALENT":"SEMANTIC_DIFFERENT",Differences=differences });
            compareResult=differences.Length==0?"SEMANTIC_EQUIVALENT":"SEMANTIC_DIFFERENT";
        }
        if(!_flow.Captured()) { AnnounceTerminal(); return; }
        if(!_b)_store.Write(_attempt!,"A.complete.json",capture);
        Marker(_b?"SPIKE001A5 B_CAPTURED_STOP_INPUT "+compareResult:"SPIKE001A5 A_CAPTURED_STOP_INPUT");
    });
    internal static void Arm()
    {
        if(_flow?.Stage!=Stage.ReadyArm)return;
        Contract.Require(Identity(),"arm-environment");
        var state=RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("arm-state");
        Contract.Require(state.Players.Count==1 && state.CurrentActIndex==0 && state.GameMode.ToString()=="Standard" &&
            CombatManager.Instance?.IsInProgress!=true && state.CurrentMapPoint is not null && state.CurrentMapCoord.HasValue &&
            !RunManager.Instance.IsPaused && NMapScreen.Instance is {IsOpen:true,IsTraveling:false},"arm-scene");
        Contract.Require(SaveManager.Instance.CurrentRunSaveTask is null || SaveManager.Instance.CurrentRunSaveTask.IsCompletedSuccessfully,"predecessor-save-still-pending");
        // Do not consume an Arm or read another save slot when the predecessor is unsuitable.
        var before=NativeCheckpoint.StableRead(_native!);
        var predecessor=state.CurrentMapCoord!.Value;
        var targetPoint=state.CurrentMapPoint!.Children.Where(x=>x.PointType==MapPointType.Monster)
            .Select(x=>(Point:x,Coord:FindCoord(state.Map,x))).OrderBy(x=>x.Coord.row).ThenBy(x=>x.Coord.col).FirstOrDefault();
        Contract.Require(targetPoint.Point is not null,"no-direct-monster-target");
        var target=new Target(Contract.Text(state.Rng.ToSerializable().Seed,"arm-seed"),state.Players[0].Character.Id.ToString(),0,
            targetPoint.Coord.row,targetPoint.Coord.col,predecessor.row,predecessor.col,state.TotalFloor);
        Contract.Arm(target,state.TotalFloor,before.Facts);
        _arm=new(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,target,before.Facts);
        _store!.Write(_attempt!,"native-path.private.json",new {Schema=PrivateStore.Schema,Profile="modded/profile1",NativePath=_native});
        _store!.Write(_attempt!,"arm.json",_arm); _store.Activate(_attempt!);
        if(_flow.Arm()) Marker($"SPIKE001A5 READY_A_NODE row={target.Row} col={target.Col} floor={target.ArmFloor}; click only this Monster then stop input");
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
            if(_flow?.Stage!=Stage.AwaitSave || room is not null || !saveProgress)return original;
            var coord=RunManager.Instance.DebugOnlyGetState()?.CurrentMapCoord;
            if(coord?.row!=_arm!.Target.Row || coord?.col!=_arm.Target.Col) { Fail("wrong-map-action"); return original; }
            if(!_flow.SaveStarted())return original;
            Marker("SPIKE001A5 MATCHED_SAVE_TASK_WRAPPED");
            return ProductionObserver.Wrap(original,()=>
            {
                if(!_flow.BeginSeal())return Task.CompletedTask;
                Contract.Require(Identity(),"seal-environment");
                var native=NativeCheckpoint.StableRead(_native!);
                var binding=Contract.Seal(_arm.Target,_arm.Predecessor,native.Facts);
                _store!.WriteBytes(_attempt!,"checkpoint.private.json",native.Bytes,PrivateStore.NativeCap);
                _seal=new(PrivateStore.Schema,PrivateStore.Revision,_candidateHash!,Process,binding);
                _store.Write(_attempt!,"seal.json",_seal);
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
    private static void Fail(string reason)
    {
        _flow?.Fail(reason);
        if(_flow is null)
        {
            try { if(_store is not null && _attempt is not null && !_store.Exists(_attempt,"terminal.json"))
                _store.WriteBytes(_attempt,"terminal.json",Encoding.UTF8.GetBytes(Json.Serialize(new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Process,Reason=reason})),Flow.MaxRecordBytes); } catch{}
            if(Interlocked.Exchange(ref _announcedTerminal,1)==0)Marker("SPIKE001A5 PRECHECK_BLOCKED "+reason);
        }
        else AnnounceTerminal();
    }
    private static void AnnounceTerminal()
    {
        if(_flow?.Stage!=Stage.Terminal || Interlocked.Exchange(ref _announcedTerminal,1)!=0)return;
        try { _store!.WriteBytes(_attempt!,"terminal.json",Encoding.UTF8.GetBytes(Json.Serialize(new {Schema=PrivateStore.Schema,Revision=PrivateStore.Revision,Process,Reason=_flow.TerminalReason,Diagnostics=_flow.Diagnostics})),Flow.MaxRecordBytes); } catch{}
        Marker("SPIKE001A5 TERMINAL_STOP_INPUT "+_flow.TerminalReason);
    }
    private static void Marker(string value) { try { GD.Print(value); } catch { _flow?.Fail("marker-failure"); } }
}

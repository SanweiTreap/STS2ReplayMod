using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

[ModInitializer(nameof(Initialize))]
public static class Runtime
{
    internal const string FixedGame="0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    // The immediate DEMO-002 R1 recorder has the identical format/checkpoint/operation/state schema.
    // This is one explicit recording-build compatibility rule, not general cross-build acceptance.
    internal const string CompatibleRecordingBuild="8A7A03F9190EE81EFFD024013518B9697A7A9A12E44D1658353013F65984D6C7";
    internal const string CompatibleDirectBuild="C825AF5E111AC5C76DE2815EFEA40A9DC5AC4DE18B58C77B2F30CA0BF4992D1B";
    internal const string CompatibleVictoryBuild="46BF021546F6A5DB1B1B8D5A2B01D640E4D5BDC6EBB87C885DDE2DBADA28EC91";
    internal const string CompatibleVictoryFixBuild="5F710B9FB7FE10B962F84D47698CE0C9B0151756A4BE0894FDD6B3834B4C65F7";
    internal const string CompatiblePresentationBuild="FBC3B3DAEE1DF3A546917B24C12CCB7A0A6F6A0406AF480253CB5AB4669AB580";
    internal const string CompatibleRosterBuild="ECC7F2516BCD8337968CB8D2798F3D81FC57F68B4B9DADE3D15A86D8A9D97987";
    internal const string CompatibleContentBuild="E428FC791BB7EA22E8CCAD37DF90A2FA10F0B5DC756033F4B7450E4C8D2B686A";
    internal const string CompatibleContentFixBuild="D02C7335EB39C0338C85CF2556687ABA0A1F4D35841FF944D4752EFEC61B1130";
    internal const string CompatibleStateRecordingBuild="FCE1E395BB83D5518F1B08B57FA5C09AFD46939A7D7C32A802C186918463CF10";
    internal const string CompatibleStateAcceptedBuild="4283890CF7FC01DEE08FAAD8FA46F2263F9CF4438F91BED5A09A3E9BDE76A92D";
    internal const string CompatibleChoiceRecordingBuild="F1D9D349A5804D3A455F78D43B964320102C5117CCA3D9B9E5160D126DB63938";
    internal const string CompatibleChoiceTitleBuild="F65870788BD6D79CBE8822DF73BBF247D7A760A533430753E7807460CA6A42D8";
    internal const string CompatibleChoiceProcessRecordingBuild="0C6FEC6168D75F73C53388FBE0112E9681497159CA7E6556E4BA04BBD28B6DA0";
    internal const string CompatibleChoiceProcessAcceptedBuild="0247258A51E615F1841950F29675C00DB75877940436F59123A2927D51A74369";
    internal const string CompatibleChoiceFinalBuild="282805714F5A74B88DE6E44D6993E15B4B13ED9AFD37E14A5F4610B6A1531DD1";
    internal const string CompatibleSilentRecordingBuild="DC8D7075E181245D79E76435C158084A6A0BB2313490A1BAB5B69A69F90092B9";
    internal const string CompatibleSilentAcceptedBuild="0DE1D7829E62775DC68227623544598720E4A890E8B67F324E83600B966634C5";
    internal const string CompatibleRegentRecordingBuild="EBB189A1D9982A631E7810313224B729B6F5BC1AD860F0BF637D2ECF392C5A88";
    internal const string CompatibleQueuedRecordingBuild="6A4B1730418177CEC01CCDAE0A4D5FDE72541D34DEBD3BC2A83ED521F654EC10";
    internal const string CompatibleFullPlaybackRecordingBuild="378309A7604BDC0CFE462BA18164F0CE7528F4CA1ACEF35304AE5B53B8E3CA7F";
    internal const string CompatibleSharedRecordingBuild="CEBCF009567E850B9C51974E66046413B80C21E0A97B6A460E3BEEF16FB07C2D";
    internal const string CompatibleDefectRecordingBuild="3926F6B5FA74DB355EBD9C19C1A054BA09462A50EA6843E9E1CA810E2487315E";
    internal const string CompatibleNecrobinderRecordingBuild="792B181B68CDB8267F3565EDC09B030352E019C63472623482FED3E5C3200024";
    internal static string GameHash="",ModHash="",Root="",Status="ReplayMod initializing";
    internal static ClipStore Store=null!;
    internal static NGame Game=>NGame.Instance??throw new InvalidDataException("game-owner-missing");
    internal static string[] Mods=>ModManager.GetLoadedMods().Select(m=>(m.manifest?.id??"")+":"+(m.manifest?.version??"")).Order(StringComparer.Ordinal).ToArray();
    internal static string Scope=>Codec.Hash(Encoding.UTF8.GetBytes("ReplayMod:"+UserDataPathProvider.GetProfileScopedBasePath(SaveManager.Instance.CurrentProfileId)));
    internal static Clip ReadClip(string id)=>Store.Read(id,GameHash,ModHash,Scope,CompatibleRecordingBuild,CompatibleDirectBuild,CompatibleVictoryBuild,CompatibleVictoryFixBuild,CompatiblePresentationBuild,CompatibleRosterBuild,CompatibleContentBuild,CompatibleContentFixBuild,CompatibleStateRecordingBuild,CompatibleStateAcceptedBuild,CompatibleChoiceRecordingBuild,CompatibleChoiceTitleBuild,CompatibleChoiceProcessRecordingBuild,CompatibleChoiceProcessAcceptedBuild,CompatibleChoiceFinalBuild,CompatibleSilentRecordingBuild,CompatibleSilentAcceptedBuild,CompatibleRegentRecordingBuild,CompatibleQueuedRecordingBuild,CompatibleFullPlaybackRecordingBuild,CompatibleSharedRecordingBuild,CompatibleDefectRecordingBuild,CompatibleNecrobinderRecordingBuild);
    private static bool _attached;
    private static int _logSequence;
    private static OrderedFileLog? _log;
    private static string _logDirectory="";
    private static double _maximumLogSerializationMilliseconds;
    internal static bool Available;
    internal static readonly string SessionId=Guid.NewGuid().ToString("N");
    internal static readonly int ProcessId=System.Environment.ProcessId;
    internal static readonly DateTimeOffset ProcessStartedUtc=new(System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime());
    public static void Initialize()
    {
        var harmony=new Harmony("STS2ReplayMod.DEMO001.0.1.0");
        try
        {
            GameHash=Codec.Hash(File.ReadAllBytes(typeof(NGame).Assembly.Location));Codec.Require(GameHash==FixedGame,"different-game-build");
            ModHash=Codec.Hash(File.ReadAllBytes(typeof(Runtime).Assembly.Location));Root=ProjectSettings.GlobalizePath("user://ReplayMod/demo-001");
            Store=new(Path.Combine(Root,"clips"));Patches.Install(harmony);
            _logDirectory=Path.Combine(Root,"sessions",SessionId);Directory.CreateDirectory(_logDirectory);_log=new();
            AppDomain.CurrentDomain.ProcessExit+=(_,_)=>_log.Complete().GetAwaiter().GetResult();
            Available=true;
        }
        catch(Exception ex){harmony.UnpatchAll(harmony.Id);Status="ReplayMod unavailable: "+ex.GetType().Name;GD.Print(Status);}
    }
    internal static void CheckEnvironment()=>Codec.Require(Mods.SequenceEqual(new[]{"ReplayMod:0.1.0"})&&SaveManager.Instance.CurrentProfileId==1&&UserDataPathProvider.IsRunningModded,"only-ReplayMod-modded-profile1-supported");
    internal static void Guard(Action action)
    {
        try{action();}catch(Exception ex)
        {
            var reason=ex is InvalidDataException?ex.Message:ex.GetType().Name;
            if(PlayerLoop.Active){ActionMonitor.Error??=reason;PlayerLoop.Error??=reason;}else Recorder.Fail(reason);
            Status="ReplayMod: "+reason;Log("observer-error",new{Reason=reason,Exception=ex.ToString()});
        }
    }
    internal static void Menu(NMainMenu menu)=>Guard(()=>
    {
        if(!_attached){Game.GetTree().ProcessFrame+=Pump;_attached=true;}
        if(Status=="ReplayMod initializing")Recorder.NewRun(false);Ui.AttachMenu(menu);
        Log("menu-ready",new{SessionId,ProcessId,ProcessStartedUtc,GameHash,ModHash,Scope,Mods,Profile=SaveManager.Instance.CurrentProfileId,ManagedRuntime=System.Environment.Version.ToString()});
    });
    private static void Pump()=>Guard(()=>{if(!Available)return;Recorder.Tick();if(Recorder.Active||PlayerLoop.Active)ActionMonitor.Tick();DecisionRoute.Tick();NativeDisplayTrace.Tick();Ui.Refresh();});
    internal static async Task NextFrame()=>await Game.ToSignal(Game.GetTree(),SceneTree.SignalName.ProcessFrame);
    internal static void Log(string kind,object value)
    {
        try
        {
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            var bytes=Codec.Bytes(new{Kind=kind,At=DateTimeOffset.UtcNow,Data=value});
            _maximumLogSerializationMilliseconds=Math.Max(_maximumLogSerializationMilliseconds,System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            _log?.Write(Path.Combine(_logDirectory,$"{++_logSequence:d4}-{kind}.json"),bytes);
        }
        catch{/* Recording/playback reports failure independently; logging must not break original game callbacks. */}
        GD.Print("ReplayMod "+kind);
    }
    internal static async Task FlushLog()
    {
        if(_log is null)return;
        await _log.Drain();
        Log("session-log-drained",new{_log.Written,_log.Failed,MaximumSerializationMilliseconds=_maximumLogSerializationMilliseconds,DiskWritesOnMainThread=false});
        await _log.Drain();
    }
}

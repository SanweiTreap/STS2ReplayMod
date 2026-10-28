using System.Text;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using Perf001Candidate.Core;

namespace Perf001Candidate;

[ModInitializer(nameof(Initialize))]
public static class Probe
{
    private static Harmony? _harmony;private static Configuration? _configuration;private static SceneTree? _tree;
    private static NGame? _owner;private static System.Threading.Timer? _watchdog;private static Stage? _announced;private static int _initialized;
    internal static Session? Flow;internal static LiveView? View;internal static Mode? FrozenMode=>_configuration?.Mode;
    private static string _gameHash="",_selfHash="",_manifestHash="",_root="";
    private static int _mainThread;private static string? _startupFailure;
    public static string Status=>Flow is not null?Flow.Stage.ToString():_startupFailure is not null?"FAILED_STARTUP":"NOT_STARTED";
    public static void Initialize()
    {
        if(Interlocked.Exchange(ref _initialized,1)!=0)return;
        try
        {
            _root=ProjectSettings.GlobalizePath(Identity.PrivateNamespace);
            // Only this candidate's own explicit future configuration. No probing player saves or settings.
            var configPath=Path.Combine(_root,"configuration.json");OwnedStore.NoLinks(configPath);
            _configuration=JsonSerializer.Deserialize<Configuration>(File.ReadAllBytes(configPath),Identity.Json)??throw new InvalidDataException("configuration-null");
            _configuration.Validate();
            var game=typeof(NGame).Assembly;var path=game.Location;
            Identity.Require(new FileInfo(path).Length==9757184,"fixed-game-length");_gameHash=Identity.Hash(File.ReadAllBytes(path));
            _selfHash=Identity.Hash(File.ReadAllBytes(typeof(Probe).Assembly.Location));
            var manifest=Path.ChangeExtension(typeof(Probe).Assembly.Location,".json");_manifestHash=Identity.Hash(File.ReadAllBytes(manifest));
            using var parsed=JsonDocument.Parse(File.ReadAllBytes(manifest));var m=parsed.RootElement;
            Identity.Require(m.GetProperty("id").GetString()=="PERF001"&&m.GetProperty("version").GetString()=="0.1.0"&&m.GetProperty("affects_gameplay").GetBoolean(),"manifest-contract");
            Identity.Require(_gameHash==Identity.GameSHA&&_selfHash==_configuration.CandidateSHA&&_manifestHash==_configuration.ManifestSHA&&
                typeof(Harmony).Assembly.GetName().Version?.ToString()=="2.4.2.0","static-binary-contract");
            _harmony=new(Identity.Owner);PatchCatalog.Install(_harmony,game,_configuration.Mode,typeof(GameCallbacks));
        }
        catch(Exception ex){_startupFailure=ex.GetType().Name;try{_harmony?.UnpatchAll(Identity.Owner);}catch{}GD.Print("PERF001 PRECHECK_BLOCKED "+_startupFailure);}
    }
    internal static PublishedIdentity Published()
    {
        var loaded=ModManager.GetLoadedMods().ToArray();
        var scope=UserDataPathProvider.GetProfileScopedBasePath(SaveManager.Instance.CurrentProfileId).Replace('\\','/');
        return new(_gameHash,_selfHash,_manifestHash,typeof(Harmony).Assembly.GetName().Version!.ToString(),ModManager.IsRunningModded(),
            UserDataPathProvider.IsRunningModded&&UserDataPathProvider.GetProfileDir(1).Replace('\\','/')=="modded/profile1",SaveManager.Instance.CurrentProfileId,
            Identity.Hash(Encoding.UTF8.GetBytes("PERF001-scope:"+scope)),loaded.Select(x=>x.manifest?.id??"").ToArray(),loaded.Select(x=>x.manifest?.version??"").ToArray());
    }
    internal static void CheckPublishedIdentity()=>_configuration!.Check(Published());
    internal static void Guard(Action action)=>ObserverGuard.Run(()=>
    {
        Identity.Require(_mainThread==0||System.Environment.CurrentManagedThreadId==_mainThread,"observer-callback-not-main-thread");action();
    },ex=>
    {
        if(Flow is not null)Flow.Fail("adapter-"+ex.GetType().Name+":"+ex.Message);
        else{_startupFailure=ex.GetType().Name;GD.Print("PERF001 PRECHECK_BLOCKED "+_startupFailure);try{_harmony?.UnpatchAll(Identity.Owner);}catch{}}
    });
    internal static void Menu()=>Guard(()=>
    {
        if(Flow is not null)return;
        _mainThread=System.Environment.CurrentManagedThreadId;CheckPublishedIdentity();View=new();Flow=new(_configuration!,Published(),_root,new MonotonicClock(),View);
        _owner=NGame.Instance??throw new InvalidDataException("game-owner-absent");_tree=_owner.GetTree();
        Identity.Require(_owner.IsInsideTree(),"persistent-game-owner-not-in-tree");
        _tree.ProcessFrame+=Frame;_owner.TreeExiting+=Exit;
        _watchdog=new System.Threading.Timer(_=>
        {
            Flow?.PollDeadlines();
            if(Flow?.Terminal==true)Interlocked.Exchange(ref _watchdog,null)?.Dispose();
        },null,100,100);
    });
    private static void Frame()
    {
        Guard(()=>
        {
            if(Flow is null||View is null)return;
            if(!Flow.Terminal)Flow.Frame(checked((long)Engine.GetProcessFrames()),View.Light());
            if(_announced!=Flow.Stage)
            {
                _announced=Flow.Stage;var marker="PERF001 "+FrozenMode+" "+Flow.Stage+(Flow.Terminal?" STOP_INPUT":"");
                GD.Print(marker);DisplayServer.WindowSetTitle("Slay the Spire 2 | "+marker);
            }
        });
        if(Flow?.Terminal==true)Cleanup();
    }
    private static void Exit(){try{Guard(()=>Flow?.OwnerExit());}finally{Cleanup();}}
    private static void Cleanup()
    {
        _watchdog?.Dispose();_watchdog=null;
        if(_tree is not null){_tree.ProcessFrame-=Frame;_tree=null;}
        if(_owner is not null){_owner.TreeExiting-=Exit;_owner=null;}
        try{_harmony?.UnpatchAll(Identity.Owner);}catch{/* teardown unknown is reported by later offline/authorized runtime audit */}
    }
}

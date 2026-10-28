using System.Text.Json.Nodes;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Spike001A2R2;

var tests = new (string Name, Action Run)[]
{
    ("schema serialization and create-new artifact", SchemaAndCreateNew),
    ("checkpoint identity detects change", CheckpointIdentity),
    ("phase ordering happy path", PhaseOrdering),
    ("phase ordering rejects cross-process A", PhaseOrderingRejects),
    ("exact mod set", ExactModSet),
    ("profile1 only", ProfileOneOnly),
    ("Cloud off", CloudOff),
    ("first terminal wins", FirstTerminalWins),
    ("A checkpoint content change negative", AContentChange),
    ("B pre-checkpoint mismatch negative", BPreMismatch),
    ("formal baseline only accepts native null checkpoint", FormalCheckpointTiming),
    ("B NumReloads plus one allowed", ReloadOnly),
    ("B non-whitelist save diff negative", NonWhitelistSaveDiff),
    ("snapshot diff negative", SnapshotDiff),
    ("Ready has no human-wait timeout", ReadyNoTimeout),
    ("declared Harmony targets exist and NGame Process is not patched", HarmonyTargets),
};
var passed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine("PASS " + test.Name); passed++; }
    catch (Exception ex) { Console.Error.WriteLine("FAIL " + test.Name + ": " + ex.Message); }
}
Console.WriteLine($"RESULT {passed}/{tests.Length}");
return passed == tests.Length ? 0 : 1;

static void SchemaAndCreateNew()
{
    var root = Temp(); try
    {
        var path = Path.Combine(root, "x.json"); JsonSupport.WriteCreateNew(path, new TerminalArtifact(ProbeConstants.Schema,
            ProbeConstants.Version, "t", 1, "X", "Y"));
        var read = JsonSupport.Read<TerminalArtifact>(path); Eq(ProbeConstants.Schema, read.SchemaId);
        Throws(() => JsonSupport.WriteCreateNew(path, read));
    } finally { Directory.Delete(root, true); }
}
static void CheckpointIdentity()
{
    var a = FileId("A", 1, "H", "T"); var b = a with { Sha256 = "X" };
    if (ExperimentRules.SameCheckpointContent(a,b)) throw new InvalidOperationException();
}
static void PhaseOrdering()
{
    Eq(ProbePhase.FixturePreparation, ExperimentRules.ResolvePhase(false,false,false,false,false,false,false,-1,-1,-1,-1,1));
    Eq(ProbePhase.AwaitA, ExperimentRules.ResolvePhase(true,false,false,false,false,false,false,1,-1,-1,-1,1));
    Eq(ProbePhase.AwaitA, ExperimentRules.ResolvePhase(true,true,false,false,false,false,false,1,1,-1,-1,1));
    Eq(ProbePhase.WaitForAExit, ExperimentRules.ResolvePhase(true,true,true,false,false,false,false,1,1,1,-1,1));
    Eq(ProbePhase.ReadyBContinue, ExperimentRules.ResolvePhase(true,true,true,false,false,false,false,1,1,1,-1,2));
    Eq(ProbePhase.AwaitB, ExperimentRules.ResolvePhase(true,true,true,true,true,false,false,1,1,1,2,2));
    Eq(ProbePhase.Terminal, ExperimentRules.ResolvePhase(true,true,true,true,true,true,false,1,1,1,2,2));
}
static void PhaseOrderingRejects()
{
    Eq(ProbePhase.Terminal, ExperimentRules.ResolvePhase(true,false,false,false,false,false,false,1,-1,-1,-1,2));
    Eq(ProbePhase.Terminal, ExperimentRules.ResolvePhase(true,true,true,true,true,false,false,1,1,1,2,3));
}
static void ExactModSet()
{
    if (!ExperimentRules.ExactModSet([ProbeConstants.Id]) || ExperimentRules.ExactModSet([])
        || ExperimentRules.ExactModSet([ProbeConstants.Id,"X"]) || ExperimentRules.ExactModSet(["X"])) throw new InvalidOperationException();
}
static void ProfileOneOnly()
{
    if (!ExperimentRules.IsProfileOne(1,"modded/profile1",true) || ExperimentRules.IsProfileOne(2,"modded/profile2",true)
        || ExperimentRules.IsProfileOne(1,"profile1",false)) throw new InvalidOperationException();
}
static void CloudOff()
{
    if (ExperimentRules.IsCloudOff(true,true) || !ExperimentRules.IsCloudOff(false,true)
        || !ExperimentRules.IsCloudOff(true,false)) throw new InvalidOperationException();
}
static void FirstTerminalWins()
{
    var latch = new TerminalLatch(); var wins = 0;
    Parallel.For(0, 100, _ => { if (latch.TrySeal()) Interlocked.Increment(ref wins); }); Eq(1, wins);
}
static void AContentChange()
{
    var a = Protection("A", "1"); var b = Protection("B", "1");
    if (ExperimentRules.CompareProtection(a,b,true,false).Passed) throw new InvalidOperationException();
}
static void BPreMismatch()
{
    var a = Protection("A", "1"); var b = Protection("A", "2");
    if (ExperimentRules.CompareProtection(a,b,true,false).Passed) throw new InvalidOperationException();
}
static void FormalCheckpointTiming()
{
    if (!ExperimentRules.IsFormalRoomBeforeCheckpoint(true) || ExperimentRules.IsFormalRoomBeforeCheckpoint(false))
        throw new InvalidOperationException();
}
static void ReloadOnly()
{
    var x = ExperimentRules.CompareContinueSave("{\"num_reloads\":2,\"seed\":\"S\"}", "{\"num_reloads\":3,\"seed\":\"S\"}", 2, 3);
    if (!x.Passed || x.AllDiffs.Count != 1) throw new InvalidOperationException(x.Failure);
}
static void NonWhitelistSaveDiff()
{
    var x = ExperimentRules.CompareContinueSave("{\"num_reloads\":2,\"seed\":\"S\"}", "{\"num_reloads\":3,\"seed\":\"X\"}", 2, 3);
    if (x.Passed || x.NonAllowlistedDiffs.Count != 1) throw new InvalidOperationException();
}
static void SnapshotDiff()
{
    var x = JsonSupport.Diff(new { hp=80, hand=new[]{"A"} }, new { hp=79, hand=new[]{"A"} });
    if (x.Count != 1 || x[0].Path != "$.hp") throw new InvalidOperationException();
}
static void ReadyNoTimeout()
{
    // Phase transition is state/artifact driven. There is deliberately no timeout state or deadline input.
    Eq(ProbePhase.AwaitA, ExperimentRules.ResolvePhase(true,false,false,false,false,false,false,7,-1,-1,-1,7));
    Eq(ProbePhase.AwaitA, ExperimentRules.ResolvePhase(true,false,false,false,false,false,false,7,-1,-1,-1,7));
}
static void HarmonyTargets()
{
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    if (typeof(NMainMenu).GetMethod(nameof(NMainMenu._Ready), flags) is null) throw new InvalidOperationException("NMainMenu._Ready missing");
    if (typeof(NGame).GetMethod(nameof(NGame._Input), flags, [typeof(Godot.InputEvent)]) is null) throw new InvalidOperationException("NGame._Input missing");
    if (typeof(NGame).GetMethod("_Process", flags) is not null) throw new InvalidOperationException("regression fixture expected NGame._Process to be absent");
}
static ProtectedState Protection(string hash, string mtime)
{
    const string root = @"C:\P"; var files = new[]{ FileId(root+@"\saves\current_run.save",1,hash,mtime) };
    return new(root, files, new([], ["saves/current_run.save"]));
}
static FileIdentity FileId(string path, long length, string hash, string mtime) => new(path,true,length,hash,mtime);
static string Temp() { var x=Path.Combine(Path.GetTempPath(),"spike001a2r2-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(x); return x; }
static void Throws(Action action) { try { action(); } catch { return; } throw new InvalidOperationException("expected exception"); }
static void Eq<T>(T expected, T actual) where T:notnull { if (!EqualityComparer<T>.Default.Equals(expected,actual)) throw new InvalidOperationException($"{expected} != {actual}"); }

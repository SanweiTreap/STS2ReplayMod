using SpikeInv006B;

namespace SpikeInv006B.Tests;

internal static class Program
{
    private static int Main()
    {
        var cases = new (string, Action)[]
        {
            ("original store returns before args", OriginalStorePoison),
            ("unknown store returns before args", UnknownStorePoison),
            ("candidate store may read args", CandidateReads),
            ("sealed prefix returns before instance work", SealedReturns),
            ("original and unknown manager fail", ManagerIdentity),
            ("single CAS terminal winner", AtomicTerminal),
            ("post-seal commit rejected", PostSealCommit),
            ("unauthorized artifact ticket rejected", ArtifactTicket),
            ("runtime schema whitelist", SchemaWhitelist),
            ("fixed build identity negative", IdentityNegative),
            ("exact logical progress name ordinal", LogicalName),
            ("classification strings are complete", Classifications)
        };
        var failed = 0;
        foreach (var (name, action) in cases) { try { action(); Console.WriteLine("PASS " + name); } catch { failed++; Console.WriteLine("FAIL " + name); } }
        Console.WriteLine($"TOTAL {cases.Length} PASSED {cases.Length-failed} FAILED {failed}");
        return failed == 0 ? 0 : 1;
    }
    private static (IdentityFirstFixture F, object O, object C, object M) New() { var o=new object(); var c=new object(); var m=new object(); return(new(o,c,m),o,c,m); }
    private static void OriginalStorePoison() { var x=New(); var p=new PoisonArgs(); x.F.Store(x.O,p.Read); Eq(0,p.Reads); Eq(1,x.F.Failures); }
    private static void UnknownStorePoison() { var x=New(); var p=new PoisonArgs(); x.F.Store(new object(),p.Read); Eq(0,p.Reads); Eq(1,x.F.Failures); }
    private static void CandidateReads() { var x=New(); var reads=0; x.F.Store(x.M,i=>{reads++; return "x";}); Eq(2,reads); Eq(1,x.F.CandidateWrites); }
    private static void SealedReturns() { var x=New(); var p=new PoisonArgs(); x.F.Sealed=true; x.F.Store(x.M,p.Read); Eq(0,p.Reads); Eq(0,x.F.ArgumentReads); }
    private static void ManagerIdentity() { var x=New(); x.F.Manager(x.O); x.F.Manager(new object()); x.F.Manager(x.C); Eq(2,x.F.Failures); }
    private static void AtomicTerminal() { var s=new ScopeSafeSession(); var wins=0; Parallel.For(0,100,_=>{if(s.TrySeal(TerminalClassification.FailBoundary,FailureSite.Boundary,null,null) is not null) Interlocked.Increment(ref wins);}); Eq(1,wins); }
    private static void PostSealCommit() { var s=new ScopeSafeSession(); s.TrySeal(TerminalClassification.FailBoundary,FailureSite.Boundary,null,null); if(s.TryCommit(EventKind.MenuReady,null,out _)) throw new(); }
    private static void ArtifactTicket() { var s=new ScopeSafeSession(); var ticket=s.TrySeal(TerminalClassification.FailBoundary,FailureSite.Boundary,null,null); var reads=0; if(s.TryWriteClosedArtifact(new object(),_=>{reads++;return true;})) throw new(); Eq(0,reads); if(!s.TryWriteClosedArtifact(ticket,_=>{reads++;return true;})) throw new(); Eq(1,reads); }
    private static void SchemaWhitelist() { var actual=typeof(RuntimeArtifact).GetProperties().Select(x=>x.Name).ToHashSet(); var expected=new[]{"SchemaId","ProbeVersion","GameVersion","GameCommit","GameBranch","GameDllLength","GameDllSha256","ProbeDllLength","ProbeDllSha256","TerminalClassification","FirstFailureSite","TerminalSequence","TerminalElapsedMonotonicTicks","Events","Gate","RunVerify","ProtectionBefore","ProtectionAfter","Candidate","Sentinel","Audit","Teardown","Limitation"}.ToHashSet(); if(!actual.SetEquals(expected)) throw new(); }
    private static void IdentityNegative() { if(ProbeConstants.GameDllLength==1 || ProbeConstants.GameDllSha256==new string('0',64)) throw new(); }
    private static void LogicalName() { if(ProbeConstants.ProgressLogicalName!="modded/profile1/saves/progress.save" || string.Equals(ProbeConstants.ProgressLogicalName,"modded/Profile1/saves/progress.save",StringComparison.Ordinal)) throw new(); }
    private static void Classifications() { foreach(var value in Enum.GetValues<TerminalClassification>()) if(string.IsNullOrEmpty(value.ToString())) throw new(); }
    private static void Eq<T>(T expected,T actual) where T:notnull { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new(); }
}

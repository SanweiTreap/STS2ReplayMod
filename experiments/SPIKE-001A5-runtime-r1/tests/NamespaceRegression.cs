using Spike001A5;
using Spike001A5.Core;

public static class NamespaceRegression
{
    public static int Run(string output,string archived)
    {
        Directory.CreateDirectory(output);
        var pointer=File.ReadAllBytes(Path.Combine(archived,"active.pointer-at-exit.json"));
        var armBytes=File.ReadAllBytes(Path.Combine(archived,"arm.json"));
        var old=Json.Read<Pointer>(System.Text.Encoding.UTF8.GetString(pointer));
        var arm=Json.Read<ArmData>(System.Text.Encoding.UTF8.GetString(armBytes));
        Contract.Require(old.Schema==2 && old.Revision=="phase0-r2" && arm.Revision==old.Revision,"actual-predecessor-schema");
        var legacy=Path.Combine(output,"SPIKE001A5",old.Revision);Directory.CreateDirectory(Path.Combine(legacy,old.Attempt));
        File.WriteAllBytes(Path.Combine(legacy,"active.json"),pointer);File.WriteAllBytes(Path.Combine(legacy,old.Attempt,"arm.json"),armBytes);
        var store=new PrivateStore(output);
        Contract.Require(store.Active() is null && !store.Exists(old.Attempt,"arm.json"),"interrupted-old-A-leaked-as-B");
        Console.WriteLine("PASS actual-interrupted-r2-pointer-does-not-select-fresh-B");
        var fresh=Guid.NewGuid().ToString("N");store.Write(fresh,"arm.json",arm with{Revision=PrivateStore.Revision,Process="SYNTHETIC_FRESH_A",CandidateHash=new string('A',64)});store.Activate(fresh);
        Contract.Require(new PrivateStore(output).Active()==fresh && fresh!=old.Attempt &&
            File.ReadAllBytes(Path.Combine(legacy,"active.json")).AsSpan().SequenceEqual(pointer) &&
            File.ReadAllBytes(Path.Combine(legacy,old.Attempt,"arm.json")).AsSpan().SequenceEqual(armBytes),"old-record-overwritten");
        Console.WriteLine("PASS fresh-A-pointer-independent-with-old-pointer-and-arm-unchanged");
        var badRoot=Path.Combine(output,"downgraded");var badPrivate=Path.Combine(badRoot,"SPIKE001A5",PrivateStore.Revision);Directory.CreateDirectory(badPrivate);File.WriteAllBytes(Path.Combine(badPrivate,"active.json"),pointer);
        var rejected=false;try{_=new PrivateStore(badRoot).Active();}catch(InvalidDataException){rejected=true;}
        Contract.Require(rejected,"old-revision-pointer-accepted");
        Console.WriteLine("PASS pointer-schema2-does-not-bypass-revision-identity");
        File.WriteAllText(Path.Combine(output,"provenance.json"),Json.Serialize(new{Source="ACTUAL_PUBLIC_INTERRUPTED_POINTER_AND_ARM_IN_SYNTHETIC_DIRECTORIES",OldRevision=old.Revision,NewRevision=PrivateStore.Revision,OldAttempt=old.Attempt,FreshSyntheticAttempt=fresh,OldPointerHash=Json.Hash(pointer),OldArmHash=Json.Hash(armBytes),OldFilesUnchanged=true,NativePayloadRead=false}));
        return 3;
    }
    private sealed record Pointer(int Schema,string Revision,string Attempt);
}

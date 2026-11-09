using System.Text.Json.Nodes;
using ReplayMod;

internal static class HistoryReplayContracts
{
    internal static void Run(string output,string root,string index,string nativePath,string unmatchedPath,string runId,string clipBuild)
    {
        var nativeBytes=File.ReadAllBytes(nativePath);
        var native=JsonNode.Parse(nativeBytes)!.AsObject();
        var key=HistoryReplayKey.Read(Path.GetFileName(nativePath),native)??throw new InvalidDataException("native-history-key-unverified");
        var unmatchedBytes=File.ReadAllBytes(unmatchedPath);
        var unmatched=HistoryReplayKey.Read(Path.GetFileName(unmatchedPath),JsonNode.Parse(unmatchedBytes)!.AsObject())!;
        var runPath=Path.Combine(root,"runs",runId+".json");
        var stamp=HistoryFileStamp.Read(runPath);
        var run=new RunStore(Path.Combine(root,"runs")).Read(runId);
        var clipReads=0;
        Clip ReadClip(string id)
        {
            clipReads++;
            return new ClipStore(Path.Combine(root,"clips")).Read(id,run.GameHash,clipBuild,run.Scope);
        }

        var catalog=new HistoryReplayCatalog(root,run.Scope,index);
        var link=catalog.Publish(key,native,run,ReadClip)??throw new InvalidDataException("legacy-history-link-unverified");
        Codec.Require(clipReads==1&&catalog.Find(key,run.GameHash,h=>h==run.ModHash)==link&&clipReads==1,"history-button-lookup-not-metadata-only");
        Codec.Require(catalog.Find(unmatched,run.GameHash,_=>true) is null,"unmatched-history-enabled");
        Codec.Require(!catalog.HasStart(unmatched.StartTime),"unmatched-history-indexed");
        Codec.Require(new HistoryReplayCatalog(root,new string('A',64),index).Find(key,run.GameHash,_=>true) is null,"different-profile-matched");
        Codec.Require(catalog.Find(key,run.GameHash,_=>false) is null,"incompatible-build-matched");
        Codec.Require(HistoryReplayKey.Read(HistoryReplayKey.FileName(key.StartTime+1),native) is null,"nearby-start-matched");
        var other=native.DeepClone().AsObject();other["seed"]="different-record-with-same-filename";
        var otherKey=HistoryReplayKey.Read(Path.GetFileName(nativePath),other)!;
        Codec.Require(otherKey.StartTime==key.StartTime&&catalog.Find(otherKey,run.GameHash,_=>true) is null&&
            catalog.Publish(otherKey,other,run,ReadClip) is null,"same-start-different-record-matched");
        other=native.DeepClone().AsObject();other["schema_version"]=1000;
        Codec.Require(HistoryReplayKey.Read(Path.GetFileName(nativePath),other)==key,"schema-only-changed-native-identity");
        var entry=Path.Combine(index,run.Scope,key.StartTime.ToString(System.Globalization.CultureInfo.InvariantCulture),key.Fingerprint+".json");
        try
        {
            File.WriteAllBytes(entry,Codec.Bytes(link with{RunFile=link.RunFile with{LastWriteTicks=link.RunFile.LastWriteTicks-1}}));
            Codec.Require(catalog.Find(key,run.GameHash,_=>true) is null,"stale-run-index-enabled");
            File.WriteAllBytes(entry,Codec.Bytes(link with{ClipFile=link.ClipFile with{Bytes=link.ClipFile.Bytes-1}}));
            Codec.Require(catalog.Find(key,run.GameHash,_=>true) is null,"stale-clip-index-enabled");
        }
        finally{File.WriteAllBytes(entry,Codec.Bytes(link));}
        Codec.Require(File.ReadAllBytes(nativePath).SequenceEqual(nativeBytes)&&File.ReadAllBytes(unmatchedPath).SequenceEqual(unmatchedBytes)&&
            HistoryFileStamp.Read(runPath)==stamp,"native-history-or-run-file-changed");

        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_ACTUAL_HISTORY_REPLAY_LINK_OFFLINE",NativeHistory=Path.GetFileName(nativePath),
            UnmatchedHistory=Path.GetFileName(unmatchedPath),link.Run,link.Clip,link.Start,run.GameHash,run.ModHash,ClipBuild=clipBuild,
            SelectedOperations=RunRoutes.Playback(run).Operations.Count,IndexRelativePath=Path.GetRelativePath(index,entry),
            ClipReads=clipReads,ExactNativeKey=key,OriginalHistoryUnchanged=true,NativeSceneExecuted=false}));
        Console.WriteLine("PASS_ACTUAL_HISTORY_REPLAY_LINK_OFFLINE");
    }
}

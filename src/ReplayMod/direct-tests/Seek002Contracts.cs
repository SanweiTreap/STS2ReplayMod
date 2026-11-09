using System.Text.Json;

namespace ReplayMod;

internal static class Seek002Contracts
{
    internal static void Run(string output,string root)
    {
        const string id="e7138bcda8374257ab1ffcdbe1955ac3";
        var run=RunRoutes.Playback(new RunStore(Path.Combine(root,"runs")).Read(id));
        var rooms=RunRoomPoints.Read(run,RunStore.ResourceDiscontinuities(run));
        var boss=rooms.Single(point=>point.Visit.Id==63);
        Codec.Require(boss is {Direct:true,StartSource:318,Entry.SourceSequence:317,Attempt:68,
            Visit:{Act:2,Row:14,Col:3},Model:"ENCOUNTER.TEST_SUBJECT_BOSS"}&&rooms.All(point=>point.Visit.Id is not (48 or 49 or 64)),
            "seek-map-did-not-use-final-test-subject-visit");
        var eventRoom=rooms.Single(point=>point.Visit is {Act:0,Row:3,Col:6});
        Codec.Require(eventRoom is {Direct:false,StartSource:9,Entry.SourceSequence:13,Model:"EVENT.ROOM_FULL_OF_CHEESE"},
            "seek-map-event-prefix-anchor");
        var key=RoomEntryCacheFile.KeyFor(run,eventRoom.Entry!,run.Operations[eventRoom.Entry!.Sequence]);
        var store=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"room-cache-fixture-"+Guid.NewGuid().ToString("N"));
        var detached=new RoomEntrySave("native-checkpoint-fixture","account-input-fixture");
        Codec.Require(RoomEntryCacheFile.Write(store,key,detached)&&RoomEntryCacheFile.Read(store,key)==detached&&
            RoomEntryCacheFile.Read(store,key with{Attempt=key.Attempt+1}) is null&&
            RoomEntryCacheFile.Read(store,key with{Expected=key.Expected+"changed"}) is null,"room-cache-keyed-read");
        var cachedPath=Directory.GetFiles(store,"*.json",SearchOption.AllDirectories).Single();
        var damaged=File.ReadAllText(cachedPath).Replace("native-checkpoint-fixture","altered-checkpoint",StringComparison.Ordinal);
        File.WriteAllText(cachedPath,damaged);
        Codec.Require(RoomEntryCacheFile.Read(store,key) is null,"room-cache-checksum");
        Codec.Require(rooms[0] is {Direct:true,StartSource:0,Model:"EVENT.NEOW"},"seek-map-native-neow-start");
        var clip=ReadClip(boss.Anchor!.Clip!);
        var turns=PlayerTurnPoints.Read(clip);
        Codec.Require(turns.Select(point=>point.Host).SequenceEqual([0,3,9,11,13,16,17,21,23]),
            "seek-player-turn-end-boundaries");
        Codec.Require(PlayerTurnPoints.Target(turns,0,true)==new PlayerTurnPoint(3,2)&&
            PlayerTurnPoints.Target(turns,10,false)==new PlayerTurnPoint(9,3)&&
            PlayerTurnPoints.Target(turns,9,false)==new PlayerTurnPoint(3,2)&&
            PlayerTurnPoints.Target(turns,23,true) is null,"seek-player-turn-navigation");
        // The next player counter can advance even when the combat round stays the same.
        var end=clip.Operations[2] with{Sequence=1,After=clip.Initial};
        var follow=clip.Operations[3] with{Sequence=2};
        Codec.Require(PlayerTurnPoints.Read(clip with{Operations=[end,follow]}).SequenceEqual([new(0,1),new(1,2)])&&
            PlayerTurnPoints.Read(clip with{Operations=[end with{Victory=true},follow]}).SequenceEqual([new(0,1)]),
            "seek-extra-player-turn-or-terminal-boundary");
        for(var act=0;act<3;act++)
        {
            var source=run.Operations.Last(op=>RunRoomPoints.BattleAnchor(op)&&op.Battle?.Act==act);
            using var save=JsonDocument.Parse(ReadClip(source.Clip!).Checkpoint);
            var map=save.RootElement.GetProperty("acts")[act].GetProperty("saved_map");
            Codec.Require(map.GetProperty("points").GetArrayLength()>0,"seek-map-saved-topology-missing");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_SEEK002_RECORDED_ROOM_AND_PLAYER_TURN_TARGETS",run.Id,
            MapActs=3,RoomPoints=rooms.Length,Jumpable=rooms.Count(point=>point.StartSource is not null),
            SelectedBoss=new{boss.Visit.Id,boss.Attempt,boss.StartSource},EventAnchor=eventRoom.StartSource,
            Turns=turns,CacheFileRoundTrip=true,WrongAttemptAndChangedProjectionRejected=true,DamagedCacheRejected=true,
            NativeRuntimeExecuted=false}));
        Console.WriteLine("PASS_SEEK002_RECORDED_ROOM_AND_PLAYER_TURN_TARGETS");
        Clip ReadClip(string clipId)=>JsonSerializer.Deserialize<ClipEnvelope>(
            File.ReadAllBytes(Path.Combine(root,"clips",clipId+".json")),Codec.Json)!.Data;
    }
}

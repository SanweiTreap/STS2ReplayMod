using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using Det001.Core;

namespace Det001;

public static class NativeCheckpoint
{
    public static void MatchArm(SerializableRun save,ArmEligibility eligibility)
    {
        Eligibility.Arm(eligibility);Contract.Require(save.Players is {Count:1},"checkpoint-eligibility-player");var p=save.Players[0];
        Contract.Require(p.Deck is {Count:10} && p.Relics is {Count:2} && p.Gold==eligibility.Gold,"checkpoint-eligibility-properties");
        Contract.Require(p.Relics.Select(r=>r.Id?.ToString()).Order(StringComparer.Ordinal).SequenceEqual(eligibility.Relics.Order(StringComparer.Ordinal)),"checkpoint-eligibility-relics");
        var neow=p.Relics.Single(r=>r.Id?.ToString()==eligibility.Neow.Model);
        Contract.Require(Json.Canonical(System.Text.Json.JsonSerializer.Serialize(neow,JsonSerializationUtility.Options))==eligibility.Neow.Persistent,"checkpoint-eligibility-neow-instance");
        Contract.Require(p.Deck.Select(c=>Json.Canonical(System.Text.Json.JsonSerializer.Serialize(c,JsonSerializationUtility.Options))).Order(StringComparer.Ordinal).SequenceEqual(eligibility.Deck.Select(c=>c.Persistent).Order(StringComparer.Ordinal)),"checkpoint-eligibility-deck");
    }
    // Presence is checked before the native parser: production schema initializers may
    // otherwise fabricate missing collections/numeric zero. No player's live payload in Phase0.
    public static (Checkpoint Facts,SerializableRun Save) Parse(byte[] bytes)
    {
        Contract.Require(bytes.Length>0 && bytes.Length<=PrivateStore.NativeCap,"native-size-invalid");
        using var doc=JsonDocument.Parse(bytes,new JsonDocumentOptions { MaxDepth=64 });
        var root=doc.RootElement;
        foreach(var key in new[]{"schema_version","game_mode","players","rng","current_act_index","visited_map_coords","map_point_history","pre_finished_room"})
            Contract.Require(root.TryGetProperty(key,out _),"native-missing-"+key);
        var players=root.GetProperty("players");
        Contract.Require(players.ValueKind==JsonValueKind.Array && players.GetArrayLength()==1,"native-players-invalid");
        Contract.Require(players[0].TryGetProperty("character_id",out _),"native-character-missing");
        RequireRng(root.GetProperty("rng"),Projection.RunKeys,"run");
        Contract.Require(players[0].TryGetProperty("rng",out var playerRng),"native-player-rng-missing");
        RequireRng(playerRng,Projection.PlayerKeys,"player");
        var visited=root.GetProperty("visited_map_coords");
        Contract.Require(visited.ValueKind==JsonValueKind.Array && visited.GetArrayLength()>0,"native-visited-missing");
        foreach(var coord in visited.EnumerateArray())
            Contract.Require(coord.TryGetProperty("row",out _) && coord.TryGetProperty("col",out _),"native-coordinate-missing");
        var history=root.GetProperty("map_point_history");
        Contract.Require(history.ValueKind==JsonValueKind.Array && history.GetArrayLength()>0 && history.EnumerateArray().All(x=>x.ValueKind==JsonValueKind.Array),"native-history-missing");
        var parsed=JsonSerializationUtility.FromJson<SerializableRun>(Encoding.UTF8.GetString(bytes));
        Contract.Require(parsed.Success && parsed.SaveData is not null,"native-parse-failed");
        var save=parsed.SaveData!;
        Contract.Require(save.SchemaVersion>=0 && save.GameMode==GameMode.Standard,"native-schema-or-game-mode");
        Contract.Require(save.Players.Count==1 && save.VisitedMapCoords.Count>0,"native-identity-missing");
        var last=save.VisitedMapCoords[^1];
        Contract.Require(last.row>=0 && last.col>=0 && save.CurrentActIndex>=0 && save.FloorReached>=0,"native-number-invalid");
        return (new(Json.Hash(bytes),Contract.Text(save.SerializableRng?.Seed,"native-seed"),
            Contract.Text(save.Players[0].CharacterId?.ToString(),"native-character"),save.CurrentActIndex,last.row,last.col,
            save.FloorReached,save.PreFinishedRoom is null,save.Players.Count),save);
    }
    private static void RequireRng(JsonElement rng,string[] keys,string scope)
    {
        Contract.Require(rng.ValueKind==JsonValueKind.Object && rng.TryGetProperty("seed",out var seed) && seed.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined),"native-"+scope+"-seed-missing");
        Contract.Require(rng.TryGetProperty("rngs",out var entries) && entries.ValueKind==JsonValueKind.Object,"native-"+scope+"-rngs-missing");
        Contract.Require(entries.EnumerateObject().Select(x=>x.Name).Order(StringComparer.Ordinal).SequenceEqual(keys.Select(JsonNamingPolicy.SnakeCaseLower.ConvertName).Order(StringComparer.Ordinal)),"native-"+scope+"-rng-keys");
        foreach(var entry in entries.EnumerateObject())
        {
            foreach(var key in new[]{"counter","s0","s1","s2","s3"})
                Contract.Require(entry.Value.TryGetProperty(key,out var field) && field.ValueKind==JsonValueKind.Number,"native-rng-state-missing");
            Contract.Require(entry.Value.GetProperty("counter").GetInt32()>=0,"native-rng-counter-negative");
        }
    }
    public static (Checkpoint Facts,byte[] Bytes) StableRead(string exactPath)
    {
        var one=PrivateStore.Read(exactPath,PrivateStore.NativeCap); var two=PrivateStore.Read(exactPath,PrivateStore.NativeCap);
        Contract.Require(one.AsSpan().SequenceEqual(two),"native-not-stable");
        return (Parse(two).Facts,two);
    }
}

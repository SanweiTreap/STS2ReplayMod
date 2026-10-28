using System.Text.Json.Serialization;

namespace Det001.Core;

public sealed record Position([property:JsonRequired] int Act, [property:JsonRequired] int Row, [property:JsonRequired] int Col,
    [property:JsonRequired] string RoomModel, [property:JsonRequired] string RoomType, [property:JsonRequired] string Encounter,
    [property:JsonRequired] int TotalFloor, [property:JsonRequired] int Round);
public sealed record Power([property:JsonRequired] string Model, [property:JsonRequired] int Amount);
public sealed record Variable([property:JsonRequired] string Name, [property:JsonRequired] decimal Base, [property:JsonRequired] decimal Enchanted);
public sealed record CostModifier([property:JsonRequired] int Amount,[property:JsonRequired] string Type,[property:JsonRequired] string Expiration,[property:JsonRequired] bool ReduceOnly);
public sealed record Card([property:JsonRequired] string Model, [property:JsonRequired] int Upgrade, [property:JsonRequired] bool Upgraded,
    [property:JsonRequired] string Persistent, [property:JsonRequired] int CanonicalCost, [property:JsonRequired] int EffectiveCost,
    [property:JsonRequired] bool CostsX, [property:JsonRequired] int? CapturedX, [property:JsonRequired] int StarCost,
    [property:JsonRequired] int Replays, [property:JsonRequired] bool ExhaustNext, [property:JsonRequired] Variable[] Variables,
    [property:JsonRequired] int BaseCost,[property:JsonRequired] CostModifier[] CostModifiers,[property:JsonRequired] string[] Keywords,
    [property:JsonRequired] string? Affliction,[property:JsonRequired] int? AfflictionAmount);
public sealed record Player([property:JsonRequired] string Character, [property:JsonRequired] int Hp, [property:JsonRequired] int MaxHp,
    [property:JsonRequired] int Energy, [property:JsonRequired] int Block, [property:JsonRequired] int Stars,
    [property:JsonRequired] Power[] Powers, [property:JsonRequired] Card[] Hand, [property:JsonRequired] Card[] Draw,
    [property:JsonRequired] Card[] Discard, [property:JsonRequired] Card[] Exhaust);
public sealed record RngEntry([property:JsonRequired] string Key, [property:JsonRequired] int Counter,
    [property:JsonRequired] ulong State0, [property:JsonRequired] ulong State1, [property:JsonRequired] ulong State2, [property:JsonRequired] ulong State3);
public sealed record RngSet([property:JsonRequired] string Seed, [property:JsonRequired] RngEntry[] Entries, [property:JsonRequired] string Digest);
public sealed record Enemy([property:JsonRequired] string Model, [property:JsonRequired] uint CombatId,
    [property:JsonRequired] int Hp, [property:JsonRequired] int MaxHp, [property:JsonRequired] int Block,
    [property:JsonRequired] bool Alive, [property:JsonRequired] bool Dead, [property:JsonRequired] bool Escaped,
    [property:JsonRequired] string NextMove, [property:JsonRequired] Power[] Powers, [property:JsonRequired] RngSet Rng);
public sealed record Semantic([property:JsonRequired] Position Position, [property:JsonRequired] Player Player,
    [property:JsonRequired] Enemy[] Enemies, [property:JsonRequired] RngSet RunRng, [property:JsonRequired] RngSet PlayerRng);

public static class Projection
{
    public static readonly string[] RunKeys = ["CombatCardGeneration","CombatCardSelection","CombatEnergyCosts","CombatOrbs","CombatPotionGeneration","CombatTargets","MonsterAi","Niche","Shuffle","TreasureRoomRelics","UnknownMapPoint","UpFront"];
    public static readonly string[] PlayerKeys = ["Rewards","Shops","Transformations"];
    public static RngSet Rng(string seed, IEnumerable<RngEntry> entries)
    {
        var ordered = entries.OrderBy(e => e.Key, StringComparer.Ordinal).ToArray();
        return new(seed, ordered, Json.Hash(Json.Canonical(Json.Serialize(new { Seed=seed, Entries=ordered }))));
    }
    public static void Validate(Semantic s)
    {
        Contract.Require(s.Position is not null && s.Player is not null && s.Enemies is { Length: > 0 }, "projection-missing");
        var p = s.Position!; var v = s.Player!;
        Contract.Text(p.RoomModel,"room-model"); Contract.Text(p.Encounter,"encounter"); Contract.Text(p.RoomType,"room-type");
        Contract.Require(p.Act >= 0 && p.Row >= 0 && p.Col >= 0 && p.TotalFloor >= 0 && p.Round > 0, "position-invalid");
        Contract.Text(v.Character,"character"); Contract.Require(v.Hp >= 0 && v.MaxHp > 0 && v.Hp <= v.MaxHp && v.Energy >= 0 && v.Block >= 0,"player-invalid");
        Powers(v.Powers);
        foreach (var pile in new[] { v.Hand, v.Draw, v.Discard, v.Exhaust })
        {
            Contract.Require(pile is not null,"pile-missing");
            foreach (var c in pile!)
            {
                Contract.Require(c is not null,"card-missing"); Contract.Text(c!.Model,"card-model"); Contract.Text(c.Persistent,"persistent-card");
                Contract.Require(c.Upgrade >= 0 && c.Variables is not null && c.CostModifiers is not null && c.Keywords is not null &&
                    c.CostsX == c.CapturedX.HasValue && (c.Affliction is null)==(!c.AfflictionAmount.HasValue),"card-invalid");
                Json.Canonical(c.Persistent);
                foreach (var variable in c.Variables!) Contract.Text(variable.Name,"variable-name");
                Contract.Require(c.Variables!.Select(x=>x.Name).Distinct().Count()==c.Variables.Length,"variable-key-duplicate");
            }
        }
        Contract.Require(s.Enemies!.Select(x=>x.CombatId).Distinct().Count()==s.Enemies.Length,"enemy-id-duplicate");
        foreach (var e in s.Enemies)
        { Contract.Text(e.Model,"enemy-model"); Contract.Text(e.NextMove,"enemy-next-move"); Contract.Require(e.Hp >= 0 && e.MaxHp > 0 && e.Block>=0,"enemy-invalid"); Powers(e.Powers); ValidateRng(e.Rng,["Instance"]); }
        ValidateRng(s.RunRng,RunKeys); ValidateRng(s.PlayerRng,PlayerKeys);
    }
    private static void Powers(Power[] values)
    { Contract.Require(values is not null,"powers-missing"); foreach (var p in values!) Contract.Text(p.Model,"power-model"); }
    private static void ValidateRng(RngSet rng, string[] keys)
    {
        Contract.Require(rng is not null && rng.Entries is not null,"rng-missing"); Contract.Text(rng!.Seed,"rng-seed");
        Contract.Require(rng.Entries!.Select(e=>e.Key).Order(StringComparer.Ordinal).SequenceEqual(keys.Order(StringComparer.Ordinal)),"rng-key-set-invalid");
        Contract.Require(rng.Entries.All(e=>e.Counter>=0),"rng-counter-negative");
        Contract.Require(Rng(rng.Seed,rng.Entries).Digest==rng.Digest,"rng-digest-invalid");
    }
    public static string Freeze(Semantic value) { Validate(value); return Json.Canonical(Json.Serialize(value)); }
    public static Semantic Thaw(string value) { var s = Json.Read<Semantic>(value); Validate(s); return s; }
    public static string[] Compare(string a, string b)
    {
        var sa = Freeze(Thaw(a)); var sb = Freeze(Thaw(b));
        using var da = System.Text.Json.JsonDocument.Parse(sa); using var db = System.Text.Json.JsonDocument.Parse(sb);
        var differences = new List<string>(); Diff(da.RootElement,db.RootElement,"$",differences); return differences.ToArray();
    }
    private static void Diff(System.Text.Json.JsonElement a, System.Text.Json.JsonElement b, string path, List<string> diffs)
    {
        if (diffs.Count >= 200) return;
        if (a.ValueKind != b.ValueKind) { diffs.Add(path); return; }
        if (a.ValueKind == System.Text.Json.JsonValueKind.Object)
            foreach (var p in a.EnumerateObject()) { if (!b.TryGetProperty(p.Name,out var q)) diffs.Add(path+"."+p.Name); else Diff(p.Value,q,path+"."+p.Name,diffs); }
        else if (a.ValueKind == System.Text.Json.JsonValueKind.Array)
        { if (a.GetArrayLength()!=b.GetArrayLength()) diffs.Add(path+".length"); else for(var i=0;i<a.GetArrayLength();i++) Diff(a[i],b[i],path+"["+i+"]",diffs); }
        else if (a.GetRawText()!=b.GetRawText()) diffs.Add(path);
    }
}

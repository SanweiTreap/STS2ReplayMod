using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;
using HarmonyLib;

if (args.Length == 2 && args[0] == "--patch-smoke")
{
    var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));
    var harmony = new Harmony("STS2ReplayMod.SPIKEINV003A.OfflinePatchSmoke");
    harmony.PatchAll(assembly);
    Console.WriteLine($"PATCHED {harmony.GetPatchedMethods().Count()} methods");
    harmony.UnpatchAll(harmony.Id);
    Console.WriteLine("UNPATCHED");
    return;
}

if (args.Length == 2 && args[0] == "--probe-targets")
{
    var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));
    foreach (var type in assembly.GetTypes().Where(type => type.Name.EndsWith("Patch", StringComparison.Ordinal)).OrderBy(type => type.FullName))
    {
        var targetMethods = type.GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (targetMethods is null) continue;
        var methods = ((IEnumerable<MethodBase>?)targetMethods.Invoke(null, null))?.ToList() ?? new();
        Console.WriteLine($"{type.FullName}: {methods.Count}");
        foreach (var method in methods) Console.WriteLine($"  {method.DeclaringType?.FullName}.{method.Name}");
    }
    return;
}

if (args.Length == 1 && args[0] == "--api")
{
    foreach (var type in new[] { typeof(SaveManager), typeof(MockGodotFileIo), typeof(RunManager), typeof(RunState), typeof(CombatReplay) })
    {
        Console.WriteLine($"TYPE {type.FullName}");
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            Console.WriteLine($"  CTOR {constructor}");
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).OrderBy(x => x.Name))
            Console.WriteLine($"  METHOD {method}");
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).OrderBy(x => x.Name))
            Console.WriteLine($"  PROPERTY {property.PropertyType} {property.Name} get={property.GetMethod?.IsPublic} set={property.SetMethod?.IsPublic}");
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).OrderBy(x => x.Name))
            Console.WriteLine($"  FIELD {field.FieldType} {field.Name} public={field.IsPublic}");
    }
    return;
}

if (args.Length != 1)
    throw new ArgumentException("Usage: FixtureInspector <absolute .mcr path>");

var path = Path.GetFullPath(args[0]);
if (!string.Equals(Path.GetExtension(path), ".mcr", StringComparison.OrdinalIgnoreCase))
    throw new InvalidDataException("Fixture must have the .mcr extension.");
var bytes = File.ReadAllBytes(path);
ModelIdSerializationCache.Init();
var reader = new PacketReader();
reader.Reset(bytes);
var replay = reader.Read<CombatReplay>();
var run = replay.serializableRun ?? throw new InvalidDataException("CombatReplay.serializableRun is null.");

static int? CollectionCount(object owner, string fieldName)
{
    var field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    var value = field?.GetValue(owner);
    return value switch
    {
        System.Collections.ICollection collection => collection.Count,
        _ => null,
    };
}

var latest = run.VisitedMapCoords.Count > 0 ? run.VisitedMapCoords[^1] : (MegaCrit.Sts2.Core.Map.MapCoord?)null;
var summary = new
{
    path,
    length = bytes.Length,
    sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
    replay.version,
    replay.gitCommit,
    replay.modelIdHash,
    bytesConsumed = (reader.BitPosition + 7) / 8,
    trailingBytes = bytes.Length - ((reader.BitPosition + 7) / 8),
    eventCount = CollectionCount(replay, "events"),
    checksumCount = CollectionCount(replay, "checksums"),
    run = new
    {
        run.SchemaVersion,
        run.CurrentActIndex,
        run.FloorReached,
        latestMapRow = latest?.row,
        latestMapCol = latest?.col,
        seed = run.SerializableRng.Seed,
        run.NumReloads,
        gameMode = run.GameMode.ToString(),
        run.Ascension,
        playerCount = run.Players.Count,
    },
};
Console.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));

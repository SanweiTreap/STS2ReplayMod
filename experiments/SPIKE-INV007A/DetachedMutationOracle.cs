using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv007A;

internal sealed class OracleParseException : Exception;

internal sealed record DetachedOracleSnapshot(SerializableProgress Expected, string BaselineCanonical,
    string BaselineSha256, int SharedMutableReferences);

internal static class DetachedMutationOracle
{
    internal static DetachedOracleSnapshot Capture(SaveManager candidate)
    {
        var baselineJson = JsonSerializationUtility.ToJson(LiveSeedCandidate.CaptureProgress(candidate));
        var baselineCanonical = JsonSupport.Canonicalize(baselineJson);
        SerializableProgress expected;
        try
        {
            var parsed = JsonSerializationUtility.FromJson<SerializableProgress>(baselineJson);
            if (!parsed.Success || parsed.SaveData is null) throw new InvalidDataException();
            expected = parsed.SaveData;
        }
        catch
        {
            throw new OracleParseException();
        }

        var shared = CountSharedMutableReferences(expected, candidate.Progress);
        return new DetachedOracleSnapshot(expected, baselineCanonical,
            JsonSupport.Sha256Text(baselineCanonical), shared);
    }

    internal static void ApplyExpected(SerializableProgress progress, ModelId characterId,
        ModelId encounterId, IReadOnlyList<ModelId> enemyIds, HashSet<string> discovered)
    {
        var encounterStats = progress.EncounterStats.FirstOrDefault(x => x.Id.Equals(encounterId));
        if (encounterStats is null)
        {
            encounterStats = new EncounterStats { Id = encounterId, FightStats = new List<FightStats>() };
            progress.EncounterStats.Add(encounterStats);
        }
        Increment(encounterStats.FightStats, characterId);
        foreach (var enemyId in enemyIds)
        {
            var stats = progress.EnemyStats.FirstOrDefault(x => x.Id.Equals(enemyId));
            if (stats is null)
            {
                stats = new EnemyStats { Id = enemyId, FightStats = new List<FightStats>() };
                progress.EnemyStats.Add(stats);
            }
            var firstFight = stats.FightStats.Count == 0;
            Increment(stats.FightStats, characterId);
            if (firstFight) discovered.Add(enemyId.ToString());
        }
    }

    internal static bool AdmissionPassed(bool parseSucceeded, int sharedMutableReferences,
        bool candidateUnchanged, bool discoveredUnchanged, AuditSnapshot audit)
        => parseSucceeded && sharedMutableReferences == 0 && candidateUnchanged && discoveredUnchanged
           && audit.CandidateUpdateCount == 0 && audit.CandidateSaveCount == 0
           && audit.CandidateWriteCount == 0;

    internal static int CountSharedMutableReferences(object expected, object candidate)
    {
        var left = CollectMutableGraph(expected);
        var right = CollectMutableGraph(candidate);
        var count = 0;
        foreach (var node in left)
        {
            if (!right.Contains(node)) continue;
            count++;
            if (count >= ProbeConstants.OracleSharedReferenceCountCap)
                return ProbeConstants.OracleSharedReferenceCountCap;
        }
        return count;
    }

    private static HashSet<object> CollectMutableGraph(object root)
    {
        var found = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<object>();
        pending.Push(root);
        while (pending.Count != 0)
        {
            var value = pending.Pop();
            var type = value.GetType();
            if (IsLeaf(type) || !visited.Add(value)) continue;
            if (value is Array { Length: 0 }) continue;
            if (type.IsClass && IsMutableSurface(type)) found.Add(value);
            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                    if (item is not null) pending.Push(item);
            }
            foreach (var field in Fields(type))
            {
                object? child;
                try { child = field.GetValue(value); }
                catch { continue; }
                if (child is not null) pending.Push(child);
            }
        }
        return found;
    }

    private static IEnumerable<FieldInfo> Fields(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (!field.IsStatic) yield return field;
    }

    private static bool IsMutableSurface(Type type)
        => typeof(IEnumerable).IsAssignableFrom(type)
           || string.Equals(type.Namespace, "MegaCrit.Sts2.Core.Saves", StringComparison.Ordinal)
           || (type.Namespace?.StartsWith("MegaCrit.Sts2.Core.Saves.", StringComparison.Ordinal) ?? false);

    private static bool IsLeaf(Type type)
        => type.IsPrimitive || type.IsEnum || type.IsPointer || type == typeof(string)
           || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset)
           || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(ModelId)
           || typeof(Delegate).IsAssignableFrom(type) || typeof(Task).IsAssignableFrom(type)
           || type.Namespace?.StartsWith("System.Reflection", StringComparison.Ordinal) == true;

    private static void Increment(List<FightStats> rows, ModelId id)
    {
        var row = rows.FirstOrDefault(x => x.Character.Equals(id));
        if (row is null) rows.Add(new FightStats { Character = id, Wins = 1, Losses = 0 });
        else row.Wins++;
    }
}

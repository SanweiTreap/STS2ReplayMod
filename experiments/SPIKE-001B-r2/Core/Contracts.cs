using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spike001B.Core;

public sealed record Target(string Seed, string Character, int Act, int Row, int Col, int PredecessorRow, int PredecessorCol, int ArmFloor);
public sealed record Checkpoint(string Hash, string Seed, string Character, int Act, int Row, int Col, int Floor, bool RoomNull, int Players);
public sealed record Binding(Target Target, Checkpoint Checkpoint)
{
    public int PlayableFloor => checked(Checkpoint.Floor + 1);
}
public static class Contract
{
    public static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string reason) { if (!condition) throw new InvalidDataException(reason); }
    public static string Text(string? value, string name)
    { Require(!string.IsNullOrWhiteSpace(value), "missing-" + name); return value!; }
    public static Target Arm(Target target, int? liveFloor, Checkpoint predecessor)
    {
        Text(target.Seed, "seed"); Text(target.Character, "character");
        Require(liveFloor.HasValue && liveFloor >= 0 && predecessor.Floor >= 0, "arm-floor-missing-or-negative");
        Require(liveFloor == predecessor.Floor && target.ArmFloor == liveFloor, "arm-floor-mismatch");
        Require(!predecessor.RoomNull && predecessor.Players == 1, "predecessor-not-finished-singleplayer");
        Require(target.Act == 0 && target.Row >= 0 && target.Col >= 0, "target-invalid");
        Require(predecessor.Seed == target.Seed && predecessor.Character == target.Character && predecessor.Act == target.Act &&
            predecessor.Row == target.PredecessorRow && predecessor.Col == target.PredecessorCol, "predecessor-identity-mismatch");
        return target;
    }
    public static Binding Seal(Target target, Checkpoint predecessor, Checkpoint saved)
    {
        Require(saved.RoomNull && saved.Players == 1 && saved.Floor >= 0, "checkpoint-not-room-before-singleplayer");
        Require(saved.Hash.Length == 64 && saved.Hash != predecessor.Hash, "checkpoint-not-new-save");
        Require(saved.Seed == target.Seed && saved.Character == target.Character && saved.Act == target.Act &&
            saved.Row == target.Row && saved.Col == target.Col, "checkpoint-target-mismatch");
        Require(saved.Floor == target.ArmFloor, "checkpoint-floor-mismatch");
        return new(target, saved);
    }
    public static void Continue(Binding binding, Checkpoint input, string processA, string processB,
        string restoredSeed, string restoredCharacter, int restoredAct, int? restoredFloor, int? restoredRow, int? restoredCol)
    {
        Require(!string.IsNullOrWhiteSpace(processA) && !string.IsNullOrWhiteSpace(processB) && processA != processB, "fresh-process-required");
        Require(input == binding.Checkpoint, "source-changed-before-B");
        Require(restoredFloor.HasValue && restoredFloor == binding.Checkpoint.Floor && restoredRow == binding.Target.Row &&
            restoredCol == binding.Target.Col && restoredAct == binding.Target.Act && restoredSeed == binding.Target.Seed &&
            restoredCharacter == binding.Target.Character, "continue-pre-room-mismatch");
    }
    public static void Playable(Binding binding, Semantic state, string? expectedRoom, string? expectedEncounter, int expectedRound=1)
    {
        Projection.Validate(state);
        var p = state.Position;
        Require(p.Act == binding.Target.Act && p.Row == binding.Target.Row && p.Col == binding.Target.Col &&
            p.TotalFloor == binding.PlayableFloor && expectedRound is 1 or 2 && p.Round == expectedRound && p.RoomType == "Monster" &&
            state.Player.Character == binding.Target.Character && state.RunRng.Seed == binding.Target.Seed, "playable-target-mismatch");
        Require(expectedRoom is null || p.RoomModel == expectedRoom, "room-model-mismatch");
        Require(expectedEncounter is null || p.Encounter == expectedEncounter, "encounter-mismatch");
    }
}
public static class Json
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = false, PropertyNameCaseInsensitive = false, IncludeFields=true, MaxDepth = 64 };
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Options) ?? throw new InvalidDataException("null-json");
    public static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    public static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
    public static string Canonical(string value) => Sort(JsonNode.Parse(value) ?? throw new InvalidDataException("null-json")).ToJsonString(Options);
    private static JsonNode Sort(JsonNode node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new KeyValuePair<string, JsonNode?>(p.Key, p.Value is null ? null : Sort(p.Value)))),
        JsonArray array => new JsonArray(array.Select(p => p is null ? null : Sort(p)).ToArray()),
        _ => node.DeepClone()
    };
}
public interface IClock { long Milliseconds { get; } }
public sealed class MonotonicClock : IClock { private readonly Stopwatch _clock = Stopwatch.StartNew(); public long Milliseconds => _clock.ElapsedMilliseconds; }

public static class ProductionObserver
{
    // Only the matched target call uses this wrapper. Other calls keep the original Task.
    // No ConfigureAwait(false): the observer retains production's Godot synchronization context.
    public static Task Wrap(Task original, Func<Task> afterSuccess, Action<string> terminal)
    {
        var result=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _=Observe(); return result.Task;
        async Task Observe()
        {
            try { await original; }
            catch(Exception exception)
            {
                if(original.IsCanceled)
                {
                    BestEffort(terminal,"production-cancel");
                    result.TrySetCanceled(exception is OperationCanceledException canceled?canceled.CancellationToken:default);
                }
                else
                {
                    BestEffort(terminal,"production-fault");
                    // Faulted OperationCanceledException must remain Faulted, not become Canceled.
                    result.TrySetException(original.Exception!.InnerExceptions);
                }
                return;
            }
            try { await afterSuccess(); }
            catch(Exception) { BestEffort(terminal,"observer-failure"); }
            result.TrySetResult();
        }
    }
    private static void BestEffort(Action<string> terminal, string reason) { try { terminal(reason); } catch { } }
}

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Perf001Candidate.Core;

public enum Mode { C0, C1, C2 }
public enum Stage { Startup, AwaitFreshCustom, AwaitMap, AwaitWarmupFocus, Warmup, MapIdle, ReadyMapNode, Loading, Validation,
    ReadyDefend, CardRequest, CardEffect, ReadyEndTurn, EndRequest, EndTurn, Draining, Complete, Failed }
public enum EventKind { MapCommit, CardAccepted, CardResolved, EndTurnAccepted, NextPlayerTurnReady }
public static class Identity
{
    public const string Owner="STS2ReplayMod.PERF001.Measurement.candidate-r1", Revision="candidate-r1", ModId="PERF001";
    public const string GameSHA="0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    public const string PrivateNamespace="user://PERF001/candidate-r1/";
    public static readonly JsonSerializerOptions Json = new() { Converters={new JsonStringEnumConverter()},MaxDepth=64 };
    public static byte[] Bytes<T>(T value)=>JsonSerializer.SerializeToUtf8Bytes(value,Json);
    public static string Hash(ReadOnlySpan<byte> value)=>Convert.ToHexString(SHA256.HashData(value));
    public static void Require(bool condition,string reason) { if(!condition)throw new InvalidDataException(reason); }
}
public interface IClock { long Now {get;} long Frequency {get;} }
public sealed class MonotonicClock:IClock {public long Now=>Stopwatch.GetTimestamp();public long Frequency=>Stopwatch.Frequency;}
public sealed record PublishedIdentity(string GameSHA,string CandidateSHA,string ManifestSHA,string HarmonyVersion,
    bool RunningModded,bool ModdedTree,int Profile,string ProfileScopeBinding,string[] LoadedIds,string[] LoadedVersions);
public sealed record Configuration(int Schema,string Revision,Mode Mode,string RunId,string Seed,bool RuntimeAuthorized,
    bool UserConfirmedCloudOff,bool EstablishBaseline,string CandidateSHA,string ManifestSHA,string ExpectedProfileScopeBinding,SceneFacts[] ExpectedScenes)
{
    public void Validate()
    {
        Identity.Require(Schema==1&&Revision==Identity.Revision&&Enum.IsDefined(Mode),"configuration-schema-mode");
        Identity.Require(System.Text.RegularExpressions.Regex.IsMatch(RunId,"^[a-z0-9-]{1,48}$")&&
            Seed.Length is >=1 and <=64&&Seed.All(char.IsAsciiLetterOrDigit),"run-id-or-fresh-seed");
        Identity.Require(RuntimeAuthorized&&UserConfirmedCloudOff,"runtime-authorization-or-user-cloud-confirmation");
        Identity.Require(CandidateSHA.Length==64&&ManifestSHA.Length==64&&ExpectedProfileScopeBinding.Length==64,"candidate-pair-profile-binding");
        Identity.Require(EstablishBaseline?Mode==Mode.C1&&ExpectedScenes.Length==0:
            ExpectedScenes.Length==4&&ExpectedScenes.Select(x=>x.Point).SequenceEqual(new[]{"map","combat","card","turn"}),"baseline-or-four-expected-scenes");
    }
    public void Check(PublishedIdentity p)
    {
        Identity.Require(p.GameSHA==Identity.GameSHA&&p.CandidateSHA==CandidateSHA&&p.ManifestSHA==ManifestSHA&&p.HarmonyVersion=="2.4.2.0","published-binary-identity");
        Identity.Require(p.RunningModded&&p.ModdedTree&&p.Profile==1&&p.ProfileScopeBinding==ExpectedProfileScopeBinding&&p.LoadedIds.SequenceEqual(new[]{Identity.ModId})&&p.LoadedVersions.SequenceEqual(new[]{"0.1.0"}),"published-loaded-profile-modded-account-gate");
    }
}
// Own immutable strings contain passive value serialization, never a live model or engine reference.
public sealed record SceneFacts(string Point,string GameMode,string Seed,string Character,int Ascension,int Act,
    int Row,int Col,string Room,string Encounter,string NeowChoice,string[] Modifiers,string[] Relics,
    string[] Deck,string[] Hand,string[] Draw,string[] Discard,string[] Exhaust,string Player,string[] Enemies)
{
    public SceneFacts Detached()=>this with {Modifiers=Modifiers.ToArray(),Relics=Relics.ToArray(),Deck=Deck.ToArray(),
        Hand=Hand.ToArray(),Draw=Draw.ToArray(),Discard=Discard.ToArray(),Exhaust=Exhaust.ToArray(),Enemies=Enemies.ToArray()};
    public void Validate(Configuration c)
    {
        Identity.Require(GameMode=="Custom"&&Seed==c.Seed&&Character=="CHARACTER.IRONCLAD"&&Ascension==0&&Act==0&&Modifiers.Length==0,"custom-scene-header");
        Identity.Require(!string.IsNullOrEmpty(NeowChoice)&&Relics.Length>0&&Deck.Length>0&&!string.IsNullOrEmpty(Player),"scene-values-missing");
        if(Point!="map")Identity.Require(Room=="Monster"&&Encounter.Length>0&&Hand.Length>0&&Enemies.Length>0,"combat-scene-values-missing");
        if(!c.EstablishBaseline)
        {
            var expected=c.ExpectedScenes.SingleOrDefault(x=>x.Point==Point);
            Identity.Require(expected is not null&&Identity.Bytes(expected).AsSpan().SequenceEqual(Identity.Bytes(this)),"scene-content-mismatch-"+Point);
        }
    }
}
public sealed record Payload(int Turn,string ActorId,string? CardModelId,int? HandIndex,string[] TargetIds,
    int Hp,int Block,int Energy,string[] HandIds,int DrawCount,int DiscardCount,int ExhaustCount)
{
    public Payload Detached()=>this with {TargetIds=TargetIds.ToArray(),HandIds=HandIds.ToArray()};
}
public sealed record RecordedEvent(int Sequence,EventKind Kind,long Tick,Payload Payload);
public readonly record struct LightState(bool Focused,bool Paused,bool MapVisible,bool Playable,int Turn,bool PlayerSide);
public readonly record struct FrameStamp(long Sequence,long EngineFrame,long Tick,Stage Tag,bool Focused,bool Paused,int Segment=0);
public sealed record Boundary(long Sequence,string Name,long Tick,Stage Stage,string ClockSemantics);
public sealed record RecorderSample(int Sequence,long CaptureFreezeTicks,long EncodingTicks,long EnqueueTicks,long SyncAllocatedBytes);
public sealed record StageSpan(Stage Tag,long Begin,long End,string Reason);
public interface ILiveView
{
    SceneFacts Qualify(string point);
    Payload Capture(EventKind kind);
}

using System.Text.Json;

namespace ReplayMod;

internal sealed record RecordedStateData(int Version,string Clip,int Position,string Game,string Mod,string Scope,
    string ExpectedHash,string RecordingRuntime,int ProcessId,DateTimeOffset ProcessStartedUtc,StateGraphData Graph);
internal sealed record RecordedStateEnvelope(string SHA256,RecordedStateData Data);
internal static class StateFile
{
    internal const int Cap=8*1024*1024;
    internal static string HashProjection(string value)=>Codec.Hash(System.Text.Encoding.UTF8.GetBytes(value));
    internal static byte[] Encode(RecordedStateData data)
    {
        var bytes=Codec.Bytes(new RecordedStateEnvelope(Codec.Hash(Codec.Bytes(data)),data));
        Codec.Require(bytes.Length<=Cap,"authority-file-too-large");return bytes;
    }
    internal static RecordedStateData Decode(byte[] bytes,Clip clip)
    {
        Codec.Require(bytes.Length is >0 and <=Cap,"authority-file-size");
        var envelope=JsonSerializer.Deserialize<RecordedStateEnvelope>(bytes,Codec.Json)??throw new InvalidDataException("authority-empty");
        var s=envelope.Data;Codec.Require(Codec.Hash(Codec.Bytes(s))==envelope.SHA256,"authority-integrity");
        Codec.Require(clip.Character=="CHARACTER.IRONCLAD"&&clip.Authority=="STATE001-first-Defend-v1"&&s.Version==1&&s.Clip==clip.Id&&s.Position==1&&s.Game==clip.GameHash&&s.Mod==clip.ModHash&&s.Scope==clip.Scope&&
            s.ExpectedHash==HashProjection(clip.Operations[0].After)&&clip.Operations[0] is {Kind:OperationKind.Card,Card:"CARD.DEFEND_IRONCLAD",Turn:1,Victory:false,CardState.Upgrade:0},"authority-association");
        return s;
    }
}

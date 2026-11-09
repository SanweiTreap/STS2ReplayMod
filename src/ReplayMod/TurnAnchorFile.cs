using System.IO.Compression;
using System.Text.Json;

namespace ReplayMod;

internal sealed record TurnAnchorKey(string Clip,string Game,string Mod,string Scope,int Host,string Initial,string Expected);
internal sealed record TurnAnchorData(TurnAnchorKey Key,string Projection,StateGraphData Graph);

internal static class TurnAnchorFile
{
    private const int Version=1;
    private sealed record Entry(int Version,TurnAnchorData Data);
    private sealed record Envelope(string SHA256,Entry Payload);

    private static string PathFor(string root,TurnAnchorKey key)=>
        Path.Combine(root,"turn-anchors",Codec.Hash(Codec.Bytes(key))+".v"+Version+".json.gz");

    internal static bool Exists(string root,TurnAnchorKey key)=>File.Exists(PathFor(root,key));

    internal static TurnAnchorData? Read(string root,TurnAnchorKey key)
    {
        var path=PathFor(root,key);
        if(!File.Exists(path))return null;
        using var file=File.OpenRead(path);
        Codec.Require(file.Length is >0 and <=StateFile.Cap,"turn-anchor-compressed-size");
        using var gzip=new GZipStream(file,CompressionMode.Decompress);
        using var raw=new MemoryStream();
        var buffer=new byte[16*1024];
        int count;
        while((count=gzip.Read(buffer))>0)
        {
            Codec.Require(raw.Length+count<=StateFile.Cap,"turn-anchor-expanded-size");
            raw.Write(buffer,0,count);
        }
        var envelope=JsonSerializer.Deserialize<Envelope>(raw.GetBuffer().AsSpan(0,(int)raw.Length),Codec.Json);
        Codec.Require(envelope is {Payload.Version:Version}&&envelope.Payload.Data.Key==key&&
            Codec.Hash(Codec.Bytes(envelope.Payload))==envelope.SHA256,"turn-anchor-association-or-checksum");
        return envelope!.Payload.Data;
    }

    internal static long Write(string root,TurnAnchorData data)
    {
        var path=PathFor(root,data.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var pending=path+"."+Guid.NewGuid().ToString("N")+".pending";
        try
        {
            var entry=new Entry(Version,data);
            var bytes=Codec.Bytes(new Envelope(Codec.Hash(Codec.Bytes(entry)),entry));
            Codec.Require(bytes.Length<=StateFile.Cap,"turn-anchor-expanded-size");
            using(var file=File.Create(pending))
                using(var gzip=new GZipStream(file,CompressionLevel.Fastest))gzip.Write(bytes);
            File.Move(pending,path,overwrite:true);
            return new FileInfo(path).Length;
        }
        finally{File.Delete(pending);}
    }
}

using System.Text.Json;

namespace ReplayMod;

internal sealed record RoomEntryCacheKey(string Run,string Game,string Scope,int Travel,int Visit,int? Attempt,string Expected);
internal sealed record RoomEntrySave(string Checkpoint,string Progress);

internal static class RoomEntryCacheFile
{
    private sealed record Data(int Version,RoomEntryCacheKey Key,RoomEntrySave Save);
    private sealed record Envelope(string SHA256,Data Entry);
    private const int Version=1,Cap=2*1024*1024;

    internal static RoomEntryCacheKey KeyFor(RunDocument run,RunOperation travel,RunOperation next)=>
        new(run.Id,run.GameHash,run.Scope,travel.SourceSequence??travel.Sequence,next.Visit,next.Attempt,
            Codec.Hash(System.Text.Encoding.UTF8.GetBytes(next.Before!)));

    private static string PathFor(string root,RoomEntryCacheKey key)=>
        Path.Combine(root,"room-entry-cache",key.Run,Codec.Hash(Codec.Bytes(key))+".json");

    internal static RoomEntrySave? Read(string root,RoomEntryCacheKey key)
    {
        var path=PathFor(root,key);
        try
        {
            var file=new FileInfo(path);
            if(!file.Exists||file.Length is <=0 or >Cap)return null;
            var data=JsonSerializer.Deserialize<Envelope>(File.ReadAllBytes(path),Codec.Json);
            return data is {Entry.Version:Version}&&data.Entry.Key==key&&data.SHA256==Codec.Hash(Codec.Bytes(data.Entry))
                ?data.Entry.Save:null;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException){return null;}
    }

    internal static bool Write(string root,RoomEntryCacheKey key,RoomEntrySave save)
    {
        var path=PathFor(root,key);
        var pending=path+"."+Guid.NewGuid().ToString("N")+".pending";
        try
        {
            var data=new Data(Version,key,save);
            var bytes=Codec.Bytes(new Envelope(Codec.Hash(Codec.Bytes(data)),data));
            if(bytes.Length>Cap)return false;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(pending,bytes);
            File.Move(pending,path,overwrite:true);
            return true;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return false;}
        finally
        {
            try{File.Delete(pending);}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){}
        }
    }
}

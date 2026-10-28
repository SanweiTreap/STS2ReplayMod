using System.Text.Json;

namespace Perf001Candidate.Core;

public sealed class OwnedStore
{
    public string DirectoryPath {get;}
    private long _bytes;
    public const long Limit=16*1024*1024;
    public OwnedStore(string namespaceRoot,string runId)
    {
        Identity.Require(System.Text.RegularExpressions.Regex.IsMatch(runId,"^[a-z0-9-]{1,48}$"),"owned-run-name");
        DirectoryPath=Path.GetFullPath(Path.Combine(namespaceRoot,runId));NoLinks(DirectoryPath);
        Identity.Require(!Directory.Exists(DirectoryPath)&&!File.Exists(DirectoryPath),"new-run-only");
        Directory.CreateDirectory(DirectoryPath);
    }
    public static void NoLinks(string path)
    {
        for(var p=Path.GetFullPath(path);!string.IsNullOrEmpty(p);p=Path.GetDirectoryName(p))
            if(File.Exists(p)||Directory.Exists(p))Identity.Require((File.GetAttributes(p)&FileAttributes.ReparsePoint)==0,"reparse-output");
    }
    public string PathFor(string leaf)
    {
        Identity.Require(System.Text.RegularExpressions.Regex.IsMatch(leaf,@"^[a-z0-9-]+\.(json|ndjson)$"),"owned-leaf");
        return Path.Combine(DirectoryPath,leaf);
    }
    public void Reserve(int bytes)
    {
        var total=Interlocked.Add(ref _bytes,bytes);
        Identity.Require(bytes>=0&&total<=Limit,"owned-output-budget");
    }
    public void Write<T>(string leaf,T value)
    {
        var bytes=Identity.Bytes(value);Reserve(bytes.Length);
        using var stream=new FileStream(PathFor(leaf),FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        stream.Write(bytes);stream.Flush();
    }
    public void Status(Stage stage,string? failure,int sequence)
        =>Write($"status-{sequence:d3}.json",new{Schema=1,Revision=Identity.Revision,Stage=stage,Failure=failure,Terminal=stage is Stage.Complete or Stage.Failed});
    public void Commit<T>(T value)
    {
        Write("commit-pending.json",value);
        File.Move(PathFor("commit-pending.json"),PathFor("complete.json"),false);
    }
    public static void CheckEvents(string path,IReadOnlyList<RecordedEvent> expected)
    {
        var count=0;
        foreach(var line in File.ReadLines(path))
        {
            Identity.Require(line.Length<=16384&&count<expected.Count,"written-event-bound");
            var actual=JsonSerializer.Deserialize<RecordedEvent>(line,Identity.Json)??throw new InvalidDataException("written-event-null");
            Identity.Require(Identity.Bytes(actual).AsSpan().SequenceEqual(Identity.Bytes(expected[count])),"written-event-content-order");count++;
        }
        Identity.Require(count==expected.Count,"missing-written-events");
        using var digest=System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        long bytes=0;
        foreach(var item in expected)
        {
            var owned=Identity.Bytes(item);digest.AppendData(owned);digest.AppendData(new byte[]{10});bytes+=owned.Length+1;
        }
        using var file=File.OpenRead(path);
        Identity.Require(file.Length==bytes&&Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(file))==Convert.ToHexString(digest.GetHashAndReset()),"written-event-bytes-checksum");
    }
}

using System.Text;
using Spike001B.Core;

namespace Spike001B;

// Append-only activation journal: interrupted attempts are retained, and fresh A
// does not require clearing a pointer or changing the candidate binary.
public sealed class PrivateStore
{
    public const string Revision="phase0-r1";
    public const int Schema=1;
    public const int NativeCap=4*1024*1024;
    public const int SemanticCap=1024*1024;
    private readonly string _root;
    public PrivateStore(string userRoot)
    {
        _root=Path.GetFullPath(Path.Combine(userRoot,"SPIKE001B",Revision));
        NoLinks(_root);
    }
    public static void NoLinks(string path)
    {
        for(var p=Path.GetFullPath(path); !string.IsNullOrEmpty(p); p=Path.GetDirectoryName(p))
            if(File.Exists(p)||Directory.Exists(p))
                Contract.Require((File.GetAttributes(p)&FileAttributes.ReparsePoint)==0,"reparse-path-rejected");
    }
    private string FilePath(string attempt,string name)
    {
        Contract.Require(attempt.Length==32 && attempt.All(char.IsAsciiHexDigit),"attempt-invalid");
        Contract.Require(name==Path.GetFileName(name) && !name.Contains(':') && !name.Contains('/'),"private-name-invalid");
        var path=Path.GetFullPath(Path.Combine(_root,attempt,name)); NoLinks(path); return path;
    }
    public void WriteBytes(string attempt,string name,byte[] bytes,int cap=SemanticCap) => Create(FilePath(attempt,name),bytes,cap);
    public void Write<T>(string attempt,string name,T data) => WriteBytes(attempt,name,Encoding.UTF8.GetBytes(Json.Serialize(data)));
    public byte[] ReadBytes(string attempt,string name,int cap=SemanticCap) => Read(FilePath(attempt,name),cap);
    public T Read<T>(string attempt,string name) => Json.Read<T>(Encoding.UTF8.GetString(ReadBytes(attempt,name)));
    public bool Exists(string attempt,string name) => File.Exists(FilePath(attempt,name));
    public string? Active()
    {
        var journal=Path.Combine(_root,"activations"); NoLinks(journal);
        if(!Directory.Exists(journal))return null;
        var files=Directory.GetFiles(journal,"*.json").Order(StringComparer.Ordinal).ToArray();
        Contract.Require(files.Length<=1000,"activation-limit");
        if(files.Length==0)return null;
        for(var i=0;i<files.Length;i++)Contract.Require(Path.GetFileName(files[i])==(i+1).ToString("D4")+".json","activation-journal-gap");
        var active=Json.Read<ActivePointer>(Encoding.UTF8.GetString(Read(files[^1],1024)));
        Contract.Require(active.Schema==Schema && active.Revision==Revision,"active-schema-invalid");
        _=FilePath(active.Attempt,"arm.json"); return active.Attempt;
    }
    public void Activate(string attempt)
    {
        _=FilePath(attempt,"arm.json"); _=Active();
        var journal=Path.Combine(_root,"activations"); NoLinks(journal);
        var count=Directory.Exists(journal)?Directory.GetFiles(journal,"*.json").Length:0;
        Contract.Require(count<1000,"activation-limit");
        Create(Path.Combine(journal,(count+1).ToString("D4")+".json"),Encoding.UTF8.GetBytes(Json.Serialize(new ActivePointer(Schema,Revision,attempt))),1024);
    }
    public static byte[] Read(string path,int cap)
    {
        NoLinks(path); var info=new FileInfo(path); Contract.Require(info.Exists && info.Length>0 && info.Length<=cap,"file-missing-or-size-invalid");
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        var bytes=new byte[stream.Length]; stream.ReadExactly(bytes); Contract.Require(stream.Position==stream.Length,"file-read-incomplete"); return bytes;
    }
    private static void Create(string path,byte[] bytes,int cap)
    {
        Contract.Require(bytes.Length>0 && bytes.Length<=cap,"output-size-invalid"); NoLinks(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); NoLinks(path);
        using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None); stream.Write(bytes); stream.Flush(true);
    }
    private sealed record ActivePointer(int Schema,string Revision,string Attempt);
}
public sealed record ArmData(int Schema,string Revision,string CandidateHash,string Process,Target Target,Checkpoint Predecessor);
public sealed record SealData(int Schema,string Revision,string CandidateHash,string Process,Binding Binding);
public sealed record CaptureData(int Schema,string Revision,string CandidateHash,string Process,string Phase,string SemanticHash,QuietWindow Window);

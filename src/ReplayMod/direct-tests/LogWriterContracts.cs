using ReplayMod;

internal static class LogWriterContracts
{
    internal static async Task Run(string output)
    {
        var directory=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"log-writer-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var writer=new OrderedFileLog();var path=Path.Combine(directory,"ordered.bin");
        for(var i=0;i<100;i++)writer.Write(path,BitConverter.GetBytes(i));
        await writer.Drain();
        if(writer.Written!=100||writer.Failed!=0||BitConverter.ToInt32(File.ReadAllBytes(path))!=99)throw new Exception("ordered-log-barrier-or-order");
        writer.Write(directory,[0]);writer.Write(path,[42]);await writer.Drain();
        if(writer.Failed!=1||writer.Written!=101||!File.ReadAllBytes(path).SequenceEqual(new byte[]{42}))throw new Exception("log-writer-did-not-continue-after-io-error");
        await writer.Complete();
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ORDERED_LOG_WRITER_PASSED",OrderedWrites=100,BarrierReadback=true,WriteErrorRetained=true,ContinuedAfterWriteError=true,GameObjectsUsed=false}));
        Console.WriteLine("ORDERED_LOG_WRITER_PASSED");
    }
}

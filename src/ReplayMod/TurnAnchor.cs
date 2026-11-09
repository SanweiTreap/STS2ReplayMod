using System.Diagnostics;
using MegaCrit.Sts2.Core.Helpers;

namespace ReplayMod;

// The first file-backed round is deliberately limited to the audited shuffle
// sample. Its only skipped host is EndTurn, so Back can still use opening 0.
internal sealed class TurnAnchor(Clip clip,CombatRoster roster,StateGraph initial)
{
    private const string AuditedClip="fa6a428c9e124344a3ddb8d2feed3a15";
    private readonly TurnAnchorKey _key=new(clip.Id,clip.GameHash,clip.ModHash,clip.Scope,1,
        StateFile.HashProjection(clip.Initial),StateFile.HashProjection(clip.Operations[0].After));
    private bool _captureAttempted;
    private bool _invalid;

    internal static TurnAnchor? Bind(Clip clip,CombatRoster roster)
    {
        if(clip.Id!=AuditedClip||clip.Format!=10||clip.Encounter!="ENCOUNTER.CEREMONIAL_BEAST_BOSS"||
            clip.Operations.Length<2||clip.Operations[0] is not {Kind:OperationKind.EndTurn,Turn:1,Victory:false,Defeat:false}||
            clip.Operations[1].Turn!=2||clip.Choices is {Length:>0})return null;
        try{return new(clip,roster,RecordedState.Bind(roster,turnAnchor:true));}
        catch(Exception ex){Runtime.Log("turn-anchor-unavailable",new{clip.Id,Phase="bind",Reason=ex.Message});return null;}
    }

    internal void Capture(int host,DirectCheckpoint point)
    {
        if(host!=_key.Host||_captureAttempted||!_invalid&&TurnAnchorFile.Exists(Runtime.Root,_key))return;
        _captureAttempted=true;
        try
        {
            var started=Stopwatch.GetTimestamp();
            var frozen=initial.Freeze();
            var data=new TurnAnchorData(_key,point.Projection,frozen);
            Runtime.Log("turn-anchor-frozen",new{clip.Id,Host=host,Nodes=frozen.Nodes.Length,
                Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds,NativePrefixVerified=true});
            TaskHelper.RunSafely(Publish(Runtime.Root,data));
        }
        catch(Exception ex){Runtime.Log("turn-anchor-unavailable",new{clip.Id,Phase="freeze",Reason=ex.Message});}
    }

    private static async Task Publish(string root,TurnAnchorData data)
    {
        try
        {
            var written=await Task.Run(()=>
            {
                var started=Stopwatch.GetTimestamp();
                var bytes=TurnAnchorFile.Write(root,data);
                return (Bytes:bytes,Milliseconds:Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            });
            Runtime.Log("turn-anchor-written",new{data.Key.Clip,data.Key.Host,written.Bytes,written.Milliseconds,Background=true});
        }
        catch(Exception ex){Runtime.Log("turn-anchor-unavailable",new{data.Key.Clip,Phase="write",Reason=ex.Message});}
    }

    internal Func<bool>? Prepare(int from,int host)
    {
        if(from!=0||host!=_key.Host)return null;
        try
        {
            var started=Stopwatch.GetTimestamp();
            if(TurnAnchorFile.Read(Runtime.Root,_key) is not {} data)return null;
            var bindings=RecordedState.LogicalBindings(clip.Id,data.Graph,initial,roster,turnAnchor:true);
            var apply=initial.PrepareApply(data.Graph,bindings,keepBoundReadonly:true);
            Runtime.Log("turn-anchor-prepared",new{clip.Id,Host=host,Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds});
            return ()=>
            {
                try
                {
                    var verify=apply();verify();
                    Codec.Require(LiveState.Playable&&LiveState.Turn==2&&LiveState.Capture()==clip.Operations[0].After&&
                        LiveState.ChoiceProjection()==data.Projection,"turn-anchor-landing-state-or-rng-mismatch");
                    return true;
                }
                catch(Exception ex){_invalid=true;Runtime.Log("turn-anchor-unavailable",new{clip.Id,Phase="apply",Reason=ex.Message});return false;}
            };
        }
        catch(Exception ex){_invalid=true;Runtime.Log("turn-anchor-unavailable",new{clip.Id,Phase="read-or-plan",Reason=ex.Message});return null;}
    }
}

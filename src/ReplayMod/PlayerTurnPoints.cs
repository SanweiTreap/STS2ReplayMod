namespace ReplayMod;

internal sealed record PlayerTurnPoint(int Host,int Turn);

internal static class PlayerTurnPoints
{
    internal static PlayerTurnPoint[] Read(Clip clip)
    {
        var operations=clip.Operations;
        if(operations.Length==0||operations[0].Turn!=1)return [];
        var points=new List<PlayerTurnPoint>{new(0,1)};
        // A completed EndTurn is recorded only after the native player phase
        // and StartTurn have settled. Turn is the player's own counter.
        for(var i=0;i+1<operations.Length;i++)
            if(operations[i] is {Kind:OperationKind.EndTurn,Victory:false,Defeat:false} end&&
                operations[i+1].Turn==end.Turn+1)
                points.Add(new(end.Sequence,end.Turn+1));
        return points.ToArray();
    }

    internal static PlayerTurnPoint? Target(PlayerTurnPoint[] points,int host,bool next)=>
        next?points.FirstOrDefault(point=>point.Host>host):points.LastOrDefault(point=>point.Host<host);
}

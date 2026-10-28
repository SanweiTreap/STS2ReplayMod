namespace ReplayMod;

internal sealed record SeekNode(int Index,int Host,int Choice,int Stage,int Anchor,string Label)
{
    internal bool Stable=>Choice==0;
}
internal static class SeekPlan
{
    internal static ChoiceEvent[] Events(ChoiceFrame frame)=>ChoiceTrace.ResultDemo(frame).Where(e=>e.Kind is not ("Open" or "Close")).ToArray();
    internal static SeekNode[] Build(Clip clip,int hosts)
    {
        Codec.Require(hosts>0&&hosts<=clip.Operations.Length,"seek-slice-host-count");
        var nodes=new List<SeekNode>{new(0,0,0,0,0,"0 · 战斗开始")};var anchor=0;
        foreach(var op in clip.Operations.Take(hosts))
        {
            foreach(var choice in (clip.Choices??[]).Where(c=>c.Boundary==op.Sequence&&!c.Automated))
            {
                var events=Events(choice);Codec.Require(events.Length>0,"seek-empty-choice-process");
                for(var stage=0;stage<events.Length;stage++)
                    nodes.Add(new(nodes.Count,op.Sequence,choice.Sequence,stage,anchor,
                        $"{nodes.Count} · 操作{op.Sequence} 选择{choice.Sequence} · "+(stage==0?"已打开":$"已选{stage}")));
            }
            anchor=nodes.Count;nodes.Add(new(anchor,op.Sequence,0,0,anchor,$"{anchor} · 操作{op.Sequence}后 · "+(op.Kind==OperationKind.EndTurn?$"回合{op.Turn+1}":op.Card)));
        }
        return nodes.ToArray();
    }
}

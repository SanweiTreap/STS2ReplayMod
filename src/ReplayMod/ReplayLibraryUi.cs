using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ReplayMod;

internal static class ReplayLibraryUi
{
    private static CanvasLayer? _layer;
    private static NMainMenu? _menu;
    private static VBoxContainer _box=null!;
    private static readonly List<RunDocument> Runs=[];
    private static readonly List<string> Problems=[];
    private static ClipListing[] _battles=[];
    private static Label Text(string value)=>new(){Text=value,AutowrapMode=TextServer.AutowrapMode.WordSmart};
    private static Button Button(string title,Action action)
    {
        var button=new Button{Text=title,CustomMinimumSize=new(0,44)};
        button.Pressed+=()=>Runtime.Guard(action);_box.AddChild(button);return button;
    }
    private static string Character(string id)=>id switch{"CHARACTER.IRONCLAD"=>"铁甲战士","CHARACTER.SILENT"=>"静默猎手",
        "CHARACTER.DEFECT"=>"故障机器人","CHARACTER.REGENT"=>"储君","CHARACTER.NECROBINDER"=>"亡灵契约师",_=>id};
    internal static void Open(NMainMenu menu)
    {
        if(_layer is not null&&GodotObject.IsInstanceValid(_layer))return;
        _menu=menu;Runs.Clear();Problems.Clear();
        foreach(var id in RunRecording.Store.Ids())
            try{Runs.Add(RunRecording.Store.Read(id));}catch(Exception ex){Problems.Add($"对局 {id[..8]}：{ex.Message}");}
        _battles=RecordingLibrary.List(Path.Combine(Runtime.Root,"clips"),Runtime.ReadClip,Runtime.Journal.Display);
        menu.Hide();
        _layer=new CanvasLayer{Layer=90};
        var root=new ColorRect{Color=new(.055f,.065f,.08f,1)};root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);_layer.AddChild(root);
        var panel=new PanelContainer{AnchorLeft=.12f,AnchorRight=.88f,AnchorTop=.06f,AnchorBottom=.94f};root.AddChild(panel);
        var margin=new MarginContainer();foreach(var side in new[]{"margin_left","margin_right","margin_top","margin_bottom"})margin.AddThemeConstantOverride(side,20);panel.AddChild(margin);
        var scroll=new ScrollContainer{HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};margin.AddChild(scroll);
        _box=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};_box.AddThemeConstantOverride("separation",10);scroll.AddChild(_box);
        Runtime.Game.AddChild(_layer);List();
        Runtime.Log("replay-library-open",new{Runs=Runs.Count,Battles=_battles.Length,BrokenRuns=Problems.Count,ScanOnlyOnOpen=true});
    }
    private static void Page(string title,Action back)
    {
        foreach(var child in _box.GetChildren()){_box.RemoveChild(child);child.QueueFree();}
        var button=Button("返回",back);button.Shortcut=new Shortcut{Events=[new InputEventKey{Keycode=Key.Escape}]};
        button.CallDeferred(Control.MethodName.GrabFocus);_box.AddChild(Text(title));
    }
    private static void List()
    {
        Page("整局录像",Close);
        if(Runs.Count==0)_box.AddChild(Text("还没有整局记录。正常新局会自动记录；已有旧片可从独立战斗打开。"));
        foreach(var raw in Runs.OrderByDescending(r=>r.Created))
        {
            try
            {
                var run=RunRoutes.Playback(raw);
                var visits=run.Visits.Where(v=>run.Operations.Any(o=>o.Visit==v.Id)).ToArray();
                var range=visits.Length==0?"尚未进入房间":$"第{visits[0].Act+1}幕{visits[0].Floor}层—第{visits[^1].Act+1}幕{visits[^1].Floor}层";
                var gaps=run.Operations.Any(o=>o.Status!="complete")||visits.Any(v=>v.Gap is not null);
                Button($"{Character(run.Character)} · {run.Created.ToLocalTime():MM-dd HH:mm} · {range}\n"+
                    $"{(RunOpening.Available(run)?"含涅奥开局":"从已记录起点")} · {run.Operations.Count}项 · {(gaps?"含缺口／未完片段":"已记录路线")} · {run.Id[..8]}",()=>Detail(raw));
            }
            catch(Exception ex){_box.AddChild(Text($"对局 {raw.Id[..8]} 暂不可用：{ex.Message}"));}
        }
        foreach(var problem in Problems)_box.AddChild(Text(problem));
        Button("独立战斗／旧片",()=>Battles(null));
        Button("开发／定向准备与旧测试入口",()=>{Page("开发准备 · 默认不改变正常新局",List);Ui.BuildDevelopment(_box);});
    }
    private static void Detail(RunDocument raw)
    {
        Page($"{Character(raw.Character)} · {raw.Created.ToLocalTime():yyyy-MM-dd HH:mm} · {raw.Id[..8]}",List);
        var run=RunRoutes.Playback(raw);var start=RunPlayback.StartIndex(run);var compatible=RunPlayback.Compatible(run.ModHash);
        _box.AddChild(Text(string.Join(" → ",run.Visits.Where(v=>run.Operations.Any(o=>o.Visit==v.Id)).Select(v=>$"第{v.Act+1}幕{v.Floor}层:{v.RoomType??v.MapType}"))));
        if(raw.Branches is {} branch)_box.AddChild(Text($"最终延续路线 · 保留{branch.Attempts.Count}次房间尝试，{raw.Operations.Count}项原始记录"));
        _box.AddChild(Text("录制状态："+(RunLifecycle.Status(raw) switch{"victory"=>"原生整局胜利","death"=>"原生死亡",
            "abandoned"=>"主动放弃","saved-exit"=>"保存退出，可继续","continued" or "in-progress"=>"进行中",
            "interrupted"=>"记录中断，需核缺口",_=>"旧记录未登记结局，不推断胜负"})+
            (raw.Opening is null?" · 未具备从真实新局开始的可重建输入":" · 含新局输入；完整播放仍以实际覆盖为准")));
        var gaps=RunStore.ResourceDiscontinuities(run);
        if(gaps.Length>0)_box.AddChild(Text("有未记录的资源变化：第"+string.Join("、",gaps)+"项之前"));
        if(!compatible)_box.AddChild(Text("当前版本不支持此记录的来源版本。"));
        if(RunOpening.Available(run))
        {
            var clip=run.Operations[start].Clip!;
            var problem=AnchorProblem(raw,run,run.Operations[start]);
            var button=Button("从开局观看 · 涅奥奖励",()=>Watch(clip,raw,0));button.Disabled=problem is not null||gaps.Length>0;
            if(problem is not null)_box.AddChild(Text(problem));
        }
        else _box.AddChild(Text("没有完整的涅奥前输入与后续首战记录，可从下方已有合法起点观看。"));
        foreach(var anchor in run.Operations.Where(o=>o is {Kind:"battle",Status:"complete",Clip:not null}&&o.Battle?.Entry!="event"))
        {
            var visit=run.Visits.Single(v=>v.Id==anchor.Visit);
            var problem=AnchorProblem(raw,run,anchor);
            var button=Button($"从已记录起点观看 · 第{visit.Act+1}幕{visit.Floor}层／尝试{anchor.Battle?.Attempt??0}",
                ()=>Watch(anchor.Clip!,raw,anchor.SourceSequence??anchor.Sequence));button.Disabled=problem is not null||gaps.Any(s=>s>anchor.Sequence);
            if(problem is not null)_box.AddChild(Text(problem));
        }
        foreach(var visit in run.Visits.Where(v=>v.Gap is not null))_box.AddChild(Text($"第{visit.Floor}层：{visit.Gap}"));
        Button("查看本局分场",()=>Battles(raw.Id));
        Runtime.Log("replay-run-detail",new{raw.Id,OpeningAvailable=RunOpening.Available(run),OldHistoryInvented=false});
    }
    private static void Battles(string? runId)
    {
        Page(runId is null?"独立战斗／旧片":"本局分场 · 内部战斗请从父事件前观看",List);
        foreach(var row in _battles.Where(r=>runId is null||r.Battle?.RunId==runId).OrderByDescending(r=>r.CreatedUtc))
        {
            var button=Button($"{Character(row.Character)} · 第{(row.Battle?.Act??0)+1}幕{row.Battle?.Floor??row.Row+1}层 · {row.Operations}步 · {row.Id[..8]}"+
                (row.Problem is {} p?"\n"+p:""),()=>{Close();TaskHelper.RunSafely(PlayerLoop.Open(row.Id,entry:"battle-list"));});
            button.Disabled=row.Problem is not null;
        }
    }
    private static void Watch(string clip,RunDocument raw,int start)
    {Close();TaskHelper.RunSafely(PlayerLoop.Open(clip,entry:start==0?"run-opening":"run-list",chain:raw,chainStart:start));}
    private static string? AnchorProblem(RunDocument raw,RunDocument route,RunOperation anchor)
    {
        if(!RunPlayback.Compatible(raw.ModHash)||raw.Scope!=Runtime.Scope||raw.GameHash!=Runtime.GameHash)return "来源版本或游戏档案与当前环境不匹配";
        if(RunStore.HasUnrecordedAfter(route,anchor.Sequence))return "此起点后含准备或未记录的缺口；请选更后的合法起点，或查看独立分场。";
        try{return NativePlaybackSupport.Reason(Runtime.ReadClip(anchor.Clip!));}
        catch(Exception ex){return "本场记录不可用："+ex.Message;}
    }
    private static void Close()
    {
        if(_layer is not null&&GodotObject.IsInstanceValid(_layer))_layer.QueueFree();_layer=null;
        if(_menu is not null&&GodotObject.IsInstanceValid(_menu))
        {_menu.Show();_menu.GetNodeOrNull<Button>("ReplayModEntry/ReplayModWatchButton")?.CallDeferred(Control.MethodName.GrabFocus);}_menu=null;
    }
}

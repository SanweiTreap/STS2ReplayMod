using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ReplayMod;

internal static class Ui
{
    private static Label? _menuStatus,_playStatus,_recordStatus,_choiceStatus;
    private static CanvasLayer? _playLayer,_recordLayer;
    private static Control? _inputBlock;
    private static ColorRect? _seekCover;
    private static Button? _play,_step,_back,_stop,_seal,_inject;
    private static OptionButton? _seekPoints;
    private static Button? _seekGo;
    private static Button? _auto;
    private static Button? _fine;
    private static bool Valid(GodotObject? node)=>node is not null&&GodotObject.IsInstanceValid(node);
    private static Label Label(string text)=>new(){Text=text,AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(350,0)};
    private static Button Button(BoxContainer box,string text,Action clicked)
    {
        var button=new Button{Text=text,CustomMinimumSize=new Vector2(0,42)};button.Pressed+=clicked;box.AddChild(button);return button;
    }
    internal static void AttachMenu(NMainMenu menu)
    {
        var panel=new PanelContainer{Name="ReplayModRecentClip",AnchorLeft=.65f,AnchorRight=.96f,AnchorTop=.12f,AnchorBottom=.90f};
        var box=new VBoxContainer();panel.AddChild(box);box.AddChild(Label("Replay · 单房间原型 0.1.0"));
        string? id=null;Clip? clip=null;string info;
        try
        {
            Runtime.CheckEnvironment();id=Runtime.Store.LatestId();
            clip=id is null?null:Runtime.ReadClip(id);
            info=clip is null?"暂无片段。新建铁甲／猎人／储君A0，首场普通战斗会自动录制。首战胜利自动保存。":
                $"最近{(clip.Operations[^1].Victory?"完整胜利战斗":"片段")} {clip.Id[..8]} · {clip.CreatedUtc.ToLocalTime():MM-dd HH:mm}\n{clip.Operations.Length}个操作 · {(clip.Character=="CHARACTER.REGENT"?"储君":clip.Character=="CHARACTER.SILENT"?"猎人":"铁甲")} A0\nSeed {clip.Seed}\n首战 · 出牌／用药／原生选择"+
                    (clip.Preparation is null?"":clip.Preparation.StartsWith("CONTENT004",StringComparison.Ordinal)?"\n定向测试：储君／星星／锻造与复制":clip.Preparation.StartsWith("CONTENT003",StringComparison.Ordinal)?"\n定向测试：猎人／连续弃牌／生成小刀":clip.Preparation.StartsWith("DEMO005",StringComparison.Ordinal)?"\n定向测试：升级牌＋能力＋小花＋能量药水":"\n定向测试样本：暗港双蛞蝓")+
                    (clip.Timing is null?"\n原始操作时间：未知（旧片）":"\n已记录操作接受／完成时间");
        }
        catch(Exception ex){info="片段暂不可打开："+(ex is InvalidDataException?ex.Message:ex.GetType().Name);}
        box.AddChild(Label(info));
        var open=Button(box,"打开最近片段 · 原生播放器",()=>{if(id is not null)TaskHelper.RunSafely(PlayerLoop.Open(id));});
        var unavailable=clip is null?null:NativePlaybackSupport.Reason(clip);open.Disabled=clip is null||unavailable is not null;
        if(unavailable is not null)box.AddChild(Label(unavailable));
        const string hunterReference="1d5346b085b04635ab3e00b43fe9190c";
        try{_ =Runtime.ReadClip(hunterReference);Button(box,"打开已有猎人样本 · 完整27项",()=>TaskHelper.RunSafely(PlayerLoop.Open(hunterReference,entry:"hunter-reference")));}catch{/* Optional reference, not the production input. */}
        const string continuousSample="fc6432b4c0af41d69defda73f1770824";
        if(File.Exists(Path.Combine(Runtime.Root,"clips",continuousSample+".json")))
        {
            try
            {
                _=Runtime.ReadClip(continuousSample);
                Button(box,"打开已有连续选择样本（CONTENT-001）",()=>TaskHelper.RunSafely(PlayerLoop.Open(continuousSample)));
            }
            catch(Exception){/* An unavailable optional sample must not hide the normal recent-clip entry. */}
        }
        var regent=Button(box,"CONTENT-004 储君测试：关闭",()=>{});
        void RegentText()=>regent.Text=TestEncounter.Armed==TestPreparation.RegentContent?"CONTENT-004 储君测试：已启用":"CONTENT-004 储君测试：关闭";
        regent.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.RegentContent?TestPreparation.None:TestPreparation.RegentContent;
            RecordedState.Armed=false;RegentText();Runtime.Status=TestEncounter.Armed==TestPreparation.RegentContent?
                "新建储君A0：星星／首次锻造／双持复制／启蒙降费；跨回合后用锻造药水，录制中插一次明显停顿":"已取消储君测试准备";});RegentText();
        var cold=Button(box,"STATE测试：从落盘第1步打开",()=>{if(id is not null)TaskHelper.RunSafely(PlayerLoop.Open(id,fromFile:true));});cold.Disabled=clip?.Authority is null;
        var queued=Button(box,"PLAYBACK-001 排队录制：关闭",()=>{});
        void QueuedText()=>queued.Text=TestEncounter.Armed==TestPreparation.QueuedCards?"PLAYBACK-001 排队录制：已启用":"PLAYBACK-001 排队录制：关闭";
        queued.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.QueuedCards?TestPreparation.None:TestPreparation.QueuedCards;
            RecordedState.Armed=false;QueuedText();Runtime.Status=TestEncounter.Armed==TestPreparation.QueuedCards?
                "新建铁甲A0，首战快速连续打两张防御，再正常结束回合并获胜":"已取消排队录制准备";});QueuedText();
        var silent=Button(box,"CONTENT-003 猎人测试：关闭",()=>{});
        void SilentText()=>silent.Text=TestEncounter.Armed==TestPreparation.SilentContent?"CONTENT-003 猎人测试：已启用":"CONTENT-003 猎人测试：关闭";
        silent.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.SilentContent?TestPreparation.None:TestPreparation.SilentContent;
            RecordedState.Armed=false;SilentText();Runtime.Status=TestEncounter.Armed==TestPreparation.SilentContent?
                "新建猎人A0：保留起始牌／蛇之戒，加爆发和刀刃之舞，三张机制牌先天保留，首战双蛞蝓。第一回合爆发→生存者；第二回合刀刃之舞":"已取消猎人测试准备";});SilentText();
        var state=Button(box,"STATE测试录制：关闭",()=>{});
        void StateText()=>state.Text=RecordedState.Armed?"STATE测试录制：已启用（先打防御）":"STATE测试录制：关闭";
        state.Pressed+=()=>Runtime.Guard(()=>{RecordedState.Armed=!RecordedState.Armed;TestEncounter.Armed=RecordedState.Armed?TestPreparation.Content:TestPreparation.None;StateText();
            Runtime.Status=RecordedState.Armed?"新建铁甲A0，首战第一个操作必须是未升级防御；胜利后正常退出，再新启动冷恢复":"已取消STATE测试录制";});StateText();
        var prepare=Button(box,"测试准备：升级牌／能力／遗物／药水",()=>{});
        var choices=Button(box,"CONTENT选择测试：关闭",()=>{});
        void ChoicesText()=>choices.Text=TestEncounter.Armed==TestPreparation.Choices?"CONTENT选择测试：已启用（下一新局）":"CONTENT选择测试：关闭";
        choices.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.Choices?TestPreparation.None:TestPreparation.Choices;
            RecordedState.Armed=false;ChoicesText();Runtime.Status=TestEncounter.Armed==TestPreparation.Choices?"新建铁甲A0：工具箱初始选择、九张定向牌、金纸计数4与开牌／能量药水。所有选择由你亲自完成":"已取消选择测试准备";});ChoicesText();
        var process=Button(box,"CONTENT过程测试：关闭",()=>{});
        void ProcessText()=>process.Text=TestEncounter.Armed==TestPreparation.ChoiceProcess?"CONTENT过程测试：已启用（下一新局）":"CONTENT过程测试：关闭";
        process.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.ChoiceProcess?TestPreparation.None:TestPreparation.ChoiceProcess;
            RecordedState.Armed=false;ProcessText();ChoicesText();Runtime.Status=TestEncounter.Armed==TestPreparation.ChoiceProcess?"新建铁甲A0：六张先天保留牌、34张牌组／工具箱／赌徒佳酿／攻击和能量药水。请亲自完成一次撤选再重选":"已取消过程测试准备";});ProcessText();
        void PreparationText()=>prepare.Text=TestEncounter.Armed==TestPreparation.Content?"已启用组合测试（点此取消）":"测试准备：升级牌／能力／遗物／药水";
        prepare.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.Content?TestPreparation.None:TestPreparation.Content;PreparationText();
            Runtime.Status=TestEncounter.Armed==TestPreparation.Content?"请新建铁甲A0：定向双蛞蝓，增加耸肩无视+／燃烧／开心小花／能量药水，会正常保存进度":"已取消测试准备";});
        PreparationText();
        _menuStatus=Label(Runtime.Status);box.AddChild(_menuStatus);menu.AddChild(panel);
    }
    internal static void Playback()
    {
        ClosePlayback();_playLayer=new CanvasLayer{Layer=100};
        var block=new Control{Name="ReplayModWatchInputBlock"};_inputBlock=block;block.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        block.MouseFilter=Control.MouseFilterEnum.Stop;_playLayer.AddChild(block);
        var panel=new PanelContainer{AnchorLeft=.02f,AnchorRight=.98f,AnchorTop=.01f,AnchorBottom=.10f};
        var box=new VBoxContainer();panel.AddChild(box);_playStatus=Label(Runtime.Status);box.AddChild(_playStatus);
        _choiceStatus=Label("");box.AddChild(_choiceStatus);
        var controls=new HBoxContainer();box.AddChild(controls);
        if(NativeSeek.Enabled)
        {
            // Cover reconstruction without changing IsVisibleInTree for native hand layout.
            _seekCover=new ColorRect{Color=new Color(.06f,.07f,.09f,1),MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false};
            _seekCover.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);block.AddChild(_seekCover);
            var navigation=new HBoxContainer();box.AddChild(navigation);
            _seekPoints=new OptionButton{CustomMinimumSize=new Vector2(430,40)};navigation.AddChild(_seekPoints);
            _seekGo=Button(navigation,"Seek · 保持Auto模式",()=>Runtime.Guard(()=>NativeSeek.RequestSeek(_seekPoints.GetSelectedId())));
        }
        if(!NativeSeek.Enabled)_play=Button(controls,"播放 / 暂停",()=>PlayerLoop.Control("toggle"));
        if(NativeSeek.Enabled)_auto=Button(controls,"Auto：关闭",()=>PlayerLoop.Control("auto"));
        if(NativeSeek.Enabled)
        {
            _fine=Button(controls,"> 精细前进",()=>PlayerLoop.Control("fine"));
            _fine.Disabled=true;
            _fine.TooltipText="Auto关闭且等待操作时，推进到下一细阶段；选择中可继续细进";
        }
        _step=Button(controls,"Step · 下一操作",()=>PlayerLoop.Control("step"));
        _step.ButtonDown+=NativeDisplayTrace.MarkStepPointerDown;
        _back=Button(controls,"后退一步",()=>PlayerLoop.Control("back"));
        _back.ButtonDown+=()=>Runtime.Log("playback-button-press",new{Control="back",Phase="down",Enabled=!_back.Disabled});
        _back.ButtonUp+=()=>Runtime.Log("playback-button-press",new{Control="back",Phase="up",Enabled=!_back.Disabled});
        _inject=Button(controls,"STATE扰动：关闭",()=>{if(PlayerLoop.CanInject)PlayerLoop.InjectOnContinue=!PlayerLoop.InjectOnContinue;});
        _stop=Button(controls,"Stop · 返回主菜单",()=>PlayerLoop.Control("stop"));
        if(_play is not null)_play.Disabled=true;_step.Disabled=true;_back.Disabled=true;
        block.AddChild(panel);Runtime.Game.AddChild(_playLayer);
    }
    internal static void ClosePlayback(){if(Valid(_playLayer))_playLayer!.QueueFree();_playLayer=null;_inputBlock=null;_playStatus=null;_choiceStatus=null;_play=null;_step=null;_back=null;_stop=null;_inject=null;_seekPoints=null;_seekGo=null;_seekCover=null;_auto=null;_fine=null;}
    internal static void Refresh()
    {
        if(Valid(_menuStatus))_menuStatus!.Text=Runtime.Status;
        if(Valid(_playStatus))
        {
            _playStatus!.Text=Runtime.Status+(NativeSeek.Enabled?NativeSeek.Pending:DecisionRoute.Enabled?DecisionRoute.PendingDisplay:"");
            _choiceStatus!.Text=ChoiceTape.VisibleDisplay;_choiceStatus.Visible=_choiceStatus.Text.Length>0;
            var cursor=PlayerLoop.Cursor;
            var choosing=ChoiceTape.Controllable;
            _inputBlock!.MouseFilter=(DecisionRoute.Enabled?DecisionRoute.Ready:choosing)?Control.MouseFilterEnum.Ignore:Control.MouseFilterEnum.Stop;
            _inject!.Disabled=!PlayerLoop.CanInject;_inject.Text=PlayerLoop.InjectOnContinue?"STATE扰动：已启用，点播放检验纠偏":"STATE扰动：关闭";
            _inject.Visible=PlayerLoop.CanInject;
            if(NativeSeek.Enabled)
            {
                _inputBlock.MouseFilter=NativeSeek.Ready?Control.MouseFilterEnum.Ignore:Control.MouseFilterEnum.Stop;
                _step!.Disabled=!NativeSeek.CanStep;_step.Text=">> 完整操作";_fine!.Disabled=!NativeSeek.CanFine;
                _auto!.Disabled=PlayerLoop.Loading||NativeSeek.Finished||NativeSeek.Stopping||NativeSeek.Restoring;_auto.Text=NativeSeek.Auto?"Auto：开启":"Auto：关闭";
                _seekCover!.Visible=NativeSeek.Covered;
                _back!.Disabled=!NativeSeek.CanBack;_back.Text="Back · 撤回整项";
                _stop!.Disabled=NativeSeek.Stopping||PlayerLoop.TearingDown;
                _inject.Visible=false;
                if(_seekPoints is not null)
                {
                    var points=NativeSeek.Index;
                    if(points.Length!=_seekPoints.ItemCount)
                    {
                        var selected=_seekPoints.ItemCount>0?_seekPoints.GetSelectedId():0;_seekPoints.Clear();
                        foreach(var point in points)_seekPoints.AddItem(point.Value,point.Key);
                        var item=Array.FindIndex(points,p=>p.Key==selected);if(points.Length>0)_seekPoints.Select(Math.Max(0,item));
                    }
                    _seekPoints.Disabled=NativeSeek.Restoring||NativeSeek.Stopping;
                    _seekGo!.Disabled=points.Length==0||NativeSeek.Restoring||NativeSeek.Stopping;
                }
            }
            else if(DecisionRoute.Enabled)
            {
                RoutePresentation.Access();
                // Disabling a Godot button clears its held press. Never apply legacy disabled states
                // before the route states: doing both every frame eats the Back release/click.
                _step!.Disabled=!DecisionRoute.CanStep;_step.Text="Step · 下一节点";
                _play!.Disabled=!DecisionRoute.CanStep&&!DecisionRoute.Playing&&!DecisionRoute.CanPausePresentation;_play.Text="播放 / 暂停";
                _back!.Disabled=!DecisionRoute.CanBack;_back.Text="Back · 上一节点";
                _stop!.Disabled=PlayerLoop.TearingDown||cursor?.StopRequested!=false;
            }
            else
            {
                var unavailable=PlayerLoop.Loading&&!choosing||PlayerLoop.TearingDown;
                _step!.Disabled=unavailable||cursor is null||cursor.Busy&&!choosing||cursor.Finished||cursor.StopRequested;
                _step.Text=choosing?"Step · 下一选择动作":"Step · 下一操作";
                _play!.Disabled=unavailable||cursor is null||cursor.Rebuilding||cursor.Finished||cursor.StopRequested;
                var backReason=PlayerLoop.BackDisabledReason;
                _back!.Disabled=unavailable||cursor?.CanRewind!=true||backReason is not null;
                _back.Text=backReason is null?"后退一步（暂停时）":"后退不可用："+backReason;
                _stop!.Disabled=PlayerLoop.TearingDown||cursor is null||cursor.StopRequested;
            }
        }
        var showRecord=!PlayerLoop.Active&&Runtime.Game.RootSceneContainer.CurrentScene is NRun&&Recorder.Attempted;
        if(showRecord&&!Valid(_recordLayer))
        {
            _recordLayer=new CanvasLayer{Layer=70};
            var panel=new PanelContainer{AnchorLeft=.25f,AnchorRight=.75f,AnchorTop=.14f,AnchorBottom=.21f};
            var box=new VBoxContainer();panel.AddChild(box);_recordStatus=Label(Runtime.Status);box.AddChild(_recordStatus);
            _seal=Button(box,"结束片段（也可保存退出自动封存）",()=>Runtime.Guard(Recorder.Seal));
            _recordLayer.AddChild(panel);Runtime.Game.AddChild(_recordLayer);
        }
        if(!showRecord&&Valid(_recordLayer)){_recordLayer!.QueueFree();_recordLayer=null;_recordStatus=null;_seal=null;}
        if(Valid(_recordStatus)){_recordStatus!.Text=Runtime.Status;_seal!.Visible=Recorder.Active;_seal.Disabled=!Recorder.CanSeal;}
    }
}

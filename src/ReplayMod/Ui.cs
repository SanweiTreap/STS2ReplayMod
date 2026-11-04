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
        var panel=new PanelContainer{Name="ReplayModEntry",AnchorLeft=.79f,AnchorRight=.96f,AnchorTop=.82f,AnchorBottom=.89f};
        var button=new Button{Name="ReplayModWatchButton",Text="回放"};button.Pressed+=()=>ReplayLibraryUi.Open(menu);panel.AddChild(button);menu.AddChild(panel);
    }
    internal static void BuildDevelopment(BoxContainer box)
    {
        _menuStatus=Label(Runtime.Status);box.AddChild(_menuStatus);
        string? id=null;Clip? clip=null;
        try{id=Runtime.Store.LatestId();if(id is not null)clip=Runtime.ReadClip(id);}catch(Exception){/* No compatible recent authority sample. */}
        var neow=new CheckBox{Text="RUN-003 涅奥新叶选牌准备（仅下一新局）",ButtonPressed=RunOpening.NewLeafArmed};
        neow.Toggled+=value=>{RunOpening.NewLeafArmed=value;if(value){TestEncounter.Armed=TestPreparation.None;RecordedState.Armed=false;}
            Runtime.Status=value?"下一铁甲A0新局：原生涅奥第一项为新叶，按原生页面选牌并确认变形":"已取消涅奥准备";};
        box.AddChild(neow);
        var combatStop=new CheckBox{Text="RUN-003：下次整局首战入场自动Stop（仅一次）",ButtonPressed=RunOpening.CombatStopTestArmed};
        combatStop.Toggled+=value=>RunOpening.CombatStopTestArmed=value;box.AddChild(combatStop);
        var actStop=new CheckBox{Text="RUN-004：下次换幕加载中自动Stop（仅一次）",ButtonPressed=RunActs.StopTestArmed};
        actStop.Toggled+=value=>RunActs.StopTestArmed=value;box.AddChild(actStop);
        var terminalStop=new CheckBox{Text="RUN-005：下次终局演出中自动Stop（仅一次）",ButtonPressed=RunEndings.StopTestArmed};
        terminalStop.Toggled+=value=>RunEndings.StopTestArmed=value;box.AddChild(terminalStop);
        var fullRun=new CheckBox{Text="RUN-005 三幕录制准备（下一铁甲 A0 新局）",ButtonPressed=RunOpening.FullRunArmed};
        fullRun.Toggled+=value=>
        {
            RunOpening.FullRunArmed=value;
            if(value)
            {
                TestEncounter.Armed=TestPreparation.None;RecordedState.Armed=false;neow.ButtonPressed=false;
                RunTailPreparation.Armed=false;RunBossPreparation.Armed=false;
            }
            Runtime.Status=value?"下一铁甲 A0 新局：开局六张升级格挡牌、背包和灯笼；地图、敌人及换幕走原版":"已取消三幕录制准备";
        };
        box.AddChild(fullRun);
        var death=new CheckBox{Text="RUN-005 死亡录制短测（下一铁甲A0新局，首普通1HP）",ButtonPressed=TestEncounter.Armed==TestPreparation.DeathRecording};
        death.Toggled+=value=>{TestEncounter.Armed=value?TestPreparation.DeathRecording:TestPreparation.None;
            if(value){RecordedState.Armed=false;RunOpening.NewLeafArmed=false;}
            Runtime.Status=value?"下一铁甲A0新局：首普通战前1HP，只结束回合到原生死亡，再正常结算返回菜单":"已取消死亡录制准备";};box.AddChild(death);
        var abandon=new CheckBox{Text="RUN-005 主动放弃短测（下一铁甲A0新局）",ButtonPressed=TestEncounter.Armed==TestPreparation.AbandonRecording};
        abandon.Toggled+=value=>{TestEncounter.Armed=value?TestPreparation.AbandonRecording:TestPreparation.None;
            if(value){RecordedState.Armed=false;RunOpening.NewLeafArmed=false;}
            Runtime.Status=value?"下一铁甲A0：首普通打一张防御，动作完成后经暂停菜单放弃并确认；正常结算返回菜单":"已取消主动放弃准备";};box.AddChild(abandon);
        var tail=new CheckBox{Text="RUN-005 后段准备：Continue当前第二/三幕普通后直连Boss（明确前缀缺口）",ButtonPressed=RunTailPreparation.Armed};
        tail.Toggled+=value=>{RunTailPreparation.Armed=value;Runtime.Status=value?
            "仅下次Continue当前第二/三幕普通保存点：地图直连原生Boss，原资源/RNG不改；这不是完整三幕样本":"已取消后段准备";};box.AddChild(tail);
        var bossPreparation=new CheckBox{Text="RUN-005 终局战前加强：Continue第三幕Boss（六牌／满血／明确缺口）",ButtonPressed=RunBossPreparation.Armed};
        bossPreparation.Toggled+=value=>{RunBossPreparation.Armed=value;if(value)tail.ButtonPressed=false;
            Runtime.Status=value?"下次Continue第三幕Boss：壁垒+、两张不动如山+、巩固+、两张全身撞击+；原生保存后入场":"已取消Boss战前加强";};
        box.AddChild(bossPreparation);
        var acts=new CheckBox{Text="RUN-004 精英→Boss短路线（仅下一铁甲A0新局）",ButtonPressed=TestEncounter.Armed==TestPreparation.RunActs};
        acts.Toggled+=value=>{TestEncounter.Armed=value?TestPreparation.RunActs:TestPreparation.None;
            if(value){RecordedState.Armed=false;RunOpening.NewLeafArmed=false;}
            Runtime.Status=value?"下一新局：暗港最左首节点精英→原生Boss。当前候选先测精英，胜利不领奖保存退出":"已取消RUN-004准备";};box.AddChild(acts);
        var loadStop=new CheckBox{Text="RUN-002：下次事件SL切换途中自动点Stop（仅一次）",ButtonPressed=EventReturnPresentation.StopTestArmed};
        loadStop.Toggled+=value=>EventReturnPresentation.StopTestArmed=value;box.AddChild(loadStop);
        var rats=Button(box,"CONTENT-006 三鼠召援：关闭",()=>{});
        void RatsText()=>rats.Text=TestEncounter.Armed==TestPreparation.EnemySummon?"CONTENT-006 三鼠召援：已启用（下一新局）":"CONTENT-006 三鼠召援：关闭";
        rats.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.EnemySummon?TestPreparation.None:TestPreparation.EnemySummon;
            RecordedState.Armed=false;RatsText();Runtime.Status=TestEncounter.Armed==TestPreparation.EnemySummon?
                "下一新铁甲A0：暗港首战三鼠；升级原三防御，增加两张全身撞击+，五牌先天保留。先防御等召援，再击杀新怪":"已取消三鼠准备";});RatsText();
        var route=Button(box,"RUN-001 短路线准备：关闭",()=>{});
        route.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();
            TestEncounter.Armed=TestEncounter.Armed==TestPreparation.RunRoute?TestPreparation.None:TestPreparation.RunRoute;
            RecordedState.Armed=false;route.Text=TestEncounter.Armed==TestPreparation.RunRoute?"RUN-001 短路线准备：已启用（下一新局）":"RUN-001 短路线准备：关闭";
            Runtime.Status="下一新铁甲A0：首场普通→营火→普通；三防御+／两全身撞击+先天保留，40HP；按准备路线正常领奖与选路";});
        var rooms=Button(box,"RUN-002 宝箱SL短路线：关闭",()=>{});
        rooms.Pressed+=()=>
        {
            TestEncounter.Armed=TestEncounter.Armed==TestPreparation.RunRooms?TestPreparation.None:TestPreparation.RunRooms;
            RecordedState.Armed=false;rooms.Text=TestEncounter.Armed==TestPreparation.RunRooms?"RUN-002 宝箱SL短路线：已启用（下一新局）":"RUN-002 宝箱SL短路线：关闭";
            Runtime.Status="下一新铁甲A0：首普通→宝箱→普通；三防御+／两全身撞击+先天保留，40HP；先正常开箱领取，再保存退出验证SL入口";
        };
        var shops=Button(box,"RUN-002 商店SL短路线：关闭",()=>{});
        shops.Pressed+=()=>Runtime.Guard(()=>
        {
            Runtime.CheckEnvironment();
            TestEncounter.Armed=TestEncounter.Armed==TestPreparation.RunShops?TestPreparation.None:TestPreparation.RunShops;
            RecordedState.Armed=false;shops.Text=TestEncounter.Armed==TestPreparation.RunShops?"RUN-002 商店SL短路线：已启用（下一新局）":"RUN-002 商店SL短路线：关闭";
            Runtime.Status="下一新铁甲A0：首普通→商店→普通；初始1000金币，40HP，三防御+／两全身撞击+先天保留；只准备新局，不修改Continue";
        });
        var events=Button(box,"RUN-002 事件SL短路线：关闭",()=>{});
        events.Pressed+=()=>Runtime.Guard(()=>
        {
            Runtime.CheckEnvironment();
            TestEncounter.Armed=TestEncounter.Armed==TestPreparation.RunEvents?TestPreparation.None:TestPreparation.RunEvents;
            RecordedState.Armed=false;events.Text=TestEncounter.Armed==TestPreparation.RunEvents?"RUN-002 事件SL短路线：已启用（下一新局）":"RUN-002 事件SL短路线：关闭";
            Runtime.Status="下一新铁甲A0：普通→问号浴场→问号光暗之门→普通；40HP，五牌先天保留；checkpoint前准备事件顺序/当前概率，观看仍原生掷房间";
        });
        var eventCombat=Button(box,"RUN-002 事件内部战斗短路线：关闭",()=>{});
        eventCombat.Pressed+=()=>Runtime.Guard(()=>
        {
            Runtime.CheckEnvironment();
            TestEncounter.Armed=TestEncounter.Armed==TestPreparation.RunEventCombat?TestPreparation.None:TestPreparation.RunEventCombat;
            RecordedState.Armed=false;eventCombat.Text=TestEncounter.Armed==TestPreparation.RunEventCombat?"RUN-002 事件内部战斗短路线：已启用（下一新局）":"RUN-002 事件内部战斗短路线：关闭";
            Runtime.Status="下一新铁甲A0：普通→问号训练假人→普通；防御+、两岿然不动+、两全身撞击+先天保留，能量药水留给第二档假人";
        });
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
        const string regentReference="cd3baa3b1be7466f9cb94d385c3db420";
        try{_ =Runtime.ReadClip(regentReference);Button(box,"打开已有储君样本 · 星星／锻造",()=>TaskHelper.RunSafely(PlayerLoop.Open(regentReference,entry:"regent-reference")));}catch{/* Optional existing sample. */}
        const string openingReference="76f55fed77ab448b9ff318b9d50eff98";
        try{_=Runtime.ReadClip(openingReference);Button(box,"打开已有工具箱样本 · 开场控制",()=>TaskHelper.RunSafely(PlayerLoop.Open(openingReference,entry:"opening-reference")));}catch{/* Optional existing sample. */}
        var regent=Button(box,"CONTENT-005 共用测试：关闭",()=>{});
        void RegentText()=>regent.Text=TestEncounter.Armed==TestPreparation.SharedContent?"CONTENT-005 共用测试：已启用":"CONTENT-005 共用测试：关闭";
        regent.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.SharedContent?TestPreparation.None:TestPreparation.SharedContent;
            RecordedState.Armed=false;RegentText();Runtime.Status=TestEncounter.Armed==TestPreparation.SharedContent?
                "新建储君A0：工具箱开场选择；先丢弃第3槽能量药水，再星星／锻造／复制／降费，跨回合用锻造药水":"已取消共用测试准备";});RegentText();
        var defect=Button(box,"CONTENT-005 故障机器人测试：关闭",()=>{});
        void DefectText()=>defect.Text=TestEncounter.Armed==TestPreparation.DefectContent?"CONTENT-005 故障机器人测试：已启用":"CONTENT-005 故障机器人测试：关闭";
        defect.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.DefectContent?TestPreparation.None:TestPreparation.DefectContent;
            RecordedState.Armed=false;RegentText();DefectText();Runtime.Status=TestEncounter.Armed==TestPreparation.DefectContent?
                "新建故障机器人A0：第一回合暗球→冰球→电击+，集中药水后结束回合；第二回合扩容→混沌+→双重释放":"已取消故障机器人准备";});DefectText();
        var necro=Button(box,"CONTENT-005 亡灵契约师测试：关闭",()=>{});
        void NecroText()=>necro.Text=TestEncounter.Armed==TestPreparation.NecrobinderContent?"CONTENT-005 亡灵契约师测试：已启用":"CONTENT-005 亡灵契约师测试：关闭";
        necro.Pressed+=()=>Runtime.Guard(()=>{Runtime.CheckEnvironment();TestEncounter.Armed=TestEncounter.Armed==TestPreparation.NecrobinderContent?TestPreparation.None:TestPreparation.NecrobinderContent;
            RecordedState.Armed=false;RegentText();DefectText();NecroText();Runtime.Status=TestEncounter.Armed==TestPreparation.NecrobinderContent?
                "新建亡灵契约师A0：能量药水→保镖→释放→碎骨→第二张保镖；第二回合死亡使者+后结束回合，以灾厄获胜":"已取消亡灵契约师准备";});NecroText();
        var firstOsty=new CheckBox{Text="Osty 首次召唤：测试时移除起始遗物",ButtonPressed=false};box.AddChild(firstOsty);
        TestEncounter.FirstOstySummonArmed=false;firstOsty.Toggled+=value=>TestEncounter.FirstOstySummonArmed=value;
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

    }
    internal static void Playback()
    {
        var native=NativeSeek.Enabled||RunPlayback.Active;
        ClosePlayback();_playLayer=new CanvasLayer{Layer=100};
        var block=new Control{Name="ReplayModWatchInputBlock"};_inputBlock=block;block.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        block.MouseFilter=Control.MouseFilterEnum.Stop;_playLayer.AddChild(block);
        var panel=new PanelContainer{AnchorLeft=.02f,AnchorRight=.98f,AnchorTop=.01f,AnchorBottom=.10f};
        var box=new VBoxContainer();panel.AddChild(box);_playStatus=Label(Runtime.Status);box.AddChild(_playStatus);
        _choiceStatus=Label("");box.AddChild(_choiceStatus);
        var controls=new HBoxContainer();box.AddChild(controls);
        if(native)
        {
            // Cover reconstruction without changing IsVisibleInTree for native hand layout.
            _seekCover=new ColorRect{Color=new Color(.06f,.07f,.09f,1),MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false};
            _seekCover.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);block.AddChild(_seekCover);
            var navigation=new HBoxContainer();box.AddChild(navigation);
            _seekPoints=new OptionButton{CustomMinimumSize=new Vector2(430,40)};navigation.AddChild(_seekPoints);
            _seekGo=Button(navigation,"Seek · 保持Auto模式",()=>Runtime.Guard(()=>NativeSeek.RequestSeek(_seekPoints.GetSelectedId()-1)));
        }
        if(!native)_play=Button(controls,"播放 / 暂停",()=>PlayerLoop.Control("toggle"));
        if(native)_auto=Button(controls,"Auto：关闭",()=>PlayerLoop.Control("auto"));
        if(native)
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
            _playStatus!.Text=Runtime.Status+RunPlayback.Display+(EndingPlayback.Active?EndingPlayback.Display:NativeSeek.Enabled?NativeSeek.Pending:DecisionRoute.Enabled?DecisionRoute.PendingDisplay:"");
            _choiceStatus!.Text=ChoiceTape.VisibleDisplay;_choiceStatus.Visible=_choiceStatus.Text.Length>0;
            var cursor=PlayerLoop.Cursor;
            var choosing=ChoiceTape.Controllable;
            _inputBlock!.MouseFilter=(DecisionRoute.Enabled?DecisionRoute.Ready:choosing)?Control.MouseFilterEnum.Ignore:Control.MouseFilterEnum.Stop;
            _inject!.Disabled=!PlayerLoop.CanInject;_inject.Text=PlayerLoop.InjectOnContinue?"STATE扰动：已启用，点播放检验纠偏":"STATE扰动：关闭";
            _inject.Visible=PlayerLoop.CanInject;
            if(EndingPlayback.Active)
            {
                _inputBlock.MouseFilter=Control.MouseFilterEnum.Stop;
                _step!.Disabled=!EndingPlayback.CanStep;_fine!.Disabled=!EndingPlayback.CanStep;
                _step.Text=RunEndings.SummaryComplete?">> 已到终点":">> 继续到结算";
                _auto!.Disabled=!EndingPlayback.CanAuto;_auto.Text=EndingPlayback.Auto?"Auto：开启":"Auto：关闭";
                _back!.Disabled=!EndingPlayback.CanBack;_back.Text="Back · 结局信息";
                _stop!.Disabled=PlayerLoop.TearingDown||RunPlayback.Stopping||NativeSeek.Stopping;
                _seekCover!.Visible=false;_inject.Visible=false;
                if(_seekPoints is not null)_seekPoints.Disabled=true;if(_seekGo is not null)_seekGo.Disabled=true;return;
            }
            if(RunPlayback.NonCombat)
            {
                _inputBlock.MouseFilter=RunPlayback.Finished&&RunEndings.GameOverActive?Control.MouseFilterEnum.Ignore:Control.MouseFilterEnum.Stop;
                _step!.Disabled=RunPlayback.Stopping||RunPlayback.Finished;_fine!.Disabled=RunPlayback.Stopping||RunPlayback.Finished;
                _step.Text=RunPlayback.Finished?">> 已到终点":">> 完整操作";_auto!.Disabled=RunPlayback.Stopping||RunPlayback.Finished;_auto.Text=RunPlayback.Auto?"Auto：开启":"Auto：关闭";
                _back!.Disabled=true;_back.Text="Back · 战外暂未开放";_stop!.Disabled=RunPlayback.Stopping;_seekCover!.Visible=false;_inject.Visible=false;
                if(_seekPoints is not null)_seekPoints.Disabled=true;if(_seekGo is not null)_seekGo.Disabled=true;return;
            }
            if(NativeSeek.Enabled)
            {
                _inputBlock.MouseFilter=NativeSeek.Ready?Control.MouseFilterEnum.Ignore:Control.MouseFilterEnum.Stop;
                // Compute the final state once. Disabling then enabling a Godot
                // button each frame clears its held press before release.
                var continueRoute=RunPlayback.Active&&NativeSeek.Finished&&RunPlayback.HasFollowing&&!NativeSeek.Stopping&&!NativeSeek.Restoring;
                _step!.Disabled=!(continueRoute||NativeSeek.CanStep);_step.Text=continueRoute?">> 继续路线":NativeSeek.Opening?">> 完成开场":">> 完整操作";
                _fine!.Disabled=!(continueRoute||NativeSeek.CanFine);
                _auto!.Disabled=!continueRoute&&(PlayerLoop.Loading&&!NativeSeek.Opening||NativeSeek.Finished||NativeSeek.Stopping||NativeSeek.Restoring);_auto.Text=NativeSeek.Auto?"Auto：开启":"Auto：关闭";
                _seekCover!.Visible=NativeSeek.Covered;
                _back!.Disabled=!NativeSeek.CanBack;_back.Text="Back · 撤回整项";
                _stop!.Disabled=NativeSeek.Stopping||PlayerLoop.TearingDown;
                _inject.Visible=false;
                if(_seekPoints is not null)
                {
                    var points=NativeSeek.Index.Where(p=>!RunPlayback.Active||p.Key>=0).ToArray();
                    if(points.Length!=_seekPoints.ItemCount)
                    {
                        var selected=_seekPoints.ItemCount>0?_seekPoints.GetSelectedId():0;_seekPoints.Clear();
                        foreach(var point in points)_seekPoints.AddItem(point.Value,point.Key+1);
                        var item=Array.FindIndex(points,p=>p.Key+1==selected);if(points.Length>0)_seekPoints.Select(Math.Max(0,item));
                    }
                    _seekPoints.Disabled=NativeSeek.Restoring||NativeSeek.Stopping||NativeSeek.DeathTransition;
                    _seekGo!.Disabled=points.Length==0||NativeSeek.Restoring||NativeSeek.Stopping||NativeSeek.DeathTransition;
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

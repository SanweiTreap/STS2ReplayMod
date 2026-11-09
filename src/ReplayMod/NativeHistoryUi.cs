using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.addons.mega_text;

namespace ReplayMod;

internal static class NativeHistoryUi
{
    private const string WatchTitle="观看回放";
    private const string WatchTitleKey="REPLAYMOD.WATCH_HISTORY.title";
    private static readonly Color IdleBackground=new("000000C0");
    private static readonly Color HoverBackground=new("7B1B15");
    private static readonly Color IdleLabel=new(1,1,1,.82f);
    private static readonly Color DisabledLabel=new(1,1,1,.5f);
    private sealed record Selection(string Scope,HistoryReplayKey Key,HistoryReplayLink Link);
    private sealed class View(NShareButton button,Control background,Control label,Control unavailable)
    {
        private Tween? _hoverTween;
        internal Selection? Selected;
        internal HistoryReplayKey? Displayed;
        internal void Update(Selection? selection)
        {
            Selected=selection;
            if(selection is null)
            {
                button.Disable();unavailable.Show();
                _hoverTween?.Kill();NHoverTipSet.Remove(button);
                button.Scale=Vector2.One;background.Modulate=IdleBackground;label.Modulate=DisabledLabel;
            }
            else
            {
                unavailable.Hide();label.Modulate=IdleLabel;button.Enable();
            }
        }

        internal void Hover(bool focused)
        {
            if(!button.IsInsideTree()||Selected is not {} selection)return;
            _hoverTween?.Kill();NHoverTipSet.Remove(button);
            var tween=_hoverTween=button.CreateTween().SetParallel();
            if(focused)
            {
                tween.TweenProperty(button,"scale",Vector2.One*1.05f,.05);
                tween.TweenProperty(background,"modulate",HoverBackground,.05);
                tween.TweenProperty(label,"modulate",Colors.White,.05);
                var table=LocManager.Instance.GetTable("main_menu_ui");
                if(!table.IsLocalKey(WatchTitleKey))table.MergeWith(new(){[WatchTitleKey]=WatchTitle});
                var description=selection.Link.Start==0?"从涅奥开场观看这局录像。":"从这局录像保留的最早可播战斗开始。";
                var tip=NHoverTipSet.CreateAndShow(button,new HoverTip(table.GetLocString(WatchTitleKey),description));
                tip?.SetGlobalPosition(button.GlobalPosition-tip.Size+new Vector2(-10,button.Size.Y));
            }
            else
            {
                tween.TweenProperty(button,"scale",Vector2.One,.5).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(background,"modulate",IdleBackground,.1);
                tween.TweenProperty(label,"modulate",IdleLabel,.1);
            }
        }
    }

    private static readonly ConditionalWeakTable<NRunHistory,View> Views=new();
    private static readonly System.Reflection.FieldInfo Names=AccessTools.Field(typeof(NRunHistory),"_runNames");
    private static readonly System.Reflection.FieldInfo History=AccessTools.Field(typeof(NRunHistory),"_history");
    private static readonly System.Reflection.MethodInfo PauseRelease=AccessTools.DeclaredMethod(typeof(NTopBarPauseButton),"OnRelease",[]);
    private static Selection? _returning;
    private static bool _opening;

    internal static void Install(Harmony h)
    {
        void Patch(Type type,string method,Type[] parameters,string? before=null,string? after=null)
        {
            var target=AccessTools.DeclaredMethod(type,method,parameters)??throw new MissingMethodException(type.FullName,method);
            HarmonyMethod? Hook(string? name)=>name is null?null:new(typeof(NativeHistoryUi),name);
            h.Patch(target,Hook(before),Hook(after));
        }
        Patch(typeof(NRunHistory),"RefreshAndSelectRun",[typeof(int)],nameof(Selecting),nameof(Selected));
        Patch(typeof(RunHistorySaveManager),nameof(RunHistorySaveManager.SaveHistory),[typeof(RunHistory)],after:nameof(HistorySaved));
        Patch(typeof(NPauseMenu),nameof(NPauseMenu.Initialize),[typeof(IRunState)],after:nameof(PauseInitialized));
        Patch(typeof(NPauseMenu),"OnGiveUpButtonPressed",[typeof(NButton)],before:nameof(AllowGiveUp));
        Patch(typeof(NPauseMenuButton),"OnPress",[],after:nameof(PauseExitPressed));
    }

    private static void Selecting(NRunHistory __instance,ref int __0,out RunHistory? __state)
    {
        __state=History.GetValue(__instance) as RunHistory;
        if(Runtime.Game.MainMenu is not {} menu||!menu.IsAncestorOf(__instance))return;
        try
        {
            var view=Views.GetValue(__instance,Mount);
            view.Displayed=null;view.Update(null);
            if(_returning is {} origin&&origin.Scope==Runtime.Scope&&Names.GetValue(__instance) is List<string> names)
            {
                var index=names.IndexOf(HistoryReplayKey.FileName(origin.Key.StartTime));
                if(index>=0)__0=index;
            }
        }
        catch(Exception ex){Failure("history-replay-button-unavailable",ex);}
    }

    private static void Selected(NRunHistory __instance,int __0,RunHistory? __state)
    {
        if(!Views.TryGetValue(__instance,out var view)||History.GetValue(__instance) is not RunHistory history||
            ReferenceEquals(history,__state)||Names.GetValue(__instance) is not List<string> names||__0<0||__0>=names.Count)return;
        try
        {
            if(history.Players.Count!=1||history.StartTime<=0||names[__0]!=HistoryReplayKey.FileName(history.StartTime))return;
            var scope=Runtime.Scope;
            var catalog=new HistoryReplayCatalog(Runtime.Root,scope);
            // Histories with no indexed start do not need to be serialized.
            HistoryReplayLink? link=null;
            if((_returning is not null||catalog.HasStart(history.StartTime))&&HistoryReplayKey.Read(names[__0],NativeJson(history)) is {} key)
            {
                view.Displayed=key;
                link=catalog.Find(key,Runtime.GameHash,RunPlayback.Compatible);
                if(link is not null)view.Update(new(scope,key,link));
            }
            Runtime.Log("history-replay-selection",new{History=names[__0],Enabled=link is not null,Run=link?.Run,PayloadRead=false});
        }
        catch(Exception ex){Failure("history-replay-selection-failed",ex);}
    }

    private static View Mount(NRunHistory screen)
    {
        var share=screen.GetNode<NShareButton>("%ShareButton");
        var button=GD.Load<PackedScene>("res://scenes/ui/share_button.tscn").Instantiate<NShareButton>();
        button.Name="ReplayModWatchHistory";
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        var width=Math.Max(share.Size.X,share.CustomMinimumSize.X);
        button.OffsetRight=share.OffsetRight-width-32;button.OffsetLeft=button.OffsetRight-width;
        button.OffsetTop=share.OffsetTop;button.OffsetBottom=share.OffsetBottom;
        button.GrowHorizontal=Control.GrowDirection.Begin;
        screen.AddChild(button);
        var background=button.GetNode<Control>("ButtonImage");
        var label=button.GetNode<Control>("LabelContainer");
        button.GetNode<MegaLabel>("%Label").SetTextAutoSize(WatchTitle);
        var icon=button.GetNode<TextureRect>("LabelContainer/HBoxContainer/Icon");
        var play=new Polygon2D{Color=new Color(1,.96f,.89f),Antialiased=true,Polygon=[new(-12,-15),new(14,0),new(-12,15)]};
        icon.Texture=null;icon.AddChild(play);
        void CenterIcon()=>play.Position=icon.Size/2;
        icon.Resized+=CenterIcon;CenterIcon();
        var unavailable=(Control)share.GetNode<Control>("TextureRect").Duplicate(0);
        unavailable.Name="ReplayUnavailable";button.AddChild(unavailable);
        var view=new View(button,background,label,unavailable);
        button.Connect(NClickableControl.SignalName.Focused,Callable.From<NClickableControl>(_=>view.Hover(true)));
        button.Connect(NClickableControl.SignalName.Unfocused,Callable.From<NClickableControl>(_=>view.Hover(false)));
        button.Connect(NClickableControl.SignalName.Released,Callable.From<NButton>(_=>TaskHelper.RunSafely(Open(view))));
        view.Update(null);return view;
    }

    private static async Task Open(View view)
    {
        if(_opening||PlayerLoop.Active||view.Selected is not {} selection)return;
        _opening=true;
        try
        {
            Runtime.CheckEnvironment();
            var catalog=new HistoryReplayCatalog(Runtime.Root,selection.Scope);
            if(selection.Scope!=Runtime.Scope||catalog.Find(selection.Key,Runtime.GameHash,RunPlayback.Compatible)!=selection.Link)
            {view.Update(null);return;}
            var run=RunRecording.Store.Read(selection.Link.Run);
            Runtime.Log("history-replay-open",new{History=HistoryReplayKey.FileName(selection.Key.StartTime),run.Id,selection.Link.Start});
            await PlayerLoop.Open(selection.Link.Clip,entry:"history",chain:run,chainStart:selection.Link.Start);
            if(PlayerLoop.Error is null)Return(selection);
        }
        catch(Exception ex){Failure("history-replay-open-failed",ex);}
        finally{_opening=false;}
    }

    private static void Return(Selection origin)
    {
        if(origin.Scope!=Runtime.Scope||Runtime.Game.MainMenu is not {} menu||!NRunHistory.CanBeShown())return;
        _returning=origin;
        try
        {
            menu.OpenCompendiumSubmenu();
            var screen=menu.SubmenuStack.PushSubmenuType<NRunHistory>();
            var restored=Views.TryGetValue(screen,out var view)&&view.Displayed==origin.Key;
            if(!restored){menu.SubmenuStack.Pop();menu.SubmenuStack.Pop();}
            Runtime.Log("history-replay-return",new{History=HistoryReplayKey.FileName(origin.Key.StartTime),IdentityMatched=restored});
        }
        finally{_returning=null;}
    }

    private static JsonObject NativeJson(RunHistory history)=>JsonNode.Parse(JsonSerializationUtility.ToJson(history))!.AsObject();
    private static void HistorySaved(RunHistory __0)
    {
        if(!RunRecording.Enabled||PlayerLoop.Active)return;
        try{RunRecording.HistorySaved(NativeJson(__0));}
        catch(Exception ex){Failure("history-replay-native-save-link-failed",ex);}
    }

    internal static void Publish(RunDocument run,JsonObject history,bool log=true)
    {
        try
        {
            var start=history["start_time"]!.GetValue<long>();
            if(HistoryReplayKey.Read(HistoryReplayKey.FileName(start),history) is not {} key)return;
            var link=new HistoryReplayCatalog(Runtime.Root,run.Scope).Publish(key,history,run,id=>Runtime.ReadScopedClip(id,run.Scope));
            if(log)Runtime.Log("history-replay-native-save-linked",new{run.Id,History=HistoryReplayKey.FileName(start),Playable=link is not null});
        }
        catch(Exception ex){if(log)Failure("history-replay-index-write-failed",ex);else Console.Error.WriteLine("ReplayMod history index: "+ex.GetBaseException().Message);}
    }

    internal static bool PauseOpen=>NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCapstoneSubmenuStack {ScreenType:NetScreenType.PauseMenu};
    internal static void OpenPause()
    {
        if(PlayerLoop.Active&&!PlayerLoop.TearingDown&&!PauseOpen&&NRun.Instance?.GlobalUi.TopBar.Pause is {} gear)
            PauseRelease.Invoke(gear,null);
    }
    private static bool AllowGiveUp()=>!PlayerLoop.Active;
    private static void PauseExitPressed(NPauseMenuButton __instance)
    {
        if(!PlayerLoop.Active||PlayerLoop.TearingDown||__instance.Name!="SaveAndQuit"||NonInteractiveMode.IsActive)return;
        // NButton.OnPress routes its click through SfxCmd, which skips every
        // cue while combat is ending. The viewer's exit is still interactive.
        var nativeClickSkipped=CombatManager.Instance.IsEnding;
        if(nativeClickSkipped)NAudioManager.Instance?.PlayOneShot("event:/sfx/ui/clicks/ui_click");
        Runtime.Log("playback-menu-exit-click",new{CombatEnding=nativeClickSkipped,FallbackRequested=nativeClickSkipped});
    }
    private static void PauseInitialized(NPauseMenu __instance)
    {
        if(!PlayerLoop.Active)return;
        try
        {
            var box=__instance.GetNode<Control>("%ButtonContainer");
            box.GetNode<NPauseMenuButton>("GiveUp").Hide();
            var exit=box.GetNode<NPauseMenuButton>("SaveAndQuit");
            exit.GetNode<MegaLabel>("Label").SetTextAutoSize("退出回放");
            var buttons=box.GetChildren().OfType<NPauseMenuButton>().Where(b=>b.Visible&&b.IsEnabled).ToArray();
            for(var i=0;i<buttons.Length;i++)
            {
                buttons[i].FocusNeighborTop=buttons[(i+buttons.Length-1)%buttons.Length].GetPath();
                buttons[i].FocusNeighborBottom=buttons[(i+1)%buttons.Length].GetPath();
            }
        }
        catch(Exception ex){Failure("history-replay-pause-menu-failed",ex);}
    }
    private static void Failure(string kind,Exception ex)=>Runtime.Log(kind,new{Reason=ex.GetBaseException().Message});
}

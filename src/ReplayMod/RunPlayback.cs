using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class RunPlayback
{
    internal sealed record RoomJump(RunRoomPoint Target,Clip Anchor,long Requested,RoomEntrySave? Cached)
    {
        internal bool Direct=>Target.Direct||Cached is not null;
    }
    private sealed class RoomJumpRequested:Exception;
    internal static bool Active,NonCombat,Driving;
    private static RunDocument _run=null!;
    private static int _index,_decision,_fine;
    private static bool _auto,_stop,_continuation,_allowRewards,_endpoint;
    private static CombatRoom? _rewardsRoom;
    private static readonly HashSet<Task> OwnedTasks=[];
    private static RunState? _mapRun;
    private static MapCoord _mapDestination;
    private static int _mapGeneration;
    private static bool _mapVoted;
    private static MoveToMapCoordAction? _mapMove;
    private static Task? _mapTask;
    private static long _phaseAt;
    private static Dictionary<int,RunContinuation[]> _continuations=[];
    private static Dictionary<int,RunContinuation[]> _progressContexts=[];
    private static int _rawOperations;
    private static readonly HashSet<int> VerifiedBattles=[];
    private static readonly HashSet<int> NeededComputedSaves=[];
    private static (RunContinuation Point,RunOperation Battle)? _eventLoad;
    private static RunRoomPoint[] _roomPoints=[];
    private static RoomJump? _requestedRoom,_landingRoom;
    private static Task? _roomReveal;
    private static bool _waitingForPhase,_roomCovered,_roomSelectionReady,_roomPresentationOpened;
    internal static IReadOnlyList<RunRoomPoint> RoomPoints=>_roomPoints;
    internal static bool RoomJumpPending=>_roomSelectionReady&&_requestedRoom is not null;
    internal static bool RoomJumpSelecting=>_requestedRoom is not null&&!_roomSelectionReady;
    internal static bool RoomJumping=>_requestedRoom is not null||_landingRoom is not null;
    internal static bool RoomCovered=>_roomCovered;
    internal static bool WarmingRoom=>_landingRoom is {Direct:false};
    internal static bool RebuildingRoom=>WarmingRoom&&_roomCovered;
    internal static RunOperation? CacheableRoomTravel=>Active&&_mapRun is {} run&&Current is {Kind:"map"} travel&&
        ReferenceEquals(run,RunManager.Instance.DebugOnlyGetState())&&run.CurrentMapCoord==_mapDestination&&
        CacheableRoomDestination?travel:null;
    private static bool CacheableRoomDestination=>_index+1<_run.Operations.Count&&
        _run.Operations[_index+1] is {Kind:not "battle" and not "event-return",Before:not null} next&&
        !_continuations.ContainsKey(next.Sequence);
    private static RoomEntryCacheKey? RoomEntryKey(RunOperation? travel)
    {
        if(travel is null||travel.Sequence>=_run.Operations.Count||
            _run.Operations[travel.Sequence] is not {Before:not null} next)return null;
        return RoomEntryCacheFile.KeyFor(_run,travel,next);
    }
    internal static bool MapBrowsingAvailable=>Active&&!_stop&&!_endpoint&&!RoomJumping&&!PlayerLoop.Loading;
    internal static bool CanOpenObserverMap=>MapBrowsingAvailable&&(NonCombat?
        _waitingForPhase||Current?.Kind=="map"&&_mapRun is null:NativeSeek.CanLeaveRoom);
    internal static RunOperation? MapSource(int act)=>_run.Operations.LastOrDefault(op=>
        RunRoomPoints.BattleAnchor(op)&&op.Battle?.Act==act);
    // This exact recording build has the same run/clip format and native inputs.
    internal const string RecordingBuild="145E3A3DE8A918CBD8AE90EBF4EFE1D7413E3F46911A1A5C26298266828F66EC";
    internal const string AcceptedChainRecordingBuild="DD563F456E5A693E9139F669B3C7E9B95ECFB11D6096A32A79F6EE82E89CF1FA";
    internal const string Run001Baseline="392B6DEB1899B1D5D9E71325382F2999D049CC16E0A9568A9B08951017D4E9DB";
    internal const string TreasureRecordingBuild="2FB5E5938104CA23256FFF25895DD2AB27A324A31CDD5484704F4149F4C584F3";
    internal const string TreasureObtainBuild="0DE66D1936532107143E6AE9B42E80CC94A6D5884A7C2FD5939C51EBDEE25B3E";
    internal const string AccountContextBuild="1AD070C28A881D7E9ACA8A7F13EFFD5F975F56217C6ECB8A43F8672AECB15C08";
    internal const string MerchantRecordingBuild="1C02153EFE00347294CBAADB2789ECDD304CCE495EE5C3C34FDB549047909A8F";
    internal const string EventRecordingBuild="B09A643DF2ECE027ECBDA8EB3EA437BCA6FF8086173B192AD3CA1E9CE7480C04";
    internal const string EventAcceptedBuild="3902D4A1203D9A1CCA5248B972565348ED321745EA335D3A6F86569CED6DBDC8";
    internal const string InternalEventRecordingBuild="7B27D6217ACE4D1B66A0D43FCAC4AF67E9C4DE21BD15D584D4109531CC0F108D";
    internal const string EventLoadBuild="0C9EAC791A373AB641FBABFF66E26C48CD45363BD7F559F685A1A0E05413B2EB";
    internal const string EventPresentationBuild="56163611CA0AA73A28D87CC2E2F3F33B5460AB8932D3A1CD23A62011688C1D99";
    internal const string EventStopBuild="A91D084118A288EEFF21E7CE1D7AE47E68B6FA9942066AAC3F5A4C3B959F07E4";
    internal const string NeowRecordingBuild="6FA1CE6B4D74C6290A3A264D3E7524EB29E848FE00A0BE13334738B177CDFBF1";
    internal const string EliteRecordingBuild="8178809C473A012396E85374C9BD2865AD126F7179BCF9DF48DEAFFF719189CC";
    internal const string BossRecordingBuild="69A1529F939E9452F2FF4AB96A753C532153C287378C63E01CE69154753CC0E7";
    internal const string ActRecordingBuild="BAE17032415D20C4F7844779B921379D8994ABC7C3BE69AC9106988EDADDA70B";
    internal const string AcceptedActRecordingBuild="7B7410D2A03968C5C8032E1351B078A8508D44348306F314848695BEDFE4E0B1";
    internal const string ThirdActRecordingBuild="4B591FA3D071A67E8D4332355825BF769B6708C919D2093A62F7531332DF8657";
    internal const string ThirdActPlaybackBuild="90B730881BE26B2B35E23E44383DADD976CF6C17CD40CAE97BF6DE0F203BE5F3";
    internal const string FinalEventRecordingBuild="565CD1A9A9AF0C54B09E6FF3E1179FB341D412ABF56AB5ED2D1FD7E0B551DB7A";
    internal const string PreparedFinalRecordingBuild="329A01248024F56D130AF6A09022BD9DCA2B540C5B260D413A05BA4F3B891310";
    internal const string DeathRecordingBuild="045FAF8B7A31A005889D591573919361828BACCC2BA9DF6CF00B044C1FB72172";
    internal const string AbandonRecordingBuild="5EF89002446D5E464717FC8E8A2F87845C3805871332274FE75FB6FAC1C9760B";
    internal const string FullRunRecordingBuild="572FFCB169D37C5EB7705A6306D4F4063CB8AD7F1380E81E2F3E7DD1FC7BE39F";
    internal const string FullRunVictoryRecordingBuild="D58370E28CFE3FE06F32FE96F15FF31111375F5607FDAFE8B45555DD3D384401";
    internal const string RunProgressRewindBuild="17FA8B05F614A93BE9D4857EE1ECED733CC5B0ED4F99C7D3BBDABDF1EFD5E6E3";
    internal const string RunRewardsRecordingBuild="177EE6F3709A7316761AEAB8E673FDF5A2FD4688E3C7851B19F594D029A14BF6";
    internal const string HollowRecordingBuild="912353A0EEAB33775E39EFA0538194835A65C120027BC7D74E1AA0D02DC7445D";
    internal const string NeowPathsRecordingBuild="EB017932EFBF60733527B4C29A0D778911020616CACF50EAEA26C3D6B3204917";
    internal const string BundleEnchantRecordingBuild="4227745F89B72F1EAD82F684CA5A1E9962A09775403B233F0E67D811D028437B";
    internal const string AncientChoiceRecordingBuild="27CD8B37016D7280CF6EF59C8F4D083C4FA28B5ADD7B20BD2E6FF6F48457CC81";
    internal const string RelicFlashRecordingBuild="1F9E6B560031BABD61B17B56165A91101DC3F4FC3CC1E130C8B1E29F01665894";
    internal const string NoncombatPotionRecordingBuild="A5BDBCA61C139A9B94B0DF30450D376F5A8E9D77CEA0CBFE84EC913E6622D146";
    internal const string CombatPreviewRecordingBuild="E048071C0151907245BC852AFE13023BCA5775C5340306E12EE56A2EF1C4901F";
    internal const string PileFlightRecordingBuild="08E6FA02D9A1DC55860C0FEF6F37CD999847AC94F9754F7566B3A3189BD864D8";
    internal const string EliteMusicRecordingBuild="329AF3F38D7316DEFA0D625D09E5F6B5F5157F48CE8B33DB235FE6EDD648EA17";
    internal const string FakeMerchantRecordingBuild="021C08DC0512FBC69A887B480FB00744EF1A22335432787B5DB66A9636A916F4";
    internal const string CombatRetirementRecordingBuild="D349FD9CBE981C053804D59A42E9E3DEA0B6E5E94B006ADAE6B69904CE8FFA2A";
    internal const string SculptorRetirementRecordingBuild="23EF060B688CDDD1BCD1BADD5CEB6CB845A065DA1B05055D44270D6030D376F2";
    internal const string SculptorParticleRecordingBuild="194AB8581EF3D7054E208F9C8630FF8D66DF717B3E7897F3BA8B67A1C57978C1";
    internal const string CompletedBaselineRecordingBuild="F276C27E0CA0B647B5FCBB1F361E82EC9B651DBEA0285A003F890BADB7CEDF10";
    internal const string RetainedSegmentRecordingBuild="804FBEF31B3EEC7BE5819DF4BE77BDA70A48B1169F97FBB78E5F326E82331736";
    internal const string ArchitectEntryRecordingBuild="D7284CDA6CD2D5F1D7294F075F8D02EAD1C545B9D165D003690D41BB7C7FC155";
    internal const string CombatRecoveryRecordingBuild="BAC8EDE60AB04701071B2CFB2B18D427BC3824C1B5F1E488AEDC1F8510D1420B";
    internal const string CombatPresentationRecordingBuild="D5C64A41B2A1B6C395CFADDD17AAF012C3EF1F1F8E7A774454843665FD097926";
    internal const string CombatPictureRecordingBuild="1AF2438ED7C2B1C8C2AB49BC8637005BB1B8DCD6EADB0AF9991E68114E0D7DFB";
    internal const string CombatSharedRetirementRecordingBuild="757F78AC1E98EEC87C335E56D76C147497B2886DD0877CAB8781C3FA45D89D29";
    internal const string CombatSceneSettleRecordingBuild="7CB745D5992B9BA70D9669DDD617E758D5D1E5984B6EC393A17F1E090A60BC66";
    internal const string BossScoreAndColdChoiceRecordingBuild="A9ABA64F6CFCDAC815F78750D17D8155D97E02C5972BDB88A78791676C0FC651";
    internal const string ComputedTurnAndAnchorRecordingBuild="DE476156629FDCDC313265FB69FCE6291330627B713FDF34AD88D1F5875B0692";
    internal const string ColdTurnAndVictoryCueRecordingBuild="E12B5AB60815A532CB5B32D2926C2031B48D6C71CD4A34276E600DB13849AE6B";
    internal const string MapVictoryControlsRecordingBuild="54ADA54E1EBC4BE6B4502A1A47763A0D6B77839310AA919858E47451D7DDF1B2";
    internal const string SeekAudioAndCacheRecordingBuild="3A35E96058EE4CD6466B198E638C179A6A5809F3CA1B02509941632203DF6AB5";
    internal const string MapEntryPresentationRecordingBuild="4ECAA9D114E018BEA6CA93CA965ECA14841DC947A661E8B425B6FC14E3DED6AB";
    internal const string MapPreviewRecordingBuild="19D52FAEFD605B71DFF0C033429C8FF2078379B73D9032E9E83193E31DB4EB64";
    internal const string FirstRoomJumpRecordingBuild="10772AD24130A564FCA23D34B3CC317F1B5326A24067BA9D01BDA22A01376DB3";
    internal const string KnockoutBoundaryRecordingBuild="12637713E0C07BC5F9D18DBD7005915CF6E1695AB26FC64DEBCAB82AED9B0E7E";
    internal const string ForwardCombatAudioRecordingBuild="684603EB00E0AD9545D414ED75DB4E700AD772200ED30C16E78EE13249826D15";
    internal const string EndingExitCueRecordingBuild="9CB69A9939109E9160717371C425B7994AD595836CC2BBD9B6CB52108126C76D";
    internal const string HistoryHoverRecordingBuild="BE0A6989D30A8F2AAB7E6BA2F2BDCAC22E16F7D51CA769472701D5B6D5437D52";
    internal const string NativeHistoryRecordingBuild="508BED31A64485A79F0DBEBAA8575A042968CA441CA2A8920F56C4A939F868D0";
    internal const string ElitePhaseOrderRecordingBuild="166F2B1384BE1011B42F666316551F9406D3978286DD262F1053F4E250D07530";
    internal const string DeskPetRecordingBuild="769F2609B02AA42E6ACDD96D4A298098BF1862910712BC52892CF2FC245C4490";
    internal const string CombatStopRetirementRecordingBuild="16D81968BCA2830780092AC039A9C72B8415C6EEA9474105442E1AFC662A2E8E";
    internal const string ActionAudioRetirementRecordingBuild="FEA5BAAF784F2361FB7E93473F6B8AB05009965DC773332A96C058D732D25BA2";
    internal const string CharacterCastRetirementRecordingBuild="3D1B390A0D0229E6122AA72CF5E72C941665707214F39BF02EAB4DBC8172E582";
    internal const string VictoryMusicRetirementRecordingBuild="4F14C3BE7FD098F55FB7710B03F53F6BB8F4CC307C4DB948EF9493FCC2AD09C1";
    internal const string DoomRetirementRecordingBuild="4825F012D26D1CAFE0FBEEFACF46723B02FC89387A9B76DE176AADE714927810";
    internal const string EnrageRetirementRecordingBuild="CD94E2B16AA4A0224E3197C5F3B459BE8FE31C7B2A80CB524BB79192F26D3266";
    internal const string SmallChoiceRetirementRecordingBuild="B2B50B97F86428887960AE5BD7DF3106404E2F23518BB3D31931047B85695963";
    internal const string EnemyPowerSettleRecordingBuild="CD3C446EAF3610CC6DA0BC8CCB10719D4ED176516F751616A874D0320454C02D";
    internal static bool Compatible(string hash)=>hash==Runtime.ModHash||hash==BossScoreAndColdChoiceRecordingBuild||hash==ComputedTurnAndAnchorRecordingBuild||hash==ColdTurnAndVictoryCueRecordingBuild||hash==MapVictoryControlsRecordingBuild||hash==SeekAudioAndCacheRecordingBuild||hash==MapEntryPresentationRecordingBuild||hash==MapPreviewRecordingBuild||hash==FirstRoomJumpRecordingBuild||hash==KnockoutBoundaryRecordingBuild||hash==ForwardCombatAudioRecordingBuild||hash==EndingExitCueRecordingBuild||hash==HistoryHoverRecordingBuild||hash==NativeHistoryRecordingBuild||hash==ElitePhaseOrderRecordingBuild||hash==DeskPetRecordingBuild||hash==CombatStopRetirementRecordingBuild||hash==ActionAudioRetirementRecordingBuild||hash==CharacterCastRetirementRecordingBuild||hash==VictoryMusicRetirementRecordingBuild||hash==DoomRetirementRecordingBuild||hash==EnrageRetirementRecordingBuild||hash==SmallChoiceRetirementRecordingBuild||hash==EnemyPowerSettleRecordingBuild||hash==CombatSceneSettleRecordingBuild||hash==CombatSharedRetirementRecordingBuild||hash==CombatPictureRecordingBuild||hash==CombatPresentationRecordingBuild||hash==CombatRecoveryRecordingBuild||hash==ArchitectEntryRecordingBuild||hash==RetainedSegmentRecordingBuild||hash==CompletedBaselineRecordingBuild||hash==SculptorParticleRecordingBuild||hash==SculptorRetirementRecordingBuild||hash==CombatRetirementRecordingBuild||hash==FakeMerchantRecordingBuild||hash==EliteMusicRecordingBuild||hash==PileFlightRecordingBuild||hash==CombatPreviewRecordingBuild||hash==NoncombatPotionRecordingBuild||hash==RelicFlashRecordingBuild||hash==AncientChoiceRecordingBuild||hash==BundleEnchantRecordingBuild||hash==NeowPathsRecordingBuild||hash==HollowRecordingBuild||hash==RunRewardsRecordingBuild||hash==RunProgressRewindBuild||hash==FullRunVictoryRecordingBuild||hash==FullRunRecordingBuild||hash==AbandonRecordingBuild||hash==DeathRecordingBuild||hash==PreparedFinalRecordingBuild||hash==FinalEventRecordingBuild||hash==ThirdActPlaybackBuild||hash==ThirdActRecordingBuild||hash==RecordingBuild||hash==AcceptedChainRecordingBuild||hash==Run001Baseline||hash==TreasureRecordingBuild||hash==TreasureObtainBuild||hash==AccountContextBuild||hash==MerchantRecordingBuild||hash==EventRecordingBuild||hash==EventAcceptedBuild||hash==InternalEventRecordingBuild||hash==EventLoadBuild||hash==EventPresentationBuild||hash==EventStopBuild||hash==NeowRecordingBuild||hash==EliteRecordingBuild||hash==BossRecordingBuild||hash==ActRecordingBuild||hash==AcceptedActRecordingBuild;
    internal static bool BattleVerified(int source)=>VerifiedBattles.Contains(source);
    internal static bool NeedsComputedSave(int source)=>NeededComputedSaves.Contains(source);
    internal static RunOperation? Current;
    internal static bool HasFollowing=>Active&&_index+1<_run.Operations.Count&&_run.Operations[_index+1].Status=="complete"&&_run.Operations[_index+1].Kind!="gap";
    internal static bool WantsNext=>HasFollowing&&(_auto||_continuation);
    internal static bool Stopping=>_stop;
    internal static bool MapStopTestArmed;
    internal static bool Finished=>Active&&_endpoint;
    internal static bool Auto=>_auto;
    internal static RunStartInput? OpeningInput=>Active?_run.Opening:null;
    internal static string Display=>Active?$"\n路线 · 第{_run.Visits.Single(v=>v.Id==(Current?.Visit??_run.Operations[_index].Visit)).Act+1}幕{_run.Visits.Single(v=>v.Id==(Current?.Visit??_run.Operations[_index].Visit)).Floor}层 · 操作{_index+1}/{_run.Operations.Count}"+
        (Current?.Attempt is {} attempt?$" · 采用尝试#{attempt}（最终延续路线）":"")+
        (NonCombat?$" · {(_auto?"自动连续":"手动等待／执行")} · 待播{_fine}":""):"";
    internal static int StartIndex(RunDocument run)=>RunStore.FirstBattleIndex(run);
    internal static Clip EventBattle(string parent,string encounter)
    {
        Codec.Require(Current?.Kind=="event-choice"&&_index+1<_run.Operations.Count,"event-battle-without-parent-choice");
        var op=_run.Operations[_index+1];
        Codec.Require(op is {Kind:"battle",Status:"complete",Clip:not null}&&op.Visit==Current!.Visit&&
            op.Attempt==Current.Attempt&&op.Battle is {Entry:"event",ResumeParent:true} b&&b.ParentEvent==parent,"event-battle-stage-or-owner-mismatch");
        var clip=Runtime.ReadClip(op.Clip!);
        Codec.Require(clip.Encounter==encounter&&clip.Battle==op.Battle,"event-battle-clip-mismatch");return clip;
    }
    internal static void Begin(RunDocument run,int? startSequence=null,RunRoomPoint? cachedRoom=null)
    {
        _rawOperations=run.Operations.Count;run=RunRoutes.Playback(run);
        Codec.Require(run.GameHash==Runtime.GameHash&&run.Scope==Runtime.Scope&&Compatible(run.ModHash),"run-environment-or-build-mismatch");
        var opening=startSequence==0;
        _index=cachedRoom?.Entry?.Sequence??(opening?0:startSequence is {} sequence?
            run.Operations.FindIndex(o=>(o.SourceSequence??o.Sequence)==sequence):StartIndex(run));
        Codec.Require(cachedRoom is {} room?_index>0&&_index<run.Operations.Count&&run.Operations[_index-1]==room.Entry&&
            run.Operations[_index] is {Status:"complete",Kind:not "battle",Before:not null} entry&&entry.Visit==room.Visit.Id&&entry.Attempt==room.Attempt:
            opening?RunOpening.Available(run):_index>=0&&RunRoomPoints.BattleAnchor(run.Operations[_index]),"run-has-no-recorded-entry-start");
        Codec.Require(!RunStore.HasUnrecordedAfter(run,run.Operations[_index].Sequence),"run-unrecorded-gap-after-selected-start");
        var discontinuities=RunStore.ResourceDiscontinuities(run);
        Codec.Require(!discontinuities.Any(s=>s>run.Operations[_index].Sequence),"run-has-unrecorded-resource-transition");
        _roomPoints=RunRoomPoints.Read(run,discontinuities);
        var first=run.Operations[_index];
        _progressContexts=run.Continuations.Where(c=>c.Status=="native-continue"&&c.Progress is not null&&c.BeforeOperation>=first.Sequence)
            .Select(c=>(Before:RunRoutes.ProgressBefore(run,c),Point:c)).Where(c=>c.Before>=first.Sequence)
            .GroupBy(c=>c.Before).ToDictionary(g=>g.Key,g=>g.Select(c=>c.Point).ToArray());
        _continuations=run.Continuations.Where(c=>c.PreFinished&&c.Visit>=first.Visit)
            .Select(c=>(Before:RunStore.ContinueBefore(run,c),Point:c)).Where(c=>c.Before>=first.Sequence)
            .GroupBy(c=>c.Before).ToDictionary(g=>g.Key,g=>g.Select(c=>c.Point).ToArray());
        _run=run;Active=true;NonCombat=opening||cachedRoom is not null;Driving=false;_auto=false;_stop=false;_continuation=false;
        _endpoint=false;
        RunEndings.Clear();
        _fine=0;_decision=0;_phaseAt=0;_rewardsRoom=null;OwnedTasks.Clear();_mapRun=null;Current=run.Operations[_index];RunPatches.Clear();
        VerifiedBattles.Clear();NeededComputedSaves.Clear();_eventLoad=null;
        foreach(var point in _continuations.Values.SelectMany(p=>p))
        {
            var source=RunRoutes.CompletedAncientChoice(run,point)??RunRoutes.CompletedEventBattle(run,point)??RunRoutes.CompletedMapBattle(run,point);
            if(source is not null)NeededComputedSaves.Add(source.SourceSequence??source.Sequence);
        }
    }
    internal static void Clear(){Active=false;NonCombat=false;Driving=false;_endpoint=false;Current=null;_rewardsRoom=null;OwnedTasks.Clear();VerifiedBattles.Clear();NeededComputedSaves.Clear();_eventLoad=null;_continuations.Clear();_progressContexts.Clear();_mapRun=null;_mapMove=null;_mapTask=null;
        _roomPoints=[];_requestedRoom=null;_landingRoom=null;_roomReveal=null;_waitingForPhase=false;_roomCovered=false;_roomSelectionReady=false;
        RunPatches.Clear();RunTreasures.Clear();RunMerchants.Clear();RunEvents.Clear();RunEventCombat.Clear();RunActs.Clear();RunComputedLoads.Clear();RunEndings.Clear();}

    internal static void PauseForMap()
    {
        _auto=false;
        NativeSeek.PauseAuto();
    }
    internal static void RequestRoomJump(RunRoomPoint target,NMapScreen screen,NMapPoint node)
    {
        if(!CanOpenObserverMap||NonCombat&&!_waitingForPhase||target.StartSource is not {} source)return;
        var clipOperation=target.Anchor??_run.Operations[StartIndex(_run)];
        Clip clip;
        try
        {
            clip=Runtime.ReadClip(clipOperation.Clip!);
            Codec.Require(NativePlaybackSupport.Reason(clip) is null&&clip.Battle==clipOperation.Battle,"room-jump-anchor-clip-mismatch");
        }
        catch(Exception ex) when(ex is InvalidDataException or IOException)
        {
            Runtime.Status="此房间的回放起点不可用";
            Runtime.Log("run-room-jump-unavailable",new{Visit=target.Visit.Id,SourceSequence=source,Reason=ex.Message});
            return;
        }
        var jump=new RoomJump(target,clip,Stopwatch.GetTimestamp(),RunRoomEntryCache.Find(RoomEntryKey(target.Entry)));
        _requestedRoom=jump;
        PauseForMap();_fine=0;_continuation=false;
        Runtime.Status="正在定位录像房间";
        Ui.Refresh();
        Runtime.Log("run-room-jump-requested",new{Visit=target.Visit.Id,target.Visit.Act,target.Attempt,SourceSequence=source,jump.Direct,
            CachedEntry=jump.Cached is not null});
        Track(SelectRoom(jump,screen,node));
    }
    private static async Task SelectRoom(RoomJump jump,NMapScreen screen,NMapPoint node)
    {
        await ReplayMap.ShowSelection(screen,node);
        if(_stop||PlayerLoop.TearingDown)return;
        SfxCmd.Play("event:/sfx/ui/wipe_map");
        PlayerLoop.BeginRoomJumpTransition();
        await Runtime.Game.Transition.RoomFadeOut();
        if(_stop||PlayerLoop.TearingDown)return;
        SeekAudio.Begin(room:true);
        _roomCovered=!jump.Direct;
        _roomSelectionReady=true;
        screen.Close(animateOut:false);
        Runtime.Log("run-room-jump-map-covered",new{Visit=jump.Target.Visit.Id,NativeFadeOutCompleted=true,MapClosedUnderCover=true});
        Ui.Refresh();
    }
    internal static RoomJump? TakeRoomJump()
    {
        var requested=_requestedRoom;_requestedRoom=null;_roomSelectionReady=false;return requested;
    }
    internal static void BeginRoomLanding(RoomJump jump)
    {
        _landingRoom=jump;_roomCovered=!jump.Direct;_roomPresentationOpened=false;
        _auto=_roomCovered;Runtime.Status="正在恢复录像房间";Ui.Refresh();
    }
    internal static void RoomSceneInstalled()
    {
        if(_landingRoom is not {} jump||_roomPresentationOpened||_stop||PlayerLoop.TearingDown||!AtRoom(jump.Target))return;
        // Native combat setup and the first draw run after the scene is installed.
        // Lift prefix audio suppression before the destination starts its opening.
        _roomPresentationOpened=true;_roomCovered=false;SeekAudio.End();
        Runtime.Log("run-room-jump-target-presentation-opened",new{Visit=jump.Target.Visit.Id,jump.Direct});
        if(jump.Direct&&jump.Target.Entry is not null)
        {
            _roomReveal=Runtime.Game.Transition.RoomFadeIn();
            Track(_roomReveal);
        }
        // A native map vote owns the fade-in after the target room is installed.
        Ui.Refresh();
    }
    private static bool AtRoom(RunRoomPoint target)=>RunManager.Instance.DebugOnlyGetState() is {} state&&
        state.CurrentActIndex==target.Visit.Act&&state.CurrentMapCoord is {} coord&&coord.row==target.Visit.Row&&coord.col==target.Visit.Col&&
        state.CurrentRoom?.RoomType.ToString()==target.RoomType&&state.CurrentRoom?.ModelId?.ToString()==target.Model;
    internal static async Task FinishRoomLanding()
    {
        if(_landingRoom is not {} jump)return;
        Codec.Require(AtRoom(jump.Target),"run-room-jump-destination-mismatch");
        var compared=jump.Target.Direct&&jump.Target.Entry is not null; // LoadRoom checked the battle's full initial projection.
        var index=jump.Direct?_index:_index+1;
        if(!compared&&index<_run.Operations.Count&&_run.Operations[index] is {Before:{} expected} next&&
            next.Visit==jump.Target.Visit.Id&&!_continuations.ContainsKey(next.Sequence))
        {
            Compare(next.Sequence,"room-jump-entry",expected,RunResources.Capture());
            compared=true;
        }
        RoomSceneInstalled();if(_roomReveal is {} reveal)await reveal;
        _roomReveal=null;_landingRoom=null;_roomCovered=false;PauseForMap();
        PlayerLoop.CompleteRoomJumpTransition();
        SeekAudio.End();
        ReplayMap.Arrived();
        Runtime.Log("run-room-jump-arrived",new{Visit=jump.Target.Visit.Id,jump.Target.Visit.Act,jump.Target.Attempt,
            SourceSequence=jump.Cached is null?jump.Target.StartSource:jump.Target.Entry?.SourceSequence??jump.Target.Entry?.Sequence,
            Source=jump.Cached is not null?"verified-native-entry-cache":jump.Direct?"recorded-entry-anchor":"verified-native-route-prefix",
            Manual=true,NativeSceneInstalled=true,EntryStateAndRngCompared=compared,
            Milliseconds=Stopwatch.GetElapsedTime(jump.Requested).TotalMilliseconds});
        Ui.Refresh();
    }
    internal static RunLifecycleEvent? TerminalFact()=>RunLifecycle.AtOperation(_run,Current!);
    internal static void VerifyDeath(string state)
    {
        var fact=TerminalFact();
        Codec.Require(Current is {Kind:"battle",Battle.Completion:"death"}&&_index==_run.Operations.Count-1&&
            fact is {Kind:"death"}&&fact.Act==LiveState.Run.CurrentActIndex&&fact.State==state,"run-native-death-position-resources-or-rng-mismatch");
    }
    internal static bool HasNativeFinalOpening=>_index+1<_run.Operations.Count&&RunStore.NativeFinalOpening(_run,Current!,_run.Operations[_index+1]);
    internal static void VerifyFinalEvent()
    {
        Codec.Require(_index+1<_run.Operations.Count,"run-final-event-recording-missing");
        var next=_run.Operations[_index+1];var visit=_run.Visits.Single(v=>v.Id==next.Visit);
        Codec.Require(RunStore.IsNativeFinalEventEntry(visit,next)&&
            LiveState.Run.CurrentRoom?.ModelId?.ToString()==visit.Model,"run-final-event-destination-mismatch");
        Runtime.Log("run-native-final-event-entry-verified",new{Visit=visit.Id,Model=visit.Model,SameRunState=true,
            FirstOperation=next.Kind,NativeRewardsVote=true,NativeTransitionCalls=1,CheckpointLoaded=false,ExpectedStateInjected=false});
    }
    internal static string? InitialProgress()
    {
        if(!_progressContexts.Remove(_run.Operations[_index].Sequence,out var points))return Current?.Progress;
        var point=points[^1];LogProgress(point,_run.Operations[_index].Sequence,true);return point.Progress;
    }
    private static void LogProgress(RunContinuation point,int before,bool initial)=>Runtime.Log("run-native-load-progress-context",new{
        BeforeOperation=before,point.Visit,point.Attempt,point.PreFinished,InitialMemoryContext=initial,
        SHA256=Codec.Hash(System.Text.Encoding.UTF8.GetBytes(point.Progress!)),IsolatedManagerOnly=true,RunResourcesOrRngRestored=false});
    private static void ApplyProgressContexts(int before)
    {
        if(!_progressContexts.Remove(before,out var points))return;
        foreach(var point in points){MemorySession.ApplyRecordedProgress(point.Progress!);LogProgress(point,before,false);}
    }
    internal static void Track(Task task)
    {
        if(!Active||!OwnedTasks.Add(task))return;
        _=task.ContinueWith(t=>{_ =t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
    }
    internal static async Task DrainAfterSceneExit()
    {
        if(!Active)return;
        var at=Stopwatch.GetTimestamp();
        while(OwnedTasks.Any(t=>!t.IsCompleted))
        {Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<5,"run-native-action-did-not-retire");await Runtime.NextFrame();}
        var faults=OwnedTasks.Where(t=>t.IsFaulted).Select(t=>t.Exception!.ToString()).ToArray();
        if(faults.Length>0)Runtime.Log("run-native-task-faults",new{Faults=faults,BeforeOriginalManagerRestored=true});
        // Faults have already been observed by the operation or its fault observer.
        // A settled failure must not prevent restoring the original manager.
        Runtime.Log("run-native-action-retired",new{Completed=true,Tasks=OwnedTasks.Count,Canceled=OwnedTasks.Count(t=>t.IsCanceled),Faulted=OwnedTasks.Count(t=>t.IsFaulted),BeforeOriginalManagerRestored=true});
        OwnedTasks.Clear();
    }
    internal static void Control(string command)
    {
        if(command=="stop")
        {Runtime.Log("run-playback-stop",new{Operation=_index+1,NonCombat,AtRecordedEnd=_index==_run.Operations.Count-1});
            _stop=true;_auto=false;_fine=0;
            if(ReplayMap.IsOpen)NMapScreen.Instance!.Close(animateOut:false);
            // Opening owns the in-flight room loader and can only retire it
            // once it finishes or suspends on an actual opening choice.
            // In combat the native driver drains the current recorded action,
            // including any choice it still has to submit under cover.
            if(!NativeSeek.Opening&&(NonCombat||!NativeSeek.StopDrainsCombat))PlayerLoop.ChoiceAbort=true;
            PlayerLoop.Cursor?.Stop();if(NativeSeek.Enabled)NativeSeek.Control("stop");return;}
        if(_stop)return;
        if(RoomJumping||ReplayMap.Observing)return;
        if(_endpoint)
        {Runtime.Log("run-endpoint-control-ignored",new{Command=command,Operation=Current?.Sequence,Pending=0,RecordedEndpoint=true});return;}
        if(!NonCombat&&!NativeSeek.Finished){NativeSeek.Control(command);return;}
        if(command=="auto")_auto=!_auto;
        else if(command=="fine")_fine++;
        else return;
        _continuation=true;
        Runtime.Log("run-playback-control",new{Command=command,Operation=_index+1,NonCombat,Auto=_auto,Fine=_fine});
    }
    internal static void AtBattleBoundary(bool auto){if(Active&&(HasFollowing||Current?.Battle?.Completion=="death"))_auto=auto;}
    internal static bool DeferRewards(CombatRoom room)
    {if(!Active||_allowRewards)return false;_rewardsRoom=room;return true;}
    internal static void Native(Action action)
    {var prior=Driving;Driving=true;try{action();}finally{Driving=prior;}}
    internal static void CheckStop()
    {
        if(_stop||PlayerLoop.TearingDown)throw new OperationCanceledException();PlayerLoop.RequireHealthy();
        if(OwnedTasks.FirstOrDefault(t=>t.IsFaulted) is {} failed)throw new InvalidDataException("run-native-owned-task-failed",failed.Exception);
    }
    internal static async Task TakePhase(string label)
    {
        Runtime.Status=label;
        _waitingForPhase=true;
        try
        {
            while(true)
            {
                CheckStop();
                if(RoomJumpPending)throw new RoomJumpRequested();
                if(PlayerLoop.HistoryMenuOpen||ReplayMap.Observing||RoomJumpSelecting){await Runtime.NextFrame();continue;}
                if(!WarmingRoom&&_auto&&_phaseAt!=0&&
                    Stopwatch.GetElapsedTime(_phaseAt).TotalSeconds<.65){await Runtime.NextFrame();continue;}
                if(_auto)break;
                if(_fine>0){_fine--;break;}
                await Runtime.NextFrame();
            }
        }
        finally{_waitingForPhase=false;}
        _phaseAt=Stopwatch.GetTimestamp();
        Runtime.Log("run-playback-phase",new{Operation=Current?.Sequence,Label=label,Auto=_auto});
    }
    internal static RunDecision Decision(string kind,string[] options)
    {
        Codec.Require(Current is not null&&_decision<Current.Decisions.Length,"run-unexpected-native-choice");
        var expected=Current!.Decisions[_decision++];
        Codec.Require(expected.Kind==kind&&expected.Options.SequenceEqual(options),"run-native-choice-options-mismatch");return expected;
    }
    internal static RunDecision? NextDecision=>Current is {} operation&&_decision<operation.Decisions.Length?operation.Decisions[_decision]:null;
    internal static async Task Wait(Func<bool> ready,string reason)
    {
        var at=Stopwatch.GetTimestamp();
        while(!ready()){CheckStop();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<45,reason);await Runtime.NextFrame();}
    }
    internal static async Task Await(Task task)
    {
        Track(task);
        while(!task.IsCompleted){CheckStop();await Runtime.NextFrame();}
        await task;
    }
    internal static async Task<bool> Run()
    {
        Runtime.Log("run-playback-open",new{_run.Id,StartOperation=_index+1,Visits=_run.Visits.Count,SameMemorySession=true,
            RecordedOperations=_rawOperations,SelectedOperations=_run.Operations.Count,PreservedAttempts=_run.Branches?.Attempts.Count});
        try
        {
            while(!_stop)
            {
                Current=_run.Operations[_index];
                if(Current.Kind=="battle")
                {
                    ApplyProgressContexts(Current.Sequence);
                    NonCombat=false;await NativeSeek.Run();
                    if(RoomJumpPending)return false;
                    if(_stop)return NativeSeek.AllMatched&&_index==_run.Operations.Count-1;
                    if(NativeSeek.DefeatReached)
                    {
                        Codec.Require(NativeSeek.AllMatched&&!HasFollowing&&RunEndings.DeathActive,"run-death-before-recorded-end");
                        VerifiedBattles.Add(Current.SourceSequence??Current.Sequence);NonCombat=true;
                        return await EndAtCurrent();
                    }
                    Codec.Require(NativeSeek.Finished&&HasFollowing,"run-battle-transition-without-complete-result");
                    Codec.Require(NativeSeek.AllMatched,"run-battle-unverified-before-continuation");VerifiedBattles.Add(Current.SourceSequence??Current.Sequence);
                    NativeSeek.Clear(preserveAudio:true);ChoiceTape.Clear();ActionMonitor.Reset();NonCombat=true;PlayerLoop.Loading=false;
                    _continuation=false;
                    _index++;continue;
                }
                Codec.Require(Current.Status=="complete"&&Current.Kind!="gap","run-recording-gap");
                NonCombat=true;_decision=0;
                await ApplyContinuations(Current);
                if(_rewardsRoom is {} room&&room.Encounter.ShouldGiveRewards)
                {
                    Codec.Require(Current.Kind is "reward" or "proceed" or "act-proceed" or RunRewardPotions.Kind||RunPotionUses.AtRewards(Current),"run-missing-reward-operation");
                    await TakePhase("显示战斗奖励");
                    _allowRewards=true;
                    try{Native(()=>AccessTools.Method(typeof(NCombatUi),"OnCombatWon").Invoke(NCombatRoom.Instance!.Ui,[room]));}
                    finally{_allowRewards=false;_rewardsRoom=null;}
                    // A native empty/completed terminal reward set hides its
                    // rewards window and leaves only Proceed. A recorded next
                    // reward still requires the actual visible offer.
                    await RewardsReady(requireVisible:Current.Kind=="reward"||RunPotionUses.AtRewards(Current));
                    // Give the fully presented page a readable Auto interval.
                    _phaseAt=Stopwatch.GetTimestamp();
                }
                await Execute(Current);
                Codec.Require(_decision==Current.Decisions.Length,"run-recorded-choice-not-consumed");
                if(Current.After is {} expected)Compare(Current.Sequence,"after",expected,
                    Current.Terminal is not null?RunEndings.PlaybackTerminal(Current):Current.Kind=="act-proceed"?RunActs.Result():RunResources.Capture());
                Runtime.Log("run-operation-verified",new{_run.Id,Current.Sequence,Current.SourceSequence,Current.Attempt,Current.Visit,Current.Kind,ResourcesAndRngMatched=true,NativeBusinessExecuted=true,ExpectedStateInjected=false});
                if(_landingRoom?.Target.Entry==Current)await FinishRoomLanding();
                if(_index==_run.Operations.Count-1)
                    return await EndAtCurrent();
                _index++;
            }
            return false;
        }
        catch(OperationCanceledException) when(_stop){return false;}
        catch(RoomJumpRequested) when(RoomJumpPending){return false;}
    }
    private static async Task<bool> EndAtCurrent()
    {
        var autoAtEnd=_auto;
        _endpoint=true;_fine=0;_continuation=false;
        Runtime.Log("run-recorded-end-reached",new{Operation=Current!.Sequence,AutoAtArrival=autoAtEnd,
            Pending=0,RecordedOperationsExhausted=true,NativeGameOver=RunEndings.GameOverActive});
        _auto=false;
        if(RunEndings.GameOverActive)
        {
            try{await EndingPlayback.Run(autoAtEnd);}
            catch(OperationCanceledException) when(_stop){return true;}
        }
        Runtime.Status=PlayerLoop.FromHistory?"已到记录终点；可从菜单退出回放":"已到记录终点；可Stop返回菜单";
        Runtime.Log("run-playback-endpoint",new{Operation=Current.Sequence,Auto=false,AwaitingStop=true,SceneKeptOpen=true,
            Pending=0,AdvanceDisabled=true,NativeGameOverControls=RunEndings.GameOverActive,
            AutoAtArrival=autoAtEnd,NativeSummaryCompleted=RunEndings.SummaryComplete});
        while(!_stop){CheckStop();await Runtime.NextFrame();}return true;
    }
    private static async Task ApplyContinuations(RunOperation operation)
    {
        if(!_continuations.Remove(operation.Sequence,out var points))return;
        ApplyProgressContexts(operation.Sequence);
        foreach(var point in points)
        {
            var visit=_run.Visits.Single(v=>v.Id==point.Visit);
            Codec.Require(operation.Visit==point.Visit&&LiveState.Run.CurrentActIndex==visit.Act&&
                (visit.ActModel is null||visit.ActModel==LiveState.Run.Act.Id.ToString())&&
                LiveState.Run.CurrentMapCoord is {} coord&&coord.row==visit.Row&&coord.col==visit.Col,"run-continue-room-boundary-mismatch");
            if(RunRoutes.CompletedAncientChoice(_run,point) is {} choice)
            {
                Codec.Require(LiveState.Run.CurrentRoom is EventRoom {IsPreFinished:true,CanonicalEvent:AncientEventModel} ancient&&
                    ancient.ModelId.ToString()==visit.Model,"run-ancient-continue-room-boundary-mismatch");
                await RunComputedLoads.Restore(point,choice,PlayerLoop.ComputedLoadKind.Ancient);continue;
            }
            Codec.Require(LiveState.Run.CurrentRoom is CombatRoom {IsPreFinished:true},"run-continue-combat-boundary-mismatch");
            if(RunRoutes.CompletedEventBattle(_run,point) is {} battle)
            {
                Codec.Require(_eventLoad is null,"run-event-load-boundary-ambiguous");_eventLoad=(point,battle);continue;
            }
            if(RunRoutes.CompletedMapBattle(_run,point) is {} completed)
            {
                _rewardsRoom=null;
                await RunComputedLoads.Restore(point,completed,PlayerLoop.ComputedLoadKind.MapBattle);continue;
            }
            // EnterMapPointInternal invokes this before restoring an already
            // completed room. Reuse that native history finalization while
            // keeping this playback's live RunState, resources and RNGs.
            AccessTools.Method(typeof(RunManager),"UpdatePlayerStatsInMapPointHistory").Invoke(RunManager.Instance,[]);
            Runtime.Log("run-native-continue-boundary",new{Operation=operation.Sequence,point.Visit,LegacyVisitBoundary=point.BeforeOperation is null,
                NativeHistoryFinalized=true,SameRunState=true,CheckpointLoaded=false,ExpectedStateInjected=false});
        }
    }
    private static void Compare(int sequence,string point,string expected,string actual)
    {
        if(RunStore.ResourcesMatch(expected,actual))return;
        Runtime.Log("run-resource-difference",new{Sequence=sequence,Point=point,Expected=expected,Actual=actual});
        throw new InvalidDataException("run-resources-or-rng-mismatch-"+sequence+"-"+point);
    }
    internal static void CompareBefore(RunOperation operation)
    {if(operation.Before is {} expected)Compare(operation.Sequence,"before",expected,RunResources.Capture());}
    internal static void ClickNative(NButton button)=>Native(()=>
    {
        // The native press plays the button's sound. Invoke the same release once
        // as before, without emitting an extra action signal.
        AccessTools.Method(button.GetType(),"OnPress",[]).Invoke(button,null);
        AccessTools.Method(button.GetType(),"OnRelease",[]).Invoke(button,null);
    });
    private static async Task Execute(RunOperation operation)
    {
        if(operation.Kind is "reward" or "proceed" or "act-proceed" or RunRewardPotions.Kind||RunPotionUses.AtRewards(operation))await RewardsReady(requireVisible:false);
        if(operation.Kind=="map")await Wait(()=>NMapScreen.Instance is {IsOpen:true,IsTravelEnabled:true} map&&!TweenRunning(map,"_tween")&&!TweenRunning(map,"_actAnimTween"),"run-map-not-ready");
        if(operation.Kind=="rest")await Wait(()=>NRestSiteRoom.Instance is not null,"run-rest-site-not-ready");
        await TakePhase(operation.Kind switch{"reward"=>"领取奖励",RunRewardPotions.Kind or RunRewardPotions.NonCombatKind=>"丢弃药水",RunPotionUses.Kind=>"使用药水","act-proceed"=>"前往下一幕","map"=>"地图选路","rest"=>"营火选择","treasure-open"=>"打开宝箱","treasure-pick"=>"领取遗物",
            "merchant-open"=>"打开商店","merchant-close"=>"关闭商店","merchant-buy"=>"购买商品","merchant-remove"=>"商店删牌","event-choice"=>"事件选项","event-return"=>"返回事件","abandon"=>"打开放弃确认",
            "proceed" or "rest-proceed" or "treasure-proceed" or "merchant-proceed" or "event-proceed"=>"离开房间",_=>operation.Kind});
        if(operation.Progress is {} progress)
        {
            MemorySession.ApplyRecordedProgress(progress);
            Runtime.Log("run-operation-progress-context",new{operation.Sequence,operation.SourceSequence,operation.Attempt,
                SHA256=Codec.Hash(System.Text.Encoding.UTF8.GetBytes(progress)),IsolatedManagerOnly=true,RunResourcesOrRngRestored=false});
        }
        // The retained map action predates a later room-entry Load. Apply the
        // Load's newer account context after that action's own input context,
        // before it generates the room for the selected restored attempt.
        ApplyProgressContexts(operation.Sequence);
        var eventLoad=_eventLoad;_eventLoad=null;
        Codec.Require(eventLoad is null||operation.Kind=="event-return","event-load-outside-recorded-return");
        if(eventLoad is null)CompareBefore(operation);
        switch(operation.Kind)
        {
            case "abandon":await RunAbandon.Play(operation);break;
            case "treasure-open":case "treasure-pick":case "treasure-proceed":await RunTreasures.Play(operation);break;
            case "merchant-open":case "merchant-close":case "merchant-proceed":case "merchant-buy":case "merchant-remove":
                await RunMerchants.Play(operation);break;
            case "event-choice":case "event-proceed":await RunEvents.Play(operation);break;
            case "act-proceed":await RunActs.Play();break;
            case "event-return":
                _rewardsRoom=null;
                if(eventLoad is {} load)await RunEventCombat.RestoreCompletedReturn(load.Point,load.Battle,operation);
                else await RunEventCombat.PlayReturn();break;
            case "reward":
                Codec.Require(await RunPatches.PlayReward(operation.Argument.RewardIndex??-1,operation.Argument.RewardKey!)==operation.Success,
                    "run-reward-result-mismatch");break;
            case RunRewardPotions.Kind:case RunRewardPotions.NonCombatKind:await RunRewardPotions.Play(operation);break;
            case RunPotionUses.Kind:await RunPotionUses.Play(operation);break;
            case "rest":
                Codec.Require(operation.Argument.Option is "HEAL" or "SMITH","run-rest-option-unsupported");
                var options=RunManager.Instance.RestSiteSynchronizer.GetLocalOptions();var option=options.ToList().FindIndex(o=>o.OptionId==operation.Argument.Option);
                Codec.Require(option>=0&&options[option].IsEnabled,"run-rest-option-unavailable");
                var restButton=NRestSiteRoom.Instance!.GetButtonForOption(options[option]);
                Codec.Require(restButton is not null,"run-rest-button-missing");
                await Wait(()=>restButton!.Modulate.A>=.99f,"run-rest-button-not-presented");
                RunPatches.UiTask=null;RunPatches.ResultTask=null;RunPatches.RestAnimation=null;
                ClickNative(restButton!);
                Codec.Require(RunPatches.UiTask is not null&&RunPatches.ResultTask is not null,"run-rest-native-handler-not-started");
                await Await(RunPatches.UiTask!);
                Codec.Require(RunPatches.ResultTask!.Result==operation.Success,"run-rest-result-mismatch");
                if(RunPatches.RestAnimation is {} animation)await Await(animation);break;
            case "proceed":
                await Wait(()=>((MegaCrit.Sts2.Core.Nodes.CommonUi.NProceedButton)RunPatches.Field(RunPatches.RewardsScreen!,"_proceedButton")!).IsEnabled,"run-reward-proceed-disabled");
                RunPatches.ProceedTask=null;
                Native(()=>AccessTools.Method(RunPatches.RewardsScreen!.GetType(),"OnProceedButtonPressed").Invoke(RunPatches.RewardsScreen,[null]));
                await Wait(()=>RunPatches.ProceedTask is not null,"run-native-proceed-not-started");
                await Await(RunPatches.ProceedTask!);break;
            case "rest-proceed":
                await Wait(()=>NRestSiteRoom.Instance?.ProceedButton.IsEnabled==true,"run-rest-animation-pending");
                Native(()=>AccessTools.Method(typeof(NRestSiteRoom),"OnProceedButtonReleased").Invoke(NRestSiteRoom.Instance,[null]));break;
            case "map":
                var coord=new MapCoord(operation.Argument.Col!.Value,operation.Argument.Row!.Value);var run=LiveState.Run;
                Codec.Require(run.CurrentMapPoint is {} source?MapTravel.GetTravelablePointsFrom(run,source).Any(p=>p.coord.Equals(coord)):
                    run.CurrentRoom is MapRoom&&coord.Equals(run.Map.StartingMapPoint.coord),"run-recorded-route-not-reachable");
                var point=run.Map.GetPoint(coord)??throw new InvalidDataException("run-map-point-missing");
                Codec.Require(_index+1<_run.Operations.Count,"run-next-room-recording-missing");
                var destination=RunRoutes.Destination(_run,operation,_run.Operations[_index+1]);
                Codec.Require(destination.Visit.Act==run.CurrentActIndex&&
                    (destination.Visit.ActModel is null||destination.Visit.ActModel==run.Act.Id.ToString()),"run-map-destination-act-mismatch");
                Codec.Require(point.PointType.ToString()==destination.Visit.MapType,"run-recorded-map-type-mismatch");
                PlayerLoop.ReleaseBattleForTravel();
                async Task Enter()
                {
                    RunPatches.Clear();
                    _mapRun=run;_mapDestination=coord;_mapGeneration=RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount;
                    _mapVoted=false;_mapMove=null;_mapTask=null;
                    var screen=NMapScreen.Instance??throw new InvalidDataException("run-map-screen-missing");
                    var travelPoint=ReplayMap.TravelPoint(screen,run.Map,coord);
                    try
                    {
                        Native(()=>screen.OnMapPointSelectedLocally(travelPoint));
                        await Wait(()=>_mapTask is not null,"run-native-travel-not-started");
                        // A submitted scene load must reach its real native boundary.
                        // LoadOpening retires a choice wait; Stop must not abandon a
                        // wrapper while the underlying scene task can still run.
                        Track(_mapTask!);await _mapTask!;
                        Codec.Require(RunPatches.Travel is not null,"run-native-room-entry-not-started");Track(RunPatches.Travel!);await RunPatches.Travel!;
                    }
                    finally{_mapRun=null;}
                }
                if(destination.Type is "Monster" or "Elite" or "Boss"&&LiveState.CombatMapType(point.PointType))
                {
                    Codec.Require(_index+1<_run.Operations.Count&&_run.Operations[_index+1] is {Kind:"battle",Status:"complete",Clip:not null},"run-next-battle-recording-missing");
                    var battle=_run.Operations[_index+1];var clip=Runtime.ReadClip(battle.Clip!);
                    await PlayerLoop.LoadEnteredBattle(clip,Enter);
                }
                else
                {
                    Codec.Require(point.PointType==MapPointType.Ancient&&destination.Type=="Event"&&destination.Visit.Act is 1 or 2||
                        point.PointType is MapPointType.RestSite or MapPointType.Treasure or MapPointType.Shop or MapPointType.Unknown&&
                        destination.Type is "RestSite" or "Treasure" or "Shop" or "Event","run-next-room-unsupported");await Enter();
                }
                Codec.Require(run.CurrentRoom?.RoomType.ToString()==destination.Type&&run.CurrentRoom?.ModelId?.ToString()==destination.Model,
                    "run-generated-room-type-or-model-mismatch");
                if(CacheableRoomDestination&&RoomEntryKey(operation) is {} key)RunRoomEntryCache.Complete(operation,_run.Operations[_index+1],key);
                Runtime.Log("run-native-room-verified",new{destination.Visit.Id,destination.Visit.MapType,ActualRoomType=destination.Type,
                    destination.Model,NativeEntryExecuted=true,RoomTypeForced=false});
                ReplayMap.Arrived();
                break;
            default:throw new InvalidDataException("run-operation-unsupported-"+operation.Kind);
        }
    }
    internal static bool TweenRunning(object owner,string name)=>RunPatches.Field(owner,name) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning();
    internal static async Task RewardsReady(bool requireVisible)
    {
        await Wait(()=>RunPatches.Rewards is not null&&RunPatches.RewardsScreen is {} screen&&GodotObject.IsInstanceValid(screen)&&screen.IsNodeReady()&&screen.IsVisibleInTree()&&
            ReferenceEquals(NOverlayStack.Instance?.Peek(),screen)&&!TweenRunning(screen,"_fadeTween")&&
            ((!requireVisible&&screen.IsComplete)||((Control)RunPatches.Field(screen,"_rewardsWindow")!).Modulate.A>=.99f),"run-reward-screen-not-presented");
        Runtime.Log("run-rewards-ready",new{Operation=Current?.Sequence,Kind=Current?.Kind,RequireVisibleOffer=requireVisible,
            RunPatches.RewardsScreen!.IsComplete,Rewards=RunPatches.Rewards!.Rewards.Count,
            NativeCompletedStateAccepted=!requireVisible&&RunPatches.RewardsScreen.IsComplete});
        // Node readiness and tween completion can precede this frame's draw.
        await Runtime.NextFrame();CheckStop();await Runtime.NextFrame();
    }
    internal static bool MapAction(GameAction action)
    {
        if(!Active||action is not (VoteForMapCoordAction or MoveToMapCoordAction))return false;
        if(_stop||PlayerLoop.TearingDown)return false;
        Codec.Require(_mapRun is not null&&ReferenceEquals(_mapRun,LiveState.Run)&&action.OwnerId==LiveState.Player.NetId&&
            RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount==_mapGeneration,"run-map-action-outside-recorded-transition");
        if(action.ToNetAction() is NetVoteForMapCoordAction vote)
        {
            Codec.Require(Driving&&!_mapVoted&&vote.source==_mapRun!.MapLocation&&vote.destination is {} target&&target.coord.Equals(_mapDestination)&&target.mapGenerationCount==_mapGeneration,"run-map-vote-mismatch");
            _mapVoted=true;
        }
        else if(action.ToNetAction() is NetMoveToMapCoordAction move)
        {
            Codec.Require(_mapVoted&&_mapMove is null&&move.destination.Equals(_mapDestination),"run-map-move-mismatch");
            _mapMove=(MoveToMapCoordAction)action;
        }
        Runtime.Log("run-native-map-action-owned",new{Operation=Current?.Sequence,Action=action.GetType().Name,Generation=_mapGeneration});return true;
    }
    internal static void MapTask(MoveToMapCoordAction action,Task task)
    {
        if(!Active||!ReferenceEquals(action,_mapMove))return;
        _mapTask=task;Track(task);
        if(MapStopTestArmed&&PlayerLoop.Loading&&NativeSeek.Opening)
            Track(StopDuringMap(task));
    }
    private static async Task StopDuringMap(Task move)
    {
        await Runtime.NextFrame();
        if(!MapStopTestArmed||!Active||Stopping||!ReferenceEquals(_mapTask,move)||move.IsCompleted||RunPatches.Travel is not null)return;
        MapStopTestArmed=false;
        Runtime.Log("run-map-stop-test-request",new{Operation=Current?.Sequence,MoveInFlight=true,RoomEntryStarted=false});
        PlayerLoop.Control("stop");
    }
}

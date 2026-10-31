using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal enum TestPreparation { None, DualEnemies, Content, Choices, ChoiceProcess, SilentContent, RegentContent, QueuedCards, SharedContent, DefectContent, NecrobinderContent, EnemySummon, RunRoute, RunRooms, RunShops }
// Explicit opt-in, consumed by ONE new supported run. Never applied to Continue or playback.
internal static class TestEncounter
{
    internal static TestPreparation Armed;
    internal static bool FirstOstySummonArmed;
    private static bool _firstOstySummon;
    private static TestPreparation _mode;
    private static RunState? _run;
    internal static string? Preparation=>_run is not null&&ReferenceEquals(_run,RunManager.Instance.DebugOnlyGetState())?
        (_mode==TestPreparation.RunShops?"RUN002-Ironclad-Underdocks-two-CorpseSlugsWeak-40HP-1000Gold-3-starter-DefendPlus-2-added-BodySlamPlus-5-innate-retain-connected-Monster-Shop-Monster":
         _mode==TestPreparation.RunRooms?"RUN002-Ironclad-Underdocks-two-CorpseSlugsWeak-40HP-3-starter-DefendPlus-2-added-BodySlamPlus-5-innate-retain-connected-Monster-Treasure-Monster":
         _mode==TestPreparation.RunRoute?"RUN001-Ironclad-Underdocks-two-CorpseSlugsWeak-40HP-3-starter-DefendPlus-2-added-BodySlamPlus-5-innate-retain-connected-Monster-RestSite-Monster":
         _mode==TestPreparation.EnemySummon?"CONTENT006-D-Ironclad-Underdocks-TwoTailedRatsNormal-3-starter-DefendPlus-2-added-BodySlamPlus-5-innate-retain":
         _mode==TestPreparation.NecrobinderContent?(_firstOstySummon?
            "CONTENT005-C-Necrobinder-first-Osty-summon-no-BoundPhylactery-3-Bodyguard-BoneShards-DeathbringerPlus-6-innate-retain-BagOfPreparation-EnergyPotion":
            "CONTENT005-C-Necrobinder-starter10-BoundPhylactery-Bodyguard-BoneShards-DeathbringerPlus-5-innate-retain-BagOfPreparation-EnergyPotion"):
         _mode==TestPreparation.DefectContent?"CONTENT005-B-Defect-starter10-CrackedCore-Darkness-ColdSnap-Capacitor-ChaosPlus-6-innate-retain-BagOfPreparation-FocusPotion":
         _mode==TestPreparation.SharedContent?"CONTENT005-A-Regent-Toolbox-StarPotion-KingsCourage-discard-EnergyPotion":
         _mode==TestPreparation.QueuedCards?"PLAYBACK001-Ironclad-starters-three-native-Defend-innate-retain-CorpseSlugsWeak":
         _mode==TestPreparation.RegentContent?"CONTENT004-Regent-starter10-DivineRight-RefineBlade-DualWield-Enlightenment-4-innate-retain-StarPotion-KingsCourage":
         _mode==TestPreparation.SilentContent?"CONTENT003-Silent-starter12-RingOfTheSnake-Burst-BladeDance-3-innate-retain-CorpseSlugsWeak":
         _mode==TestPreparation.ChoiceProcess?"CONTENT002-native-choice-process-long-piles-SeekerStrike-innate-retain":
         _mode==TestPreparation.Choices?"CONTENT001-real-choices-Toolbox-Trade-Tyranny-Entropy-Burst-Survivor-PreparedPlus-GritPlus-Joss4":
            _mode==TestPreparation.Content?"DEMO005-directed-CorpseSlugs-ShrugPlus-Inflame-HappyFlower-EnergyPotion":"DEMO004-directed-Underdocks-CorpseSlugsWeak"):null;
    internal static void BeforeNew(RunState state)
    {
        _run=null;_mode=Armed;_firstOstySummon=_mode==TestPreparation.NecrobinderContent&&FirstOstySummonArmed;FirstOstySummonArmed=false;
        if(_mode==TestPreparation.None||PlayerLoop.Active)return;Armed=TestPreparation.None;
        Runtime.CheckEnvironment();
        Codec.Require(state.CurrentActIndex==0&&state.AscensionLevel==0&&state.Players.Count==1&&
            state.Players[0].Character.Id.ToString()==(_mode==TestPreparation.NecrobinderContent?"CHARACTER.NECROBINDER":_mode==TestPreparation.DefectContent?"CHARACTER.DEFECT":_mode is TestPreparation.RegentContent or TestPreparation.SharedContent?"CHARACTER.REGENT":_mode==TestPreparation.SilentContent?"CHARACTER.SILENT":"CHARACTER.IRONCLAD")&&state.Modifiers.Count==0,"test-entry-character-or-A0-mismatch");
        state.SetActDebug(ModelDb.Act<Underdocks>().ToMutable());_run=state;
    }
    internal static void AfterNew()
    {
        if(Preparation is null)return;
        var rooms=(RoomSet)AccessTools.Field(typeof(ActModel),"_rooms").GetValue(_run!.Act)!;
        Codec.Require(rooms.normalEncountersVisited==0&&rooms.normalEncounters.Count>0,"test-entry-room-list-not-ready");
        var previous=rooms.normalEncounters[0].Id.ToString();rooms.normalEncounters[0]=_mode==TestPreparation.EnemySummon?ModelDb.Encounter<TwoTailedRatsNormal>():ModelDb.Encounter<CorpseSlugsWeak>();
        if(_mode is TestPreparation.EnemySummon or TestPreparation.RunRoute or TestPreparation.RunRooms or TestPreparation.RunShops)
        {
            var player=_run.Players[0];var defense=player.Deck.Cards.OfType<DefendIronclad>().Take(3).Cast<CardModel>().ToArray();
            Codec.Require(defense.Length==3&&defense.All(c=>c.CurrentUpgradeLevel==0),"rat-test-starter-defends");
            var attacks=Enumerable.Range(0,2).Select(_=>_run.CreateCard<BodySlam>(player)).ToArray();
            foreach(var card in attacks)player.Deck.AddInternal(card,silent:true);
            foreach(var card in defense.Concat(attacks))
            {
                card.UpgradeInternal();var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
                card.EnchantInternal(enchantment,1);enchantment.ModifyCard();
            }
            Codec.Require(player.Deck.Cards.All(c=>ReferenceEquals(c.Owner,player)&&_run.ContainsCard(c)),
                "rat-test-deck-owner-or-run-registration");
            if(_mode is TestPreparation.RunRoute or TestPreparation.RunRooms or TestPreparation.RunShops)
            {Codec.Require(rooms.normalEncounters.Count>=2,"run-test-second-encounter-missing");rooms.normalEncounters[1]=ModelDb.Encounter<CorpseSlugsWeak>();player.Creature.SetCurrentHpInternal(40);}
            if(_mode==TestPreparation.RunShops)player.Gold=1000;
        }
        if(_mode==TestPreparation.NecrobinderContent)
        {
            var player=_run.Players[0];
            Codec.Require(player.Deck.Cards.Count==10&&player.Relics.Any(r=>r is BoundPhylactery),"necrobinder-starter-preparation-shape");
            var guard=player.Deck.Cards.Single(c=>c is Bodyguard);var unleash=player.Deck.Cards.Single(c=>c is Unleash);
            var extraGuard=_run.CreateCard<Bodyguard>(player);var shards=_run.CreateCard<BoneShards>(player);var doom=_run.CreateCard<Deathbringer>(player);
            doom.UpgradeInternal();doom.FinalizeUpgradeInternal();
            foreach(var card in new CardModel[]{extraGuard,shards,doom})player.Deck.AddInternal(card,silent:true);
            var guaranteed=new List<CardModel>{guard,unleash,extraGuard,shards,doom};
            if(_firstOstySummon)
            {
                player.RemoveRelicInternal(player.Relics.Single(r=>r is BoundPhylactery),silent:true);
                var third=_run.CreateCard<Bodyguard>(player);player.Deck.AddInternal(third,silent:true);guaranteed.Add(third);
            }
            foreach(var card in guaranteed)
            {
                var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
                card.EnchantInternal(enchantment,1);enchantment.ModifyCard();
            }
            player.AddRelicInternal(ModelDb.Relic<BagOfPreparation>().ToMutable(),silent:true);
            Codec.Require(player.AddPotionInternal(ModelDb.Potion<EnergyPotion>().ToMutable(),0,silent:true).success,"necrobinder-test-energy-potion");
        }
        if(_mode==TestPreparation.DefectContent)
        {
            var player=_run.Players[0];
            Codec.Require(player.Deck.Cards.Count==10&&player.Relics.Any(r=>r is CrackedCore),"defect-starter-preparation-shape");
            var darkness=_run.CreateCard<Darkness>(player);var cold=_run.CreateCard<ColdSnap>(player);
            var capacitor=_run.CreateCard<Capacitor>(player);var chaos=_run.CreateCard<Chaos>(player);
            chaos.UpgradeInternal();chaos.FinalizeUpgradeInternal();
            var zap=player.Deck.Cards.Single(c=>c is Zap);zap.UpgradeInternal();zap.FinalizeUpgradeInternal();
            foreach(var card in new CardModel[]{darkness,cold,capacitor,chaos})player.Deck.AddInternal(card,silent:true);
            foreach(var card in new[]{darkness,cold,capacitor,chaos,zap,player.Deck.Cards.Single(c=>c is Dualcast)})
            {
                var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
                card.EnchantInternal(enchantment,1);enchantment.ModifyCard();
            }
            player.AddRelicInternal(ModelDb.Relic<BagOfPreparation>().ToMutable(),silent:true);
            Codec.Require(player.AddPotionInternal(ModelDb.Potion<FocusPotion>().ToMutable(),0,silent:true).success,"defect-test-focus-potion");
        }
        if(_mode==TestPreparation.QueuedCards)
        {
            var player=_run.Players[0];var cards=player.Deck.Cards.OfType<DefendIronclad>().Take(3).ToArray();
            Codec.Require(cards.Length==3,"queued-test-needs-three-starter-defends");
            foreach(var card in cards)
            {
                var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
                card.EnchantInternal(enchantment,1);enchantment.ModifyCard();
            }
        }
        if(_mode is TestPreparation.RegentContent or TestPreparation.SharedContent)
        {
            var player=_run.Players[0];
            Codec.Require(player.Deck.Cards.Count==10&&player.Relics.Any(r=>r is DivineRight),"regent-starter-preparation-shape");
            var refine=_run.CreateCard<RefineBlade>(player);var dual=_run.CreateCard<DualWield>(player);var light=_run.CreateCard<Enlightenment>(player);
            foreach(var card in new CardModel[]{refine,dual,light})player.Deck.AddInternal(card,silent:true);
            foreach(var card in new CardModel[]{refine,dual,light,player.Deck.Cards.Single(c=>c is FallingStar)})
            {
                var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
                card.EnchantInternal(enchantment,1);enchantment.ModifyCard();
            }
            Codec.Require(player.AddPotionInternal(ModelDb.Potion<StarPotion>().ToMutable(),0,silent:true).success&&
                player.AddPotionInternal(ModelDb.Potion<KingsCourage>().ToMutable(),1,silent:true).success,"regent-test-potions");
            if(_mode==TestPreparation.SharedContent)
            {
                player.AddRelicInternal(ModelDb.Relic<Toolbox>().ToMutable(),silent:true);
                Codec.Require(player.AddPotionInternal(ModelDb.Potion<EnergyPotion>().ToMutable(),2,silent:true).success,"shared-test-discard-potion");
            }
        }
        if(_mode==TestPreparation.SilentContent)
        {
            var player=_run.Players[0];
            Codec.Require(player.Deck.Cards.Count==12&&player.Deck.Cards.Count(c=>c is Survivor)==1&&
                player.Relics.Any(r=>r is RingOfTheSnake),"silent-starter-preparation-shape");
            var burst=_run.CreateCard<Burst>(player);var dance=_run.CreateCard<BladeDance>(player);
            player.Deck.AddInternal(burst,silent:true);player.Deck.AddInternal(dance,silent:true);
            // Native, serializable enchantment: guarantee only the three mechanism cards.
            // Preserve every starter and the native starter relic; no opening-hand or playback injection.
            foreach(var card in new[]{burst,dance,player.Deck.Cards.Single(c=>c is Survivor)})
            {
                var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
                card.EnchantInternal(enchantment,1);enchantment.ModifyCard();
            }
        }
        if(_mode==TestPreparation.Content)
        {
            var player=_run.Players[0];var shrug=_run.CreateCard<ShrugItOff>(player);
            shrug.UpgradeInternal();shrug.FinalizeUpgradeInternal();player.Deck.AddInternal(shrug,silent:true);
            player.Deck.AddInternal(_run.CreateCard<Inflame>(player),silent:true);
            player.AddRelicInternal(ModelDb.Relic<HappyFlower>().ToMutable(),silent:true);
            var potion=player.AddPotionInternal(ModelDb.Potion<EnergyPotion>().ToMutable(),0,silent:true);
            Codec.Require(potion.success,"test-content-potion-preparation-failed");
        }
        if(_mode==TestPreparation.Choices)
        {
            var player=_run.Players[0];
            foreach(var old in player.Deck.Cards.ToArray()){player.Deck.RemoveInternal(old,silent:true);_run.RemoveCard(old);}
            void Add<T>(bool upgrade=false) where T:CardModel
            {
                var card=_run!.CreateCard<T>(player);if(upgrade){card.UpgradeInternal();card.FinalizeUpgradeInternal();}player.Deck.AddInternal(card,silent:true);
            }
            Add<ToolsOfTheTrade>(true);Add<Tyranny>();Add<Entropy>();Add<Burst>();Add<Survivor>();
            Add<TrueGrit>(true);Add<Prepared>(true);Add<Discovery>();Add<Bludgeon>(true);
            player.AddRelicInternal(ModelDb.Relic<Toolbox>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<BagOfPreparation>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<RingOfTheSnake>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<Lantern>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<Anchor>().ToMutable(),silent:true);
            var paper=(JossPaper)ModelDb.Relic<JossPaper>().ToMutable();paper.CardsExhausted=4;player.AddRelicInternal(paper,silent:true);
            Codec.Require(player.AddPotionInternal(ModelDb.Potion<AttackPotion>().ToMutable(),0,silent:true).success&&
                player.AddPotionInternal(ModelDb.Potion<EnergyPotion>().ToMutable(),1,silent:true).success,"test-choice-potions-preparation-failed");
        }
        if(_mode==TestPreparation.ChoiceProcess)
        {
            var player=_run.Players[0];
            foreach(var old in player.Deck.Cards.ToArray()){player.Deck.RemoveInternal(old,silent:true);_run.RemoveCard(old);}
            void Add<T>(bool innate=false,bool retain=false,bool upgrade=false) where T:CardModel
            {
                var card=_run!.CreateCard<T>(player);if(upgrade){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
                if(innate||retain)
                {
                    Codec.Require(innate&&retain,"test-process-keyword-pair");
                    var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
                    card.EnchantInternal(enchantment,1);enchantment.ModifyCard();
                }
                player.Deck.AddInternal(card,silent:true);
            }
            // Real, serialized keyword modifications guarantee the mechanism cards without playback injection.
            Add<SecretWeapon>(true,true);Add<SeekerStrike>(true,true);Add<Hologram>(true,true,true);
            Add<Prepared>(true,true,true);Add<Discovery>(true,true);Add<Bludgeon>(true,true,true);
            for(var i=0;i<28;i++)Add<StrikeIronclad>();
            player.AddRelicInternal(ModelDb.Relic<Toolbox>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<BagOfPreparation>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<RingOfTheSnake>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<Lantern>().ToMutable(),silent:true);
            player.AddRelicInternal(ModelDb.Relic<Anchor>().ToMutable(),silent:true);
            Codec.Require(player.AddPotionInternal(ModelDb.Potion<GamblersBrew>().ToMutable(),0,silent:true).success&&
                player.AddPotionInternal(ModelDb.Potion<AttackPotion>().ToMutable(),1,silent:true).success&&
                player.AddPotionInternal(ModelDb.Potion<EnergyPotion>().ToMutable(),2,silent:true).success,"test-process-potions-preparation-failed");
        }
        Runtime.Log("test-encounter-prepared",new{Preparation,PreviousFirstEncounter=previous,ActualFirstEncounter=rooms.normalEncounters[0].Id.ToString(),
            SerializedBeforeCheckpoint=true,NormalEntryUnchangedWhenUnarmed=true,Character=_run.Players[0].Character.Id.ToString(),
            DeckOwnersAndRunRegistrationValid=_run.Players[0].Deck.Cards.All(c=>ReferenceEquals(c.Owner,_run.Players[0])&&_run.ContainsCard(c)),
            Deck=_run.Players[0].Deck.Cards.Select(c=>new{Model=c.Id.ToString(),c.CurrentUpgradeLevel,Saved=c.ToSerializable()}).ToArray(),
            Relics=_run.Players[0].Relics.Select(r=>r.Id.ToString()).ToArray(),Potions=_run.Players[0].PotionSlots.Select(p=>p?.Id.ToString()).ToArray()});
        Runtime.Status=_mode==TestPreparation.RunShops?"商店SL短路线已准备：首普通→商店→普通；初始1000金币；保留每次尝试":
            _mode==TestPreparation.RunRooms?"宝箱SL短路线已准备：首普通→宝箱→普通；保留每次尝试":
            _mode==TestPreparation.RunRoute?"短路线已准备：首普通→营火→普通；正常领奖，沿指定连接节点前进":
            _mode==TestPreparation.EnemySummon?"三鼠召援测试已准备：原牌组中三防御升级，添加两张全身撞击+，五牌先天保留；先防御等原生召援，再攻击新怪":
            _mode==TestPreparation.NecrobinderContent?"亡灵契约师已准备：保留原10张／原遗物；两张保镖／释放／碎骨／死亡使者+先天保留；能量药水，首战双蛞蝓":
            _mode==TestPreparation.DefectContent?"故障机器人已准备：保留原10张／破损核心；六张先天保留；暗球／冰球／扩槽／随机球／集中药水，首战双蛞蝓":
            _mode==TestPreparation.SharedContent?"共用测试已准备：储君／工具箱初始选择／星星与锻造／第3槽能量药水供丢弃；首战双蛞蝓":
            _mode==TestPreparation.QueuedCards?"排队测试已准备：铁甲原牌组，三张防御先天保留，首战双蛞蝓；快速连续打两张防御":
            _mode==TestPreparation.RegentContent?"储君测试已准备：原10张起始牌／王权；精炼／双持／启蒙／陨落之星先天保留；星星与锻造药水；双蛞蝓":
            _mode==TestPreparation.SilentContent?"猎人测试已准备：保留12张起始牌和蛇之戒；爆发／生存者／刀刃之舞先天保留；暗港双蛞蝓":
            _mode==TestPreparation.ChoiceProcess?"过程测试已准备：原生工具箱／手牌／长抽牌堆与弃牌堆／探寻打击，六张先天保留牌（真实新局）":
            _mode==TestPreparation.Choices?"选择测试已准备：双蛞蝓／工具箱／九张机制牌／金纸4／攻击和能量药水（真实新局）":
            _mode==TestPreparation.Content?"测试准备：双蛞蝓＋耸肩无视+／燃烧／开心小花／能量药水（真实新局）":"测试准备已应用：暗港首战双蛞蝓（真实新局，会正常保存进度）";
    }
    internal static void ConfigureRunRoute(MegaCrit.Sts2.Core.Map.ActMap map)
    {
        if(PlayerLoop.Active||_mode is not (TestPreparation.RunRoute or TestPreparation.RunRooms or TestPreparation.RunShops)||Preparation is null)return;
        var first=map.GetPointsInRow(1).Where(p=>p.PointType==MegaCrit.Sts2.Core.Map.MapPointType.Monster).OrderBy(p=>p.coord.col).First();
        var camp=first.Children.OrderBy(p=>p.coord.col).First();var second=camp.Children.OrderBy(p=>p.coord.col).First();
        camp.PointType=_mode switch{TestPreparation.RunRooms=>MegaCrit.Sts2.Core.Map.MapPointType.Treasure,
            TestPreparation.RunShops=>MegaCrit.Sts2.Core.Map.MapPointType.Shop,_=>MegaCrit.Sts2.Core.Map.MapPointType.RestSite};
        second.PointType=MegaCrit.Sts2.Core.Map.MapPointType.Monster;
        Runtime.Log("test-run-route-prepared",new{NativeConnectedNodes=true,BeforeRecordingCheckpoint=true,
            Route=new[]{first,camp,second}.Select(p=>new{Row=p.coord.row,Col=p.coord.col,Type=p.PointType.ToString()}).ToArray()});
        var middle=_mode switch{TestPreparation.RunRooms=>"宝箱",TestPreparation.RunShops=>"商店",_=>"营火"};
        Runtime.Status=$"短路线：第{first.coord.row+1}层第{first.coord.col+1}列普通 → 第{camp.coord.row+1}层第{camp.coord.col+1}列{middle} → 第{second.coord.row+1}层第{second.coord.col+1}列普通";
    }
}

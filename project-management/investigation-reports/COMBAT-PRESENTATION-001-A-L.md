# COMBAT-PRESENTATION-001 · A–L 原生战斗演出静态审计

固定 v0.111.0，`sts2.dll` SHA-256 前缀 `0861BFA1`。范围为 `Models.Monsters` A–L 的 60 个文件，另追调用到遭遇、能力、命令和场景脚本。表中 `类名:行号` 指向本机固定版本反编译目录，场景脚本归属以同版本 PCK 内的 `scenes/creature_visuals` 场景为证。已知实机证据单列。其余判断均为静态发现。

## 判定口径

- **通用**指 `MonsterModel.PerformMove`、`NCreature.PerformIntent`、`CreatureCmd.TriggerAnim`、`AttackCommand.Execute`、伤害和格挡等路径中，Replay 已审核的直属计时点。仅在当前房间、动作和代次匹配且正在撤销时释放计时。原生动作、信号、牌选、伤害、复活、生成和钩子继续执行。一个招式走通用计时仍可能有独立演出尾巴。
- **待审计时**指源码中找到的额外计时，当前 `NativePhaseRetirement.PlainWaitAfter` 或 `WaitAfter` 未绑定该直接调用方。纯演出定时器可作为候选，压缩的前提是复核相邻游戏提交、取消条件以及画面所有者。遇到有真实业务的 `await` 继续等待其正常提交。
- **独立**指 Spine 信号、场景挂载、音频句柄、异步任务或 tween 拥有自己的寿命。当前覆盖包括 `VfxCmd.PlayVfx` 生成的临时节点、Attack 回调工厂、意图粒子、已登记的专用发射器，以及镜头和音频的部分入口。普通 FMOD 单次音在撤销中可被静音，已开始的音只有指定事件保留了独立停止句柄。挂在模型或背景上的长期状态需要分别核对。
- 除表中标明的局部证据，所有条目的首次正放、演出中 Back、原生重做和实际听感均待运行确认。表里“未见专用”表示在现有 Replay 入口中未找到明确覆盖，源码事实不能直接判定玩家可见故障。

Replay 对照位置见 [NativePhaseRetirement.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/NativePhaseRetirement.cs:69)、[NativeDisplayTrace.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/NativeDisplayTrace.cs:272)、[CreatureHudTrace.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/CreatureHudTrace.cs:73)、[CreaturePresentation.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/CreaturePresentation.cs:143)、[NativeSeek.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/NativeSeek.cs:435)。

## A–B（17 个文件）

| 模型与入口 | 原生演出与提交 | Replay 现状与核查重点 |
| --- | --- | --- |
| Aeonglass · AeonglassBoss | `EbbMove` 的重击后格挡，`EyeLasersMove` 双击，`IncreasingIntensityMove` 的施法后升级现有 Wither、生成并预览 Wither、加力量。入场给 Withering Presence 和人工制品，`queen_progress` 入场 1、死亡 5。WitheringPresencePower 每达到计数，等待 0.5 s 后生成入手并重置计数。见 `Aeonglass:80,118,135,158`、`WitheringPresencePower:46`。 | 通用攻击和生成。能力的直属 0.5 s 待审，保留计数、造牌和入手。场景 `NAeonGlassVfx:208,214,273,314` 持有第二轨圆环、凋萎／碎片／火花／擦地粒子。需核 Back 对第二轨与粒子的恢复，胜利 Back 的专曲重启还需核 `queen_progress`。 |
| Architect · TheArchitectEventEncounter | `Architect:17` 的隐藏意图只完成空任务。终局从 TheArchitect 事件对话驱动，`NArchitectVfx:86` 有头部第二轨和 `NBasicTrail`。 | 终局演出模型，归 RUN-005 的事件和终局边界，排除敌方回合验收。 |
| AssassinRubyRaider · RubyRaidersNormal | `KillshotMove:32` 的 Attack 0.5 s、斩击与原生 AttackSfx。 | 通用，核斩击与已启动音效。 |
| Axebot · AxebotsNormal、StockPower | `BootUpMove:110` 磨刀、音效，直属 0.25 s 后格挡和按重生次数给力量。双击和上勾拳走 Attack，后者施加虚弱、脆弱。`StockPower.AfterDeath:17` await 生成同槽位新机器人，先隐藏其场景，随后 `RunSafely` 启动 `RevealReplacementAfterDeathAnim:30`，按旧节点死亡时长等待再显形、播 respawn。 | 约 298 ms 慢样本。BootUp 的额外 0.25 s 待审。格式 10 有通用生成身份登记，不能代替显形任务的代次归属。已开始的 `NAxebotVfx:129–184` 受击、死亡与着地粒子需核。生成提交与新节点显形分别验证。 |
| AxeRubyRaider · RubyRaidersNormal | `SwingMove:47` 斩击后格挡，`BigSwingMove:56` 重攻击，均用原生 Attack 动画和 SFX。 | 通用。 |
| BattleFriendV1 · BattlewornDummyEventV1Encounter | `BattleFriendV1:26,33,38` 空行动，入场限时 3，受击木屑、死亡后驻留死循环。`BattlewornDummyTimeLimitPower:19` 敌方回合结束扣计数，耗尽时逃跑。 | 可达事件战斗。核时间到和击杀两种结算以及回退后模型。木屑由 `NBattlewornDummyVfx:65–76` Spine 信号发射，未见专用收束。 |
| BattleFriendV2 · BattlewornDummyEventV2Encounter | `BattleFriendV2:26,33,38` 同 V1，第二档使用自身 HP 和事件奖励。 | 共享 V1 的路径，保留第二档成功返回父事件的已有代表证据。 |
| BattleFriendV3 · BattlewornDummyEventV3Encounter | `BattleFriendV3:26,33,38` 同 V1，第三档对应自己的遭遇及事件结算。 | 共用演出，单列结果与档位。 |
| BigDummy · Mocks | `BigDummy:21–34` 隐藏空行动。调用来源是 MockBoss／Elite／Monster／NoRewards／TwoMonsterEncounter 等测试模型。 | 排除标准可达敌人。 |
| BowlbugEgg · BowlbugsWeak／Normal | `BiteMove:65` 咬击后格挡，单次专用攻击和死亡 FMOD，入场回调只 await 基类。 | 通用，音尾按共享单次音归类。 |
| BowlbugNectar · BowlbugsWeak／Normal | `ThrashMove:58` 吐击音＋钝击，`BuffMove:66` 增益动画 0.6 s 后加力量，专用死亡音。 | 通用，核增益与死亡声音。 |
| BowlbugRock · BowlbugsWeak／Normal、SlumberingBeetleNormal | `HeadbuttMove:84` 头撞后按失衡进入 `Stun:96`，先专用音和 0.6 s 动画再改招式。`DizzyMove:103` 清失衡并唤醒。ImbalancedPower 在击打结果回调也可置眩晕。 | 眩晕业务继续提交。核格挡前后触发的失衡、眩晕姿势与转回原意图，已启的 stun 音需单独听。 |
| BowlbugSilk · BowlbugsNormal、SlumberingBeetleNormal | `ThrashMove:52` 双击，`SpitMove:62–85` 根据目标改 `SpineBoneNode` 坐标，再施法和施加虚弱。 | 通用施法和命中。目标节点的局部位移不在通用身体位置快照中，验证不同目标和回退后的吐丝尾巴。 |
| BruteRubyRaider · RubyRaidersNormal | `BeatMove:40` 钝击，`RoarMove:48` 施法音及 0.6 s 动画后加力量。 | 通用，核吼声与增益。 |
| BygoneEffigy · BygoneEffigyElite | `InitialSleepMove:58` 思考后 0.5 s；空睡眠；`WakeMove:70` 精英二阶段音乐、力量、对白后 0.5 s；`SlashMove:82–108` 骨骼目标位置、径向模糊、斩击后 0.25 s。 | 三段纯演出等待已有定点绑定，气泡和径向模糊已有共用清理，精英音乐阶段有已测代表。另核 Slash 的目标局部坐标与已开始 SFX。 |
| Byrdonis · ByrdonisElite | `PeckMove:52` 多啄，`SwoopMove:61` 俯冲；`TerritorialPower.AfterSideTurnEnd:21` 才加力量，入场挂 Territorial。 | 通用，力量是回合钩子提交。 |
| Byrdpip · 玩家宠物 | `Models.Relics.Byrdpip:71` 通过 `PlayerCmd.AddPet` 给玩家，`Models.Monsters.Byrdpip` 是宠物的模型。 | 排除敌方演出矩阵。现行录播宠物边界应另行评估。 |

## C–D（14 个文件）

| 模型与入口 | 原生演出与提交 | Replay 现状与核查重点 |
| --- | --- | --- |
| CalcifiedCultist · CultistsNormal、SeapunkNormal | `IncantationMove:77` 施法音、对白后直属 `CustomScaledWait(.25,.5)`，再加 Ritual。`DarkStrikeMove:86` 伤害前 await `PlayAttackSfx:112`，音效的 `enemy_strength` 随逻辑计数增加。Ritual 的力量在回合结束钩子提交。 | 直属气泡节奏待审，气泡有登记。攻击音参数和计数需区分，保留计数变化，已播放的参数化 FMOD 未见停止句柄。 |
| CeremonialBeast · CeremonialBeastBoss | `StampMove:170` 动画后另有标准模式 0.4 s。`PlowMove:178` 四段间隔夹方向线、爆线、径向模糊、屏震、伤害、hit-stop 及后续力量。`SetStunned:208` 由 `PlowPower.AfterDamageReceived:28` 等待，清力量和设置眩晕分支。`BeastCryMove:222` 等 0.3／0.75 s 后施加 Ringing，另有 Stomp／Crush 的命中特效回调。`RingingPower:22–64` 对在场及新入场手牌挂附着并在回合结束清除。 | 已实测冲撞 Back；四段计时、两种线条、镜头和两条冲撞音已有专项。Stamp 与 BeastCry 直属等待待审，保留眩晕、附着及加力量提交。`NCeremonialBeastVfx:140–197` 自有持续能量和死亡 CPU 粒子，死亡粒子 `Finished` 经独立任务接死亡延迟。要对照尾声归属，勿对已启动信号一刀切。 |
| Chomper · ChompersNormal、TunnelerNormal | `ClampMove:67` 双击，`ScreechMove:76` 对白／施法音、1 s 动画后生成 3 张 Dazed；入场人工制品。 | 通用，确认预览及弃牌提交在同一步，核对白和音尾。 |
| CorpseSlug · CorpseSlugsWeak／Normal | `WhipSlapMove:111` 多击、`GlompMove:121` 重击、`GoopMove:129` 施加脆弱。另一只死亡时 `RavenousPower.AfterDeath:24–33` 开吞食音和动画，await 眩晕和力量提交，后续 `StunnedMove:37` 结束吞食。 | 有实机同伴死亡与力量呈现修复历史。单次音和两只蛞蝓的姿势交叠仍按触发点核查。 |
| CrossbowRubyRaider · RubyRaidersNormal | `FireMove:56` 射击斩击，`ReloadMove:65` 专用声和动画后格挡，原生满弹和空弹各有 idle、受击与死亡态。 | 通用，Back 核不同原生循环和装弹音。 |
| Crusher · KaiserCrabBoss 左钳 | `Crusher:120–145` 受击和死亡直接驱动共享 `NKaiserCrabBossBackground`。先死或最后死分别设 `kaiser_crab_progress` 2／5。`Thrash／BugSting／Adapt／EnlargingStrike／GuardedStrike:150–192` await 背景左臂演出 1／.5／.8／.65 s 后才结算伤害或增益。右钳死亡时 `CrabRagePower.AfterDeath:31` 补力量和格挡。 | 优先跟 M–Z 的右钳合并。背景 `PlayAttackAnim:132–141` 用带 token 的 Wait，通用无 token 白名单不会释放。左右臂、身体和反应分轨，背景粒子由 `NKaiserCrabBossVfx:194–202,318–373` 所有。需处理背景轨与音乐进度，普通生物模型快照只覆盖一部分。 |
| CubexConstruct · CubexConstructNormal、ConstructMenagerieNormal | `ChargeUpMove:129` 开生物绑定蓄力 loop 后等 .75 s；`RepeaterBlastMove:139` 先改 FMOD loop 参数、等 .4 s、伤害，再改参数、等 .2 s 后力量和 AttackEnd；`ExpelBlastMove:152` 双击同类节奏。`CubexConstruct:95–108` 离场停止 loop，受击改 enemy_hurt。 | 额外间隔均待审，不能切断 phase 与业务。FMOD loop 需要按恢复目标区分蓄力前后，普通单次音管理无法覆盖。`NCubexConstructVfx:98–132` Spine 驱动可复用 laser／rings 与自己的 tween，缺少同类专项退役。 |
| DampCultist · CultistsNormal | `IncantationMove:77` 采用 0.45 s 施法和本怪对白／音色，另有同 CalcifiedCultist 的 .25／.5 s。暗击同一参数化音效模式。 | 共用 CalcifiedCultist 的待审计时，保留各自音色与计数。 |
| DecimillipedeSegment · 三节抽象基类 | `AnimSegmentsAttack:222` 依次触发各节后台 driver、挂落石，然后 await 0.5 s 才继续各招伤害和后续力量／虚弱。`NDecimillipedeRocksVfx:56–79` 独立任务用 Chaotic RNG 和 `Task.Delay` 分批启动落石，再等待 5 s 释放，场景退出会取消。 `ReattachMove:212` await `ReattachPower.DoReattach:33` 恢复交互和 HP。该 Power 在本节死亡后留队设 DEAD／REATTACH，三节皆亡才启动集体 fade。 | 053 已实测部分击倒、待复活、胜利 Back 及重做，复活实际动画和多节攻击残留仍待验。攻击 0.5 s 与胜利后的 0.25 s fade 延迟分属不同调用方。集体 fade 恢复有专项 token，`NDecimillipedeSegmentDriver:88–101` 的偏移 tween、`NDecimillipedeSegmentVfx:124–181` 独立喷流和碎裂，以及落石任务尚需分别核对。 |
| DecimillipedeSegmentBack · DecimillipedeElite | `DecimillipedeSegmentBack:11–14` 通过自身场景的分段 driver 播共享攻击摇动。 | 继承上行。现有实机已覆盖尾节先倒，补击退中的 driver／落石。 |
| DecimillipedeSegmentFront · DecimillipedeElite | `DecimillipedeSegmentFront:11–14` 使用自身 driver 及相同基类。 | 继承上行。现有实机覆盖头节先于最后一节倒下，保留前段节点位置差异。 |
| DecimillipedeSegmentMiddle · DecimillipedeElite | `DecimillipedeSegmentMiddle:12–17` 中节驱动多个分段节点，继承同套复活和落石。 | 继承上行。现有实机末击落在中节。 |
| DeprecatedMonster · SaveUtil 兜底 | `DeprecatedMonster:23` 的 STUB 隐藏空行动，`SaveUtil:95` 只在无法按 ID 取到原模型时兜底，未见生产遭遇引用。 | 排除固定版本标准敌人。 |
| DevotedSculptor · DevotedSculptorWeak | `ForbiddenIncantationMove:42–50` 音、施法、.3 s 后尖叫／台词、.75／1 s 后 Ritual。`SavageMove:53` 普通钝击。`NDevotedSculptorVfx:80–112` 由 Spine 事件触发模型上的叫声和攻击粒子。 | 两个施法间隔、两条 FMOD、常驻发射器和意图尾迹已有专项，051／052 有原生正放、Back 和重做证据。 |

## E–H（18 个文件）

| 模型与入口 | 原生演出与提交 | Replay 现状与核查重点 |
| --- | --- | --- |
| Entomancer · EntomancerElite | `SpitMove:55` 施法再叠 Personal Hive 与力量，`BeesMove:69` 单次动画多段斩击，`SpearMove:79` 刺击。`PersonalHivePower.AfterDamageReceived:23–41` 先生成 Dazed 并用战斗 RNG 随机入抽牌堆，再预览和等待 .5 s。 | 预览后的等待直属能力，待审，随机位置与写入都保留。`NEntomancerVfx:131–175` 的 swarm tween、常驻虫群和发射器由模型持有。053 有一次胜利 Back 未记录恢复 BGM 调用，声音待确认。 |
| Exoskeleton · ExoskeletonsWeak／Normal | `SkitterMove:77` 一次动画多击，`MandiblesMove:87` 重咬，`EnrageMove:95` 施法加力量。`HardToKillPower:15–32` 限伤并闪图标。 | 通用，限伤与力量是业务，核伤害上限生效时的图标和重做。 |
| EyeWithTeeth · FogmogNormal／Fogmog 召唤 | `DistractMove:45` 音、攻击动画、斩击 VFX 后加 3 张 Dazed。入场 Illusion 再挂 Minion。 `IllusionPower.AfterDeath:77` 保留死敌、播 Stun、设置 `REVIVE_MOVE`；`ReviveMove:125` WakeUp 后实际回血，战斗结束另走 Dead。 | 高优先级功能风险。当前 `CombatUnits.Restorable` 放行 Osty、实验体与千足虫的特定状态，未覆盖场内死亡的 Illusion。需要用正确的身份、目标交互、幻影姿势和下一招来验收。 |
| Fabricator · FabricatorNormal | `FabricateMove:76` 两次召唤，`FabricatingStrikeMove:83` 攻击后召唤，`DisintegrateMove:92` 普通攻击。 `SpawnBot:110` 选空槽、抽取模型并 await Add 与 Minion，战斗命令会注册原生对象、生成场景、跑入场与历史钩子。 | 通用生成登记。验证相同 ID 下的新实例、槽位及落地；可生成的 Guardbot 等由各自入场 helper 设置节点。不能省略召唤或 Minion 后置提交。 |
| FakeMerchantMonster · FakeMerchantEventEncounter | `Swipe／SpewCoins／ThrowRelic／Enrage:76–110` 每招先 await `ShowDialogueForMove:137`，由 Chaotic RNG 选台词，有台词时等 .5 s。之后分别斩击、八段喷金币、施加脆弱或力量。 | 台词已登记，额外 .5 s 待审。`NFakeMerchantVfx:69–92` Spine 粒子独立，核喷金币结束和重做。原生事件入战、战后领奖有 RUN-005 代表证据。 |
| FatGremlin · GremlinMercNormal | 由 `SurprisePower:18` 创建，`SpawnedMove:61` 唤醒。 `FleeMove:67` 台词后 .75／1.25 s，禁交互、逃跑音和动作后再等 1.25 s 才提交 Escape。 `HeistPower.BeforeDeath` 击杀时生成追回金币奖励并改地图历史。 | 两段独立计时待审。逃跑会移除活体、写逃跑名单并影响 `GremlinMercNormal` 后续奖励，核对回退身份。当前 roster 明确拒绝活着但已脱离的历史成员，属功能验证项。 |
| FlailKnight · KnightsElite | `WarChantMove:59` 吼声和动画后加力量，`FlailMove:66` 一次动画双击，`RamMove:76` 单次冲击。 | 通用，保留两段命中与原生 SFX 区分。 |
| Flyconid · FlyconidNormal、SnappingJaxfruitNormal | `VulnerableSporesMove:52`、`FrailSporesMove:63` 切不同的模型发射器可见性，后者经通用 Attack 等待后生成 `NSporeImpactVfx`；`SmashMove:77` 钝击。 `NSporeImpactVfx:133–149` 独立任务与 token。 | 命中工厂节点已有隐藏路径。 `NFlyconidSporesVfx:88–119` 的原生 Spine 信号、两种喷雾的启停与可见性需单独验。 |
| Fogmog · FogmogNormal | `IllusionMove:60` 召唤音及 .75 s 动画后按槽位生成 EyeWithTeeth；`Swipe／Headbutt:70,79` 斩击，Swipe 加力量。 | 通用生成，`NFogmogVfx:87–115` 的推进烟尘为模型发射器。和 EyeWithTeeth 的保留死亡／复活合测。 |
| FossilStalker · FossilStalkerNormal | `Tackle／Latch／Lash:68,77,85` 施加脆弱、钝击或多击；`SuckPower.AfterAttack:22–45` 根据未被格挡的命中加力量。 | 通用，`NFossilStalkerVfx:83–114` 的喷雾与 Spine 起始回调独立。力量仍跟随实际伤害。 |
| FrogKnight · FrogKnightNormal | `ForTheQueen／StrikeDownEvil／TongueLash:89,96,104` 力量、普通攻击和脆弱。 `BeetleChargeMove:113–120` 先记已冲锋，攻击后另等 1 s；Plating 按回合钩子给格挡、递减。 | BeetleCharge 的伤害后等待是候选，保持低血分支状态和 Plating；其余通用。 |
| FuzzyWurmCrawler · FuzzyWurmCrawlerWeak、OvergrowthCrawlers | `AcidGoop:62` 按目标改 `SpineBoneNode`，清膨胀后攻击；`Inhale:83` 设置膨胀、音效、动画后加力量。 | 逻辑膨胀和主循环可恢复。 `NFuzzyWurmCrawlerSpitTrailVfx:91–122` 持有独立吐痰 trail，核目标局部变换、旧 trail 和重做。 |
| GasBomb · LivingFogNormal 召唤物 | `AfterAddedToRoom:53` 挂 Minion，`ExplodeMove:67` 爆炸攻击经 `NGaseousImpactVfx` 后 await Kill 自身。 | 不能跳过自杀及死亡钩子。命中工厂已有隐藏，`NGasBombVfx:101–136` 的 idle／爆裂／消散发射器是模型自己的。和 LivingFog 同测。 |
| GlobeHead · GlobeHeadNormal | `ThunderStrike／ShockingSlap／GalvanicBurst:64,74,83` 多段雷击、脆弱、力量。 `GalvanicPower:26–55` 给牌加附着，打出触发雷击 VFX 和实际伤害。 | 共同命中和通用附着，须核牌离场与回合边界的受击状态。 |
| GremlinMerc · GremlinMercNormal | `GimmeMove:73` 首次短台词后 .25／.5 s，再金币 VFX 和多击。`DoubleSmash／Hehe:95,112` 多击或增益；均会 await `ThieveryPower.Steal:21`。死亡 `SurprisePower:18–39` 传递偷取金币事实，生成 Sneaky 与 Fat，并标记遭遇。 | 气泡、金币 VFX 有共用入口，直属等待待审。偷钱、派生生成及后续奖励是业务，先验身份和财物，后验两次音源与散落金币。 |
| Guardbot · Fabricator 防御召唤池 | `AfterAddedToRoom:27` 调 `FabricatorNormal.SetBotFallPosition:61–68` 按实际比例和位置设模型落地标记。 `GuardMove:46–54` 施法后依次给符合条件的同伴格挡。 | 格挡走通用。恢复走 scene-only 重建时不重跑业务入场，需核 `FallControl` 局部坐标是否需要独立恢复。 |
| HauntedShip · HauntedShipNormal | `Swipe／Stomp:54,62` 斩击、多击；`HauntMove:72` 施法后 .6 s 出尖叫，另等 .2／.5 s 再施虚弱并生成 Dazed。 | 两段额外等待待审，尖叫节点共用登记。 `NHauntedShipVfx:113–155` 眼、头部泡沫由 Spine 开关，不属于临时尖叫。 |
| HunterKiller · HunterKillerNormal | `Goop／Bite／Puncture:51,58,66` 施 Tender、咬击、一次动画三击。`TenderPower:44–60` 按出牌扣力量、敏捷，回合末归还。 | 通用，`NHunterKillerVfx:75–106` 的吐液发射器由 Spine 启停，需核 Back 后是否提前停止、重做能否再起。 |

## I–L（11 个文件）

| 模型与入口 | 原生演出与提交 | Replay 现状与核查重点 |
| --- | --- | --- |
| InfestedPrism · InfestedPrismsElite | `Jab／Radiate／Whirlwind／Pulsate:80,88,97,107` 斩击、多击、格挡和 Vital Spark。 `VitalSparkPower:25–79` 给牌加 Tainted，打出后加 TaintedPower 并随 Power 更新或清除附着。 | 通用。需要同时核独有牌附着和演出；不能把规则中的 Tainted 变化当成视觉尾迹。 |
| Inklet · InkletsNormal | `Jab／Whirlwind／PiercingGaze:88,96,106` 钝击，一次动画三击或单击。`SlipperyPower:19–38` 修正伤害并扣层数。 | 通用，区分挡伤规则与模型动画。 |
| KinFollower · TheKinBoss | `QuickSlash／PowerDance／Boomerang:120,128,135` 斩击、施法加力量、双击。Boomerang 写 `AttackDistanceControl`；遭遇首个从舞步起手，次个不同。 | `NKinFollowerVfx:112–159` 持有两条 `NBasicTrail` 和草屑，未见同等专项。需核两随从重叠和与神官共用的音乐时点。 |
| KinPriest · TheKinBoss | `OrbOfFrailty／Weakness:126,136` 通过命中工厂生成榴弹，再提交负面；`BeamMove:146` 通过攻击回调启动常驻 beam 和 FMOD；`RitualMove:161` 台词后加力量；`AfterDeath:82` 随从死设 BGM 1、全死触发回应，神官死设 5。 | beam、榴弹和台词已有专门清理与实机记录。`NKinPriestVfx:96–125` 还含 Spine 信号和火花，核和随从叠加时的声音、粒子，以及跨随从死亡恢复 `the_kin_progress`。 |
| KnowledgeDemon · KnowledgeDemonBoss | `CurseOfKnowledgeMove:149` 开场台词和动画后，为活着的目标各起一次 `ChooseCurse:170`，await `Task.WhenAll`。Disintegration／MindRot／Sloth／WasteAway 的 `OnChosen:25–30` 提交对应 Power，完成后才增计数。`Slap／KnowledgeOverwhelming／Ponder:193,201,213` 攻击、三击、回血加力量，并改 `IsBurnt` 与 BGM 参数 1／2；离场设 5。 | 选牌、施咒和 `Task.WhenAll` 都是业务边界。通用攻击等待可按原机制收束。 `NKnowledgeDemonVfx:158–181,264–305` 的爆炸、伤害、余烬发射器和烧伤表现独立。需核 Back 横跨选牌及音乐阶段。 |
| LagavulinMatriarch · LagavulinMatriarchBoss | `Sleep:115` 入场设睡姿、Plating、Asleep 后挂 `NSleepingVfx`。受伤或 Asleep 耗尽会触发醒来、眩晕或 AI 转移。`AfterDamageReceived／AfterDeath:130,148` 停睡眠粒子并改眼睛第二轨，`WakeUp／Slash／Slash2／Disembowel／SoulSiphon:189–242` 继续正常动作。 | `NSleepingVfx.Stop:152–165` 是独立异步尾声，5 s 后释放。Back 跨唤醒需重建正确的持续睡眠效果和第二轨，不能把被停掉的节点引用当成数据恢复。普通恢复只核主循环。 |
| LeafSlimeM · 多种史莱姆编组及 Flyconid | `ClumpShotMove:42` 攻击。`StickyShotMove:50–73` 更新 `SpitTarget` 局部节点再施法、投射效果及生成两张 Slimed。 | 通用 VFX 及牌生成，核复用场景的目标局部坐标和同屏不同目标。 |
| LeafSlimeS · SlimesWeak／Normal、SlitheringStranglerNormal | `TackleMove:42` 普通撞击；`GoopMove:50` 施法、攻击音和 slime impact 后生成一张 Slimed。 | 通用，核短施法残余和生成牌时点。 |
| LivingFog · LivingFogNormal | `AdvancedGas／Bloat／SuperGasBlast:76,85,104` 以 `NGaseousImpactVfx` 为命中工厂；Bloat 等 0.35 s 演出后向空槽提交 GasBomb，之后才攻击；AdvancedGas 加 Smoggy，能力会改变手牌及其播放条件。 | 通用命中工厂、生成和逻辑。 `NLivingGasVfx:172–270` 的攻击、受击、减益、消散与再聚由模型上的信号驱动。应和 GasBomb 的生成及自爆配套核对。 |
| LivingShield · TurretOperatorWeak | `ShieldSlam／Smash:54,62` 普通斩击及力量。`RampartPower.AfterSideTurnStart:21–31` 依次给其他单位格挡。 | 通用，回合开始的护盾结算需保持顺序。 |
| LouseProgenitor · LouseProgenitorNormal | `Web／Pounce:87,111` 卷曲时先播 uncurl 音／0.9 s 动画再清逻辑卷曲，`CurlAndGrowMove:102` 卷曲、格挡与力量。 `CurlUpPower:34–68` 受伤先标记，在卡牌结束钩子播卷曲再给格挡并移除自身。 | 动画计时走通用，需分别核卡牌触发及原生回合触发后的姿势、格挡和撤销后的音效。 |

## 按调用边界归类

1. **功能边界先行。** EyeWithTeeth 的 Illusion 留队复活与 FatGremlin 的活体逃离各有明确原生路径，目前的 Back 成员判定缺少对应通路。千足虫有可复用的留队／胜利退场证据，只有实际触发倒下并复活才覆盖回血演出。Axebot 的生成仍走业务路径，额外显形独立于动作结束，不能拿一次身份比对替代。优先分别核下一步仍可推进、Back 和重做均与原生状态一致。
2. **单纯计时与并发分开。** Axebot 的 BootUp、FrogKnight 的冲锋后停顿、假商人与两种 Cultist 的对白、HauntedShip 的尖叫，可逐项审核精确调用方。CeremonialBeast 的冲撞已有定点，后续吼叫仍有额外等待。对 Aeonglass、Entomancer 的牌预览前后，只释放计时或显示等待，保留实际牌生成、RNG、牌堆和能力提交。Kaiser Crab 带 token 的背景等待以及千足虫的 `Task.Delay` 属于另一种所有权。
3. **常驻场景表现。** 按常驻 Spine 发射器、子节点位移、额外轨道、持续声音和异步释放分开登记。DevotedSculptor 的已测发射器可作为同类对照。独立恢复的首批代表可用 Entomancer 的虫群 tween、CubexConstruct 的蓄力音与激光、LagavulinMatriarch 的睡眠粒子和眼睛轨、Kaiser Crab 的共享背景。停止废弃尾声后仍应让下一次原生 cue 正常触发。
4. **音乐阶段与招式音独立。** 本分册查到 Aeonglass 的 `queen_progress`、Crusher 的 `kaiser_crab_progress`、KinPriest 的 `the_kin_progress`、KnowledgeDemon 的 `knowledge_demon_progress`，以及 BygoneEffigy 的精英二阶段入口。当前胜利 Back 有重启专曲入口，`CreaturePresentation.MusicUpdated` 的特定参数快照针对实验体。应由共用 BGM 审计判断重启与参数恢复如何组合，再用实际听感验收。

本册覆盖按固定反编译目录文件名核对，60 个条目均有对应源码，无遗漏和重复。未运行游戏、未修改录播业务源码。

## 首批恢复候选 build-055

- EyeWithTeeth 仅在 format 10、仍属于原生战斗、带 IllusionPower 且 `IsReviving` 与 `REVIVE_MOVE` 同时匹配时接受留队死亡状态。检查点恢复同一单位的状态、意图和 HUD；再次前进仍由原生执行复活。
- Back 和 Seek 共用的检查点接受原生逃跑名单中已登记的活敌，核同一对象及稳定 ID，并保留原生逃跑名单。未知、重复、仍在场或非活体的逃跑项会使校验失败。跨逃离恢复只重建需要的场景节点，偷窃蚱蜢的持牌显示从恢复后的 SwipePower 重建。原生 CombatRoom 仍作为检查点根；恢复后校验额外奖励列表的对象和顺序。逃跑、偷牌、还牌和发奖均走原生正放。
- Release 与直接契约构建均为 0 警告、0 错误。`enemy-generation`、`units`、`run-envelope`、`run-branches` 通过。Fogmog 19 步旧片 `5abec0a7` 与 ThievingHopper 5 步旧片 `8b8f45f1` 均通过生产读取。后者只覆盖偷牌前击杀，未实测偷牌逃跑。候选已备份并安装，回执 `build-055-a8e58872`，SHA-256 `BAC8EDE60AB04701071B2CFB2B18D427BC3824C1B5F1E488AEDC1F8510D1420B`。
- 实机短测一从「独立战斗／旧片」打开 `5abec0a7`。4/19 Back 到 3/19，确认眼球恢复血条，再 `>>` 验证击倒。到 10/19 确认原生复活，Seek 9/19 验证待复活状态，再 `>>` 验证相同单位复活。短测二手动启用开发菜单的「胖地精逃跑短测」，新建铁甲 A0，录下佣兵偷金币、佣兵被击杀、胖地精逃跑及另一只地精被击杀。独立分场跨逃离的同一个结束回合做 Back 与 Seek 后重做，核胖地精进出和胜利。金币奖励用录制时的原生结果和含已记录奖励动作的房间路线核对。运行结果待用户短测。RUN-005 建筑师的整局短测独立记录。

## 两册合卷：检查点状态、场景和 SFX 的所有权

按 build-055 源码核对。上述两段短测暂停，以下样本仅供之后排期。原生动作先收束到可恢复边界，随后 `DirectCheckpoint` 清已登记的演出、恢复数据、按需重建敌方场景、回填显示。数据快照不保留 Godot 对象、委托、Task 和令牌。普通 Back 在敌方名单相同且未跨指定生死／阶段时复用原节点，按需重建只调用 `NCombatRoom.AddCreature`。它不重复 `AfterAddedToRoom` 或死亡、逃离、奖励钩子。模型恢复的是主 Animator 循环、缩放、色相、位置、HUD 和已有专项。主 Animator 的 `SetNextState` 只设置主轨。以下覆盖以这些边界为准。 [DirectCheckpoint.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/DirectCheckpoint.cs:122) · [CreaturePresentation.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/CreaturePresentation.cs:153) · [CombatRoster.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/CombatRoster.cs:151)

| 调用链与所有者 | 现有覆盖和剩余边界 | 日后最短区分样本 |
| --- | --- | --- |
| **留队死亡 `IllusionPower`** | 查遍生产调用，只有 EyeWithTeeth 与 TheObscura 召出的 Parafright 挂该能力。两者的 `AfterDeath` 都保留死敌并设置 `IsReviving`、`REVIVE_MOVE`，但 `CombatUnits.IsRetainedIllusion` 限 EyeWithTeeth。Parafright 的待复活点仍会被拒绝，是可从源码确认的检查点缺口。Parafright 有 `stun → stunned_loop`、`wake_up → idle_loop` 及专属复活音，未来扩展要保留这些差异。胖地精、偷窃蚱蜢与限时假人是查到的三个生产 `Escape` 调用方，build-055 已接共享原生名单校验。限时假人另有父事件结算。 | Fogmog 旧片跨同一复活边界 Back／Seek 是共用基线。TheObscura 只补一段击倒 Parafright、留场一拍、原生唤醒，核 `stunned_loop`、血条和复活音。逃离沿用胖地精主样本。若测假人，只补超时退出及父事件结果。 |
| **同一模型状态与模型侧委托** | ToughEgg 孵化的 `IsHatched`、Power、HP、AI 和 RNG 属数据；卵壳与孵化后主循环已有通用恢复路径，无需再次孵化。独立风险是单次订阅，`SlumberingBeetle` 和 `SoulNexus` 的 `Died` 回调首次触发后自解订，Cubex 的 `CurrentHpChanged` 在模型移除时解订，实验体的 Power 订阅也在移除时解订。`CombatUnitSubscriptions` 只对账 tracker 自己的处理器。数据和场景重建均不会补回这些模型处理器。静态可确认重绑缺口，实际差异需验证；如 SoulNexus 的初始 `writhe` 可由皮肤初始化复现，第二次死时何时清轨仍要核。 | 一段 SoulNexus 的末击、Back、同一末击，核首次和第二次 `Died` 分支及 Track 1，观察点设在退场钩子之前。若专测孵化，只跨 `HATCH_MOVE` 核卵壳、同一 ID、HP 与下一意图。 |
| **生成后独立显形与入场节点参数** | `StockPower.AfterDeath` 等到新 Axebot 已加入，再隐藏它并启动脱离主流程的显形任务。任务唤醒后按同一 `Creature` 查当前节点，无旧节点或代次校验。退回替换前时，旧任务通常失去可写节点。Seek 回同一替换体的历史边界时，旧任务有机会写到新场景。发生窗口和可见性待验。`FabricatorNormal.SetBotFallPosition` 由 Guardbot／Noisebot／Stabbot／Zapbot 入场业务设置 `Visuals/FallControl`，场景重建不重跑。其默认值是否足够仍未知。 | Axebot 击杀后趁显形未结束 Back，再 Seek 回替换点，观察是否有二次显形或旧 `respawn`，同时核槽位与 ID。机器人落位只需 Fabricator 一次从退召唤到 Seek 回已召唤点，对照 `FallControl` 和落地位置。 |
| **临时效果、直接挂房间和相机** | `VfxCmd.PlayVfx`、Attack 工厂及已挂钩的 `Create` 有明确登记，房间级通用相机、气泡、意图粒子也已有归属。`NWormyImpactVfx.Create` 已挂钩，因此感染虫手动 `AddChild` 仍覆盖。`DecimillipedeSegment.AnimSegmentsAttack` 直接实例化落石，`Creature.GetVfxContainer` 返回房间容器。落石 `_ExitTree` 会取消独立 `Task.Delay`，但同房间 Back 和敌人节点重建都不会触发它退出，也没有找到此类型的登记。实验体 `BurningGrowlMove` 手动挂 `NTestSubjectBurnVfx` 到房间后景，也未见工厂登记。它和实验体模型自身的两个烧灼轨是不同实例。 | 千足虫落石第一颗出现时 Back，观察回退后是否还有下一颗或旧岩石，再 `>>` 核只出现本轮落石。普通工厂回归可用 PhrogParasite 的 Infect，同时核弃牌堆生成。实验体如需单独区分前后景，只测 Burning Growl 的房间挂件。 |
| **模型复用粒子、轨迹和 tween** | 节点实际移出树时，模型子发射器、子轨迹和节点绑定 tween 随旧场景退役。保留同一 `NCreature` 时，恢复主循环不会对已发射的粒子、`NBasicTrail` 点列、子骨坐标或独立 tween 做通用恢复。咔咔和神官的已登记发射器／beam 是明确专项。机械骑士喷火与引擎、Flyconid 双喷雾、同族随从的轨迹、Myte／Jaxfruit 目标、BowlbugSilk 与史莱姆的骨骼目标均走复用节点。虫术士的 `NEntomancerVfx` 自己持有 `_swarmTween`，已有原生 `CancelSwarmAttack` 可归位并关闭攻击粒子。待测目标是去掉攻击虫群，同时保留常态虫群。甲虫睡眠动画也说明不能整棵树关粒子。 | 机械骑士等喷火首个发射事件后 Back，再重做，核喷火停、引擎回到目标点状态、重做时喷火仍能启动。虫术士等虫群离身后 Back，核独立 tween 回位且常态粒子仍在。目标骨骼与 trail 优先跟有实际残留证据的样本合测，静态未证实普通下一招会错目标，因为其会重新配置。 |
| **目标本来就有的节点、材质与额外轨道** | Lagavulin 和 SlumberingBeetle 的睡眠粒子挂在模型 `%SleepVfxPos`。`NSleepingVfx.Stop` 后等待 5 秒自删，模型字段随即清空，复原主循环或重建模型都不会自动恢复已停止的睡眠实例。Lagavulin `IsShellAwake` 与眼睛 Track 1 分开。Aeonglass 圆环、Queen／SoulNexus 的额外轨、Vantom 的充能轨、实验体多轨、WaterfallGiant 的蒸汽阶段、SoulFysh 材质也有各自归属。初始化能回填固定皮肤／默认轨，阶段轨、材质终值和持续发射器仍应以目标点为准。帝王蟹左右臂和身体、反应及背景粒子在房间背景，重建任何一个钳子均保留这个背景，背景 `_ExitTree` 的 token 只在实际退出时生效。 | Lagavulin 从首次受击 Back 到睡眠，再 Seek 到已醒点核睡眠粒子。眼睛轨以跨半血的受击为观测点。帝王蟹只取「一钳已死、另一钳正蓄力」附近，Back／Seek 双向核两只钳、身体与反应轨。其他次轨复用这个判断，只有不同的相位数据或材质提交另补样本。 |
| **已开始 one-shot 与退役后才触发的 one-shot** | `NativePhaseRetirement` 只在房间、动作及代次归属匹配时处理。Debug audio 有 ID 可停止。普通 FMOD one-shot 在收束期间的新调用可置静音，已有独立停止句柄限无参数的四个审过的咔咔／仪式鹿事件。参数化 Cultist 攻击声走同一 FMOD 代理，当前无停止句柄。Spine 回调如拿不到动作归属也不会被共用入口静音，需核事件触发在哪个执行流。`SfxCmd.Play` 自带 `IsEnding`，而 `PlayDeath` 没有这道条件，正放时天然不播的声音按原生基线比对。 | 咔咔已有普通正放与撤销回归，补 Calcified／Damp Cultist 任一参数化暗击。听见起音后 Back，再听原生重做，分别记录旧尾音和新起音。重做缺声先对照相同原生路径是否被 `IsEnding` 阻止。 |
| **Creature loop、全局 loop、曲目** | `NCreature.StartSfxLoop` 记录 path 和停止参数，仍委托全局 `NAudioManager` 按 path 播放。`_ExitTree` 能自然停掉节点登记的 loop，复用节点时没有相同清理，重建节点时也不会根据检查点重启该有的 loop。Cubex 充能、Owl 飞行、SlumberingBeetle 睡眠、Rocket 充能、Jaxfruit idle 和 WaterfallGiant 环境声归这里。Ovicopter 直接经 `NAudioManager` 起停全局 idle，怪物场景退出无法代替模型的 `BeforeRemovedFromRoom`，整局清理的 `StopAllLoops` 也不能替代同房间 Back 对目标状态的对账。相同 path 多所有者的混音和停止方式需实测。曲目及阶段参数由 PM 另表合并。 | Owl 正放飞起与俯冲，再分别回到飞起前及已飞行点，核循环静／响和俯冲重做。Ovicopter 只补一次击杀后的 Back，核同房间全局 idle 恢复。需要参数对照时用 Cubex 蓄力→攻击阶段，和激光／rings 同次观察。 |

资料口径补充。`Creature.cs:790,807` 是前景和后景容器归属。`NCreature.cs:550,1234–1263` 是节点 loop 的实际启停。`NativeDisplayTrace` 是按房间记已知来源，音频的动作归属检查则在 `NativePhaseRetirement.CurrentOwner`。同一个招式可以有被登记的命中节点、未登记的房间尾声及目标必须保持的持续粒子，归类时分别核对。知识恶魔的选牌和 `Task.WhenAll`、贪食者吞食的强制击杀及位置 Tween 完成信号、孵化血量 RNG、逃离登记与事件奖励继续视为原生业务边界。源代码说明了谁能写入和清理，也能证明部分入口缺失。实际 Spine 事件发生帧、FMOD 尾音长短和延后任务是否与一次 Back 相撞，仍属实机问题。 [NativeDisplayTrace.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/NativeDisplayTrace.cs:272) · [NativePhaseRetirement.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/NativePhaseRetirement.cs:233) · [CombatUnitSubscriptions.cs](/C:/Example/STS2ReplayMod/src/ReplayMod/CombatUnitSubscriptions.cs:15)

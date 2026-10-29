# 战斗内容覆盖：材料、机制缺口与取样计划

更新：2026-10-29，D-128，PM静态盘点。游戏固定v0.111.0-41cef1ea；代码基线为891e5ff（PLAYBACK-002/build-003），避免与正在并行改动的PLAYBACK-003源码混读。新包完成后只复核变化项，不重复整次调查。本页替代2026-10-28盘点作为当前参考，历史结果仍在各CONTENT/STATE报告与Git版本中。

同日D-129扩展：完整材料/契约与候选批次见[BATTLE-COVERAGE-PLAN](BATTLE-COVERAGE-PLAN.md)，静态索引脚本见[Build-CombatCoverageIndex](../tools/Build-CombatCoverageIndex.ps1)。已生成本地15类/1539文件及池/命令/遭遇引用索引；注册表确认5个常规角色，内容可达性还受池、解锁和单/多人过滤。PLAYBACK-003已产出001候选但运行Pending，不把下列静态基线自动当作新候选验收。

## 结论与使用方式

已有原生CardModel/PotionModel/Hook与统一CardSelect通路，覆盖不是为每张牌重写效果。现在需要补输入种类、专属状态/目标、对象生命周期、选择所处阶段及可恢复上下文，并用少量能暴露差异的真实组合检查。全部原版卡牌/遗物/药水和战斗内容仍是目标。

区分三类：**静态确认缺口**（代码明确拒绝/未表达）、**已有通路未证**（可能复用但缺少验证）、**旧路线已测/新路线待接**。普通前进、录制结果、内存Back/再执行、文件恢复/纠偏分别标记，不能一个“支持”勾掉所有层次。静态检查没有产生新的运行PASS。

## 已有与需要整理的材料

| 材料 | 目前可用 | 如何使用/还缺什么 |
| --- | --- | --- |
| 固定游戏C# | 本地反编译缓存；游戏DLL身份见CURRENT_STATUS | 查共用入口、模型私有状态、RNG与生命周期；不为每个方法补IL，歧义时再查 |
| 内容模型与池 | Cards 596、Relics 300、Potions 65、Powers 268、Monsters 121、Encounters 91个C#文件 | 这是目录模型文件数，含派生/废弃/非正常获取内容，不是可收集数量或已支持数量。按CardPools/RelicPools/PotionPools、注册与可达性再映射 |
| 角色/资源模型 | Characters目录8个文件，包含RandomCharacter/DeprecatedCharacter等；PlayerCombatState有Stars、OrbQueue、Pets | 不能按文件数宣称有8个可玩角色；为实际可玩角色列专属资源/单位/目标 |
| UI与本地化 | 原生窗口类、资源路径、模型ID可从源码追踪 | 中文名与模型ID按实际来源对应；特殊窗口/宠物/球的节点按需定位，不先全量重做UI |
| 真实录像与报告 | 猎人1d5346b085b04635ab3e00b43fe9190c、储君cd3baa3b1be7466f9cb94d385c3db420等已有样本 | 复用真实样本，记录版本/实际动作/预期结果；不改片段顺序假造覆盖，旧路线通过不直接算新路线通过 |
| 用户经验 | 已有随机数/选择/动画经验，本轮询问额外主动操作及易准备的新单位样本 | 经验用于发现遗漏和选样；发生频率/根因/支持结论仍以固定源码或运行验证区分 |

源码根为`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。模型文件数由各`MegaCrit.Sts2.Core.Models.*`目录的`rg --files -g '*.cs'`计数，未启动游戏或加载程序集来枚举内容。

## 当前覆盖与机制缺口

| 机制 | 基线确认/已有证据 | 需要补的材料或验证 |
| --- | --- | --- |
| 普通出牌、用药、结束回合 | OperationKind三类，原生TryManualPlay/EnqueueManualUse/EndPlayerTurnAction；已有多种真实样本 | 对照正常可达主动操作查漏，区分输入接受/执行/取消；不是逐卡手写效果 |
| 丢弃药水 | **静态确认缺口**：原生NPotionPopup会提交DiscardPotionGameAction；当前枚举/Queue白名单不接受 | 补独立丢弃语义、槽位/实例、退出与恢复，不与喝药合并；一瓶药水丢弃并Back即可暴露差异 |
| 选择窗口与所处阶段 | 通用叶入口覆盖手牌、战斗牌堆、小候选、牌组及Bundle；旧CONTENT含初始/药水/回合选择，新路线固定猎人手牌选择已证 | 新普通入口接回初始选择；按实际调用核对升级/附魔/变形预览、跳过/有序多选/连续选择、敌方强制选择。不要先造任意并发框架 |
| 生成/复制/变化/自动打出 | 格式4+有生成/变化、自动派生Task和实例跟踪；格式5有DupeOf/锻造字段；旧样本已测部分 | 将实例/牌区/临时费用/附魔/叠加次数及新旧模型关系接回新路线，验证派生效果不再重放为手动输入 |
| 遗物与能力触发 | 原生Hook执行；旧样本有开心小花、金纸、工具箱、必备工具/暴政/熵等 | 回合开始/回合内/回合结束都需覆盖；私有计数/冷却/延迟回调与订阅恢复不可只看闪光或Power.Amount |
| 遗物成员增减 | **静态确认缺口**：AddRelicInternal/RemoveRelicInternal在观察期间主动拒绝 | 先找本版本正常战斗中真实可达来源；可达后再补成员/订阅/状态，不为假设场景先建扩展框架 |
| 储君星星/锻造/动态费用 | 字段与CONTENT-004固定13项已有旧路线证据，新路线未以该片验收 | 优先迁回已有样本，检查反复修改剑/副本/跨牌堆/跨回合；不是重新做星星系统 |
| 充能球 | **明确比较缺口**：LiveState无OrbQueue容量/顺序/球状态专门投影；故障机器人资格未开放 | 原生OrbQueue已有容量/顺序/触发，通用内存遍历可能覆盖部分数据，不能断言从零。仍需明确核对/重绑球HUD、充能/激发/被动/集中/扩缩容、可变球状态与RNG |
| 宠物、新怪生成/分裂/增援 | **静态确认缺口**：CreateCreature观察期拒绝；CombatRoster拒新成员/nextCreatureId变化，DirectCheckpoint拒Pets，目标仅Enemy/Player | 单位稳定ID、目标/所有者关系、AI/RNG、场景与订阅重建；死亡原怪恢复不代表新单位恢复。Osty等先找最小样本 |
| 换形/多阶段Boss | CombatRoster检查原Visuals/Body引用；逻辑模型与行为私有状态不能只看血量/意图 | 区分换外观、换模型、换单位；补阶段/计数/招式机和节点绑定。不能把所有外观变化一概当新生成单位 |
| 回合与自动连锁 | EndTurn及GenericHook/ReadyToBeginEnemyTurn等自动动作已有窄路径；新路线允许当前宿主选择续接 | 额外/跳过回合、持续触发和敌方选择按实际因果链归属；选择不总属于玩家出牌。暂停不整体锁死当前宿主 |
| 战斗结束 | BattleEnd/Operation明确表达Victory；旧路线有完整胜利，新路线固定前22，PLAYBACK-003正接完整单场 | 失败/玩家死亡/复活与特殊结束需独立结果语义；玩家死/宠物边界当前拒绝。不要把一份未标完整的片段当完整失败录像 |
| 录制准入/后续战斗 | **明确范围限制**：fresh、单attempt、row1、A0、首幕普通战斗，仅铁甲/猎人/储君，Continue不录 | 多次战斗开始/封存、精英/Boss/后续Act/难度与Continue恢复；这是场景生命周期，不是补几张牌即可解决 |
| 文件强记录与纠偏 | STATE-001仅铁甲首张未升级Defend固定文件点；内存Back更广但不是文件能力 | 更多真实录制关键点、类型/逻辑身份、对象重建及新进程恢复/偏差纠正；不要以重演缓存代替原局权威事实 |
| 规模与节奏 | 格式5/6已落盘输入接受/完成时间；原始节奏播放器未实现。当前256操作/4MiB容量 | 长战斗和多房间要检查容量/采集开销；时间包含加载/暂停等，不能直接当已完成的有效原始节奏 |

## 知识恶魔：用户提供的代表例

用户回忆：二层Boss知识恶魔周期性要求玩家选择负面效果，约“每三回合”。本轮已定位固定源码`KnowledgeDemon.ChooseCurse`：候选通过CombatState.CreateCard生成，实际调用`CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), cards, target.Player)`；结果经`IChoosable.OnChosen()`执行。

**Confirmed（静态）**：这走已知小候选选牌叶入口，不是新的按钮输入种类。源码在两次Curse之间安排Slap、KnowledgeOverwhelming、Ponder三个其他行动，并按Curse计数小于3分支。特殊性在敌方行动/Blocking上下文、效果候选和Boss跨周期状态，不能只支持玩家手动牌内的选择。现有Recorder.ChoiceBoundary在有Pending时可关联当前操作，EndTurn可能是归属入口，仍需以后真实Boss路径验证。

**待验证**：敌方阶段的录制/重放/暂停/恢复，以及Boss计数/外观状态是否完整保存。用户“每三回合”的经验与源码中间三个其他行动分开描述，不冒充已经进行动态周期测试。本轮不要求用户重新测试Boss，也不将其塞进PLAYBACK-003首战范围。

## 单位生命周期：用户提供的四个代表样本

2026-10-29用户凭经验补充，下表地点/直观回合描述按用户观察记录；模型与方法已对照固定v0.111.0源码，未运行游戏。四例足够安排第一批有区分度的生命周期验证，不要求用户现在再测试或继续找更多样本。

| 用户描述 | 已定位原生模型 | 静态确认的路径 | 未来取样应检查什么 |
| --- | --- | --- | --- |
| 一层暗港三只老鼠，有召唤意图 | TwoTailedRatsNormal / TwoTailedRat | 初始三只；CallForBackup经CreatureCmd.Add<TwoTailedRat>加入新单位。CanSummon还检查冷却、计数、空槽及队友意图 | 新CombatId/对象、槽位/目标顺序、召援计数与AI/RNG；Back到召唤前再做，不残留旧单位/订阅 |
| 二层直飞产卵虫，首回合召三个卵、下一回合孵化 | OvicopterNormal / Ovicopter / ToughEgg | LayEggsMove最多在空槽创建三个ToughEgg并加Minion；HatchMove设IsHatched、移除相关Power，Hatch在同一Creature上重设最大/当前HP并播放孵化，不在该方法新建怪 | 分开验证“产卵生成”与“原卵孵化”；保留单位身份，恢复Hatch/内部标记、状态与动画，检查RunRng.Niche抽取孵化HP。具体动态孵化时点未测 |
| 二层精英残杀千足虫，未全杀则倒下个体两回合后复活 | DecimillipedeElite / DecimillipedeSegment / ReattachPower | ReattachPower阻止死后移出战斗，记录私有isReviving、禁用交互；DeadState后接Reattach，其他节未全死时治疗原Creature；全部倒下才触发共同退场 | “已死但仍在队列/战斗”的成员，不能按HP0立即删掉；恢复复活等待/交互性、招式和其他节关系；最后全灭与部分倒地两类终局区别。两回合为用户经验，未做运行计时 |
| 三层Boss实验体有三阶段，转阶段以复活表现 | TestSubjectBoss / TestSubject / AdaptablePower | Adaptable阻止死后移出战斗并触发倒地；RespawnMove递增Respawns，分别按第二/第三形态HP治疗同一Creature，增减Power并改变动画/大小 | 两次阶段转换、原身份/Respawns/私有isReviving、最大HP/Power/招式与显示同步；不能把第一次HP0算作胜利，也不能用新造同名怪替代原实例 |

这些例子把两个原本容易混淆的维度分开：**新实例加入**与**原实例改状态/形态**；**死亡移除**与**倒地留场等待复活**。现有CombatRoster/DirectCheckpoint拒新成员、宠物或非存活成员的限制仍是明确缺口，不能只删除检查就宣称支持。通用化应继续以原生战斗结束事件为终局依据，不改用“敌人血量归零”推断。

后续建议先三鼠验证纯新增，再以产卵/孵化验证新增叠加原实例变化，继而千足虫验证倒地留场/延迟恢复，最后用实验体验证多阶段终局。此为素材与未来取样顺序，不新签工作包，不将二层/精英/Boss加入正在执行的PLAYBACK-003首战范围。

## 比较与恢复需要单独确认的状态

- 已有投影：run/player/敌人RNG、牌实例/升级/费用/动态值、牌区顺序、资源、药水槽和遗物序列等；原生各类RNG不能被简化为一个全局计数。
- 潜在盲区：Power投影主要是ID/Amount，遗物是Saved/Vars等；模型私有字段和触发订阅并非都可由这些投影证明正确。通用内存memento可遍历许多可变字段，但明确排除Task/Delegate/Godot节点，文件新对象又有类型/绑定约束。这里是验证风险，不是已确认某张牌有bug。
- 用户关于“洗牌主要取决于弃牌堆集合”“不同适应打击实例可能不同”等经验/推测用于取样：保留实例与有序牌堆、检查对应RNG前后值；尚未通过受控试验证明只由集合决定。

扩展源码确认：洗牌经StableShuffle先排序再使用独立Shuffle流，随后仍可被重排Hook改变；AdaptiveStrike生成克隆/设本战费用0，没有直接RNG调用，但整条Hook链是否耗RNG不能由此排除。详见完整计划的RNG节，不能继续把“集合决定”无条件当成事实或直接否定玩家经验。

## 建议的后续批次（未同时派发）

1. **正在做：PLAYBACK-003完整单场。** 正常入口、新文件、真实胜利/Back/Stop；覆盖材料不阻塞它，关联普通问题可就近修。
2. **已有共用内容迁回。** 用铁甲/猎人/储君已有样本检查初始/回合/药水选择、遗物计数、生成/复制/变化/费用。并列登记丢弃药水这种明确输入漏项，按最小真实例补上。
3. **专属资源与单位生命周期。** 球类状态/HUD为一组；宠物/生成怪/复活/换形按共同单位身份与场景机制组织。每组取能区分数据与显示的样本，顺带补所需权威状态，不逐牌建立工作包。
4. **完整战斗场景。** 失败与玩家死亡、精英/Boss及后续房间/难度/Continue；知识恶魔用作敌方强制选择代表。地图/奖励/商店整局与楼层导航按实际生命周期接入。

所有批次仍以实际功能闭环验收：录下操作/选择→原生前进→关键点Back后再执行→必要文件恢复→正常退出。按本批改动取必要项，不能把每行都变成每张牌的强制完整矩阵。

## 用户经验与可追加材料

- 已收到额外主动操作问题的知识恶魔例子；静态确认它仍属统一选牌，但拓宽了来源阶段。丢弃药水已由源码另行查出。
- 新单位/形态问题已收到三鼠、直飞产卵虫、残杀千足虫、实验体四例并完成上述静态定位；暂不需要更多材料或开游戏确认。具体动态时点、录播/Back结果仍待对应功能迭代验证。
- 如还有卡牌/用药/结束回合/选牌以外的真实战斗操作，可随时追加。中文名、发生条件及可见操作足够，模型映射由PM查源码，不要求用户报类名。
- 撤销结束回合已收到用户答复“多人有，单人没有”。源码类型/按钮分支保留为材料，当前不列单人MVP必需输入，留多人阶段；经验判断不冒充穷尽时序运行证明，无需现在额外测试。

## 依据与本轮边界

- 代码基线891e5ff：`LiveState`（Supported/Mechanics/Capture/Target）、`Clip`（OperationKind/TargetRole/256与4MiB）、`Recorder`（fresh/Checkpoint/ChoiceBoundary/Seal）、`Patches.Queue/Spawn/RelicMembership`。
- `ChoicePatches`/`ChoiceTape`/`NativeChoice`：已挂叶入口、重叠scope拒绝、候选/来源/自动选择与窗口；`CombatRoster`/`DirectCheckpoint`/`StateGraph`：成员/宠物/节点身份和数据/执行上下文边界；`BattleEnd`：当前Victory生命周期。
- 固定原生源码：`DiscardPotionGameAction`与`NPotionPopup`、`PlayerCombatState`/`Entities.Orbs.OrbQueue`、`KnowledgeDemon`/`KnowledgeDemonBoss`，以及`TwoTailedRat.CallForBackup/CanSummon`、`Ovicopter.LayEggsMove`、`ToughEgg.HatchMove/Hatch`、`ReattachPower`/`DecimillipedeSegment`、`TestSubject.RespawnMove/Revive`/`AdaptablePower`；模型/遭遇/池文件可按需查询。
- 本轮仅源码/既有报告复核、模型文件计数和文档更新；无游戏/程序集运行、无新probe、无源码/安装/存档修改。PLAYBACK-003正在由Implementation独立实现，以上静态基线不反写其未来成果。

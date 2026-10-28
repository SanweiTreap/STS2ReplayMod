# CONTENT-001 通用战斗选择录制与回放

2026-10-27，D-106/D-092。**build-001已完成固定组合的结果级选择录播、稳定点后退、胜利/Stop重开，用户确认通过，实施证据提交PM复核。** r2两次打开、36次实际操作/36次自动选择、19次直接后退均匹配。用户另明确期待原生窗口中分步展示点选过程，当前轻量文字UI未实现该体验，已另存提议并按用户授权交PM。重复加号小问题已修为build-002并安装，只有构建/数据回归，不能把build-001实测冒充新DLL完整运行。沿原对话/工程，未自动启动游戏、Computer Use或headless。

## 候选与恢复

ReplayMod0.1.0，固定v0.111游戏SHA沿项目；DLL `F1D9D349A5804D3A455F78D43B964320102C5117CCA3D9B9E5160D126DB63938`，sealed/installed/output匹配。收据 `evidence/content-001/preparation/build-001-21fd7abe.json`。本轮用户实际确认Cloud仍关闭，安装前后game0，原活动账号及仅ReplayMod/modded1通过；52个当前modded1存档文件与STATE001恢复点一致，未新增正常进度，复用完整恢复点，不重复全账号备份/审计。

以上为build-001安装时事实。当前显示修正build-002为 `F65870788BD6D79CBE8822DF73BBF247D7A760A533430753E7807460CA6A42D8`，见preparation/build-002-6aafc237.json；已验证的F1D9 pair/settings/selector保留到CONTENT001恢复点preparations/build-002-6aafc237。新版本只去掉CardModel.Title的额外升级后缀，并加入F1D9精确兼容以读同一旧片；候选/结果校验原本就忽略显示标签，原数据/hash未改。没有要求用户重复整场测试，也不把新版本记为已完成本包完整运行矩阵。

旧可用STATE-001/build-003（4283890C…E76A92D）pair/settings/selector保留到 `C:/Projects/STS2ReplayMod-local/backups/STATE001-20261027-122340-b30c90322d/preparations/build-001-21fd7abe/`。原STATE clip/state/失败/成功证据和恢复点保留；无整存档恢复、无其他账号读取。旧README存evidence/state-001/README-at-close.md。

## 已实现（尚待原生验证）

### 统一选择与生命周期

ChoicePatches按固定原生签名挂CardSelectCmd非委托叶入口：FromChooseACardScreen、FromSimpleGridForRewards、FromSimpleGrid、5参数FromCombatPile、FromHand、FromHandForUpgrade、FromDeckForUpgrade/Transformation/Generic、cards参数FromDeckForEnchantment、FromChooseABundleScreen。不重复记录外层的FromHandForDiscard或短重载，也没有逐卡重写效果。

ChoiceTape在原始native Task前保存实际输入候选，包装其返回结果；同时核对ReserveChoiceId/SyncLocalChoice，并处理自动/确定分支绕过Sync的真实返回。录制完全沿原生UI，拒绝外部LocalSelector，本mod不设置测试selector/MockSelectedCard。原生自动selector继续运行并核对其结果，未保证所有非确定自动selector均能匹配。

格式4的ChoiceFrame关联全局序号、玩家槽0、初始0边界或当前宿主操作序号、回合、来源模型，保存有序候选/结果、数量/取消/manual/自动路径、原生ID诊断和相对开始/持续时间。候选含模型、升级、可变标记、已注册战斗ID/牌组索引，临时候选用已核对顺序与状态指纹在当前列表中取原实例；名称只作显示，不用同名牌代替身份。bundle逐选项保留卡序。数量上限为有效候选数，另留原始RequestedMax，支持GamblingChip的999999999式上限。

Recorder在进入房间checkpoint后、Playable前即收初始选择，等待实际entry/start-turn任务与稳定态后捕获Initial。后续选择关联Pending操作，回合开始选择随EndTurn边界，连续/多选保持结果顺序；无宿主且非初始的额外异步选择明确拒绝。人思考的选择Scope时长从结算超时中扣除，不以每帧近似计算（避免长时间无渲染后误超时）。

播放先载入tape，再建立原始初始房间，按原始命令生成候选并验证。ShouldSelectLocalCard仅在观看时走等待分支；因为原生WaitForRemoteChoice对Singleplayer直接抛错，适配其Task返回，在当前保留的choice ID与Scope一致时投递PlayerChoiceResult。原有SignalPlayerChoiceBegun/Ended、暂停/恢复GameAction以及牌效不替换。没有往原生_receivedChoices遗留旧完成Task，稳定点要求该队列为空。固定源码PlayerChoiceReceived订阅只有原生CombatReplayWriter，观看时该writer原本禁用；没有因此省略其他已知游戏结算订阅。

轻量UI显示候选、升级与原勾选/空结果，自动投递前约0.65秒停留；不弹手动确认，不让观众更改决定。原始持续时间已留数据，本包不做原始节奏重播或完整配置UI。

### 生成、变化、身份和恢复

Format4解除牌生成/派生自动出牌拒绝，效果继续走CardFactory/CardCmd/CardPileCmd等原生通路；不是把生成结果当第二次手动输入。记录生成/变化完成后的实际模型、战斗ID、升级、目标/实际牌区和指纹，并按原生操作后投影/RNG核对。ActionMonitor追踪派生OnPlayWrapper任务，等待宿主与子任务成功。Format4的CardDetails另含关键字、Tags、保留/Sly、重放值，避免只记录持久化id/upgrade。

既有IdentityMemento原本包含CombatState._allCards、NetCombatCardDb字典/nextID、牌区、模型/能力/历史/RNG；新增牌、浮动候选、复制/变化对象因此进入对应稳定点，恢复时旧卡DB/状态订阅退订，恢复后按实际牌区重新订阅，手牌模型/holder按实例整理。新增选择消费cursor独立保存在DirectCheckpoint，后退时恢复，原生choice IDs保持递增，不把旧signal/Task放回。真正的跨生成/变化回退效果仍须这次实测，纯数据生成替换检查不是UI通过证据。

JossPaper消耗阈值、抽牌、CardsExhausted模数和EtherealCount继续走本体；只在观看时替换DoActivateVisuals的一秒异步_IsActivating显示flag，保留Flash和逻辑hook，正常录制不改。这样回退不会被旧延迟flag回调覆盖。ContentPresentation已有实际计数/status/标签读回，但金纸本包尚未运行。

### 文件与旧能力

新片格式4；Choices=null在旧格式省略，旧格式1/2/3 checksum保持。增加已验收4283890C前驱build精确兼容，其他未知build仍拒绝；clip仍256操作/4MiB，上限内选择最多1024条/每条最多1024候选。本包不改变STATE原片的v1 authority sidecar，也不为所有新选择点构建文件状态恢复；新格式4普通片Authority=null，旧STATE测试录制另沿格式3。

继续使用独立MemorySession/SaveManager、禁用原生replay writer/战斗同步写入及奖励界面、Stop后原manager/Continue/backup/progress比较。当前代码与既有隔离方法相同，本包具体行为需新运行；ShouldSave=false仍不当全写入证明。

## 检查与实际通过状态

Release构建0错误/0警告。`choice-contracts.json`六项纯数据检查通过：初始/操作/回合关联、连续/有序多选与70秒时长序列化、同名异ID/升级拒绝且标签/原生递增ID可变、合法跳过/数量/边界错误拒绝含无界RequestedMax、格式4envelope覆盖结果、生成牌注册表/牌区/逻辑cursor恢复而live ID保持，以及实际旧STATE文件校验保持（若按条目分组，以JSON的6项为准）。另跑既有6项memento回归和真实STATE原clip/state的原hash/关联读取。它们不执行游戏构造器，不证明Harmony安装、人的选择、原生队列/动画或回放通过。

**实际录制与所选组合的顺播、跨选择/生成/变化内存后退、金纸UI及胜利Stop/重开已通过r1/r2。原生窗口及细分点击过程展示未实现；格式4选择点文件冷恢复未实现。两者不混称为本轮已完成能力。**

## 用户例子与机制对应

| 例子 | 本地源码通路/当前状态 |
| --- | --- |
| 攻击药水开牌、Discovery | CardFactory.GetDistinctForCombat→FromChooseACardScreen→SetToFreeThisTurn→AddGeneratedCardToCombat；已挂通用路径，待人工 |
| 许愿Wish、秘密武器SecretWeapon | FromCombatPile（后者filter Attack）→原牌入手；已挂，未实测这两张 |
| Quasar无色开牌、Abundance、Splash | 已查均走FromChooseACardScreen与原生生成/升级/免费修改；Quasar含星数机制，未做完整储君入口实测。用户“储君印牌”中文具体映射未锁定，不以猜名宣称覆盖 |
| KnowledgeDemon | 已查怪物通过BlockingPlayerChoiceContext→FromChooseACardScreen；该机制已挂，Boss入口仍在录制范围外，未实测 |
| ToolsOfTheTrade/Tyranny/Entropy | AfterPlayerTurnStart→FromHand/ForDiscard；Entropy随后TransformToRandom。测试准备含三者，待人工 |
| GamblingChip、用户所称烘焙手套 | 前者初始AfterPlayerTurnStart→FromHandForDiscard，已处理其超大Max。后者中文模型映射未锁定，不造具体路径 |
| 猎人弃牌/连续及多选 | Survivor、Prepared+经FromHandForDiscard→FromHand；Burst让下一技能连续结算。测试准备含此组合，待人工 |
| 金纸JossPaper | AfterCardExhausted/AfterSideTurnEnd→原生Draw/阈值/状态；准备初始计数4，以一次真实消耗触发，待人工 |

## 第一段人工录制

1. 主菜单启用“CONTENT选择测试”，不要启用STATE测试。新建铁甲A0，古代事件选不改变牌组的选项，进入首排普通战斗。工具箱开场选择任一牌，先别打这张赠牌。准备的9张牌应全部能在首手看到（加工具箱共10张），不需要刷种子/掉落。
2. 首回合先打必备工具+、暴政、熵。喝能量药水，再喝攻击药水并亲自选一张攻击牌，先不要打它。
3. 打爆发，再打生存者；完成连续两次弃牌，优先弃工具箱赠牌和重锤+，保留坚毅+、早有准备+、Discovery。
4. 打坚毅+，选择消耗药水给的攻击牌，让金纸由4触发原生抽牌。再打早有准备+，亲自多选弃两张，尽量保留Discovery；能打Discovery时任选新牌即可，不要求打出随机的新牌。然后结束回合。
5. 下回合应连续出现必备工具弃牌、暴政消耗、熵变化，亲自选择，尽量保留重锤+用于击杀。之后正常打赢；不用为了样本拖战斗或反复刷随机卡。
6. 自动保存后不领奖、不去下一房间，保存退出回菜单并正常关游戏。若显示未封存/错误，保留提示退出，不重录。Implementation先核对录像及真实ChoiceFrames，再给菜单观看/跨选择后退、胜利Stop/重开步骤。

## 仍缺失/未证明

首场普通战斗单人铁甲入口仍保留；跨池牌的机制不等于其他角色完整入口已支持。单位/宠物/分裂生成、遗物成员变化、多人、终局后退、奖励/整局、所有额外非CardSelect选择仍不包含。当前同时只接受一个活跃叶选择，重叠并发会明确拒绝；支持连续await选择不等于任意并发。自动selector若非确定导致结果不同会诊断，不无条件伪造为一致。未验证所有特殊目标/能力/预览/异步卡死或所有卡牌隐藏状态。取消整条GameAction仍是未完成操作；命令合法返回的skip/empty/cancel结果才录入。真实集成失败沿本包修复，不为普通bug另开审批或宣布工程退役。

## r1真实录制结果与观看停点

用户按步骤完成所有真实选择及金纸触发，首战胜利自动保存后正常退出。Runtime `50279862bc554878ab3db6830e3376a1`，58事件无failed/error/unsupported，现场game0。原片 `fc6432b4c0af41d69defda73f1770824`，格式4，16操作/15选择，685604 bytes，seed ZBK7REXURWM2、col1双蛞蝓，原build-001/F1D9D349…DB63938未变。

15帧均Automated=false，原生ID0…14；真实初始0边界工具箱选择EternalArmor，随后入手获得战斗ID9。第5步攻击药水选择Uppercut后以ID10入手；第7步爆发后Survivor连续两次分别弃Bludgeon+ ID8、EternalArmor ID9；第8步TrueGrit+选择消耗Uppercut ID10。第9/13/15步Prepared+分别保存两项有序多选[1,3]/[2,0]/[2,1]。第10步Discovery选择Breakthrough并以ID11入手。第11/14步EndTurn分别收三次回合开始选择（Tools/Tyranny/Entropy），Entropy先把Survivor变成CorrosiveWave ID12，后把Breakthrough变成Juggling ID13。记录有3次生成、2次变化完成事件，未只保存选项序号而忽略实际模型/身份。

金纸从第7步后的Saved.props CardsExhausted4/Status Active，到第8步后的省略默认0/Status Normal；Uppercut实际进入exhaust，手牌数量由4经打牌/消耗/抽1到3，draw0经重洗/抽牌到3，discard4到1（最后TrueGrit进弃牌），和原生阈值抽1一致。第10/11/14步后计数分别1/2/3。操作后与初态的ChoicesConsumed均和截至对应边界的真实帧数一致，最后胜利State为15。原始事件日志中的SavedProperties可能因通用JSON省略字段，计数结论取原文件After的原生序列化，不拿props空对象当零值。

独立纯.NET读取原片，原envelope/hash、格式4约束、16/15计数、所有边界消费位置、人工/连续/多选/回合开始帧、金纸4→0均通过，原文件不改；见 `evidence/content-001/recording-files-r1.json`。这项实际文件检查仍不是观看运行通过。安全帧数据 `recorded-choices-r1.json`、58事件 `runtime-r1.json`，原checkpoint只在私有clip中，不公开。

新增正常游玩进度完整恢复点 `C:/Projects/STS2ReplayMod-local/backups/CONTENT001-20261027-145813-b34d252680/`，1279文件/132049476 bytes，来源/副本SHA全部读回，原活动账号匹配、game0，另一账号排除、无整存档恢复/换DLL。原clip和原session逐SHA私有归档其runtime/content-001-r1；收据recording-archive-r1.json。后续仅观看时复用，不重复全账号备份。

第二段请求：正常“打开最近片段”，开场自动使用工具箱原决定后暂停0/16；用Step到5、7、8、9、11等边界各后退一步并重做，分别核对药水生成/连续弃牌/金纸/多选/回合开始三选择及变化；后续Play到16真实胜利Stop，重开短Step/Stop，正常退出。无需再录，选择应自动使用原决定，观看者不另选；初始工具箱选择发生在0点之前的加载阶段，不是STATE文件入口。等待实际顺播/后退/显示/隔离事件后更新报告。

## r2实际观看和恢复核对

用户确认开场/后续选择自动、指定后退重做、胜利、Stop和重开均正常并已退出。Runtime `d36918a5c54245109e7a02bdc1d32017`，PID43544/start UTC07:10:15.2036881Z，共438事件、0失败/差异。原片fc6432b4…不变，build-001/F1D9D349…运行。`evidence/content-001/runtime-r2.json`与带断言汇总`runtime-verification-r2.json`分别保留安全日志和审核摘要；原始438事件逐文件SHA归档在CONTENT001恢复点runtime/content-001-r2/session。

- 两次加载均在Initial匹配前自动重放工具箱原决定，初态后到0/16，第二次新会话也如此。36个choice-delivered与36个choice-completed逐条核对原frame.Sequence/Boundary/Chosen，全部Matched=true；第一次包含多次回退后重做，第二次只有初始选择。每次当前原生ID递增，与旧录制ID不强行相等；19次恢复均选择逻辑cursor回滚、原生_receivedChoices空、live ID保持递增。
- 36个真实操作全部After/RNG匹配，首场完整到16真实胜利、原生VictoryTask成功、奖励拦截；第二次重开Step到1 Stop，AllOperationsMatched=false是主动短Stop且Error=null。两次closed均原SaveManager恢复，所比较Continue/backup和主progress文件/内存progress保持。
- 19次直接恢复全部StateMatched、0prefix/无room重建，包含5→4两次（药水/生成撤销）、7→6一次（连续选择撤销）、8→7三次（金纸/消耗）、9→8两次（多选）、10→9两次（Discovery生成）、11→10三次（回合开始三选择及变化），以及连续回到0等。重做第5/7/8/9/10/11步分别3/2/4/3/3/4次；额外较早能力/能量操作按日志回归，不增加旧矩阵。
- 实际生成8次（工具箱2、攻击药水3、Discovery3），变化5次（第11步4次、第14步1次），金纸阈值/抽牌实际4次；19次ContentPresentation卡/药水/遗物计数/能量读回通过。它们支持所选生成/变化路径及相应显示，并不是任意生成卡/所有视觉路径证明。
- 原录像byte hash与r1完全一致，game0。相对录制后完整恢复点检查53个modded1/saves文件：主存档和主progress保持，唯一变化为progress.save.backup，其当前字节与未变的主progress完全一致；该后副本另行保存/hash读回。与原生加载时刷新备份一致，但没有时间点日志，不断言仅凭closed能定位具体写入时刻。MemorySession只比较主progress，不包括这个progress备份；**不能声称53文件全部未变或没有任何原生写入**。原前副本在完整恢复点，后副本在runtime/content-001-r2/progress.save.backup.after.private，不恢复覆盖它。

## 用户反馈和交付停点

顶部升级牌标题双“+”来自原生Title带后缀而ChoiceTape又手工追加。build-002仅使用原生Title；Release0错误/0警告、六项选择数据检查通过，包含标签可不同但身份/升级/指纹必须一致的原检查。保留已实测F1D9 pair，当前F658已安装，未改原片/期望/格式，也未扩新的核心录播行为；小显示修正不重跑完整战斗矩阵。新DLL尚无新的实际观看记录，后续使用可观察文字，不以编译代替视觉验证。

用户提出显式展示选择窗口、按选中/撤选/确认等多个动作呈现选择过程，并授权向PM汇报。提议见 `CONTENT-001-choice-ux-proposal.md`。当前格式4只有最终返回结果顺序与scope时间，没有完整点击/撤选/hover轨迹；旧片不能被表述为已录到原始交互。当前Step仍为宿主操作整体，回退只到稳定点。下一范围由PM与用户讨论，Implementation本轮没有自行扩选择中途seek/过程事件，也没有把文字展示称已经满足该新期望。

交付源码、候选/已测pair、README、r1/r2原始及安全证据；测试期角色/房间/并发选择/特殊生成等限制仍见前文。所有覆盖结论按实际机制组合，不是全牌库/全部角色/所有取消/自动selector/完整UI过程或选择点磁盘恢复完成。管理状态与验收由PM维护。

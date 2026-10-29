# 当前项目状态

更新：2026-10-30。PLAYBACK-003/build-001参考猎人完整27项/胜利Back/重开，以及另一旧铁甲格式6片的普通recent入口完整胜利/显式Seek续播已通过；源码e13d1f4、r2报告e639042已推送。本轮新录与其普通入口重开仍待验证，本包不关闭。PM覆盖静态材料已交，仓库按D-129公开。旧PLAYBACK-002/003验收保留，旧PLAYBACK001/003仍Unknown。

## 玩家现在能用到什么

已有单场战斗回放原型，还不能录制/观看整局。PLAYBACK-003已在参考猎人完整27项验证原生前进、真实胜利后Back再胜利与退出重开；精细与完整操作导航、真实队列取消重排另沿PLAYBACK-002已测范围保留。CONTENT旧固定组合不自动算新版所有内容已回归，全内容/发布性能未完成。

当前PLAYBACK-003/build-001已将普通“打开最近片段”接入统一原生核心，并解除固定HunterClip/22项限制。r1参考猎人27项、r2普通入口另一铁甲13项均完整播放，已取得不同ID/长度的真实入口证据；后者仍为10月28日旧录像，本轮新录普通入口闭环仍待验证。

在已经支持并实测的战斗范围内，可以：

- 自动记录真实出牌、结束回合和用药，胜利时保存录像，包括最后一次击杀操作。
- 从主菜单打开录像，播放、暂停、逐操作前进、停止和重开。当前还是测试性质的入口，不是完整录像库。
- 最新固定猎人入口区分精细前进与完整操作，暂停可将合法预约卡真实入队；自己的选择触发后排退回手牌，结算后按原请求自动重入队且只执行一次。选择页可沿流程观察，但不是直接Seek目标。
- 显式Seek保持请求时播放/暂停和Auto状态，同时清旧路线请求；Back仍按完整操作撤回并暂停。精细停在选择后可直接Play续当前，无需补Step。这些以003/r2所测路径为限。
- 在同次观看中，直接退回已经访问过的完整操作边界，再继续原生播放；选择阶段可精细前进/暂停，但不是直接Seek目标。所测路径恢复真实状态，不需要重开房间或从头重播。
- 回退所测出牌、抽牌、回合切换、能力、药水和遗物计数；双蛞蝓样本还能撤销其中一只敌人的死亡，恢复原身份、目标顺序及另一只的强化和意图。
- 在已测打开/停止路径中，使用独立回放会话，退出后恢复原manager，所比较的普通Continue与进度保持。局部实验成功不等于完整产品所有路径已经安全。

已测内容还包括工具箱开场选择、攻击药水开牌、爆发后的连续弃牌、有序多选、Discovery生成、必备工具/暴政/熵的回合开始选择和变化，以及金纸回合内触发。真实16操作/15人工选择录像，两次打开中36个自动选择/36操作/19次直接后退匹配，含胜利Stop与重开短Stop。PM逐次对照已录结果、重算光标，见[验收](../evidence/content-001/project-lead-review-2026-10-27.md)。这不代表全部卡牌、遗物、药水或组合都支持。

CONTENT-002已收束：默认展示最终确认的选择，原始41事件保留；所测路线支持选择前后双向导航、实际数据恢复与历史纯展示，Step按正常时钟等待语义边界。用户确认与各build证据分开，见[阶段验收](../evidence/content-002/project-lead-review-2026-10-28.md)。旧003/r2的446事件只代表当时范围；该包关闭时缺少的连续选择路线/普通选择中Stop，现由CONTENT-003/r2在猎人固定组合中取得专门证据，不反写为旧020全矩阵通过。

## “观看时后退”与“文件里的强记录”

这是两项不同能力，介绍进度时必须分开。

**观看时后退**：当前主要依靠观看会话中的内存状态，能回到已经看过的多个稳定点；退出游戏后，这份内存不再存在。这不等于录像文件已经为每一步保存完整可恢复状态。

**从文件恢复与纠偏**：STATE-001已证明，正常录制时保存的“第一回合、第一张未升级防御结算后”状态，可以在新的游戏进程中直接恢复，无需先执行第一张牌。刻意把能量2改为3、格挡5改为7后，播放器实际检出了偏差，重新读文件恢复为2和5，并能继续原录像至胜利；恢复没有用旧内存快照或硬改常量代替。

该文件能力目前只验了这一个操作点和这两类受控差异，不是所有步骤的自动纠偏。当前测试入口还要求与录制不同的进程/Runtime；从文件直接进入第1步时没有第0步内存点，因此该入口1→0不可用。能力/药水之后的文件恢复、多回合冷恢复、任意seek和原生卡死救援仍未证明。

## 当前范围与缺口

当前固定STS2 v0.111.0、仅ReplayMod、默认modded profile1。入口开放铁甲/猎人/储君A0首幕首排普通战斗、无额外run modifiers；储君格式5资源/时间与观看恢复按固定组合验收。格式6允许接受/完成交叠，既有铁甲格式6的普通入口完整播放和Seek续播已在PLAYBACK-003/r2通过，不代表所有排队组合；其他角色未开放，Continue不录制。旧格式1–4时间未知，原片校验保持；STATE-001仍铁甲格式3固定文件点，固定组合不代表全内容通过。

还没有面向玩家完成：

- 后续战斗、精英/Boss及其他角色；连续一幕/整局、奖励、商店和事件。
- 全卡牌/遗物/药水覆盖。通用CardSelect叶入口及所测生成/变化已接入，部分派生自动出牌路径已放开但不代表所有调用端通过；其他选择种类、并发选择、新单位和遗物成员变化仍有限制。范围外明确诊断，不默认为完整录像。
- 召唤/分裂/宠物、玩家失败、任意换形和一般终局恢复。参考猎人最后手动攻击胜利后的Back/原生再击杀已实测三次；不外推结束回合或其他派生效果胜利、任意终局的可续接性。
- 完整时间轴和楼层路线图、原始节奏/分类配置节奏、录像库与分享流程。
- 可发布的性能保证。目前仅有小范围真实路径观察，不重建通用性能测试平台。

用户报告的拖牌取消后换位、凋萎与抽牌互卡尚未复现。官方历史修复及本地源码支持这类异步时序风险存在，不能把具体根因写成已确认。见[动画与时序笔记](technical-notes/NATIVE-ANIMATION-TIMING.md)。

## 当前决定与下一步

[PLAYBACK-002](handoffs/PLAYBACK-002.md)按D-127阶段收束并保留003。用户现已授权Implementation实施[PLAYBACK-003](handoffs/PLAYBACK-003.md)：去掉固定原片/22项限制、正常入口用新核心、完整至胜利/Back/Stop及新录文件重开；普通修复/相关重构沿D-092。PM同时只读盘点战斗内容覆盖，参考现有CONTENT-COVERAGE而非从零重复调查，可向用户问代表机制/样本。覆盖研究不是实现前置，不扩本轮至整局/全内容。

PM首轮覆盖材料已写入[CONTENT-COVERAGE](technical-notes/CONTENT-COVERAGE.md)，基线固定891e5ff，不混读并行新代码。已静态确认丢弃药水独立输入未接、充能球比较缺口、新单位/宠物/遗物成员增减显式拒绝、失败局/后续战斗等限制。用户提供知识恶魔、三鼠召援、产卵虫/孵化、千足虫复活、实验体转阶段；均已静态定位，后四例区分新实例加入、原实例变化和倒地留场。对应动态时点/录播恢复未测，不要求现在开游戏，不阻塞或扩大PLAYBACK-003。

覆盖扩展调查已形成[BATTLE-COVERAGE-PLAN](technical-notes/BATTLE-COVERAGE-PLAN.md)与可复现索引脚本：15类/1539源码模型、950池引用、2094命令引用、80 Act→遭遇/135遭遇→怪物引用，仅作静态检索。注册表5角色、单人过滤、事件战斗、5类球、Osty生成/复活、被动仙灵/蜥蜴尾、分流RNG及StableShuffle排序已定位；输入/状态/生命周期契约与候选批次明确。用户经验明确Undo为多人有、单人无，当前不加单人输入；真实时序/恢复/文件/性能验证仍单列，不宣称所有战斗已支持。

PLAYBACK-003/001首轮已验：PM读取报告/摘要及用户“这个好了，我接下来玩一会，你继续你的任务”，并从封存420原事件重算首次1..27、31次原生结果/RNG全匹配、4次真实VictoryTask/奖励前投影匹配和3次胜利Back26暂停清量。两Stop在27/1，无Error/Outstanding，原manager及所比较Continue/进度恢复；r1两次Entry均hunter-reference，r1自身无recent证据。启动Invalid Task ID及退出资源诊断保留，不称引擎日志无警告。见[r1摘要](../evidence/playback-003/runtime-verification-r1.json)。

10月30日r2已验：用户“应该好了？”，PM读报告/摘要并从封存244事件重算普通Entry=recent打开铁甲13项，首次1..13胜利、显式Seek5暂停/Auto开启/清量且状态/RNG匹配，再6..13胜利；21次原生结果/RNG、2次真实胜利Task/投影及Stop13均通过，原manager/所比较Continue进度恢复。该文件创建于10月28日，录制版本不同，不称本轮新录；Seek不是Back，只有一次打开。见[r2摘要](../evidence/playback-003/runtime-verification-r2.json)。

下一步仅补同版真实新录→普通入口完整播放→整项Back/续播→Stop短重开。游玩后当前51文件补充备份已由Implementation完成并读回，r2读取时game0；沿现有候选继续，无需换DLL或再测旧片。备份是已知当前文件补充，不称新完整账号快照。初始选择/全部内容和其他胜利形态仍未证明，覆盖研究不扩本包。

PLAYBACK-002/build-002已实际测试：r1摘要867事件/2 opens，48次原生结果/RNG、10选择、20匹配恢复、8次普通提前接受；两Stop在4/22无Error/Outstanding，原manager及所比较Continue进度恢复，用户总体认可。选择取消/归手/重入队均0，20/21/22只是Auto串行；精细仅无选择操作4，不能认选择精细/直接Play/混排真人通过。见[更正后摘要](../evidence/playback-002/runtime-verification-r1.json)，PM读摘要与真实用户消息，未重计867原事件；最初消息ahead误报0已更正为8。

003/r2首次证实真实选择退队重排：generation2暂停接受20/21/22（原生ID30/31/32），21选择3取消未执行的32且保留请求/原生归手；21连续选择/必要结算结束后22以35/Attempt2沿同请求25重入队，仅匹配完成一次、结果/RNG正确。摘要806事件/2 opens，57接受、11暂停接受未开始、52原生结果/RNG、16选择、11到达匹配，显式Seek四组合及Back暂停、21两次Continuation均有记录。两Stop在20/9无Error/Outstanding，原manager/所比较Continue进度恢复；其余4次取消是退役，不计选牌退队。见[r2摘要](../evidence/playback-002/runtime-verification-r2.json)，不认全部混排/全场/性能通过。

D-123至D-126的现行规则见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)：普通Step/Back以完整操作为单位，选择自动按录制回应；精细阶段非直接Seek目标。Pause可浏览、Play续当前，后继退队后仅恢复已预约项。以上已有固定样本；一般混排、更多内容及队列窗口扩大仍待后续，不以原生OOP接口存在当全覆盖证明。

固定v0.111.0静态核对：等待选牌按玩家队列处理，队友类星体不必阻塞自己的早有准备；自己的早有准备进入手牌弃牌选择时，会取消本玩家尚未执行的排队出牌。因此录像待播请求与原生已接受动作需区分，三选一等保留各自原生策略。见[原生核对](technical-notes/PLAYBACK-EXPERIENCE.md)。本次静态结论不代替真实取消重排验证。

选择按录像自动回应、Pause控制与整项导航已由D-123/D-124确认；[选择窗口建议流程](technical-notes/PLAYBACK-EXPERIENCE.md)中的聚焦、阅读停留/视觉提示仍是配时建议，未锁定具体时长。减少现有代理层的可行性待实现核实，不把产品规则写成代码已完成。

操作、展示阶段与稳定恢复锚点继续区分：精细前进不增加选择恢复对象，旧宿主仍需安全退役；不据产品简化承诺瞬时Seek。聚焦/阅读时长以实现便利和实测收敛，不建设额外动画平台。

导航当前基线：D-121独立运行/Auto/请求，D-123完整操作与整项Back，D-125另有精细阶段但选择不直接Seek。D-126暂停可真实预入队、Play才执行；显式Seek保持Running/Auto但清旧计划，Back到达仍暂停。D-118效果自然收尾/背景浏览保持；见[体验基线](technical-notes/PLAYBACK-EXPERIENCE.md)。

[SEEK-001](handoffs/SEEK-001.md)按原范围阶段收束，PLAYBACK-002从保留源码接续而非重跑旧任务。恢复后正常重执行业务/RNG并核对等价，不套旧纯显示RngUnchanged；只读/隔离/不复活旧Task保持。旧选择点局部重建是历史证据，D-125新入口不再提供选择页直接Seek。未访问位置、精确帧、通用文件与跨房间仍待后续，不并入本轮。

SEEK-001/002 r1两Runtime/1070事件、3次打开；Implementation摘要记41原生宿主结果-RNG核对、38新选择上下文/提交、22次到达匹配，3次退出无PendingHost且原manager/所比较Continue进度恢复，见[r1摘要](../evidence/seek-001/runtime-verification-r1.json)。仅猎人片前4宿主/9节点、同战斗与已建索引，无旧trace.Play；不是全场/通用Seek或多牌排队通过。PM读取摘要/报告及关键源码，未重计全部原事件；001未运行、003未反馈结论保持。

003/r2用户确认符合预期。Implementation摘要626事件/2 opens、16原生结果-RNG匹配、14次匹配/暂停/清旧余量到达、7次选择布局核对；有限三步、量尽门保持、暂停保留4步再恢复、Auto独立及两种Stop/短重开通过，见[r2摘要](../evidence/seek-001/runtime-verification-r2.json)/[报告](investigation-reports/SEEK-001-report.md)。关闭节点4/1，无PendingHost、控制全清、原manager/所比较Continue进度恢复。PM读用户消息/摘要关键项，未重计626事件。Auto与有限量同时非零去重仍只合同、Auto Pause在末端前、003仅1次EndTurn；不认完整矩阵/多牌入队/整场胜利。选择重建等待按D-120暂接受，不承诺发布性能。

## 旧路线已获得的执行进展（供新包参考，不续跑清单）

CONTENT-004原片cd3baa3b1be7466f9cb94d385c3db420已实际录成13操作/3回合，Stars3→6→4，剑13/克隆14的降费/重置与手里/弃牌18→33均核对；两剑IsDupe=false，双持单候选自动选择，非人工UI覆盖。13组接受/完成时间已真实落盘及生产读回；24.833秒是指定两操作间全部等待，不分离成精确5秒。旧片时间未知、原始节奏模式未实现，见[r1复核](../evidence/content-004/runtime-verification-r1.json)。

最新008/r7两runtime为2453＋402事件，各完整13步胜利/正常Stop，68 Back、57历史显示RNG保持、127恢复核对，原manager/Continue/进度比较恢复，原片不变，见[r7复核](../evidence/content-004/runtime-verification-r7.json)。只认已保存最新进程Godot日志，旧进程原生日志覆盖不认；既有预载/退出诊断保留。007的叠放与更早版本结果各自归档。

008已去掉开悟不可见消耗清理的额外2秒，用户确认正常，但通用exhaust cue后>0.8秒仍存在；Play还检查Move结束后0.55秒，前沿可重叠业务、历史/选择常额外等待。其他mod门槛有全手牌2px/0.5度、连续3帧、选择PlayPileTween/0.2或0.36秒ready、Forge/终点退场及历史预热/恢复。它们有重叠，不能简单相加；‘无逐卡冷却表’不等于‘无固定等待’。完整清单见[报告顶部](investigation-reports/CONTENT-004-report.md)。

固定原生接受卡牌时先非await入队视觉，再RequestEnqueue，业务按序执行；当前mod最多存一导航意图、单Pending且要Playable/显示稳定/可Back节点才提交下一操作，录制同样有该假设。快速原生输入队列未被现有逐步样本证明，不能只扩大Step缓冲。最新Finish各段约0.8–1.36秒是业务后展示检查，不是整动画/点击首帧。当前事实与建议见[播放手感讨论稿](technical-notes/PLAYBACK-CADENCE.md)。

用户已确认D-117：每次Step计一现有操作/选择节点，连点累计，Pause/Back/Stop清尚未提交请求；已提交业务按安全边界处理。现签[PLAYBACK-001](handoffs/PLAYBACK-001.md)，先做普通两牌真正原生连续入队，区分输入/动作归属/完成/恢复点，验证累计Step和录制/首次/历史/Back/Stop。仅扩Step请求容量与自然提交条件，不靠快进跳动画；具体队列恢复仍待证明。相关普通bug同包迭代，不另开INV/逐阶段审批。

PLAYBACK-001/001的真实13操作胜利片3eb6baeb507043259b4e59562ae510a5已封存，格式6/SHA2C278C4F…126CC8。Implementation r1摘要记60事件、13接受/执行/完成，首两防御实例3/2，能量3→2→1、格挡0→5→10；第二接受时前原生Task未完成，交叠32.8004ms，生产读回一致，见[r1摘要](../evidence/playback-001/runtime-verification-r1.json)。原生日志6 InvalidTaskID与12退出诊断保留，未据此定因或称全干净；PM本次读摘要/报告，未重数全部原事件。

002/r2实测FAIL：461事件、8 opens/Stop，4次提前提交第二防御，原生完成2次而外层仅核对1次，卡在1/AwaitingEngine；历史播放0、无完整胜利。Implementation定位累计请求已被预提交消费，DecisionCursor已EngineWait而PlaybackCursor.TryBegin仍要求另一_step；源码/生产游标合同覆盖此旧失活形态。8次原manager/所比较Continue进度恢复不代替流程通过，见[r2失败摘要](../evidence/playback-001/runtime-verification-r2.json)。

003只让当前本会话已Committed的宿主进入外层核对，不重复扣请求/派业务；Stop drain新增已提交真实结果/胜利核对。离线通过、原生未验；下一缩为同新片三击精确到3、Back两次到1、Step到2(E1/B10)、Stop正常退出，先解除阻塞并触及历史，再同包继续显示/取消/终点和既有选择回归。提前入队仍仅格式6同回合无选择非终结Self/None技能单张lookahead，格式6去自动0.55，末动作/选择/终点保护保留；不把002未触及的历史尾部称通过，不重录/重测008或加审批。

MVP目标仍为自动录制、尽可能覆盖单人场景、独立Replay入口、不占用正常Continue、双向导航和可配置节奏。全部卡牌、遗物、药水是覆盖目标，固定样本通过不缩小该目标。多人为MVP后第一优先，跨版本为后续大目标；接管游戏不在MVP内。

## 当前工程与恢复点

- 可演进工程：[src/ReplayMod](../src/ReplayMod/README.md)。已有本地反编译C#缓存，日常定位/hook源码优先，IL仅用于歧义，不逐方法复证。
- Git：公开仓库[SanweiTreap/STS2ReplayMod](https://github.com/SanweiTreap/STS2ReplayMod)，2026-10-29按最新用户要求从private改为public，API及无凭据Git读取已确认。原始证据/存档/构建/第三方快照仍仅留本地，见[协作规则](REPOSITORY.md)。开发快照不是发布。
- 当前安装PLAYBACK-003/build-001，SHA-256 `378309A7604BDC0CFE462BA18164F0CE7528F4CA1ACEF35304AE5B53B8E3CA7F`，[收据](../evidence/playback-003/preparation/build-001-f7f907e1.json)，源码e13d1f4已推送，PM读回安装匹配、运行Pending。上一已验PLAYBACK-002/003（8D7B…B4AA）pair/806事件保留，D-127结论不变；该包未运行的001中间版不记真人PASS/FAIL。
- 上一已验SEEK-001/build-003（SHA `1D2A4848166951E9623015DEBE37744E9424F3CF37CCE33BB0BFB99F867AB919`）pair/源码/原片/旧失败保持；D-122所测验收不变，[旧收据](../evidence/seek-001/preparation/build-003-9705e1cb.json)。新旧结果分开，不补判SEEK/001或旧PLAYBACK-001/003。
- 最新完整恢复点按收据为`C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，1280文件、132063795 bytes，来源/副本读回且来源稳定。Implementation按真实清单复核r4的54个saves内容保持；旧r2汇总53是沿旧点硬编码，原比较循环覆盖新清单54项，已加注更正，原日志不改。内容相同不证明无同值写入；更早progress备份差异/前后副本与全部重要原片/事件保留，PM不重复整账号审计。
- 部分快照：同恢复点下CONTENT004-current-saves-20261028-150051为41文件/4471059 bytes；PLAYBACK001的203942快照49文件/4348913 bytes、最新20261028-223031快照51文件/4439940 bytes，Implementation记录已知save/config/native replay父目录来源稳定及逐hash读回。它们不替代旧完整恢复点、不表示重审历史run；未执行恢复/扫描另一账号。
- 当前Implementation对话：`01a17814-d254-7df0-8c01-affe43d0e04c`，从已验PLAYBACK-002/003接PLAYBACK-003完整单场；PM并行维护覆盖笔记/状态，不碰源码或安装。旧PLAYBACK-001/003不补判。
- 实验文件格式有精确身份/已知前驱build限制，不代表一般跨版本播放。

## 有效开发授权与工作方式

沿用D-092：先备份，允许较大胆的mod/hook、相关代码成批修改及同任务普通bug修复，不再为每次失败退役整份代码、另开调查或逐阶段申请PM批准。unmodded profile1不再禁止读取/复制；默认测试modded1，另一账号不纳入。备份不能替代最终产品对普通Continue/进度的会话隔离。

游戏由用户手动启动和操作，不使用Computer Use、替代GUI自动化或自行启动headless游戏。换DLL前确认游戏正常退出；Cloud现场确认，不自动更改。保留可用安装，不自动卸载。细则见[WORKFLOW](WORKFLOW.md)和[TEST_PROTOCOL](TEST_PROTOCOL.md)。

## 证据与长期记忆入口

- 当前[PLAYBACK-001工作包](handoffs/PLAYBACK-001.md)/[实现报告与录制步骤](investigation-reports/PLAYBACK-001-report.md)及[手感/等待笔记](technical-notes/PLAYBACK-CADENCE.md)；已关闭[CONTENT-004验收](../evidence/content-004/project-lead-review-2026-10-28.md)/[实现报告](investigation-reports/CONTENT-004-report.md)，此前旧包证据边界保持。
- 已测组合与中途死亡：[DEMO-005报告](investigation-reports/DEMO-005-report.md)、[DEMO-004报告](investigation-reports/DEMO-004-report.md)；早期内存后退与完整首战见DEMO-002/003报告。
- [下一任务清单](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[决策](DECISIONS.md)、[调查台账](INVESTIGATIONS.md)。
- 本次压缩前的完整技术状态：[2026-10-27状态归档](archive/2026-10-27-state-001-accepted/CURRENT_STATUS.md)。旧r2/r3失败、日志hash命名差异、具体操作计数和旧阶段结论均保留，不追认失败为通过。

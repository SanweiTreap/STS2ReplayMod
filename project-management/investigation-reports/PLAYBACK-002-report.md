# PLAYBACK-002：精细前进、完整操作与原生退队重排

2026-10-29，D-123–D-126。当前 **build-003/r2所测暂停入队、选择退队重排、Seek意图与精细直接Play已有真实通过证据**。用户确认“整体比较符合预期”，另询问入队动画是否原生，已据调用链说明原生队列Tween与回放直接提交的区别。本轮保留003，不改动画／DLL或追加测试，结果交PM按范围验收。002/r1原来未证的结论保留，不反写；旧SEEK-001/003验收与旧PLAYBACK-001/003 Unknown保持。

## build-003 / r2 实测闭环与动画来源

runtime `cf723a82c06c49d7966f8a96c31e5d76`，806事件、两次打开，hash8D7B0C…B4AA；57次真实接受，其中11次明确暂停接受且BusinessStarted=false，52次原生开始/完成结果-RNG核对均通过，16次原生选择，11次Seek/Back状态及请求时Running/Auto匹配。显式Seek四种运行/Auto组合都有记录，Back仍暂停；精细停点之后直接Play的Continuation目标在21完成两次。两个Stop分别完成20/9，Error=null、Outstanding=false、控制/请求清空，原manager及所比较Continue/进度恢复。没有把这两次描述为固定步骤1的短重开或全场胜利。读取时game0、原片hash保持。

真实A/B/C证据：generation2，19:30:23–25三次暂停接受20/21/22，原生ID30/31/32均未开始业务；Play后20正常完成。21的选择3在19:30:54取消C（ID32，Retiring=false、StillReserved=true、BusinessStarted=false），原生返回日志明确打击仍在逻辑手牌且有手牌holder。21及连续选择/自动链结束在19:30:56；随后C以新ID35/Attempt2重新提交并开始，ReenteredAfterNativeCancellation=true，同generation中ID35仅完成一次，结果/RNG与原记录一致。请求仍为原ID25完整目标，没有再追加/重复消费。其余4次取消是Stop/Seek退役，不计为选择退队成功。本轮在固定真实样本中首次证明所要求的原生接受→选择取消→归手→必要结算→重入队→C单次结算闭环，不外推所有卡牌/队列。

原始806事件和最新Godot日志在固定恢复点`runtime/playback-002-r2-20261029/`逐文件哈希核对保留；摘要`evidence/playback-002/runtime-verification-r2.json`，独立列出取消原因/新旧ID、同代完成次数和Seek四种意图。模组事件中无失败；没有据此宣称整个引擎日志绝无警告。旧PLAYBACK003资料未读。

动画回答：当前生产提交由`PlayerLoop.SubmitOperation`调用原生CardModel.TryManualPlay，原生ActionQueueSet.ActionEnqueued触发NCardPlayQueue.OnActionEnqueued→OnLocalCardPlayed→TweenCardToQueuePosition。固定反编译版本后者以0.35秒Cubic/EaseOut同时更新队列位置、缩放与alpha；本模组没有patch/替代该队列Tween。Mod控制何时接受与允许业务开始，入队表现使用原生流程。回放没有先执行鼠标拖牌的StartCardPlay/BeginDrag准备过程，因此“入队动画是原生”不等于起始姿态/鼠标轨迹与手动出牌逐帧一致；本轮用户仅询问来源，未据此擅自重写动画。

范围仍为原片前22宿主、当前战斗和已访问完整边界。更广原生队列覆盖、精细/完整所有混排、原始时间/楼层导航/全游戏通用恢复与性能不在本次结论。当前无新增人工要求；后续由PM/用户决定。

## build-003：D-126 窄修订准备历史（待测表述由r2取代）

普通完整请求FIFO中的连续真实卡牌，现在可在运行门暂停时提交原生TryManualPlay／ActionQueueSynchronizer／NCardPlayQueue，窗口最多为当前项及后两张（3项），不消费/完成导航请求，不扣资源或运行牌效。真正开始仍由GetReadyAction中的运行门和目标归属控制。Fine、EndTurn、药水、未来生成或当前不合法项阻断窗口，不越序；自身选择中不挪实际候选，并记住选择所属宿主，直到整个宿主必要结算结束才恢复新接受。接受时解析当前原实例及原生合法性，完整录制前态/资源/升级/动态费用/实例仍在实际开始时核对；遇目标暂不能按原目标序号解析则留待提交，不把ahead尝试变成播放错误。

条目明确区分`已原生入队`、`尚待提交`、`退队待重排`与`结算中`。未开始动作不计结算超时，计时在实际开始时重置；否则长时间暂停观察原生队列会误报60秒超时。正常录制的接受时间记录不变，修改的是结算监控期限。暂停期间只接受，不启动的实际证明仍待003日志。

显式Seek请求记录不可变的Running/Auto意图；退役/恢复期间可关闭门，但最终`OperationRequests.Restore`恢复请求时两值，清旧Active/Pending且不重新带入旧FIFO。运行+Auto开会从目标自然继续，运行+Auto关且无量则留在边界等待，暂停仍停。重复点击尚未开始恢复的Seek也保留最初的运行意图，避免读取临时关闭门。Back单独带暂停意图、Auto保留。到达日志分别记录IntendedRunning/Auto、实际值和Back，UI显示`Seek · 保持播放状态`。精细最后目标观察暂停、直接Play仅续当前规则保留。

Release/direct构建0警告0错误；9组生产规划/原片读取合同通过，新增显式Seek四种运行/Auto组合、恢复期间临时门状态不覆盖请求意图、Back仍暂停、暂停接受不授予业务且Play仅消费一次的检查。见`evidence/playback-002/checks/build-003/operations.json`。未启动游戏，原生暂停接受及选择取消/归手/重入队仍待实测；002/r1的8次ahead不当003新行为证明。

003 DLL SHA256 **`8D7B0C2D8061648DF8A047387C11B879E34D8FB862A52BEC626366C796B7B4AA`**，收据`evidence/playback-002/preparation/build-003-e2156e8e.json`，pair在`evidence/playback-002/builds/build-003/`。002 pair/settings/selector保留在固定恢复点`preparations/build-003-e2156e8e/`，867原始事件/Godot已封存，当前源码/合同/报告在该准备点封存。换DLL前后game0、固定游戏hash/原账号核对、本批Cloud有效off，只ReplayMod/modded1；没有存档恢复或自动游戏启动。001未运行、旧包003 Unknown不变。

### 003最短人工步骤（替代002旧步骤）

同一新入口、同一原片，不重录。先开启Auto并Play到切片22建立本次完整边界索引：

1. 此时暂停，Seek19应仍暂停、Auto仍开。关Auto，点三次`>>`，三张牌（20爆发／21生存者／22打击）应真实显示在原生队列，仍停19且资源不变。Play；在弃牌窗口可Pause观察原生退回/候选，Play后应自动重排C并只执行一次到22，不补Step。
2. 再次Seek19，开Auto（仍暂停），把下拉目标先选20；Play后在选牌期间点Seek，恢复20后应自行继续到22，无需再按Play。这验证运行+Auto的Seek续播。
3. 关Auto，Seek20仍暂停；点Play（无请求）应等待，运行门开。再Seek20应仍保持运行但不自动出牌。此时点`>`应直接进入选择并按精细目标暂停；只点Play应补完21、不会打22。最后Stop；短重开`>>`一次，检查暂停入队但未出牌，Stop并正常退出。中途失败则退出，不补旧矩阵。

## build-002 / r1 实际结果与待讨论建议

runtime `344955f36ff44f449255eddf0c0dc43f`，867事件、两次打开，hash74C88E…E2E0F。48次原生接受／开始／完成及原结果/RNG核对，10次本地原生选择，20次操作边界恢复匹配。第一次在已建立0..10索引内多次整项Back/Seek，最后完成4处Stop；第二次正常Auto至22并Stop。两个关闭均Error=null、Outstanding=false，控制/请求清空，原manager及所比较Continue/进度恢复；读取时game0。原始867文件和最新Godot日志在固定恢复点`runtime/playback-002-r1-20261029/`按哈希保留，摘要`evidence/playback-002/runtime-verification-r1.json`。

用户“基本符合预期”按总体体验记录，不能扩为全部包验收。本轮有8次Ahead=true原生提前接受，取消、归手及取消后重入队事件均为0；20/21/22实际是Auto依次执行，没有19后三个显式完整请求的序列，**真正原生退队重排/单次结算仍未证明**。先前交PM时误将20/21/22没有ahead扩大成全runtime，已明确纠正。实际精细请求只有一次无选择操作4，精细选择页／直接Play续当前／混排的真人路径也未见对应记录；已有离线合同不改写成这些原生路径通过。普通完整请求处理内部两次弃牌、暂停预约、完整边界回退和Auto至22有数据。

两条用户建议在r1反馈时先交PM，而非立即静默改变契约；随后D-126明确授权003立即同包做，实际实现见上。Back仍按原规则暂停，未随Seek一同改变。

以下为002候选准备历史（显式Seek默认暂停等旧说明已由003/D-126替代），不作为新的重复测试要求。

## 当前入口和范围

主菜单 `PLAYBACK-002 · 分层前进／退队重排`，读取原猎人录像`1d5346b085b04635ab3e00b43fe9190c`（格式4，27操作，SHA256 `15C5BC983E52354688E9EB971EEB4A223D65DA49DD023A79EDCC9A34EF3AF3F3`），新路径扩至前22操作后暂停。没有篡改录像／ModHash或重录；生产读取沿既有精确录制版本白名单。

原片20爆发→21生存者→22打击同属第5回合，21有两次真实手牌选择（选择3/4）；三张牌在19后均已在手牌、实例与22处记录身份一致，可作为A/B/C提前接受、选择退队、后继重入队样本。此前2生存者之后是EndTurn，不能冒充“选择后普通出牌”的样本；不是重排原片顺序。到19需要首次正常原生观看建立边界；没有从文件直接恢复19或整局预计算。前22包含原本第16步击杀，状态/死亡处理沿现有原生与身份恢复，不将其扩大成全内容新验收。

## 实际控制语义

`OperationRequests`是生产调度器，不再使用无类型Step整数。请求保留ID、Fine/Whole类型和目标宿主/阶段，按FIFO排队；目标来自录像的语义路线描述，不是选择恢复状态。`>> 完整操作`目标为当前未完操作的结束，若已有活动/待播目标则从它之后预约，连续三个完整请求绑定三个不同操作。`> 精细前进`目标为下一原生阶段；普通无选择牌的精细目标仍是操作结束；有选择则包括打开、选中和实际确认后的下一阶段。没有原生Confirm则不虚构。

运行门初始暂停、Auto关、请求空。暂停点击仅预约并显示待播条目（卡牌内部ID/宿主/目标），Play才调度。Pause保留当前目标及未来FIFO，已开始计算/显示自然收尾，选择等待可浏览；Play先续当前目标。普通完整操作内部的全部选择由同一目标覆盖，Auto关或后继请求为空不阻止已获准的当前整项结束，也不另外扣步。

Auto是独立开关，不解除暂停，显式FIFO优先于Auto产生的完整目标；同一目标只由一条调度路径执行。连续显式精细/完整请求按FIFO处理。**最后一个显式精细目标到达且没有后续显式请求时，明确关闭运行门暂停，Auto保持原值**，让用户观察；有后续显式请求则继续FIFO。PM已认可这项D-125细化。普通完整请求量尽时门保持运行等待。

**精细停在选择内部后直接Play**：如果没有显式待播目标，生产调度器生成仅指向当前未完宿主结束的Continuation目标，不消费额外Step，不重新开牌/付费；Auto关时完成当前项后等待，不启动下一张未预约牌。显式待播目标存在时尊重FIFO；完整操作边界上Auto关且无请求，Play不会凭空启动牌。UI显示“续当前”，区分此目标与用户追加请求。

Back在当前未完操作（含选择阶段）回到其开始前；完整边界回退最近整项。Seek列表只含本次已访问的完整操作边界0..22；选择阶段不入列表，也不保存独立DirectCheckpoint。要重看选择先回宿主前再原生前进。Back/Seek清旧路线目标/请求、到达暂停，Auto保留；Stop清会话控制。暂不加入下一回合/楼层空按钮、原始时间/倍速模式。

## 原生队列与选择

新路线保留`NativeSeek`名称作为现有播放隔离/恢复hook的入口，内部改为操作调度。正常前进/回退后前进均走原生TryManualPlay→ActionQueueSynchronizer→GameAction，不调用旧NativeDisplayTrace.Play。`PlayerLoop.SubmitOperation`只负责原生提交；在实际开始业务前重新核对原记录前态、回合、手牌索引和卡身份，完成时比结果/RNG。原生接受与真正开始/完成分开记录。

完整当前牌执行中，可将后续最多两张**显式已预约**、同回合、当前已存在且可原生使用的普通卡真实提前接受；不按卡名限制，不提前提交EndTurn/药水，不绕过身份或合法性。精细请求阻断这条ahead扫描，选择已等待时不移走候选。未来生成/尚未合法的牌等实际满足后再提交，不伪造队列显示。Auto自身不无界预提交后继，本轮真正三牌入队由显式三项请求验证。

新路径使用原生**本地**CardSelectCmd分支/原生UI/source/PlayerChoiceContext，不再另建历史ChoiceScope窗口代理。`SignalPlayerChoiceBegun(CancelPlayCardActions)`实际取消未开始的后排PlayCardAction，NCardPlayQueue执行原生归手显示。监控仅移除这次已取消的接受/交付映射，保留对应原请求；原生取消不算请求完成，不付费/执行C业务。Burst第一次选择取消的C在第二次选择间也不抢先重排；等整个B及自动链必要结算结束、运行门允许时，按原请求顺序解析结算后的原身份重新TryManualPlay，新ActionId、接受次数递增，最终请求只完成一次。无新增请求/Auto量则不发无关后继。

`ActionQueueSet.GetReadyAction`过滤只阻止尚未获准的手动项开始，允许当前已开始选择宿主恢复执行以及自动hook；不整体Pause ActionExecutor而把当前续接锁死。运行恢复会唤醒原生executor。监控在真正的下一手动动作之前或队列空闲处发布上一项结果，再保存边界数据；队列仍忙时也不丢失中间状态。自然Pause应允许B收尾但阻止尚未开始的C，仍需实际验证。

Back/Seek/Stop先原生取消旧未开始的预约提交，按录制选择收完至多当前手动宿主，再在执行器/监控空闲时恢复边界，增加generation并清归属；不复活旧Task/TCS。沿用遮挡层保留底层逻辑可见，提示当前整项退役/应用数据。选择不再是目标，因此没有重建目标选择宿主的Seek；从旧选择离开仍可能有原生退役等待，不承诺瞬时。

关键日志：`operation-control/phase`（请求ID/类型/目标/运行门）、`operation-native-enqueued`（原生ID/是否ahead/尝试次数）、`operation-native-cancelled/returned-to-hand`（未开始/仍预约/原生归手）、`operation-native-started/verified`（新ID/取消后重入队/原结果RNG匹配）、`operation-boundary-captured/seek-arrived/driver-closed`（只恢复整项、代际及退出归属）。日志用于确认真正退队重排，不能仅凭用户看见待播条目报成功。

## 检查、版本与恢复

build-002 Release/direct均0警告0错误。`evidence/playback-002/checks/build-002/operations.json`走真实原片生产读取并核对文件不变；7组生产调度检查覆盖：三项完整独立绑定且内部选择不扣步、Fine/Whole/Whole混排、精细观察不被Auto吞掉、ahead预约保留不重复消费、精细页直接Play只续当前、边界Play不生新项、Seek/Stop状态与真实A/B/C输入身份。没有Godot/原生队列执行，不将离线结果当原生通过，也没有重跑旧矩阵。

当前DLL SHA256 **`74C88E494AD7D511169945CEC3A972ACC2C81D79F9010455FD836BA1E83E2E0F`**，manifest0.1.0，安装收据`evidence/playback-002/preparation/build-002-54cb0738.json`，pair封存`evidence/playback-002/builds/build-002/`。001（EF226CB5…5D857，build-001-5c68cd98）是交用户测试前准备中间版：PM复核指出精细页直接Play续接缺口，补生产目标/检查后换002。001 pair/源码/6组旧检查保留，**无001真人运行PASS/FAIL**，不要求用户测中间版。

基础仍为固定原账号恢复点`C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`；SEEK-001/003 pair/settings/selector在`preparations/build-001-5c68cd98/`，未运行001 pair在002准备点；重要旧片/证据及003源码封存/Git保持。换版前后game0、原账号/固定游戏DLLhash核对，Cloud沿当前批次有效确认off，只启ReplayMod/modded1。未恢复存档、扫描另一账号或启动正式/headless游戏。002当前源码/合同/报告在对应准备点封存；Git自然停点只提交本包文件，PM管理页不暂存。

## 首次最短人工步骤

无需重录。手动打开主菜单 **PLAYBACK-002 · 分层前进／退队重排**；`>`与`>>`是导航粒度，不是速度。

1. 暂停／Auto关，连点`>>`三次，确认待播为操作1/2/3且不前进；Play后应完成到操作3，第2项内部两次弃牌自动完成，不另点。开启Auto继续至切片22自动暂停，建立本次边界索引。
2. Seek到19，关闭Auto，暂停中`>>`三次后Play。观察20爆发、21两次弃牌、22打击：后继应先真实入队、选择时退回手牌、选择及当前项结束后自动重新入队，只打一遍；无需补点。可在弃牌界面按Pause，确认后继不开始，Play续到22。若来不及暂停不重复全片，日志先判定真实取消重排。
3. Seek到20，`>`一次再Play应停第1弃牌窗口；再`>`一次、Play停已选中；**此时直接Play，不加任何请求**，应补完21的两次弃牌并停操作21，Auto关时不自动打22。Back应回20；可按`>`、`>>`各一次后Play，验证混排能补完21。Seek列表应始终没有选择页。
4. 再Back到20，`>`一次后Play停选择窗口，Stop。短重开新入口，`>>`一次再Play到1后Stop，正常退出。中途失败则正常退出，不继续补后续或重录。

首次原生结果未知；若提前接受或取消重排未发生，明确为未完成项，在本包修复，不用始终串行或假预览称已实现。

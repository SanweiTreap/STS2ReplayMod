# RUN-001：整局外壳与第一条非战斗录播链

## 最新摘要 · 2026-10-31 · build-003 已安装，奖励选牌补录待测

- 当前安装RUN-001/build-003，DLL `145E3A3DE8A918CBD8AE90EBF4EFE1D7413E3F46911A1A5C26298266828F66EC`，收据`evidence/run-001/preparation/build-003-87ed5cdc.json`。002/r1原生短路线正常完成，但卡牌奖励记录因受限存档接口漏项；003改记录原生实际offer，待重录验证。
- r1实证：四次实际访问含涅奥缺口、普通→营火→普通；首战8操作胜利片及后战1操作片段均封存读回匹配，金币/药水/选路/营火SMITH候选选择已录，后战Bash升级1。仅CardReward选牌未记录，不能判整链录制或播放通过。
- r1原件/69事件等74数据文件已封存，当前51文件3,960,178 bytes补存`RUN001-post-r1-current-20261031-154821`；正常保存进度是本次未完短路线后战。旧009/001/002 pair和恢复点保持，无restore/自行启动。
- 003构建0警告错误、5项文件/资源缺口合同通过；真实r1只读诊断第5项前有漏记变化。失败root不改写/不补猜选择，旧002战斗片仍可独立读取，旧整链明确不可用。新卡牌奖励身份/候选记录和整链播放、休息/跳卡/合法点Continue/选择中Stop仍待原生实测。
- 原生入口已定位：RewardsSetSynchronizer.SelectLocalReward、NCardRewardSelectionScreen.OptionSelected、RestSiteSynchronizer.ChooseLocalOption及NDeckUpgradeSelectScreen、NMapScreen原生选路/TravelToMapCoord。单场播放器现有胜利奖励屏蔽保持；整局模式在观看者推进时才开放原生奖励，避免新奖励等待破坏战斗Back。
- 首个短样本拟默认关闭准备：铁甲A0暗港普通战斗→营火→普通战斗，使用原生牌/遭遇与实际相连地图节点，准备在checkpoint前明确记录。未支持房间留缺口，非战斗Back/跨房间Seek禁用。
- 下一动作：003重新启用短路线准备，新铁甲A0重复短路线：最左普通获胜→金币/选一张奖励牌/有药水领取→最左营火升级痛击→最左普通打一张防御+后正常存退关游戏。只因这轮选牌缺失补录，核成功后进入整链观看；不补造旧决策、不重跑旧50步/角色矩阵。证据本地`evidence/run-001/`。

## 001→002：首个纵向实现，尚无原生运行结论

沿CONTENT-006的真实RunId与Continue观察，独立`runs/<RunId>.json`校验封装保存实际房间访问与语义操作，战斗仍引用原格式10片，不合并成巨型战斗JSON。幕/层/坐标/地图类型与实际RoomType/Model分开，重进增加entry计数、战斗attempt继承原规则，地图/奖励是操作阶段；未支持/嵌套房间留gap/subroom信息，旧片不伪造历史路线。未完战斗Continue另开尝试并留中断边界，菜单能从各个已录战斗起点顺播，不能跨缺口冒充完整整局。房间与步骤保存发生在阶段边界，菜单附着时读取，非每帧解析库。

奖励沿RewardsSetSynchronizer.SelectLocalReward记录实际索引/完整原生奖励身份，NCardRewardSelectionScreen.OptionSelected记录最终候选与决策；营火沿ChooseLocalOption，升级通过原生NDeckUpgradeSelectScreen选择/确认，休息走原生Heal/Hook；地图沿原生NMapScreen.OnMapPointSelectedLocally的投票GameAction及TravelToMapCoord/EnterMapCoord转换。没有按牌/药水/遗物手写资源效果。`RunResources`仅比较原生Players、RNG、Odds、共享遗物池、幕/难度/坐标历史、房间历史及不含saved_map的Act状态，明确不是通用地图/文件权威比较；原生地图省略bool读入差异不靠覆盖expected修补。下一战另用现有完整LiveState初态/RNG投影核对。

整局模式复用PlayerLoop/MemorySession一次安装与统一退出；只有合法第一战锚点载入原生checkpoint。锚点另记录当时原生Progress环境供解锁/奖励池等使用，放在隔离manager，不修改普通Progress；后续地图→营火→战斗继续同一RunState，不载入后场checkpoint、也不注入其expected。跨战斗退出清理旧roster/牌/单位订阅后交给原生房间退出，下一战只校验并复用NativeSeek/原生动作。Stop场景退出先取消原生窗口，再观察/收束未完战外Task，恢复原manager前再次drain；这些新退出路径尚未实测，不提前判隔离通过。

控制沿`>`细阶段、`>>`完成当前语义操作、Auto连续推进，无新Play/Pause。整局的战斗胜利先保留现有Back机会，观众推进后才原生显示奖励；单场打开仍屏蔽奖励、停在胜利。战外由共同门控驱动原生选择窗口、选中与确认，忽略录制时来回犹豫，只演示最终决策。战外Back/跨房间或开场重入Seek明示禁用；单场原有开场Back保持。新控制与奖励等待、地图投票、后战开场及异常清理均待人工，不以代码结构声称已可用。

默认关闭的RunRoute准备仅用于下一新铁甲A0：原生创建两BodySlam并登记Owner/Run，升级原三Defend及两BodySlam，五牌原生RoyallyApproved先天/保留，40HP；暗港前两普通遭遇为CorpseSlugsWeak。在地图交给原生NMapScreen.SetMap创建UI前，选第一排最左普通及它的最左子、孙节点，把同一真实相连链设为Monster→RestSite→Monster，明确日志。001先在GenerateMap结束后改类型，会晚于原生地图节点UI创建，人工运行前002移到SetMap前缀；不改正常未启用准备的地图/Continue/观看，不重掷RNG或播放时改expected。

主工程与DirectContracts构建均0警告0错误；4项RunData文件/归属/中断状态/校验破坏隔离检查通过，见`checks/run-data.json`。实际009旧一步片经原ClipStore显式兼容读回通过，见`checks/legacy-009-read.json`；只验证格式/生产读取，未运行Godot或Native奖励/营火/整链/旧角色回归。没有为新准备创建镜像Owner测试或重跑旧纯模型合同。

用户确认本包Cloud仍关闭、没有新进度；安装前固定原账号/game0、51当前已知文件与`CONTENT006-post-r10-current-20261031-021506`全部hash相同，复用该恢复点，收据`pre-001-recovery-check.json`。001准备build-001-66377d55保留009 pair；002准备build-002-08a8e213保留001 pair，均在本包evidence/run-001内独立seal构建编号，manifest保持原值。没有另扫账号/历史或全账号快照，无自动恢复/Cloud切换/自行正式或headless启动。首轮只先取实际短路线录制，普通bug在本包修，不向PM逐候选请求审批。

## r1：002短路线正常，战斗卡牌奖励身份序列化异常造成漏记

用户完成正常领奖、营火升级痛击、进入后战一张防御+后存退/关游戏。session `5662e9dc2a3a4ed5b420d30312cd3281`69事件，Run `758153d9b0034d4a8585db343bd7fb84`，4个visit：1层Ancient/实际Event.Neow如实gap；2层Monster/CorpseSlugsWeak，3层RestSite，4层Monster/CorpseSlugsWeak。首战`a31797900bee4ab6ab818ab80f7e2514`8操作真实胜利；后战`eb310bf7bf5f4616919b23faca1b0a32`1操作interrupted，两次clip-production-readback均ExactPayloadMatched。SMITH记录deck-upgrade选择索引6，后战初态Bash升级1。正常游戏操作成功不等于完整记录成功。

1条observer-error：`NotImplementedException: Tried to serialize a CardReward with card creation flags! ... Flags: IsFromCombat`，首调用点RunPatches.RewardKey→原生CardReward.ToSerializable。002误把奖励存档DTO当作所有运行时offer的通用身份；异常被Guard拦住，正常原生选择仍执行。记录了金币20与药水领取，但没有建立卡牌奖励操作/候选/最终索引。Godot只有实际获得CARD.TAUNT的结果行，不足以重建完整候选与选择过程，不能据此猜补旧片。原root共10项，在药水后与Proceed前的资源投影出现未记录牌组变化，003只读诊断定位到第5项前；002当时没有显式gap记录也作为本轮缺陷保留。

原session+两片/root/journal/Godot共74数据文件逐hash封存`runtime/run-001-r1-20261031-154821`，摘要`runtime-verification-r1.json`、复用脚本收据`collection-r1.json`。已知51当前文件3,960,178 bytes补存`RUN001-post-r1-current-20261031-154821`；新正常游玩/存盘变化按事实保留，未恢复旧三鼠或修改当前进度，game0。收集脚本只增加Package/RunIds参数复用，未另造框架/遍历账号。无整链观看事件，本轮整链记录结论FAIL_CARD_DECISION_OMITTED。

## 003：使用原生运行时offer身份，明确漏记缺口，待补录

CardReward身份改为实际已生成的有序卡牌（复用已成功用于营火候选的CardKey：模型/升级/原生SerializableCard）、CanSkip/CanReroll及只读Source/Flags；不再调用明确不支持IsFromCombat的CardReward.ToSerializable，也不清除flags或重建expected奖励。药水奖励另记录真实Potion.ModelId，避免原生基类SerializableReward不含药水型号；领取继续走同一原生同步器和结果比较。任何录制Begin失败现在留显式gap/visit标记并保留原异常，正常游戏仍执行；资源断点另作只读诊断，禁止有漏项的起点播放，不凭结果填补步骤。旧原件/校验均不改，旧002战斗片加入精确hash兼容，旧候选整链在UI明确禁用。

主工程/DirectContracts构建0警告错误；原4文件合同加1资源漏记诊断及不篡改历史合同通过，见`checks/reward-offer-fix/run-data.json`。同一诊断对真实r1 root实得断点5（写入r1摘要），不是假对象验证新NativeReward路径；尚未原生补录，未称奖励修复或整链PASS。游戏0下003安装完成，DLL `145E3A3DE8A918CBD8AE90EBF4EFE1D7413E3F46911A1A5C26298266828F66EC`，manifest不变，收据`preparation/build-003-87ed5cdc.json`，002 pair/config和失败保留。Cloud沿本包确认，无自行启动/恢复；下一只重录这条短路线补齐真实选牌，再取整链播放。

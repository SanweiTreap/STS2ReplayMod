# PLAYBACK-003：普通入口与完整单场原生播放

更新2026-10-30。007/r8已通过胜利恢复后的新牌登记/使用、重复恢复不多挂及Stop/短重开清理；D-131相关手动/Auto/Back/Seek路径取得实际证据。用户另要求片尾自动关Auto，已安装008小改，更新后的终点合同通过，不要求重跑旧矩阵。008不冒充另有原生运行证据，r1–r7历史和004未安装草稿保留，旧PLAYBACK-001/003仍Unknown。

## build-007 / r8 修复实测通过；build-008片尾关闭Auto

用户反馈“应该是好了”，并要求“播放结束后auto自动关闭”。runtime `fc942348dc1445759f584b31b7ed0246`共732事件、三次recent打开新片，50次原生结果/RNG均匹配、11次原生选择、3次真实VictoryTask/原始奖励前胜利投影匹配。7次Back/Seek均状态/RNG/请求时意图匹配、清计划；含Auto开启Seek后继续、关闭后当前整项收尾，以及胜利Back后手动等待。实际不是简单照抄最短草案，按日志所做路径记录。

订阅修复的原生证明：7次恢复中2次胜利后各补5个当前牌堆监听，其余5次Attached=0，已有卡双向ID及保存计数器核对均通过。最后Seek9后，隐秘匕首生成两张小刀的CombatIndex为18/19，随后操作10和11在generation10各匹配完成一次，既能完成生成后的严格投影，也能提交/使用新小刀，没有再报缺ID。三次退出在21/11/1，Error=null、Outstanding=false、控制/请求清空，原manager与所比较Continue/进度恢复。清理订阅Before分别0/5/5、Remaining均0，证明原生胜利退订及Stop主动退订/重开均在所测路径正常。

732原事件及对应Godot日志在既有恢复点`runtime/playback-003-r8-20261030/`逐hash读回封存，摘要`evidence/playback-003/runtime-verification-r8.json`。模组失败事件0，不宣称引擎绝无既有启动/退出诊断；新片hash仍33C47126…99B5F。r6/r7失败及调用栈不覆盖/反写为成功，不外推全部卡牌/一般文件恢复/发布性能。

008只在最终路线边界同时清Auto，并将终点状态文案显示为“播放结束”；片中Auto切换、手动细进、Back/Seek意图、胜利核对和登记修复不变。既有完整路线合同更新为末端Running=false/Auto=false、无余量且不能追加业务，并通过4组检查；Release/DirectContracts构建0警告0错误。变更是用户要求的低影响结束状态，不为此追加整场或旧矩阵，008本身尚无另一次游戏运行。

已安装008 DLL `1E5FABDCB4177C4C9B07363BB960E6F1FCACF50B61BFBEA0B10187EDDCA73D73`，收据`evidence/playback-003/preparation/build-008-2ea13753.json`；旧007 pair/配置在既有恢复点同名preparation保留，game0/原账号/固定原生DLL/pair核对，仅ReplayMod/modded1、本批Cloud原确认保持。没有启动游戏、恢复档或重录。007实际修复通过与008片尾小改离线结论分开交PM收束，不追加订阅审计。

## build-007：恢复原生战斗卡牌登记监听

006/runtime `838a7c2c04c64959b13c0feb3dfb9e9a`共308事件再次复现：先21次原生结果/RNG和真实胜利通过，再经胜利Back/Seek恢复重看隐秘匕首时抛出异常。原始栈明确是`NetCombatCardDb.GetCardId`无法为新生成CARD.SHIV取ID，经过`NetCombatCard.FromModel`、`LiveState.CardStateEntry/Mechanics/Capture`、`ActionMonitor.Complete`、`NativeSeek.FilterReady`；完整栈只留私有原事件，报告不复制实例地址/本机路径。闭合在9、Error明确，driver Outstanding=true，原manager及所比较Continue/进度恢复仍true，不称干净Stop。

固定原生源码证实：NetCombatCardDb.StartCombat订阅AllPiles.ContentsChanged，为新卡通过OnPileContentsChanged/IdCardIfNecessary分配双向ID；OnCombatEnded会解除并清_subscriptions。DirectCheckpoint的数据memento恢复_idToCard/_cardToId/_nextId，但刻意排除订阅/委托；此前只恢复StateTracker订阅，未恢复NetDb监听。故胜利后Back能使用已保存ID的卡而通过，回到生成新卡前则出现缺ID，并非隐秘匕首专属效果或D-131队列语义问题。

修复在数据恢复后按当前CardPile对象身份补缺失订阅：重新挂原生OnPileContentsChanged回调，创建当前pile/action订阅记录并挂回原生OnCombatEnded清理。已有订阅不重复添加，不复活旧委托/Task，不调用StartCombat重置数据库，不用测试分配ID接口、不改录像/ModHash或跳过缺卡校验。补挂后核对_nextId保持以及所有已有卡的双向映射一致；后续生成仍由原生牌堆事件按保存计数器登记。ClearRoom在原生Run清理后显式调用该纯退订回调，核对订阅剩余0，避免Stop留下新挂的旧牌堆引用，不执行胜利/奖励业务。

新增4组纯模型/生产AttachMissing合同通过：复现仅恢复ID表但无监听时新卡缺ID；补监听不改旧ID/计数器且新卡顺序正确；重复恢复/重复pile来源不多挂；连续胜利/恢复后仍无编号漂移且句柄可清理。这些检查使用实际IdentityMemento和生产缺订阅计算，未执行原生游戏对象/私有绑定，不能替代游戏短测。14组D-131请求、4组完整路线及实际新片生产读回兼容检查保持通过。Release/DirectContracts构建均0警告0错误，见`evidence/playback-003/checks/build-007/`。

308原事件及Godot日志在既有恢复点`runtime/playback-003-r7-20261030/`逐hash封存，摘要`evidence/playback-003/runtime-verification-r7.json`，新片仍保持原字节。已安装007 DLL `1625D7D5EB0E372B30C9E95B2A1CF723F82FAC55714B628643AA312627F5FD79`，收据`evidence/playback-003/preparation/build-007-0f76229a.json`；旧006 pair/settings/selector在既有恢复点同名preparation保留，game0/原账号/固定原生DLL/pair核对，仅ReplayMod/modded1，本批Cloud原确认保持。没有启动游戏/恢复存档，产品模型不改。

同片针对性验证仍沿已成功复现的胜利→Back20→Seek4→Seek9→Fine打开/选一张→Whole完成10；再用一张生成小刀到11，以确认新ID能实际提交执行，Stop后短重开1/Stop并正常退出，核对结果/RNG、登记计数器和退出订阅/Outstanding清理。无需重录或重复旧控制矩阵；007原生结果Pending。

## build-005 / r6异常与build-006诊断

用户报告“崩溃”。runtime `f76c1ed657b84b75b3ef33d791c340fe`共331事件：默认手动、合法Fine/整项累计及选择收尾实际先完成1..21、全部21组结果/RNG与真实胜利匹配；8次Fine接受。随后Back20、手动Seek4、Seek9均匹配。重播10隐秘匕首时，Fine请求25打开选择、Fine26选中第一张，Whole27收尾到选中第二张/确认，牌效生成日志后发生`observer-error: InvalidOperationException`，未交付第二次10的结果。

原日志只存异常类型，未保留消息和调用栈，不能凭异常名断言LINQ/队列/恢复等具体根因。回放退出到主菜单，driver关闭时Outstanding=true，playback-closed在9且Error明确，原manager及所比较Continue/进度恢复核对为true；此后Godot有正常Quit记录，读取时game0。0次Auto切换，不据本轮宣称Auto切换矩阵或整包已通过。

331原事件和对应Godot日志已在既有恢复点`runtime/playback-003-r6-20261030/`逐hash读回保留，摘要`evidence/playback-003/runtime-verification-r6.json`；新片hash仍33C47126…99B5F。异常及Outstanding不隐藏，不以初次完整胜利抹去恢复后的失败。

build-006仅在Runtime.Guard的observer-error与PlayerLoop的playback-failed中记录`ex.ToString()`，包含原始消息/内层异常/调用栈，留在私有运行日志；调度、UI、选择、RNG及恢复代码未改，不是推测性修复。Release构建0警告0错误，未因纯错误日志变更重复旧合同矩阵。DLL `63639812D8C7EEC4C9C43840E024907BD3462D6C174593D2A88EB357B3A08C77`已安装，收据`evidence/playback-003/preparation/build-006-b38dd4b2.json`，game0/原账号/固定原生DLL/pair核对，仅ReplayMod/modded1，本批Cloud原确认保持。旧005 pair/配置在既有恢复点`preparations/build-006-b38dd4b2/`，未启动游戏或恢复档。

请求最短针对性重现：同一最近新片开Auto到胜利以建立本次边界；Back20，再Seek4、Seek9；Auto关，`>`打开隐秘匕首、`>`选一张、`>>`收尾。发生异常或未复现都正常退出后反馈，不重录/重跑控制矩阵。此路径用于取得原始栈，不称006已修复或要求用户重复验证整个D-131。

## build-005：D-131手动/Auto两模式候选

按更新后的本包handoff和用户确认，公开原生播放器不再创建Play/Pause按钮，NativeSeek.Control也移除toggle处理，不能通过隐藏控制恢复暂停。默认内部生命周期门开放、Auto关、无目标时手动等待；不会自动提交第一项。内部Running只保留用于退役/恢复/Stop/片尾，独立STATE旧工具控件不改。

`>`仅Auto关、没有Active/Pending请求且到达可用观察点时开放，点击即请求下一细阶段。UI、Control入口与请求模型均检查资格；当前已开始动作执行中不能追加Fine，`>>`仍可累计。原生选择页通过HasView/Ready识别等待，即使Host>completed且宿主Task未完成，也可再次细进；没有要求全体Tween静止或简单按!InFlight禁用。到点保留生命周期门、无Auto/无请求就等待，不产生WholeContinuation。选择中`>>`目标为所属操作的完整边界，自动按录像完成剩余选择和牌效，不另开一张牌。

Auto开则沿完整项连续调度；关Auto只停止自动追加，已开始Whole（含选择）以及显式FIFO继续自然收尾，不增加中途取消/保留整队暂停的框架。模式切换不丢已有合法请求。Back清计划、恢复已访问完整边界后转手动/Auto关；Seek清计划并按请求时Auto继续/等待，导航恢复的内部门规范化为开放，避免片尾原Running=false变成无公开恢复入口的隐藏暂停。Stop/片尾正常关闭；原生取消、身份/结果/RNG核对和隔离逻辑保持。

Release/DirectContracts均0警告0错误。`checks/build-005/operations.json`的14组生产请求政策检查通过：默认手动、Fine忙/Auto拒绝、未完选择宿主中的等待/细进、无隐式Whole、整项累计/FIFO、选择整项收尾、Auto切换保留当前/显式计划、Back手动、Seek意图及Stop。4组完整路线/原片读取和新片生产兼容读回也通过；均为离线检查，不宣称本候选已原生运行。

已安装build-005 DLL `FE7B24A0D58CDBD9B40756F041A6997B5D4C4E44EA1CA09DFE108351CC7BA084`，收据`evidence/playback-003/preparation/build-005-d3a79956.json`。安装pair与封存一致，换包前后game0/原账号/固定原生DLL核对，仅ReplayMod/modded1、本批Cloud原确认仍适用；旧build-003 pair/settings/selector在既有恢复点`preparations/build-005-d3a79956/`。没有启动游戏、恢复存档或修改新片。D-130未安装草稿及其11文件/hash检查保存在`drafts/PLAYBACK003-D130-20261030-before-two-mode/`，不覆盖它来冒充本候选。

待测同片最短相关路线：默认手动且无Play/Pause，`>`完成1刀刃之舞后等待，再累计8次`>>`到9等待；`>`进入10隐秘匕首选牌，稍候应不自动选择，`>`可再到选中阶段，`>>`完成剩余宿主到10等待。Auto关Seek4仍等待，开Auto后Seek4应自动继续；关Auto让当前Whole/显式计划收尾，Back后应Auto关并手动等待，最后Stop正常退出。无需重录、再次完整胜利或重跑旧矩阵；具体实际覆盖以日志为准。

## r5结果与用户正在修订的交互模型

build-003/runtime `5c46cac81355473386a96fe8e0d1c00f`共245事件，普通recent打开同一新片一次。暂停/Auto开启时操作2/3/4以Fine/Whole/Fine预约，分别原生ID1/2/3，在Play前均AcceptedWhilePaused=true、BusinessStarted=false；Play后按序各执行一次，4到达Running=false/Auto=true，之后直到用户再次Play才继续。三张小刀混排已实测通过，用户确认符合预期。

本轮14次原生结果/RNG均匹配（首次1..10，后续Back后8..11），3次Back到9/8/7均匹配且暂停清量；3次原生选择。Stop在11，Error=null、Outstanding=false、请求/控制清空、原manager和所比较Continue/进度恢复。本轮没有完整胜利，不以旧主链替代本轮覆盖。原245事件及对应已退出进程Godot日志在既有恢复点`runtime/playback-003-r5-20261030/`逐hash封存，摘要`evidence/playback-003/runtime-verification-r5.json`。

用户另报告隐秘匕首以`>`开始时未入队。日志中Fine请求16、Host10、Choice2/Prefix0/Complete=false在00:55:20.769520暂停预约，旧代码拒绝Partial Fine预提交，到Play后的00:55:31.239191才提交；后续原生选择/结果仍匹配。该缺口是把阶段目标的Complete当作宿主接受条件，不是某张牌的动画绑错。曾准备按宿主映射去重的草稿，但未构建/安装，收到用户新规则后已撤掉，不扩这条路线。

随后用户先经PM明确D-130：Fine仅Running且Auto关闭可新预约，到点保持Running等待，Pause/Play不能凭空补Whole。相应入口/UI/Arrive/ToggleRunning草稿构建0警告0错误，13组请求政策、4组完整路线及新片生产兼容读取检查通过；草稿DLL `A8FEFD89199948D1940A769F016EF799F0F29AF2A0FDE79C90FC65E39C660FAE` **未安装、不是已发候选、没有原生运行结论**。检查暂在`evidence/playback-003/checks/build-004/`，不能据目录名宣称已安装004。

用户又提出取消独立Pause、默认运行，以Auto开/关表达连续/手动的新方向。讨论期间遵PM要求没有安装旧D-130草稿。影响分析指出手动忙时Fine资格、关Auto的当前宿主收尾、旧Back隐藏暂停等关联点；此后用户接受D-131，现由上述005落实，旧草稿另存，不丢原证据或重录。

## build-003：已预约的Complete Fine/Whole统一预入队窗口

用户报告“先>再>>只能前面进队”，并授权与PM确认是否良定义。PM确认上一轮“不越Fine后继”过严，应与D-125保持一致：有后续显式请求则FIFO继续，仅最后Fine无后续显式请求才暂停。Complete Fine后的已预约连续同回合合法卡可共用原三项窗口，不能因为请求类型为Fine而截断；不新增Auto目标/未预约后继。内部选择阶段和同宿主多个阶段继续阻断，运行门/原生合法性/资源/取消归属不变。

代码只去掉NativeQueueCandidates在Complete Fine后的无条件break，三项上限保留；候选仍来自当前Active与已有Pending，没有拉入自动后继。最后Fine的Arrive暂停规则不变。合同改为Whole/Fine/Whole前缀均可预约，新增暂停Fine/Whole/Fine三请求按序预排、不授予业务/不丢请求、运行逐次消费、最后Fine压住Auto及不生成第四个目标检查；已有单Fine和内部选择阻断合同保留。

build-003 Release/DirectContracts均0警告0错误，12组请求合同、4组完整路线和新片原字节/生产读回兼容检查通过，见`evidence/playback-003/checks/build-003/`。已安装DLL `87F3039C1CB1E6308A2B46C3C19C1D34B693A35B546DCA19CF299E15E622D8F6`，收据`evidence/playback-003/preparation/build-003-c3ecc630.json`，旧build-002 pair/settings/selector在既有恢复点同名preparation保留；game0/原账号/固定原生DLL核对、pair一致、仅ReplayMod/modded1，本批Cloud原确认保持，不启动游戏或恢复档。新片不改、不重录。

待测最短路线：普通最近入口打开同新片，Auto关，`>>`/Play完成1刀刃之舞，暂停并开Auto；依次预约`>`、`>>`、`>`（操作2/3/4三张已生成小刀），三张应都在暂停期间原生入队但不出牌/伤害。Play按2→3→4各执行一次后暂停，Auto仍开、不自行打5；Stop并正常退出。无需再次录制或全场回归，结果仅待此混排增量。

## build-002 / r4：单独Fine通过、混排缺口真实定位

runtime `c0f6c1ea817340bd9e7a7e0c8c67c34e`共471事件、两次recent打开同新片；37次原生结果/RNG全部匹配（1..21及1..16），6原生选择、一次真实胜利，两Stop在21/16，Error=null、Outstanding=false、控制/请求清空，原manager及所比较Continue/进度恢复。没有把第二次描述为短Stop1。用户确认单独Fine正常。

具体证明：第一次打开，暂停/Auto开时Fine预约小刀11与12，分别原生ID14/15，在请求后即AcceptedWhilePaused=true、BusinessStarted=false；直到对应Play才开始，完成目标均Running=false/Auto=true，后续观察期间没有自动跨过。单独Fine入队/业务门/停点已实测通过，不因后续混排问题抹去。

第二次打开在完成13后，00:34:51.920299暂停预约Fine14（原生ID18入队），00:34:52.745381追加Whole15、00:34:57.145538追加Fine16，但后两项直到00:35:01 Play后才提交。执行到14时Running=true，继续15，再到最后Fine16才Running=false/Auto=true；这证实执行FIFO和预入队前缀规则不同，问题在候选过滤，不是导航丢失/重复牌效。另有Fine/Fine队列也受相同截断，统一按上述已预约前缀处理。

471原事件与对应已退出进程Godot日志已在既有恢复点`runtime/playback-003-r4-20261030/`逐hash封存，摘要`evidence/playback-003/runtime-verification-r4.json`。模组失败事件0，混排视觉缺口单列。r3新片hash和主链结论保持；包不因单独Fine通过就关闭，继续build-003相关短测。

## build-002：暂停Fine完整卡边界的原生入队修复

用户在r3总体认可，指出暂停点`>`出小刀未播放入队动画。原日志精确定位：第二次打开，在已完成11时，00:21:10.001939暂停接受Fine请求13、目标操作12/CARD.SHIV/Complete=true；该卡未预提交，直到00:21:22.203118 Play才提交原生动作。约12.2秒包含用户主动暂停观察，不是引擎卡住12秒。原因是旧FillNativeQueue一概排除Fine；动画没有换绑，牌也已实际生成。

修复按路线目标判断：下一项Fine恰为完整卡边界，且当前实例存在/本回合/目标/原生合法时，也允许暂停提交原生队列；不消费导航请求、不付费/执行牌效。Play后仍由原运行门授予业务，到Fine目标暂停并保留Auto。build-002当时在Fine之后截断ahead窗口；该条现由build-003的已预约混排澄清修正。Fine内部选择阶段、EndTurn、药水、未来未生成/不合法卡及自身选牌保护继续阻断。统一的OperationRequests.NativeQueueCandidates产生FIFO前缀，原生合法性仍在NativeSeek检查；无小刀牌名特例。

build-002还显式兼容本轮新片录制build-001的hash378309A7…3CA7F，沿原文件格式/校验/Scope检查，不改ModHash或原始录像。这样修复版可直接使用已录新片，无需重录。Release/DirectContracts构建均0警告0错误，11组请求合同通过（新增暂停Fine候选不授予业务/不失停点，以及Whole三项窗口/混排Fine截止/内部选择阻断）；4组完整路线合同保持通过。新片经生产ClipStore精确读回及已知hash兼容、路线全长/原字节不变检查通过，见`evidence/playback-003/checks/build-002/`；离线检查不替代原生入队短测。

已安装build-002 DLL `1F9020D8CBF76329975BAF473F16AA44B83EFF302BD27243E8B117B0E68017C0`，收据`evidence/playback-003/preparation/build-002-004fdfcc.json`。封存pair与安装一致，game0/原账号/固定原生DLL核对，仅ReplayMod/modded1、本批Cloud原确认关闭；旧build-001 pair/settings/selector保存在既有恢复点`preparations/build-002-004fdfcc/`。700事件和新片先封存，未自动启动游戏/恢复存档。当前仅此修复的原生验证Pending，不重跑整场矩阵。

短测沿用最近新片：Auto关，暂停时预约10次`>>`再Play，到10/21后暂停、开启Auto，点一次`>`；原生队列应显示小刀，仍完成10、无伤害/牌效。Play后应完成11并自动暂停，Auto仍开、不自行打12；再`>`/Play一次可核对第二张。Stop后重开，Auto关、`>>`/Play完成1再Stop，正常退出。该流程无需重录、精确抢点或再次完整胜利。

## build-001 / r3 本轮新录主链与Fine缺口

runtime `8cf68011be8948699ee52823291f7a78`共700事件，真实录成新猎人格式6片`e4b7368bd8a54f74a21597630542e2c4`，21操作/4人工选择，原文件SHA256 `33C471265877E4C19437A9AA5C5DBA68CE42BE4062B7775BB5D4F85813B99B5F`。记录和播放hash均为build-001，胜利自动seal，21项时间元数据及生产读回ExactPayloadMatched=true；不是修改旧ID或旧片元数据。录制与两次普通`Entry=recent`打开均在本次runtime，加载走文件生产读取和独立内存会话。

第一次完整1..21胜利，Back20暂停清量且状态/RNG匹配，再原生21胜利；第二次先完成1、Back0，再完整1..21至胜利，共44次原生播放结果/RNG全部匹配、8次原生选择、3次VictoryTask成功与原始奖励前投影匹配。两个Stop都在21，Outstanding=false、控制/请求清空、Error=null、原manager和所比较Continue/进度恢复。第二次是重开后完整再胜利，不能改写为短Stop1；唯一退队为退役，不计新的自身选择退队证明。

原始700事件、对应已退出进程的Godot日志和新片副本在既有恢复点`runtime/playback-003-r3-20261030/`逐hash读回封存，摘要`evidence/playback-003/runtime-verification-r3.json`。模组失败事件0，已知暂停Fine行为缺口单列，不以“没有错误事件”隐去用户视觉反馈。原片及既有r1/r2资料保持；新录普通入口主链作为已取得增量保留，最终收束等待上述build-002短测。

## build-001 / r2 普通入口与游玩后备份

用户10月30日询问“应该好了？”。读取时game0，原账号仍匹配。同版runtime `b5dbcf0fdc4d4b179e9479fcdb2eba5a`共244事件，实际以`Entry=recent`/native-actions打开旧铁甲格式6片`3eb6baeb507043259b4e59562ae510a5`，总13项。原始文件创建于10月28日21:32，录制hash6A4B1730…4EC10，文件hash`2C278C4F19E896C13A604D98F590091AB4F2D0B68C0A09EE8813486D1B126CC8`；不是本候选新录。

首次完整序列1..13，真实胜利后显式Seek5保持请求时暂停/Auto开启、清旧计划，状态/RNG匹配，约122.53ms；随后原生6..13再胜利。共21次原生结果/RNG全部通过、2次真实VictoryTask与原始胜利投影匹配。该恢复是显式Seek，不记作整项Back。Stop在13，Error=null、Outstanding=false、请求/控制清空、原manager和所比较Continue/进度恢复；只有一次打开，未补这份文件的短重开证明。

244事件、对应已退出进程Godot日志及该原片副本已在既有恢复点`runtime/playback-003-r2-20261030/`逐hash读回保留，摘要`evidence/playback-003/runtime-verification-r2.json`。模组失败事件0；Godot含两条启动预载Invalid Task ID和退出资源/RID诊断，不宣称引擎无警告。此次已证明普通入口不依赖原猎人ID/27长度，并验证另一既有格式6输入；仍不代替“本轮真实新录→普通入口→完整→Back→Stop重开”要求。

正常游玩后已补存最新当前文件：51文件/3,941,279 bytes，原文件与副本hash读回一致，收据`evidence/playback-003/post-gameplay-current-files.json`，私有位置为原恢复点`PLAYBACK003-postplay-current-20261029-161356/`（UTC命名，当地10月30日）。范围为上次已知当前文件及原账号两个当前选择槽的直接save文件，不遍历历史/其他账号，不称新完整账号备份。没有换DLL、恢复档或修改配置；同一build继续用于最后新录验证。

## build-001 / r1 实际结果

用户确认“这个好了”，随后正常游玩。测试runtime `80e3077014d04645a0f5cb0a2b8d7ef5`共420事件、两次参考入口打开，hash与已安装build-001一致，源码e13d1f4。首次原生完成序列完整为1..27，含跨回合、两组连续弃牌、生成/使用小刀和中途击杀；之后三次从胜利Back26均状态/RNG匹配、到达暂停/清旧计划，分别约85.33/33.05/34.36ms。原生当前回合等待保留，旧Task未恢复，HUD恢复事件三次。

第一次完整胜利加三次原生再击杀，共4次真实VictoryTask成功和原始奖励前胜利投影匹配，均AllOperationsMatched/VictoryReached=true。合计31次原生结果/RNG核对全部通过（27+3次重播27+短重开1），4次原生选择。两个Stop分别在27和1，Outstanding=false、控制/请求清空、Error=null，原manager与所比较Continue/进度恢复；短重开明确未标全部完成/胜利。原27项片hash不变，没有用旧显示轨迹替代原生动作。

原始420事件及对应已退出测试进程的轮转Godot日志在既有恢复点`runtime/playback-003-r1-20261029/`逐文件hash读回封存，摘要`evidence/playback-003/runtime-verification-r1.json`。Godot文件含启动阶段ScriptManagerBridge/WorkerThreadPool的Invalid Task ID和退出资源/RID诊断，保留原文，不宣称引擎日志绝无警告；模组失败事件为0。测试PID已退出，读取时用户另有游戏进程，未替换DLL、修改配置或恢复存档。

本轮两次都是`hunter-reference`入口，r1自身没有新录文件或`recent`运行证据，不能据此关闭本包；后续r2已取得另一旧片的recent证据，仍缺新录。开场选择、结束回合触发的胜利恢复、更多内容/一般终局与性能仍未证明。用户随后正常游戏的当前文件已在r2补存，不使用测试前旧备份替代新增进度。

## 行为与实现

普通“打开最近片段 · 原生播放器”现在默认进入NativeSeek统一原生核心，按文件实际操作数建立路线；核心不再要求固定HunterClip、22项或没有开场选择。已有27项猎人片仅保留可选的参考按钮，实际通过正常入口打开的新文件需要另行录制、实测。未支持的格式/角色/人工选择类型在入口明确说明，不静默转旧显示；STATE文件冷恢复仍是独立旧工具。格式4–6、铁甲/猎人/储君已有录制资格不等于其全部内容已通过。

完整操作继续提交原生CardModel.TryManualPlay/药水/结束回合，使用原生GameAction、牌效、选择UI和RNG，核对原记录前态、实例及结果。没有调用旧NativeDisplayTrace.Play补动画；恢复仍使用必要的原生显示清理/同步辅助函数。原生队列的0.35秒移动/缩放/淡入保持，未增加拖牌模拟。精细/完整FIFO、暂停真实接受而不执行业务、自身选择取消后原请求自动重排、Play仅续当前、显式Seek保持Running/Auto并清旧计划、Back整项并暂停均沿D-125/D-126。

最后一次击杀必须等待真实VictoryTask成功、CombatWon与原生执行器空闲，并匹配录制时奖励生成前冻结的胜利投影，才设置VictoryReached和AllOperationsMatched。到文件末端正常停止，不再提交额外操作；序号到末端本身不代表已赢。胜利核对仍包括本场最初的所有敌人，中途已移除的敌人不会在恢复后漏出最终Defeated投影。

胜利后Back恢复结束前已经访问的完整操作边界，再由原生业务重新完成战斗。终局本身不保存成可直接Seek的memento；下拉仅列可恢复的完整可玩边界。终局恢复先等待原生PlayContainer的短0.25秒退出淡出结束（最多等至Won后0.3秒），退役剩余显示，恢复数据、订阅、原生HUD位置/计数及EndTurn按钮状态，重新启用原生同步器PlayPhase。此短等待专用于胜利退出尾部，不是逐卡计时表。

恢复不复活旧Task/TCS。当前实现要求原生战斗回合循环和两个当前回合信号仍处于未完成等待，显式检查失败即报`victory-turn-waiters-not-resumable`。原猎人最后手动打击胜利已三次实测满足此条件；结束回合/派生效果胜利及其他机制是否可恢复不据此宣称通过，需真实样本。普通已访问边界的恢复路径保留。

开场选择在原生加载期间按录像的最终决策完成，随后才核对并捕获第0完整边界；不会将开场选牌伪装成第1次手动操作或可Seek节点。该路径去掉旧演示禁令，但本包尚未取得开场选择运行证据。当前只允许已接入的选择叶入口，未知类型明确拒绝。召唤/新单位、宠物、失败局、其他角色、后续楼层/整局及通用文件Seek没有解锁。

## 离线验证

Release和DirectContracts构建均0警告/0错误。`evidence/playback-003/checks/build-001/full-playback.json`的4组生产读取/路线支持检查通过：原片27项含最后Victory保留；支持策略和路线不依赖固定ID/27长度；开场选择不增加第0独立选择节点；未支持输入明确说明。不同ID检查仅为内存规划检查，不能代替新录文件证明。

同目录`operations.json`的9组既有请求合同通过，包括精细/完整混排、三完整请求、直接Play续当前、Seek清量及四种Running/Auto意图、暂停接受不授予业务。原27项猎人片SHA256仍为`15C5BC983E52354688E9EB971EEB4A223D65DA49DD023A79EDCC9A34EF3AF3F3`。这些检查不证明本包原生胜利/回退/新录录像已通过。

## 候选与人工验证

build-001 DLL SHA256：`378309A7604BDC0CFE462BA18164F0CE7528F4CA1ACEF35304AE5B53B8E3CA7F`，安装pair与封存一致，收据`evidence/playback-003/preparation/build-001-f7f907e1.json`。旧PLAYBACK-002/003 pair、settings与selector保存在既有CONTENT002恢复点的`preparations/build-001-f7f907e1/`，未覆盖旧源码/失败证据。换包前后game0、固定原生DLL和原账号核对，只ReplayMod/modded1，未启动游戏或恢复存档。

本轮补充已知当前文件备份：`evidence/playback-003/pre-test-current-files.json`，51文件/4,439,940 bytes，原文件与副本逐个hash读回一致；私有副本位于既有恢复点`PLAYBACK003-current-saves-20261029-124138/`。按上次已知文件清单复制，没有扫描其他账号或重新枚举历史；这是当前文件补充备份，不称新完整账号快照，原完整恢复点仍保留。用户本批已确认Cloud关闭。

先验证原猎人片完整27项真实胜利，胜利Back到26，再完成27、Stop；遇错误正常退出并读日志，不让用户继续测旧版。通过后新建一场本轮真实受支持首战，从普通最近入口完整播放，验证整项Back/续播、胜利及Stop/重开，检查正常Continue/进度隔离。无需重跑上一包全部矩阵或在同一战斗塞入所有机制。

首轮具体步骤：手动启动，选“打开已有猎人样本 · 完整27项”，开Auto并Play至27/27战斗胜利；Back应回26并暂停，手牌/敌人/能量恢复，Play应再次原生完成27及胜利；Stop回菜单。再次打开该样本，关Auto、预约一次`>>`并Play，完成1后Stop，正常退出。读取实际日志后再安排新录，不混用旧版本运行结果。

后续最小新录计划：沿用同一build，不改DLL；先保留用户本次正常游玩的最新进度。用户方便时可开启现有CONTENT-003猎人测试准备，新建猎人A0，首回合爆发→生存者完成两次弃牌，次回合刀刃之舞生成并使用小刀，以普通攻击赢首战；无需复刻原片27项或塞齐全部机制。首战保存后正常回主菜单，从“打开最近片段 · 原生播放器”打开这份新ID录像，完整至胜利，Back整项再完成，Stop后重开一步再Stop。用文件原始hash、记录完整性、Entry=recent、实际新ID/总数和原生结果/隔离日志验证。若用户已有本轮自动录成的受支持首战，也可直接核对并使用，避免无谓重录；不把普通游玩未经检查的文件冒充已保存样本。

上述人工步骤为准备历史，现已由r3取得本轮新录/普通recent主链的真实证明；新片重开也已完成，两Stop在21。r4单Fine修复已通过，当前仅剩build-003已预约混排窗口相关短测，沿同一原片继续，不重录或重复旧矩阵。

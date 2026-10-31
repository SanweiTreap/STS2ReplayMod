# RUN-001：整局外壳与第一条非战斗录播链

## 最新摘要 · 2026-10-31 · 005/r8真正重启Continue与后续领奖归属通过

- 保持RUN-001/build-005，无换版。r8是不同启动时刻的新进程，原生加载表示Key精确匹配r7已观察存盘，MatchedObservedSave=true、PreFinished=true、ChangedBeforeSetup=false；用户回到第二场胜利奖励页，无重打。RunId仍9946446b，不靠seed/坐标猜局。
- 四次房间访问保持；第二场所在visit4 Entries由1到2，新增一条native-continue说明重进，未增加房间/战斗。原两份8步胜利片hash及前10项操作逐项原封不动，只有seq11金币、seq12跳卡、seq13离开追加，均complete。11事件、0 observer-error；生产RunStore读回完整且无资源断点。
- r7已录首战跳卡索引3/4及营火HEAL，HP80→85/max85；r8第二场亦跳卡索引3/4。两场胜利及后续领奖现已同局关联，仍须实际播放此新链，不能仅凭记录/文件读取判跳卡与休息回放通过。
- r8共16数据文件封存；当前51文件4,384,648 bytes补存`RUN001-post-r8-current-20261031-193520`，4个原生replay/current_run/progress文件是本次正常Continue存盘变化，无缺失，game0，无restore/自行启动。关键证据`runtime-verification-r8.json`、`collection-r8.json`、`checks/r8/run-production-read.json`；r7及用户原新增进度恢复点保留。
- 剩余只需此新链两项观看：从第2层首战锚点，先在真实奖励三卡选择窗口中Stop；重开同锚点Auto，核跳卡/HEAL80→85、第二战完整胜利及Continue后记录的金币/跳卡/回地图终点，再Stop。原单场与其他矩阵不重跑，005不重装。r5手动升级链、r6 Auto/营火选择Stop与重开结论保持。
- 整包仍IN_PROGRESS，完成本次回放后再交PM阶段验收；目前不声称任意界面保存、跨房间Seek、其他房间/难度或全部遗物组合支持。奖励选牌Stop此前实际停在胜利处，保持待测，不追改成通过。

## r7：跳卡/休息双胜利录制与合法保存点确认
- 保持RUN-001/build-005。新路线9946446b，两场普通战斗各8项胜利，4次真实访问（涅奥缺口→普通→营火→普通）、10项操作全部complete；生产RunStore/ClipStore校验读回匹配且原件未改。r5手动升级链及r6 Auto/营火选择Stop结论保持，本新链尚未原生播放。
- 首战卡牌候选3张＋跳过，最终索引3/4，native Success=false表示未领取卡牌、符合主动跳过，不是失败。营火原生HEAL成功，HP 80→85（max85），金币112保持；第二战真实获胜，尚未领取其任何奖励。99事件，0 observer-error、无缺失操作；未支持涅奥只在合法战斗起点之前，不补造。
- 当前原生current_run.save确为第4层第二场monster已完成（is_pre_finished=true），StartTime匹配本局；原始SaveKey及原生已观察的加载表示Key都绑定RunId9946446b。只读核验，没有重构/启动游戏；真正新进程Continue仍未测。详细`checks/r7/current-save-binding.json`，两片及root生产读取在`checks/r7/`。
- 104数据文件封存，当前51文件4,396,487 bytes补存`RUN001-post-r7-current-20261031-192959`。相对r6是本次正常录制的5个native replay/current_run/progress文件变化，无缺失，game0，无restore/自行启动。关键本地证据`runtime-verification-r7.json`、`collection-r7.json`。
- 下一步只做真正重启Continue：应回第二战胜利奖励页，不重打；领取金币、卡牌仍跳过，离开到地图后不选下一节点，保存退出并正常关游戏。核同Run/同visit重进、原两战不重复与奖励后续录制，再用此新链验证跳卡/HEAL完整播放及奖励选牌窗口中Stop。005无需换版/重录。
- 本包仍IN_PROGRESS。奖励选牌Stop上轮实际停在胜利处，用户已确认可能记错，不判通过/不记bug；原生暂停菜单只回菜单，当前合法存盘来自第二次CombatManager完成战斗时SaveRun(preFinishedRoom)，不声称任意界面保存/跨房间Seek支持。

## r6：Auto整链、营火选择中Stop与重开通过
- 保持RUN-001/build-005（DLL `DD563F456E5A693E9139F669B3C7E9B95ECFB11D6096A32A79F6EE82E89CF1FA`），不换版、不重录旧片。r5两条手动升级链已通过；r6同片0cdfdef2的Auto原生链至后战初态/RNG及Defend+、终点Stop通过。后战仍为一项前缀，不是胜利。
- r6共四次打开：Auto全链；首战胜利处Stop；营火升级选牌窗口中Stop；重开一项后Stop。492事件，26项战斗操作、11项战外操作核对，0 observer-error；四次原manager与所比较Continue/Progress恢复通过。Auto全链14任务收束；营火中止11任务收束，其中3个原生选择取消、0故障，重开正常。
- 奖励选牌Stop本轮未覆盖：第二次观看没有进入reward phase，停在首战8/8胜利。已向用户核对，答“可能停在胜利画面了”；仅记为胜利处Stop，不判奖励选择中止通过，也不记新bug。下一短样本播放时补这一点，不重测已过的Auto/营火Stop。
- 497原始数据文件封存，当前51文件4,389,898 bytes补存`RUN001-post-r6-current-20261031-190753`，相对r5仅settings.save.backup变化；新增正常游戏进度保持，game0，无restore/自行启动。关键本地证据`runtime-verification-r6.json`、`collection-r6.json`。历史失败与r5代表手动链保留，不逐候选唤醒PM。
- 剩余：奖励选牌中Stop、跳卡、营火HEAL、合法原生保存点真正退出/新进程Continue。已查原生暂停菜单只退菜单，并不额外写盘；营火选择后没有自动保存，不能让用户在此退出后假称已保存休息。下一新短路线合并跳卡/HEAL，第二战胜利出现奖励页后先不领奖，利用CombatManager原生SaveRun(preFinishedRoom)的合法完成点关游戏，先核保存再安排真正Continue。
- 同005现有默认关闭的短路线准备可直接用，用户亲自选择跳卡/HEAL，无新增牌/遗物模拟或正常存档覆盖。先录两场短普通战斗，之后核Continue归属/无重复及同局后续，再用新片补奖励窗口Stop与休息/跳卡回放。整包仍IN_PROGRESS，战外Back/跨房间Seek与其他房间继续不开放。

## r5：首条原生升级链，手动细进与整项通过
- 当前RUN-001/build-005保持安装，DLL `DD563F456E5A693E9139F669B3C7E9B95ECFB11D6096A32A79F6EE82E89CF1FA`，收据`preparation/build-005-ba46e13d.json`。r5用户确认效果正常；本段通过，不标RUN-001整包完成。
- 同一原片0cdfdef2两轮整链：一轮全程`>`细进，一轮`>>`整项。各自首战8项胜利→金币→选CARD.HEMOKINESIS→原生地图→营火升级Bash→地图→后战完整初态/RNG核对→原生Defend+→Stop。后战只录了一项中断前缀，不是后战胜利；没有载入后场checkpoint/注入expected。
- 日志378事件，18项原生战斗操作、14项战外操作、2次后战初态核对与2次run Complete=true；0 observer-error。`>`在奖励页完整出现后等待约5秒才领取，选择页面与选中/确认均逐阶段推进；地图投票及派生移动4对都归属正确。两轮各14个登记任务全部收束，无取消/故障；Stop后原manager与所比较Continue/Progress恢复通过。
- 383原始数据文件已封存，当前51文件4,389,898 bytes补存`RUN001-post-r5-current-20261031-182557`；相对r4仅settings.save.backup变化，正常游玩新增进度保持，game0，无restore/自行启动。关键证据本地`runtime-verification-r5.json`、`collection-r5.json`。r1漏记、r3地图拒绝、r4按钮吞点击等失败原件均保留。
- 剩余：Auto整链、奖励/营火选择中Stop、HEAL、卡牌奖励跳过、合法保存点真正退出/新进程Continue；本轮不能把手动两条链冒充这些分支通过。下一步同005补Auto及选择中Stop，再合并一份短录制覆盖跳卡/休息和合法Continue，不重跑五角色/长战斗。
- 稳定能力说明已补README；本段结果交PM合并状态，无产品范围变化或逐候选审批请求。新非战斗Back/跨房间Seek仍禁用，普通战斗Back保持，其他房间/难度/多人仍不在本包。

## r3/r4：地图派生动作及胜利后按钮衔接失败，修复安装历史
- 用户正常游玩结束后，确认game0及原账号，补存当前51文件4,389,898 bytes至`RUN001-pre004-current-20261031-181559`，相对r2有5文件变化，无缺失；实际新进度位于modded1，原账号unmodded1当前save leaves亦纳入检查。安装004后51文件复核，仅settings.save配置改写，进度文件未变，无旧档恢复/自行启动。备份与核对收据：`preparation/pre-004-new-progress-backup.json`、`post-004-progress-check.json`。
- 当前安装RUN-001/build-005，DLL `DD563F456E5A693E9139F669B3C7E9B95ECFB11D6096A32A79F6EE82E89CF1FA`，manifest保持。收据`preparation/build-005-ba46e13d.json`，004/003 pair和安装前配置保留；仅ReplayMod、modded1，沿用本批Cloud关闭确认，尚无005游戏结论。005构建0警告0错误，安装后51文件复核，进度未变；未恢复/自行启动。
- r4/004：整链入口首战及多次Back/重做到胜利，之后未发出任何run-control/phase；另开同片单场8项胜利通过。没有observer-error，两次观看退出的原manager/所比较Continue和Progress恢复通过。UI每帧先按单场终点禁用`>`/`>>`/Auto，再按整链续播启用，原生Button禁用清掉held press，导致无法释放点击。005仅将三按钮最终Disabled状态各赋值一次，保留其他播放逻辑；不判004奖励/地图修复已通过。
- r4会话524事件＋root/2片/journal/日志共529文件封存；当前51文件4,389,898 bytes补存`RUN001-post-r4-current-20261031-182133`，相对pre004仅两settings文件变化，正常新进度保持。证据`runtime-verification-r4.json`、`collection-r4.json`及`preparation/post-005-progress-check.json`。继续原路线0cdfdef2，先手动胜利后`>`显示奖励并停住，再Auto完整链。
- r3同片0cdfdef2两次原生播放：首战各8项及胜利通过；金币、选牌和离开各自资源/RNG核对通过。两次均在随后地图原生派生MoveToMapCoordAction被旧战斗保护拒绝，自动关闭观看；整链FAIL，未到营火/后战。两次原manager/所比较Continue和Progress恢复通过，不据此声称全部文件零写入。
- 奖励页跳过是呈现门控问题：IsNodeReady早于原生淡入完成，Auto同帧领奖/打开选牌。另查出直接调用奖励/营火同步器绕过原生按钮收尾，已一并改为原生按钮入口，保留原生奖励行移除、选择及营火动画。奖励展示成为首项奖励操作内的细阶段，`>`可只展示停住；Auto等原生展示完成并给共同0.65秒可读间隔，不按牌模拟效果。
- 地图投票与派生移动现在只在当前记录操作作用域放行，核对RunState、owner、目标、地图generation及一次投票/一次移动。等待完整Move任务/原生房间进入，所有已登记UI/业务/选择/营火收尾任务在恢复原manager前收束；不扩大成战外动作通用放行。此修复尚无游戏实证。
- 004候选主工程和DirectContracts构建0警告0错误，真实root经生产RunStore读取/校验且未改原件；显式兼容003精确录制hash，继续使用0cdfdef2及原片，不重录。未执行游戏/原生运行，休息/跳卡、合法点Continue、选择中Stop与完整链仍待实测。
- r3已关闭会话269事件＋root/2片共272文件封存，见本地`evidence/run-001/runtime-verification-r3.json`；未采集正在进行的正常游戏存档、journal或活动引擎日志。r1/r2原件与恢复点保持。下一次先验证首战胜利后`>`显示奖励页停住，再Auto至营火升级/下一战记录终点与Stop；无需重新录制。

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

## r2：003奖励选牌完整录制，合法战斗锚点后的资源链连续

用户补录后“ok了。你看看”。session `14a8617c27634a4d95758f3126f81893`70事件，无observer-error/显式missing。Run `0cdfdef2363c4dfd8fc7b613560ebaf9`有4次实际访问、12项，全部status complete；第1层Neow实际Event仍标未支持，前3项不当本包合法播放起点。第4项锚定2层普通战斗`c87ff59118ac4ef7ab5d34050dbf4bd2`8操作真实胜利，后场4层普通战斗`e3de04de82cc4b55888ad01c8c35ac02`1操作interrupted，两片生产封存读回匹配。

合法起点后的第5金币、第6 CardReward-v2及reward-card选择（3牌+跳过共4项、选2即CARD.HEMOKINESIS）、Proceed、地图到营火、第9 SMITH/deck-upgrade（6候选选4为CARD.BASH）、营火离开、地图到后战均已记录。后战初态Bash升级1。资源连续性只在未支持涅奥内的第3项之前有断点，从合法锚点4之后为0，不因此误判本轮选牌仍漏记，也不宣称Neow链条支持。003运行时offer身份修正与营火最终决策记录取得实际原生证据；下一战初态来自正常游玩，这仍不是回放自然生成后战的证明。

复用收集脚本封存70事件+两片/root/journal/Godot共75数据文件`runtime/run-001-r2-20261031-160430`，摘要`runtime-verification-r2.json`及`collection-r2.json`。增加既有DirectContracts的verify-run读取入口，使用实际RunStore校验root和原件hash不变、定位合法起点/断点，见`checks/r2/run-production-read.json`；这是生产文件读取，不运行Godot，不重跑未变的纯合同。当前51文件3,964,225 bytes读回补存`RUN001-post-r2-current-20261031-160430`，正常新局/存盘变化保留，game0，Quit/shutdown正常；无restore/新DLL/自行启动。整链观看、休息/跳卡分支、Continue及选择中Stop继续待测，003保持，按顶部步骤先测这份现成路线。

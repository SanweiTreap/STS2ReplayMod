# CONTENT-006：持续录制、Continue、战斗列表与召援

## 最新摘要 · 2026-10-31 · 009/r10 · DONE_WITH_LIMITATIONS

- 当前安装009（DLL `163F63D7A8D9042AC9A17AA3F05C4C45CA52AADC12898FE704723CE36A89F0B7`），无需换版；收据`evidence/content-006/preparation/build-009-4f319035.json`。
- 已确认：后续普通战斗持续录制/真实资源承接、短片与完整片独立观看、已完成与未完房间跨进程关联/newattempt、相关Back/胜利/Stop；r9三鼠真实召援前恢复/原生再召/召援后已访问点恢复、稳定目标/击杀和完整胜利通过。
- r10自动路径闭环：不同PID135492→107384，成功raw写盘key7212D1…与原生读入keyC1AC17…确实不同；009预先登记后，新进程Continue MatchedObservedSave true，自动保持同Run/同Battle并Attempt1→2。两份独立一步片段开场相同，没有对该局做显式修正，用户确认分组正确。
- 历史FAIL保留：r7新局准备空归属、r8跨进程错拆、旧001错组及退出问题分别保留；后续修复不追改原片/原结论。范围限固定本体、单人A0首幕普通地图战斗与所测代表机制；不覆盖精英/Boss/事件战斗/失败、通用文件权威恢复、未访问/跨房间Seek或整局外壳。
- 下一动作：本包实现验证按既定样本完成，交PM作阶段收束；保留可用009，不再请求本包重复测试或自行启动/restore。后续产品目标由PM与用户讨论。
- 关键证据：本地`evidence/content-006/runtime-verification-r10.json`、`runtime-verification-r9.json`及对应`collection-r10/r9.json`。r10共34事件等39数据文件归档，当前51文件补存；本轮正常新局/保存产生的进度变化如实保留，不称零写入。

## 实施历史（以下各节状态以当时轮次为准）

2026-10-30。D-134同工程实施中。001/r1六场录制/资源与新进程后续录、004/r2原生菜单退出、005/r3早晚独立完整播放/Back及已完成房间write→load→Continue有各自实证；007/r4华丽收场在途Back等待/声效/尾迹通过，用户明确确认。原误拆组六片不改，索引已有据校正。007保持；后续强普通/未来第4场、召援及未完房间attempt仍未验收，本包未完成。

## build-001：A/B/C首个可测增量，尚未游戏实测

移除正常录制的fresh/一次attempt/首排限制，格式9支持五角色A0首幕普通Monster地图房间逐场录制。每个原生`EnterMapCoordInternal`建立独立生命周期：正常旅行只接收退出旧房间后、创建新房间前的`ToSave(null)`；继续加载则使用`SetUpSavedSingleplayer`真实传入的SerializableRun，不假定saveGame:false会触发保存。等待该次entry Task及本场玩家开场Task成功、可操作且初始选择结束后才捕获Initial。非普通、已完成重进和范围外房间跳过，后续受支持房间可再次开始；不改正常房间/奖励逻辑。

胜利仍自动封存并生产精确读回；正常领奖后的牌组/遗物/药水/HP/金钱/RNG由下一场实际checkpoint承接，尚待实际多场核对。每场结束清理操作/选择/球身份、observer任务引用及STATE记录缓存，不保留上一场的录制任务。保留256操作和4MiB上限，命中明确报本场未封存，旧成功文件不覆盖。

格式9新增Battle元数据：Replay随机RunId、局内受支持战斗顺序、坐标新attempt、act/floor、原生start_time、map/continue-known/continue-unmatched入口、victory/interrupted。旧格式1–8无此字段，省略null保证旧载荷字节一致；仍只允许已有显式录制build，另精确兼容CONTENT-005/011。旧片列表显示未知归属，不补造运行关系。

Replay自有`recording-journal.json`记录新局GUID与实际观察到的存档指纹；Continue跨进程以完整存档的规范化内容匹配，忽略save_time/run_time/num_reloads三项正常变化，保留start_time、资源、历史/RNG等。新局总分配新GUID，不用seed/角色/坐标猜组；两个明确不同新局发生完全相同指纹碰撞时标ambiguous，Continue新建未匹配组，避免串局。没有过去的实际存档证据则continue-unmatched，不声称与旧片同局。普通存档不写Replay标签。

Continue重进未完成普通战斗时，验证传入存档PreFinishedRoom为空且最新坐标正确，使用其原生房间前起点开启新attempt；不串退出前操作。有已完成房间状态/缺少安全起点时跳过当前段，下一普通战斗恢复。是否所有实际保存形态规范化后均能匹配、以及重进同房间的开场数据/RNG一致，当前均待原生证据。

保存退出只封存已完成操作前缀及其完整选择。正在执行/待选的下一动作不强行完成、不回答选择，不写进文件；格式9允许没有EndTurn的真实中断前缀。尚无完整Initial或零完整操作时只记录中断诊断，不产生假胜利或不可播放零操作片。故中断文件不等于保存了退出瞬间的全部操作。

主菜单保留最近快捷打开，增加按RunId分组的逐场列表，显示场次/层数/attempt、角色、遭遇、时间和胜利/片段；每个条目直接使用自身checkpoint。旧固定样本和准备入口折叠到开发区域。目录扫描仅在菜单附着时执行；旧片首次读取建立轻量catalog，后续读取元数据，mtime变化或损坏元数据回读原片。坏片逐项报错/禁用，不妨碍好片。打开时仍执行完整生产完整性/版本/环境检查，catalog不是权威恢复数据。D-131/D-133与观看隔离/退出路径保持。

## 构建与有限验证

主工程和DirectContracts均0警告0错误。`evidence/content-006/checks/increment-abc/library.json`含5组直接合同：跨实例实际存档/可变时间字段识别；三场顺序及同房新attempt；重复seed新局/完全指纹歧义与资源不匹配不串组；后续层无EndTurn中断片生产保存/精确读回及假胜利拒绝；坏片隔离和好片元数据缓存复用。这些执行真正journal/Codec/ClipStore/library代码，但不执行游戏Recorder、Harmony入口、选择退出、房间创建或节点恢复，不当作原生生命周期通过。

已有工具箱开场政策7组通过，既有首次Osty格式8原片经生产读取/显式兼容通过，原文件未改；输出同目录`opening.json`/`first-osty-read.json`。没有重跑未变的旧整场矩阵，没有实际游戏运行结论。

用户本批确认Cloud关闭、无新进度。game0/原账号核对后，沿原完整恢复点补存已知51文件、3,953,223 bytes至`CONTENT006-pre-abc-current-20261030-194038`，逐hash读回；收据`evidence/content-006/pre-abc-current-files.json`。这是已知文件补存，不是新的完整账号快照；未扫描history/另一账号、未restore。

CONTENT-006/build-001已安装，DLL SHA256 `061EC2212B4E0A6B30D7153FA6FCBD8BC89E1D887935EE1823128BF46F572593`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`evidence/content-006/preparation/build-001-61568ba9.json`。保持仅ReplayMod/modded1，旧CONTENT-005/011 pair/config由安装脚本保留；没有启动游戏。任务间编号分开，不以本包001覆盖旧005/011历史。

## 下一次人工动作及未完成项

先以正常A0新局验证A/B/C，不开旧定向准备：至少三场普通战斗，第一、第二场正常领奖，第二场后保存退出并正常退出进程；重新启动Continue，进行第三场（至少两场首排后），第三场胜利后正常退出。下一自然停点查三份独立文件、奖励承接及Continue分组，再从列表验证较早/较晚独立原生播放、Back/胜利/Stop和切片隔离。出现入口/录制错误立即保留日志同包修，不追加五角色全矩阵。

D已定位固定原生`TwoTailedRatsNormal`：三只TwoTailedRat、五个位置，原生CALL_FOR_BACKUP及等待/数量/空slot约束。尚未修改其业务、spawn白名单或一般敌方roster恢复；当前build-001遇到敌方新生仍明确拒绝该段，不称完整支持。后续同包扩稳定CombatId/未来分配器、有序roster、同模型目标、场景/订阅和已访问点恢复，再取真实召援/跨生成Back/原生再召援/新怪目标及击杀证据。文件事实、运行比较、观看memento和任意文件冷恢复将分别报告。

当前仅源码/文件/纯合同和安装证明，A/B/C原生实测与D均Pending；CONTINUE新attempt、选择中退出及目录坏片均未实际游戏验证。精英/Boss/事件战斗、跨幕/难度、失败局、孵化/复活/多阶段、跨房间Seek/奖励语义播放/整局路线图、通用文件权威恢复仍非本包承诺。

## D源码候选：一般敌方生成登记/恢复已接通，未安装

用户进行001三场测试期间继续实现，安装保持001；以下不是001的功能或运行证明。没有中途换DLL、启动游戏或新增用户测试要求。D未验收，本包不关闭。

原生TwoTailedRat的CallForBackup执行Summon动画及原生等待，按空slot调用CreatureCmd.Add<TwoTailedRat>，然后同步队友CallForBackupCount；CanSummon另核回合冷却、最多三次、空位置及其他同伴意图。原生CombatManager.AddCreature会初始化AI并按Encounter.Slots重排敌人。这些业务完整复用，不按怪名手写召援、动画或结算；恢复节点时不重跑此业务入口。

新源码采用格式10，保留001格式9兼容并明确允许其精确录制hash。Mechanics.CombatUnits新增有序Enemies：稳定CombatId/Model、slot、存活/HP/上限/格挡、Power、独立RNG、NextMove、AI当前状态/是否执行首招/招式日志和模型派生类标量字段。三鼠的起始招式索引、召援冷却/次数因此成为显式文件事实；原8/9省略该字段，旧片字节不补写。格式10校验敌我ID全局唯一、成员在未来分配器以内及基础内容。原始State的Enemies仍含意图类型/伤害/次数，完整字符串参与原生结果与恢复比较。模型非标量引用图不是通用文件恢复数据。

观看增加纯数据CombatIdentityRegistry，初始化已有成员/未来ID，在原生AttachCreature完成分配后、Encounter spawn回调前登记每个新实际引用，避免同名怪或嵌套生成分配顺序混淆。registry作为memento根与原生CombatState allocator一起恢复；历史节点锚点单独保留，当前分支登记可退回到新单位不存在前。重新前进仍原生创建新引用、获得对应逻辑ID；已访问的召援后点恢复其自己的对象/成员/顺序/AI/私有计数/RNG。死亡移除新单位不会提前倒退allocator。目标在格式10按稳定ID和Model从当前敌人解析，Index保留作录制事实，不把排序后同名其他单位当作目标。

Roster移除/重建扩至全部已知敌方引用；场景AddCreature后登记真实节点/身体，即使该单位同操作内又死也可清理其旧尾迹。跨生成恢复先退役旧场景/订阅，再恢复数据与登记表，对未来分支对象detach，最后只调用NCombatRoom.AddCreature重建当前显示并恢复血条/意图/模型及导航，不调用CombatManager.AddCreature/AfterCreatureAdded，不重新roll AI/HP或触发召援Hook。新场景是否在固定引擎中完整初始化、血条/意图与层级是否正确，仍需要实际运行确认。

格式10恢复前后及Stop对所有已知单位的CombatStateTracker八类原生监听按当前成员清理/恰好绑定一次，并核对旧分支余量零；显示节点自身的Power/死亡/战斗事件沿原ExitTree退订，Osty PCS死亡订阅另沿既有修复。数据memento继续排除delegate/旧Task/TCS/场景；旧格式不新增这套全单位监听重整路径。具体原生字段/方法和回调数量仅经源码/编译核对，未取得运行通过。

默认关闭的开发准备可在下一新铁甲A0选择暗港三鼠首战；保留原牌组/遗物，将原三张防御升级并增加两张全身撞击+，五张机制牌先天保留。变更在checkpoint前进入原生可达存档且写明Preparation；正常新局/Continue/播放不自动注入。拟先防御等真实召援，再对新怪出牌并获胜，短取跨生成Back/新引用再召/召后已访问点恢复与目标/击杀。当前001尚无该入口，不要求用户重roll正在测的三场；若自然遇召援按001真实边界保留素材，非operator/protocol FAIL。

主工程与DirectContracts均0警告0错误。`evidence/content-006/checks/enemy-draft/enemies.json`5组直接合同使用实际registry/IdentityMemento/Codec/ClipStore：同模型排序后稳定目标；反复跨生成恢复成员/AI/RNG/allocator、新引用对应ID及召后旧点的原引用；新怪死亡移除后未来计数不回退；格式10有序敌人/slot/AI计数/RNG生产精确读回；重复/敌我冲突ID和未来计数回退拒绝。既有journal/library 5组、球4组、单位4组同目录通过。它们没有调用原生AttachCreature、召援、Godot节点、订阅方法或任意点冷恢复，不代替原生证明。

D文件事实、原生比较入口、观看内存恢复/新对象分支源码均已接通；任意文件点创建敌人并权威恢复仍未实现。当前安装及人工要求仍为A/B/C的001三场样本，D候选不封存为可用安装/不报通过。自然停点先查001实际运行，再决定同片观看和D后续候选，相关普通bug同包修。

## r1 / 002：六份真实胜利片与跨进程续录；一次分组缺陷，修版待测

用户反馈“CONTENT-006应该可能符合要求？你看看”。核到001三个进程session：`12807da5cd8541da95929c7f1157129a`15事件、`d2d6263883ac4a5fbdbf0478c2d4693a`479事件、`3222682d75ba48bb90e40fb0d7ef5975`41事件，共535。后两进程完成六份格式9猎人普通战斗录像、95项完整原生录制操作、10个选择，六次真实Victory/自动Seal/ExactPayloadMatched；额外生产读取/格式兼容全部通过。无observer、recording-unsupported或playback-failed事件，不能因此声称未记录的路径通过。

| 局/场 | floor/遭遇 | 完整操作/选择 | 真实初始变化 |
| --- | --- | --- | --- |
| 第一局1，7de7f63b | 2，ShrinkerBeetleWeak | 18/2 | HP70、99金、12牌 |
| 第一局2，7f68f015 | 5，SlimesWeak | 22/2 | HP70、66金、15牌，增加Acrobatics/Backflip/CloakAndDagger |
| 第一局3，4b955387 | 6，FuzzyWurmCrawlerWeak | 23/2 | HP70、81金、16牌，再增加EchoingSlash |
| 第二局1，33a9bbfe | 2，SeapunkWeak | 6/1 | HP58、99金、12牌，原生新局牌组含Expose/GrandFinale |
| 第二局2，ad557e94 | 4，ToadpolesWeak | 20/2 | HP52、182金、12牌 |
| 第二局3，8515d8e3 | 7，CorpseSlugsWeak | 6/1 | HP52、118金、12牌，增加Backflip/减少一Strike |

Preparation均null，无本mod测试注入；表中战外变化证明实际后场checkpoint承接了游玩资源，不把每个具体变化武断归因于领奖或商店。第一局同RunId `de425acb7e0c40f482caef605b0e2464`、顺序1/2/3，三场中两场首排后；仍全为Weak遭遇，后续强普通怪尚无样本。

第二局原生StartTime均1793362422。20:14:50第一次同进程Continue返回MatchedObservedSave=false，原RunId `3fd0f55b6eee4fdaab1167f30737e619`被拆为`9ec7bc8c2da049df854d12b70b80765b`，第二场错误标为该组Battle1；这是实现分组缺陷，不能验收为正确同局分组。20:16:07继续已完成房间状态匹配新组；20:17:20在新进程启动后再次Continue匹配同一新组，并从随后第7层独立录成第三场。故跨进程后续录实际成立，但不抹掉前一次误拆。三次Continue均没有以continue-known直接录重进未完成普通战斗，当前房间新attempt重录仍未测。

新格式9原片没有任何native-playback-open；C仅有菜单列表生成证据，还没有较早/较晚新场独立播放/Back/胜利/Stop的运行证明。第一进程通过battle-list打开旧格式5储君，日志在开场/控制阶段结束，无Stop/关闭记录；用户已被异步询问当时手动关/强退还是崩溃，未回答前记Unknown，不能称观看隔离通过。第二进程通过recent打开旧Osty后Stop0，Error null、原manager/所比较Continue与进度恢复，此为旧片开场退出证据，非新片多场观看。

六原片SHA/资源/分组与session事实见`evidence/content-006/runtime-verification-r1.json`，六份额外生产读取见`checks/continue-boundary/read-*.json`。535事件+六原片+六catalog+原journal+最新Godot共549数据文件逐hash封存`runtime/content-006-r1-20261030-202155`。原始引擎RID/resource等退出诊断保留，不因mod错误计数0而抹除。最新进程有Quit confirmed/Steamworks shutdown和game0；原片不修改，不把旧错误分组重写成成功样本。

### 修复：把身份关联绑到原生实际读写边界

001只在ToSave返回时登记指纹，并到SetUpSavedSingleplayer前才序列化传入DTO。源码确认NMainMenu先取得LoadRunSave结果、调用RunState.FromSerializable，再调用SetUpSavedSingleplayer；ToSave观察也不等于实际存储内容。001未记录第一次误拆前的原始读取DTO/写入内容，不能指认具体变动字段或宣称已完整证明唯一根因。检查过CanonicalizeSave后确认它在固定代码只由多人加载调用，不用它解释本次单人故障，也未hook或改动该路径。

002在正常SaveManager.LoadRunSave刚返回时冻结实际DTO JSON并绑定该对象引用，早于RunState还原/菜单后续处理；Continue优先用这份原生加载起点识别/重进，不以较晚DTO替代。仅正常manager采集，观看isolated manager排除；观看Stop返回原manager的菜单读取仍可登记，不开启普通录制。另包装正常RunSaveManager.SaveRun(SerializableRun,bool)的返回Task：保留原异常/取消，只有原生保存成功后从该manager的实际store/本profile当前run路径读回内容，按保存开始时捕获的Replay RunId登记。Replay不写普通存档；观察失败经Guard报告，不把原生成功保存改成业务异常。只观察当前明确新局/Continue的普通单人录制，观看不走写入登记。

新增日志区分loaded/setup两份SaveKey、是否在SetUp前变化和实际原生保存读回键；不放宽为seed/character/坐标匹配，也不忽略更多游戏字段。旧三组及原始6份录像保留为实测事实，未制造历史同局成功结论。实际边界回读、主线程耗时及第一次关联问题是否消失仍需修版运行证明。

主工程/DirectContracts0警告0错误。journal/library现7组合同，增加模拟实际写入与较早ToSave不同、读取后DTO变动仍使用冻结起点，以及保存完成关联捕获的run而非后来current run；5组enemy-generation合同和六原片生产读取通过。这些没有实际调用新的Harmony/native store方法，不称修复通过，D节点/监听/召援原生仍Pending。

用户本批Cloud仍按明确关闭处理。原账号/game0核对，新游玩后已知51文件、3,978,222 bytes补存`CONTENT006-post-r1-current-20261030-202547`并逐hash读回，收据`evidence/content-006/post-r1-current-files.json`；是已知当前文件而非完整新账号快照，无history/其他账号遍历、无restore，当前游戏进度保留。002已安装，DLL SHA256 `A99D1CAA46E374A141E415A90C194B41A2CA2833E009B3B3B13F460B0CB8AF57`，manifest同前，收据`evidence/content-006/preparation/build-002-1fc81a01.json`。001 pair/config及六原片/原journal/重要失败保留，不自行启动游戏。

下一无需重录三场：继续现有猎人局，下一普通战斗正常完成/领奖/存退，核002读取/写入边界日志与既有组延续、强普通遭遇；当前存档下一普通为SeapunkNormal，但不替用户消耗RNG/选路线。然后从第一局列表打开第1场与第3场，各自直接初态/原生结果匹配，合并Back/胜利/Stop及切换另一场隔离。D若自然未遇，沿自然停点另用默认关闭三鼠准备取真实召援/跨生成/再召/新目标/击杀证据，不让用户重roll既有三场或把范围内生成算操作者FAIL。整局外壳保持下一阶段规划。

## 用户澄清旧储君退出 / 003：暂停菜单保存退出死等修复候选

用户回答已问的19:45旧储君退出情况：在回放里试“保存并退出”后，所有可操作按钮失效，随后手动关游戏重启。第一进程记为**回放退出路径缺陷及用户手动终止**，不再是退出原因Unknown，不称游戏崩溃或成功Stop。r1摘要FirstOldPlaybackExit已补此说明；原15事件与原始证据不改。实际日志第一次stop Accepted true、第二次false，停在“正在结束当前整项操作”，没有driver/playback closed，符合尚未完成退出的事实。

固定原生NPauseMenu.CloseToMenu会禁用各按钮后调用NGame.ReturnToMainMenu；暂停capstone打开时CombatManager.Pause，关闭capstone才Unpause并触发子菜单OnSubmenuClosed/清除热键阻塞。旧拦截仅把ReturnToMainMenu变成Stop并立即返回已完成Task，没有关闭暂停capstone。NativeSeek.Drain的Quiescent又依赖LiveState.Playable，而Playable要求CombatManager未暂停；开场0没有在途业务也会继续死等，原暂停按钮已被禁用，形成用户可见的卡住。源码调用链与用户/既有日志一致；没有用这项解释另一件Continue分组问题。

003仅在观看Active且未TearingDown时，提前接管NPauseMenu.CloseToMenu，在原生禁用按钮/StopAll之前请求Replay Stop、清旧计划，再通过原生NCapstoneContainer.Close关闭菜单，解除CombatManager暂停及原生菜单/热键状态。fallback NGame.ReturnToMainMenu同走这条退出路径。选择/动作仍沿原生安全收束和既有观看隔离恢复，不把业务Task强制标完、不调用正常SaveRun保存观看内容，不改普通游玩CloseToMenu行为。

每个Open另有自己的新完成信号，原菜单退出调用者拿到的是等待实际观看清理/原context恢复后的Task；不再立即返回假完成。该信号独立于游戏动作/旧TCS，最外finally总结束，旧会话完成不会误通知后开的会话。新增playback-menu-exit-requested记录关闭前后暂停状态、native capstone关闭、未请求普通保存及真实等待。003仍需要游戏中实际按钮/解除暂停/driver关闭/manager与Continue比较证据，构建不是运行PASS。

主工程0警告0错误。仅改退出路径与显式002录制兼容，不重复未变的离线矩阵或造只镜像实现的UI假测试；人工检查合并既有早场录像Back/Step后在战斗中点原生保存退出，后场播胜利后Replay Stop。002未发现新进程/运行数据，不把002实际写→读→Continue验证拼成已过。为验证B，下一普通战斗领奖存退后再Continue进地图、再存退，必须核到本修复版本自身成功写入后的那次读取与Continue/组延续，不只看001原存档第一次能载入。

安装前game0；与post-r1补存的51已知文件比较，仅settings.save因002准备配置变动，当前run/progress等其余50文件hash相同。`evidence/content-006/pre-003-known-current-check.json`记录这一点；003准备额外保留当前settings/selector及002 pair/config，未把配置变化称新游玩进度或整体回滚。003 DLL `F8758053902DA8793F259C6CC44192720873513346C3F77950821068A101DF50`，manifest同前，收据`evidence/content-006/preparation/build-003-95eec260.json`，仅ReplayMod/modded1，沿本批Cloud关闭，无restore/其他账号遍历/自行启动。002/001 pair、原片和旧失败保留。

当前交用户步骤为：继续现有猎人局一场普通战斗、领奖存退→再Continue到地图后再存退；第一局de425acb第1场独立打开后做Back/Step并在战斗中用原生保存退出；第3场独立播放到胜利、相关Back重做及Replay Stop，然后正常关游戏。无需重录旧三场。D召援样本及未完房间attempt仍留待自然停点，CONTENT-006不关闭。

### 004安装取代未运行003：全局fallback不与原生业务形成等待环

003安装后、用户运行前源码复核进一步区分两类调用者：NPauseMenu.CloseToMenu是外部UI退出，可等待完整观看清理；全局NGame.ReturnToMainMenu也可能由当前原生业务Task调用。若后一类等待观看退出，而Stop又等待该业务收束，会形成反向等待。未见003实际运行/此类死等，不把源码发现当用户新失败。

004保留提前接管PauseMenu、原生关闭capstone/解除暂停及该外部UI的真实清理完成Task；global fallback仍请求相同Stop/菜单关闭，但立即返回以允许原生业务自然结束，随后由观看循环完成退出。沿原有business fallback早返回合同，避免为了修菜单引入业务循环；不提前标记任何真实动作Task完成。普通游戏仍直接执行原函数。003完成信号最外finally/每会话归属保持。

主工程0警告0错误、diff-check通过，不重复未变的旧矩阵；004实际菜单、边界读写/Continue、早晚片观看、D仍未跑。game0安装004，DLL `11B57F1F51A35B21D6B62A1303E06FB9D4360F0718671D54A143EF3FAC21D17C`，manifest同前，收据`evidence/content-006/preparation/build-004-323f7a3d.json`。003/002/001 pair/config保留；本次没有新游戏进程/正常游玩进度、无restore/自行启动。用户下一步骤保持上述合并短测，当前版本以004为准。

## r2 / 005：保存退出实际通过；按用户确认修旧样本分组索引

用户确认“现在保存并退出正常了”，并说明原先是**两局，每局三场**，第一局连续，第二局中途在问号及战后SL。不是三局；也不把问号变成战斗房间或新run。r1原生start_time/同一进程Continue/实际checkpoint与六片分布和用户说明一致，第二局的3fd0f55b/9ec7bc8c是001误拆出的两个Replay身份。原始错组仍是有效失败，下面显式修正不当成自动分组修复已过。

004/session `4aa9a617f5124438ab0dc5aed6fc6a36`82事件，全部从battle-list打开新格式9片：33a9bbfe一次、ad557e94两次，各自Initial匹配。第一份出1个完整原生操作/结果匹配后Stop1，后两次Stop0；三次Error null、原manager/所比较Continue和progress恢复。没有Back或完整胜利，不能把这些打开当早晚全场已验收。无新录制/无强普通或D样本。

第三次ad557e94在0通过原生pause-save-and-quit退出：PausedBefore true、After false、NativeCapstoneClosed true、NormalSaveRequested false，约169.33ms后playback-closed0，Error null、隔离比较通过，用户视觉确认正常。这一精确入口/点位的退出修复取得原生证明；不外推所有在途/选择/异常退出。牌监听及Osty清理沿日志保留，未出现observer/playback-failed，最新正常Quit/shutdown/game0。

本进程20:47:03.767有一次Continue，匹配9ec7bc8c、PreFinished true、FromNativeLoadSnapshot true、Loaded/Setup键相同（ChangedBeforeSetup false）。随后20:47:03.795记录原生成功保存读回，20:47:07.964菜单再次加载同键；说明实际write和后续load路径已运行。但此次Continue发生在write之前，**没有该write之后的下一次Continue**，仍未完成PM要求的实际写→读→Continue闭环，不用001已知存档第一次匹配替代。六原片之外没有新战斗文件。

82事件+两份实际打开原片+校正前journal+Godot共86数据文件逐hash封存，见`evidence/content-006/runtime-verification-r2.json`的ArchiveRelative；原片同r1 hash不变，原始引擎退出诊断保留。用户本轮两局说明已写派生摘要，不回改raw。

### 005：显式、有证据的旧索引校正，不修改录像载荷

RecordingJournal新增可选Corrections（旧journal仍可读）。仅显式调用、具证据且两组已有房间不重叠/场次连续才允许把旧alias连到canonical；重复同一校正幂等，拒绝重叠房间/嵌套关系，不凭seed自动合并。这里按用户确认及r1共同start_time/seed/顺序，将9ec7bc8c作为3fd0f55b的延续、旧场次偏移1；原journal run条目及旧clip事实保留。菜单最近详情和列表都应用显示映射，原catalog存原字段不重写，六份封存JSON及其checksum完全不改。

Canonical的已知房间/attempt计数承接两段，实际SaveKey若原来指alias，后续Continue解析到canonical；成功保存关联比较canonical身份，避免因显式校正误标ambiguous。下次新普通战斗应接本局第4场；同坐标正常重进仍沿既有新attempt机制。未写普通存档、未注入expected/RNG，也不凭索引校正宣称原自动分组根因已证明修复。

当前game0下先封存校正前journal并补存已知51份当前文件3,966,529 bytes（`CONTENT006-post-r2-current-20261030-205256`，逐hash读回、非完整账号快照），再用真实生产journal方法作本次显式校正。读回显示恰好两组各3场，组内1/2/3；所有六份原片hash及native current_run hash不变。校正前后journal另双份保留，before `7ED815DA391F954DDBF3E07979D3A9CF0AD0131A603626518DD49D24034C016C`，after `06A4C13E948EB76A5489885D67B468E92BEA8A21F327C2F995B4B3719FF6EC8B`；位置及逐片视图在`evidence/content-006/run-index-correction.json`。这是真实索引/文件读回证明，不是Godot列表渲染或后续原生续录已过。

主工程/DirectContracts0警告0错误；journal/library现10组，新增旧分组显示合并/重进后counter第4/alias保存不误ambiguous、校正列表不重写真实ClipStore载荷、重叠房间拒绝与不同新局不合并。输出`checks/run-correction/library.json`，仍非实际原生创建/渲染证明。本次自然源码修订也把ExitFromMenu日志等待字段改为调用者显式传入：PauseMenu true、global business fallback false，控制流不变，不为该字段另加测试。

005已安装，DLL `07CE397A9450ACB0D15E4B4B0630B7343B07668448F5EB7CE9A914ECE57E3E3F`，manifest同前，收据`evidence/content-006/preparation/build-005-f239a8aa.json`。004/003/002/001 pair及失败素材保留，原账号/game0/本批Cloud关闭，无restore/其他账号history遍历/自行启动。原生SaveQuit已按004/r2通过，005只改索引/显示/计数及日志参数，后续索引在实际菜单、写后Continue和第4场仍待看运行；本包不关闭。

下一不重录六片：先确认菜单确为两局各三场，可用第二局3fd0f55b第1/3场（均6操作）合并早晚独立完整回放、相关Back与胜利/Stop；沿现有Continue进地图→存退→再Continue取实际write后的匹配，再在自然停点正常游玩下一普通强怪及D。未完成的原生路径不由本次用户身份澄清替代。

## r3 / 006：早晚完整回放和已完成房间写后Continue通过；GrandFinale在途体验待修版

用户按原步骤测试，报告华丽收场长动画中Back：遮罩长，攻击声后才揭开，仍见花瓣尾迹。其余测试完成。005回放session `af0477bb15db42f3b8726882bee5333a`649事件，后续新进程Continue session `0320da5cabe14971916bc047d7f22e23`13事件，共662；无新录制/强普通或D样本。两份原片不改。

第二局第1场33a9bbfe（floor2，HP58/99金/初始牌组）与第3场8515d8e3（floor7，HP52/118金/牌组已变化）均通过普通battle-list从自己的checkpoint独立加载、Initial匹配，没有先播前场。22次原生结果/RNG匹配中正常17、Retiring5；13次Back到已访问完整点，状态/RNG/模式及清计划匹配。九次真实终局投影/原生VictoryTask匹配均是Host6 GrandFinale：正常终局到达4、被撤销动作退役收束5，是两片的首次及重做，不写成九场独立完整录像。已观察每份整片从开场到原生胜利，也反复跨胜利Back再执行。

最后分别继续Back后Stop4/5，而不是Stop6；两次closed Error null、原manager/所比较Continue与progress恢复，driver闭合Outstanding false、Auto false、Pending0，牌及Osty清理按原日志保存。最终AllOperationsMatched/VictoryReached false是停在回退后位置，不否认之前记录的完整终局，也不把最终退出虚报为胜利终点。用户未报此次普通前进/早晚初态异常；数据闭环与下述在途体验缺陷分开。

新进程005在21:06:48.396原生SaveRun成功并读回canonical3fd0f55b的实际键；21:06:53.263重新load同键；21:06:56.645后续Continue匹配该canonical/LoadedSnapshot true，随后再次成功写入。实际write→load→Continue这一对完整发生，已超过此前只读001旧存档的证据；loaded/setup键相同，ChangedBeforeSetup false。此样本PreFinished true，是已完成房间继续/地图的关联，不等于未完成普通战斗newattempt或第一轮误拆的具体字段根因已复现修好。没有回到额外错误组。

原始662事件+两原片+三个相邻Godot日志+当前journal共668数据文件逐hash封存，位置/逐结果归属/13条耗时及实际写读Continue时间见`evidence/content-006/runtime-verification-r3.json`。正常Quit/shutdown与game0核对，无observer/playback/retired-timer故障。原始引擎诊断保留。B所测完成房间关联及C早晚单场独立原生观看/Back/终局/Stop按005/r3取得运行证明，不外推冷恢复/强普通/召援/未完房间。

### GrandFinale在途Back：同包体验缺陷，006已装待测

五次Retiring GrandFinale均按原生业务收束后再退，退役约156.8–1397.6ms、总到达499.2–1752.1ms；四次较长在约1.46–1.75秒总范围。稳定终局Back另有几十ms，不混入在途优化对照，也不以非配对请求时点算固定百分比。用户观察的花瓣/声效是有效可见缺陷，不用数据匹配代替体验通过。

源码确认当前退役等待只绑StartTurn/EndTurn作用域；GrandFinale.OnPlay原生先Create/AddChild视觉，再唯一一次Cmd.Wait(totalAnticipationDuration=1.625)，随后走正常AttackCommand伤害。该纯演出等待完全未进入旧broker。NGrandFinaleVfx与Impact的Create是直接factory，不经VfxCmd，旧SourceVisuals未登记；它们视觉自己的PlaySequence仍会继续，后者约2秒，导致回退后尾迹。两种视觉ExitTree只取消自己的视觉CTS，不是牌效/伤害Task。

006给真实PlayCardAction.ExecuteAction加入AsyncLocal根操作归属，绑定当前Pending引用/CombatState引用/NativeSeek代；StartTurn同样要求真实EndTurn根。复用RetiringPhaseWait，只在Back/Seek退役释放当前/后续已审纯计时，不取消原生timer，不结束游戏业务Task。共有TriggerAnim/Damage等既审调用可在Card根内归属，补共享AttackCommand.Execute的纯before-hit等待；GrandFinale仅审其固定OnPlay唯一1.625秒演出Wait，不泛化为所有卡Cmd.Wait，不按卡名写效果/动画，选择/Hook/未知等待仍原样。原OnPlay及AttackCommand/伤害/AI/Hook/RNG继续完整原生执行并核对。

两类纯视觉Create的底层重载登记到现有SourceVisuals（同一节点HashSet去重），DirectCheckpoint恢复前沿既有统一清理退出整棵节点、让各自ExitTree取消视觉CTS；不恢复旧视觉协程、不人工播放花瓣终帧。新增retired日志列出SourceEffects类型/数量，便于核实际登记/清理；实际GPU节点退出/尾迹是否消失仍待人工观察和原生日志。

撤销根操作的音效只按真实owner退役。Debug音效以manager引用+native id归属，正常Play仍执行以保留pitch RNG/池行为；退役时精确停止该owner已在播或随后新播的id，已过期条目清掉，不StopAll/不波及菜单其他声音。新FMOD one-shot仍执行原函数但该owner退役阶段volume为0；普通录制/播放音量不变。其他owner/room/代不匹配均不改，统计stopped/muted进入retirement日志。这是复用原生声效生命周期，不自写攻击声音时序；停止池行为与跨代隔离仍待实际运行确认。

主工程0警告0错误、既有4组纯timer async合同（正常异常/取消、释放当前但原timer不取消、后续代隔离、迟到fault观察）通过，见`checks/card-retirement/phase-wait.json`。它们未运行新的Harmony根归属/直接caller绑定/音效池或Godot视觉退出，不能作为优化PASS。无需重复005已过的两片全场/Continue或004菜单退出矩阵。

原账号/game0及当前已知51文件3,966,529 bytes另存`CONTENT006-post-r3-current-20261030-212652`逐hash读回，收据`evidence/content-006/post-r3-current-files.json`，非新完整账号快照，无restore/history/其他账号遍历。006已安装，DLL `055C85CB7DDEF560086EE7DF039066981C9CB62E6068318B5385A34BD930FEE6`，manifest同前，收据`evidence/content-006/preparation/build-006-554a8ed2.json`；005及此前pair/原片/失败证据保留，Cloud沿本批确认关闭，无自行启动。006在途体验修复Pending。

下一只同片短测：第二局第1或第3场推进到5/6，`>>`打华丽收场后在聚光/花瓣演出未完时Back，反复2–3次，观察遮罩耗时、是否仍冒出已撤销攻击声/花瓣；核root/caller/released及VFX清理、原生结果/RNG和回到5/6。然后正常再打完6至胜利并Stop、正常关游戏。无需重录或重跑两片完整前缀。强普通、D召援和未完room attempt在此自然停点之后继续本包，不以视觉小修关闭或替代任务。

### 007：006运行前补强根归属继承及声卡播放前静音

006尚无游戏运行时继续源码复核：派生/自动PlayCardAction若不等于手动Pending.Action，应保留现有有效父owner，而不是置空，避免丢掉EndTurn或组合牌根中的已有归属。007仅为真实手动Card根建立新owner，派生动作继承父；CurrentOwner仍逐项核Pending引用、room引用和代，旧上下文不能误绑定下一步。没有新增玩法/重写自动牌，不把预运行修订记用户失败。

Debug音效除按owner精确Stop外，007在原生Play前把撤销owner音量设0，然后让Play仍完整执行pitch RNG/音源池并按其实际ID停止，避免Play与postfix之间的音频线程间隙漏出开头；普通声音参数不变。Muted统计按API调用次数命名OwnedAudioCallsMuted，不当作唯一FMOD事件数量。原等待/VFX方案及全部数据校验保持。构建0警告0错误，实际线程/作用域与体验仍待同片短测。

007已在game0安装，DLL `9BCECE95FF90DE51CB3A06B3E9B5F8069838C1E10F139D0FA43602D22926F0FF`，manifest同前，收据`evidence/content-006/preparation/build-007-1ee95b73.json`。006/005及此前pair/config保留，本轮没有新游戏进程、无restore/自行启动；当前测试以007为准，不重复要求005已经答过的B/C或004退出矩阵。

## r4：007华丽收场在途Back提速/声效/花瓣通过，保留当前版本

用户完成后明确回答遮罩时长、攻击声和花瓣尾迹“都正常，符合预期”。007/session `eff15ca1e3b94fb298e97ae385c49474`412事件，一次普通battle-list打开33a9bbfe原片（格式9/6项/第6华丽收场），Initial匹配。13次原生结果/RNG中正常7、Retiring6；7次Back到5，状态/RNG/模式及清计划均匹配。八次原生终局到达为正常2/退役6，是同一原片的首次/重做，不当八场或八份独立录像。

六次真正in-flight GrandFinale Back均实际Released1/Skipped1，退役59.6481/18.4658/17.5755/15.1840/15.0215/15.8155ms，总到达474.2154/361.8846/357.3587/359.0780/351.4746/362.9596ms。另一次已到稳定胜利后Back总36.458ms、released/skipped0，不混作在途提速证据。005/r3旧真在途499.2–1752.1ms与新351.5–474.2ms分别留存，请求时点不严格配对，不宣称固定百分比；用户此次体感确认是独立依据。

43条真实pacing绑定含8条native-card-visual-anticipation，对应该片八次GrandFinale；其余StartTurn2、Damage10、TriggerAnim20、PerformMove2、PerformIntent1，当前owner/room/gen检查通过。共享AttackCommand.before-hit本样本未触发，不能补判其独立运行覆盖。六次退役各精确停止1个Debug声、静音API调用3次；原生Play仍跑，音源池/pitch路径没有被全局跳过。七次显示退役均登记/退出一个NGrandFinaleVfx及一个Impact，还有相应伤害字/火花；用户确认揭罩后不再见尾迹，原生取消视觉CTS未带来mod或late timer fault。

最后正常再做第6步到真实胜利，Stop6 AllOperationsMatched/VictoryReached true，Error null、Outstanding false、Auto false/Pending0，原manager及所比较正常Continue/progress恢复；牌/Osty订阅按日志清理。正常Quit/shutdown/game0，原片SHA `F50C99D4EE6AE3B4571AB9A288EE3E61EE3783C480A6E984CC1F9E825EB22472`未变。没有新录制或下一强普通/D/未完房间样本。

412事件+原片/Godot/journal共415数据文件逐hash封存`runtime/content-006-r4-20261030-221541`，摘要`evidence/content-006/runtime-verification-r4.json`，含用户明确回答。未见observer/playback/retired timer fault；Godot原始Invalid Task ID及退出shader/RID等诊断保留，不把mod错误0写成引擎全无诊断，也不无证据归因本次视觉清理。该单片、真在途时点的体验/数据闭环按007/r4通过；未知卡/VFX、其他音效类型、所有并发/异常路径不因此放宽。

不再构建/换DLL或重复GrandFinale/两片/菜单矩阵。原账号/game0下当前已知51文件3,966,529 bytes补存`CONTENT006-post-r4-current-20261030-221927`，逐hash读回，收据`evidence/content-006/post-r4-current-files.json`；非新完整账号快照，无restore/history/其他账号/自行启动。007安装保持，已有恢复pair/失败及索引校正前后材料保留。

下一把后续普通强怪/第4场与未完房间跨进程newattempt合并：Continue现有猎人局，沿普通地图进入下一Monster战斗（存档下一个普通为SeapunkNormal，不替用户选路/消耗RNG）；先打一张普通防御/打击、等该操作完整核对后正常保存退出并关游戏。新进程Continue应从同场原生开场重进，记本局第4场新attempt，不拼退出前步骤；再正常获胜领奖、存退并正常关游戏。预期新封存为第4场尝试1的中断前缀、尝试2的完整胜利，各自真实checkpoint/选择/资源/RNG。没有完整操作时只诊断无假片，此处指定一张稳定基础牌以明确取得前缀。

这不是观看Seek/Back测试或要求一次战斗中间状态原生接续，SL回到房间前是原游戏保存语义；游戏本身重进/存盘不算观看隔离失败。先查实际文件/attempt/强普通/冷进房开场，再安排该新格式10片观看；D三鼠按后续自然停点用既有明确默认关闭准备接原生生成/跨生成Back，不重roll旧六片，不追加旧五角色矩阵。CONTENT-006仍未关闭。

### 后续路径核对：休息点与精英战未生成录像，属于范围限制

用户反馈“似乎并没有保存？因为下一场战斗是精英战”。核对当前原生存档：同局StartTime 1793362422，先经过休息点升级Neutralize，再进入row8/col6的ENCOUNTER.TERROR_EEL_ELITE；当前pre_finished_room为elite且is_pre_finished=true，玩家HP19。原生游戏进度已保存，latest录像仍为8515d8e32c9249deaeeea611bc6f974f，没有新封存。

两次后续session 39343403e57f48f8bf198148b1e9df33、901d736d0f0a487b89e633a1cec778e8均有原生写盘/Continue身份观察，解析到校正后的同局3fd0f55b6eee4fdaab1167f30737e619；未见recording-room-checkpoint/start或clip-sealed、observer-error。源码入口明确只收MapPointType.Monster，精英房间保留_enabled以待下一受支持普通战斗，不生成尝试或假片。此为范围外路径观察，不判普通战斗录制失败，也不补判第4场/未完房间newattempt通过。

前述SeapunkNormal是下一普通遭遇池，并非保证地图紧接着就是普通战斗；操作说明应强调普通怪物节点，不能让用户为此重打精英。007保持安装，不回滚进度、不扩精英支持、无新DLL或自行启动；游戏已退出。下一次实际进入首幕普通Monster房间时继续既定第4场/未完房间测试。

## r5：第4场后续普通群怪两次尝试封存通过，同进程重进与跨进程分开

用户完成下一普通战斗保存退出、重进、获胜后正常退出。007/session 650174eb3f9e487097c1c329aa650fa7（2026-10-31 00:27:56–00:29:52，86事件）实际进入第11层row10/col6的ENCOUNTER.SEAPUNK_NORMAL，CalcifiedCultist/Seapunk两敌，属于后续普通群怪。沿校正后RunId 3fd0f55b6eee4fdaab1167f30737e619，正确记本局第4场，无Preparation。开场HP19、金币234、牌组13；包含精英后获得的BookOfFiveRings和第10层Planisphere及升级Neutralize等真实资源，不恢复旧首战基线。

尝试1 `2a9fa4fc286c47c889fd4d2d807c6fa6`格式10，map入口，完整执行一张Expose后正常存退，封存1操作/0选择的interrupted片段，Victory false，未强补EndTurn。文件194,198 bytes，SHA `D65B1E2E32F533C3365E9A4D9B10EE1EB1238447819E89CF6B6B60E4E84A36E0`。尝试2 `a97044e637a347db873713727f605a5b`格式10，continue-known入口，14操作/0选择、最后GrandFinale真实胜利自动封存；文件918,515 bytes，SHA `4782AF8D853D14FC7FC53D76CDE52EF6A15755C6ED81201926A99DF6B2243A0D`。两片均原生clip-production-readback ExactPayloadMatched，尝试2从独立开场重新以Sequence1记录Expose，没有续接尝试1完成后的状态或操作序列。

两个checkpoint均在房间创建前，pre_finished_room null；仅去除原生save_time/run_time/num_reloads后内容相同，Initial原始投影字符串完全相等（含开场手牌/单位/AI/RNG）。00:28:49成功原生写入key40C250…→00:28:58实际load→00:28:59 Continue及checkpoint命中同局，Loaded/Setup一致、ChangedBeforeSetup false、ReusesOldOperations false。尝试1与2分立而同局同场，胜利完成与短片边界真实；同进程未完房间newattempt路径通过。

严格保留覆盖界限：本次三次menu-ready的ProcessId均108208、同session/同ProcessStartedUtc。中途是保存返回主菜单后Continue，不是关掉整个游戏再启动；最终退出是真实Quit confirmed/NGame.Quit/Steamworks shutdown，当前game0。此前r3已完成房间write→新进程Continue闭环不替代此未完房间跨进程。当前不要求重打这场，将未完跨进程合并后续D三鼠准备。两片尚未实际打开观看，故不把录制原生读回等同格式10冷开场/前进/Back/Stop通过，也不补判D一般敌方召援。

前一session178a7f2976a04dfb998b8d23213d82d2的9事件与本次86事件、两原片/Godot/journal合计99数据文件逐hash封存`runtime/content-006-r5-20261031-003209`；摘要`evidence/content-006/runtime-verification-r5.json`。未见observer-error，本次无播放事件；引擎退出shader/RID/resource诊断原样保留，不声称引擎零错误。既有DirectContracts对真实尝试2做生产ClipStore完整读取/兼容和路线终点检查通过，见`checks/r5/victory-production-read.json`；该检查不运行Godot或原生观看，尝试1实际生产封存读回由原生事件提供。

当前原账号已知51文件4,002,995 bytes补存`CONTENT006-post-r5-current-20261031-003209`并逐hash读回，收据`post-r5-current-files.json`；未遍历其他账号或新建全账号快照，无restore/新候选/自行运行。007保持，下一最短人工观看：列表第4场尝试1播放唯一操作到片段终点并Stop；独立打开第4场尝试2，完成第3项BladeOfInk生成小刀后Back至2、重做第3项，再Auto至真实胜利、Back13重做14，Stop并正常退出。无需重录、继续推进地图或重跑旧片矩阵。CONTENT-006保持未完成。

## r6：格式10中断片段与后续群怪完整片原生观看通过

用户报告“测试完毕，一切正常”。007/session `1c6187afff6a47c79160820328a9a431`（2026-10-31 00:37:59–00:40:25）599事件，普通battle-list依次独立打开第4场尝试1 `2a9fa4fc…`及尝试2 `a97044e6…`，均InitialMatched true，未要求先播前一场。两片原始文件SHA与r5一致。

一步interrupted片段实际2次原生结果/RNG、2次Back到0均匹配；过程中也退到pre-room重开，再正常进入初态，没有把无胜利的一步片强行补成完整战斗。完整片20次结果/RNG、7次Back均匹配，覆盖第3项生成小刀后回2重做、使用小刀后回3/回2、3次胜利后回13（前两次重做至胜利，最后一次Stop）；该片三次真实终局为同一片首次/重做，不是三份新录像。总22次结果均NormalNativeBusiness/Retiring false、9次恢复均StateAndRngMatched，模式/Auto/清计划匹配；开场重建另有2次，不混算9次已访问点恢复。用户实际画面通过与数据核对分别记证。

最终玩家额外Back后Stop位置分别0、13，因此playback-closed的AllOperationsMatched/VictoryReached为false；这是当前停止位置的真实值，不改写为Stop在终点。完整片此前实际已播放到14并核胜利三次。两次Error null、OriginalManagerRestored及NormalContinueAndOriginalProgressUnchanged true；driver关闭均Outstanding false、Running/Auto false、Pending0，卡/单位相关监听日志完成清理。正常Quit confirmed/NGame.Quit/Steamworks shutdown，game0；没有要求为了Stop终点字段再跑已完成样本。

599事件+两原片/Godot/journal共603数据文件逐hash封存`runtime/content-006-r6-20261031-004235`，摘要`evidence/content-006/runtime-verification-r6.json`；mod observer-error 0，原始引擎诊断保留，不据此声称引擎零错。逐hash比较r5已知51文件，current_run.save及主progress.save未变，仅progress.save.backup变化；不得把51项全未变作为结论。当前51文件4,003,071 bytes补存`CONTENT006-post-r6-current-20261031-004235`全部读回，收据`post-r6-current-files.json`，保留变化后backup且不restore。无其他账号遍历/整账号新快照/新DLL/自行启动。

A/C本次格式10后续普通群怪和短片独立观看、相关Back、Stop隔离在此样本通过；未完房间跨进程与D一般敌方召援仍Pending。下一沿既有默认关闭的三鼠准备，不新增构建：主菜单展开开发准备、启用CONTENT-006三鼠召援，开铁甲A0新局进入首普通。明确它会替换当前猎人的Continue，猎人当前进度已有独立恢复点；测试准备只在新局创建时改暗港首普通遭遇为TwoTailedRatsNormal、升级原三Defend/添加两BodySlam+，五牌加原生RoyalApproved先天/保留。准备发生在真实checkpoint前，正常Continue不再注入、观看不注入expected。

合并未完跨进程最短动作：首战打一张Defend+并等结算，原生保存退出到菜单后再正常退出到桌面；重新手动启动后Continue。以不同ProcessId/ProcessStartedUtc和真实写盘→load→Continue核同run/同battle/newattempt，不能只看菜单重新出现。重进先使用可用防御拖回合，暂不攻击直到原生出现援军；再用全身撞击等优先攻击/击杀新来的老鼠并正常获胜，自动封存后正常退出。先核录制/真实生成/目标/AI/RNG文件事实，再给该片跨召援Back/重做/已访问后点恢复与Stop的精确步骤，不要求用户一遍完成未说明索引的观看。召援采用原生AI/75%条件分支，不手改计数或强迫召援；本包仍未关闭。

## r7：007三鼠准备导致两次地图生成失败；008修正原生卡牌归属，待重测

用户“尝试了两次，都报bug了”。007/session `a1212bd6065648de8e09bf80dd03a6b9`（2026-10-31 00:56:19–00:58:14）12事件，两次test-encounter-prepared后都未到recording-room-checkpoint/start，无seal。Godot两次相同`Exception starting singleplayer run : NullReferenceException`，首栈RunState.Contains(AbstractModel)→IterateHookListeners→Hook.ModifyGeneratedMap→RunManager.GenerateMap，失败在新局地图生成，未进入召援或回退。observer-error为0不能判成功，异常由原生StartRun捕获并回菜单，按实际FAIL保留，不要求用户重复旧版或退役工程。

源码与固定本体反编译定位：D分支两BodySlam使用ModelDb.Card<BodySlam>().ToMutable()后直接Deck.AddInternal。原生CardPile.AddInternal只加牌堆，未设Owner/未加RunState._allCards；RunState.Contains对卡牌及附魔会解引用Card.Owner.IsActiveForHooks。原有各角色其他准备均用RunState.CreateCard，该入口原生AddCard(card,owner)完成Owner/登记并AfterCreated。008仅把D两牌改走同一原生入口，仍显式Deck.AddInternal；附魔/升级/遭遇准备范围不变，新增D准备后全Deck Owner==player且ContainsCard的校验，并在准备日志列DeckOwnersAndRunRegistrationValid。未用异常吞掉/默认Owner猜测/跳Hook/人工地图替代处理，也不改一般敌方召援逻辑。真实新局校验是否通过仍待人工，不把源码核对当运行PASS。

008显式追加已知007 hash `9BCECE95FF90DE51CB3A06B3E9B5F8069838C1E10F139D0FA43602D22926F0FF`读取兼容，保留r5/r6两份格式10原片，不允许任意ModHash。Release构建0警告0错误；对真实007胜利原片执行既有生产读取/路线终点/原字节不变检查通过（`checks/rat-owner-fix/007-victory-production-read.json`），该离线检查不运行新局、Godot Hook或D召援。不为简单工厂修正造镜像owner单元测试，也不重复未变的5组生成纯模型合同冒充原生新局证据。

12原事件+Godot/journal共14数据文件逐hash封存`runtime/content-006-r7-failed-20261031-010121`，摘要`runtime-verification-r7-failed.json`。本次读取当前原生current_run时发现已无该文件，随后逐个既有51路径确认仅current_run.save及其backup不存在；其余49文件3,850,593 bytes逐hash补存`CONTENT006-post-r7-failed-current-20261031-010121`，缺失明确进入snapshot/`post-r7-failed-current-files.json`，不伪造51文件全有或恢复旧文件。失败后的缺失本身保留为事实，未凭空判断正常保存或游戏进度未变。最初归档尝试在读取该缺失文件时停止，已有私有副本保留，随后以上14数据文件的完整归档读回成功；未改原文件。猎人r6恢复点继续保留，无自动restore/其他账号遍历/全账号新快照。

游戏0下008已安装，DLL `AAC068D2477D6FEB9954D7C8FFAB6FB80074739DB60E2E572F083041E30C9CCD`，manifest仍`69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`preparation/build-008-97365a21.json`。007 pair/config及此前失败/原片保留，Cloud沿当前批次已确认关闭，无自行启动/换账号/恢复旧存档。下一重新启用默认关闭三鼠准备、开新铁甲A0，先确认首战可进入；能进入则继续此前一张Defend+结算后保存退出、完整关到桌面、手动新进程Continue、等原生召援并击杀新鼠/胜利封存退出。未完跨进程与D仍Pending，本包未完成。

## r8：008新局与真实召援录制通过，未完跨进程实际执行但自动分组失败

用户“测完了”。008/session `99eb4c9b54b34e619f0a3b90bad7d100`17事件与`3171386e11484889a57e78b62b75dd07`215事件，合计232。PID119392/Started01:06:53→PID119952/Started01:13:34，原始两份Godot保留正常退出/重启事实；不是同进程菜单Continue。新局仅一次test-encounter-prepared，DeckOwnersAndRunRegistrationValid true，成功进入首普通三鼠，007两次GenerateMap失败的008修复在此原生样本通过。重启后无再次准备事件，不重复注入。

第一次原片`e964ac3aeed84826b80d6b193e326e93`，格式10，原Run bb6f010bd4ea47ffadd36b78ea243d98，第1场/尝试1，实际Preparation为三鼠准备，打一张Defend后中断，1操作/0选择，175,384 bytes，SHA `1DB9755EDAC3D61FD373259BABF356365FB2E481B4A8C9B5AB72ED797C1A6E47`。重启后原片`660995ce527442be9adf1895ddce7d8a`，格式10/50操作/0选择，胜利自动封存，2,677,973 bytes，SHA `9FA826B86414B67D3D9BC08F8E42440F44761D38ED366E87F15140953974B26B`。后片Preparation null是实际代码未在Continue设置准备模式，来源由前进程prepared事件、相同checkpoint及已序列化牌组证明，不伪称普通无准备局或再次注入。两片原生生产读回ExactPayloadMatched，Initial原字符串完全相同，第二片独立从Sequence1重录，没有续接退出前操作。

真实单位轨迹：初始ID1/2/3；第4项移除1、第12项移除3，13 EndTurn原生召援ID4，当前有序[2,4]/Next5；15项移除2。23 EndTurn召援ID5后有序[5,4]/Next6，展示同模型新对象会插到前面；32 EndTurn召援ID6后[5,6]/Next7。原片21/22/29以稳定ID4为目标并在29击杀，30/31/38以ID5为目标并在38击杀，39/43/50以ID6为目标、最后BodySlam真实胜利。不是静态初始三怪替代召援，也没有按敌方索引重标已有ID；一般敌方生成的录制/目标/AI/RNG文件事实已取得，跨生成显示/Back/再召/后点恢复仍未观看，不补判D通过。

本次未完跨进程自动分组明确FAIL：第一次实际写盘key88EF97…，菜单load及新进程load均3849FF…；新进程FromNativeLoadSnapshot true，Loaded/Setup同3849FF、ChangedBeforeSetup false，但MatchedObservedSave false，原始另起Run19eddce1b08e455693ff722c115d4490的第1场/尝试1，Entry continue-unmatched。不同PID证明跨进程路径实际执行，并不证明身份闭环。检查两个checkpoint全部字段差异只有十个`acts[0].saved_map.points[*].can_modify`从缺省到true（除原生时间/重载字段外）；固定本体SerializableMapPoint明示CanBeModified初始化true、写出WhenWritingDefault省略false。原始文件指纹和成功原生读入DTO再序列化的指纹会不同，旧观察只绑定前者。该地图字段有实际语义，不能简单从SaveKey忽略或宣称完全等价；不据本次发现反推旧001错组的唯一原因。

### 009：登记成功保存的原生读入表示，显式修正本次同房间历史归属

NativeSaved继续先await真实原生SaveRun成功，捕获原runId、读实际store内容并登记raw key，再调用本体JsonSerializationUtility.FromJson<SerializableRun>只构造保存DTO、验证成功后ToJson登记实际原生读入表示。没有创建RunState/执行业务/改变正常save、没有删掉can_modify或按seed/坐标猜同局；独立新局的精确碰撞仍按既有ambiguous拒绝。日志增加NativeLoadRepresentationKey/Observed以便下次核raw→load真实闭环，原native failure/cancel和观察Guard保持。009显式追加008 hash兼容，已有50步片可直接观看。新保存Hook路径及自动跨进程匹配尚待实际运行。

新增显式CorrectReentry只供本次证据修正，不由Continue自动调用：限定alias仅一房间且与canonical房间重叠、已有正尝试计数、无嵌套纠正；记录可选AttemptOffset，显示同一Battle及累加Attempt，canonical继承未来计数，幂等重调不再累加。旧CorrectContinuation仍拒绝重叠房间。新增3组相关合同与旧10组合计13通过：raw/read表征不同仍需明确登记且未观察到的游戏字段变化拒绝、同房间显式修正及重载后attempt3/下一场2、不同房间与多房alias拒绝。它们不执行本体JsonSerializer/Harmony或原生游戏，不代替新Hook实测。

原journal及两片先完整封存，再用已编译纯类型对Replay自有journal执行有证据的19edd…→bb6f…、BattleOffset0/AttemptOffset1修正；列表两片应同局第1场尝试1/2，原片Battle.RunId/Attempt/Entry仍是原始FAIL，不改rawclip/catalog/native save。`run-reentry-correction-r8.json`记录前SHA `8B668C2F3BB39A503F631E50042B2A2705D15B515E9602C1B7A4877EA5B45206`，后`4A3C2AF3D4922FB4558ED8540A2CBA99C755177177E62F03C0A308AC7C6F87A1`，两原片及51个已知正常文件hash均未变。该人工证据修正不是自动分组通过。

232原事件+两片/原journal/两进程Godot共237输入数据文件逐hash封存`runtime/content-006-r8-20261031-012229`，另加纠正后journal读回为238数据文件，摘要`runtime-verification-r8.json`保留RawInputArchiveFiles237；引擎退出诊断保留，observer-error0不掩盖上述身份FAIL。当前51文件4,057,734 bytes补存`CONTENT006-post-r8-current-20261031-012229`，原猎人r6恢复点及r7失败缺失事实保留，无restore/其他账号遍历/全账号新快照。原片真实生产读取/路线终点/原字节不变检查通过，见`checks/native-load-representation/rats-production-read.json`。

009主工程/DirectContracts构建均0警告错误、13合同通过。游戏0下已安装，DLL `163F63D7A8D9042AC9A17AA3F05C4C45CA52AADC12898FE704723CE36A89F0B7`，manifest不变，收据`preparation/build-009-4f319035.json`；008及此前pair/config/失败原片保留，Cloud沿当前批次确认，无自行运行。下一只观看现成50步片：`>>`到13，Back12后重做13反复两次，观察新鼠模型/血条/意图/旧成员；到14后Seek13核已访问召援后点，再Auto胜利（23/32的后两次召援及ID目标也全量核），Back49重做50，Stop正常退出。无需重录这50步。修复后未完跨进程自动分组仍需后续极短原生保存/重启样本，不重跑完整战斗；本包不关闭。

## r9：三鼠召援跨生成恢复与原生重做通过，剩余自动分组短测

用户“结果符合预期”。009/session `7dd1a39b4bae431d83205a86447a8ad7`，994事件，普通battle-list独立打开现成50步片660995ce…，Initial匹配，原片SHA与r8一致。58次原生结果/RNG全部匹配（正常56/退役2），8次Back/Seek状态/RNG及模式/Auto/清计划匹配：五次回12、一次回11、一次回召援后13、一次胜利后回49。真实native factory创建8次，其中ID4共6次（含反复回退后新原生对象）、ID5/6各一次，稳定ID/同模型排序/未来分配器随目标点恢复，非复活旧Task或重放前缀。

回12时成员仅ID2、原生与登记Next均4；回11含ID2/3；回13含ID2/4、Next5且VisitedPointObjectsRetained true，未再次触发spawn业务；回49重建ID6显示、Next7，后续末项正常原生击杀。八次恢复均RoomRebuilt false、PrefixOperations0、SpawnHooksRun false，单位订阅17次核对无retired残留，Stop时Active0；用户确认新鼠模型/血条/意图与目标操作正常。两次实际终局是同一片首次/末项重做，不是两份录像。最终Stop50 AllOperationsMatched/VictoryReached true、Error null，原manager及所比较正常Continue/progress恢复，driver无Outstanding/Auto/待执行项；正常Quit/shutdown/game0。两次在途退役总约1.40s/0.88s仍如实留日志，未用本次功能PASS宣称所有召援演出等待已做性能优化。

原事件+片/Godot/journal共997数据文件封存`runtime/content-006-r9-20261031-020856`；摘要`runtime-verification-r9.json`、收集收据`collection-r9.json`。复用本地小脚本`C:/Projects/STS2ReplayMod-local/tools/Collect-Content006.ps1`完成固定账号会话/片段归档及已知文件逐hash补存，不另造框架或扫描其他账号。当前51文件4,057,762 bytes备份`CONTENT006-post-r9-current-20261031-020856`；current_run与主progress不变，progress.save.backup和settings.save.backup变化，未归因/恢复或称51项零写入；原始引擎诊断保留。无新DLL/构建或重复纯合同。所测D录制/观看/跨召援恢复闭环通过；009自动分组未测试，不以D通过替代，下一按顶部两次一步片段的极短真实存退/重启收束。

## r10：009未完房间跨进程自动分组/新attempt通过，按样本收束本包

用户“测试完毕。看起来分组对了”。009/session d6edd545995d48e9985ec41420f9d58b有18事件，PID135492/Started02:12:04；cbb11646497441be8eeae60eab1bdf1a有16事件，PID107384/Started02:13:15。两份原始Godot均有Quit confirmed/Steamworks shutdown，game0。第一进程最后成功raw写盘key`7212D12F2B425F85AB1572441A5FB9189AD4B1A7865E09802CEC710881B210A9`，其实际原生DTO读入表示登记为`C1AC1796291B14C0AD935F639B781FE1A86BA06980C047B8ABD97237D11507E6`；两key确实不同，实际触发上轮缺省字段缺口。新进程冻结load、Setup及Continue同C1AC17…，MatchedObservedSave true、PreFinished false、ChangedBeforeSetup false、ReusesOldOperations false，原生写盘/双表示登记的009新Hook路径取得真实证据。

两片原始Battle均同Run `b5af957dbd7446c4b222e48c1da142e3`、第1场：`53eddcf8126b463c98b0c6597fa32631`尝试1/map、`160f329f73de46fda5c9e66633954df6`尝试2/continue-known，都是格式10、1操作/Sequence1的Defend、interrupted/无胜利，原生生产读回ExactPayloadMatched。Initial原字符串完全相同，第二片独立从房间开头录制，没有拼旧步骤。第一片Preparation标明原三鼠准备，第二片null，prepared事件仅一次，重启未重新注入；当前原生存档仍为未完房间，符合本轮停手点。两原片SHA分别`CA09D336F346CBB71B640B7BE2CD9E5831FCACDC3EE5372BC41EC238438E6CCB`、`54D82DC76240F144B67272E3DA008C3AFAE576964C2B17D8E6C9874BC94EF620`，176,039/176,329 bytes。该Run不存在Corrections条目，没有调用显式校正，本次自动匹配与r8人工修历史归属严格分开。

34事件+两片/journal/两进程Godot共39数据文件逐hash封存`runtime/content-006-r10-20261031-021506`；摘要`runtime-verification-r10.json`、复用收集脚本收据`collection-r10.json`。新正常测试局及存盘造成current_run及backup、主progress及backup、原生latest.mcr、settings.backup与r9变化，按正常测试操作留存，不写零写入或自动回滚。当前51文件3,952,518 bytes补存`CONTENT006-post-r10-current-20261031-021506`并读回；猎人r6、三鼠r8及旧失败恢复点均保留。observer-error0，原引擎诊断保留；无新DLL/构建/离线镜像测试，也不为新一步片再重复已过的短片观看矩阵。

实现侧结论DONE_WITH_LIMITATIONS：A持续逐场录制与资源承接、B已完成/未完房间跨进程真实关联和独立attempt、C普通列表早晚/片段/完整战斗独立观看与退出隔离、D一般敌方新增单位的本条三鼠原生录制/稳定目标/生成前后Back与后点恢复/再召/胜利均已有实际样本闭环。仍限固定本体、单人A0首幕普通地图战斗；未知卡/所有组合、精英/Boss/事件战斗、玩家失败、孵化/复活/变形/多阶段、多人/跨版本、任意文件冷恢复/权威纠偏、未访问/跨房间Seek、整局外壳和发布性能不外推。旧FAIL/Unknown留原章节；009保持安装，无需继续本包测试，阶段交PM收束。

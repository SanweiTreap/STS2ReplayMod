# CONTENT-006：持续录制、Continue、战斗列表与召援

2026-10-30。D-134同工程实施中。001/r1已取得六场录制、资源承接与新进程Continue续录证据，但发现一次真实分组错误；当前安装002，包含读取/保存边界修复及D召援候选，均尚待原生验证。新录像列表回看、一般召援恢复未验收，本包未完成。

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

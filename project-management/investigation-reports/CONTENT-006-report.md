# CONTENT-006：持续录制、Continue、战斗列表与召援

2026-10-30。D-134同工程实施中。当前安装只交付A/B/C首个候选；D三鼠召援源码候选已接通但未安装/未实测。不把构建/纯合同称为原生运行通过，本包未完成。

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

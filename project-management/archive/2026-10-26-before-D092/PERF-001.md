历史快照：D-092精简/放宽授权前的项目管理文档；不作为当前执行权限。

# PERF-001 录制性能测量

签发日期：2026-10-26，Asia/Shanghai  
状态：Phase0已验收；Phase1离线候选IN_PROGRESS，安装/配置/游戏运行 HOLD  
负责人：原 Implementation 对话「完成 SPIKE-INV012A 工作包」；thread `01a0e2d5-084e-76d2-ab5c-cee41644b0f7`，保留当前模型设置。

## 目标与当前授权

只回答：怎样把录制自身的成本、测量工具的成本及游戏原有开销区分开，并据此安排一次足够小、可重复的正式游戏性能实验。DET-001已关闭，其真实重洗A/B只能证明一个确定性样本，不是性能基线。

本阶段交付可执行的离线统计/管线小实验和后续运行合同，不构建或安装游戏测试模组，不请求用户启动游戏。不是再做一次泛化调查，也不在这里实现正式 recorder、优化旧探针、实现恢复或确定最终文件架构。遵循 `WORKFLOW.md` 与 `TEST_PROTOCOL.md`；下文为本阶段范围，旧probe的安装/账号读取权限不继承。

先读 CURRENT_STATUS 当前状态、本工作包及 DET 的最终 PM 验收；按需查 MVP 的 G-02 和现成公开 DTO/固定 IL，不重读全部历史、不重跑旧全套验证。源码 `experiments/PERF-001/`，证据 `evidence/perf-001/`。PM管理状态文档由PM维护，Implementation只写本包源码、报告和证据。

## 当前环境说明

用户于2026-10-26报告已在modded侧切到profile1并关闭所有模组。记为用户报告，不声称持久配置、实际loaded set、Steam Cloud、当前游戏进程或版本已现场通过；不为本离线阶段读取这些数据。

唯一个人保护槽仍是不加载模组的profile1；另一账号排除。尤其禁止把「关闭所有模组再开游戏」作为无探针性能基线，该启动可能切入个人存档树。后续需先设计保持同一modded上下文的对照，不以已关闭probe或旧基线恢复来满足条件。

固定调查基准仍为v0.111.0/41cef1ea，`sts2.dll` 9,757,184 bytes / SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`，Harmony 2.4.2.0。允许metadata/窄IL只读核对，不加载正式游戏运行路径；身份不同只报告，不自动换版本或扩查新API。未来游戏运行须重验当时环境，不把10月24日收尾收据当今日配置。

## 对照设计

至少定义下列三个可比模式；允许在同一未来新测试模组中选择模式，不要求三份工程或三种存档树。

1. **关闭记录的共同基线**：保持modded上下文的最小壳和共用测量设施，不启用候选录制热路径。若无法安全获得真正未插桩基线，明确此模式是「共同测量壳基线」，不是零模组/零观察开销；不得接触个人槽补齐它。
2. **空观察对照**：使用候选所需的同一组hook和相同采样设施，但不采集状态、不序列化、不排队写盘。用于估计hook/观察成本；不能仅把磁盘输出改成null就叫空观察。
3. **候选记录路径**：最小语义事件加已定义payload，区分状态采集/冻结、序列化/编码、入队、后台写入和收尾drain。冻结DTO前不得把live对象交给后台；若本阶段仅能测公开DTO的后半段，采集/线程迁移成本明确待正式游戏验证。

三模式使用相同的画面/帧率设置、场景资格、测量边界和共同设施。列出哪些差值可以解释、哪些有交互项不能简单相减；旧DET的逐事件JSON、hash/trace、大量诊断和人工停顿不进入候选成本。已有全状态投影不是已获批准的权威恢复快照，不能以其序列化速度决定STATE-001。

## 指标和样本合同

将未来游戏端测量的采样来源、边界、单位、聚合与不可测项写清，必要符号只作窄IL核对。

- 帧间隔用单调原始时钟和明确的process-frame边界；区分process间隔、CPU用时及真正呈现/GPU时长。没有呈现证据，不把process间隔命名为GPU帧时长或准确的丢帧数。
- 空闲、地图交互、地图到房间加载、出牌/结算、EndTurn分别标窗。加载可单列，但不能事后删掉有问题的帧；保留全窗口汇总和分段汇总、开始/结束边界及异常原因。人工思考/切出/暂停不计入动作延迟；空闲观察窗仍包含其实际全部帧，不只挑快速帧。
- 输出样本数、总观察时长、mean、p50/p95/p99、max及明确阈值以上的计数。写明分位算法；小动作样本不足时只报告原值/范围，不用几个操作的p99包装稳定性，也不把相邻帧当独立重复。异常值保留，污染样本单列，不静默剔除。
- 区分同步同线程分配、进程级GC/分配与后台writer分配。跨await/线程不能用线程局部差值声称整个动作分配；GC次数/时间、CPU或显存无法可靠取得时标Unavailable而非0。
- I/O记录入队数量/字节、实际write/flush次数和语义、写耗时、队列峰值/上限、丢弃/失败/背压、最终drain后的完整性。写入API返回不等于物理落盘，后台写不是零开销，未drain或少记事件的快路径不能获性能PASS。
- 确定预热与正式窗、运行次序/重复策略、最小样本和硬上限。比较同一内容/固定正常流程；不要为复用fixture恢复旧native checkpoint或关掉正常存盘来美化结果。先给小型pilot预算，再决定正式重复数，避免要求用户反复打大量相同对局。

当前不预设发布毫秒阈值或宣布「无感」。可以提出描述性hitch档位及理由，但G-02发布预算须依据正式游戏基线和玩家体验另行批准。

## 本阶段实际离线工作

1. 用少量固定输入实现并测试统计器/记录完整性：分位算法、ticks单位转换、空窗/少样本、长尾不丢、重复/逆序时间、无效数值、队列溢出和未drain不得正常完成。测试覆盖真实可到达的数据路径，不追求数量。
2. 以已验公开 `evidence/det-001/runtime/records-af99c3b9c7bf411882d580c2f3565408/A.point-0.semantic.json` 至 `A.point-4.semantic.json` 的DTO载荷作**离线代理**，准备一个小型 .NET 9 Release 管线对照：空操作、序列化到内存、序列化并经有界队列写入本包目录。可复用纯DTO代码/框架，不依赖旧probe运行DLL；也可用独立解析表示，但须精确披露解析/构造/哈希哪些在计时内。
3. 代理实验预热、固定有界迭代、记checksum/数量/字节保证实际执行且写完后可解析；同一输入前后hash保持，oracle/expected不得与可变candidate共享。分别记主线程与writer/总完成，不只测enqueue。输出运行时、Release/调试附加状态可取得项、输入hash、实际N与观测值；单次命令控制在60秒内，总输出不超过64MiB，不逐次生成成千JSON。
4. 代理结果仅校验测量管线并提供DTO序列化/写入成本线索，不是STS2 FPS、live采集成本、最终录像体积或发布性能。可指出最值得正式测的一项，不能以离线快/慢选择正式架构或顺手优化。

所有输出仅在本包workspace，原公开fixture只读。不读当前/私有账号、设置、profile、存档、Cloud、旧recovery或完整游戏日志；不运行Prepare/Closeout等修改脚本，不重建/安装/运行退役probe，不启动STS2或正式headless，不用Computer Use/GUI自动化。不要求用户停在某画面等待离线工作。

## 后续人工运行草案

提交一份能直接转为同包下一阶段的短流程：保持modded1的共同基线方案、最小新候选增量、独立源/安装身份、版本/loaded/Cloud的现场门、预期写入窗口、每种模式的同场景来源及run order、用户的具体动作和目标后停手/退出。禁止无资格地借用旧seed/坐标或要求只有燃烧之血等不可满足条件。

首轮优先小型idle/map/card pilot，明确是否含自然重洗/房间加载，怎样在不放大操作数的情况下覆盖最敏感窗口。不要设计正式UI、播放器、通用profiler或全场景矩阵。此草案不授权安装、配置修改、请求人工启动、战斗/Continue或任何新的运行。

## 交付与停止

交 `project-management/investigation-reports/PERF-001-Phase0-report.md`、`evidence/perf-001/README.md`、测量合同、最小源码/测试及可复现入口、原始离线结果和小型hash索引；索引只列本包交付及直接公开输入，不递归纳入旧全部索引/build缓存。未变旧文件不重复全套build/144测试/数千hash核对。

报告分开Confirmed、Inferred、Unknown，结论只选 `READY_PERF_MEASUREMENT_CONTRACT` 或 `NEEDS_SCOPE_REVISION`（给最小缺口）。离线统计和管线验证通过、三模式可比较且不触及个人槽、未来pilot边界明确即可交PM，不自行构建游戏候选或请求玩家测试。

Implementation可直接修复本包离线harness、补回归和重跑相关测试，保留已交付版本/失败证据；不需每个缺陷新建INV或新对话。真实环境无法观测、范围/验收含义需改变、原始输入被修改或发现未知副作用时交PM，不追求PASS。正式游戏attempt=0。

## Phase0验收与Phase1离线候选授权

2026-10-26 D-091：Phase0接受为 `ACCEPT_PERF_MEASUREMENT_CONTRACT`。PM只读核对64项/5输入、9000原始样本/6份写出、11原构建源码绑定，独立重算统计并复现349行/5方法IL；未重建、重跑23项或重新计时。原报告/索引/源码封存，验收 `evidence/perf-001/project-lead-phase0-review-2026-10-26.md`。早期104ms与后续38–42ms不认作稳态，也不要求再测到满意。

以下替代原Phase0的「不构建游戏候选」限制，仅授权**离线候选构建**；当前账号/配置/存档/Cloud/日志/游戏进程读取、安装启用、配置修改、请求用户启动和runtime仍全部HOLD。目标为最小idle/map/card pilot准备同一候选的C0/C1/C2，不做全局profiler、正式录制或UI。

### 独立身份和代码权限

- 新工程 `experiments/PERF-001-candidate/`，证据 `evidence/perf-001/candidate-r1/`，DLL/Mod ID `PERF001`、manifest SemVer `0.1.0`，owner `STS2ReplayMod.PERF001.Measurement.candidate-r1`，未来private namespace `user://PERF001/candidate-r1/`。现在只写workspace的synthetic store和sealed候选，不创建真实user数据。
- 同一个DLL支持C0/C1/C2，模式启动前冻结、测量中不可变。manifest保持 `affects_gameplay:true`，固定IL窄核对加载/存档树选择契约；未来仍须实际modded1门，不能只靠manifest字面或mode=C0推定隔离。必要依赖优先只用已有游戏/Godot/Harmony；不新增RitsuLib等无必要依赖。
- 可复制必要纯统计/DTO/writer或已验observer生命周期模块到独立工程，保留来源，不修改/引用退役运行DLL或旧owner，不复制bin/obj/成千旧证据。Phase0公开源码/原始代理不改；后续修复在新候选lineage并保留版本/回归。

### 候选必须满足的测量契约

1. 先窄查并实现精确CommonBoundary/Candidate patch表，列declaring type、签名/token、回调和用途。C0仅common，C1/C2候选集合完全相同；C1空处理器不得仍做C2状态采集/序列化。仅使用metadata与同形Harmony dummy证明可证部分，不在无引擎环境调用production构造/方法制造崩溃。
2. 共用persistent process-frame来源、首帧确认、monotonic raw stamps/sequence、预分配有界数组及分窗，跨场景不丢唯一采样源。可采用已验SceneTree.ProcessFrame生命周期或有充分理由的等价实现，但统一定义采样入口/顺序/自身成本。跨窗间隔保留且归属规则唯一，不双计或删慢帧。
3. MapCommit、合法CardAccepted/CardResolved、EndTurnAccepted及NextPlayerTurnReady分别绑定正常production调用与原Task成功/idle。相应时钟边界不能只是返回Task对象或API请求；固定20字段readiness可作为定位参考，不能每帧做整套DTO/hash来代替轻量共同门。内部同tick边界允许序号有序，frame stamp的重复/逆序依合同判无效；不能照搬严格毫秒递增误拒合法同步事件。
4. 共同资格sidecar在正式窗外或明确的共同校验段记录完整场景信息；三模式同Custom/新seed/A0/无modifier、相同Neow/遗物/卡组/手牌/敌人与关键动态属性才能比较。loading/action结束先取原始结束stamp，再做昂贵比较，不能把投影时间冒充游戏effect时长；共同校验段仍留全窗帧并单列。今日Custom解锁、seed回显和内容一致性明确留runtime门，不读今日玩家状态猜资格。
5. C2只在约定事件同步复制自有标量/字符串/数组，后台不持live对象。payload依sealed测量合同，实际API不足交PM，不擅自扩成全状态快照；记录采集/冻结、编码、入队及writer指标而非只计enqueue。小pilot只有少量事件，不注入重复伪事件或故意放大状态制造I/O；不能由缓冲API返回宣称持续磁盘负载已覆盖。
6. writer保持记录/字节/单条上限、严格顺序和粘性失败；实际预期事件清单与其完整payload可在窗关闭后独立解析复核。所有成功/失败路径均尝试有界stop/drain并明确线程是否退出；观察失败、writer错/超时/溢出、数组满不能交成功结果或在后台继续无限写。
7. observer不跳过/改原方法，不替换或包装production Task、结果、异常，不改RNG/HP/牌堆、保存开关、RunManager/SaveManager或游戏逻辑，不自动出牌/EndTurn/地图/Continue。异常只终止本包观察/后续人工请求，不能吞production fault或阻止在途动作。正常游戏存盘仍是受测共同环境，不以关闭它消噪声。
8. 共用状态提示仅有限必要marker，不逐帧/逐字段JSON、hash或trace；提示和校验自身成本有明确tag且三模式一致。人工Ready等待与真实动作/采样deadline分开，状态页面晚打开仍显示真实terminal，不重复旧的Ready超时或WAITING_EVENTS误报。指标与完整性校验尽量窗外执行，实际写入时间也如实归属。

### 本阶段离线验证和交付

用同一typed正常路径贯通配置/schema→published loaded门的模拟输入→新Custom场景→10秒warmup/30秒map idle→map/load→Defend→EndTurn→drain/完整commit。按实际模拟时钟推进，不真的等待40秒；三模式的patch计划、资格/边界/观测原始序列同源。覆盖无首帧、错误profile/mode/身份、旧Continue/seed/内容不符、跨场景/边界race、effect fault/cancel/未返回、失焦/污染、数组/队列溢出、少事件、write失败/未drain、终态后不追加及owner收尾。不要用测试数或空布尔替代实际管线可达性。

执行Release build、相关合同回归、精确metadata patch/unpatch及真实Harmony同形dummy非干扰/原Task身份测试；不能安全离线执行的实际Godot/production回调标Unknown。无需重复原未变23项/旧144或再跑任何计时benchmark。已定位harness缺陷可在新candidate内自行窄修，保留交付版本，不因每项缺陷新开INV；生产副作用、验收语义改变或无法界定时交PM。

交 `PERF-001-candidate-report.md`、独立sealed DLL/manifest及必要源码/证据小型索引、窄IL与patch表、三模式差异审计、状态/写入边界及更新的人工pilot**草案**。可沿用3 fresh进程/3个同Custom新局、C1→C0→C2、每模式一Defend＋一EndTurn/不胜负领奖的初始pilot，不新增自然重洗/发布重复矩阵。固定120fps仅是旧草案建议，不在离线阶段改设置或未经说明把改变渲染负载当对照；最终现场设置三模式一致并记录。

结论为 `READY_OFFLINE_PERF_CANDIDATE` 或 `NEEDS_SCOPE_REVISION`。成功也只交PM验收，不安装、配置、请求用户游戏或认定G-02。Phase1正式runtime attempt=0；下一门才是PM据候选证据签发有限配置/安装/人工pilot，同包推进不另造INV。

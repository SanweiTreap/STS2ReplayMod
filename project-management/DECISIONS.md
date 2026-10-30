# 决策日志

## D-001：早期不承诺最终架构

日期：2026-08-21  
状态：Accepted

决策：在完成现有实现理解、产品边界和关键技术实验之前，不决定具体最终代码架构。

理由：目前已知存在性能、确定性和版本兼容风险，证据不足以支撑架构选择。

## D-002：RunReplays 作为参考实现而非默认基底

日期：2026-08-21  
状态：Accepted

决策：借鉴 RunReplays 的 hook、command 与游戏交互方式，但每个部件必须经调查后才能归类为直接复用、借鉴后重做或不采用；不默认继承整体架构。

## D-003：当前主线限于单人 Replay

日期：2026-08-21  
状态：Accepted

决策：当前阶段围绕单人 replay 建立证据与 MVP。多人保留为未来研究方向，不让其需求扩大当前 milestone。

## D-004：项目文档是跨会话事实来源

日期：2026-08-21  
状态：Accepted

决策：跨会话需要保留的状态、调查和决策必须写入 `project-management/`；聊天记录不作为唯一依据。

## D-005：当前调查与 MVP 规划以 public-beta v0.111 为目标版本

日期：2026-08-21  
状态：Accepted

决策：近期技术调查、兼容修复优先级和 MVP 规划以 public-beta v0.111 为目标。v0.107 保留为 RunReplays 已成功运行的历史基线和对照版本；当前不承诺同时发布或维护两个版本。

理由：项目发起人及其目标玩家群当前主要使用 v0.111；官方 v0.111.0 公告说明短期内不会按原节奏立即发布下一 patch，提供了数周调查窗口。v0.111 已知存在 RunReplays compatibility break，因此若继续围绕 v0.107 实现，会降低对实际玩家的价值。

复审条件：官方提前发布破坏性更新、v0.111 出现阻止 MVP 的技术障碍，或目标用户版本分布出现相反证据。

## D-006：不为恢复 v0.107 历史测试的精确按钮单独重测

日期：2026-08-24  
状态：Accepted

决策：不为确认历史成功测试点击的是 `Replay to floor …` 还是 `Replay Floor` 而专门切换回 v0.107。将精确按钮记录为未知，继续依靠源码确认操作语义、依靠历史日志确认自动 replay 确实执行成功。

理由：该区别只影响旧测试步骤的完整还原，不影响语义重放路线是否可行的结论；切换版本有约 300 MB 下载和环境变更成本。当前更有价值的是理解模块、定位 v0.111 break，并最终验证本项目自己的实现。

复审条件：后续兼容性调查无法从源码/日志区分两条启动路径，或实现实验需要一个可重复的 v0.107 对照流程。

## D-007：先定义 MVP，RunReplays 调查改为按需进行

日期：2026-08-24  
状态：Accepted

决策：暂停 D4–I6 的历史行为追问，直接进入第五轮 J/K/L，先形成单人 MVP 玩家闭环。RunReplays 不作为计划 fork 并继续开发的代码基底，也不再要求先完成全量模块调查；后续仅围绕已选 MVP 能力查阅相关源码、提取候选 hook/反例，或作为本项目最小实验的对照。

理由：现有证据已经证明语义操作重放路线基本可行，并暴露性能和版本兼容风险。继续恢复旧测试细节不会显著改变产品范围或下一轮实验，而尚未定义 MVP 会导致调查缺乏明确停止条件。

保留意见：完全忽略 RunReplays 会浪费已存在的 hook、command、RNG、UI 和性能失败经验。因此采用“产品先行、问题驱动查阅”，而不是“停止参考”或“全面深挖”。

复审条件：MVP 定义中出现无法判断可行性的核心能力，或自有技术探针遇到 RunReplays 已可能解决/解释的问题。

## D-008：受支持的单人对局默认自动录制

日期：2026-08-24  
状态：Accepted

决策：安装并启用 Replay Mod 后，受支持的单人对局默认自动开始录制，不要求玩家在每局开始前手动开启。提供全局设置以关闭自动录制。

理由：玩家通常在对局进行中或结束后才意识到内容值得回看；逐局手动开启会产生无法补救的漏录，并破坏面向普通玩家的使用闭环。

约束：自动录制使空闲和热路径性能开销成为 MVP 核心验收项。当前尚未决定首版覆盖范围、存储保留策略、录制状态提示和局内开关语义。

复审条件：技术实验表明自动录制无法在可接受性能预算内实现，或自动录制对存档/隐私产生无法缓解的副作用。

## D-009：性能优先是发布门槛，完整单人覆盖仍是目标

日期：2026-08-24  
状态：Accepted

决策：当极低录制开销与首版全部单人场景覆盖暂时无法兼得时，优先保证默认自动录制不明显影响正常游玩。同时继续以尽可能覆盖全部单人场景为产品目标，首版临时排除项必须明确标注、进入覆盖清单并继续补齐。

理由：后台自动录制若造成卡顿，会破坏每一局正常游戏；但长期只支持少量场景也无法成为真正面向玩家的 Replay Mode。性能优先决定发布门槛，不等于降低最终覆盖目标。

约束：需要建立量化性能预算和单人场景覆盖矩阵。未支持/未验证场景不能被静默算作成功录制。

复审条件：性能实验表明完整覆盖并不会造成冲突，或某类场景在技术上无法支持且需要重新定义产品范围。

## D-010：正常录制静默，仅对例外状态提示

日期：2026-08-24  
状态：Accepted

决策：正常自动录制期间不常驻显示录制图标或实时文件大小。仅在录制被关闭、无法启动、中途失败或进入明确不支持场景时，以玩家可理解的方式提示。

理由：默认录制应尽量不干扰正常游戏；持续状态和文件大小不是玩家在成功路径中必须关注的信息。但静默失败会让玩家误以为对局已被可靠保存，因此异常必须可见。

约束：异常检测与通知属于 MVP 闭环；提示形式、频率和失败记录在 replay 列表中的展示仍待设计。

复审条件：可用性测试表明玩家普遍无法判断录制设置是否生效，或平台规范要求持续显示录制状态。

## D-011：管理页首版支持删除与打开目录，自定义命名延期

日期：2026-08-24  
状态：Accepted

决策：MVP 的 replay 管理页面提供删除 replay 和打开文件所在目录。自定义命名/重命名延期；首版利用角色、时间、seed、进度或结局等元数据自动生成可识别名称。

理由：删除是自动录制产生文件后的基本管理能力，打开目录支持诊断和手动分享。自动元数据足以满足首版识别需求，自定义命名不是最小观看闭环的必要条件。

约束：删除操作需要防误触设计；自动命名格式、重复名称处理、文件大小展示和是否采用可恢复删除尚未决定。

复审条件：可用性测试表明自动元数据无法让玩家可靠找到目标 replay，或分享流程要求稳定的玩家自定义名称。

## D-012：MVP 基本观看控制与导航方向

日期：2026-08-24  
状态：Accepted

决策：暂停/继续、按已记录玩家操作 Step、速度调整、停止并安全退出均属于 MVP。前进与后退是重要产品能力，不得在未讨论最低粒度和技术实验前整体延期。

补充要求：不同操作类别的播放停留时间应可通过 config 调整，例如出牌间隔和选卡界面停留时间。Counter-Strike demo 系统作为时间轴、变速和双向导航的产品参考。

理由：Replay Mode 的核心价值不仅是自动演示，还包括定位和复盘具体决策；不同场景使用统一等待时间无法同时满足观看效率与可读性。

约束：前进/后退的精确粒度、是否包含可拖动时间轴、真实操作时间是否记录，以及分类间隔与全局倍率的组合尚未决策。Counter-Strike 的 tick/状态流实现不可直接当作 STS2 架构事实。

复审条件：NAV-001 证明某一粒度在性能或确定性上不可接受，届时调整粒度而非默认取消全部导航能力。

## D-013：采用双层导航产品模型

日期：2026-08-24  
状态：Accepted

决策：MVP 导航目标包含三个层级：上一/下一玩家操作、上一/下一回合/房间/楼层等结构节点、点击或拖动时间轴定位。界面按两个尺度组织：房间内使用操作时间轴，全局使用类似成绩结算页面的实际路线图；点击路线节点进入相应房间/楼层，再用时间轴精确定位。

理由：长对局中的“找哪一间房”和房间内的“找哪一个决策”是不同尺度的问题。双层结构比一条覆盖整局的超长时间轴更容易浏览，也与 STS2 现有路线/结算视觉语言一致。

视觉参考：`evidence/product-references/run-history-route-reference-2026-08-24.png`，SHA-256 `E2031A5BA4CB9CF9717B409252732D77C6D9C2079CBE6C1E6CD0594F372ED834`。该截图不锁定最终 UI。

约束：路线节点需要哪些元数据、房间内时间轴的标记密度、状态恢复机制和定位延迟仍待设计/实验。

复审条件：可用性原型显示双层导航比统一时间轴更难理解，或技术实验无法提供可靠的跨房间定位。

## D-014：MVP 记录操作时间戳并提供两种播放节奏

日期：2026-08-24  
状态：Accepted

决策：录制文件保存语义操作的相对时间戳。MVP 默认使用按操作类别配置的压缩节奏，同时提供“原始节奏”模式，按记录间隔调度语义操作；全局速度倍率可作用于当前模式。

理由：时间戳保留原始操作节奏的选择空间，数据需求较小；分类节奏更适合默认观看，可避免玩家长时间思考使 replay 过分拖沓。

技术假设：只记录操作时间戳可能足以复现玩家操作 cadence，但尚未验证。它不能自动精确复原动画、渲染、加载或异步 UI 时序；回放仍必须等待游戏处于可执行下一语义操作的状态。

约束：使用单调时钟还是其他时间源、暂停/失焦/加载是否计时、时间戳粒度、异常长间隔处理及 UI readiness 协调尚待 TIME-001 验证。

复审条件：TIME-001 证明单一操作时间戳不足以达到产品预期，届时扩展时序事件模型，不默认放弃原始节奏模式。

## D-015：原始节奏只记录活跃可交互时间

日期：2026-08-24  
状态：Accepted

决策：原始节奏保留游戏窗口聚焦、游戏未暂停、非加载且玩家可正常交互期间的真实思考时间；暂停菜单、窗口失焦、加载画面和可识别的模组/系统阻塞不计入操作间隔。

理由：原始节奏要表达玩家决策节奏，而不是电脑加载、切出游戏或技术卡顿。玩家在可操作界面停留较久则属于真实决策过程，应予保留。

技术假设：这些状态可被可靠且低开销地识别，尚未验证。若“系统阻塞”和玩家正常思考无法区分，应优先记录可观测状态并避免凭耗时猜测。

复审条件：TIME-001 发现某类排除状态无法稳定检测，或用户测试认为排除规则造成原始节奏不自然。

## D-016：接管游戏不属于 MVP，拒绝不可逆转换

日期：2026-08-25  
状态：Accepted

决策：MVP 不支持从 replay 当前状态接管并继续游戏。停止观看不得把 replay 不可逆地转换为普通对局；原 replay 必须保持独立、可重新打开和导航。

理由：接管的分支、存档、成就、统计、继续录制和返回 replay 语义均值得单独商榷，不是最小观看闭环的必要能力。项目发起人认为 RunReplays 接管后无法返回 replay、回放直接变为普通对局的体验不好，本项目明确不采用该行为。

未来约束：若重新考虑接管，应优先研究独立练习分支或沙盒会话，并在实现前明确返回 replay、数据隔离和副作用。当前不承诺具体方案。

复审条件：MVP 完成后，用户价值证据表明接管是下一阶段高优先级，并且产品语义与安全边界已单独评审通过。

## D-017：采用强记录与自动纠偏方向

日期：2026-08-25  
状态：Accepted

决策：Replay 除语义操作外，还应记录足够的权威状态用于校验与恢复。检测到可恢复 divergence 时，播放器自动用记录状态纠偏、继续播放并输出结构化诊断日志，不默认暂停等待玩家选择。

理由：偏差通常意味着实现缺陷或版本/内容变化。项目应优先通过更充分的记录避免偏差演变为播放终止，并让玩家尽量完成观看；日志用于开发者定位根因。

约束：具体采用完整快照、增量、状态指纹和何种密度尚未决定，必须与性能、文件大小、写盘和导航实验共同验证。强记录提高可恢复性，但不能保证缺失类型/资源、破坏性 schema 变化或损坏文件仍可恢复，也不自动构成跨版本支持承诺。

诊断要求：日志至少关联 replay/版本、结构节点、语义操作、expected/actual 摘要、恢复来源、恢复结果和后续再校验结果；正常成功路径不得沿用 RunReplays 式同步逐事件重日志而破坏性能。

复审条件：STATE-001 证明所需记录开销无法满足性能门槛，或状态恢复会引入比停止更严重的不可信行为；届时调整记录层级和支持边界，不静默降低可靠性承诺。

## D-018：不可恢复状态停止并明确报错

日期：2026-08-25  
状态：Accepted

决策：若权威记录状态本身无法读取、装载或恢复，播放器停止在最后可信位置并显示明确错误，同时保留诊断日志；不继续播放已知无法校正的流程。

理由：自动纠偏适用于记录状态仍可信且可用的偏差。缺失类型/资源、破坏性 schema 变化或文件损坏时，强行继续只会制造不可信画面。

复审条件：未来跨版本层能够把当前不可恢复类别转换为经过验证的可恢复类别。

## D-019：跨版本播放是 MVP 后独立大型目标

日期：2026-08-25  
状态：Accepted

决策：在不同游戏版本上播放同一个 replay 不属于 MVP，也不得被安排为“顺手完成”的小兼容任务；它是单版本 MVP 完成后的独立大型目标。当前 MVP 目标为 public-beta `v0.111.0` 的同版本录制与播放。

当前义务：从首版开始记录精确游戏构建、Replay 格式版本、启用模组/版本和可取得的内容标识；版本不匹配时明确诊断。这是未来兼容的必要基础，不代表当前承诺迁移或播放。

理由：卡牌/规则变化、类型或资源删除、API/schema 变化和状态迁移都会影响语义，不可用“能载入文件”替代兼容证明。强记录有助于恢复，但也可能增加 schema 耦合。

复审条件：MVP 完成并有稳定 fixtures 后，单独批准跨版本 milestone、目标版本矩阵和成功标准。

## D-020：MVP 不正式支持其他 Gameplay Mod

日期：2026-08-25  
状态：Accepted

决策：MVP 的正式兼容保证限于原版游戏内容、Replay Mod 及必要依赖。其他 gameplay mod 的正式兼容延期；纯 UI/外观模组只做尽力兼容，不作首版保证。

当前义务：Replay 记录全部启用模组及版本，以及能取得的稳定 ID、artifact/hash 和 gameplay 影响标识。录制与播放的 gameplay mod 集合/版本不一致时必须明确警告或拒绝，不静默假定兼容。

理由：Gameplay mod 可改变内容、RNG、状态结构、操作入口和 hook，支持成本接近小型兼容平台。先稳定原版内容闭环，后续才能定义可扩展的兼容协议。

复审条件：MVP 验证完成，或某个必要依赖实际引入 gameplay 状态而必须进入正式测试矩阵。

## D-021：同环境跨机器分享与安全导入属于 MVP

日期：2026-08-25  
状态：Accepted

决策：MVP 正式支持把 replay 文件复制到另一台机器，并从 Replay 管理页导入；播放保证限于精确游戏构建、Replay 格式/模组版本及 gameplay mod 环境匹配的情况。环境不匹配时提供明确诊断，不强行播放。

当前义务：把外部 replay 视为不可信数据，验证格式、schema、完整性、大小和环境元数据；禁止执行嵌入代码、任意路径读写、路径穿越和静默覆盖。文件格式还应避免默认泄露本机绝对路径、Windows 用户名等非必要身份信息。

理由：分享 replay 是观看型产品闭环的自然组成部分，而同环境约束能将首版范围与跨版本、Gameplay Mod 兼容清楚分离。导入同时扩大了攻击面，因此安全与隐私不是后续补丁，而是该能力的验收条件。

复审条件：需要支持在线分发、自动下载依赖、跨版本播放或不匹配 gameplay mod 环境时，必须另立产品与安全设计。

## D-022：MVP 后优先推进多人 Replay

日期：2026-08-25  
状态：Accepted

决策：MVP 后大型方向按以下产品优先级排序：`多人 Replay → 跨版本播放 → 隔离的可游玩 Replay 分支 → 其他 gameplay mod 正式兼容`。多人 Replay 是其中最重要的方向。

边界：该排序不把多人纳入当前单人 MVP，也不授权在 MVP 阶段提前扩大实现范围。当前 Replay 数据契约和实验应避免无理由封死未来多人扩展，但不得以未经验证的多人假设增加当前热路径开销或架构复杂度。

理由：项目发起人明确将多人回放视为 MVP 后最重要的玩家能力。跨版本仍具有长期保存价值，但排在多人之后；可游玩分支必须采用隔离且可返回 Replay 的产品语义；Gameplay Mod 兼容平台排在最后。

复审条件：单人 MVP 完成后，多人技术调查证明其前置条件尚不具备，或游戏多人机制发生重大变化；任何调整都应保留多人作为首要产品目标与实际排期可行性之间的区别。

## D-023：单人 MVP 产品基线 v1 完成

日期：2026-08-26  
状态：Accepted

决策：以 `MVP.md` Baseline v1 作为当前单人 MVP 的产品范围基线。它包含完整玩家闭环、G-01 至 G-11 发布验收 Gate、明确非目标及由产品要求导出的技术问题。`PROD-001` 因而完成。

边界：本决定确认“需要达到什么结果”，不确认具体架构、检查点方案、Replay 文件格式或性能数值。所有未验证技术判断继续作为假设，并按 `EXPERIMENT_PLAN.md` 逐波验证。

理由：J/K/L 产品输入已经全部完成，继续把范围保持为草案会让技术调查缺少稳定停止条件；但在实验前锁定实现方案同样没有证据。

复审条件：关键实验表明某项 Gate 在合理成本内不可行，或项目发起人明确改变产品目标。任何缩减必须更新 MVP、决策日志和玩家可见限制，不能由实现任务静默决定。

## D-024：INV-001 通过，项目进入阶段 1

日期：2026-08-27  
状态：Accepted

决策：接受 `investigation-reports/INV-001-report.md`。阶段 0 的产品基线、参考身份、历史边界、定向模块清单和证据追溯条件均已满足，项目进入阶段 1“关键技术假设验证”。

同时将原 `SPIKE-001` 拆为硬依赖的两段：先以 `SPIKE-001A` 验证原生 `SerializableRun` 的输入就绪点恢复基线；只有基线一致，才执行 `SPIKE-001B` 的一张卡加 End Turn capture/dispatch。

理由：INV-001 已确认 v0.111 候选符号和版本断裂，但没有运行游戏。恢复边界是最早、最可能使后续语义重放失去可信起点的未知；先隔离它能避免在 fixture 本身不一致时误诊 capture、readiness 或 dispatch。

边界：接受报告不等于接受候选 hook 的运行语义，也不等于选定最终存档/快照架构。001A 失败时 Implementation Codex 只报告首个阻塞，不得自行扩大为自制权威状态系统。

复审条件：001A 运行证据表明原生恢复边界与报告假设不同，届时由 Project Lead 重排 Wave 1，而不是静默跳过恢复验证。

## D-025：当前原生恢复链不能作为首回合检查点基线

日期：2026-08-27  
状态：Accepted

决策：接受 SPIKE-001A 的负面结果。测试过的 `FromSerializable → SetUpSavedSingleplayer → LoadRun` 链不能作为本项目当前的安全、等价首回合检查点恢复方案；`SPIKE-001B` 保持阻塞。

理由：跨重启恢复虽到达可操作战斗，但手牌/牌堆和两组 RNG 状态与 fixture 不一致；同时 setup 改写了普通 `current_run.save`，且 `ShouldSave=false` 未阻止写入。继续叠加 capture/dispatch 会混淆恢复、确定性和存档隔离三个问题。

边界：本决定只否定已测试的边界与调用顺序，不否定原生 `SerializableRun` 用作房间前/游戏原生保存边界，也不预先决定采用自制快照。下一步先执行只读 INV-002，追踪正常 load/continue 与安全隔离候选。

复审条件：INV-002 找到有方法体证据支持的不同调用顺序、官方卸载/隔离入口，或房间前检查点语义；随后必须用新的受限实验验证，不能仅凭静态分析解锁 001B。

## D-026：接受 INV-002，并只验证房间前 checkpoint 候选

日期：2026-08-28  
状态：Accepted

决策：接受 `investigation-reports/INV-002-report.md`。中途 `ToSave(CurrentRoom)` 不再作为可恢复战斗快照候选；不再尝试用外部 `ShouldSave=false` 阻止 `SetUpSavedSingleplayer` 的 reload-count 写盘，也不尝试在活动战斗中直接拼接 `CleanUp → setup`。下一步只执行 `SPIKE-001A2`：在严格隔离的 disposable profile 中验证房间前 `PreFinishedRoom=null` checkpoint 经正常 Continue 后的首个输入就绪状态是否与不重启 baseline 等价。

理由：方法体证据确认 `CombatRoom.ToSerializable()` 不包含手牌、牌堆、回合或完整战斗状态；load 会 fresh reconstruct combat，并再次消费建怪和洗牌 RNG。官方在创建新房间前保存 `PreFinishedRoom=null`，因此把 RNG 保存于首次 room setup 之前是当前最小、可证伪且有生产语义支持的候选。

安全边界：静态证据只证明 save path 按 profile id 分段，不证明生产环境端到端隔离。001A2 在启动运行步骤前必须确认 ordinary/disposable profile id 与绝对路径不同，并记录 ordinary save 的 hash、长度和 mtime；任一身份不明或 ordinary 文件变化立即停止。disposable profile 自己的 reload-count 写入是已知行为，不得称为“无写盘”。

复审条件：001A2 同时通过隔离检查和 A/B 状态等价，Project Lead 才考虑解锁 SPIKE-001B；任一失败都返回恢复/隔离问题，不叠加 capture/dispatch 或快速重演实现。

## D-027：MVP 播放必须是主菜单独立会话，并先调查 RitsuLib

日期：2026-09-04  
状态：Accepted

决策：Replay 管理与观看从主菜单专门入口进入，产品层级与“单人游戏”“多人游戏”并列。玩家无需创建/切换 Replay 专用 profile，也无需覆盖、放弃或替换当前单人 `Continue` 进度。打开 Replay、停止、失败及重新启动游戏后，既有普通对局必须保持原状。

技术影响：`SPIKE-001A2` 的 disposable-profile + normal Continue 仍可作为“房间前 checkpoint 是否能确定性重建”的受控对照，但不再是最终播放会话候选，也不能单独解锁 action replay。先执行只读 `INV-003`，调查 v0.111 的主菜单 scene 入口、Replay/RunManager 生命周期、非进度运行上下文、save/stat/achievement 副作用和 teardown；再据此改写下一运行实验。

RitsuLib 处理：把 RitsuLib 作为候选共享依赖而非预设依赖。优先调查其生命周期、Harmony patch、设置 UI、诊断、运行时 UI、兼容门控和临时数据能力；不得因为存在 `SaveScope.InMemory` 或 run-sidecar API，就推断它能隔离游戏自己的 `RunManager` 或 `current_run.save`。每个拟采用能力都必须固定版本/commit、许可证、v0.111 兼容性、调用语义、依赖成本及无库替代方案。

理由：独立观看是 Replay Mode 与普通 save/continue 功能的产品边界。若先围绕 profile 切换或普通 Continue 建立播放器，后续很可能需要推翻会话生命周期和存储设计。RitsuLib 可能显著减少通用 UI/生命周期/诊断工作，但不能在未核实前被当作核心确定性或状态隔离方案。

复审条件：INV-003 证明 v0.111/RitsuLib 没有足以安全建立独立会话的入口，或最小运行实验发现不可隔离的普通进度副作用；届时重新评估运行上下文实现，不降低 G-11 产品要求。

## D-028：接受 INV-003，先验证事务式 ReplaySaveContext

日期：2026-09-04  
状态：Accepted

决策：接受 `investigation-reports/INV-003-report.md`。下一运行实验固定为 `SPIKE-INV003A`，只验证“完整 in-memory `SaveManager` transaction + `RunManager.ShouldSave=false` + finally-owned ordered teardown”能否建立不影响普通 Continue 的 Replay session。RitsuLib 不加入该实验，也暂不成为 MVP 必要依赖。

理由：`SetUpReplay` 绕过 reload-count 写入并使用 no-op replay network service，但它是 debug-oriented primitive，默认 `shouldSave=true`；战斗胜利路径又会直接调用 `UpdateProgressAfterCombatWon`/`SaveProgressFile`，不受 `ShouldSave=false` 保护。游戏没有 per-run SaveManager 注入点，而全局测试 override 是当前唯一能接住 direct store writes 的明确构件。组合候选仍有全局覆盖、异步任务、shared mutable state、平台 side channel 和 teardown 泄漏风险，因此只批准 fail-closed 实验，不采纳为架构。

安全修订（历史假设，后由 D-030/D-032 修正）：首轮固定使用项目发起人已完成全内容解锁的模组存档 1 号。D-028 当时假定其中应建立或保留“真实普通 Continue”；实际执行中用户澄清该 Continue 只是测试用隔离哨兵，真实普通对局位于其他槽且不得读取。播放开始后全程保持同一 profile；实验前备份并固定 Continue 哨兵、全解锁进度及所有其他受保护文件/目录身份，Steam Cloud 关闭；任何原 store/cloud/提交 counter、普通文件或全解锁进度变化立即停止。

RitsuLib 结论：固定 v0.5.18 / commit `f224961a9392e010335da092240b90ee8235317f` 作为本轮证据。其 lifecycle、patch diagnostics、settings、UI、localization、compatibility gates 保留为外围 Adopt candidate；主菜单 peer injection、核心 save transaction 与 replay state 隔离使用直接游戏 API/Harmony。核心隔离通过后再单独决定是否值得增加必要依赖。

后续关系：SPIKE-001A2 保留并修订为独立 reconstruction control，不并入 SPIKE-INV003A；只有 session isolation 与 reconstruction 两项分别通过，Project Lead 才复审 SPIKE-001B。

复审条件：SPIKE-INV003A 发现任何不可观察或不可拦截的普通进度副作用、不能可靠恢复全局 manager、cleanup 后残留 state/scene/synchronizer，或异常/重启路径破坏 Continue；发生后停止该候选，不叠加 capture/dispatcher。

## D-029：固定模组存档 1 号，并由项目发起人手动执行游戏测试

日期：2026-09-04  
状态：Accepted

决策：所有当前运行实验固定使用模组存档 1 号。该 profile 已由项目发起人新建并完成全内容解锁，可作为角色、卡牌、遗物、遭遇等测试环境；任务仍须保护其 save/progress 身份，不能把“测试存档”理解为允许静默破坏。

执行协议：Implementation Codex 禁止使用 Computer Use 或其他 GUI 自动化启动、点击或控制 STS2。需要运行证据时，先提交一轮具体测试需求，包含动作、目标、预期结果、停止条件、禁止动作和需回传证据；项目发起人负责打开游戏并手动执行。多 phase 实验逐轮解锁，前一轮证据未确认前不得推进下一轮。

理由：人工游戏操作比当前 Computer Use 流程更快，也更便于项目发起人观察异常；固定全解锁测试 profile 能减少重复准备场景的成本。自动化限制不适用于命令行构建、静态调查、probe 准备或日志分析。

复审条件：未来出现稳定、快速、可审计且项目发起人明确愿意采用的游戏测试 harness；在此之前不得由 Implementation Codex 自行恢复 GUI 自动化。

## D-030：接受 SPIKE-INV003A 的阶段证据，但不通过 session isolation

日期：2026-09-08  
状态：Accepted

决策：接受 `investigation-reports/SPIKE-INV003A-report.md` 为按 handoff 完成、实验结果 FAIL。接受 Phase A management-only 与 Phase B watch/normal Stop 的局部正面证据；Phase C 未命中 direct-write victory sentinel，因此不把 transaction 候选升级为已通过，也不把这次 FAIL 解释为已发现存档隔离泄漏。

理由：Phase B 中 in-memory manager 安装、native replay setup、正常 Stop、ordered teardown、原 manager 恢复和主菜单重建均有机器证据，原 store/write/delete、外部 submit 和 12 个受保护文件没有变化。Phase C 的 11 个 native events 在战斗胜利前耗尽；`UpdateProgressAfterCombatWon`/`SaveProgressFile` 没有被调用，所以本轮没有实际测试最关键的 direct-write 路径。operation/teardown/audit 没有报错，不能据此否定 transaction。

范围限制：测试槽 profile1 的 Continue 只是 active-profile 隔离哨兵，不是用户其他槽中的真实普通对局。结果只覆盖固定 v0.111.0 构建、固定 fixture、正常 Stop 路径和本轮可观察边界；setup/post-launch fault、restart、victory direct write、全部平台 side channel 仍未知。SPIKE-001A2 与 SPIKE-001B 不解锁。

下一决策：先执行只读 `INV-004`，调查 recorder/fixture 完整性与 official debug dispatcher 语义，并将 direct-write isolation sentinel 与 replay-to-victory correctness 解耦。INV-004 只推荐一个下一运行实验；在其完成前不重试 Phase C，也不继续 D/E/F。

复审条件：INV-004 找到有方法体证据、可审计且不依赖盲目 fixture 重试的哨兵；随后由 Project Lead 另发新工作包，补齐 direct-write、fault 和 restart matrix。

## D-031：接受 INV-004，并将 direct-write isolation 与 replay fidelity 分开验证

日期：2026-09-08  
状态：Accepted

决策：接受 `investigation-reports/INV-004-report.md`。下一运行实验固定为 `SPIKE-INV004A`：复用已验证的 transaction/setup/ordered teardown，但不 dispatch replay events；在合法 replay combat state 中直接调用当前 isolated `SaveManager.Instance.UpdateProgressAfterCombatWon(player, room)` 与 `SaveProgressFile()`，只验证 mutation/write 是否路由至 memory manager/store。

理由：固定 fixture 的 11 个 events 已确认包含第三回合致死 action，checksum 17 显示 action id 10 执行后敌方从 full state 消失；因此不能再把 Phase C 简化为“fixture 只有两个回合”。同时 native 格式没有通用 victory/complete 标志，official debug caller 与 Phase C dispatcher 的 event loop 又逐项一致。继续依赖 replay 自动打到胜利会把 replay readiness/cancellation 与 save isolation 混为一个失败结果。

证据边界：readiness/action cancellation 是当前首要可证伪假设，不是已确认根因；Phase C 没有逐 event actual state、cancel outcome 或 checksum comparison。SPIKE-INV004A 的直接方法调用也不是自然 `EndCombatInternal`、achievement check 或完整 victory pipeline；即使通过，只能证明固定 isolated session 中这两个 production SaveManager 方法的路由和写入隔离。

后续关系：SPIKE-INV004A 通过后，另行批准 fault/restart matrix；两者通过后才复审 session isolation。Replay fidelity 作为独立技术问题保留，不得再承担 save-isolation sentinel。SPIKE-001A2 与 SPIKE-001B 继续阻塞。

复审条件：SPIKE-INV004A 出现 original/cloud/external boundary、错误 write path/payload、共享引用、teardown 残留或受保护文件变化，则收窄或否决 transaction 候选；完整 PASS 时也不得越界宣称自然胜利或完整 session isolation 已通过。

## D-032：修正测试 prompt 的 profile 语义、现场澄清与目标后动作

日期：2026-09-10  
状态：Accepted

决策：所有运行测试 prompt 将模组存档 1 号定义为专用测试槽，其 Continue 仅为 active-profile 隔离哨兵；继续保护该槽的全内容解锁、文件 identity、目录结构和实验基线，但不得称其为用户需要长期保留的真实普通对局，也不得把结果外推到其他槽。

Prompt 要求：除目标、步骤、预期和停止条件外，必须明确“达到目标后做什么”，尤其在战斗场景中逐项说明是否禁止领取金币、卡牌、药水、遗物或进入地图。用户现场说明与旧 handoff 假设冲突时，Implementation Codex 应明确记录并采用最新事实；若冲突改变安全边界、允许写入范围或验收标准，则暂停并交回 Project Lead 修订，而不是自行扩张任务。

基线规则：fixture/场景准备阶段、正式基线建立前发生的意外奖励领取可记录并以新状态重建完整基线；基线建立后、transaction 开始前发生变化则旧基线作废，必须重新 preflight；受测 transaction/phase 内或判定窗口内发生的变化不得 rebaseline 掩盖，必须保留证据并按 handoff 的 FAIL/INVALID/停止规则处理。

理由：SPIKE-INV003A 暴露了两个容易造成错误结论的 prompt 缺口：测试槽被误述为真实普通对局，以及“完成战斗”没有明确奖励后的停手边界。修正后可避免错误扩大安全结论，也避免正常人工习惯污染比较基线。

复审条件：测试环境改变为另一个正式指定槽，或未来建立能自动冻结输入并标记基线窗口的可靠 harness；在此之前所有运行 handoff 继承 `TEST_PROTOCOL.md` 的上述语义。

## D-033：接受 SPIKE-INV004A 的安全失败，先诊断 manager 等价性

日期：2026-09-10  
状态：Accepted

决策：接受 `investigation-reports/SPIKE-INV004A-report.md` 为工作包完成、实验结果 FAIL。失败点是安装全局 override 之前的 isolated-manager content-equivalence gate；因此 direct `UpdateProgressAfterCombatWon`/`SaveProgressFile` 隔离没有被测试，不能据此认定原存档泄漏、direct-write 路由失败或 transaction 候选被否决。

证据边界：candidate 创建阶段发现从受保护磁盘文件重新初始化的 manager 与当前 live manager 的 Settings/Prefs/Progress 合并比较不等。probe 未记录具体组件或字段，且在 `TransactionAudit.Start`、`MockInstanceForTesting`、`SetUpReplay` 与场景启动前停止。安全哨兵通过：原 manager 身份、`RunManager.State == null`、Continue、12 个受保护文件与 inventory 保持不变；原 progress canonical hash 前后相同。恢复阶段的无条件 bookkeeping 不能替代一次已启动 audit，也不能宣称原 store 零调用已被动态审计。

下一任务：只批准 `SPIKE-INV004B`。先静态核对 Settings/Prefs/Progress/profile 初始化、默认值、迁移和规范化路径，再在主菜单逐组件比较 live manager、disk-rehydrated candidate 与 live-seeded candidate；候选必须同时满足独立引用和精确状态等价。该任务禁止安装 override、建立 replay/run state、调用 progress 方法、读取/分派 event 或写真实 store。

后续关系：只有 SPIKE-INV004B 确认确切差异来源，并得到可审计的独立等价 candidate，Project Lead 才考虑发布一个新的 direct-write sentinel 工作包；不得直接重试或修改原 SPIKE-INV004A 继续运行。fault/restart matrix、SPIKE-001A2 与 SPIKE-001B 均继续阻塞。

复审条件：若逐组件差异证明只是确定性的 JSON 表示顺序且结构值等价，可修订 equality gate；若 live-seeded 完整初始化仍无法得到独立等价 manager，则重新评估全局 SaveManager transaction 候选，而不是降低 G-11 存档隔离要求。

## D-034：接受 SPIKE-INV004B 的局部成功，先关闭 startup/save scheduling 未知

日期：2026-09-10  
状态：Accepted

决策：接受 `investigation-reports/SPIKE-INV004B-report.md` 为工作包完成，最终分类保持 `PARTIAL_DIAGNOSED`。Candidate B 在本轮固定主菜单 snapshot 上达到 Settings/Prefs/Progress raw 与 canonical 精确等价，并通过 367 个可变节点零共享引用检查；但 Candidate A 同样完全等价，SPIKE-INV004A 的历史差异没有复现或定位，所以不满足原工作包的 `PASS_DIAGNOSED` 条件，也不解锁 direct-write sentinel。

证据边界：两个 candidate 的 I/O 均封闭在独立 `MockGodotFileIo`，没有全局 manager override、run/scene、replay event 或 progress mutation/save。诊断窗口内原 manager、live hashes、12 个受保护文件及 inventory 均不变。正常退出后 `settings.save`、`prefs.save.backup` 与 `progress.save.backup` 的 mtime 更新而长度/hash 不变；该现象发生在诊断窗口之外，不能归因于 candidate，但也不能表述为退出阶段无真实写入。Steam Cloud 未被机器验证，本轮完成消息亦未单独重申；结合此前固定测试环境只保留为外部未复核项，不扩大本轮结论。

下一任务：批准纯静态 `INV-005`，追踪 production `SaveManager` 初始化到主菜单就绪以及正常退出期间的 Settings/Prefs/Progress mutation 与保存调度，特别识别未等待 task、deferred callback、退出 flush 和可观测 quiescent boundary。目标不是追索一个已不可复现的具体 JSON 字段，而是判断 live-seeded capture 能否在 transaction 前避开 pending production writes。

后续关系：INV-005 必须给出且只给出一个下一建议：以 live-seeded candidate 进入重新编写的 direct-write sentinel、先增加一个更窄的 runtime timing gate，或停止并重新评估全局 transaction。未经 Project Lead 新工作包，不得重试 SPIKE-INV004A/004B。fault/restart matrix、SPIKE-001A2 与 SPIKE-001B 继续阻塞。

复审条件：如果静态证据能界定主菜单稳定点且所有先前 save task 已完成或可等待/审计，则可用 Candidate B 取代 disk-rehydrated 构造；若存在无法观察或约束的延迟真实-store writer，则先否决或收窄全局 override 方案，不降低 G-11。

## D-035：接受 INV-005，并用显式事件建立 capture gate

日期：2026-09-11  
状态：Accepted

决策：接受 `investigation-reports/INV-005-report.md`，采用其唯一建议 `REVISE_WITH_TIMING_GATE`。下一运行实验固定为 `SPIKE-INV005A`：显式观测 `InitializeGraphicsPreferences` entry/exit、`NGame.GameStartupComplete` 完成、original `CurrentRunSaveTask` null/completed 及相关 store operations；不安装 manager override、不构造 candidate、不启动 run，也不调用 progress mutation/save。

理由：Settings/Prefs/Progress 的常规保存均同步，startup cloud sync 与 prior run save 又在 production main-menu 路径中被 gate，因此目前没有证据要求停止 transaction 候选。但 graphics initialization 由无 task handle 的 `CallDeferred` 调度，会在执行时读取当前 `SaveManager.Instance` 并可能修改 Settings。现有 `_Ready` peer entry 早于 startup-complete，静态 IL 也不能证明 deferred callback 已在任一现有边界前执行；固定等待若干帧只是经验延时，不是可审计 barrier。

证据边界：localization/display repair 与 `FastMode 3→2` 是可造成 live/disk 漂移的 confirmed production 路径，但不是已确认的 SPIKE-INV004A 历史根因。退出 temp-write/backup/timestamp-sync 精确解释观察到的 mtime 形状，但真实那次退出的 attribution 仍为推断。任意第三方模组 callback 不在固定程序集静态保证内；当前 MVP 环境仍只保证 Replay Mod 及必要依赖。

下一实验判定：只有 patch 在 callback 排队前已生效、graphics callback 正常 exit、startup task 完成、original run-save task 无 pending、manager identity/组件 hash/受保护文件满足 gate，才可记为 `PASS_GATE`。超时只允许判 `STOP_MISSING_EVENT`，不得作为成功依据。成功后仍需 Project Lead 另发采用 live-seeded candidate 的新 direct-write sentinel；SPIKE-INV005A 本身不解锁 fault/restart、reconstruction 或 action replay。

管理边界：Implementation Codex 只提交 probe、证据和 investigation report；除 handoff 明确授权外，不修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md` 或 Roadmap。INV-005 已写入的两处状态更新经本次复核后由 Project Lead 接管并修订，不作为后续默认授权。

复审条件：若 runtime 证明事件 gate 稳定达成，则以该 gate 为后续 transaction precondition；若 callback 无法可靠观测、发生于 gate 后或出现无法归因的 original-store writer，则停止并重新评估全局 override 候选。

## D-036：SPIKE-INV005A 以 safety failure 控制，先收窄 cloud 与审计语义

日期：2026-09-13  
状态：Accepted

决策：接受 `investigation-reports/SPIKE-INV005A-report.md` 的复审分类：原始 runtime artifact 保留 `FAIL_GATE`，项目工作包以 `COMPLETE — FAIL_SAFETY` 控制。旧 probe 立即退役，不得重跑或在原工作包中修补。下一任务只批准纯静态 `INV-006`，调查 cloud-disabled startup 的 production barrier 层级，并定义 scope-safe、fail-closed 的审计契约。

理由：本轮确认 patch-active 早于 graphics entry，graphics callback 正常 exit，`GameStartupComplete` 成功，original manager 未替换且 `CurrentRunSaveTask` 为 null；这些局部事件说明 timing hook 可行。但 observed Steam remote leaf task fault 被当前 gate 当作失败，而 production outer cloud task 可捕获 leaf fault 并继续完成，说明 gate 层级过低。更严重的是，instrumentation 在 `_enabled`/scope 判断之前就解析并 hash payload，并把首个 fault 延迟到 startup/graphics 完成后才 Finish，导致 out-of-scope profile payload 的派生信息进入 artifact，违反明确的数据范围。

安全边界：没有证据显示 probe 修改了其他 profile，也没有保存原始 payload；越界内容限于逻辑审计元数据、长度与 SHA-256。但这些仍属于未经授权的派生访问。原 artifact 本地保留为 restricted evidence，不得发布、外传或用作 fixture；是否删除或生成可公开的 sanitized replacement 是单独的破坏性/证据管理决定，未经项目发起人指示不执行。

INV-006 必须回答：哪一层 task 是 production 真正等待的 cloud barrier；leaf fault 在 Steam Cloud 关闭/remote API unavailable 时如何被捕获；future observer 如何在接触 payload 前以 active profile1 allowlist 过滤；对非目标调用是完全忽略还是至多保留一个不含次数、path、operation、时间和 payload 的全局布尔值；首个 terminal condition 如何原子 seal 并让所有 patch fast-return。报告只能建议“签发 scope-safe timing probe”或“停止该 instrumentation 路线”之一。

后续关系：在 INV-006 和新的 scope-safe timing gate 均通过前，不得发布 live-seeded direct-write sentinel。SPIKE-001A2、SPIKE-001B 与 fault/restart matrix 继续阻塞。SPIKE-INV005A 的局部 timing 结果不得外推为 capture gate PASS。

复审条件：若静态证据找不到与 production 一致且不需观察其他 profile 的 barrier，或无法在 Harmony observer 中先过滤再读取 payload，则停止该 instrumentation 路线并重新评估 transaction 候选；不得以“只有 hash”降低 profile 边界。

## D-037：Alchyr ModTemplate 只作为未来正式工程脚手架参考

日期：2026-09-13  
状态：Accepted

决策：接受 `investigation-reports/TOOLING-001-report.md` 的 `ADOPT_AS_SCAFFOLD_REFERENCE`。在正式实现仓库搭建时，可用固定 commit `55ca2c606e6c78dd39689a5cf979b243a49652e7` 的 Empty Mod 作为布局参考；当前不安装模板、不迁移 experiments、不引入默认 BaseLib 依赖，也不改变 INV-006 与 session-isolation 的调查路线。

理由：模板提供 Godot/.NET、manifest、Harmony initializer、游戏路径发现和 DLL/PCK 发布的成熟样板，能降低后续工程搭建成本；但它不提供 replay、状态恢复、seek 或存档隔离能力。仓库及 template package 作者是 Alchyr，BaseLib 被其文档描述为社区库，因此“游戏内提供/链接”不能外推为 Mega Crit 维护的官方代码。

采用门禁：正式生成工程前必须固定全部 dependency/tool 版本，更新明确的 game compatibility，分离 build/test/deploy，禁止普通 build 默认写 live game 目录，令 PCK export failure 阻断 release，保持 Publicize 默认关闭，并建立本项目许可证、测试、CI 与构建身份记录。BaseLib、ModAnalyzers 与 PCK 必须分别按实际能力决定，不继承模板默认值。

后续关系：该决策只减少正式工程的样板工作，不解锁任何 replay 技术 gate。作出本决策时的下一任务仍为 INV-006；其后续状态由 D-038 接管。

## D-038：接受 INV-006，批准全新 scope-safe outer-cloud timing probe

日期：2026-09-13  
状态：Accepted

决策：接受 `investigation-reports/INV-006-report.md` 的 `READY_SCOPE_SAFE_TIMING`，并由 Project Lead 修正 contract 中 post-seal artifact writer 的字面冲突。下一任务固定为全新 `SPIKE-INV006A`：只通过零参数 `NGame.DoCloudSync()` postfix 捕获 outer task，并与 graphics normal exit、startup/menu marker、original manager identity、`RunManager.State == null`、original `CurrentRunSaveTask` 及一次人工 Verify 汇合。不得 patch global store/leaf、读取 path/payload 或沿用旧 artifact schema。

理由：固定 v0.111.0 IL 证明 `GameStartup` 保存 `DoCloudSync` 返回 task 并只轮询 `IsCompleted`，没有取 result/status/exception；因此 success、fault、cancel 都满足 production admission。`CloudSaveStore` 在 lower layer 捕获预期 Steam leaf write exception，解释了 leaf fault 与 outer completion 可同时成立。`GameStartupComplete` 在正常与 startup catch 路径都可被置为完成，故只能作为后续 lifecycle marker，不能单独证明 cloud work 收束。Project Lead 独立复现 hash-gated 查询，输出 SHA-256 与提交证据完全一致，错误输入命中 `BLOCKED_IDENTITY`。

安全契约：新 probe 的最小证据不依赖任何 store、path 或 payload。所有普通 observer entry point 必须 pre-argument 检查 sealed/enabled；terminal 使用单一 CAS，事件锁二次检查形成闭合集合；post-seal continuation fast-return。只有 terminal winner 持 opaque ticket 可对已复制的 closed snapshot 执行一次 artifact write，其他 post-seal artifact call 在 snapshot/path 访问前返回。artifact 只保留 enum、runtime identity、monotonic ordering 和固定 build identity。contract synthetic negative/race cases 与 forbidden-surface source/binary audit 在安装前必须通过。

证据边界：INV-006 是静态结论，不证明新 gate 在运行中一定出现，也不证明 live-seeded candidate、direct progress write 或完整 session isolation。outer task 在 Steam uninitialized 分支可能仍 pending 而 production 已继续；未来 probe 遇到该情况必须停止为 missing barrier，不能以 production 继续或 fixed delay 当作 settled。

后续关系：只有 `SPIKE-INV006A` 得到 `PASS_SCOPE_SAFE_GATE`，Project Lead 才能另发新的 live-seeded direct-write sentinel。fault/restart、SPIKE-001A2、SPIKE-001B 继续阻塞。

## D-039：接受 SPIKE-INV006A，解锁独立 live-seeded direct-write sentinel

日期：2026-09-13  
状态：Accepted

决策：接受 `SPIKE-INV006A` 为 `COMPLETE — PASS_SCOPE_SAFE_GATE` 并立即退役该 probe。下一任务为全新 `SPIKE-INV006B`，在同一固定 build 上重新建立该 timing gate，以 live-state serialization 构造 independent in-memory candidate，再执行两个 direct production progress methods 与 ordered teardown。不得在旧 probe 中继续。

证据：runtime artifact 为 schema `spike-inv006a.timing.v1`，事件 1–10 连续且 monotonic ticks 严格递增；唯一 outer task `Pending → RanToCompletion`；graphics `1 entry / 1 normal exit / 0 active`；Gate/Verify 的 manager/task identity、status 和 11 项条件完全一致；无第二 lifecycle 或 post-seal event。Project Lead 独立核对 artifact 3,554 bytes / SHA-256 `7F45652CC5AB7524CB3444B99FA8AF8AF5EFF0E47EB433A3306C2FAE44B52823`，并重跑 preflight：23/23 tests、0 warning/0 error、source/binary forbidden-surface audit 和 build identity 均通过。

安全边界：006A 未读取 save/profile/path/payload，未安装 override、构造 candidate、调用 progress/save 或进入 run。该 PASS 只证明固定环境中的 pre-override timing gate，不证明存档隔离。probe 已达到首个终态，必须关闭且不得重跑。

006B 约束：candidate 必须从 gate 后 live state seed，不再使用 disk rehydration；保护 I/O 仅限 profile1 subtree、exact account selector、machine settings 与固定 fixture；禁止 account-root/其他 profile 枚举。original/unknown store audit 必须先按 instance identity fail closed，未授权前不得读取 path/payload。PASS 只证明 direct update/save 路由至 isolated memory store。

后续关系：只有 006B PASS 才能另发 setup/post-launch fault 与 restart matrix；SPIKE-001A2、SPIKE-001B 继续阻塞。

## D-040：接受 SPIKE-INV006B 的安全前置失败，先静态收窄 `RunState`

日期：2026-09-14  
状态：Accepted

决策：接受 `SPIKE-INV006B` 为 `COMPLETE — BLOCKED_PRECONDITION` 并立即退役。接受 timing gate、profile1 保护 baseline、live-seeded candidate 等价/引用独立和 ordered teardown 的正面证据；不接受任何 direct-write isolation 结论。下一任务固定为纯静态 `INV-007`，拆分共用 `RunState` 的全部断言，并从两个 production progress 方法的真实读取反推最小调用前置条件。不得修改或重跑 006B。

证据：closed artifact 为 17,487 bytes / SHA-256 `98B91618F67E7AD5E651C1BC1B41515040A599976FC237E8FC24279134538511`，事件 1–17 连续；运行到 `OverrideInstalled` 后没有 `SceneReady`、`ProgressUpdated` 或 `ProgressSaved`。Profile/Settings/Prefs/Progress raw/canonical 全部等价，共享可变引用为 0；update/save/memory-write 计数为 `0/0/0`。teardown cleanup 一次、原 manager 恢复、run state null、Continue 可见；12 个保护文件与 inventory 无变化，12 份 recovery backup 校验一致。Project Lead 独立复核固定身份并重跑 preflight：12/12 synthetic tests、0 warning/0 error、source/binary audit 与 70-method patch smoke 均通过。

解释边界：`RunState` 同时代表至少八个不同条件，跨越单玩家反序列化、`ShouldSave`、`NGame`、manager/run identity、room/encounter、spawned enemies 和 fixture-specific identity。用户看见战斗画面及 game log room marker 只支持“post-load 检查更可疑”的推断，不能确认具体失败条件。旧 artifact 按最小化原则没有保存这些值，所以不能通过重读证据唯一定位。

后续关系：INV-007 若给出 `READY_NEW_SENTINEL`，Project Lead 才能签发全新 ID/schema 的运行实验；若给出 `INSUFFICIENT_STATIC_EVIDENCE`，只能先设计另一个最小、非敏感、固定枚举诊断。fault/restart、SPIKE-001A2 与 SPIKE-001B 继续阻塞。

## D-041：接受 INV-007，批准最小契约的新 direct-write sentinel

日期：2026-09-14  
状态：Accepted

决策：接受 `INV-007` 的 `READY_NEW_SENTINEL`。下一任务为全新 `SPIKE-INV007A`，保留 006B 已验证的 timing、profile1 protection、live-seeded candidate、identity-first boundary audit 与 ordered teardown，但以两个 production progress 方法真实读取的字段建立 pre-call contract；实际 character/encounter/enemy IDs 只用于动态 mutation oracle，不作为固定身份 gate。不得修改、构建、安装或重跑 006B。

证据：Project Lead 独立复现 hash-gated IL 与 fixture checker。IL 输出 160,808 bytes / SHA-256 `A36EDD5BE0499162B297CDC6C1BA23547990352D38FC7B1BA27937E1AD4EEBAE`；fixture contract 1,081 bytes / SHA-256 `BBCF28AE5D6B7F6652751C6D15476F0E243DFB43EB0A21EBD648B2B6265EE0BB`；checker build 0 warning/0 error。IL 确认 update 读取实际 player/character、CombatState/Encounter、RoomType、enemy list/IDs 和 candidate Progress；save 读取 candidate Progress、migration、profile provider 和 store并同步 `WriteFile`，内部捕获写异常。

删除项：新 sentinel 不得要求固定 IRONCLAD、两只 TOADPOLE、固定 encounter/enemy ID、两个 encounter getter 的引用/ID 对比、`CombatState.RunState` 与外部 runState 相等，或 executor/frame readiness。`RoomType == Monster` 仅作为避免 Elite/Boss helper 的实验范围；`ShouldSave=false`、candidate identity 与 run-state binding 只能标为 isolation safety/setup integrity，并使用独立失败枚举。

验收边界：PASS 必须同时证明 update 恰好一次、update 后无 write、candidate mutation与独立动态 oracle相等、save 恰好一次、memory store 恰好一次同步 exact progress write、payload canonical相等、无 original/unknown/external boundary，并完成 teardown与保护检查。即使通过，也不代表自然 victory、achievement、fault/restart、完整 session isolation 或 replay fidelity。

后续关系：只有 SPIKE-INV007A PASS，Project Lead 才考虑独立 fault/restart matrix。SPIKE-001A2 与 SPIKE-001B 继续阻塞。

## D-042：将一终态规则改为 evidence-preserving revision policy，批准 007A R2

日期：2026-09-14  
状态：Accepted

决策：SPIKE-INV007A 原始 attempts 分类为 `INVALID_HARNESS_ATTEMPTS / NO DIRECT-WRITE CONCLUSION`。不要求复制全新工程；批准在相同 `SPIKEINV007A` mod id/source tree 上做一次窄 R2 revision。必须保留所有旧 artifact/recovery，提升 probe version 与 schema至 v2，改用 `runtime-artifact-r2.json` 和独立 recovery目录，并仅修复 Ready 后 timeout、attempt artifact identity、prior-attempt integrity 与相关 tests。

证据：R1 artifact 3,108 bytes / SHA-256 `D38EC65F8380DDC2AFF1C8F6EB9E7FDEBEAB1D706424C4471C36DAAD0488ACA2`，事件到 `GateReady` 后在约 120 秒由 `TimeoutCallback` 封存；没有 RunAccepted/protection/candidate/audit/sentinel/teardown。源码确认 timeout从 Activate 创建，`GateReady` 不取消，callback也不检查 `_gateReady`。后一进程创建 12 份 recovery backup，但 writer在固定 `runtime-artifact.json` 已存在时返回 false，因此只显示 `BLOCKED_ARTIFACT_WRITE`，真实业务 terminal缺失。

规则修订：首个终态规则继续禁止覆盖证据、现场追着业务失败改假设和无保护重试；但明确的 pre-business harness defect可由 Project Lead批准同 id窄修订。若业务可能执行而 artifact缺失，必须在新进程先把当前 allowlisted内容/inventory与该 attempt的 pre-action recovery逐项比较，并确认 Continue/run/manager基线；差异即 `BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`，不得恢复或 rebaseline。Ready 后不得继续用 startup timeout约束人工响应时间。

范围边界：R2 不得修改 direct-write 业务契约、场景、candidate、audit、oracle、teardown、允许 I/O或 PASS标准。安装只可在游戏关闭且 installed pair精确匹配 R1已知 hash时替换 007A自己的 DLL/manifest。R2 达到首个 terminal后不再重试。

后续关系：只有带完整 R2 artifact、teardown和 protection证据的 `PASS_DIRECT_WRITE_ISOLATED` 才解锁 fault/restart 评估。SPIKE-001A2 与 SPIKE-001B 继续阻塞。

## D-043：接受 R2 direct-update 局部证据，以 detached oracle 进入 R3

日期：2026-09-14  
状态：Accepted

决策：将 SPIKE-INV007A-R2 验收为 `VALID TERMINAL — ORACLE_ALIASING`。接受 update在candidate上恰好一次、无original/unknown/external boundary、零write、teardown和保护集合通过；不接受artifact的 `FAIL_MUTATION` 为production机制失败，也不接受完整direct-write PASS。批准同一源码的窄R3 revision，只修复oracle深度隔离、pre-call non-interference、attempt v3 identity和R2 prior-integrity。

证据：R2 artifact 19,259 bytes / SHA-256 `4B48A75F650A687F206255F2425140B3F640277E3D62F21495F99CC6086B691F`，事件1–22连续且ticks递增；candidate update/save/write为`1/0/0`，boundary firstFailure None。固定IL显示 `ProgressState.ToSerializable()` 对EncounterStats/EnemyStats只新建outer list，保留nested stats/FightStats对象；源码随后对该结果执行 `ApplyExpected`，因此在production update前已改变candidate。expected/actual hash差异符合一次oracle预增加一次production增加的因果链。

安全结论：12项protection before/after逐字段相等，12份R2 recovery hash与source一致；cleanup、manager restore、run state null和Continue均通过。灰色scene来自load后约3.4 ms即teardown，不是本实验失败；`Invalid Task ID`目前仅为伴随日志，未证明影响update，R3不得顺手调查。

R3门禁：expected必须从pre-call candidate baseline JSON deserialize round-trip构造；ApplyExpected后、production前candidate canonical和player discovered必须仍等于baseline，且expected/candidate mutable graph共享引用为0。任一失败以oracle-specific分类停止且update count必须为0。不得通过放宽comparison、调用两次、事后减一或忽略字段获得PASS。

后续关系：只有R3完整验证update、save、唯一memory write、payload、boundary、teardown与protection后，才考虑fault/restart。SPIKE-001A2与SPIKE-001B继续阻塞。

## D-044：接受 R3 direct-write PASS，先静态收窄 fault/restart matrix

日期：2026-09-14  
状态：Accepted

决策：将 SPIKE-INV007A-R3 验收为 `PASS_DIRECT_WRITE_ISOLATED` 并永久封存。接受固定 v0.111.0 isolated Monster session 中 detached oracle、一次 update、一次 save、唯一同步 memory progress write、payload canonical equality、identity-first boundary、ordered teardown 与 12 项 protection/recovery 证据。下一任务只批准纯静态 `INV-008`；先追踪 setup/post-launch fault 注入点、transaction owner、pending task/callback、teardown 顺序与下一进程 restart verification，再决定是否签发 runtime matrix。

证据：R3 artifact 19,953 bytes / SHA-256 `AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553`，schema/version 为 `spike-inv007a.direct-write.v3` / `0.3.0-direct-write-r3`，terminal 为 `PASS_DIRECT_WRITE_ISOLATED / None`，事件 1–24 连续且 ticks 严格递增。oracle parse成功、shared mutable references为0、candidate/discovered未变、pre-call audit为0；update/save/write为`1/1/1`，expected/actual hash同为 `F25CA6D8523112C5699A24CEA374380A76CE3645C5EA080EB857EB7C2AE5F2E3`。Project Lead独立复核12项before/after全字段一致，12份R3 recovery长度/hash与artifact记录一致。

证据边界：该 PASS 只验证两个被直接调用的 production progress methods 在固定 transaction 中的路由与写入隔离。它不证明自然 victory/achievement、任意 replay 内容、异常 teardown、进程重启、所有平台 side channel、其他机器/Steam状态或完整产品 session isolation。灰色/残缺UI不进入本结论。

下一任务边界：INV-008不得启动游戏、访问profile/save/cloud或recovery payload、修改/构建/安装/运行旧probe，也不得直接实现runtime probe。它必须区分可控 owner exception、teardown自身失败与强制进程终止；强制崩溃默认不进入首轮运行矩阵，除非静态证据证明不可替代且能给出安全恢复协议。

后续关系：只有 fault/restart runtime matrix 按新契约通过，Project Lead 才复审 transaction候选的 session-isolation阶段结论并修订 `SPIKE-001A2`。`SPIKE-001B`继续阻塞；direct-write PASS不能跳过reconstruction control。

## D-045：接受 INV-008，签发四阶段 controlled fault/restart matrix

日期：2026-09-15  
状态：Accepted

决策：接受 INV-008 的 `READY_FAULT_RESTART_MATRIX`。下一任务为全新 `SPIKE-INV008A`，按四个独立fresh process顺序执行：setup owner fault、restart-after-setup、post-launch owner fault、restart-after-postlaunch。两个fault均为probe-owned同步异常，只能由transaction owner捕获并进入唯一finally；teardown failure独立分类并覆盖“预期异常已捕获”的表面成功。

证据：Project Lead独立重建StaticLifecycleQuery为0 warning/0 error，并生成132,086-byte输出，SHA-256 `453C31CC5C22E0FD0C54F3D0CFC191FBDB967B2DBFF508A06A050A2A635354B8`，与封存输出逐行一致；错误程序集触发`BLOCKED_IDENTITY`。固定IL确认`SetUpReplay`与`Launch`同步返回、NRun/scene同步安装边界、`RecordInitialState`进入replay direct writer的调用位置、`CleanUp`主体disposer序列与只清理部分全局字段的single finally，以及`LoadMainMenu`在调用时读取当前manager/task和Continue的顺序。

运行边界：setup fault固定在`SetUpReplay`完整返回、override/state/`ShouldSave=false`/sync-disable/writer-not-recording门禁通过后且Launch前；post-launch fault固定在Launch返回、新NRun同步安装后且GenerateMap/room/frame/action前。不得进入map/room、dispatcher、victory、reward或progress方法。强杀、native crash、断电与未执行finally的恢复明确延期。

跨进程修订：fault phase自身before/after仍要求12项hash/length/mtime与inventory全等。用户在terminal后正常退出可能触发已由INV-005确认的production保存链；紧邻restart phase只允许`settings.save`、`saves/prefs.save.backup`、`saves/progress.save.backup`出现内容/长度不变的mtime-only变化，并必须如实记录为正常退出写入。其他mtime或任一内容、inventory、Continue、`NumReloads`变化均失败；restart PASS不得表述为零写入或crash recovery。

停止关系：每阶段artifact/recovery不可覆盖，下一阶段只由上一PASS artifact解锁。任一FAIL/BLOCKED后停止，不恢复backup、不rebaseline、不自动进行安全性更弱的下一phase。四phase全PASS后，Project Lead才复审当前transaction候选是否足以通过session-isolation阶段；不会自动解锁crash test、SPIKE-001A2或SPIKE-001B。

## D-046：008A v1 为 pre-business 环境阻断，批准安装生命周期 R2

日期：2026-09-15  
状态：Accepted

决策：将SPIKE-INV008A v1分类为`PREBUSINESS ENVIRONMENT BLOCK / NO FAULT-RESTART CONCLUSION`。Phase 0证据保留为通过；首次`BLOCKED_IDENTITY`与随后`BLOCKED_PHASE_ORDER`均不解释为transaction、fault point或teardown失败。批准在相同`SPIKEINV008A` mod id/source tree上做窄R2，提升schema/revision并使用独立四phase artifact/recovery chain，只修复install/enabled-state lifecycle、configuration precheck和prior-attempt identity。

证据：Project Lead独立解析`setup-owner-fault.json`（2,359 bytes / SHA-256 `56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48`）：事件1–8只到startup terminal，`protectionBefore/After`、candidate、recovery、fault、audit、teardown与restart均为null；对应日志确认007A与008A同时loaded。`invalid-phase.json`（1,479 bytes / SHA-256 `A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F`）只有`PatchActive → BLOCKED_PHASE_ORDER`，所有业务字段亦为空；此时日志已确认007A disabled且只loaded 008A。Project Lead另行复跑34/34 tests、Release 0 warning/error、source/binary audit及112-method Harmony smoke均通过。

流程修订：未来要求唯一probe的任务必须在安装新probe之前完成旧probe禁用和持久化验证；不得先安装后进入同一进程修正。wrong-mod-set应作为独立precheck evidence，不消费尚未建立protection baseline的业务phase；只有handoff显式预授权的纯配置修正可重新precheck。安装文件清理必须由Project Lead逐handoff授权且hash精确，源码/证据永不随之删除。

R2边界：v1两份artifact及全部既有证据不可变。R2不得降低唯一loaded ID门禁或修改fault点、teardown、observer、protection、mtime、人工动作与PASS标准。installed 008A pair只有在游戏停止、v1 hash精确且最新日志证明007A持久化disabled时才能被R2 pair替换；不得修改007A文件或enabled配置。

后续关系：四个R2业务phase全部PASS后才复审controlled fault/restart isolation。任一业务FAIL/BLOCKED停止；只有configuration precheck wrong-mod-set享有工作包内一次或多次attempt-specific安全修正授权。SPIKE-001A2与SPIKE-001B继续阻塞，强制crash继续延期。

## D-047：R2 为 loaded-mod 观察时机缺陷，批准带有限修复授权的 R3

日期：2026-09-15  
状态：Accepted

决策：将 SPIKE-INV008A-R2 分类为 `PREBUSINESS_PRECHECK_TIMING_DEFECT / NO R2 FAULT-RESTART CONCLUSION`。保留 R2 precheck artifact，但不把空 loaded set解释为真实配置错误。批准在相同 `SPIKEINV008A` mod id/source tree上进入R3：移除initializer中的最终loaded-set判定，把它放到可证明的post-loader且pre-profile/protection边界；提升schema/revision并使用R3专属business artifact/recovery，原四阶段契约不变。

证据：R2 precheck artifact为242 bytes / SHA-256 `2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916`，只记录`loadedModCount=0`、空ID列表和`WrongLoadedModSet`。同一完整进程日志为252,133 bytes / SHA-256 `7F01AE43BA2E43776D7A60413B3D6BC07875BFF38B92DE448420DDC573540171`，明确记录007A因disabled被跳过、loaded list恰好为008A、最终`Loaded 1 mods (16 total)`，且没有008A invalid SemVer警告。源码确认`ProbeEntryPoint.Initialize → ProbeController.Activate`同步执行，而`Activate`立即调用`ModManager.GetLoadedMods()`；loader直到initializer返回后才报告mod初始化完成和最终loaded count。没有R2 business/invalid artifact或recovery，protection/candidate/override/run/fault均未建立。

流程修订：Project Lead可在handoff中授予有类别与次数上限的`harness repair envelope`。在能机器证明仍处于pre-business、旧证据不可变、业务契约与安全边界不变时，Implementation Codex可自行修改/测试/构建/精确替换当前probe并请求下一次人工启动，无需每个代码修补都返回Project Lead。游戏仍由项目发起人手动运行；触及protection/candidate/override/run/recovery/fault/业务调用、改变PASS范围或超出修复额度时授权立即终止。

R3边界：首次exact loaded-set检查首选放在正常`NMainMenu._Ready` callback最前部，且必须早于`SaveManager.Instance`、Continue与prior-integrity读取；后续gate revalidation仍保留exact check。若该边界经静态/运行证据仍不稳定，只能在repair envelope内进一步延后到可证明的post-loader/pre-profile callback，不得删除或放宽门禁。

后续关系：R3四个业务phase全部PASS后才复审controlled fault/restart isolation。SPIKE-001A2、SPIKE-001B与强制crash继续阻塞。

## D-048：R3 prior gate不可满足，批准fresh phase-local baseline的R4

日期：2026-09-15  
状态：Accepted

决策：将SPIKE-INV008A-R3验收为`VALID ARTIFACT / IMPOSSIBLE PRIOR GATE / NO OWNER-FAULT OR RESTART CONCLUSION`并永久封存。R3不得修改、重跑、恢复backup或解锁下一phase。批准同一实验lineage的R4，仅修复baseline来源、Continue证据与terminal UI；两个fault点、transaction owner、teardown、boundary、保护范围和PASS标准保持不变。

证据：R3 artifact 3,740 bytes / SHA-256 `5C8E49B98F7BC9A647033F779DC3CACCEBE3D840214E4D95995EBADB7C0B13F0`，在sequence 8、约11.94秒封存`BLOCKED_PRIOR_INTEGRITY`。loaded-set与历史attempt identity通过；protection/candidate/recovery/fault/audit/teardown/restart均为null。Project Lead对固定12路径仅做length/hash复核：profile1十项与AccountSelector全部仍等于007A-R3；仅`settings.save`由5,122 bytes/hash `D1E034...63F9`变为5,238 bytes/hash `268051...B11F`。固定API文档确认SettingsSave.ModSettings承载mod enabled/disabled，故精确字段归因虽未读取payload、仍为推断，但旧settings字节恒等与已要求的模组启用切换在设计上冲突。

确认的harness根因：`VerifyR3`把`CaptureRecoveryBaseline`产生的copy mtime交给`RestartRules.Compare`，而copy未保留源mtime；因此广泛mtime差异不是原文件广泛写入证据。它还从007A-R3读取`teardown.continueAfter`/`restart.continueCurrent`，但该固定schema两者均不存在；随后又要求expected非null，使`continueEqual`确定为false。该gate没有正向可达状态。

R4规则：历史artifact/recovery只验证可比的内容身份和证据链；第一fault phase在GateReady fresh捕获Continue，在Run后且override/setup前建立12项source snapshot与recovery，并用source artifact mtime做phase/restart比较。recovery copy只验证bytes。任何旧schema字段必须在离线preflight先验证存在；完整prior gate必须用真实固定前驱artifact通过正向集成测试。

后续关系：只有R4四phase全PASS才复审controlled fault/restart isolation。SPIKE-001A2、SPIKE-001B和强制crash继续阻塞。

## D-049：接受 R4 cleanup-contract FAIL，先静态判定 RunLobby 条件义务

日期：2026-09-15  
状态：Accepted

决策：将 `SPIKE-INV008A-R4` 验收为 `VALID BUSINESS TERMINAL / FAIL_TEARDOWN_CLEANUP / ROOT CAUSE UNRESOLVED` 并永久封存。接受 setup fault 注入、owner catch/finally、保护内容、Continue、run state 清理和 manager 恢复的局部正面证据；不执行紧邻 restart phase，也不直接授权 R5。下一任务只批准纯静态 `INV-009`，判定缺失的两个 RunLobby 调用在 setup-fault 边界是否为 production 必需义务，并制定 setup/post-launch 分阶段 cleanup oracle。

证据：R4 artifact 为 23,241 bytes / SHA-256 `40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040`，终态 `FAIL_TEARDOWN_CLEANUP`、failure site `Cleanup`。fault/catch/finally 各一次；Launch、scene install、业务动作与写入均为零；12 项保护内容及 Continue 相等，state null、manager restored。固定 oracle 的 15 步只缺 `RunLobby.remove_RemotePlayerDisconnected` 与 `RunLobby.Dispose`。既有 IL 同时确认 `SetUpReplay` 调用 `InitializeRunLobby`，但 `CleanUp` 仅在 `RunLobby != null` 时执行这两步，artifact 未记录 lobby 状态，因此根因尚未闭合。

解释边界：`FAIL_TEARDOWN_CLEANUP` 是有效的 probe 终态，但不是已确认的 production resource leak。若 INV-009 证明 replay setup 按设计不持有 lobby，则只能精确修订阶段 oracle，不得泛化放宽 teardown；若证明该边界应持有 lobby，则需定位其丢失/未清理原因后再决定是否有安全的新运行实验。无论哪种结果，R4 本身不得重跑。

后续关系：INV-009 只有在给出可复现的 phase-aware contract 后，Project Lead 才决定是接受 setup-fault teardown 局部 PASS、修复 production-facing cleanup 候选，还是签发全新 revision。restart、post-launch、SPIKE-001A2、SPIKE-001B 与强制 crash 均保持阻塞。

## D-050：接受 INV-009，批准 phase-aware oracle 的 R5 四阶段链

日期：2026-09-15  
状态：Accepted

决策：接受 INV-009 的 `ACCEPT_PRODUCTION_CLEANUP_WITH_REVISED_ORACLE`。R4 fresh-process replay setup的正确call-level义务为13项core cleanup；两个RunLobby调用是一组`Conditional(preCleanupRunLobby != null)`义务，在R4路径中Not applicable。R4可重解释为setup owner fault的core call-set局部PASS，但因原oracle提前阻断menu load，没有menu restoration或restart结论。批准同一008A lineage的全新R5 revision完整重走四阶段，不得重跑或覆盖R4。

证据：Project Lead独立复现INV-009 hash-gated查询。`NetReplayGameService.Type=Replay(4)`，`IsMultiplayer`只接受Host/Client；`InitializeRunLobby`只在true分支构造、set并订阅RunLobby，程序集唯一setter caller亦在该分支。`CleanUp`仅在getter非null时成对退订/Dispose。固定输出253,870 bytes / SHA-256 `488814C65B9CD8E56F888CFCFE50AD7BEAA890E0396F084BED3916CFBDF772D7`，bounded R4摘要2,063 bytes / SHA-256 `2B50415302BBE5D38874C6AA5CA29EAF2012FA7D4DE923AA842A8AD26762D9D0`；错误artifact负向门禁通过。

R5规则：Replay在pre-setup、post-setup、post-launch、post-scene及cleanup entry的RunLobby均必须为null，任何非null或identity变化fail closed。13项core call仍全部必需；若未来条件为non-null，两个lobby调用必须成对且各一次，不能永久忽略。post-launch另须证明fault前current scene为owned NRun，恢复后为新main menu且旧NRun不再current。其余timing、protection、Continue、boundary、task、manager、immutable evidence和teardown-failure优先级不变。

未知边界：production cleanup Dispose RunLobby后不清空backing field，静态上存在同进程multiplayer→replay继承stale disposed lobby的可能；R5 fresh-process gate只负责拒绝该状态，不证明其一般可达性或后果。强制crash仍延期。

后续关系：只有R5四phase全PASS，Project Lead才复审controlled fault/restart isolation，并决定是否修订SPIKE-001A2。SPIKE-001B继续阻塞。

## D-051：R5 为 menu observer 顺序缺陷，批准窄 R6

日期：2026-09-15  
状态：Accepted

决策：将R5验收为`VALID BUSINESS TERMINAL / MENU OBSERVER ORDERING DEFECT / PRODUCTION MENU FAILURE NOT SUPPORTED`并永久封存。接受其setup fault、lobby oracle、13项core cleanup、owner catch/finally、manager restore、main-menu scene identity、protection与Continue局部PASS；不接受`menuReadyObserved=false`为production menu未ready证据，不执行R5 restart。批准同一008A lineage的窄R6，只修复restored-menu observer identity建立与独立signal结果报告，之后完整重走四阶段。

证据：R5 artifact 25,790 bytes / SHA-256 `9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9`。固定IL确认`NMainMenu._Ready`在`IL_0493`调用`RefreshButtons`后才返回；probe在RefreshButtons postfix要求`ReferenceEquals(menu,_menu)`，但`_menu`仅在随后`_Ready` postfix更新。故refresh信号确定丢失。combined `Task.WhenAll`超时使tuple assignment不发生，两个local均保留false。artifact仍确认restored scene为NMainMenu且Continue读取成功。日志`Invalid Task ID`早于约265秒后的RunAccepted，排除其由teardown触发的直接因果。

R6规则：在`_Ready` prefix只为active transaction记录候选新menu identity；`RefreshButtons`仅接受该identity；`_Ready` postfix仍是menu-ready完成点。两个signal状态必须在超时/异常后分别落盘并有精确failure site，不得增加10秒等待、删除任一信号、接受错误identity或用scene/Continue替代callback。其余R5 contract全部冻结，使用全新schema/artifact/recovery。

后续关系：只有R6四phase全PASS才复审controlled fault/restart isolation。`Invalid Task ID`保留为独立未知，不在无因果证据时扩大R6；若它在R6 teardown之后重新出现并与失败同序，再另行调查。

## D-052：接受 R6 四阶段 PASS，解锁 profile1 reconstruction control

日期：2026-09-15  
状态：Accepted

决策：将 `SPIKE-INV008A-R6` 验收为 `PASS_CONTROLLED_OWNER_FAULT_RESTART_MATRIX` 并永久封存。接受两个 controlled synchronous owner fault 及其紧邻 normal-restart verification 的完整 PASS；结合 `SPIKE-INV007A-R3` 的 isolated direct progress update/save 证据，当前 transaction 候选已满足进入下一项独立 reconstruction control 的本阶段前置条件。批准全新 `SPIKE-001A2-R2`，旧 `SPIKE-001A2.md` 永久退役。

证据：四份 R6 terminal artifact 可解析且前驱 hash chain 闭合：setup fault 26,719 bytes / SHA-256 `6A665C06B04DC6A9B96BB98C09A8E903FA46AA95F8ED672A09C67E9C2A719F7E`；setup restart 14,045 bytes / `2FD08866A1F897E4F5905E57E0A5C64C036A3C006A2BB3841C4D9F91ABF9C205`；post-launch fault 27,848 bytes / `23B6D9B7A0CC34B2C178F120F5BBDF4D4AE89A4CA2709891A3B33AEF4EC6B2DE`；post-launch restart 14,057 bytes / `23B7A3C9868829C889F052A40C7AA9A6C8AD9F6354FFF984C9515C2231F8B3AD`。两个 fault phase 的 owner throw/catch/finally、phase-aware cleanup、manager/scene/menu restoration、保护与 Continue 均通过；两个 restart phase 只读且无 override/candidate/run。全部业务计数为零。完整独立复核见 `evidence/spike-inv008a/project-lead-r6-final-review.md`。

解释边界：本 PASS 不证明强制终止、原生崩溃、断电、自然胜利、完整 replay dispatch、地图/房间/奖励 side channel、重建一致性或正式 MVP 可发布性。强制 crash 继续延期；008A 不得继续运行或恢复 recovery。

001A2-R2 规则：固定使用 profile1，不读取或切换其他 profile。用户可在正式 baseline 前准备/替换测试 Continue；A 后只有原 checkpoint 内容精确不变才解锁 B，不实现 restore helper。B 必须通过主菜单正常 Continue，并把 production 已知的 `NumReloads +1` save 作为窄白名单差异审计，其他变化 fail closed。只允许一个有效 A/B compare，不出牌、不领奖、不 capture/dispatch。Implementation Codex 获得正式 baseline 前最多两次 pre-business harness 修订权；一旦进入受测窗口即停止自动修补权限。

后续关系：`SPIKE-001A2-R2` 由新的 Implementation Codex task 执行，以隔离 008A 的复杂 probe lineage。只有 A2-R2 经 Project Lead 验收后，才决定是否解锁 `SPIKE-001B`。

## D-053：社区 Modding Tutorials 采用为参考索引，不改变当前依赖

日期：2026-09-16  
状态：Accepted

决策：将本地 `SlayTheSpire2ModdingTutorials-master.zip` 分类为 `ADOPT_AS_REFERENCE_INDEX / NO_CURRENT_DEPENDENCY_CHANGE`。采用其 migration ledger 与 Hook Trigger Order 作为后续静态调查入口；将 RitsuLib lifecycle events、Node Attachments、config 与 toast 提升为 recorder observer/UI 外围候选，但不据此把 RitsuLib 变为 MVP 必要依赖，也不修改正在运行的 `SPIKE-001A2-R2`。

证据与边界：归档为11,282,845 bytes，SHA-256 `1957F102973BCA1F8F089D8F25C0D70F31220091371CE33657F000D732083721`，499 entries；对应公开仓库为 `GlitchedReme/SlayTheSpire2ModdingTutorials`。归档无 commit 元数据和 LICENSE，教程自述为 public-beta/WIP，并提示 API 频繁变化；Hook Trigger Order 又明确由 AI 整理并审阅。因此任何影响实现或验收的条目仍须在固定 v0.111.0 DLL、固定 RitsuLib 源码或运行实验中复核，不复制授权不明代码。

技术影响：若 A2-R2 通过，在签发 001B 前优先用教程形成最小 capture coverage checklist，并把 vanilla hook、RunReplays hook 与 RitsuLib v0.5.18 lifecycle surface 做窄静态对照。只有对照证明 RitsuLib 显著减少 patch 面、payload 足以表达玩家意图且性能风险可控时，才重新作依赖决策。若 A2-R2 失败，继续优先解决 reconstruction；教程不改变 blocker。

完整评估：`project-management/investigation-reports/REF-002-community-modding-tutorials-review.md`。

## D-054：A2-R2 为 harness observability failure，先执行 INV-010

日期：2026-09-16  
状态：Accepted

决策：接受 `SPIKE-001A2-R2` 已按 handoff 停止并永久退役，Project Lead 分类为 `ACCEPT_STOP / INCONCLUSIVE_HARNESS_OBSERVABILITY_FAILURE`。保留 Implementation 报告中的 operator-level `FAIL_ISOLATION_OR_PROTOCOL` 作为当轮停止桶，但不把它解释为 reconstruction diff、save isolation leak 或 native checkpoint 路线失败。A/B 有效 compare 数为0，`SPIKE-001B`继续阻塞。下一任务只批准纯静态 `INV-010`。

证据：R3完整日志186,853 bytes / SHA-256 `175C68C3E068CC781C120CCF0D5B2870E2F2530856B07DB27500E673B77AB5D0`；arm 6,841 bytes / `6308DA190E1663A023C2D9BC38C386308C3AE34F24B69C86FDC1A67759A7E197`。日志确认exact mod/profile环境、pre-baseline arm、节点点击后的50,627-byte production save及`SHRINKER_BEETLE_WEAK` ActiveCombat创建；没有baseline、A、B或R3 terminal。源码确认capture只由`ProbeTicker._Process`驱动，`TryReadiness`将所有exception静默转为null，失败帧不记录raw字段，故无法区分ticker lifetime、字段异常、恒false predicate或quiet-frame重置。

安全解释：没有正式 protection baseline，不能判定保护PASS或FAIL。可见current run/backup/native replay变化发生在fixture/pre-baseline或正常退出阶段；没有证据支持越界profile访问或已确认持久损坏。旧probe不得修改、重跑、restore或rebaseline。

设计修订：下一契约必须把room-before checkpoint写入完成与A snapshot readiness分离。baseline应在production checkpoint可验证后立即封存，而不是等到input-ready；capture driver必须有heartbeat，逐字段rejected-state与exception telemetry，并在进入目标阶段后有界地产生terminal artifact。等待用户点击的Ready阶段仍不得超时。

下一关系：INV-010只能给出全新reconstruction control、diagnostic-only或retire native control中的一个建议。未经Project Lead后续决策不得创建runtime probe或进入001B。完整复核见`evidence/spike-001a2-r2/project-lead-final-review.md`。

## D-055：接受 INV-010，签发全新 SPIKE-001A3 reconstruction control

日期：2026-09-17  
状态：Accepted

决策：接受 INV-010 的 `READY_NEW_RECONSTRUCTION_CONTROL`。固定 DLL 已给出足够的时序与方法体证据，可以在 production `SaveRun(null,true)` 原 Task完成后、把completion交还`EnterMapPointInternal`之前，stable-read并验证`PreFinishedRoom=null` checkpoint，同时原子封存profile1 protection baseline；不再把baseline推迟到input-ready。签发全新identity/schema的`SPIKE-001A3`，旧`SPIKE001A2R2`继续永久退役。

证据：INV-010 report SHA-256 `59D52227F5146253CEF41C5E93EDBC5AC4195EEE754CD4DCE6F39000F5FC61E0`；contract `54D99A392E5B113E78F770984100549E486A19D06145469E1D22BC54F1B02CF1`；IL output `C2EE5545BC51DF2A63CB815426BAC417955046877F3B41523AB76C444037F7A4`。Project Lead重跑synthetic contract tests为20/20 PASS。完整复核见`evidence/inv-010/project-lead-review.md`。

新契约：ticker直接挂`NGame`、显式Always并记录生命周期heartbeat；readiness逐字段记录value/null/exception，使用executor not-running/not-paused、current action null、current run node、稳定target identity及30个连续process frames；checkpoint completion后启动不依赖ticker的30秒deadline，即使ticker停止也必须生成bounded terminal。人工arm等待不超时。

修订权限：Implementation Codex可在同一A3 task/lineage内进行最多两次pre-business窄harness code revision，覆盖precheck、ticker/deadline、telemetry、artifact writer及尚未命中匹配save completion时的observer实现缺陷。匹配save completion被观察、baseline/recovery建立、A capture开始、保护差异或结果不明后，权限立即终止；不得用修订改变场景、保护集合、允许I/O或PASS标准。

后续关系：A3只比较一个第一幕普通弱怪的uninterrupted A与normal Continue B。只有形成有效terminal并经Project Lead验收，才复审`SPIKE-001B`；A3 PASS不自动解锁capture/dispatch，也不证明正式Replay session isolation、更多场景、性能或强记录。

## D-056：A3为checkpoint identity harness failure，先静态拆分floor语义

日期：2026-09-17  
状态：Accepted

决策：将`SPIKE-001A3`验收为`ACCEPT_STOP / INCONCLUSIVE_HARNESS_CHECKPOINT_IDENTITY_FAILURE`并永久退役。接受matching production `SaveRun(null,true)`已命中，以及captured checkpoint的seed、character、act、`PreFinishedRoom=null`与map coord匹配；不接受`BLOCKED_CHECKPOINT_VALIDATION`为production reconstruction或isolation失败。正式baseline、recovery、A/B snapshot均不存在，有效compare为0，`SPIKE-001B`继续阻塞。

根因：R2 arm把`TargetIdentity.TotalFloor`设为`-1`，`ValidateTarget`在captured floor绑定前先要求与`-1`相等，故合法checkpoint必然失败。terminal后异常又被重新抛回`MoveToMapCoordAction`，日志记录AggregateException并伴随用户观察到黑屏；observer-only failure不应改变production continuation。

设计风险：固定DLL的`RunState.TotalFloor`由`MapPointHistory`计数派生，而`EnterMapPointInternal`在room-before save后才append目标history。因此checkpoint `FloorReached`与目标战斗readiness的`TotalFloor`可能是不同语义。不得只交换两行代码或把`-1`猜成某个值后直接运行。

下一关系：只批准纯静态`INV-011`，追踪arm/checkpoint/A/B四个floor、normal Continue恢复、target字段绑定与observer异常分支，并审计所有placeholder/validation ordering。INV-011给出可执行契约前不得创建successor probe；A3不得修改、重跑、restore或rebaseline。完整复核见`evidence/spike-001a3/project-lead-terminal-review.md`。

## D-057：接受INV-011，签发typed target的SPIKE-001A4

日期：2026-09-17  
状态：Accepted

决策：接受INV-011的`READY_SUCCESSOR_RECONSTRUCTION_CONTROL`。固定floor关系为`CheckpointFloorReached = ArmPredecessorFloor`，A/B playable `TotalFloor = CheckpointFloorReached + 1`；`ActFloor=row+1`为独立act内语义。签发全新`SPIKE001A4`，A3保持永久sealed。

证据：report `7BE386861AC14433F4C61FB42A93C3C9879AF6B2B7B0E42D6463C2F50CAF2DBB`；contract `1527728A8481D98D357D4484D7A3269153F2F9A918B2657A02A3C9096511CFB7`；placeholder audit `C5CF56B9C11CF4D43893ADE5E4C5E73AD9F0DDBEDAD262787AE5F6C609258843`；IL output `E6C4FE313C367651160F545DF2C7A48716A03FF86912153AFF266F5267C17FCC`。Project Lead重跑40/40 tests并重新生成相同hash的2,307行IL输出。复核见`evidence/inv-011/project-lead-review.md`。

契约修订：target拆为arm-known、checkpoint-unbound/bound、A-bound room/encounter和phase-local object identities。禁止`-1`、empty、default coord、magic null text或fallback值作为已观察事实参与PASS。A/B使用独立semantic projection。production success后的observer-only failure必须封存terminal但保持wrapper RanToCompletion；production自身fault/cancel保持原语义。

后续关系：SPIKE-001A4只允许一个有效A/B compare。只有其形成有效terminal并经Project Lead验收，才复审`SPIKE-001B`；PASS不自动解锁capture/dispatch。

## D-058：建立workspace外的本地反编译导航缓存

日期：2026-09-17  
状态：Accepted

决策：批准non-blocking`TOOL-001`，为固定v0.111.0 `sts2.dll`建立hash-gated、版本隔离、可重复生成的本地反编译缓存。反编译C#只用于搜索与导航；原始DLL和可复现IL继续作为正式证据。

边界：生成结果必须位于`C:\Projects\STS2ReplayMod-local\decompiled\`，不得进入workspace、evidence、references或模组源码；每个cache记录DLL/tool/command identity与non-authoritative声明。禁止修改、编译、执行或分发反编译源码。任务不读取存档、不启动游戏，不阻塞或解锁任何INV/SPIKE gate。

后续关系：TOOL-001可与SPIKE-001A4 Phase 0并行，由独立Implementation Codex task执行；它不能改变A4的固定证据或验收标准。

## D-059：接受A4后基线harness失败，先做纯静态INV-012

日期：2026-09-25  
状态：Accepted

决策：`SPIKE-001A4`分类为`ACCEPT_STOP / INCONCLUSIVE_POST_BASELINE_HARNESS_FAILURE`并永久退役。唯一r2业务attempt已封存正式room-before checkpoint baseline/recovery，但没有A snapshot、B或有效compare。`FAIL_HARNESS_AFTER_BASELINE / attempt.resume`不代表原生重建不等价。源码确认baseline seal后提前完成capture deadline且expiry在baseline存在时返回；无A的动态首因尚未确认。后续resume进程terminal中的zero-tick telemetry不得当作原业务进程的ticker证据。

保护边界：退出后`current_run.save`与checkpoint raw bytes相同，但`replays/latest.mcr`和`settings.save`内容变化，故完整protection未通过。这些文件的本次writer/timing尚未证明，不得事后加入白名单。A4已在下一独立进程中证明持久disabled；该cleanup-only进程进入profile3进行gameplay/write违反no-run协议，但A4未加载，不能据此重分类其已封存业务终态或外推其他profile安全性。不读取、修复或恢复profile3。A4安装pair保留disabled，禁止重跑、修改、restore/rebaseline、Phase B。

后续关系：只批准纯静态`INV-012`，形成deadline、telemetry和退出写入/protection的下一合同或更窄诊断。`SPIKE-001B`继续阻塞；INV-012验收前不得创建下一runtime probe。证据见`evidence/spike-001a4/project-lead-final-review.md`。

## D-060：验收TOOL-001为非权威导航缓存

日期：2026-09-25  
状态：Accepted

决策：接受`TOOL-001`交付的workspace外、按固定DLL身份隔离的本地反编译缓存。Project Lead核实manifest为v0.111.0/`41cef1ea`/固定SHA，`nonAuthoritative=true`，源码目录有3,538个C#文件。只作搜索和定位，不作为方法体或行为结论的独立权威；正式技术结论仍引用原DLL及hash-gated可复现IL。该工具不改变任何实验gate。

## D-061：先固定存储语义与无头边界，再开展INV-012

日期：2026-09-27  
状态：Accepted

决策：接受用户对过去存储/写入假设的质疑。A4保护路径是通过production `UserDataPathProvider`和`ProjectSettings.GlobalizePath`计算，不是随意猜目录；问题主要在把不同作用域及不同时间窗口的文件都纳入同一raw-hash恒等oracle。固定v0.111.0的非权威导航源码显示：`settings.save`是account级本机文件，`profile.save`位于modded account层，profile1下另有run/progress/prefs与原生`replays/latest.mcr`；`NGame.Quit`保存Settings，`NRun`退出可走`RunManager.CleanUp → WriteReplay`。A4具体内容变化的字段、调用时点和因果关系仍为Unknown，不事后放宽A4判定。

无头策略：优先不启动游戏的.NET/Mock/fixture测试；独立Godot `--headless`工程是节点生命周期的候选，但不证明正式游戏行为。正式STS2 `--headless`仍会进入游戏启动/存档/Steam边界，不能当作隔离。固定release导航源码中的`autoslay`入口要求`!IsReleaseGame()`，而当前DLL该方法返回true，故不将内置AutoSlayer当成现成正式版测试CLI。`TEST_PROTOCOL.md`的禁止Codex自行启动正式STS2进程继续有效；如未来要改，须另行设计隔离并取得用户明确授权。

后续关系：修订现有纯静态`INV-012`，新增存储拓扑/写入表和无头可行性矩阵，不新建或运行probe。只有INV-012以固定DLL/IL验证后，Project Lead才决定后继runtime合同及是否进行安全的headless feasibility实验。`SPIKE-001B`仍阻塞。

## D-062：验收INV-012，只签发窄诊断离线Phase 0

日期：2026-09-27  
状态：Accepted

决策：接受`INV-012`的`READY_NARROW_DIAGNOSTIC`。固定DLL/IL支持account/modded/profile1存储拓扑、`NGame.Quit/SaveSettings`与`NRun.CleanUp/WriteReplay`的条件性writer，以及A4在baseline seal后取消唯一deadline的静态缺口；Project Lead独立重跑14/14合成合同测试。原业务进程是否tick、哪个readiness条件阻止A以及A4两项内容差异的精确调用方仍未知。A4保护FAIL与A/B有效compare为0保持原判，不追认任何内容变化。

下一任务：签发全新`SPIKE-INV012A`，当前只授权Phase 0离线源码、固定目标IL、observer-only Harmony patch审计、独立deadline/限量持久telemetry、合成/race测试、patch/unpatch smoke及未来人工运行草案。不得安装/启用probe、启动STS2（包括headless）、读取当前用户状态或直接请求游戏内测试；Phase 0通过后由Project Lead另行决定运行阶段。该诊断也不自动解锁后续reconstruction A/B或`SPIKE-001B`。验收见`evidence/inv-012/project-lead-review.md`。

Harmony定位：本地HarmonyLib 2.4.2.0的`[HarmonyPatch]`可作为方法级观察手段，不是存档隔离或活性保障机制。下一探针只读Prefix/Postfix，不修改生产参数、返回值、Task或异常；不以此预先决定正式产品架构。工具评估见`investigation-reports/REF-003-HarmonyPatch-assessment.md`。

## D-063：接受INV012A r5离线证据，暂缓运行并要求同探针r6窄修

日期：2026-09-28  
状态：Accepted

决策：接受`SPIKE-INV012A` Phase 0 r5的离线交付证据，但**不验收为可安装/可运行**。Project Lead独立复核DLL/manifest SHA、Release 0 warning/error、28/28合成测试、9/9固定方法patch/unpatch、dummy non-interference和9目标/16回调的hash-gated IL；IL输出与r5封存文件同为47,969 bytes / SHA-256 `CF6BF14BF67648FE674083846EABAFB546F8304E2AA31324B00AF6B275B12F4F`。这些证据证明离线准备的主要路径成立，不证明真实Godot生命周期、退出writer或A/B。

运行前缺口：`DiagnosticCore.Start`可因stage-start证据失败先封存，而`SaveEntered/SaveCompleted`仍继续尝试写业务记录；`MainMenuReady`在loaded-set/profile身份门前写boot，门失败或`NGame.Instance`缺失时静默返回。两者分别损害terminal后证据顺序与prebusiness scope/可见性合同。授权同一`SPIKEINV012A`探针的Phase 0-R6离线窄修，保留r5候选及旧证据，不新建实验lineage；修正并补负向测试后再次交Project Lead。不得安装/启用probe、启动正式STS2或请求用户人工运行。完整复核与任务边界见`evidence/spike-inv012a/project-lead-phase0-review.md`、`handoffs/SPIKE-INV012A-Phase0-R6.md`。

后续关系：r6离线验收后才复审是否授权独立运行诊断；A4历史结论不变，`SPIKE-001B`继续阻塞。`.config/dotnet9`仅为空目录，不为清理执行递归删除。

## D-064：验收INV012A Phase 0-r6，先做只读运行前预检

日期：2026-09-28  
状态：Accepted

决策：接受`SPIKE-INV012A` Phase 0-r6为离线阶段完成。Project Lead核对r6 DLL 52,224 bytes / SHA-256 `CA1DAF85B5C12AE043A2BF20E7D9026DA894D336F649913A7E1D1BE2C69C1720`及未变的manifest/r5身份；独立重跑Release 0 warning/error、35/35合成合同、9/9精确目标patch/unpatch、dummy非干扰，以及固定IL、r6 delta IL和数据白名单审计，三份输出hash均与封存输出一致。r5的终态后追加记录与身份门前写attempt问题已按离线合同修复。该验收仅覆盖probe离线行为，不证明真实游戏生命周期、退出writer、存档保护或A/B。

下一任务：只批准`SPIKE-INV012A-RUNTIME-PREFLIGHT`，在r6尚未安装、无游戏进程时，以范围受限只读检查确定当前modded profile选择、退役probe/AscensionUnlocker持久启用状态、候选/manifest身份与三段保护窗口。A4 cleanup曾误入profile3，因此不从历史“固定profile1”推断当前选择。预检若遇不明路径/配置/Cloud状态，fail closed并交回Project Lead；不得自动切profile、修改设置、安装probe或启动游戏。预检验收后才决定是否签发独立安装/人工运行工作包。证据与边界见`evidence/spike-inv012a/project-lead-phase0-r6-review.md`、`handoffs/SPIKE-INV012A-RUNTIME-PREFLIGHT.md`。

`SPIKE-001B`继续阻塞；A4/r5证据仍封存，不追认退出内容变化。

## D-065：接受INV012A预检的配置阻断，先调查安全准备路线

日期：2026-09-28  
状态：Accepted

决策：接受`SPIKE-INV012A-RUNTIME-PREFLIGHT`的`BLOCKED_CONFIG`，不是探针代码FAIL。Project Lead独立以范围安全方式重读两个精确配置文件的过滤字段与hash，确认当前modded selector为profile3、`mods_enabled=true`、唯一持久enabled ID为`AscensionUnlocker`，且STS2进程数为0。r6身份有效但未安装；Steam Cloud当前状态仍Unknown。预检和复核均未访问其他profile或修改配置。

安全影响：固定v0.111.0 IL证实`GameStartup → InitProfileId(null)`从`profile.save.LastProfileId`选择当前profile，随后立即调用该profile的`InitProgressData/InitPrefsData`并建立run-history目录。故selector仍为3时，“先打开游戏到主菜单再切到1”至少会读取/初始化profile3，不能按既定边界默认为安全无害，也不能以历史Cloud-off/测试槽约定跳过当前配置门。当前不授权安装r6、启动游戏、切profile或直接编辑`profile.save`/`settings.save`。

下一任务只批准纯静态`INV-013`：建立经用户明确授权后可恢复的离线配置准备与一次特许UI准备两条路线的读写/备份/Cloud/停止合同，比较其风险；不执行变更。报告经Project Lead验收后再向用户请求具体选择与授权。证据见`evidence/spike-inv012a/preflight/project-lead-review.md`，任务见`handoffs/INV-013.md`。A4与`SPIKE-001B`状态不变。

## D-066：暂停INV-013配置准备任务，保护用户日常存档配置

日期：2026-09-28  
状态：Accepted；暂时覆盖D-065的“下一任务只批准INV-013”执行指令，不否定其只读预检事实

决策：项目发起人说明日常会在modded profile2/3及“普通profile1”游玩，进入modded profile时启用`AscensionUnlocker`。因此当前selector=profile3与该模组启用不能被当作需要自动清理的异常状态。`INV-013`原工作包暂停，不交Implementation Codex；不修改selector、settings、模组启用状态，也不以当前配置启动实验。modded profile2/3按个人日常槽保护，既有modded profile1专用测试槽约定暂保持但需用户确认。

待澄清：“普通profile1”属于unmodded还是modded存档树；modded profile1是否仍专供实验；未来测试中`AscensionUnlocker`是否必须保持启用。答复前不假定禁用该模组安全或必要。下一步仅澄清这些边界，并据此修订/替换静态工作包；`SPIKE-INV012A` runtime与`SPIKE-001B`仍未解锁。

## D-067：只将unmodded profile1列为个人进度保护目标，简化modded实验边界

日期：2026-09-28  
状态：Accepted；取代D-066的待澄清状态及其modded2/3保护假设，不追溯重判旧attempt

用户确认：“普通profile1”是不加载模组的profile1，正在用于个人连胜，须保护；modded profile1仍是专用测试槽，modded profile1/2/3均可供项目实验使用，模组启用/禁用可自由调整。它们虽也可能用于用户日常游玩，但不再作为必须无变化的个人存档保护对象。具体任务应仍声明目标modded槽、loaded-mod集合与必要的phase-local baseline，避免混淆准备变化、探针副作用和正常游戏写入。不得进入、读取、复制、修改或恢复unmodded profile1，除非用户以后另行授权。

由此退役未执行的`INV-013`：为禁止接触modded profile3而设计的离线selector/settings安全调查不再是前置任务。旧预检的selector=3、AscensionUnlocker enabled及r6身份门不匹配仍是当时事实，但`BLOCKED_CONFIG`不再表示当前日常配置不安全。用户允许切换modded槽与模组，所以先**保留已验收的R6候选**，只批准`handoffs/PREP-INV012A-R6.md`的简短人工准备和只读复核，不为适配当前profile3先造R7。准备通过后再独立验收安装与人工运行；不因用户放宽modded槽使用授权而追认A4旧保护FAIL、重跑退役probe或解锁`SPIKE-001B`。Cloud状态在准备/实际运行前均需用户现场确认。

## D-068：验收PREP，合并runtime步骤并扩大有限harness修复权限

日期：2026-10-09  
状态：Accepted；取代D-067的“准备后再分拆每个安装/配置步骤”，不追溯改写旧实验

确认：用户报告PREP完成；Project Lead只读检查modded selector=1、mods_enabled=true、AscensionUnlocker为唯一enabled、旧probe均disabled、游戏进程=0，固定游戏/Harmony/r6 DLL+manifest身份匹配且r6未安装。Cloud按用户完成报告记录，未机器独立验证，正式启动前仍现场确认。未读profile payload或触及unmodded profile1。验收见`investigation-reports/PREP-INV012A-R6-report.md`与过滤摘要。

问题判断：之前把可消耗测试槽与个人连胜进度混为一谈，使用过宽的跨窗口hash恒等要求；已定位的harness bug也反复触发退役/静态INV/新探针循环。许多离线测试证明局部规则，却未验证完整正向路径可达。主要责任在Project Lead的规约/任务切分，不应归咎于实现者遵守工作包。

变更：新签发`handoffs/SPIKE-INV012A-RUNTIME.md`合并r6安装、仅配置启动、正式窄诊断和至多两次已定位harness修复后的fresh-attempt重测。配置进程允许AscensionUnlocker+r6共存并得到不创建attempt的身份拒绝；正式诊断仍仅r6/modded1。新attempt可在必要正常游戏写入后建立自己的有效基线；不追认旧窗口差异，不覆盖证据、不复活旧probe。允许已建立实验状态后的明确harness修复，而非一律仅限pre-business。

边界：保护unmodded profile1，不自主启动/控制游戏，不改生产Task/行为，不改业务假设或降低验收标准；诊断不作isolation/crash/A/B证明。有限修复预算和退回条件明写，不无限循环。未来优先同一probe/milestone由同一实现对话负责，单一源码写入者，PM验收；不保证token必然下降。`WORKFLOW.md`与新版`TEST_PROTOCOL.md`为新任务的共享规则，旧封存结果继续按原合同。

下一步：runtime包`READY_TO_DISPATCH`，优先继续「完成 SPIKE-INV012A 工作包」。已只读确认对话工具可用，但用户仅提及能力，尚未明确授权发送消息，所以本轮未派发/未启动对话。得到可解释诊断后再审successor A/B control；INV-013不执行、SPIKE-001B保持BLOCKED。模型升级不替代固定DLL/IL/runtime证据。

## D-069：项目内Implementation消息协调的持续授权

日期：2026-10-09  
状态：Accepted；取代D-068当时“尚未获发送授权”的执行阻断

用户明确答复：“你直接执行就可以。以后都可以直接授权”。PM可直接向本项目已有Implementation对话下发工作包、请求进展、交回有限修订与验收反馈，不逐次索取发送许可。需保留工作包范围和派发记录，不扩展到其他项目、新建用户对话、新的个人数据权限或自行启动/控制游戏。

当前执行：已向已有「完成 SPIKE-INV012A 工作包」（thread `01a0e2d5-084e-76d2-ab5c-cee41644b0f7`，host `local`）发送 `SPIKE-INV012A-RUNTIME.md` 启动prompt，消息工具返回同一thread id确认接收。同一lineage继续，不新建对话、不改变模型设置。派发结果已写入CURRENT_STATUS与TODO；runtime/A/B结果仍待实测。记录见`evidence/spike-inv012a/runtime-dispatch-2026-10-09.md`。

## D-070：验收012A真实窄诊断，签发A5离线重建control

日期：2026-10-09  
状态：Accepted

确认：012A runtime-r1在用户手动fresh process中达到20字段30连续帧capture-ready，并记录CombatReplayWriter.WriteReplay与SettingsSaveManager.SaveSettings各entry/return=1/1。PM直接核对14份原始probe JSON的hash、完整白名单、同attempt/process/boot、strict monotonic顺序与唯一capture，并核对candidate/bin/live身份一致、旧r5/r6产物不变、游戏退出；用原有.NET9.0.20工具独立重跑38/38合同与dummy非干扰。验收为`ACCEPT_DIAGNOSTIC_PASS / NO_RECONSTRUCTION_CONCLUSION`，不声称A snapshot、A4历史根因、文件内容因果、隔离或crash。

修复：r6主菜单identity通过但0帧，已在现有repair envelope内改用SceneTree.ProcessFrame、GD.Print及实际首帧后PASS。不放宽readiness、不修改生产Task/参数；只使用一次已定位修复后的fresh重测。证明新观察设施可用，不需要再为相同harness问题另发INV。

收尾：runtime-r1仍安装/enabled时不能宣称已停用。授权实现者先不可覆盖归档两次指定attempt的probe自有JSON，再在游戏退出、精确hash匹配且workspace可恢复时移除本包安装的两个文件；不改settings/profile/save、不碰旧probe或证据。配置条目可能残留，不能把文件清理当作持久disabled证明。

下一任务：`handoffs/SPIKE-001A5.md`，新identity、同一实现对话，仅Phase0离线A/B reconstruction control准备与012A收尾。继承INV-011 floor/身份、INV-012数据窗口与已验证活性，删除无关的modded全文件恒等前置门。匹配保存调用若需要observer wrapper才能在房间创建前封存checkpoint，必须明确其Task identity改变且用真实Task证明production成功/fault/cancel及异常语义不被observer污染；该实验授权不追溯适用于012A。

候选/data contract/完整正向路径验收前不安装或运行A5。重建等价性仍Unknown，SPIKE-001B仍阻塞。用户说明实现对话模型已由gpt-6 sol high切到gpt-6.1 sol xhigh，作为用户报告记录；派发未覆盖该设置，也不据此重跑已验收证据。消息协调沿用D-069持续授权；本包已发送给同一Implementation对话，记录见`evidence/spike-001a5/dispatch-2026-10-09.md`。

## D-071：接受A5离线核心与012A收尾，窄修capture诊断

日期：2026-10-09  
状态：Accepted

确认：012A两个指定attempt的17份归档逐份hash匹配，两个精确安装文件已移除且workspace可恢复；settings启用条目未修改，不能据此宣称持久disabled或下次启动必为modded。A5 r1候选130,560 bytes / SHA-256 `1F4A6DEC9B50A361D70D7113453C544951FCA3BF6DB164B15175BA36A91AFC1B`，24份交付文件和10份源码身份通过。PM独立复测Release、99/99合同、4/4 patch、dummy与固定IL审计。完整正向链为synthetic，真实A/B仍为0。

分类：`ACCEPT_OFFLINE_CORE / REVISE_CAPTURE_DIAGNOSTICS / RUNTIME_HOLD`。r1虽然deadline有效，但失败帧状态未保留，terminal在Reset后写入导致最后frame/计数/字段样本丢失。授权在同一A5工作包做Phase0-r2，只补有界持久诊断和区分no-tick/never-ready的回归，不改变readiness/floor/Task/语义门，不新增INV或probe ID。保留r1封存候选/报告/证据；实现者可直接窄修源码，不必逐次请求编辑许可。

运行：本次不授权配置写入、安装、玩家状态读取或游戏运行。r2验收后合并这些步骤与人工A/B，并在首次启动前证明真实有效modded配置。持续协调授权按D-069执行；模型设置不覆盖。验收见`evidence/spike-001a5/project-lead-phase0-review-2026-10-09.md`，要求追加在`handoffs/SPIKE-001A5.md`第6节。重建等价性仍Unknown，SPIKE-001B保持BLOCKED。

## D-072：验收A5 r2并合并放行配置安装与人工A/B

日期：2026-10-09  
状态：Accepted；取代D-071的A5 RUNTIME_HOLD，保留r1离线结论和旧证据

确认：PM独立重跑Release 0 warning/error、122/122合同、4/4 patch、真实Harmony dummy与增量审计；331份r2交付文件、12源码、r1旧交付24/24和11份源码/manifest存档身份通过。r2 sealed pair与独立build一致，DLL为143360 bytes / SHA-256 `8DFB0C4AE6393F61D178EBA7A380690F0574BBE27C69246BB53B214306C5BF09`。持久no-tick/never-ready判别、最后字段/计数冻结、限频诊断和终态/写失败回归成立，readiness/floor/Task/语义标准未放宽。分类为`ACCEPT_PHASE0_R2 / AUTHORIZE_MANUAL_AB`，实际A/B仍为0。

变更：原handoff第7节合并离线mod设置/selector准备、精确安装、用户手动A、退出后exact-byte核对和fresh正常Continue B。用户已允许自由调整mod开关及使用modded槽，故范围明确的配置准备不再另发调查；仅允许mod字段与modded last_profile_id，完整配置备份留private，其他设置不改。首次启动前必须有真实有效modded1/only-A5配置，不能先零mod启动再启用或依赖已移除012A的enabled残留。Cloud每次由用户现场确认。

实现者可直接完成上述步骤并分段请求用户测试；运行预算为首次完整A/B加2次证据明确的harness窄修后fresh完整A/B。基线后修复可用，但不变假设/标准/I/O，不恢复native，不混用失败A与新B，旧证据不覆盖。真实差异也有价值，得到一个有效答案即停止；未知production副作用、source变更、个人槽风险或预算用尽交PM。收尾只可精确清理本包可恢复安装pair或禁用，旧probe及证据保留。

当前验收时游戏进程=0、A5未安装。协调按D-069向同一Implementation对话发送，不创建新对话或覆盖模型。验收见`evidence/spike-001a5/project-lead-phase0-r2-review-2026-10-09.md`。正式产品Continue隔离目标不降低，SPIKE-001B不自动解锁。

## D-073：记录用户中断并保持原账号范围续跑A5

日期：2026-10-21  
状态：Accepted；澄清D-072的同包续跑，不重置预算或改写旧证据

确认：r2 attempt `861af3a9e6d449acbd192b0a8d3c2f56`到达only-A5/modded1/Ready及Arm，用户在目标输入前要求正常退出，随后要求继续。只有Arm/active，无seal/A capture/complete；分类为`USER_INTERRUPTED_BEFORE_TARGET / NO_VALID_AB`，不是production或harness失败。PM核对公开Arm/事件/pointer共5份hash。已Arm正式记录1/3、有效比较0；用户中断不能虚报成harness修复，新revision不能重置总数。

续跑：接受同ID/schema2的runtime-r1 identity namespace和独立源码副本，保留原r2全部331文件及旧private active。PM核对326文件/live pair，并独立复测125/125、4/4、dummy/138引用和889个探针方法identity-only IL通过。DLL hash为`AB97852918D5291B5D38E4D80C199C38A71F154A94EC982F5CE70510ACB52596`，仅两处identity字符串变更，业务标准不变。当前已安装，复核时游戏进程0。

账号：新增第二账号目录使准备停在配置前。用户在PM对话表示另一份由其自行删除相关配置；不解释为要求Codex删除，也不认为已删完或已切回原账号。另一份排除在任务外，不检查/改动其配置/payload，不要求先删目录。原A5账号由当前Steam ActiveUser、唯一匹配目录及旧probe scope交叉确定；若仍有必要的登录确认留在原实现对话，一致后依D-072继续同包，不按最近日志或mtime猜目标，不新建INV或审批。

后续状态：原对话已收到用户明确确认原账号且本轮不切换，三重匹配与配置回读通过；公开启动前收据`runtime/prelaunch-A2-runtime-r1-2026-10-21.json`放行fresh主菜单预检，不提前放行Continue/建局/F8。PM读取公开收据同步，不另行读取账号配置；另一份目录仍无需处理。

下一步仍是原账号modded1/only-A5配置与fresh完整A/B；旧Arm不拼新B，Cloud/实际loaded等按新进程重验，SPIKE-001B不自动解锁。复核及已发送的实现者反馈见`evidence/spike-001a5/project-lead-progress-review-2026-10-21.md`。PM仅写文档/离线复核，未操作游戏、配置或任一账号数据。

## D-074：验收首个实际room-before A/B并进入001B离线准备

日期：2026-10-22  
状态：Accepted

确认：A5有效attempt `3e6aaa0160e441c293b5ac4a0030b6d0`在固定v0.111.0/41cef1ea、原账号modded1/only-A5、Standard Ironclad A0、seed JFZS2H4V4XN2、CORPSE_SLUGS_WEAK入口取得一个实际等价A/B。seal Floor1/null-room→playable Floor2/round1；same CP输入链、fresh B、两端20字段/30帧/max gap34ms、A.complete与B compare-complete及无terminal成立。PM独立读取原始公开记录，8088-byte A/B semantic逐字节及JSON相同、Differences=[]，405份交付及旧24/331/326份索引保留通过。分类`PASS_ROOM_BEFORE_RECONSTRUCTION_CONTROL`，不是synthetic，不覆盖其他内容/之后轨迹或完整Replay。

收尾：PM检查进程0、两个精确A5安装文件不存在、18份其他旧probe不变。持久disabled/29 installed manifests均disabled、私有备份/active保留以绑定收据为据，未重读账号配置/private raw；下次启动预计unmodded，不启动验证，也不声称验证个人槽安全。记录2/3即停，不追加第三轮。验收/只读脚本见`evidence/spike-001a5/project-lead-runtime-review-2026-10-22.md`。

阶段变化：仅将该样本首操作点冻结投影的重建等价从Unknown改为Confirmed，解除001B的reconstruction前置阻塞。批准`handoffs/SPIKE-001B.md` Phase0：一张无选择/非胜利普通卡和一次EndTurn的accepted capture、两次正常API分派、初始/出牌后/next-round三点比较。继续普通Continue作测试槽control，不当成最终Replay架构，不同时扩张isolated session/纠偏/seek/UI。新API窄核对、离线合同与候选放同包；现在不安装/读当前玩家状态/启动游戏，候选经PM验收后才放行运行。继续原实现对话，不另建INV/对话，不覆盖模型。

证据修正：10月21日某旧日志的user://逻辑路径被当绝对路径prefix比较，false不证明另一账号；此前该归属只按实现端报告记录，现在该旧比较的归属证据记为Unknown。实际A/B进程后来通过account token/ActiveUser/旧probe scope重新核对，结论不依赖旧推断。另一账号仍不纳入任务，不读取或删除。旧A2/A3/A4判定不改，性能/原始节奏/完整隔离等仍未验证。

执行：001B Phase0已直接发送给原Implementation对话，工具确认接收；未新建对话或覆盖模型。派发记录`evidence/spike-001b/dispatch-2026-10-22.md`，新候选待交付，runtime仍HOLD。

## D-075：接受001B离线核心，窄修动作阶段可观测性

日期：2026-10-22  
状态：Accepted；001B保持RUNTIME_HOLD，未授权运行

确认：PM独立核对001B r1交付951份及19份source等文件，sealed pair与独立build一致；Release0 warning/error、88/88合同、7/7实际patch、dummy、159引用/21新增及受限B效果调用审计通过。A5全部封存索引保留。accepted不是请求/CompletedTask，三点Round1/1/2、逐点比较后才分派、A完整commit/ReadyB及fresh输入等离线核心可接受；实际两操作轨迹未验证。DLL hash `D66F55B72C3A70D3AE897A478B62BD04ADC768A8F12D4A5987A0109E75286942`。

缺口：Pending的执行/效果/End-ready/完成状态仅在内存，observer契约拒绝常被归为统一`observer-InvalidDataException`，Action超时只有`ActionAcceptedAndEffectComplete`，无法从持久证据区分新边界停点。这是harness诊断缺口，不是production失败，也不要求新增研究或降成功标准。

决策：分类`ACCEPT_OFFLINE_CORE / REVISE_ACTION_DIAGNOSTICS / RUNTIME_HOLD`。原handoff末节授权同一001B Phase0-r2，复用已有回调冻结有限site/flags及受控code、补持久判别/写失败/竞态回归，不增加生产patch/API效果，不改accepted、round/floor/readiness/Task/投影或ReadyB门。补首玩家TurnNumber初始化/切换的窄IL说明，不将synthetic turn=1当实测。保留r1原951交付材料，可用独立源码工作副本保持可复现，不另建probe/INV/对话。

当前进程0、001B未安装，账号配置/玩家状态均未读写。r2交增量候选再审，不请求用户新游戏动作；后续配置/安装/人工运行仍在同包放行。验收：`evidence/spike-001b/project-lead-phase0-review-2026-10-22.md`。协调沿用D-069，不覆盖模型。

## D-076：验收001B r2并放行同包人工两操作轨迹

日期：2026-10-22  
状态：Accepted；取代D-075的运行HOLD，保留原r1/r2封存材料

确认：PM独立核对r2交付2137份、21份source等身份、sealed与独立build一致；Release0 warning/error、109/109、7/7、两套真实Harmony dummy、159引用/无新生产patch或效果调用以及业务保留审计通过。r1 951和A5 405/24/331/326目标不变。四个持久动作停点可区分，Task是最后已观察状态；可信code使用私有标记，外来同名message不能伪造。PlayerTurn构造初值1/Increment/SwitchSides窄IL补齐，未强填runtime值。分类`ACCEPT_PHASE0_R2 / AUTHORIZE_TWO_ACTION_RUNTIME`。

候选DLL200192 bytes / SHA-256 `F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7`，manifest367 bytes / SHA-256 `22F4DAFF17705B748B69C6EB16EA089D361D868AA373266CF8F3C185A6AFDD65`，同ID/schema2/private phase0-r2。验收时未安装、进程0、业务attempt0；真实轨迹仍Unknown。

授权：原handoff最新runtime节合并原账号mod设置/selector准备、精确安装、用户手动A一张Defend和一次EndTurn、A完整提交/退出后same CP、fresh正常Continue B自动两步及三点比较。原账号通过当前ActiveUser/既有scope确认；另一账号/个人unmodded1不读取或改写，raw数据只private。安装/配置/实际loaded分验，每次Cloud现场确认，不自动游戏/正式headless。

完整PASS须三点eq、两端各两条accepted且完成。初始diff只有重建差异、操作0；post-card diff仅局部轨迹且不得继续End，均明示覆盖点后停，不追PASS。最多3条已Arm记录，准许有据harness-only修复及明确中断/不合格fixture的fresh重准备；旧证据不覆盖、不跨candidate拼A/B、不restore native、不降标准。未知副作用/输入变化/风险/预算用尽交PM。收尾只禁用及精确清理可恢复本包pair，不动旧证据。A5保持关闭，不自动解锁PERF/DET或产品能力。

验收：`evidence/spike-001b/r2/project-lead-review-2026-10-22.md`；运行合同为`handoffs/SPIKE-001B.md`最新runtime节，原对话继续、不覆盖模型。PM只维护文档/离线验收，不实现游戏操作。

执行：已直接发送原Implementation对话，工具确认接收；派发前2137份目标再次全通过。用户关于另一份存档不纳入任务的最新说明已明确转达，不读取/删除、不要求先清理。记录：`evidence/spike-001b/r2/runtime-dispatch-2026-10-22.md`。配置安装及真实轨迹仍待阶段收据。

## D-077：验收001B实际两操作轨迹并关闭运行

日期：2026-10-23，Asia/Shanghai  
状态：Accepted；001B CLOSED，取代D-076的在研运行状态

确认：唯一attempt `4f958fdc207f4b9ba83ede826848f9ed`、Arm1/3，原账号modded1/only B、Standard Ironclad A0、seed EBJVET20T859、SHRINKER_BEETLE_WEAK。A人工Defend＋EndTurn与fresh正常Continue B两操作实际accepted/完成均2；三稳定点Round1/1/2逐字节及JSON结构相同，Differences0。PM独立核对158份最终交付、92原始公开记录、六稳定窗口、事件/deadline/完整hash链及旧2137/951/405/24/331/326目标。A退出及B主菜单输入45122 bytes/同CP exact，之后B正常Continue的reload另分窗，未restore/rebaseline。runtime候选保持D-076已批r2，不重复无变化的离线合同。

收尾：game0、持久禁用、精确本包pair移除可恢复，18个其他项目安装文件保持；完整源码/private和公开证据保留。disabled记录补齐、外部nullable/BOM核验缺口及未加载Workshop manifest漂移各自记录，不改业务结论、不外推零写入或全环境不变。个人unmodded1/另一账号未触及，PM不重读账号/private核验。剩余预算不用，不再运行001B。

改变：最小证据由“初始点可重建”推进到“一个实际样本的两操作捕获/分派及三点等价”；Wave1窄正向链退出门满足，允许规划Wave2，但不宣称多次重复稳定性或正式Replay隔离完成。PM发现三点的RunRng/PlayerRng/当前敌人Rng完整投影均未变；不能据EndTurn/抽牌推定RNG消费路径已验证。

下一优先：DET-001先以固定DLL/公开fixture设计最小RNG推进正常路径及独立副本偏差检测，后续运行另签。DET只回答确定性边界/检测，恢复纠偏归STATE-001；PERF另设无诊断污染的对照，不直接把本包探针I/O当发布预算。没有自动解锁新游戏测试、导航或产品实现，不为已记录的外部工具问题新建INV。

验收：`evidence/spike-001b/project-lead-runtime-review-2026-10-23.md`；独立只读复现`Verify-ProjectLead-runtime.ps1`及PM validation同目录保存。Implementation报告/索引不改，权威状态由PM更新，原对话保持现有模型。

执行：关闭反馈已直接发送原Implementation对话，工具确认接收，不覆盖模型；没有派发新运行/实现任务。该反馈不要求再次修改sealed交付或回发消息。

## D-078：以首次自然重洗收紧DET-001离线准备

日期：2026-10-23  
状态：Accepted；Phase0离线授权，RUNTIME_HOLD

依据：用户经验将洗牌、随机生成卡牌列为常见随机行为，并认为弱怪适合维持多回合；其集合唯一决定洗牌顺序、印牌/洗牌使用不同RNG是推测，复制自身RNG未知。适应打击表面相同实例的差异作为卡身份风险，不先归因随机数。来源及原表述边界记INVESTIGATIONS最新节，不当作固定机制Confirmed。

决策：DET-001首选普通铁甲/第一幕弱怪的首次自然重洗，先固定DLL确认draw/reshuffle链、stream/owner、输入顺序/实例与RNG状态依赖及已有投影覆盖，再用公开快照独立副本验证偏差检测，提交最短有界A/B合同。不是穷尽随机系统，不把抽牌/新手牌直接当RNG推进；药水/印牌是后续候选，暂不要求用户roll，复制身份另记不扩本包。

授权`handoffs/DET-001.md`的离线Phase0：现有Implementation、固定程序集/公开fixture及新离线工具，允许直接修工具；不访问当前账号/private/log/profile、不配置/安装、不启动游戏或请求人工测试，不创建runtime probe候选、不改已封存001B/A5。READY_FIRST_RESHUFFLE_CONTRACT或具体NEEDS_SCOPE_REVISION交PM，后续实现/运行在同包另行放行，不新增INV拆每个步骤。恢复归STATE、PERF独立对照；产品范围未变。

执行：已直接发送原Implementation对话，工具确认接收，不覆盖模型/不创建新对话。派发`evidence/det-001/project-lead-dispatch-2026-10-23.md`，实际机制结论及离线验证待交付，不将玩家经验升级为Confirmed。

## D-079：验收DET机制检测并修订涅奥资格后构建离线候选

日期：2026-10-23  
状态：Accepted；OFFLINE_CANDIDATE_AUTHORIZED，RUNTIME_HOLD

确认：PM独立核对DET Phase0的51文件/6公开输入，独立Release0 warning/error、11检测/15synthetic流程案例通过，5份新固定IL同hash，原001B158/六fixture保持。自然重洗使用Run.Shuffle且原投影覆盖；draw抽空后的下一次抽牌请求触发，十卡排列为9次NextInt/9步kernel。普通十卡/每回合5抽且无额外影响时R3首次重洗是静态推导，不是真实DET运行。集合不构成无条件顺序保证，具体印牌/复制不外推。

现场修订：用户明确涅奥选项本身就是遗物，不拿遗物不可能，但保留初始十卡容易。撤销原草案仅燃烧之血资格，不沿用不可满足门；同包先由固定IL选择兼容涅奥遗物的最小白名单，不放开任意被动、不要求用户roll。原Phase0报告/合同/51文件封存，最新handoff取代冲突内容。

接受分类`ACCEPT_PHASE0_MECHANISM_AND_DETECTOR / REVISE_NEOW_ELIGIBILITY`，允许`handoffs/DET-001.md`最新节的独立DET001离线候选：五点/四操作、完整九步state oracle、真实资格读取、事件序号/非严格毫秒、局部token规范化及closed-generic/Task非干扰验证。Phase0粗布尔DTO不直接当runtime gate。新源码/evidence独立，不依赖旧probe运行DLL或改封存源；可直接修harness并补针对回归，不另建INV或通用框架。

边界：不读当前账号/private/log/profile，不配置enabled/live mods、不安装或请求游戏；正式attempt0、实际重洗仍Unknown。候选经PM独立验收后同包另放行运行，STATE/PERF/产品未解锁。验收`evidence/det-001/project-lead-phase0-review-2026-10-23.md`及PM validation记录，原Implementation继续、模型不覆盖。

执行：离线候选阶段已直接派发原Implementation，工具确认接收；用户最新澄清与资格撤销已转达。记录`evidence/det-001/project-lead-candidate-dispatch-2026-10-23.md`，当前只确认派发，候选/白名单尚未交付。

## D-080：验收DET离线候选并保持本轮运行HOLD

日期：2026-10-23  
状态：Accepted；ACCEPT_OFFLINE_CANDIDATE / RUNTIME_HOLD

确认：73交付/32源身份与sealed同hash独立build通过，Release0 warning/error、68/68、11实际metadata patch/unpatch、18回调审计及同形真实Harmony dummy通过；14新增游戏引用只读，效果仍原三处正常B分派，原投影/SaveRun wrapper保持。旧DET51/6、001B158及索引保持。Neow metadata新生成仅JSON字序不同、结构相同，不是source/机制差异，无返修要求。

资格接受为恰有BurningBlood＋GoldenPearl，固定IL支持后者仅取得150金币且相关draw/shuffle hooks无额外效果，正常Neow被提供时可选并保留十卡；不保证每局提供，不要求当前roll。FTUE/实际牌组/遗物等现场资格仍须未来验证。完整九步state oracle、同毫秒序号/local token规范化、scope/Task/终态/分派边界的离线实现接受，但实际CardModel callback/R3 scope及真实两端重洗未知，不把metadata/dummy当production执行通过。

身份：DET001/manifest0.1.0/candidate-r1/schema1；DLL273408 bytes / SHA-256 `B83F8C3140EFF3CD1532A5C97D2C8E9D3E2AADDCD44F862E456E084AD04DE0CA`，manifest304 bytes / SHA-256 `187EAC0FDB0A65CC4CA2408092D872B17B7B8CEC44BFF598603635F6F0AF4681`；73文件索引 hash `B29827E8D509C255E7687963024FEFA2C11E16F56F447C4ABF05395F28DF2D33`。

本轮只完成离线验收收尾。game0、两个live文件不存在、正式attempt0；不签配置/安装、当前账号/private/log读取或人工游戏，不自动解锁下一phase。下一最有价值证据为恢复测试后的同包受控A/B，届时另签原账号/资格准备/Cloud/预算与精确安装。A5/001B不复活，STATE/PERF/产品未解锁。保留全部源码/产物/报告/证据，原对话模型不覆盖。

验收：`evidence/det-001/project-lead-candidate-review-2026-10-23.md`；独立输出和PM validation同目录索引保存，Implementation报告/交付保持封存。

执行：已直接发送原Implementation验收反馈，工具确认接收，不覆盖模型；无离线返修或新运行任务，继续保持本轮离线收尾边界。

## D-081：放行DET首次自然重洗的配置安装和分段人工A/B

日期：2026-10-23  
状态：Accepted；RUNTIME_AUTHORIZED / CONFIG_INSTALL_THEN_MANUAL_AB

依据D-069持续协调授权和D-080离线候选验收，在同包签发`handoffs/DET-001.md`最新运行节，不另建INV/对话。签发前只读Verify-Delivery再次通过73文件、旧DET51/6和001B158；未重跑无变化的68项。D-080的HOLD仅为上一阶段边界，本决策放行受限准备及人工运行，不代表安装或业务结果已完成。

范围：原测试账号/modded1/only DET001、固定candidate-r1及游戏身份；允许game0时范围明确的settings/selector备份和开关修改、精确pair安装，以及本包private和当前进程有界证据。每次用户确认Cloud关闭并手动启动，Codex不操作游戏。原账号由ActiveUser/既有scope匹配，不读另一账号或个人unmodded1。

流程：初次只准备一局GoldenPearl＋BurningBlood/原始十卡，资格不足先报PM不盲roll；A人工Defend/EndTurn/Defend/EndTurn，完整A/退出后same CP才fresh Continue B自动同四操作。五稳定点及E0…E4/两端真实九步state须完整成立，第一差异停止后续分派但不取消在途production。有效差异也是答案，不为PASS重测；normal Continue的reload/正常退出分窗，不声称零写入。

最多3条已Arm、同包已定位harness窄修/fresh完整重测，不能降低资格、readiness/CP、成功观察或RNG oracle；旧索引/候选/证据保留。未知production副作用/个人风险/输入改变/预算用尽交PM。收尾禁用及可恢复的本包精确pair清理获授权，不删任何证据。STATE/PERF/纠偏/产品未解锁，A5/001B不复活。当前formal attempt0，真实重洗/A-B仍Unknown，实际派发与配置安装另留收据。

执行：已直接发送原「完成 SPIKE-INV012A 工作包」对话，工具确认接收，不覆盖模型/新建对话。派发记录`evidence/det-001/project-lead-runtime-dispatch-2026-10-23.md`。安装/配置和实际运行仍待阶段收据，不因派发宣称完成。

## D-082：按用户现场要求核查并扩充兼容涅奥选项

日期：2026-10-23  
状态：Accepted；NEOW_SCOPE_REVISION / CANDIDATE_R3_PREPARATION

授权来源：PM直接读取原Implementation用户消息。用户先允许重开找金珍珠；随后因多次未出，要求考虑“华美发束、轰鸣海螺、熔岩石、飞鞋、钓鱼竿”。最新准备要求取代继续刷GoldenPearl的路线，不能继续拿D-081旧一局/GoldenPearl-only文字阻断，但也不是五项自动兼容。

决策：原Implementation可直接在同包固定DLL逐项核查并在独立candidate-r3纳入兼容精确子集，保留GoldenPearl，运行仅BurningBlood加一个已证实Neow选项。相关Release/资格正负向/patch/非干扰与白名单门通过、game0和精确可恢复旧live门后，延续D-081配置安装及分段人工运行，不再为相同范围重复请求批准。下一次只准备一局，缺允许项则报告，不无限roll。十卡、五点四操作、九步RNG、Task/CP/readiness、第一差异停止及Arm最多3条不变；未知/不兼容项不纳入。

r1共享泛型补丁故障和r2窄修作为D-081 repair envelope内报告记录，r2到达实际Neow不作目标重洗PASS。PM核对r2交付72文件/索引hash吻合，未独立重跑72项。r1/r2全部源/产物/索引/运行证据保留，A/B不得跨candidate拼接；当前formal Arm据实现者报告0，换装前现场核实。五项实际兼容矩阵及r3尚未交付，STATE/PERF/产品不解锁。

合同`handoffs/DET-001.md`文末D-082；用户消息定位/PM核对`evidence/det-001/project-lead-neow-scope-revision-2026-10-23.md`。PM不改实现者在研报告/源码，不承担本轮调查实现。

执行：已直接将D-082同步原Implementation，工具确认同threadId接收，明确无需再次批准相同核查/修订范围，不要求回发确认消息，不覆盖模型。尚无五项兼容矩阵或r3交付，不将范围同步当机制验收。

## D-083：验收r3精确涅奥资格和安装准备

日期：2026-10-23  
状态：Accepted；ACCEPT_R3_OFFLINE_AND_PREPARATION / CONTINUE_AUTHORIZED_RUNTIME

PM独立核对82交付、旧r2 72/r1 73/DET51/6/001B158保持；独立Release0 warning/error、144/144、11 metadata目标/15回调及1未变非泛型callsite、Harmony/utility回归通过，30源身份和same-hash build吻合，测试summary/audit逐字节一致。三份固定IL的53＋7＋7方法重新提取逐字节同hash，六类型metadata结构相同。

接受BurningBlood加恰好一个GoldenPearl、SilkenTress、BoomingConch、LavaRock、WingedBoots或FishingRod，仅本次直接普通弱怪/R3停手/不胜负领奖及精确未用状态窗口。schema2实际Neow计数/标志/完整序列化实例与CP/各点绑定，缺字段/未知/混合/用过/不符均拒绝。只新增5只读引用，r2观察绑定/原效果/Task/CP/readiness/五点四操作及九步oracle不变。静态窗口兼容不等于六项都已实际运行。

r3 DLL281600 bytes / SHA-256 `20A7E5BEB203E29D5A691086A33AAC7641B6BD4B16C2FB8D8C88DE935A4A8446`，manifest304 bytes / SHA-256 `187EAC0FDB0A65CC4CA2408092D872B17B7B8CEC44BFF598603635F6F0AF4681`；索引hash `DBF5CF55CAE072811E7C919B269D2A8C3ED50F98E79453E1DA67817CB43B7563`，owner/private candidate-r3/schema2。准备链11公开目标、live exact pair和其他18项目文件保持通过；账号/配置备份采用已绑定收据，PM未重读当前账号/private、未重放安装或启动游戏。

无返修要求或新增批准门，沿D-082/D-081的request-07 fresh主菜单、当前用户Cloud/原账号确认、实际only DET001/modded1/r3预检，再单局六选一/分段Arm和A/B。收据Arm0/3、预算不加，真实资格/CP/目标回调/重洗/A-B仍Unknown。报告/索引封存，STATE/PERF/产品不推进。验收`evidence/det-001/project-lead-candidate-r3-review-2026-10-23.md`，独立输出及只读复现入口同目录。

执行：验收反馈已直接发原Implementation，工具确认同threadId接收，未覆盖模型；如已发request-07则等待当前回传，不重复启动请求或安装。未派发其他任务/要求修改sealed交付或回发确认。

## D-084：按用户授权改为有限批次Neow准备

日期：2026-10-23  
状态：Accepted；R3_ACCEPTED / BOUNDED_NEOW_PREPARATION_BATCH

PM直接读取原Implementation用户消息：“我退出了。不能让我直接roll吗？现在这么多遗物还是很好roll的”。改为主菜单身份/当前Cloud等预检通过后，同一进程内最多5局正常UI新建Standard铁甲A0，六个已核准Neow选项任选一个，到地图停手；未出现则正常退出到主菜单再新建，不用主菜单Continue，不必每局关闭整个游戏或逐局审批。5局均无或异常则停报，不自动开启下一批。

这覆盖D-082/D-083单局即停准备条款，不改r3、六项限定资格或五点四操作/九步/Task/CP/readiness等标准。准备局数不计正式Arm，预算仍跨revision最多3条，当前公开inspection为0。无需新候选/重跑144/另建INV；设置正常化仍按原game0备份/窄补/回读授权，exact r3复用不重装。用户仍手动操作，其他mod、个人槽/另一账号排除，旧终态保留，STATE/PERF/产品不推进。

旧unexpected-continue已由用户确认实际点击Continue，属未Arm输入偏差/预期门拒绝；fresh635edc7caa37489f8a79bc15734fc0cf主菜单通过后无合格选项，属准备不足，后续game0/Arm0有公开inspection。均无目标重洗/A-B，不为此改probe。来源及现行操作合同`evidence/det-001/project-lead-preparation-batch-2026-10-23.md`、`handoffs/DET-001.md`文末。

执行：D-084已同步原Implementation，工具确认接收，明确不用再次批准相同批次权限，不覆盖模型，不要求回发确认。当前仅记录新准备权限，未将下一批视为已运行或成功。

## D-085：允许未Arm准备重启后的当前进程来源补验

日期：2026-10-23  
状态：Accepted；CONDITIONAL_PRE_ARM_PROCESS_RECOVERY

当前用户报告已选GoldenPearl到地图，但机器进程由31328/attempt1282373a69274578ac9ed62931ae031f变为58312:639283459045976987/attemptaf99c3b9c7bf411882d580c2f3565408。当前重建收据有自身candidate/process/actual only DET001/modded1/原scope及seq1/2启动证据，无Arm/seal/terminal/claim、正式0/3；PM只读核对两事件长度/hash及字段绑定。不能沿用旧外部主菜单gate，也不宣称D-084同进程流程完整遵守。

尚无正式比较窗口，允许补齐本次启动前原账号/Cloud关闭、正常新建非Continue、未节点/F8等关键用户来源，再按当前状态复核并封存before-Arm恢复收据后使用现有局开始全新A。用户澄清尚待回传，本决策不是已满足条件或当前F8放行。不得为补验改DLL/记录或降低实际资格/CP/Task/readiness；新Arm/CP/A/B只绑定当前attempt，不拼旧数据。

准备次数记约数/Unknown或超限偏差即可，不单凭次数销毁未Arm fixture，不追认≤5；本恢复不授权再roll或增加Arm预算。关键身份/Cloud/新建等不能确认、已有Arm/CP、terminal或未知生产副作用则停止并报告。实现者可对同类未Arm准备重启按此清单直接处理，无需逐次新任务/批准；不要求重装或重跑144。

合同`handoffs/DET-001.md`文末D-085；证据及尚缺条件`evidence/det-001/project-lead-pre-arm-process-recovery-2026-10-23.md`。实际资格/重洗/A-B仍Unknown，个人槽/另一账号排除，STATE/PERF/产品未推进。

执行：条件式恢复裁定已直接发原Implementation，工具确认接收；明确等已提出的用户澄清、关键项全过才F8，不要求退出重刷/新候选/重测或回发确认，不覆盖模型。未收到当前条件满足的恢复收据，不能宣布已进入A。

同日澄清：PM已读取用户新回复，确认当前启动前原账号/Cloud关闭、当前普通新建；次数及闪退阶段Unknown不再追问。旧1282进程有限日志收据有D3D12 device lost/HRESULT0x887a0005/DXGI_ERROR_DEVICE_REMOVED，与原precheck84313-byte前缀hash一致、追加320 bytes；PM只核对公开关联，未读raw。RootCause未知，Steam挤号因果未证，不证明旧进程零写入或crash recovery。

D-085排除项所指是当前进程故障或影响本次输入/状态的未知副作用，不是任意旧设备错误只要根因未知就无限阻断。旧renderer marker不单独阻断af99 fixture；用户条件已补齐，实现者当前状态最终复核/不可覆盖恢复收据通过后，可直接按原标准请求F8，不再请PM批准，不为旧marker开旁支调查/改probe/重测/重刷。当前恢复收据及实际业务仍待证，不预先宣布Arm或PASS。

执行补充：上述澄清已送达原Implementation，工具确认接收；明确直接完成当前最终复核后按门继续，不需要新增批准/任务或回发确认，未覆盖模型。

## D-086：条件授权输入诊断和精确Neow准备Continue离线修订

日期：2026-10-23  
状态：Resolved；R3_ARMED_1_OF_3 / R4_ROUTE_NOT_ACTIVATED

多次F8回传后仍仅启动事件，按键交付/_Input首因Unknown，不判资格或production失败。PM直接读到用户又关闭另一游戏补按，已通知先核对最新：已Arm继续r3，终态按实分类；仅仍未Arm且无seal/terminal时允许下列路线，不再重复试键。

允许用户正常保存退出保留当前Neow局，game0在原scope只读/私有绑定退出后稳定raw/DTO及退出差异，不restore。当前45473-byte/hash70A3842A…DE89摘要并非正式CP/FTUE或恢复证明。独立r4离线实现game-thread F8/聚焦只读、release/rising-edge/单owner有界观测，以及single-use exact准备源hash/DTO/candidate/scope绑定的准备Continue。只限无Arm/ReadyB准备态，正式B/原Task/CP路径保持，固定IL核查实际Neow/pre_finished/parent_event能否正常恢复地图。

本阶段只授权保存fixture和离线实现；新增来源分支需完整实际source正负向、Release/patch/非干扰/引用审计及PM一次增量验收，当前不安装r4/请求r4启动或Continue，不另建INV。正式A/B两端r4/新Arm-CP，准备源不是旧B CP；声明准备reload写入，不改native或直接调恢复API/全局关guard。Arm3/六项资格/五点四操作/九步/Task/readiness/首差异等不降，r3旧证据保持。

实际执行结果：实现者核对用户最新主动F8，原r3已产生arm.json/armed seq3/1771659ms/唯一activation0001，af99/process58312/candidate-r3及实际FTUE/原十卡/GoldenPearl等资格通过，正式1/3。PM独立核对三公开文件长度/hash及process/attempt/schema/candidate绑定和资格。目标direct Monster row1col0，CP/seal/S0/weak encounter待节点后记录，尚无A/B或目标重洗。

据本决策的“已Arm继续r3”前置分支，r4条件路线未激活，不实施/安装准备Continue，不正常退出重刷或再请求F8。上文条件草案保留但不是现行任务；不能把无输入历史时点当当前状态，也不认定关闭另一游戏是成功根因。源报告由实现者维护，当前无需回发确认或额外任务。合同`handoffs/DET-001.md`文末D-086，范围记录及PM first-arm校验同evidence/det-001，STATE/PERF/产品不推进。

## D-087：验收A端真实首次重洗采样并继续原fresh B门

日期：2026-10-23  
状态：Accepted；ACCEPT_A_CAPTURE_AND_REAL_FIRST_RESHUFFLE / B_PENDING

PM独立核对r3/af99 A的87公开记录身份、五点/四实际accepted-completed、63连续单调事件、20字段/30帧及complete/ReadyB hash链。真实E0…E4、原Shuffle/Draw Tasks成功、10实例成员/hook顺序及S3/E1、E2/S4/最终两堆链接通过。独立PowerShell BigInteger九步从实际E1得到E2完整四state，counter9→18，不依赖candidate ShuffleOracle.Advance或只看counter。只调用sealed detached DTO/Core，不读raw/private/native或engine/生产效果，不重跑离线144。

固定v0.111.0、only DET001/modded1、GoldenPearl＋BB、seed20FRCAL12Q8Y/SHRINKER_BEETLE_WEAK/row1col0，前驱Floor1→playable2。正式Arm1/3，CP hash `AD987823B7FBA7260542A6951DEB3594928EF717B0942E141B9956FB1AA80DD2`。A完成时同CP以实现者范围受限双读收据为据，PM未重读；退出后same CP、fresh B输入门和A/B仍待证，不标整包PASS。

沿原同包合同正常退出、game0同CP/必要配置/候选、fresh B当前原账号/Cloud/loaded/once-claim门，全部通过才Continue一次/完全停手、逐点相等后四操作。无需新增批准/人工动作重发；差异/源变化/终态照原处理，不restore/rebaseline或追PASS。r4路线不执行，个人槽/另一账号排除，STATE/PERF/产品未解锁。

验收`evidence/det-001/project-lead-A-complete-review-2026-10-23.md`、独立`project-lead-A-complete-validation-2026-10-23.json`及只读复现脚本；实现者已按原流程安排退出，无需回发确认或修改sealed交付。本决策只推进已授权B前门，不声称新运行/整包结果已完成。

## D-088：验收真实RNG重放单样本等价并仅推进收尾

日期：2026-10-24，Asia/Shanghai  
状态：Accepted；ACCEPT_SINGLE_SAMPLE_RNG_REPLAY_EQUIVALENCE / CLOSEOUT_PENDING

PM独立重验A87/B90公开记录；fresh B81240与A58312、same r3/af99/原CP。五份7545/7544/7584/7584/7583-byte full semantic逐字节/结构全等，五E边界规范化全等；两端四实际accepted/completed、B四dispatch与动作完整匹配、每次请求在前点capture后，63/59事件及readiness/完整commit链通过。两端E1/E2完整entry相同并匹配独立BigInteger九步，detached各端trace另验证state/counter/digest、成员/hook/S3-S4/最终两堆和原Shuffle/Draw成功，不仅counter+9。

输入门/当次Cloud/原账号/loaded/一次claim及A退出后same CP有hash绑定运行收据，PM不重读private/native。B normal Continue后native45864/hash8ACC6448…379F在已声明reload/save窗口，私有原CP/A保持，不要求之后native仍等于A，也不restore/rebaseline或称零写入。正式Arm1/3，不用剩余预算。

只完成原授权正常退出、game0持久禁用/精确本包可恢复pair回收、最终索引/证据和PM收尾验收；在此之前不标工作包完全关闭，不新增游戏测试、r4或其他实现任务。样本范围固定v0.111.0/普通铁甲十卡/GoldenPearl＋BB/SHRINKER_BEETLE_WEAK，不能外推任意内容/版本、多次稳定性、纠偏、seek、性能或完整产品隔离。STATE/PERF/产品未解锁。

验收`evidence/det-001/project-lead-B-complete-review-2026-10-24.md`及独立JSON/只读脚本；日期为本次PM复核日期，原运行交付不改。实现者已按原流程请求退出并安排收尾，无需重发动作/回发确认或重复批准。

## D-089：最终验收DET单样本RNG重放及收尾并关闭

日期：2026-10-24，Asia/Shanghai  
状态：Accepted；ACCEPT_SINGLE_SAMPLE_RNG_REPLAY_EQUIVALENCE / CLOSED

PM执行公开只读Verify-RuntimeDelivery通过：最终331文件长度/hash与索引一致，18旧索引列示4657目标及6输入保持；重验A87/B90＋activation1、五点全等、两端各四actual成功/B四dispatch、五规范化边界及完整九步/原Task/成员/牌堆链，63/59连续单调事件和commit完整。最终索引SHA-256 `C89647402FF43D777F8778137B7D64C71021593280F39F7CC98F171F4D412E38`。未重建/重跑144，未读当前账号/native/private或控制游戏，178 private副本状态依已绑定收据。

closeout e8dd68e34a294cce9bfdde1e54084531最终通过：PM直接核对game0、本包两精确安装文件不存在、其他18项目文件保持；workspace sealed可恢复，所有源/备份/原始/失败证据保留。持久禁用/非probe原flag窄还原/selector1未写、剩余29 installed有效启用0依范围收据。首次只读文件移除IOException时配置已禁用，Finish只LiteralPath/Force精确回收两文件，不重放配置；独立Verify及最终复现通过。

A退出/B配置/fresh B Continue前原CP45534/hashAD9878…0DD2保持；B声明reload/save窗口NumReloads0→1/native45864/hash8ACC64…379F，B结束/退出/离线收尾不变、private原CP保持。不声称零写入或所有330-byte差异只该字段，不restore/rebaseline。普通Continue只是测试control，个人unmodded1/另一账号未访问；下次预计unmodded仅配置推断，不额外启动验证。

正式Arm1/3，剩余不用，DET001和历史probe不再运行；r4未实施，不要求修改sealed交付或再开测试。本次只确认固定环境/GoldenPearl/原十卡/弱甲虫一个真实RNG消费样本，其他内容/重复稳定性、未投影状态、纠偏、seek、性能及独立会话仍未证，阶段1不完成。CURRENT_STATUS/TODO收束为最终事实，旧过程由D-081…D-088及sealed报告保留。

下一优先规划PERF-001离线测量合同，区分无探针/空观察/候选记录及空闲/地图/卡牌的hitch/分配/序列化/I/O；本决策只排优先级，未派发/批准新实现或运行，STATE/NAV/产品不自动解锁。最终验收`evidence/det-001/project-lead-runtime-review-2026-10-24.md`及PM validation同目录；Implementation报告/索引保持封存，原对话模型不覆盖。

执行：最终关闭反馈已直接发送原Implementation，工具确认同threadId接收，未覆盖模型或新建对话；明确不修改sealed交付/回发确认或追加测试，没有派发新任务。

## D-090：恢复工作并启动PERF离线测量准备

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；PERF-001 Phase0 authorized / GAME_RUNTIME_HOLD

用户明确「好。你继续吧」，结束10月24日暂停，并报告modded侧已切profile1、所有模组关闭。只记录用户报告，不推定持久/actual loaded、Cloud、版本或进程门通过；不读取当前玩家数据确认离线任务不需要的信息。个人unmodded1与另一账号始终排除，用户正常游玩后的测试槽不要求符合旧基线，不restore旧档。

DET-001按D-089关闭，不运行旧probe或消耗剩余Arm。签发 `handoffs/PERF-001.md`，延用原Implementation「完成 SPIKE-INV012A 工作包」`01a0e2d5-084e-76d2-ab5c-cee41644b0f7`，不覆盖模型。允许workspace中定义共同测量壳/空观察/候选记录三模式、分解idle/map/card的frame间隔/分配/采集/序列化/I/O，实施最小统计测试与公开DET DTO的有界 .NET 管线代理实验，交测量合同、原始结果和人工pilot草案。离线harness可自行窄修，原输入和sealed证据保持，不新建泛化INV或用户对话。

未来对照须保持同modded上下文，不能以关闭所有模组启动而切入个人存档树。没有真正零插桩基线时明确共同设施成本，不冒称纯原版；DTO代理不包括live采集/真实引擎帧/呈现成本，enqueue成功不等于writer完成，API写完不等于物理落盘。G-02发布阈值、STATE权威快照与最终录制架构均不在此决定。

本阶段不构建/安装游戏模组、修改设置、读账号/private/native/Cloud或当前日志，不启动STS2/正式headless/Computer Use，也不请求玩家测试；正式runtime attempt0。下一门为PM验收Phase0测量方法后在同包决定最小候选与有界人工pilot。

执行：已直接向上述原Implementation发送工作包及权限摘要，工具确认相同threadId接收，未覆盖模型或新建对话；状态IN_PROGRESS。不预称离线实验、合同或正式性能已通过。

## D-091：接受性能测量合同并放行最小候选离线构建

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；ACCEPT_PERF_MEASUREMENT_CONTRACT / OFFLINE_CANDIDATE_AUTHORIZED / RUNTIME_HOLD

PM读源码/原始报告，执行已审只读交付及proxy验证：64项/5公开输入、18块/9000正式CSV样本、6文件3300记录/25,115,655 bytes、完整payload/order/checksum及writer排空通过。索引SHA-256 `7D040844BF504DC87BE57122327789D4EF80021650E247F3330FC21839C8B096`保持。另用独立PowerShell重算9块mean/分位/max，核11构建源码绑定并复现5方法349行固定IL一致；PM未重build/23功能/计时或读取当前玩家数据。源线程直接用户确认计时窗的消息已核对，不由另一任务请求推定用户权限。

早期104ms与后续38–42ms差异保留，不认固定预热已稳态，不追加跑到满意；DTO代理不含live采集/引擎帧或持续磁盘压力，Noop共同成本、线程/进程分配及writer重叠区分明确。Write/Flush成功不外推持久化；此验收不定发布预算、正式格式、STATE权威快照或一般性能。

同 `handoffs/PERF-001.md` 追加Phase1：独立PERF001/candidate-r1、同DLL三模式，落实共同帧源/精确common与candidate patch、原Task成功/idle边界、资格sidecar、同步冻结及有界writer失败收尾。只准workspace离线构建/相关合同与metadata/Harmony dummy非干扰验证；原Phase0/DET及旧产物封存不改，不重复未变测试/再计时。harness可同lineage窄修，不新建泛化INV。

Custom同新seed/无modifier作为最小pilot草案接受，不宣称当前已解锁/同内容或Standard一般可用。三模式保留同modded上下文和正常存盘，不能零模组启动进入个人槽；manifest与存档树须窄IL核对、运行时再验。少量事件/缓冲写不能代表持续I/O负载，不注入伪事件凑样本。

当前不安装/修改配置/读账号存档Cloud日志或游戏进程，不启动或请求玩家游戏，正式runtime attempt0。下一门为PM候选验收后再同包签发有限配置/安装/人工pilot。个人unmodded1/另一账号排除；延用原Implementation对话及模型，不创建新对话。验收 `evidence/perf-001/project-lead-phase0-review-2026-10-26.md` 与独立验证JSON/只读入口。

执行：Phase0验收反馈与Phase1范围已直接送达原Implementation，工具确认同threadId接收，未覆盖模型；Phase1状态IN_PROGRESS。没有发出配置/安装或人工游戏请求，不预称候选已构建/验证完成。

## D-092：备份优先、源码优先和成批原型迭代

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；取代当前开发中的旧个人槽禁读/绝对不变与逐步审批限制，不追溯改写旧实验结论

用户明确认为个人存档保护过严，接受先做存档备份、更多mod/hook操作、开发版bug，必要时通过Steam验证/重装和手动恢复存档；最近不打连胜，unmodded profile1不再维持旧的最高保护限制。授权原测试账号本地存档/配置备份，包含unmodded profile1及modded槽；备份后可做与目标相关的可恢复实验，默认仍使用modded1。另一账号仍排除，不扩展到无关系统/数据。当前尚未执行新备份，不预称可恢复。

工作方式改为反编译C#定位→hook/原型实现→构建与相关回归→用户实测→同任务修复。已有本地3538份C#缓存，日常直接用于调用/实现判断；IL仅在反编译含糊、异步/底层语义影响结论或运行不符时补查，不逐方法复证。允许同一迭代合并有联系的录制/播放/状态/UI改动，首轮失败可接受，不把每个bug变成INV或新probe。默认Harmony运行时补丁；确需原文件变更先保留精确原件/恢复办法，不因用户容忍损坏就改无关游戏文件。

Implementation在已批准目标内可自行修hook、重构、备份、安装本项目mod、调整相关配置并请求人工测试；无需PM逐步批准。备份须在游戏正常退出后完成、可读且关键内容核对，原始账号路径/payload不公开；Cloud由用户实际确认，不自动改设置。正式STS2启动/操作仍由用户手动执行，不授权Computer Use/替代GUI自动化或自动正式headless。可恢复实验权限不降低成品独立Replay不覆盖正常Continue的产品目标。

当前PERF候选已在构建验证中，保留已做工作，完成必要build/关键正常路径和相关回归后直接备份→安装/配置→请求三模式小pilot；D-091的仅离线HOLD由此解除，不另签一串前置任务。不要为旧矩阵补齐所有审计再实测，不追加离线计时/通用profiler。性能比较仍需相同游戏行为/记录完整，不能用改断言或丢记录换PASS。

pilot有答案就收束，下一步优先单房间录制→主菜单独立播放→暂停/单步/退出，再尽早最小后退；真实性能、强记录等沿原型缺口推进，不要求全部技术调查结束才写功能。发布标准、多人/跨版本优先级不变。暂未派发该后续demo实现，不借放权扩成无边界大改。

CURRENT_STATUS/WORKFLOW/TEST_PROTOCOL/TODO/当前PERF工作包已精简，修改前快照在 `archive/2026-10-26-before-D092/`，旧证据/报告保留。封存重要运行结果与对应版本，不冻结所有开发源码；PM验收关键语义，不重复全部构建/hash/旧测试。

执行：新授权和精简工作包已直接发往原Implementation，工具确认同threadId接收，未覆盖模型/另建对话。明确不再等待“候选验收才准备份安装”的旧门，也不要求为了新权限重建工程。当前未收到新备份/安装/实测完成证据，不把派发写成结果。

## D-093：验收真实PERF短pilot并转单房间功能闭环

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；ACCEPT_REAL_GAME_PILOT_METHOD / PERF_CLOSED / DEMO_AUTHORIZED

PM按D-092只验关键结果：同build-006的C1-07/C0-01/C2-01，发布身份与四份实际场景相同；各五语义事件/七原Task成功观测完整。原始分段帧同段连续、interval数及1200个idle间隔复算一致；跨段复用的lead callback不当新增帧/跨段间隔。C2五条1636 bytes从expected内容重建字节/SHA相符，writer停止无丢弃。已计时三阶段合计5.1536ms、同步分配167328 bytes；首次4.9701ms和后四次差异保留，不认原因/稳态。未重build/27项回归、Phase0计时或旧全套审计。

同一次有序pilot不足以给可信增量/长局/G-02，C0共同壳成本与资格/首次使用/缓冲I/O边界按原报告保留。原账号完整本地恢复点及清理依公开收据，PM直接确认当前游戏0、精确pair不存在，不重读当前账号存档或重放准备/清理脚本。最新Custom正常存档保留，未整包恢复；旧失败/版本不改写。验收 `evidence/perf-001/candidate-r1/project-lead-pilot-review-2026-10-26.md`。

本轮最小答案已得到，PERF关闭、不追加矩阵/通用profiler。签发 `handoffs/DEMO-001.md`，在原Implementation对话按D-092直接查源码/整合hook、录制/文件/会话/UI及播放器：真实正常片段自动录制→主菜单从磁盘独立打开→播放/暂停/玩家操作Step→Stop/再次打开。可在同一批次修复和备份后安装/请求用户实测，无新增逐阶段HOLD；播放器可正常await游戏API和管理自己会话，旧observer-only约束不构成禁止实现播放器的理由。

先限定一个普通战斗短片段，不将旧fixture/固定Defend脚本冒充录制，不要求所有技术研究完成才写功能；正式格式/全场景/复杂选择/跨版本/多人/接管不在本任务。成品正常Continue不被占用目标仍保留，未做到就报告部分可用/副作用，不用备份恢复宣传隔离通过。闭环有可用版本先交回，下一步优先最小后退，不为后退或性能大平台拖延当前交付。

原对话/模型保持，备份可按D-092复用或在有新进度时更新；另一账号排除、用户手动游戏不变。当前是授权和排期，不预称DEMO源码/安装或功能已完成。

执行：PERF关闭反馈与DEMO-001工作包已直接送达原Implementation，工具确认同threadId接收，未覆盖模型或新建对话。DEMO状态IN_PROGRESS，不要求另回确认或先交静态调查。

## D-094：验收首个单房间核心闭环并由干净对话接续后退

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；ACCEPT_SINGLE_ROOM_CORE / CLOSED_WITH_LIMITATIONS；DEMO-002 authorized

PM核对本轮唯一私有归档clip（不读当前native存档、不公开checkpoint）：129735 bytes、完整文件hash/envelope Data重算相符，实际Strike/Defend/EndTurn及初态/三步After与公开投影/digest相同。16条事件有两次initial matched、六次1/2/3匹配、两次无Error的完整退出；源码读取磁盘/内存context/游戏逻辑及检查路径支撑这次结果。installed=sealed build-001、当前game0；不重build/六项/旧审计。验收 `evidence/demo-001/project-lead-review-2026-10-26.md`。

控件手势是用户总体确认，不冒称逐次机器记录。只有一个运行会话、两次都播至结尾；跨进程重开/实际中途Stop与正常Continue点入未验证。退出相等限正常Continue主文件/backup和原manager进度/身份，不外推所有个人数据/Cloud。已形成的闭环据此收束，缺少场景留待下一自然功能测试，不追加矩阵。可用ReplayMod仍安装启用、未换未测代码；新完整恢复点DEMO001-20261026-180851-5ee9771d51按收据有效，无恢复/本体修改。

用户明确选择“新建干净对话，用短交接接续（推荐）”，授权创建新Implementation任务。新对话使用现有STS2ReplayMod项目本地目录，非fork、不搬旧聊天、不新建worktree/副本；默认用户模型配置，不擅自指定模型。旧Implementation结束DEMO-001，不再是后退源码写入者。此选择依据阶段交接与用户偏好，不声称token数/压缩次数已证明质量退化。

签发 `handoffs/DEMO-002.md`：基于现有源码加稳定暂停边界“后退一步”，以房间重建＋前缀重演达到p-1、状态匹配后保持暂停，之后Step/继续/Stop正常。先不完整时间轴/跨房间/反向状态恢复；处理旧文件的精确ModHash限制，重录或明确兼容，不篡改旧录像元数据。权限沿D-092，正常开发/备份/安装/用户实测同任务推进，Project Lead仍只负责管理和关键验收。

执行：旧Implementation已收到验收关闭及停止写入通知。工具已在现有STS2ReplayMod本地项目创建并派发新任务 `01a17814-d254-7df0-8c01-affe43d0e04c`（local），未fork、未新建worktree、未指定模型覆盖用户默认；DEMO-002状态IN_PROGRESS，后续消息发新对话。当前不预称已实现后退。普通bug在新任务内修复，不为每次失败再换线程；完成报告由新对话输出，PM读取，不要求向旧对话发送消息。

## D-095：接受重建后退基线但纠正产品目标为直接稳定点恢复

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；ACCEPT_REBUILD_AND_MIDSTOP_BASELINE / DIRECT_BACK_UNMET；DEMO-002继续

PM核对真实12操作clip b51769abc3fc4bcb9b0335839a270543（实际QSSH9K8MBQLX、SEAPUNK_WEAK/row1col4），私有归档文件hash/envelope/12操作与第4/8/12步EndTurn相符，四次1→0/3→2/2→1/8→7到达状态/RNG匹配且暂停，11 forward/10 prefix成功。实际在7/12中途Stop无Error并恢复所比较原manager/Continue/进度；AllOperationsMatched=false如实表示未播完，不是全部完成或失败。当前installed=sealed、game0，不重build/9项/旧审计，未读当前native存档。验收 `evidence/demo-002/project-lead-review-2026-10-26.md`。

用户在Implementation对话明确指出期待直接回到上一步而不是重置并自动播放前缀；该人类消息已直接核对。当前重建/重演符合PM过渡任务，不是实现者擅自改设计；约1.8–5.2秒体验尚未达到产品目标。不将隐藏同一慢重演或仅终点相等认作完成，MVP NAV补入该澄清。完成后后退/重开/跨进程/8→7后重做EndTurn等未验，不追认。

继续同DEMO-002与现有新Implementation，优先验证播放期已访问稳定点的内存缓存/真实恢复：先当前基础牌/普通敌人，恢复状态和UI、RNG后能正确继续，特别跨EndTurn重做。无需先改正常录制热路径或建设持久化全场景快照系统；现有比较JSON不是已证明可恢复的完整快照。允许先出同回合直接恢复部分增量，跨回合缺口如实标，不以重播fallback冒称同一直接恢复；最终架构和量化延迟仍待证。

权限沿D-092，可同任务查源码/修hook/构建/备份安装/请求用户实测，不另开对话或INV。优先回放时缓存以隔离正常录制成本；缓存可保留对象身份锚，但可变字段/容器必须有独立状态，清理时释放。边界是实际逻辑恢复及后续等价，不是手填HUD；若恢复范围不足给出实际缺口/最小可行增量，不再次泛化猜谜。

执行：验收/预期责任说明及直接恢复修订已发送当前实现对话 `01a17814-d254-7df0-8c01-affe43d0e04c`，工具确认接收；不回旧长对话，不新建任务。当前没有直接恢复已完成或延迟达标事实，不要求额外确认才继续开发。

## D-096：接受直接后退固定样本并转完整首战胜利回放

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；ACCEPT_DIRECT_BACK_FIXED_SAMPLE / DEMO-002_CLOSED_WITH_LIMITATIONS；DEMO-003 authorized

PM查看DirectCheckpoint/IdentityMemento/PlayerLoop，核对两个Runtime会话各1次初态room-load、共30次真实restore及38次forward，目标/投影/RNG匹配、0前缀和额外room-load；独立按事件重算位置，确认第4/8步EndTurn退后重做、到12后退11/退0续播。用户直接确认“测试完毕。符合预期”。实际记录12.0385–51.517ms含局部UI/一帧核对，不是显示器延迟或发布预算。验收 `evidence/demo-002/direct-r1/project-lead-review-2026-10-26.md`。

两次最终4/12退出AllOperationsMatched=false不抹掉曾播完12；一轮Busy时Stop由在途EndTurn完成后退出，一轮稳定点Stop，均原manager与比较的Continue/进度保持。不同Runtime.SessionId/各Open1只按跨Runtime读磁盘表述，无逐OS PID链，不认任意崩溃重启。当前sealed/installed build-002匹配，game0；旧12操作文件9F6425…274E3未改，仅明确已知前驱格式兼容。未重build/5项/旧矩阵，备份按本批收据、无恢复或本体修改。

恢复只支持当前基础牌/普通战斗的已访问稳定点：独立可变字段/容器＋同会话身份锚，完整RNG值，保留live turn loop/待完成信号和action ID；仅观看时缓存。实际继续支持本样本，但不推出任意内容/隐藏状态都足够，死亡/成员变化/宠物拒绝。DEMO-002这个增量收束，不继续追加已回答矩阵。

下一任务 `handoffs/DEMO-003.md` 补当前明显可用性缺口：首场普通战斗胜利自动封存、完整顺序回放到最后操作和结束，再Stop/重开。保留活战斗段暂停/Step/直接后退；死亡/胜负终点后退可先明确不支持，不因难点让最后一击丢失/整段失败，也不假称死亡恢复已完成。奖励选择/下一房间/失败结局/任意全状态/发布性能不并入本增量。

继续当前Implementation `01a17814-d254-7df0-8c01-affe43d0e04c` 与源码，D-092授权备份/安装和人工实测，无阶段审批/新对话/工程。保留可用build与旧数据，必要格式变化如实版本化，不篡改旧clip。消息工具已确认DEMO-003派发；随后用户要求「接下来我要打一把。正好我们在这里停一下」，PM已发送暂停通知并将任务标为PAUSED_BY_USER。等待用户恢复，不声称完整战斗已实现。

续接：用户随后明确「不打了。继续吧」，解除上述临时暂停。恢复DEMO-003和D-092既有开发权限，继续原Implementation/源码；不重新申请阶段批准，不据此声称功能已实现或游戏进程已退出。

## D-097：收束完整首战胜利回放，下一目标与用户讨论后另定

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；ACCEPT_COMPLETE_FIRST_COMBAT_FIXED_SAMPLE / DEMO-003_CLOSED_WITH_LIMITATIONS

build-002实际重播旧10步、新录9步Beetle胜利自动封存/完整播放，解决装饰死亡动画造成的尾延迟；build-003实测修复后退时已有玩家缩放/手牌入场问题。用户最终确认「可以了，这次测试ok」，明确本轮不扩群怪，要求与PM讨论下一任务。验收 `evidence/demo-003/project-lead-review-2026-10-26.md`。

PM核对最终原事件游标、47次forward/20次直接恢复及20次呈现读回、3次完整胜利退出，原manager/所比较Continue与进度保持；sealed/installed build-003匹配，r2/r3原9步私有clip归档hash不变。没有重build/测试/恢复或全账号重复审计。计时仅样本路径，不认发布预算；显示值读回加用户确认不等于任意像素/形态覆盖。旧Sludge Spinner未封存失败和旧证据保留，不追认为成功。

生成/死亡未支持主要是后退跨该边界时的复活/撤销对象，当前禁用，不等于群怪顺序播放必然渲染错误；顺播走原生创建/死亡，但群怪/召唤无本轮样本，不能宣称通用支持。现有对象缩放恢复不等于场景树/节点替换/任意形态逆恢复。

本轮收束，可用build保留；Implementation `01a17814-d254-7df0-8c01-affe43d0e04c`已停，不自动追加测试/下一任务。下一步与用户明确更常见战斗的覆盖目标和死亡边界的最低导航行为，再签发增量；当前未授权新的具体工作包，既有D-092开发权限不变。

## D-098：明确全部卡牌、遗物、药水覆盖目标及早期群怪候选

日期：2026-10-26，Asia/Shanghai  
状态：Accepted（产品覆盖目标和下一方向）；具体工作包尚未派发

用户认可下一阶段规划，补充「对全部卡牌和遗物和药水的支持」，尤其提醒第二阶段不能遗漏遗物。MVP覆盖目标明确为受支持版本全部原版卡牌（含升级）、遗物和药水，含自动触发、持续/可变状态及联动；现有基础牌白名单为开发阶段限制。按机制和真实场景分批实现/验证，不把代表样本成功宣称为全部内容已经支持。正常录制性能等既有要求不变。

用户经验提供「最早能遇到的群怪是暗港的两组弱怪」。PM只读核对固定v0.111反编译：`Models.Acts/Underdocks.cs`列入`CorpseSlugsWeak`/`ToadpolesWeak`，两份遭遇定义均普通Monster/IsWeak且生成两只同类怪物，Toadpole还有前后位差异。静态确认候选资格，不推定全游戏最早排序、任意seed首房必出或运行通过；适合检查中途减员后同名目标身份/顺序以及跨死亡恢复。未启动游戏/修改模组或玩家数据。

下一方向保持普通群怪完整录播＋跨中途击杀的直接后退和按已录操作继续。召唤、终局后退、全部卡牌/遗物/药水不同时塞入该增量；具体范围继续细化，同工程/Implementation，不因本次线索先开独立调查。MVP基线与TODO已补充，DEMO-003关闭结论不变。

## D-099：启动普通群怪录播与跨中途死亡直接后退

日期：2026-10-26，Asia/Shanghai  
状态：Accepted；DEMO-004 IN_PROGRESS，消息工具已确认原Implementation接收；尚无本轮运行结论

用户在认可规划、补充全部卡牌/遗物/药水覆盖目标及暗港弱怪线索后明确「好。我们继续？」。签发 `handoffs/DEMO-004.md`，沿原Implementation `01a17814-d254-7df0-8c01-affe43d0e04c` 和 `src/ReplayMod/`直接推进，不新开工程/对话/独立INV。

一场普通双怪真实录完/播完，中途一敌死亡且仍有敌存活后，直接退回致命操作前，恢复实际对象、同名身份/成员顺序/目标/状态/显示，再按已录操作重做至胜利、Stop/重开。CorpseSlugsWeak优先，ToadpolesWeak可替代，一组实际通过即可。允许同任务提供可复现seed/明确标识测试入口降低准备成本，不篡改实际checkpoint/旧录像或冒称普通随机样本。恢复机制尚未验证，不把它当成只补字段的小修，也不以只显示复活或房间前缀重演过验收。

本任务明确放开原普通敌人的中途死亡逆恢复，可修订旧成员变化/SameNodes等拒绝条件；召唤/分裂/新生成、任意换形、胜利终局后退、玩家失败、药水/选牌/后续房间与全部内容不同时扩入。活战斗缩放/手牌体验、胜利封存及观看进度隔离保持，旧DEMO-003固定样本不外推为新场景已通过。

D-092相关源码/hook/缓存/生命周期/UI重构、构建/必要回归、备份/安装及人工测试权限继续，无逐阶段审批；普通bug同任务修。用户手动游戏、另一账号排除等现有边界不变。最新已知恢复点DEMO003-20261026-231903-3ae7b9b1e9，需保留新进度则更新，旧可用build/clip保留。PM仅签发/维护状态，不代实现，也不提前声称跨死亡恢复成立。

## D-100：接受定向双蛞蝓的跨中途死亡恢复，收束DEMO-004

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；ACCEPT_TWO_ENEMY_MID_DEATH_REWIND_FIXED_SAMPLE / DEMO-004_CLOSED_WITH_LIMITATIONS

用户确认「测试完毕。符合预期」，提醒后补完成Stop/正常退出。PM核对包含两个closed的最终297事件、CombatRoster/DirectCheckpoint路径和sealed/installed身份，而非早先仍停在胜利页面的快照。定向暗港双蛞蝓11步真实胜利封存，42次forward/31次restore匹配，7次5→4恢复原Id1至7HP/Vulnerable2/GLOMP并回滚Id2吞噬/Strength4/STUNNED；同名目标顺序、重做第5步8次与余下战斗匹配，0前缀/仅初始room-load。验收 `evidence/demo-004/project-lead-review-2026-10-27.md`。

两次同Runtime打开分别退0中途Stop、完整至11胜利Stop，Error=null，原manager及所比较Continue/backup/progress文件/对象保持；第一轮AllOperationsMatched=false为正常未播完，不是差异。恢复实际原对象/协调订阅，跨死亡只重建显示；不调用spawn逻辑。计时及所选显示值/体验只认当前样本，不认发布预算/像素完备或全部隐藏状态恢复。

build-001可用安装保留；最新完整本地恢复点按收据为DEMO004-20261027-003416-2f3bc72631，1276文件/132024473 bytes来源/副本匹配，旧pair/clip/事件/恢复点保留。PM未重build/运行旧测试或读取当前原生存档/整包重复审计，无恢复或本体改动。

仅固定定向CorpseSlugsWeak证据，不认自然随机样本、Toadpoles/其他群怪、召唤/生成/分裂、终局回退、玩家死亡/宠物、任意形态或全部卡牌/遗物/药水/房间通过。本轮收束，不加矩阵或自动开下一任务。下一优先按D-098规划常用内容与录制范围扩展及权威状态记录，具体包尚未签发。

## D-101：启动非初始牌、遗物自动触发与药水的组合录播增量

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；DEMO-005 IN_PROGRESS，消息工具已确认原Implementation接收

用户在DEMO-004验收后明确「好。那就继续？」。按D-098全部卡牌/遗物/药水目标签发 `handoffs/DEMO-005.md`，原Implementation/工程继续，不先建立独立调查或换聊天。扩通用普通出牌/用药和状态恢复路径，不将三张基础牌白名单仅换一组ID后称全支持。

实际组合至少含一张升级非初始牌、一种能力/持续效果、一件可变计数且实际自动触发的遗物和一次真实药水使用；优先可明确准备ShrugItOff/Inflame/HappyFlower/EnergyPotion或等价内容。固定本地源码确认HappyFlower的TurnsSeen/回合触发、ShrugItOff的抽牌升级以及EnergyPotion的玩家目标，不预判输入边界/Task/显示恢复已正确。回放需跨对应操作后退并恢复计数、槽位和实际效果，按原录像重做到胜利并正常退出/重开。

定向初始内容须进入实际checkpoint并标记，观看不重复注入。先限无需额外选择/新战斗单位的一场普通战斗；选牌/自动出牌链等如未支持要明确拒绝/标不完整，不能漏操作仍封存完整胜利。完整整局、召唤/终局回退、任意组合和持久化强记录/纠偏不并入。可同任务修订基础牌/牌组/升级/操作格式等旧实验限制、相关hook/memento/UI并做必要构建/备份安装/人工测试，无逐阶段审批，既有用户手动游戏等边界不变。

最新恢复点DEMO004-20261027-003416-2f3bc72631，需保留新进度时更新、旧可用pair/clip保留。当前只有新目标授权，尚无本包代码/运行通过结论；完成交回PM收束，不自动扩下一包。

## D-102：接受升级卡/能力/计数遗物/药水组合，收束DEMO-005

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；ACCEPT_COMBAT_CONTENT_COMBINATION_FIXED_SAMPLE / DEMO-005_CLOSED_WITH_LIMITATIONS

用户确认连续后退符合预期并退出，另补打开/短测Stop/退出。PM核对ContentPresentation/活动节点过滤、两Runtime事件游标、36 forward/18 restore及槽位/计数/能量UI；r2完整至17胜利Stop，r3新Runtime位置1主动短测Stop，两次原manager及所比较Continue/progress保持。原失败窗口10→9七次、四次真实残留节点/淡出清理，药水重做4次/花触发8次及跨死亡三次有实证。验收 `evidence/demo-005/project-lead-review-2026-10-27.md`。

原录像实际含Inflame、ShrugItOff+、EnergyPotion玩家目标/消耗/历史和HappyFlower计数2→0/加能量及胜利。r1两次失败保留，不用局部成功追认整轮通过；升级抽牌撤销沿r1局部证据，修复版未单列重测未变输入路径，保留来源。sealed/installed build-003及三份原片归档匹配，未改原hash；无重新构建/旧测试或全账号重复审计。

可用安装/旧pair/失败/原片保留；最近恢复点DEMO005-20261027-013423-1f0ea24e59，此后只有观看，不重复整包备份。仅认组合及所测边界，不认全部内容、选择/自动链/生成、终局/后续房间、发布性能或持久化强记录/纠偏。

本轮收束，不自动派下一包。结合用户原生时序经验，下一优先建议规划磁盘可恢复权威状态与偏差后安全恢复/继续，具体范围未签发。D-092开发权限保持。

## D-103：直接推进权威状态落盘、冷恢复与自动纠偏最小闭环

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；STATE-001 IN_PROGRESS（用户醒来后明确恢复）

用户明确「继续推进权威状态落盘和纠偏，直接把下一份任务发给Implementation Codex」。签发 `handoffs/STATE-001.md`，沿原Implementation/工程直接实施，不先新开调查/对话或等待阶段批准。

首轮一个真实非零稳定点即可，可先首回合/成员不变。正常录制采集可恢复数据落盘，退出录制进程后新启动游戏，在初始观看房间直接应用该点，零前缀/校验；再对两个独立逻辑量受控扰动，真实检出后重新读/解码文件数据恢复同位置/校验，原Play自动继续至真实胜利、Stop/重开。仅比较投影/旧进程memento/原生不含完整战斗的SerializableRun或重演前缀不能代替。实际状态schema/绑定/安全应用机制尚未决定，不承诺本轮全状态/任意点/原生卡死/跨版本。

允许同任务调整采集/codec/映射/恢复/控制/UI/hook及必要格式，保留旧pair/原片；采集数据先独立冻结，后台不并发遍历live graph，真实路径少量耗时/bytes观察，不扩性能平台。现有对象与live Task/场景依赖按新上下文重绑定，不序列化旧Task或依赖旧引用。可恢复差异自动继续且保留原控制语义；不可安全恢复时明确停止/正常退出，不伪造不存在的原局后续。

D-092备份/安装/人工测试及普通bug同任务修权限保持。新录一次、用户正常退出并新启动，最短冷恢复/扰动/纠偏/后续播放和退出检查即可；无需全进程故障矩阵。最新恢复点DEMO005-20261027-013423-1f0ea24e59按需复用/更新，另一账号/Computer Use等既有边界不变。当前是授权与目标，尚无本包实现或运行结论。

暂停补充（2026-10-27）：用户在Implementation明确「暂停一下可以吗，我要关电脑睡觉了」，已停止STATE-001；仅阅读/初步方案，未改实现/构建/安装/运行，已验收DEMO005/build003和原恢复点保持。续接记录见STATE-001-work-in-progress.md，等用户恢复，不自动开工/测试/安排唤醒。PM仅记录暂停，不追加检查或派其他任务。

恢复补充（2026-10-27）：用户在PM明确「我醒了。继续吧」，解除上述临时暂停。恢复STATE-001及D-092既有权限，从续接记录继续原Implementation/工程，不新开工作包或重复旧矩阵；现有分析不当既定架构，尚无本包实现/运行结论。

## D-104：接受首Defend点的跨进程文件恢复与自动纠偏闭环

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；ACCEPT_FIRST_DEFEND_FILE_RESTORE_AND_CORRECTION_FIXED_SAMPLE / STATE-001_CLOSED_WITH_LIMITATIONS

用户确认build-003测试通过并退出。PM核对RecordedState读/应用、StateGraph写入/读回、PlayerLoop冷入口/比较纠偏及文件回调路径，确认每次实际读文件/独立解码，captureData=false不取memento恢复值。原录制PID10832、r4观看PID43824/启动时间实际不同；2次文件直达1/14，后续Sequence均≥2、无前缀，400绑定的初态形状确实不同，35逻辑锚点匹配。验收evidence/state-001/project-lead-review-2026-10-27.md。

5次文件应用（2冷恢复＋3纠偏）、历史11项/18RNG和投影读回匹配；PM从指定私有原日志逐叶复核三次差异仅Energy/Block的4条镜像路径。纠偏重新读文件，Playing保持true/false/true，光标不增错，20后续动作/4内存后退匹配。首场14胜利Stop、第二场重开短测busy Stop至4，两次原manager和所比较Continue/progress保持；第二次AllOperationsMatched=false是主动未播完。

sealed/installed build-003及原clip/state私有归档byte SHA核对。日志FileSHA256实际为Data payload570CDB…，物理state文件byte SHA51D8B9…，含义已区分，保留已测二进制不为日志改名再换版。旧r2拓扑门/r3容器登记失败与原片不改写；原恢复点STATE001-20261027-122340-b30c90322d保留，之后只有观看不重复整备份/恢复。PM未重build/运行游戏/旧测试或读当前原生存档。

只认首回合首Defend后1点/稳定上下文中的受控偏差；当前文件入口仍有不同录制进程/Runtime来源门，1→0不可用。同进程普通播放/内存回退不等于同进程文件纠偏。更多状态点/新增能力或用药变化、多回合/跨死亡生成的文件恢复、在途卡死/原生凋萎、跨版本、全隐藏状态和发布性能不由此通过。

本轮收束，可用安装保留，不自动追加测试/下一能力。下一优先建议在已测组合内选能力或用药后的新关键点，扩权威状态字段/取点和恢复边界；具体目标待用户讨论，未派新包，D-092开发权限保持。

## D-105：先推进战斗内容覆盖，以共用机制接入选择与派生效果

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；规划优先级调整，本次未派发实现工作包

用户明确全部内容最终都要做，下一段首先完成内容覆盖，并询问全牌池、回合内遗物和局内选择。优先级由再增加一个能力/药水文件恢复点，调整为战斗内容机制覆盖；完整强记录仍是目标，相关状态/文件扩展跟随内容实施，不建立更多单点实验前置链。

本次源码核对：当前普通牌/药水调用原生执行入口，没有逐卡重写效果，但仍锁定铁甲首战；ReserveChoiceId、生成牌、自动出牌等被明确拒绝，OperationKind也无选择结果。遗物走原生hook，不因回合内触发就需要单独实现，但HappyFlower样本不能证明所有遗物的录播/恢复。固定本体已有PlayerChoiceSynchronizer、CardSelectCmd及CombatReplayWriter的选择记录通路，可以作为接入基础，尚未在本项目运行验证。

下一实施重点为统一选择的记录/重放，含出牌/用药中、回合/战斗开始及连续选择；补生成/复制/变化与自动派生动作的身份/顺序/状态，再扩各角色牌池与遗物/药水联动。按共用机制成批实现，避免每张牌一个特例/工作包；实际覆盖与恢复分别验，不将OOP派发等同于完整输入录制或任意状态恢复。用户所有具体例子及源码入口已记入technical-notes/COMBAT-CONTENT-COVERAGE.md。

既有通过/失败、安装与备份不变。本轮仅阅读源码并维护计划，未改实现、构建或运行游戏。D-092权限及用户手动游戏流程继续有效。

## D-106：授权CONTENT-001实施通用战斗选择录播

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；CONTENT-001 IN_PROGRESS

用户明确「好。按照你的规划继续做吧」。签发handoffs/CONTENT-001.md，使用现有Implementation对话01a17814-d254-7df0-8c01-affe43d0e04c及src/ReplayMod，不新开对话/探针或先交一轮纯调查。

目标为正常人工选择的录制、观看中自动完成原决定、稳定操作点后退重做，覆盖出牌/用药中、战斗/回合开始和连续选择。可同任务修改选择事件/格式/队列/状态/显示，补生成、复制、变化与派生效果的直接依赖，解除妨碍验证的角色/输入演示限制；不逐卡重写效果，不把OOP原生派发或取消拒绝当全内容已支持。原生入口方案由Implementation依源码确定。

按少量机制组合实录/播放/后退/胜利和退出，保留重要失败并同任务修复，不要求每张牌完整矩阵或逐阶段PM批准。当前可用版本/旧录像/恢复点保留，备份按需复用或更新；D-092用户手动游戏、正常退出后换DLL、Cloud确认和另一账号排除继续有效。完整整局、召唤/宠物、终局任意seek及通用磁盘恢复不自动并入。当前只授权工作，尚无本包新增实现/运行结论。

## D-107：接受选择结果级录播与内存后退，另记原生选择过程展示期望

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；CONTENT-001_CLOSED_WITH_LIMITATIONS

Implementation交回用户已实际确认的build-001结果级组合：真实16操作/15人工选择片，两次观看36个自动选择/36操作/19直接恢复，包含工具箱开场、攻击药水开牌、连续/有序多选、Discovery、Tools/Tyranny/Entropy回合开始选择及变化、金纸触发。PM阅读选择scope/交付/恢复源码，逐次对照安全原choice帧并重算step/restore光标与native ID顺序；失败/差异事件0，一次完整胜利Stop和重开短Stop、原manager及所比较Continue/主progress通过。验收evidence/content-001/project-lead-review-2026-10-27.md，不认全内容/其他角色完整入口。

当前build-002只去掉重复升级标题并加入原实测F1D9精确兼容，PM核对sealed/installed为F6587078…CA6A42D8。构建/数据检查通过，完整运行证据仍归F1D9 build-001，不为显示小修改重跑全部矩阵。最新恢复点CONTENT001-20261027-145813-b34d252680按收据保留；退出53个saves仅progress.save.backup变化，后值等于未变主progress，但调用时刻未知，不称全文件零写入。PM未重读当前原生存档/重复全账号审计。

用户经Implementation补充希望调出原生选择窗口，像真实操作一样拆分展示选择过程，已记录investigation-reports/CONTENT-001-choice-ux-proposal.md。当前只有顶部文字与最终结果交付，宿主整体Step；格式4没有逐点击/撤选/hover轨迹，旧最终结果不能反推成真实交互记录。过程展示、Step子动作与选择中途Back范围需继续讨论，本次未派新包，不把全部半态恢复自动并入。内容覆盖仍为主线，D-092权限保持。

## D-108：实施原生选择过程展示，暂停浏览与子动作单步

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；CONTENT-002 IN_PROGRESS

用户补充常见选择窗口为手牌、抽牌堆、弃牌堆、三选一/二选一及特殊探寻打击，要求打牌后自动打开选择界面，暂停时允许自由滚动，下一步聚焦并选中；强调聚焦样式以后细化，并明确认可PM安排「你做一下吧」。据此签发handoffs/CONTENT-002.md，沿现有Implementation/工程直接实施，无新增调查/阶段批准。

新录真实打开、选中/撤选、确认/跳过/关闭及相对时间，播放按原生界面/手牌模式可见呈现；Step推进选择子动作，观众暂停时只能浏览不能改决定。初始加载/宿主等待期间控制也需工作，下一Step自动找回原实例并使其可见；原生单选即提交不造额外确认。Back先按已完成宿主稳定点，途中Stop清理正常；不把中间Task快照/任意半态倒放合入。旧格式4可标明结果演示，不反推真实轨迹。

PM只读源码找到SeekerStrike的先伤害、CombatCardSelection随机子集、FromCombatPile过滤路径，中文映射与实际UI交Implementation核实；不把特殊候选扩大为全牌堆。精美聚焦、全时间轴/整局/跨版本仍不并入。D-092相关改动/备份安装和人工测试授权保持；当前只是任务启动，无新功能通过结论。

## D-109：最终决策模式优先，选择路线前后对称

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；CONTENT-002继续实施，同包修订

用户在Implementation明确首版「整体效果符合测试预期且没有bug」，但修订为：回退覆盖选择路线、前进/后退对称；默认播放最后确认的决策序列，省略思考中的反复选中/撤选；原始过程可全部记录，未来两种模式可并存，现在优先最终决策模式。用户授权其自行与PM沟通；PM直接读取该条userMessage确认，不能把此修订当Implementation自拟需求。

撤销D-108仅宿主稳定点Back与原始过程默认优先的边界。CONTENT-002不另立工程/ID或先关闭再审批；更新同一工作包，允许相关控制、状态恢复和等待上下文重构。Back/Step沿同一展示路线，含选择已提交/窗口关闭后返回相应选择，并能再次前进；只倒放UI/移动光标不恢复状态或跳过整段只退宿主不能满足。技术方案由Implementation选择，不复活旧Task/TCS，不以玩家可见从头重播替代既有直接后退体验。

最终决策投影保留实际生效结果/实例/必要顺序，不因压缩展示改写牌效/RNG。原始Events继续保留，完整原始过程观看UI不要求本轮实现；旧片不得伪装成真实点击轨迹。暂停滚动、自动聚焦原实例、观众不能改决定、Stop和普通Continue/进度要求保持；精美焦点/完整时间轴/整局/跨版本不并入。

首版录制15操作/8选择/41事件已核对，用户观看正常的机器日志复核仍由Implementation推进；最近交付build-003/0247258A…与原录制build-002/0C6F和原片保留，不追认新目标通过。最新完整恢复点按收据为CONTENT002-20261027-171201-97855f350a，1280文件/132063795 bytes。D-092开发权限不变，PM已通知直接同包继续，不等新阶段批准。

D-109进展：build-004/86AA29E3…已构建/安装，DecisionRoute候选为真实数据历史节点加新原生展示等待，仅执行前沿交付业务结果；原41事件不改、最终决策投影33事件。5组新数据/控制和7组既有选择检查通过，原生双向/半态恢复仍待人工验证，PM不提前验收、也不增加批准门。旧D-108/build-003 r2已有446事件/26操作/10稳定点恢复、46暂停投影/RNG比较和6实例定位的报告，PM独立重计step/restore/退出；三次关闭为初始Stop/完整胜利/短重开，Error=null、所比较原manager/Continue/主progress保持，没有普通选择中Stop记录。旧r2后53存档内容相同沿Implementation复核，不证明无同值写入。原源码/原片/事件与已测pair保留，新目标单独判定。

D-109/r3进展：用户报告build-004所有Back无效，168事件中正向15操作/8结果/8交付/33节点至胜利匹配，但0Back请求/0恢复；不能以正向或零error认双向通过，也不能说恢复算法已经执行失败。Implementation定位Ui.Refresh旧/新规则每帧先禁后启吞按钮点击，005改为if/else互斥、加按下/释放日志并构建安装，SHA D045E580…81DF6，未实测。PM读取汇总/收据并核对当前UI分支，未重复构建或运行游戏。004失败pair与168原始事件保留；下一直接短测工具箱Step→Back→Step→Stop，先触及首次恢复再继续原范围，无新ID/批准或整场重跑。

D-109/r4进展：同一build-005初始已提交工具箱选择短测实际通过，37事件记录Back按下/释放和接受、1→0/0→1的真实状态/RNG与Owned校验、原赠牌ID34恢复，原生生成/交付各1次。Stop宿主0为预定短停，Error=null且所比较原manager/Continue/主progress保持；原始事件归档，不外推待决跨Open/多选/连续/终局。下一短段到宿主3验证秘密武器未提交跨Open、探寻伤害撤销和早有准备确认后前缀返回，保持DLL，不重复初始点/重录/整场矩阵。Implementation同时更正r2汇总53项为新恢复点真实54项，原循环已比较全部54项，保留更正注记和原日志；r4同清单内容保持不等于无同值写入。

D-109/r5反馈：用户报告build-005中央出牌展示不随节点控制/Back悬空、佳酿历史前进缺抽牌动画，以及牌堆计数/浏览异常。Implementation现场初步核对Runtime193ca1b0691b41f998771bf4348a12e0有793事件/82路线到达/52恢复，前沿到宿主7后Stop，退出比较保持；正式报告/汇总尚待落盘。旧逻辑/RNG/手牌断言未覆盖PlayContainer、牌堆按钮_currentCount与历史动画，不能算D-109通过。允许且正在同包修显示、计数/浏览、节点控制及不重复业务的历史迁移动画，补相关诊断；不重录、新开目标/审批或将数据匹配冒充完整视觉正确。

D-109/build-006进展：r5正式报告/793原始事件及54项存档/原片保持复核已落盘。显示修复候选FB40ADB4…88CFE已构建/安装，按节点同步中央NCard、牌堆缓存/文本/浏览、展示来源回调与历史纯展示迁移，并新增对应诊断；没有原生画面复测，D-109未验收。005 pair/settings/selector及006源码保留build-006-0a69ad5e准备目录。下一仅定向检查中央卡跨Open、佳酿Back→Step动画与牌堆数量/浏览再Stop，不重录/整场矩阵或新增审批；候选实现不是运行通过。

## D-110：历史前进保持正常原生展示语义，不接受统一简化迁移作为完成形态

日期：2026-10-27，Asia/Shanghai  
状态：Accepted；CONTENT-002同任务修正实现与验收要求

用户在Implementation反馈Back逻辑应正确，但Back后Step全部动画像简化播放，询问刻意设计或绑错。PM直接读取该userMessage与原因说明；源码确认006历史迁移只存Card/From/To并逐张用固定0.16秒ghost Tween，没有原生类型、批次/并发与时序。不是某张牌动画资源绑定错，不能以状态检查通过认正常历史重播。PM此前只写不重业务的纯展示，未明确正常原生观感，现补足要求；此为修复已有回放体验，不扩大为全动画产品。

r6 Runtime d9e325b6e432498da48a1fd240bd1229有1035事件/80到达/62数据恢复/109节点显示与62恢复显示检查，29次简化动画后状态/RNG不变，原生操作1…12匹配后Stop，退出比较保持；只观察Draw浏览23=23且打开，未见Discard、完整胜利或连续选择。原片/1035原始事件和54项存档内容保持沿Implementation复核，不补造未测通过。006及既有成果保留，不重复请求用户测试006动画。

同速度下，已支持片段的历史Step/Play需保留普通出牌/抽弃回手/生成消耗及必要攻击反馈的正常动作类型、批次/并发和先后语义，不把所有动作统一插值；不要求像素逐帧或Back反放特效。保留真实历史恢复与最终决策模式。允许同包分离/复用原生展示和通知/挂接副作用，或采集观看前沿必要展示信息，技术方案由Implementation验证；不重复业务、改RNG或复活Task，不凭原生helper名字假定其纯展示，也不全局屏蔽通知掩盖问题。

下一直接实做抽弃牌批次＋普通出牌/攻击的可对照切片后请求最短人工检查，不先新开INV/平台或只交方案等阶段批准；确需新录制信息才说明，不机械重录。具体无法安全拆分的展示类型给明确阻碍/取舍回PM，不静默把简化模式当用户接受。D-092权限不变，整体仍未验收。

D-110候选进展：Implementation正在实做观看期限定展示切片，采前沿卡牌实际姿态/批次时钟和已核对纯显示入口的生物动作/特效/伤害数字/声音等，历史沿同一时钟展示后应用目标真实数据/重开选择，保留Back。只取卡牌/血条显示而非整场景逐帧复制，不改录制文件、不采原Task/TCS或要求重录。首轮对照秘密武器/探寻/早有准备至佳酿宿主6，不外推击杀/敌方回合全视觉；仍为未运行验证的技术候选，需确认观感、暂停/速度、副作用、回收及少量真实开销，不因实现采样而当架构定稿/功能通过。

D-110/build-007进展：F8595D9C…3A77F观看期展示切片已构建安装，006固定ghost删除；共用30Hz采样卡牌/血条/堆数字与已审查cue，Release/时钟合同/原片兼容读取通过，尚无原生展示或实际采样开销结论。006 pair/settings/selector及007源码/合同保留build-007-6d0e7a54。下一只复用原片比较探寻出牌/攻击/命中和佳酿抽弃批次/计数/抽弃浏览，到宿主6 Stop。液体shader延迟任务未隔离而未接，击杀/敌方回合/全部自定义VFX/连续/胜利视觉不宣称支持；这是候选限制，不是用户同意删需求，D-110仍未验收。

D-110/r7反馈：用户认可Back整体逻辑，但指出手牌编号/显示像重复硬刷新且卡顿，药水历史前进缺饮用表现。Implementation初步核对1851事件/3打开关闭/30原生操作（15胜利＋10＋5）/129到达/82恢复/40显示重播与所比较退出保持，无浏览事件，正式汇总待落盘。采样max73.2144ms、累计2124.6557ms/1799帧，不能宣称性能通过或将全部卡顿归因于此。007仅隐藏NCard、重复重建、逐帧文本与末尾Snap存在显示交接缺口；同包改完整手牌/编号遮罩、冻结模型缓存/节点复用/按需属性和单次真实UI交接，并补药水瓶/空槽/泼洒与独立液体材质采样。新方案待实测，不重跑OnUse/旧任务，不让用户先补旧测试；普通修复不另审批、原片/版本/失败保留。

D-110/build-008进展：9FA167AE…2DAE003候选已安装，007失败1851原始事件与54项存档/原片保持核对已归档。008实现整容器/编号遮罩、身份变体冻结模型缓存/池化/分帧准备/按需更新、遮罩内真实UI交接及瓶子/空槽/泼洒/独立液体shader，Release/文件读取和数据合同通过，不认原生画面/性能改善。007 pair/settings/selector及008源码合同保留build-008-6693e749。下一最短只查早有准备交接、佳酿跨Open用药动画及确认后抽弃批次/编号，到宿主6 Stop；饮用不在已喝药的选择前缀重复。无重录/全矩阵/新审批，D-110仍未验收。

2026-10-28，D-110/r8进展：008原生op1后探寻选择失败，104事件和Godot日志确认ObjectDisposedException，NCard.OnReturnedFromPool→Create→NCardGrid.InitGrid；自有显示卡借原生池后被Exhaust延迟归池，再直接销毁造成free列表污染，非原片/目标错误。首故障应保留，退出operation-cancel不覆盖它。当前改私有scene/弱归属延迟清理与私有Free拦截，真实Hand/Play走原生归池，增加共享池及实际grid-ready检查；构建/延迟归属合同通过，009未安装，native仍待。准备129.6842/303.1513ms与capture max33.7294ms分开记录，不称性能改善；同步去隐藏显示遍历/重复刷新日志和计算，保留严格校验。下一仅秘密武器Back/Step两次后完成探寻→Stop，不重录/后段矩阵/审批，失败与原片/54项存档保持复核均保留。

2026-10-28，D-110/build-009进展：6C68780A…C4B7792已安装，私有scene/弱归属延迟Free、真实Hand/Play归池、共享池与grid就绪及恢复开销修复落地，Release/时钟变体和延迟归属合同/原片读取通过，无native实测，不验收。008 pair/settings/selector与009源码合同保留build-009-a5b42356，r8的104事件/Godot完整日志及安全异常栈归档，原片/54存档保持。下一只秘密武器Back/Step两次再完成探寻、健康可来回一次，到宿主2 Stop，同时看分阶段延迟；不补后段/重录/整场或新增审批，不以代码认性能改善。

## D-111：补全原生视觉层，纯历史展示可中断并接收导航

日期：2026-10-28，Asia/Shanghai  
状态：Accepted；CONTENT-002同任务修复，未验收

用户在Implementation确认009探寻能完成，随后明确首次正常、历史入手一卡一卡且所有手牌动态可打出高亮缺失到整段结束；质疑所有动作必须等动画播完的设计；佳酿饮用整栏消失且缺瓶身旋转。PM直接读取原文。Implementation初步核对r9 Runtime0ab4dce1f40a4c55ad5e0547cf86361a的2打开/退出、26原生操作含胜利、74恢复/37历史重播，所比较退出保持，正式汇总待落盘；不能据此认画面正确。整hand/potioncontainer遮罩、未采CardHighlight、遗漏NItemThrowVfx是当前源码缺口。

保留原生动态高亮/已入手牌及无关药水槽的持续显示，补完整瓶身投掷/旋转与人体液体；高亮视觉不开放观众真实出牌。纯历史展示Step/Back/Pause/Stop可接收请求，有序结束/退役当前专有展示再到请求目标，执行互斥而非全段禁用；不丢输入/多走节点或重派业务。真实原生业务不能强退到未定义半态，请求到下一安全可恢复边界处理并显示等待，不以此锁住纯展示。旧Task/TCS/池归属和严格状态/RNG等约束保持。

用户提出缺高亮可能解释卡顿感觉，是有价值的待验证解释；既有129/303ms准备等真实耗时仍留存，视觉连续性与实际阶段开销分开分析，不据此认性能已过。直接同包修显示层/瓶身/动画中断和导航接收，完成有效候选再最短原片比较，无需重录/旧矩阵/新立项或审批。D-109/110原目标不删减，整体仍未验收。

D-111/build-010进展：01955728…6E840C9C已安装，构建/生产导航与既有显示归属/路线合同、原片兼容读取通过，没有原生构造/010画面或性能实测。高亮/投掷Sprite按实际采样，保留药水槽背景；process采样最多120Hz；历史展示接单次意图后有序交还，Back边源/Step下一/Pause停目标/Stop清意图，业务安全边界处理。恢复/校验改直接HUD端点并复用能力节点。r9正式1743事件/Godot和54存档/原片保持归档，UI中位44.0886/max345.7579ms、Data中位1.7768/max41.3647ms等为旧阶段指标，不是010改善。009 pair/settings/selector及010源码合同保留build-010-f4cbcfc1；下一仅高亮/移动中Back-Pause及佳酿瓶身/其余槽窗口Stop，不重录/整场或提前验收。

D-111/r10反馈：用户认为010上一组问题似已修，新增/细化编号幻影挤位、秘密武器入场瞬移/仅几帧移动、编号/免费绿色0不同步及重锤死亡HUD/意图到末尾才更新；首次是否同样未知，不要求补测。Implementation初步核对3227事件/10原生操作到重锤/233到达/189恢复/90显示及15意图drain，退出比较保持，正式汇总待落盘。源码缺口为Label变换/插值不足、cost由冻结模型重算、64ms/3帧的choice-open采样被Snap截断PlayPileTween、漏父StateDisplay和intent。当前同包补实际呈现字段/在途运动/死亡HUD，不推定真实多第8牌。D-111导航中断与安全业务边界保持，不为保尾加固定/全场动画等待；不新立项/重录/旧版补测或提前验收。

D-111/build-011进展：D7621D3F…4D43144候选已安装，构建/合同/原片读取通过，未native011画面/性能实测。补编号完整变换、实际费用色/字、当前来源PlayPileTween保尾与Hand.Add边界采样，以及父StateDisplay/intent私有观察回放，D-111意图接收/安全边界保持，不增固定/全场动画等待。r10的3227事件/Godot、85池检查无disposed及54存档/原片保持已归档；准备中位42.5136/max259.0961ms、UI中位25.4427/max344.9139ms、Data中位2.0801/max39.9862ms路径不同，不认严格性能改善。010 pair/settings/selector及011源码合同留build-011-806220f2，下一只秘密武器/能量药水/重锤HUD短对照Stop，不重录/胜利/旧版补测或提前验收。

## D-112：Step接收与执行分离，统一展示时钟并优先通用原生显示

日期：2026-10-28，Asia/Shanghai  
状态：Accepted；CONTENT-002同包修正，整体未验收

用户在Implementation确认011上一组问题修复、首次循序基本正常，指出历史攻击药饮用到开窗间隔错误、能量图标动画缺失、首回合结束中Step令回合计数错；明确希望历史如首次等待结算边界，并询问从根源复用原生函数而非逐药人工模拟。PM直接读取原文和正常退出确认，纠正先前将“可接收Step”扩为“结束当前展示跳尾”的解释。

首次/历史统一：可提前接收一次Step意图，当前段按正常时钟到原生等价选择/结算边界才执行下一步，不因Step快进/跳剩余cue或早增回合。选择等待本身是可续接边界，不以整个宿主Task结束作前置；Back/Pause/Stop仍按安全规则有序处理，不扩强退业务。接受与执行状态明确，严格结果/RNG及只读保持。

choice-open放入同一原生等价时间轴，不在尾播完再Show/重复WaitReady；必要原生初始化/防误点仍留。目标Data可内部提前准备，能量/回合/HUD不可提前露末态。优先源码审查后复用通用纯显示入口（NEnergyCounter能量变化为候选），服务所有来源；混业务的原函数分离显示/适配上下文，不整段重跑，不为保现架构逐药补模拟。允许同包调整采样/UI层，仍不重派业务/改RNG/复活Task。

r11 Runtime0a9e2da1cb194fc388c1769f9ddbaf35当前4035事件、四次Stop12/8/12/12均Error=null为初步信息，最终归档待完成，旧用户确认不认新显示通过。下一有效候选后只攻击药时序/能量变化/首EndTurn期间提前排Step至下一回合稳定Stop，无重录/胜利/旧矩阵/新审批。D-092流程及其他MVP目标保持。

D-112/build-012进展：813EFBF0…527438BF候选已安装，Release/production Step不截尾与marker时序等合同、既有路线/原片读取通过，无012原生构造/实测。Step一次排队到等价边界、choice-open在同钟源marker并行UI ready/余段、Data仅到选择/稳定显示边界提交；能量通用NEnergyCounter纯粒子与原生回合横幅/源round复用，装饰尾不作全场门禁。r11最终4035事件/Godot、44操作/250到达/199恢复/83显示、101池检查和4次退出/54存档/原片保持归档，无胜利；19次历史截断含多类导航，不全算Step。011 pair及012源码合同留build-012-860df0e5。下一只攻击药时序/能量orb/EndTurn提前Step边界与回合显示Stop，不重录/胜利/旧矩阵或提前验收。

D-112/r12反馈：用户确认012上一组时钟/能量/Step修好，但历史末端高亮/敌人意图重启、选择窗层级/暗幕和秘密武器入手仍未过。初读Runtime498dcf60540e49849feed018a4c5ff1d当前3009事件/完整15步胜利Stop及比较进度保持，用户退出确认，最终归档待完成，不据此视觉通过。commit无条件SetNextState/UpdateCard/UpdateIntent与Renderer/PotionRoot Z1000是交还/层级缺口，当前同包统一修末帧交还与原生overlay，保留D-112时钟/边界。62样本仍有1498.5681px单跳/143.5559ms gap，复制/全投影阻塞可能扰动native delta*7，因果贡献待核实；不能将受观测影响的轨迹当未扰动基准，稳定点预备模型亦须看是否只搬移卡顿，不任意慢飞掩盖。不重录/补旧版/新增审批或全场平台，下一候选尚待。

2026-10-28，D-112缺陷命名/范围更正：用户纠正上一轮剩余问题是秘密武器而非探寻打击；此前013/014反馈中将该窗口/入手卡顿写作探寻的当前解释以此为准，原始事件/ModelID不改，历史r8探寻对象池故障及009修复仍按原证据。014意图显示用户确认正确，保留同原生NIntent路线，不退回旧影子；现修其他9牌让位硬写、Add前不可见起点、边界Snap截断在途运动和手牌/出牌层级。TopLevel绕层级是待核实解释，不认静态修好。r14初读5384事件/7次正常Stop12/5/5/1/2/2/2无胜利，最终归档待完成，旧数据正确不等于整体通过。保持D-112业务边界，不等全部动画/逐卡特判、重录/旧版补测或新审批。

## D-113：CONTENT-002按已测范围收束，保留可用版本并结束本轮

日期：2026-10-28，Asia/Shanghai  
状态：Accepted；DONE_WITH_LIMITATIONS；今晚不追加游戏测试或新任务

用户最终确认‘终于，终于，全部完成了’，要求阶段收束、PM与Implementation交流，随后休息。此前020图标大小的待确认项据此转为用户体验通过，不再要求重复测试。保留build-020、原片/原始事件、源码、恢复点和重要失败；Implementation只整理报告/归档后停止，PM维护验收/状态。

确认范围为已测单场组合中的原生选择界面、默认最终决策路线、同路线Step/Back及真实状态恢复，以及本轮历史动画/显示问题的用户验收。D-109至D-112语义保持：原始事件不删、历史不重业务/不改RNG、Step收请求后正常等语义边界，不快进跳尾。用户确认与机器证据分别记录，各build通过不外推为020全矩阵。

本决策改变完成状态，不宣布全部卡牌/遗物/药水、整局MVP、任意seek、通用文件强记录或发布性能完成。当前连续选择路线/普通选择中Stop等缺少专门复核，作为后续覆盖条目，不追加为今晚收束门。下一仍以战斗内容覆盖、按共用机制逐步解除角色/首战限制为方向；具体下一包下次确定。

## D-114：恢复内容覆盖主线，先打通猎人首战与连续选择/生成牌

日期：2026-10-28，Asia/Shanghai  
状态：Accepted；CONTENT-003启动，尚无新增运行通过

用户休息后要求继续今天的工作，D-113的当晚停工到此结束；CONTENT-002验收不重开。沿用户已确认的全部卡牌/遗物/药水覆盖优先级，PM选择猎人作为下一个角色切片：当前LiveState.Supported和Codec.Validate确有铁甲硬限制，猎人起始牌/开局遗物、连续弃牌及生成小刀可检验统一通路是否可复用。角色/组合为PM本轮实施选择，不描述为用户已确认全部细节。

同包打通录制资格、校验/重建、状态恢复与角色显示，保持铁甲既有路径；用Burst→Survivor和BladeDance→Shiv实际组合补当前路线连续选择及普通选择中Stop证据。启用范围先铁甲＋猎人，仍限单人A0/首排普通战斗；不把新角色自动纳入STATE001固定权威文件点，也不同时实现其他角色、宠物/召唤、整局或发布性能。具体准备和最短测试由Implementation按真实原生逻辑安排，普通bug直接迭代，D-092/D-109至D-112保持。

## D-115：CONTENT-003按固定猎人组合验收收束，Back高亮直接到位

日期：2026-10-28，Asia/Shanghai  
状态：Accepted；DONE_WITH_LIMITATIONS；保留build-004，无待用户操作

结合Implementation记录的用户逐项确认与实际事件，PM验收猎人首战组合：001真实录制，002完整观看/连续选择和生成消费导航/FromHand选择中Stop与重开，003碎裂及004Back高亮分别通过。PM独立重计r2/r3/r4的关键事件和关闭，不将早期完整胜利当最终004全矩阵。源码、安装、两原片、恢复点及重要失败保留，不再为本包追加测试，未启动下一包。

用户新增体验要求已落实：Back到目标节点时原生手牌高亮直接到位，正常录制、首次前进及历史Step保留原生过渡；完成已有Tween终点，不另写业务可用性/颜色。D-109至D-112的最终决策、真实恢复、只读、历史不重业务及Step正常时钟保持。

只确认固定准备组合，未另测无准备猎人、任意并发/嵌套、全部grid/bundle Stop或特殊高亮；不宣布全内容/角色/整局、通用文件恢复或发布性能完成。下一优先规划角色专属资源覆盖，储君星星/锻造作为候选，先核实实际缺口再签包，不作为已经调查或实现的事实。

## D-116：实施储君资源/可变卡牌组，同时持久化操作时间

日期：2026-10-28，Asia/Shanghai  
状态：Accepted；CONTENT-004启动，尚无新增构建/运行通过

用户明确批准按建议合并储君资源和可变卡牌，并顺便补操作时间戳。CONTENT-003保持关闭，现沿原工程执行CONTENT-004：星星获取/支付、首次/累计锻造、生成/复制牌实例与动态状态/费用，相关遗物药水按共用来源取样。复制及多张剑的联动以原生规则/真实录制为准，不预设独立变化或手写牌效。资源字段已存在不当作端到端通过。

把普通出牌/用药/EndTurn接受及语义完成时刻按明确单调录制基准写入录像，与现有选择时间对齐，读回核对并以一次人工停顿说明数据真实；旧片无时间标未知。暂不在本包实现原始节奏模式、变速或时间轴，既有Step/Back语义保持。

优先一至两份真实组合，Implementation可直接调整相关hook/格式/录制/状态/显示、备份安装并请求人工测试，普通bug同包迭代，无先行INV/阶段审批。仍固定版本单人A0首排普通战斗，旧STATE铁甲文件点不扩；充能球、单位增减、连续房间/整局与通用文件强记录另行推进。D-092及人工游戏边界保持。

## D-117：累计Step、打通普通牌连续入队，CONTENT-004按已测范围收束

日期：2026-10-28，Asia/Shanghai  
状态：Accepted；PLAYBACK-001启动，尚无新构建/运行结论

用户明确选择连点累计步数：每次Step计一个现有操作/选择节点，连点3次累计走3节点，包含正在执行的请求，不吞额外点击。Pause/Back/Stop清掉尚未提交的前进请求；已经入原生队列的业务按安全边界处理，不承诺任意在途瞬时撤销。到片尾/Stop/重开清余量与旧回调，不把请求带入新会话。

本条修订D-112的一条提前意图上限与过度串行提交，不撤销正常动画时钟、选择/回合业务语义和D-115 Back高亮。下一实现先做两张普通牌真实连续入队，录制归属/接受完成时间、原生执行、首次/历史展示和恢复一起处理；不能只给按钮加计数。是否能在原生重叠入队时保留每个可Back节点尚需实际验证，不写成已成立。

CONTENT-004储君资源/可变卡牌与时间落盘按已测13操作组合DONE_WITH_LIMITATIONS，保留008；PM验收见evidence/content-004/project-lead-review-2026-10-28.md。用户“目前无明显bug”不外推全内容/性能通过。新的输入队列/播放手感由PLAYBACK-001承接，避免旧包无限扩展；不新增节奏模式、时间轴或全部并发框架。Implementation沿D-092自主相关改动/普通bug迭代、备份安装及人工实测，不再先行INV/逐阶段批准。

## D-118：先细化局内播放体验，Pause让已开始动作自然收尾

日期：2026-10-28，Asia/Shanghai  
状态：Accepted（讨论优先与暂停原则）；其余具体边界仍待定

用户要求寻找看起来自然、用起来简单且实现简洁的局内播放效果，暂不急于推进代码。Implementation保持暂停；003结果未反馈，不作结论。用户在两种暂停体验中明确选择“已开始的动作自然收尾后停住”，录像控件、查看卡牌和滚动列表能力保留，不要求立即冻结当前动画帧。

2026-10-29补充：用户认可A正在结算、B仅排队时Pause让A已经开始的效果/自动连锁运行完，B不开始、等继续播放；背景/idle等纯装饰不必全冻结。卡牌自然收尾原则已确认，不能以排空全部原生队列或全部显示节点销毁代替。原生挂起/继续的技术可行性尚需验证；敌方回合、跨阶段/选择的具体边界仍待细化。Stop/Back如何处理已提交业务另定，不将Pause规则自动套用。

同日敌方回合补充：用户接受当前敌人完整行动后暂停和整轮结束后暂停两种粒度，明确交由实现按便利选择。优先稳定并复用原生边界，不需要再次就这两个选项请用户决定，也不细分每段伤害作为硬要求；尚未技术核实哪种更简单。

用户同时建议参考多人模式的原生排队动画；优先复用原生业务/队列/显示并减少不必要改动，以满足自然、简单和简洁实现。此为实现取向，不把最少行数当硬约束，也不启动多人Replay。Step/Seek精度、跳转后的播放状态及节奏细节继续讨论；D-117累计偏好保持，Implementation继续暂停，不把产品确认当恢复实施授权。见technical-notes/PLAYBACK-EXPERIENCE.md。

## D-119：确认导航体验，启动可续播状态与原生前进验证

日期：2026-10-29，Asia/Shanghai  
状态：Accepted（产品体验与验证方向）；SEEK-001启动，最终恢复架构尚未定稿

用户认可Step正常展示到下一个操作/选择节点、点击累计，Back直接恢复上一节点并暂停，Seek直接呈现目标状态且默认暂停；选择沿原生实际打开/选择/确认，无独立确认不人为增加步骤。D-118的自然收尾Pause、A运行完而B仅排队不开始、敌方停点由实现便利选择保持。最终任意位置定位目标不缩小为只能到几个已看节点，精确帧恢复/缓存策略仍需验证。

用户问下一步，PM沿持续授权签发SEEK-001，结束新包的讨论HOLD，先证明跨回合与选牌相关状态的恢复后原生续播；不继续旧历史显示系统逐动画补丁，也不先搭建整局预计算/无头平台。可先少量会话内状态索引与可选新路线，保留旧源码/pair；真实验证后决定默认迁移，不因产品接受就称最终架构可行性已全证。

新路径修订D-110/D-112/D-117中旧历史纯显示“不得重新派业务/RNG不变”的适用范围：恢复后的原生前进正常执行牌效并消耗RNG，核对的是每次从相应恢复状态出发与记录得到同样结果/RNG，不能重复计入或复活旧Task。观众仍只读、录像决策不变，普通进度隔离保持；旧路径历史验收原结论不改。

PLAYBACK-001停放并由新方向接续，未整包验收；003用户已测但结果未反馈仍Unknown，不强求补测/补报才允许新包。暂停期间保留的源码与文档作Git检查点；相关普通bug/重构/备份安装/人工实测沿D-092自行推进，无新增INV/阶段审批。

2026-10-29 r1复核补充：用户在Implementation确认SEEK-001/002基本符合预期，同时报告弃牌Seek显示/等待、暂停后不能继续有限Step队列等问题，要求先区分代码和架构、不急修，可必要时调整交互。固定片段数据/原生续播已有证据，不认视觉/交互全通过或关闭整包；当前不换DLL/追加测试。需要区分有限请求与无限连续播放，具体按钮/恢复交互和选择Seek速度策略继续讨论；不自动缩减选择节点或认多牌排队已通过。

## D-120：r1后先修显示与队列交互，选择Seek短等待暂可接受

日期：2026-10-29，Asia/Shanghai  
状态：Accepted（下一轮优先级）；本次选择不自动启动新测试

用户在“先修显示/交互、短等待暂接受”和“优先验证更快选择Seek”之间选择前者。保留精细选择节点与已证的统一原生前进，不为加速自动吸附到其它节点；当前样本约1–2秒、个别约2.4秒的选择相关恢复等待可暂作过渡，不能外推任意长等待或认发布性能通过。

下一修复范围是保持重建场景逻辑可见并正确刷新选择手牌布局，以及满足有限Step队列暂停/继续的用户期待、明确区分连续播放。暂停应保留有限余量，继续有限队列做完后停；Seek/Back清旧路线请求并在目标暂停。具体按钮拆分/明确改名由实现选择，避免隐式变无限模式，不将普通UI选择再变成审批。

暂不重构旧上下文退役/目标重建策略、不移植多牌lookahead或扩大测试矩阵；实际多牌原生排队仍未验证，不以三击选择节点代替。当前只整理同一SEEK-001的下一轮修复目标，未派新测试或换版；这不是恢复过去逐阶段审批制度。普通后续迭代沿D-092进行。

## D-121：播放/暂停与连续播放模式独立

日期：2026-10-29，Asia/Shanghai  
状态：Accepted（控制模型及Step排队语义）

用户补充：播放/暂停控制时间是否继续推进；播放表示继续运行，有有限Step就执行有限Step，连续播放打开时也继续。连续播放是可开关的独立模式。采用此模型，覆盖D-120把“继续有限队列”和“连续播放”作为两种播放命令的提议；不需要为两种队列各造一个恢复按钮。

概念上区分运行/暂停状态、连续播放开关、有限Step余量。Pause保留余量和模式，Play仅恢复运行、不隐式开启无限播放；连续模式开关不自行解除暂停，有限请求与自动推进不能重复提交同一节点。用户随后明确选择：暂停时Step只加入队列，按Play后才运行。Step本身不改变运行门或连续模式；这修订D-119“点Step立即推进”的按键行为，执行时正常展示/选择停点与累计原则保持。

连续模式关闭时仅执行有限余量，用完停在当前节点等待新请求，不自动变无限播放；队列耗尽不应暗改独立运行开关，因此已经处于运行状态时新Step可自然执行。UI应清楚反馈运行/暂停、连续开关与待执行数量，避免暂停点Step看似无响应。未新增立即单步越过暂停的例外模式。

已开始效果仍按D-118自然收尾，不因“时间暂停”措辞改为立即冻结所有帧。Back/Seek清旧路线请求、到达默认暂停；连续模式不能使跳转后自动运行。具体控件布局可选播放/暂停控件加一个连续播放开关，不绑定像素/UI方案。本次为产品细化，未派代码/安装/新测试；D-120修显示/交互优先级保持。

同日实施恢复：用户在上述控制规格收束后明确要求“按你说的继续”。恢复SEEK-001 r1窄修复，沿D-120仅修显示/控制、保留短等待和精细Seek；可直接实现、构建、必要备份安装及请求人工短回归。此前产品问答的“不自动启动测试”不再作为本轮HOLD，不新增普通迭代审批，不扩大到快速上下文/多牌lookahead或补旧003结论。

## D-122：SEEK-001固定猎人切片阶段验收，保留003并继续整体架构讨论

日期：2026-10-29，Asia/Shanghai  
状态：Accepted；DONE_WITH_LIMITATIONS（猎人前4宿主/9节点），未启动下一任务

用户在Implementation确认测试结果符合预期并转回PM讨论。PM核对实际用户消息、003/r2摘要的控制/关闭/限制项，结合先前关键源码阅读，接受所测原生恢复续播、选择布局和D-121控制回归；未重计626原事件、重复构建或追加测试。002与003证据分别保留。

003/r2有16次原生宿主结果/RNG核对、14次匹配且暂停/清旧余量到达、7次选择布局检查；两次Stop在选择节点4和短重开节点1，无PendingHost，控制全清，原manager及所比较Continue进度恢复。用户体验确认与这些断言分开记录。

仅固定切片/所测路径：Auto与有限量同时非零去重仍只有生产合同，本轮自动派发有限量为0；Auto Pause在末端8之前；003仅一次EndTurn，不把002多次结果改写为003。不认多手动牌原生提前入队、整场胜利、未访问点、通用文件Seek或发布性能。选择重建约1.8–2.4秒、离开选择的旧宿主排空仍存在，按D-120暂接受，不承诺瞬时恢复。

保留SEEK-001/build-003（1D2A4848…7AB919）和原片/源码/失败，本阶段不再改码/换版/追加测试，不补判旧PLAYBACK003。下一由用户/PM讨论稳定锚点、选择局部重建、未访问索引与真实多牌队列如何衔接；尚未签新包或定稿整局预计算/最终恢复架构。

## D-123：完整玩家操作排队与整项回退，选择作为操作内部过程

日期：2026-10-29，Asia/Shanghai  
状态：Accepted（Step及Back操作单位）；未派新实现

用户明确选择将Step改为下一次完整玩家操作排队：出牌、用药、结束回合等。牌内的选择窗口继续原生展示、按录像自动选择/确认，不再把选中/确认分别消费一个Step；暂停仍可浏览。修订D-119/D-121中Step以选择子节点计数的单位，保留累计、只排队及运行门/连续开关独立原则。

一个请求在开始该操作时消费一次；已开始操作的选择与自动效果属于同一次运行，不需要额外Step，也不能因为Auto关闭或有限余量已为0而卡死当前操作。显式Pause仍能在选择等待处停止自动回应并保留窗口，当前计算中的效果自然收尾。Play先继续这项未完成操作，再按剩余队列/连续模式启动后继。

暂停Step应加入可见的操作条目并参考多人队列动效，不只是计数；排队UI可反馈，牌效/业务不提前执行。待播操作与原生已经接受的GameAction分开，未来生成牌/选择依赖未就绪时不能强行提交。优先复用原生队列显示，具体能复用到哪层尚未验证。

用户随后明确选择Back也按完整操作回退：当前牌效/选择尚未完成，撤回这一整项操作至开始前；操作已完成则撤回最近完成的一整项。Back清旧路线待播请求、到达暂停，不逐个撤销选中/确认。基础恢复锚点对应完整操作的稳定边界；选择阶段仍可暂停阅读，时间轴更细定位另行保留，不自动变为不可定位。

SEEK-001/003按旧已测范围验收保持，D-123是尚未实现的新交互；不声称该版本已支持整操作队列/Back，也不因此承诺瞬时退役正在选择的旧宿主。当前未派新代码/换版/测试。

## D-124：选择不占Step/Back节点，原生退队后自动恢复待播出牌

日期：2026-10-29，Asia/Shanghai

状态：Accepted（产品交互）；尚未实现或运行验证

用户补充并认可：选牌界面不再进入Step操作控制范围，Pause仍可控制；Step和Back只控制关键节点。关键节点沿D-123指完整玩家操作（出牌、用药、EndTurn等）的边界，不将打开、选中、确认各立一个导航步骤。进行中的选择按录像自动回应；显式Pause可停留浏览，Play继续当前操作，不额外消费Step。选择中按Back仍撤回当前整项至开始前，不逐个倒退选择。

原生因手牌选择取消未执行出牌时，允许播放牌退回手牌的过程。选择及当前操作必要结算完成、原生可接受出牌后，自动将先前已预约但未执行的后继按原序恢复到出牌队列；不需观众再点Step补回。原生取消不算已播放，不重复消费请求或执行牌效，也不将未预约的新操作擅自加入有限队列。后继牌身份/合法性须以结算后的真实状态解析，不提前挪走选择候选。

恢复排队与开始业务分别处理：Pause仍阻止后继执行，Auto关闭且无有限请求时不自动增加播放量。自己的选牌界面中Step只预约后续完整操作，不提交选择答案或强行挪实际手牌。三选一等仍沿各自原生取消策略，不统一强造退队动画。

此决定收束上一轮原生机制提案，细化D-121/D-123，未缩小未来时间轴细粒度定位目标；配时/聚焦视觉仍待实现选择。已确认原生静态分支，回放取消归属、重入队和Back退役仍需验证。下一最有价值的功能验证为“入队→手牌选择退队→录制选择→自动重入队→后继执行”，并覆盖选择中Pause与整项Back后重播；当前未派新代码、安装或游戏测试。

## D-125：分层前进，选择阶段仅供精细前进而非直接Seek

日期：2026-10-29，Asia/Shanghai

状态：Accepted（导航粒度与选择边界）；图标、具体停点和调度细节待细化，尚未实现

用户提出最终播放器区分“下一个精细操作、下一个出牌动作、下一回合、下一层”，暂以`>`、`>>`、`>>>`、`>□`表达。用户明确精细操作包括选择窗口的打开、选中、确认；同时明确这仍不提供可直接Seek到的选择页面节点。符号只是候选，不视为倍速功能或当前版本已有四项能力。

精细前进只推进当前存活的原生流程至下一个有意义阶段；选择阶段可暂停查看/继续，但不因此建立独立可寻址的恢复点，不提供直接跳至该页的Seek入口。原生没有独立确认时不虚构。要重看选择，先Back/Seek至所属完整操作之前，再沿原生流程播放或精细前进到达。

D-124“选择不算Step节点”限定于普通完整操作Step及Back；另增精细前进模式，不恢复所有控制共用选择子节点的旧模型。普通完整操作仍包含其内部所有选择；Back保持撤回完整操作，不随精细前进逐项撤销选中/确认。Pause/Play独立、暂停只预约的原则保持；精细/完整/回合等请求混排和Auto下的目标停留方式须在实现前理清，不能把新模式直接混入旧整数Step计数。

下一回合/楼层是最终分层导航方向，确切落点及自然播放/跳转展示细节未锁定；当前仅单战斗原型，楼层导航未实现。D-124原生退队→按录像选择→必要结算完成→自动恢复已预约后继的规则保持；选择记录仍需保存并用于原生回答，即使它不是Seek目标。

此决定收窄旧讨论中要求独立选择页面Seek的范围，旧SEEK-001/003选择节点实测按历史原范围保留。下一实现候选优先当前战斗中的完整操作/精细前进与退队重排衔接，不把四个图标解释为立即扩到整局。当前只更新产品文档，未派实现/安装/测试。

同日实施恢复：用户在D-125收束后明确要求“我们做一下”。签发[PLAYBACK-002](handoffs/PLAYBACK-002.md)，现有Implementation从已验003继续，解除本目标讨论HOLD，沿D-092自主相关代码/备份安装/人工短测。先当前战斗完整/精细前进、整项Back与真实原生退队重排，回合/楼层按钮留最终方向，不立即扩整局。旧结果不追认，新交互待验证。

同日控制细化（PM接受Implementation方案，尚未原生实测）：显式精细目标到达且无后续显式请求时，运行门明确变为暂停，Auto值保留，供观众观察；存在后继显式请求时依FIFO继续。普通完整操作请求量尽仍按D-121保持运行门等待，不一概自动Pause。UI和日志明确表达精细观察暂停。

精细暂停后，若当前宿主尚未完成，单按Play即可继续该项至结束，不需要再预约Whole/Fine或消费新Step，也不执行下一张未预约牌；存在显式后继目标时尊重其FIFO。若已在完整边界、Auto关且无请求，则Play保持等待，不凭空启动下一项。PM在首版源码发现前一种状态缺少continuation授权，已要求同包修复并补最短验证，不另开调查或扩大范围。

同日修复落地：PLAYBACK-002/build-002以Continuation目标补齐精细页直接Play，7组离线生产规划检查通过。PM核对源码、对应判别合同、报告与安装DLL身份，接受进入首次人工短测；原生取消重排和实际控制仍待验证，不以此记整包PASS。

## D-126：暂停时真实预入队，显式Seek保持运行意图

日期：2026-10-29，Asia/Shanghai

状态：Accepted；PLAYBACK-002同包立即窄改，待验证

用户在Implementation实测后明确提出：“暂停时应该让step的卡牌进入排队队列”“seek后不修改播放和暂停的状态”，并要求与PM交流是否立即改。PM读取该真实用户消息后决定立即在当前包实施，不另建任务或分阶段审批。

暂停只限制业务开始，不禁止原生接受已预约出牌。在非选择、身份/合法性满足的手牌边界，将FIFO中可接受的连续完整出牌真实送入原生NCardPlayQueue；保留请求直到真正开始/完成，不扣资源/执行牌效。未生成/不合法、精细目标等阻断项保留待播，不越序伪造入队；合理预提交窗口可先保持，UI区分真实入队与尚待提交。自己的手牌选择中不挪候选，退出选择并完成当前必要结算后再恢复后继，沿D-124规则。

显式Seek记录请求时的Running/Auto意图，清旧路线Active/Pending并安全退役旧原生提交；恢复过程暂时阻断业务，到达后保留这两个值。运行且Auto开则继续播放；运行但Auto关且请求已清则在边界等新请求；暂停则仍暂停。保留的是运行意图，不是旧位置的待播计划。恢复意图必须贯穿调度/恢复层，不能仅删一个Running=false而保留其他重置。

本次只修改显式Seek。Back维持已确认的整项回退并暂停、保留Auto、清旧路线，这是PM选择保留的既有交互，非用户此次另提要求；共用恢复入口应区分导航意图。精细末目标的观察暂停/Play续当前保持。新规则覆盖D-119/D-121等旧“Seek和Back都到达暂停”中显式Seek的部分。

r1按所测范围接受：用户总体确认；摘要867事件/2 opens，48次结果/RNG匹配、10选择、20匹配到达、8次提前接受，两Stop无Error/Outstanding且原manager/所比较Continue进度恢复。提前接受8的更正取代初次消息误报0；选择取消/归手/重入队仍均0，不能认完整闭环。精细仅无选择操作4，选择页精细/直接Play与混排仍无真人证据。PM读更正后摘要及真实用户消息，未重计全部原事件。

下一最值验证合并为：暂停三完整真实入队且业务不开始；19→20/21/22选择触发退队/重入队并只结算一次；选择精细后仅Play；Seek运行/暂停保持与相关Stop。不把Stop/Seek退役产生的取消算作选牌退队成功，不补全套旧矩阵；普通失败同包修。

实施准备：003已安装（源码891e5ff），9组生产规划/原片检查通过；PM核对暂停预提交与执行过滤分离、Seek请求/恢复意图、报告及检查产出，并读回安装身份一致，未重复构建。新行为仍待003人工短测，002/r1的普通ahead证据不挪作003通过。

## D-127：PLAYBACK-002固定猎人片段阶段验收，保留003

日期：2026-10-29，Asia/Shanghai

状态：Accepted；DONE_WITH_LIMITATIONS（原猎人前22操作及所测控制/队列链）

用户在Implementation确认“整体比较符合预期”。PM读取真实用户消息、003/r2摘要和报告，核对原生入队动画源码；不重复构建、重计全部806事件或要求追加测试。保留当前003和源码891e5ff，结果文档a879a56已推送，不改变动画/DLL。

关键闭环首次获得实证：同generation2的20/21/22暂停接受原生ID30/31/32；21手牌选择取消尚未开始的22/ID32，Retiring=false、StillReserved=true，原生归手已验证。21的连续选择及必要结算完成后，22以新ID35/Attempt2沿同一Whole请求25重新提交；同代ID35匹配完成仅一次，结果/RNG正确。其余4次取消是退役，不计为选择退队。暂停只接受不执行业务、退队不丢请求/重扣、后继自动重排在此链成立。

摘要为806事件/2 opens，57接受、11次暂停接受且业务未开始、52次原生结果/RNG匹配、16选择、11次Seek/Back到达状态匹配；显式Seek四种Running/Auto组合都有记录，Back仍暂停；21的Continuation完成两次，支持精细选择页直接Play续当前。两Stop在完成20/9，无Error/Outstanding，控制请求清空，原manager及所比较Continue进度恢复。原片不变，game0。不将这些Stop描述为原计划的固定短重开或全场胜利。

入队动画使用原生NCardPlayQueue及其约0.35秒Tween；回放不执行前置鼠标拖牌准备，所以起始观感不保证与手动拖牌逐帧相同。用户只询问动画来源，不据此追加动画重写或测试。

验收仅固定前22、所测原生队列链、精细/整项控制及已访问边界恢复；不认所有卡牌/所有混排、完整战斗、未访问索引、文件通用恢复、楼层/整局或发布性能。旧002/r1的未证结论不反写，旧PLAYBACK-001/003仍Unknown。

当前任务阶段收束，无新增代码/人工要求。下一最值得推进的是将新原生播放路线从固定片段推广到完整单场闭环，再接回既有内容覆盖；先消除硬编码原片/22项切片限制并覆盖胜利收尾/Back/Stop，不先做四个导航按钮或扩楼层。此为后续建议，尚未签发新任务。

## D-128：完整单场实现与战斗内容覆盖调查并行

日期：2026-10-29，Asia/Shanghai

状态：Accepted；PLAYBACK-003按所测单场范围完成，PM覆盖静态材料已交

用户明确要求两个任务一起做：Implementation开始近期完整单场任务，PM调查“覆盖所有战斗内容”需要的材料，也可直接询问用户经验。签发PLAYBACK-003给现有Implementation，保留已验003为起点；不等待覆盖研究完成、不先开新INV或另一个实现对话。

近期玩家目标为受支持范围内新录一场→正常入口→新核心完整原生播放到胜利→整项Back/续播→Stop/重开。移除固定录像/22项限制，维持已定控制与独立会话，不自动扩到后续房间/全角色/全部内容。普通相关修复、构建、备份安装及人工短测沿D-092自主。

PM覆盖调查基于固定v0.111.0本地反编译与既有覆盖笔记，按共用机制和真实样本整理代码限制、未测通路、旧/新路线覆盖差异；需要时询问用户易准备例子，不把经验当动态验证或逐牌测试清单。PM不碰Implementation源码/安装/游戏操作，结论写回项目文档；发现旁支只登记，不擅自扩本包。

同日r1进展：build-001参考猎人完整原生战斗及胜利后Back阶段通过。PM核报告/摘要、真实用户确认及封存420原事件：首次1..27连续，31原生结果/RNG匹配，4真实胜利Task/奖励前投影匹配，三次Back26状态/RNG及暂停清量正确，两Stop27/1无Error/Outstanding且原manager/所比较Continue进度恢复。原片与源码身份沿报告保留，结果文档e3857d0。两次均hunter-reference，无新录recent证据，本包不关闭；下一仅补新文件普通入口闭环，不追加旧片矩阵。用户正在正常游玩，保持当前安装/配置/进度，待其方便退出先保留新增进度。启动/退出引擎诊断保留，不外推全部胜利形态、初始选择、全内容或发布性能。

2026-10-30同包修复澄清：用户反馈新录基本符合预期，但暂停Fine“>”小刀缺少入队动画。PM核build-001 FillNativeQueue仅接受Whole；无选择且Fine目标恰为下一普通牌完整边界时应沿原生合法性真正入队，Play前仍不付费/触发效果，Play后到Fine目标暂停/保留Auto，后继不越过Fine扩大ahead。按路线判断，不按牌名特判；选择/EndTurn/药水阻断保持。本次是D-125/D-126的反馈一致性修复，沿D-092原包直接处理，不新发阶段审批或要求重录；新录主链报告待证据封存/PM复核，本包尚不关闭。

同日混排更正：用户认可build-002单独暂停Fine入队，但反馈Fine后Whole未预入队。上一轮“不越Fine扩大ahead”由PM写得过严，现更正为不拉入未预约后继；已存在的连续同回合合法显式请求可沿原三项窗口预入队，不在每个Complete Fine处截断。最终Fine无后续显式请求仍暂停/保留Auto，有后续显式请求则FIFO继续，符合D-125；预排队不额外生成Auto目标，内部选择/同宿主多阶段、原生合法性及Pause业务门保持。Implementation可沿原包直接修复/短测，不新发审批或重录。该更正覆盖上一轮冲突表述；已有新录主链验收保留，混排修复尚未通过。

## D-129：仓库改为公开，继续完成战斗覆盖的静态材料调查

日期：2026-10-29，Asia/Shanghai

状态：Accepted；仓库public与本轮覆盖静态材料已完成，动态覆盖按后续功能验证

用户明确要求将仓库改public，覆盖之前私有要求。PM检查当时677个跟踪文件/19次历史中的排除材料路径与常见凭据格式，未发现命中；以既有管理员凭据修改可见性，API确认public且关闭凭据的Git读取成功。公开范围仍为现有源码/文档，忽略规则、项目许可证、原始存档/日志/游戏缓存范围不变。

用户同时要求把覆盖所有战斗所需信息继续查清。PM沿固定游戏源码和891e5ff静态基线整理实际内容池/调用点、输入/选择、角色资源、单位生命周期、RNG/状态、终局与生命周期契约，并明确剩余动态验证条件。静态调查不能据此宣称所有内容已实测支持；不将“所有信息”解释成必须先穷尽所有组合才让Implementation继续。

PLAYBACK-003完整单场保持当前授权与范围；PM覆盖研究不改源码/安装，不另起大规模实现或扩本包。结论落项目文档与可复用本地索引，后续按共用机制形成可独立实现的任务。

同日输入范围补充：用户就UndoEndTurn回复“多人有，单人没有”。当前不将该动作列为单人MVP必需录制输入，留多人阶段；这是正常玩法经验依据，不冒充源码/动态穷尽证明，后续有真实单人反例再处理，不新增当前测试要求。

## D-130：精细前进仅用于手动播放，到点保持运行等待

日期：2026-10-30，Asia/Shanghai

状态：Historical；此前Accepted，独立播放门及相关入口语义已由后续D-131替代

用户明确提出“>只允许在播放且不auto的情况下使用”，并选择“保持播放，等待下一次>”。因此新Fine请求仅在Running=true且Auto=false时可受理；暂停或Auto开启时按钮禁用，处理入口也拒绝，不新增请求/消费预算/提交牌。此条件针对新请求，模式切换仍保留已经合法受理的FIFO。

到精细目标后保持Running，不因最后Fine自动切Pause；Auto关闭且没有后续显式请求时，等待下一次>或>>，不自动跨阶段、回答选择或启动后继。选择页可停留浏览。主动开启Auto则按连续模式继续，>随即不可用；片尾/Stop仍正常终止。

Pause再Play应恢复该精细等待或原有目标，不能凭空把精细等待升级为WholeContinuation而直接完成宿主、跳过后续精细阶段。此条覆盖D-125/D-126关于最后Fine自动暂停和暂停时新增Fine入队的旧要求；>>暂停整项预约、Back整项回退并暂停、Seek意图保持及Stop清理规则不变。

Implementation停止继续扩展Partial Fine暂停预入队，沿PLAYBACK-003直接调整入口/UI、到达等待和相关恢复语义，做针对性检查与同片短测。已有新录主闭环及旧控件证据保留其历史范围，不因产品变更追认为失败或要求重录；不新增工程/INV/阶段审批。

## D-131：取消独立Play/Pause，收敛手动/自动推进

日期：2026-10-30，Asia/Shanghai

状态：Accepted；PLAYBACK-003按所测单场范围DONE_WITH_LIMITATIONS，当前008，主运行验证来自007/r8

用户提出去掉独立Play/Pause、改为手动等待/Auto自动推进，听取PM说明后要求检查无问题直接继续实现。PM核源码：Acquire在Auto关且无Active/Pending时天然等待，可复用现有原生执行/选择链，无阻断设计矛盾。打开录像默认手动等待（Auto关），>或>>点击即推进，无需先按Play；Auto开连续推进。等待按业务/导航阶段判断，动画时钟照常，不靠所有动画静止判断。

>仅Auto关且手动等待时可用，忙时禁用；UI与处理入口一致。等待包括Fine到点后的选择页，不能因宿主Task未完成或InFlight就禁用。>>点击即请求完成当前完整宿主，完整边界则请求下一项，连点累计显式FIFO；选择页>到下一精细阶段，>>完成其剩余选择/牌效。Fine到点仍等候，不自动补WholeContinuation；开启Auto则按连续模式继续。

关Auto停止自动追加，当前已开始Whole（含内部选择）与已预约显式队列自然收尾后等待；不强截中途，也不默默丢请求。用户已获说明：取消独立Pause就不再提供“执行中停住并保留整条手动队列”的单独入口。Back清旧计划、按完整操作恢复后转手动/Auto关，落点等待；Seek清旧计划并按请求时Auto决定到达后连续或等待。Stop/片尾正常结束，生命周期内部阻断可保留，但公开原生播放器不能有无法恢复的隐藏Pause或旧toggle入口。

Implementation按更新后的PLAYBACK-003直接实现、必要重构/合同检查/备份安装并给用户同片短测，无需等新任务或审批。不扩Partial Fine暂停预排，不重录或重跑已验主闭环/整场矩阵。D-130及更早冲突规则留历史，当前以本条为准；代码方案和普通修复沿D-092自主。

同日收束：PM核007/r8封存732事件与用户“应该是好了”确认，原生结果/RNG、选择、7次恢复/意图、恢复后新卡登记/使用及三次退出/监听清理在所测范围通过，任务DONE_WITH_LIMITATIONS。50次匹配包含1次导航退役中的自然收尾，不称50次独立连续前进；r6/r7失败不反写。用户另明确要求“播放结束后auto自动关闭”，008仅末端清Auto与文案、更新4组终点合同，PM读代码/离线结果后接受，无需重复整场；不宣称008另有原生矩阵。新内容/整局/通用文件与性能目标仍未完成，未自动派下一任务。

## D-132：同包推进五角色基础战斗回放

日期：2026-10-30，Asia/Shanghai

状态：Accepted / IN_PROGRESS；CONTENT-005已授权，尚无新增运行结论

用户在讨论下一内容批次时要求“还有两个角色感觉可以一起做了”，之后明确“好。开始吧！”。此前只安排、不启动的限制至此解除；签发CONTENT-005给现有Implementation，不新建对话或实验工程。

本批以当前单人A0首战为边界，短补通用输入和旧三角色实际缺证（初始选择衔接、储君资源/可变牌及相关遗物药水、真实丢弃药水），随后同包实现Defect的球与Necrobinder的Osty/Doom。旧片和已有通路优先复用，普通相关bug直接修，不把全面旧内容回归当新角色的前置。Osty必要的单位身份/场景/订阅与Doom致死后恢复纳入，其他怪物召援、多阶段Boss、失败局和后续楼层另列。

交付按机制分段，两角色各少量代表录像验证新录/普通入口/原生播放/关键Back续播/Stop重开。记录新状态文件事实、比较和观看恢复各自范围，不把内存恢复或新字段等同通用冷恢复。D-131控制保持，D-092授权相关源码/格式/hook/备份安装和人工短测，不新增阶段审批；用户仍手动启动/操作游戏。

原授权以008为起点，007/r8与008离线小改的证据边界不变，不能因此称五角色或全部卡牌/遗物/药水已支持。

同日A阶段验收：002/r1旧储君完整原生播放/胜利Back及新格式7录制通过，r2同新片recent回放/丢药与锻造Back再做/胜利/Stop短重开获用户确认。PM读真实反馈、报告与封存摘要，r2有16结果/RNG、4匹配恢复和两次干净退出，不重计原事件或重跑矩阵。A按所测范围完成；B/C源码/离线已接入，当前004已安装待新角色实测，无五角色运行完成结论。

用户提出细化开场：第一回合横幅长且阻塞、工具箱期望受>/>>控制，并追问能否一起Back到真正开场。PM核旧横幅等待/Boundary0自动选择，撤回仅重开重看的简化建议，修订方向为开场整体可前进/后退、选择页面非独立Seek。固定源码Toolbox在第一回合BeforeHandDraw、ChoicesParadox在AfterPlayerTurnStart，均先于自由出牌；当前0在其后是实现边界。具体恢复方案/延迟待验证，体验稿未派发代码修改；004新角色测试继续，不追判A失败。

## D-133：当前测试轮后实施开场控制与整体回退

日期：2026-10-30，Asia/Shanghai

状态：Accepted / IN_PROGRESS；候选006已安装，原生运行Pending

用户在开场方案及Back边界讨论后确认“可以。这轮测试结束之后就改这个？”。现有人工轮先按当前安装收束，随后优先实施开场修订，不中途混入换版，也不要求先补完尚未开始的全部角色矩阵。CONTENT-005加入D段，现有Implementation沿D-092直接推进，未完成B/C任务保持，无新INV/逐阶段审批。

目标为横幅纯展示尾不额外阻断业务就绪控制；开场原生选择接手动>/整段>>/Auto，单次开场>>落首次可出牌、不额外出操作1；增加完整战斗开场恢复目标，Back可从首次可出牌继续退开场，开场选择中Back回开场，清计划/Auto关等待。选择页面不作为独立Seek，原生回合/遗物/抽牌先后保持。D-131其他规则不改。

实现需从干净状态重新执行原生开场，核对候选/RNG/实例和初态，不能复活旧Task或用开场后Initial冒充前态。房间前checkpoint可作重建来源，但具体恢复方式/延迟尚未实测；沿现有工程做可用增量和短测，不先造通用半态平台。旧004/A/B结果按原版本保存，不追认为开场新版验证。

同日进展：B的004/r3–r4所测闭环经PM复核，包含三次玩家回合末电球胜利Back后的新信号原生续播，仍不外推C/Doom。D的006候选/源码9698a02已安装：干净重建开场与同一请求控制已接入，原生横幅尾不额外等待；005封存未发测。PM核安装收据及报告/离线边界，不重复构建，开场重入/视觉/延迟尚待人工，C未取样。

## 尚未决策

- 是否在跨版本 milestone 前提供 v0.107 的独立同版本构建。
- Replay 数据格式与长期兼容承诺。
- RunReplays 各部件的复用分类。

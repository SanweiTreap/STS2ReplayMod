历史快照：D-092精简/放宽授权前的项目管理文档；不作为当前执行权限。

# Todo

状态约定：`READY` 可直接交付；`IN_PROGRESS` 已派发在研；`BLOCKED` 缺少输入；`BACKLOG` 尚未排期；`DONE` 已有验收证据。

当前状态（2026-10-26）：A5/001B/DET-001已关闭。D-091验收PERF-001 Phase0测量合同与离线代理，下一步Phase1离线构建最小三模式候选；不安装/配置/读取玩家数据或请求游戏。真实性能、稳态/发布预算仍Unknown；用户先前报告modded selector1/全关闭不作为现场门，个人槽排除。

## DONE — PROD-001：定义单人 MVP 玩家闭环

目标：明确玩家如何开始录制、找到 replay、加载、观看、控制和处理失败，并列出 MVP 排除项。

输入方式：按 `USER_INPUT_CHECKLIST.md` 第五轮 J/K/L 与项目发起人逐项确认；产品选择先形成草案，技术上尚未验证的部分明确标为假设或风险。

验收产物：

- 一条端到端玩家流程。
- MVP 必需能力、可延期能力与明确非目标。
- 每项能力的玩家价值和最小验收标准。
- 由产品选择导出的技术问题清单，作为后续定向调查与实验输入。

完成证据：`MVP.md` Baseline v1.1 已包含端到端玩家闭环、MVP 发布验收基线、明确非目标和技术问题；2026-09-04 补入主菜单独立 Replay 会话硬约束。J/K/L 回答保存在 `USER_INPUT_CHECKLIST.md`，实验依赖见 `EXPERIMENT_PLAN.md`。

## DONE — INV-001：建立 MVP 相关的 RunReplays 参考地图

目标：只针对已选 MVP 能力，把 RunReplays 中可能有参考价值的 hook、command、状态与失败模式映射出来，不提出最终架构，也不做全项目复刻。

交给 Implementation Codex 的输入：

- RunReplays 固定参考源码：`references/RunReplays-may-2026/`，tag `may-2026`，GitHub 显示短 commit `05b0edd`，manifest `v0.2.0`。
- 当前目标游戏身份：public-beta `v0.111.0`、commit `41cef1ea`；v0.107 只作为已有历史证据，不要求切换环境。

工作范围：

1. 记录源码来源、版本标识、许可证、构建方式和依赖。
2. 只定位 `SPIKE-001` 所需的 run 生命周期、玩家语义操作、RNG/初始状态、结构节点、状态保存/恢复、UI readiness 和已知热路径 I/O；未进入最小探针的能力不调查。
3. 对每项注明对应文件/符号、已观察行为和仍待验证问题。
4. 记录所有明显位于热路径中的同步 I/O、反射、序列化、遍历、日志或 UI 查找，但不在本任务中重构。
5. 标出候选入口在 v0.111 的已知 API/UI break；不要求切换 v0.107 或复现历史 sample。

明确非目标：

- 不设计本项目最终架构。
- 不进行大规模重构。
- 不修复 v0.111 compatibility break。
- 不实现新 replay 功能。

验收产物：

- 一份 investigation report，包含证据链接（文件和符号）及版本信息。
- 一张模块清单，所有条目均标注“已确认 / 推断 / 未知”。
- 可复现步骤或明确阻塞原因。
- 建议的下一项最小实验，说明它降低哪项风险。

完成状态：2026-08-27 经 Project Lead 验收通过。八类入口、v0.111 break、热路径风险与最小场景均满足 handoff 停止条件；关键 DLL 签名已独立复核。

执行 handoff：`project-management/handoffs/INV-001.md`。

完成报告：`project-management/investigation-reports/INV-001-report.md`。

## DONE — SPIKE-001A：v0.111 原生 fixture 恢复基线

前置：已满足；INV-001 已确认 `ToSave`、`FromSerializable`、异步 saved setup、`LoadRun` 与 RNG 序列化候选签名。

目标：只验证 public-beta `v0.111.0` 在首章第一场弱怪、玩家首回合输入就绪点生成的原生 `SerializableRun` fixture，能否在独立恢复后回到相同结构位置、战斗摘要与完整 RNG digest。

边界：只做 disposable developer probe；暂不记录或 dispatch 玩家操作，不实现 recorder/player、正式文件格式、UI、纠偏、跨版本、接管、多人或全场景覆盖。不得覆盖普通 live save slot。

验收产物：固定版本/模组环境、可重复的 fixture 生成和独立恢复步骤、至少两次恢复对比、expected/actual 白名单状态与 RNG digest、首个失败步骤、探针源码/构建说明和 handoff report。

执行 handoff：`project-management/handoffs/SPIKE-001A.md`。

完成结果：有价值的失败。`ToSave` 捕获在观察白名单内无副作用；同进程 setup 因 `State is already set.` 停止；跨重启恢复完成但产生 14 项手牌/牌堆/RNG diff，且 `SetUpSavedSingleplayer` 改写普通 `current_run.save`。恢复基线未建立。

完成报告：`project-management/investigation-reports/SPIKE-001A-report.md`。

## DONE — INV-002：v0.111 原生恢复语义与安全隔离调查

前置：SPIKE-001A 已用运行证据证明当前候选链不是安全、等价的首回合检查点恢复路径。

目标：只读追踪 v0.111 正常 continue/load、RunState 生命周期、保存写入和房间/战斗重建调用链，回答原生 `SerializableRun` 的设计恢复边界、普通 save 写入为何绕过 `ShouldSave=false`、是否存在安全卸载/隔离入口，以及下一次运行实验应验证“房间前检查点 + 快速重演”还是其他候选。

边界：不运行新的恢复尝试，不修改 probe、游戏或存档，不实现 save 拦截、自制快照、player/recorder 或正式架构。

执行 handoff：`project-management/handoffs/INV-002.md`。

完成状态：2026-08-28 经 Project Lead 验收通过。报告定位了 Continue、reload-count 写盘、RunState teardown、room reconstruction 与 RNG 消费的具体调用链；确认中途 combat serialization 不是战斗快照，并把动态 RNG 归因保留为推断。

完成报告：`project-management/investigation-reports/INV-002-report.md`。

## COMPLETE / INCONCLUSIVE — SPIKE-001A2-R2：profile1 房间前 checkpoint 正常 Continue A/B

前置：INV-002 已确认官方房间前保存使用 `PreFinishedRoom=null`；SPIKE-INV007A-R3 与 SPIKE-INV008A-R6 已分别通过 direct progress-write 和 controlled owner-fault/normal-restart 门禁。该任务只作为独立 reconstruction control，不重复承担 session isolation 证明。

目标：在固定模组存档 1 号中，对同一个由游戏正常产生的房间前 checkpoint 比较“不重启直接进入房间”的 A 与“新进程从主菜单正常 Continue”的 B，在首个输入就绪点验证结构、玩家、牌堆、敌人与完整可取得 RNG 是否等价。

边界：baseline 建立前允许用户在专用测试槽准备新 run；A 后 checkpoint 内容必须精确不变才可进入 B。不切换或读取其他 profile，不实现 restore helper，不替换 save manager，不出牌、不领奖、不 capture/dispatch。B 的已知 production `NumReloads +1` 写入必须解析并窄审计，不能宣称零写入。

执行 handoff：`project-management/handoffs/SPIKE-001A2-R2.md`。旧 `SPIKE-001A2.md` 已退役，不得执行。

完成结果：R3 在正确环境进入目标弱怪，但 probe 一直停留于 `READY_A_NODE`。没有正式 baseline、A snapshot、B 或 terminal artifact，故没有 A/B compare。Project Lead 接受旧 probe 永久停止，但将结果分类为 `INCONCLUSIVE_HARNESS_OBSERVABILITY_FAILURE`：无 rejected-frame telemetry，catch-all 又静默返回 null，无法定位 ticker、exception 或 predicate；也没有正式 protection window，不能判定 isolation PASS/FAIL。

复核：`evidence/spike-001a2-r2/project-lead-final-review.md`。

## COMPLETE — INV-010：A2-R2 capture driver、readiness 与 checkpoint timing 静态调查

目标：纯静态解释 R3 stall 的可判定部分，逐项验证 readiness predicate，追踪 node click→room-before save→combat→input-ready 时序，并定义带 heartbeat、raw rejected-state、异常与有界 terminal 的新 capture contract。

边界：不启动游戏，不读取当前 profile/save/cloud，不修改/构建/安装/运行旧 probe，不创建新 runtime probe。只能给出 `READY_NEW_RECONSTRUCTION_CONTROL`、`READY_DIAGNOSTIC_ONLY` 或 `RETIRE_NATIVE_CHECKPOINT_CONTROL` 中一个建议。

执行 handoff：`project-management/handoffs/INV-010.md`。

结果：`READY_NEW_RECONSTRUCTION_CONTROL`。确认 production `SaveRun(null,true)` completion 严格早于 room creation，可在其返回 continuation 前验证并封存 room-before checkpoint/protection baseline；新 control 必须使用 persistent heartbeat、逐字段 readiness/exception telemetry、独立30秒deadline和30连续process frames。报告：`project-management/investigation-reports/INV-010-report.md`。

## COMPLETE / INCONCLUSIVE — SPIKE-001A3：room-before checkpoint uninterrupted A / normal Continue B

目标：使用全新 probe identity，在第一幕普通弱怪节点的 production `SaveRun(null,true)` await 边界原子封存 `PreFinishedRoom=null` checkpoint与profile1 protection baseline，随后比较不中断A与新进程正常Continue B的首个稳定可操作状态。

执行 handoff：`project-management/handoffs/SPIKE-001A3.md`。

结果：R2命中matching production room-before save，但因arm target的unknown floor以`-1`参与提前相等校验而`BLOCKED_CHECKPOINT_VALIDATION`。没有baseline、recovery、A/B snapshot或有效compare；不是production reconstruction FAIL。observer异常同时逃逸到production map action。A3永久退役。

复核：`evidence/spike-001a3/project-lead-terminal-review.md`。

## COMPLETE — INV-011：checkpoint/playable floor与observer non-interference

目标：纯静态闭合arm predecessor、room-before checkpoint、uninterrupted A playable及normal Continue B playable四种floor语义；拆分target identity字段，并制定observer自身失败不污染production continuation的契约。同步审计A3所有sentinel/capture-later字段与校验顺序。

执行handoff：`project-management/handoffs/INV-011.md`。

结果：`READY_SUCCESSOR_RECONSTRUCTION_CONTROL`。确认checkpoint floor等于arm predecessor floor，A/B playable floor均为checkpoint floor+1；冻结typed target、room/encounter分离、phase-local object identity与observer non-interference契约。Project Lead复核：`evidence/inv-011/project-lead-review.md`。

## COMPLETE / INCONCLUSIVE — SPIKE-001A4：typed target的room-before A/B reconstruction control

目标：以全新identity实现INV-011 successor contract，在同一production room-before checkpoint上完成uninterrupted A与normal Continue B的首个稳定可操作状态比较。

执行handoff：`project-management/handoffs/SPIKE-001A4.md`。

结果：r2唯一业务attempt建立正式room-before baseline/recovery，A capture没有形成；源码确认baseline后deadline提前结束。后续resume terminal为`FAIL_HARNESS_AFTER_BASELINE`，有效A/B compare为0。退出后checkpoint raw bytes未变，但`latest.mcr`与`settings.save`内容变化，protection FAIL。A4持久disabled；最终cleanup进程的profile3 gameplay/write违反no-run协议，但不重分类A4业务终态。A4永久退役，禁止重跑、restore、rebaseline、Phase B。Project Lead复核：`evidence/spike-001a4/project-lead-final-review.md`。

## DONE / NON-BLOCKING — TOOL-001：本地可重复反编译导航缓存

目标：为固定v0.111.0 `sts2.dll`建立workspace外、hash-gated、版本隔离且幂等的本地反编译缓存；仓库只保存生成脚本与说明。

执行handoff：`project-management/handoffs/TOOL-001-local-decompilation-cache.md`。

结果：workspace外缓存已生成；manifest固定v0.111.0 DLL/hash、ilspycmd 9.1.0.7988与non-authoritative属性，3,538个C#文件可供导航。Project Lead已核实目录与manifest；仍不作为权威证据，不解锁实验。报告：`project-management/investigation-reports/TOOL-001-report.md`。

## COMPLETE — INV-012：A4后基线捕获活性与退出写入边界

目标：纯静态先固定v0.111.0的account/modded/profile1存储拓扑、文件语义和写入边界，再审计A4的deadline、原进程ticker/readiness telemetry、退出内容变化及后继protection合同；同步评估.NET/独立Godot/正式STS2三层无头可行性。给出下一运行实验的预先固定合同或更窄诊断建议。

执行handoff：`project-management/handoffs/INV-012.md`。不得启动游戏、读取当前profile数据、修改A4或创建下一probe；A4 terminal与profile3 cleanup偏差须分开归类。

结果：Project Lead于2026-09-27验收为`READY_NARROW_DIAGNOSTIC`。固定IL证实存储/退出writer条件路径，A4唯一timer在baseline seal后提前取消；原进程ticker/readiness动态首因和两项内容差异的具体调用方仍未知。独立重跑14/14合成合同测试；A4保护FAIL不追认、A/B有效compare仍为0。复核：`evidence/inv-012/project-lead-review.md`。

## COMPLETE — SPIKE-INV012A：原进程活性与退出writer窄诊断（离线与runtime已验收）

目标：用全新身份准备observer-only诊断，未来只辨别原业务进程ticker/readiness/observer异常与`NRun`/`NGame`退出writer调用元数据，不做A/B。Phase 0只允许源码、固定IL、合成/race测试、Harmony patch/unpatch smoke、隐私与安装生命周期审计及人工测试草案。

执行handoff：`project-management/handoffs/SPIKE-INV012A.md`。Phase 0不授权安装/启用probe、启动STS2（含headless）、读取当前profile或请求用户进行游戏内测试；运行阶段需Project Lead另行决定。

r5离线证据：Release 0 warning/error、28/28合同测试、9/9 patch/unpatch及non-interference smoke均由Project Lead复现；固定IL输出hash一致。源码复核另发现终态后追加业务记录和身份门前写boot/失败静默返回两个pre-runtime合同缺口，因此不放行运行。复核：`evidence/spike-inv012a/project-lead-phase0-review.md`。

历史修订：同一探针`project-management/handoffs/SPIKE-INV012A-Phase0-R6.md`已完成离线窄修；r5候选保持封存。

r6结果：Project Lead独立验收两处缺口已关闭，r6 DLL SHA-256 `CA1DAF85B5C12AE043A2BF20E7D9026DA894D336F649913A7E1D1BE2C69C1720`；Release 0 warning/error、35/35合同、9/9 patch/unpatch、dummy非干扰及三份固定IL/白名单审计均复现通过。复核：`evidence/spike-inv012a/project-lead-phase0-r6-review.md`。2026-10-09 PREP 已通过；当前运行合同为 `project-management/handoffs/SPIKE-INV012A-RUNTIME.md`，不再沿用旧 Phase 0 报告中的未来运行草案。

## COMPLETE / BLOCKED_CONFIG — SPIKE-INV012A 运行前配置与保护预检

目标：在r6未安装、游戏未启动时，scope-safe地核对当前modded profile选择、退役probe/AscensionUnlocker持久enabled状态、候选/manifest、Cloud确认来源与三段保护窗口。A4 cleanup曾误入profile3，故不从历史“固定profile1”推定当前所选槽。

执行handoff：`project-management/handoffs/SPIKE-INV012A-RUNTIME-PREFLIGHT.md`。只读且只输出过滤后的配置摘要；不得安装、启动、切换profile或请求用户游戏测试。Project Lead验收后再决定独立人工运行阶段。

结果：2026-09-28 Project Lead按**当时r6合同**独立验收`BLOCKED_CONFIG`。预检时selector=`profile3`，唯一持久enabled ID=`AscensionUnlocker`；r6未安装，Cloud当时状态未知。固定IL显示正式游戏启动会先按selector选择profile，再初始化该profile的Progress/Prefs。用户随后允许modded profile1/2/3用于实验，因此触碰modded3不再是个人数据安全阻断；但原r6身份门仍不接受该loaded set/profile。复核：`evidence/spike-inv012a/preflight/project-lead-review.md`。

## SUPERSEDED_UNEXECUTED — INV-013：profile1测试环境的安全配置准备合同

目标：只用固定DLL/IL、既有过滤证据和静态合同，比较可恢复离线配置准备与一次特许UI准备的实际读写范围，明确用户授权、备份/Cloud/验证门。不得修改当前selector/settings，不启动游戏或安装r6。

原执行handoff：`project-management/handoffs/INV-013.md`，已退役且未执行，不得发送给Implementation Codex。用户已明确唯一要保护的个人进度是不加载模组的profile1；modded profile1仍为专用测试槽，modded profile1/2/3与模组开关可供实验使用。原工作包为避免触碰modded3而设计离线配置编辑，已无必要。r6自身的精确profile/loaded-set门仍需适配或满足；不自动解锁运行。

## DONE — PREP-INV012A-R6：使用原探针的简化环境准备

目标：保留已验收的R6原候选，不因用户允许使用modded3就先造R7。项目发起人按熟悉的设置流程，在不进入任何对局的准备会话中将当前modded选择切到profile1，**暂保留`AscensionUnlocker`启用**；随后正常退出。只读复核selector、enabled IDs、游戏与R6候选身份。R6安装前不制造零启用模组状态，避免意外切入不加载模组的profile1；后续仍须建立phase-local实验基线。

执行handoff：`project-management/handoffs/PREP-INV012A-R6.md`。当前只批准人工选择modded profile1与只读复核，不安装R6、不切换模组、不运行诊断；若准备无法避开unmodded profile1则停止并重新设计。通过后由Project Lead另签安装、模组集合调整与人工诊断阶段。

结果：2026-10-09用户报告完成、Project Lead过滤只读复核通过：selector=1、mods_enabled=true、唯一 enabled=AscensionUnlocker、九项旧 probe disabled、游戏进程=0、固定游戏/Harmony/r6身份通过、r6未安装。Cloud来源为用户完成报告，不作机器验证。验收报告：`project-management/investigation-reports/PREP-INV012A-R6-report.md`。

## DONE — SPIKE-INV012A-RUNTIME：合并的安装与人工窄诊断

目标：用已验收 r6 先完成模组配置，再观察实际原进程 ticker/readiness 与 native 退出 writer 元数据；不做重建 A/B、隔离或 crash 验证。

执行handoff：`project-management/handoffs/SPIKE-INV012A-RUNTIME.md`。同一实现对话完成安装/配置/一次正式诊断；最多两次已定位 harness 修复后重测，fresh build/attempt且旧证据不覆盖。允许可消耗 modded profile1 的必要正常游戏写入，禁止触及 unmodded profile1。用户手动运行；不使用 Computer Use 或自主 headless。

推进标准：完整且可解释的 ticker/readiness/退出窗口结果，无需为了追求 PASS 反复造探针；随后 PM 才评审 successor A/B control。2026-10-09依用户明确授权已派发到现有实现对话，消息工具确认接收；尚无新的runtime结论。记录：`evidence/spike-inv012a/runtime-dispatch-2026-10-09.md`。

阶段进展：r6 DLL/manifest已由Implementation安装，PM独立核对live pair hash/长度和游戏进程=0通过；配置会话未回传，A门尚未全部完成。人工请求：`evidence/spike-inv012a/runtime/manual-request-config-2026-10-09.md`；复核：`evidence/spike-inv012a/runtime/project-lead-install-review-2026-10-09.md`。配置核对通过后可在本包直接请求正式诊断，不新增工作包。

最终结果：r6主菜单0帧后同包修复ticker/正常标记；runtime-r1完成真实入房，20字段30连续帧capture-ready，native replay/settings writer各entry/return=1/1。PM核对14条原始记录身份/hash/字段/顺序并重跑.NET9合同38/38、dummy非干扰，验收为`ACCEPT_DIAGNOSTIC_PASS / NO_RECONSTRUCTION_CONCLUSION`。早期阶段记录保留为历史，不再等待配置。复核：`evidence/spike-inv012a/runtime/project-lead-runtime-review-2026-10-09.md`。收尾已验收17/17归档hash与两个精确安装文件移除；候选/源码/证据保留，配置enabled条目未修改，不宣称持久disabled。

## DONE — SPIKE-001A5：房间前checkpoint A/B control

目标：新identity继承正确floor、语义snapshot及已验证ticker/deadline，得到一个有效A/B compare。已完成，以下进展按时间保留，不作为重跑授权。

执行handoff：`project-management/handoffs/SPIKE-001A5.md`已CLOSED。最终验收：`evidence/spike-001a5/project-lead-runtime-review-2026-10-22.md`。

派发：2026-10-09已发送给「完成 SPIKE-INV012A 工作包」，未指定模型覆盖，消息工具确认接收。证据：`evidence/spike-001a5/dispatch-2026-10-09.md`。

r1验收：独立Release 0 warning/error、99/99合同、4/4 patch/unpatch、dummy/IL审计与24文件交付hash通过。真实A/B仍为0。唯一窄修是保留capture最后样本/计数和限频诊断，使超时能区分no-tick与never-ready；要求追加在原handoff第6节，维持原readiness/floor/Task/语义标准。验收：`evidence/spike-001a5/project-lead-phase0-review-2026-10-09.md`。r2之后同包配置/安装/人工A/B，不另开INV。

r2已完成并验收：独立122/122、4/4、dummy/增量审计、331文件/12源码身份、r1保留通过；capture超时诊断缺口关闭。DLL hash `8DFB0C4AE6393F61D178EBA7A380690F0574BBE27C69246BB53B214306C5BF09`。验收：`evidence/spike-001a5/project-lead-phase0-r2-review-2026-10-09.md`。两种有效比较结果都停止交PM；source变化或未定位production故障不自动重测，未安装/未运行状态以当前阶段收据更新。

第7节运行授权已直接派发给同一Implementation对话，工具确认接收，未覆盖模型；配置/安装收据和实际A/B结果待回传。记录：`evidence/spike-001a5/runtime-dispatch-2026-10-09.md`。

10月21日进展：旧`861af3a9e6d449acbd192b0a8d3c2f56` only-A5/modded1/Ready/Arm成立，随后用户在目标前退出并要求继续，分类`USER_INTERRUPTED_BEFORE_TARGET / NO_VALID_AB`，原active/arm保留，不算harness/production故障。新runtime-r1只改identity，326文件及原r2的331文件保留、live pair、独立125/125、4/4、dummy/IL通过。已Arm 1/3，不重置预算；当前因多账号停在配置前，待当前原账号确认/匹配。用户表示自行删除另一份配置，Codex不检查/改动它，不要求删除后才测试。复核：`evidence/spike-001a5/project-lead-progress-review-2026-10-21.md`。

最新：原对话已收到用户当前原账号确认，三重匹配及配置回读通过，发出A2 fresh主菜单请求；尚无新process的loaded/profile/Ready或新Arm。启动前收据：`evidence/spike-001a5/runtime/prelaunch-A2-runtime-r1-2026-10-21.json`；当前请求：`evidence/spike-001a5/runtime/manual-request-A2-main-menu-runtime-r1-2026-10-21.md`。上一段账号拦截为已解除的准备阶段历史，不再要求用户处理另一目录。

最终完成（10月22日）：有效attempt `3e6aaa0160e441c293b5ac4a0030b6d0`，same CP、fresh B、A/B各30连续帧/20字段/max gap34ms，无terminal，A.complete及B compare-complete齐全。两个8088-byte semantic独立字节/JSON相同，Differences=[]；405份交付和旧24/331/326份索引保留通过。收尾live pair移除、18个其他旧probe不变、进程0；persistent disabled据绑定收据，private/workspace证据保留。记录2/3，停止，不执行第三轮；该结果不证明后续轨迹或产品隔离。

## DONE — INV-003：独立 Replay 会话与 RitsuLib 适用性调查

目标：只读追踪 v0.111 从主菜单进入独立 Replay 管理/播放 scene 的候选入口，以及 Replay `RunManager`/network/save/stat/achievement/teardown 生命周期；同时固定并评估 RitsuLib 对主菜单 UI、生命周期、patch、设置、诊断、临时数据和兼容门控的真实能力。

必须回答：在已有普通 Continue 的同一玩家 profile 下，如何建立不写 `current_run.save`、不增加 reload count、不替换正常进度且退出后可恢复原主菜单状态的 Replay 会话；若没有现成入口，下一实验应验证哪一个且仅一个最小隔离候选。

边界：不启动游戏，不安装 RitsuLib，不修改 probe/存档，不实现 Replay UI 或运行上下文，不把 RitsuLib 文档中的 `InMemory` store 外推为游戏 save 隔离。

执行 handoff：`project-management/handoffs/INV-003.md`。

完成状态：2026-09-04 经 Project Lead 验收通过。已确认主菜单 peer entry 需要 Godot 节点注入与 hook；`SetUpReplay` 只是 debug-oriented primitive，默认 `shouldSave=true`；`ShouldSave=false` 不能阻止战斗胜利后的直接 progress 写入。RitsuLib v0.5.18 已固定，但不解决核心 session/save 隔离，暂不作为必要依赖。

完成报告：`project-management/investigation-reports/INV-003-report.md`。

## COMPLETE / EXPERIMENT FAIL — SPIKE-INV003A：事务式 ReplaySaveContext 会话隔离

前置：INV-003 已给出方法体/调用者证据，确认当前唯一值得运行验证的组合候选是“全局 in-memory SaveManager transaction + `ShouldSave=false` + 单一 ordered teardown”。

目标：在固定的模组存档 1 号（已完成全内容解锁）中建立/保留测试用 Continue 隔离哨兵，从主菜单 peer 入口启动最窄 native replay fixture；证明播放、正常停止、直接 progress-save sentinel、受控 setup/post-launch 失败和重启检查均不会写/delete/submit 测试槽进度，并且 Continue 哨兵与全解锁进度保持完全不变。该措辞按执行期间的用户澄清修正，不外推到其他槽的真实普通对局。

边界：本实验不使用 RitsuLib，不切换 profile，不实现 Replay 管理产品 UI、capture/dispatcher、room-before A/B、seek 或完整架构。禁止 Computer Use/GUI 自动化；Implementation Codex 分 phase 提交人工测试需求，由项目发起人操作游戏。任何原 store/cloud/side-channel 写入、全解锁进度变化或 teardown 残留立即 fail closed。

执行 handoff：`project-management/handoffs/SPIKE-INV003A.md`。

完成状态：2026-09-08 经 Project Lead 验收。Phase A management-only 与 Phase B watch/normal Stop 通过；Phase C 的 native 事件流在战斗胜利前耗尽，未命中 direct progress-write sentinel，因此按首个可审计 FAIL 停止，D/E/F 未执行。该结果没有发现 ordinary/profile 文件写入、外部 submit 或 teardown 泄漏，但不足以证明完整 session isolation，也不否定 transaction 候选。

完成报告：`project-management/investigation-reports/SPIKE-INV003A-report.md`。

## COMPLETE — INV-004：native CombatReplay 完整性与隔离哨兵解耦调查

前置：SPIKE-INV003A 已以 Phase C FAIL 结束；正常 Stop 的 transaction/teardown 有局部正面证据，但 direct-write、fault 和 restart 路径仍未验证。

目标：只读解释固定 native `.mcr` 为何在胜利前耗尽，逐项比较官方 debug replay caller 与 probe dispatcher，并选择一个不依赖任意 Replay 正确性的 production direct-write 隔离哨兵。

边界：不启动游戏，不修改或重跑 probe，不解锁 D/E/F，不实现 capture/dispatcher，不引入 RitsuLib。不得靠反复生成 fixture 碰运气。

执行 handoff：`project-management/handoffs/INV-004.md`。

完成状态：2026-09-08 经 Project Lead 验收通过。固定 fixture 已确认包含第三回合致死 action 及对应末尾 checksum；native 格式没有通用 victory/complete 标志。Phase C dispatcher 与 official debug caller 的 event-loop 语义一致，readiness/action cancellation 仅为首要推断而非根因。下一隔离哨兵与 replay-to-victory correctness 已解耦。

完成报告：`project-management/investigation-reports/INV-004-report.md`。

## COMPLETE / EXPERIMENT FAIL — SPIKE-INV004A：direct progress-write 隔离哨兵

前置：INV-004 已确认固定 fixture 的事件完整性与 dispatcher fidelity，并推荐独立 direct-write sentinel；SPIKE-INV003A Phase B 已提供正常 setup/Stop/teardown 的局部复用基线。

目标：在不 dispatch replay events、不调用自然 victory/achievement path 的 isolated replay session 中，直接调用 production `UpdateProgressAfterCombatWon` 与 `SaveProgressFile`，验证 mutation/write 只落入 memory manager/store，原进度与受保护文件不变。

边界：只验证两个 SaveManager 方法的隔离路由；不执行原 D/E/F，不验证 replay fidelity、自然战斗胜利、achievement 或完整 session isolation。禁止 Computer Use/GUI 自动化。

执行 handoff：`project-management/handoffs/SPIKE-INV004A.md`。

完成状态：2026-09-10 经 Project Lead 验收。实验在安装 override 前的 isolated-manager content-equivalence gate 失败并按规则停止；没有进入战斗、建立 replay state、读取/分派 event 或调用两个 progress 方法。原 manager、Continue、12 个受保护文件及 inventory 均保持不变。该结果说明 disk-rehydrated manager 与当前 live manager 的等价性不是稳定前提，不是 direct-write leakage 证据，也不否定 transaction 候选。

完成报告：`project-management/investigation-reports/SPIKE-INV004A-report.md`。

## COMPLETE / PARTIAL_DIAGNOSED — SPIKE-INV004B：pre-override deep-copy 等价诊断

前置：SPIKE-INV004A 已安全停止，并将失败定位在 override 安装前；相同 disk-rehydrate 构造曾在 SPIKE-INV003A Phase B 通过，但本轮只留下一个合并的 Settings/Prefs/Progress 不等价异常。

目标：逐组件识别 live manager 与 disk-rehydrated candidate 的确切结构差异及其来源；同时验证从当前 live serialized state 注入 mock store 后完整初始化的 candidate，能否在引用独立的前提下与 live Settings/Prefs/Progress 精确等价。

边界：只允许主菜单上的诊断构造与比较。不得安装 `SaveManager` override，不得建立 replay/run state，不得调用 progress update/save，不得读取或分派 replay events，不得写入真实 store，也不得重试 SPIKE-INV004A。禁止 Computer Use/GUI 自动化。

执行 handoff：`project-management/handoffs/SPIKE-INV004B.md`。

完成状态：2026-09-10 经 Project Lead 验收。Candidate A 在本轮三组件完全等价，未复现或定位 SPIKE-INV004A 的历史差异；Candidate B 三组件精确等价，且双方各 367 个可变节点中共享引用为 0。candidate I/O 全部封闭在 mock store，诊断窗口内 live manager、12 个受保护文件和 inventory 不变。正常退出后有三个文件 mtime 更新但内容 hash 不变，属于窗口外 production lifecycle 行为。因历史差异未定位，本任务不解锁 direct-write。

完成报告：`project-management/investigation-reports/SPIKE-INV004B-report.md`。

## COMPLETE — INV-005：SaveManager 启动期 mutation 与保存调度调查

前置：SPIKE-INV004B 已证明 live-seeded candidate 在一个固定主菜单 snapshot 上可以精确等价且引用独立，但未解释 SPIKE-INV004A 的瞬态 disk/live 差异；正常退出另观察到三个内容不变文件的 mtime 更新。

目标：只读追踪 production `SaveManager` 初始化、主菜单就绪和正常退出调用链中对 Settings/Prefs/Progress 的 mutation、save/write、未等待 task、deferred/callback 与退出 flush，确定 live-state capture 的可证实 quiescent boundary，并为下一 direct-write sentinel 给出一个明确的 go/revise/stop 建议。

边界：纯静态调查；不得启动游戏、修改/运行 probe、访问或复制存档、安装 override、调用 progress 方法或实现新 sentinel。不得为了定位不可复现的历史字段而扩大到无界全程序集调查。

执行 handoff：`project-management/handoffs/INV-005.md`。

完成状态：2026-09-11 经 Project Lead 验收。production 顺序确认为 Settings（very-early）→ cloud sync gate → profile → Progress → Prefs；三个组件的正常 Save 均为同步 `void`。发现 Settings 的 localization/display repair 与 Prefs 的 non-editor `FastMode 3→2` 可形成 live/disk 瞬态漂移；退出的同步 temp-write/backup/timestamp-sync 链可解释三个内容不变文件的 mtime 变化。由于 `InitializeGraphicsPreferences` 使用无 task handle 的 `CallDeferred`，现有 `NMainMenu._Ready` peer entry 不是静态可证实的 quiescent boundary，唯一建议为 `REVISE_WITH_TIMING_GATE`。

完成报告：`project-management/investigation-reports/INV-005-report.md`。

## COMPLETE / FAIL_SAFETY — SPIKE-INV005A：startup timing/barrier probe

前置：INV-005 已以 `REVISE_WITH_TIMING_GATE` 验收。组件保存同步、startup cloud 与 prior run save 有 production gate；剩余关键未知是无 handle 的 `InitializeGraphicsPreferences` deferred callback 相对主菜单入口和 `GameStartupComplete` 的实际顺序。

目标：在不安装任何 `SaveManager` override、不启动 run、不构造 candidate、不调用 progress mutation/save 的主菜单会话中，显式观测 graphics callback entry/exit、`GameStartupComplete`、original `CurrentRunSaveTask` 和相关真实-store operations，验证能否形成事件驱动且稳定的 live-state capture gate。

边界：只做 startup timing/barrier。成功必须由 callback completion 等显式事件证明，不得用等待固定帧数或固定秒数替代。禁止 Computer Use/GUI 自动化；项目发起人手动启动游戏。不得修改项目管理状态文件。

执行 handoff：`project-management/handoffs/SPIKE-INV005A.md`。

完成状态：2026-09-13 经 Project Lead 验收。机器 artifact 自分类 `FAIL_GATE`：一个 Steam remote leaf `WriteFileAsync` task fault，gate 未打开，Verify 未启用。工作包控制结果提升为 `FAIL_SAFETY`，因为 probe 在首个失败已知后继续审计，并在 scope 判断前对 out-of-scope profile payload 计算并记录 length/hash。没有 override、candidate、run、scene 或主动 save/progress 调用，但没有建立保护 baseline 或 capture gate。旧 probe 已退役，禁止重跑。

完成报告：`project-management/investigation-reports/SPIKE-INV005A-report.md`。

## DONE — INV-006：cloud barrier 与 scope-safe audit contract

前置：SPIKE-INV005A 已以 `FAIL_SAFETY` 结束。局部 timing 证明 graphics callback 与 startup task 均可观察，但当前 observer 错把 nested Steam leaf task fault 当作 outer production barrier failure，并越界派生了其他 profile 的 payload hash。

目标：纯静态确认 cloud-disabled startup 中 outer/leaf task 的异常捕获与完成语义，选择与 production 一致的 barrier 层级；同时定义一份 future observer contract，要求先做 profile1 allowlist 再读取/hash 参数，非目标调用只能完全忽略，或至多设置一个不含次数、路径、操作和 payload 的全局布尔值，并在首个终态原子 seal。

边界：不得启动游戏、访问任何 profile/save/cloud 数据、解析或复制越界 runtime records、修改/构建/运行旧 probe，也不得直接实现下一 timing probe。只允许固定 DLL、当前 probe 源码和必要的官方 Steamworks API 文档。

执行 handoff：`project-management/handoffs/INV-006.md`。

完成状态：2026-09-13 经 Project Lead 验收通过，最终建议 `READY_SCOPE_SAFE_TIMING`。固定 IL 证明 production 仅轮询 `DoCloudSync` outer task 的 `IsCompleted`，接受任一 terminal status；`GameStartupComplete` 不是 cloud-settled 证明。未来 timing probe 无需 global store/leaf patch、path 或 payload，并已形成原子 seal、闭合集合、artifact 白名单；Project Lead 修正 post-seal writer 歧义后，contract 共含 15 项 synthetic negative/race cases。

完成报告：`project-management/investigation-reports/INV-006-report.md`。

## DONE — SPIKE-INV006A：scope-safe outer-cloud timing gate

前置：INV-006 已通过并确认 outer task 可以用零参数 postfix 捕获；旧 SPIKE-INV005A 已退役且 restricted artifact 禁止读取。

目标：用全新 ID、目录和 schema 的最小主菜单 probe，只观测 graphics callback、`DoCloudSync` outer task、startup/menu marker、original manager/run-save task 和一次人工 Verify。禁止所有 global store/path/payload/save/profile instrumentation。

边界：只验证 timing gate 与 observer safety；不安装 override、不构造 candidate、不读取任何存档、不调用 progress/save、不启动 run。所有 GUI 操作由项目发起人按精确请求手动完成。

执行 handoff：`project-management/handoffs/SPIKE-INV006A.md`。

完成状态：2026-09-13 经 Project Lead 验收为 `COMPLETE — PASS_SCOPE_SAFE_GATE`。唯一 outer task 从 Pending 到 RanToCompletion；graphics 恰好一次正常生命周期；Gate/Verify 的 manager/task identity、status、11 项条件一致。closed artifact、23 项 synthetic tests、source/binary forbidden-surface audit、Release/installed identity 均独立复核通过。probe 已退役，禁止重跑。

完成报告：`project-management/investigation-reports/SPIKE-INV006A-report.md`。

## COMPLETE / BLOCKED_PRECONDITION — SPIKE-INV006B：live-seeded direct progress-write isolation sentinel

前置：SPIKE-INV006A 已通过 scope-safe timing gate；SPIKE-INV004B 已证明一次 live-seeded candidate 等价/引用独立；旧 SPIKE-INV004A 因 disk/live 差异未到达 direct methods。

目标：在新进程中先重建已验证 timing gate，再用 live-state seed 构造 isolated manager，安装 override，在不 dispatch replay event 的合法 combat scene 中直接调用 update/save，验证唯一 progress mutation/write 完全封闭于 memory manager/store 并完成 ordered teardown。

边界：只读取/备份 profile1、account selector、machine settings 与固定 workspace fixture；禁止其他 profile、broad path/payload audit、自然胜利、fault matrix、reconstruction 和 action replay。

执行 handoff：`project-management/handoffs/SPIKE-INV006B.md`。

完成状态：2026-09-14 经 Project Lead 验收。timing gate、profile1 保护 baseline 与 live-seeded candidate 等价/引用独立均通过；override 安装后进入 replay combat setup，但在 `SceneReady` 前命中多个断言共用的 `RunState` 前置失败。两个 direct progress 方法与 memory write 均未发生，因此 direct-write isolation 仍未测试。teardown、原 manager 恢复、Continue 与 12 个保护文件/recovery backup 全部通过。probe 已退役，禁止修改或重跑。

完成报告：`project-management/investigation-reports/SPIKE-INV006B-report.md`。

## COMPLETE — INV-007：SPIKE-INV006B `RunState` 前置条件静态定位

前置：006B 已安全停止；其 fixed `RunState` site 聚合了 setup 前、scene/load 后、fixture identity 等多类断言，封存 artifact 不含可唯一定位子条件的 runtime 值。

目标：纯静态拆分全部聚合断言，对照 003A、官方 replay caller 与两个 direct progress 方法的实际字段读取，建立最小必要前置条件契约；决定是否足以签发全新 sentinel，或只允许一项最小非敏感诊断。

边界：不得启动游戏，读取当前存档/profile/cloud/recovery payload，运行或修改 006B，安装 override，建立 run/scene，调用 progress/save，或实现新 runtime probe。不得扩大到 replay fidelity、自然胜利、fault/restart、capture/dispatch 或架构设计。

执行 handoff：`project-management/handoffs/INV-007.md`。

完成状态：2026-09-14 经 Project Lead 验收为 `READY_NEW_SENTINEL`。固定 IL 与 fixture checker 独立复现一致；调查将 production 必要字段、transaction safety 和 fixture-specific 身份断言分开，并证明固定角色、两只固定怪物、encounter 重复对比及 executor/frame readiness 不属于两个 direct progress 方法的调用要求。旧失败子条件仍不可恢复，但已足以定义新 sentinel 的最小契约。

完成报告：`project-management/investigation-reports/INV-007-report.md`。

## COMPLETE / INVALID_HARNESS_ATTEMPTS — SPIKE-INV007A：最小契约 direct progress-write isolation sentinel R1

前置：scope-safe timing、live-seeded candidate 与保护/teardown 构件已有运行证据；INV-007 已给出两个 direct progress 方法的最小字段需求和独立失败分类。

目标：在全新一次性 probe 中删除 006B 的固定角色/怪物和 executor/frame gate，按实际 room/player/encounter/enemy 参数动态计算 expected mutation；调用一次 update 与一次 save，验证 candidate canonical mutation、零提前 write、唯一同步 memory progress write 和 ordered teardown。

边界：不 dispatch event、不自然胜利、不调用 achievement helper，不验证 replay fidelity、fault/restart 或完整 session isolation；只使用 profile1 allowlist，禁止其他 profile 与 broad path/payload audit。禁止 Computer Use，游戏由项目发起人手动操作。

执行 handoff：`project-management/handoffs/SPIKE-INV007A.md`。

完成状态：2026-09-14 经 Project Lead 复核，无有效 direct-write 结论。R1 在 `GateReady` 后等待人工点击期间被未解除的 120 秒 timeout 错误封存；后一进程实际点击后建立了 recovery backup，但因固定 artifact 已存在而无法保存业务终态。旧 artifact 和 backup 均保留，不能据此判断 update/save、boundary 或 teardown。

报告：`project-management/investigation-reports/SPIKE-INV007A-report.md`。

## COMPLETE / ORACLE_ALIASING — SPIKE-INV007A-R2：harness 修复后的受控第二次尝试

目标：不复制新工程，只在现有 007A 上取消 GateReady 后 timeout、采用 R2 schema/专属不可覆盖 artifact，并加入 prior-attempt integrity gate及相应 race tests。通过前次 recovery/current allowlist 等价检查后，重新执行同一 direct-write 业务假设一次。

边界：保留全部 R1/后一 attempt 证据；不得改变 candidate、boundary、mutation oracle、teardown、允许 I/O、场景或 PASS 标准。任何 prior baseline 内容/inventory 差异都阻塞，不得恢复或 rebaseline。游戏仍由项目发起人手动操作。

执行 handoff：`project-management/handoffs/SPIKE-INV007A-R2.md`。

完成状态：2026-09-14 经 Project Lead 验收。R2 artifact有效完整；update在candidate上恰好一次，boundary、teardown与12文件保护通过。但expected由浅层 `ToSerializable()` 构造并与candidate共享nested stats，oracle先加一次、production再加一次，导致无效的mutation mismatch。save未执行。R2不得重跑或覆盖。

artifact：`evidence/spike-inv007a/runtime-artifact-r2.json`。

## COMPLETE / PASS — SPIKE-INV007A-R3：detached mutation oracle 修复

目标：在同一007A源码上只把expected改为baseline JSON deserialize round-trip的独立graph，证明oracle计算前后candidate/discovered不变且shared mutable references为0，再执行原update/save契约。

边界：保留R1/R2全部证据；使用v3 schema、独立artifact/recovery和R2 prior-integrity gate。不得放宽canonical equality、修改scene/candidate/boundary/teardown或扩大到渲染、Invalid Task ID、自然胜利和fault/restart。

执行 handoff：`project-management/handoffs/SPIKE-INV007A-R3.md`。

完成状态：2026-09-14 经 Project Lead 独立验收为 `PASS_DIRECT_WRITE_ISOLATED`。R3 artifact 身份、24 个连续 monotonic 事件、detached oracle、candidate update/save/write `1/1/1`、expected/actual canonical equality、唯一同步 memory progress write、payload、boundary、teardown、12 项 protection 与 12 份 recovery 均通过。R3 永久封存，不得重跑；结论不覆盖自然 victory、achievement、fault/restart、replay fidelity 或完整 session isolation。

artifact：`evidence/spike-inv007a/runtime-artifact-r3.json`。

## COMPLETE — INV-008：fault/restart transaction lifecycle 静态契约

前置：SPIKE-INV007A-R3 已通过 direct-write isolation；原 SPIKE-INV003A 的 setup failure、post-launch failure 与 restart phases 因 Phase C 停止而从未执行。

目标：纯静态追踪现有 transaction 从 override 安装、replay setup、scene launch 到 finally teardown 的所有 ownership、task/callback 与恢复顺序；分别定义 setup 后/launch 前 fault、scene-ready 后 fault以及下一进程 restart verification 的最小安全契约，并判断是否足以签发一个有界 runtime matrix。

边界：不得启动游戏、访问任何 profile/save/cloud/recovery payload、构建/安装/修改/运行旧 probe，或直接实现下一 runtime probe。不得扩大到自然 victory、achievement、replay dispatcher、UI 渲染、强制进程崩溃、reconstruction 或 action replay。

执行 handoff：`project-management/handoffs/INV-008.md`。

完成状态：2026-09-15 经 Project Lead 验收为 `READY_FAULT_RESTART_MATRIX`。静态查询器build 0 warning/0 error，Project Lead独立生成相同132,086-byte输出与SHA-256；setup/post-launch两个同步owner fault点、不可跳步teardown、独立teardown failure分类和两个restart verification边界均已形成契约。强制进程崩溃延期，restart PASS不得外推为crash recovery。

报告：`project-management/investigation-reports/INV-008-report.md`。

## COMPLETE / PREBUSINESS BLOCK — SPIKE-INV008A v1：controlled owner-fault 与 restart isolation matrix

前置：R3已通过direct-write isolation；INV-008已给出固定v0.111.0的两个安全fault点与四phase契约。

目标：以全新probe在四个依次解锁的fresh process中验证setup owner fault、紧邻restart、post-launch owner fault、紧邻restart。预期异常必须由owner捕获；teardown各步、boundary、Continue、12项保护和跨进程持久状态必须独立通过。

边界：不强杀进程，不dispatch event、不自然胜利、不进map/room、不出牌/领奖，不调查UI渲染、replay fidelity、reconstruction或action replay。游戏只由项目发起人手动运行。fault phase内mtime必须完全一致；restart phase只允许测试协议固定三项content-identical mtime变化。

执行 handoff：`project-management/handoffs/SPIKE-INV008A.md`。

完成状态：Phase 0通过，34/34 tests、Release 0 warning/error、source/IL audit和112-method smoke有效。首次运行因007A与008A同时enabled，在protection/candidate/override前封存`BLOCKED_IDENTITY`；用户禁用007A后的新进程只loaded 008A，但旧artifact已锁定phase，封存`BLOCKED_PHASE_ORDER`。两次均无recovery与业务字段，故无fault/restart结论。v1永久封存。

报告：`project-management/investigation-reports/SPIKE-INV008A-report.md`。

## COMPLETE / PREBUSINESS DEFECT — SPIKE-INV008A-R2：安装生命周期修复后的原四阶段矩阵

目标：保留同一源码与四阶段业务契约，只提升schema/revision并使用R2专属artifact/recovery；增加安装前旧probe disabled持久化验证、wrong-mod-set独立precheck与v1 prior-attempt identity gate。

边界：不得删除/覆盖v1 artifact或旧源码/证据，不得降低唯一loaded probe门禁，不得改变fault点、teardown、protection、mtime或PASS标准。只有纯配置precheck失败可按工作包预授权修正；任一业务terminal仍立即停止。

执行 handoff：`project-management/handoffs/SPIKE-INV008A-R2.md`。

完成状态：R2安装与真实enabled状态正确，SemVer `0.2.0`动态通过；但probe在initializer内过早调用`GetLoadedMods()`并观察到0个mod，生成独立precheck artifact后停止。同进程完整日志随后确认007A disabled且恰好loaded 008A。没有R2 business/invalid artifact或recovery，protection/candidate/override/run/fault均未建立，故无fault/restart结论。

## COMPLETE / IMPOSSIBLE PRIOR GATE — SPIKE-INV008A-R3：loaded-mod timing修复后的原四阶段矩阵

目标：保留v1/R2证据与四阶段业务契约，把exact loaded-set precheck移到可证明的post-loader、pre-profile边界；提升R3 schema/artifact/recovery identity并继续原矩阵。

边界：不得降低唯一loaded probe门禁，不得改变fault点、teardown、protection、mtime、允许I/O或PASS标准。Implementation Codex获得有次数上限的pre-business harness repair envelope，可自行修改当前probe并请求追加人工启动；一旦触及protection/candidate/override/run/recovery/fault或业务调用即失效并交回Project Lead。

执行 handoff：`project-management/handoffs/SPIKE-INV008A-R3.md`。

完成状态：loaded-set post-loader修复有效，但首个R3 business artifact在按钮执行前封存`BLOCKED_PRIOR_INTEGRITY`。Project Lead确认profile1与AccountSelector内容仍等于007A-R3；唯一内容差异是包含mod enable配置的`settings.save`。gate还错误使用recovery副本mtime，并要求前驱artifact不存在的Continue字段，故该prior gate不可满足。没有protection/candidate/override/run/recovery/fault或teardown，R3永久封存且无fault/restart结论。

## COMPLETE / BUSINESS FAIL — SPIKE-INV008A-R4：setup owner-fault teardown

目标：保留原owner-fault矩阵，修复R3 prior gate的baseline来源：历史artifact/recovery只做可比内容与证据链验证；第一phase fresh捕获Continue和12项source snapshot，transaction后严格比较；restart使用前一phase artifact的source mtime。

边界：不得恢复/rebaseline R3，不得把R3视为save漂移证据，不得改变fault点、teardown、boundary、允许I/O或PASS标准。必须以真实固定前驱artifact/schema证明完整gate存在正向可达路径，游戏仍由用户手动运行。

执行 handoff：`project-management/handoffs/SPIKE-INV008A-R4.md`。

完成结果：R4 首次有效进入 setup owner-fault 业务路径，fault/catch/finally、保护集合、Continue、run state 和 manager 恢复均通过，但固定 15 步 cleanup oracle 缺少两个受 `RunLobby != null` 条件控制的 lobby 清理调用，终态为 `FAIL_TEARDOWN_CLEANUP`。R4 永久停止，restart phase 未执行；该结果尚不能区分 production cleanup 缺陷与 phase-inapplicable oracle 要求。

完成证据：`evidence/spike-inv008a/setup-owner-fault-r4.json`，23,241 bytes，SHA-256 `40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040`。

## DONE — INV-009：setup/post-launch phase-aware teardown 契约

目标：纯静态确定 `SetUpReplay` 返回、Launch 前后的 `RunLobby` 所有权与 `CleanUp(true)` 条件路径，解释 R4 缺失两个 lobby 调用的适用性，并给出不可被宽泛放松的分阶段 cleanup oracle。

边界：不得启动或控制游戏，不得读取当前 profile/save/cloud/recovery payload，不得修改、构建、安装或运行 008A，不得创建 R5。只允许固定程序集与现有 R4 artifact/source 的只读分析和可复现查询工具。

执行 handoff：`project-management/handoffs/INV-009.md`。

完成状态：2026-09-15 经 Project Lead 独立复现后验收，分类为 `ACCEPT_PRODUCTION_CLEANUP_WITH_REVISED_ORACLE`。Replay setup 不创建 RunLobby；R4 两个缺失调用在 fresh-process replay setup/post-launch 均不适用。两阶段 core `CleanUp(true)` 义务相同，但 post-launch 另有 NRun scene replacement 义务。

完成报告：`project-management/investigation-reports/INV-009-report.md`。

## COMPLETE / BUSINESS FAIL — SPIKE-INV008A-R5：phase-aware fault/restart matrix

目标：把 INV-009 的条件 RunLobby oracle 与 post-launch scene ownership gate 应用于全新的 R5 四阶段链，完整验证 setup owner fault、相邻 restart、post-launch owner fault及相邻restart。

边界：不得重跑/覆盖 R4；必须使用 R5 schema、artifact和recovery。Replay各边界RunLobby必须为null，任何stale/identity变化fail closed；13项core cleanup保持必需，post-launch必须证明owned NRun被新main menu替换。游戏仅由项目发起人手动操作。

执行 handoff：`project-management/handoffs/SPIKE-INV008A-R5.md`。

完成结果：setup fault业务路径的lobby、13项core cleanup、fault owner、manager、scene、protection和Continue均通过；menu/refresh combined signal等待超时。Project Lead确认是probe observer顺序缺陷：`RefreshButtons`发生在`_Ready` postfix更新新menu identity之前，事件被拒绝；combined await又掩盖了已完成的menu-ready signal。R5永久停止，无restart结论。

完成证据：`evidence/spike-inv008a/setup-owner-fault-r5.json`，25,790 bytes，SHA-256 `9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9`；Project Lead分析见`evidence/spike-inv008a/project-lead-r5-menu-observer-review.md`。

## COMPLETE / PASS — SPIKE-INV008A-R6：controlled owner-fault / restart 四阶段矩阵

目标：只修复新main-menu identity捕获与`RefreshButtons`/`_Ready`信号顺序、独立状态报告，随后用R6专属证据链完整重走setup fault/restart与post-launch fault/restart。

边界：不增加等待时长、不删除任一menu信号、不以scene identity替代callback gate；其余R5业务与安全契约冻结。R5不得修改/重跑，游戏仍仅由项目发起人手动操作。

执行 handoff：`project-management/handoffs/SPIKE-INV008A-R6.md`。

完成结果：四个 fresh-process phase 全部通过；两个 fault phase 均为 `PASS_OWNER_FAULT_TEARDOWN`，两个 restart phase 均为 `PASS_RESTART_INTEGRITY`。双 menu signal、phase-aware cleanup、manager/scene restoration、保护状态与 Continue 均通过，所有 map/room/gameplay/reward/progress/replay dispatch/write 计数为零。验收严格限于 controlled synchronous owner fault 与 normal restart，不外推到 crash recovery、自然胜利或完整回放。

Project Lead 复核：`evidence/spike-inv008a/project-lead-r6-final-review.md`。

## DONE — SPIKE-001B：最小出牌与结束回合 capture/dispatch

2026-10-23 PM验收`ACCEPT_TWO_ACTION_TRAJECTORY_CONTROL / CLOSED`。唯一attempt `4f958fdc207f4b9ba83ede826848f9ed`、Arm1/3；Standard Ironclad A0、seed EBJVET20T859、SHRINKER_BEETLE_WEAK、row1col1。相同room-before CP，A人工Defend＋EndTurn，fresh正常Continue B自动两次逻辑分派；两端各2条实际accepted且完成，三点Round1/1/2逐字节/JSON结构相同，Differences0。六个30帧/20字段窗口、完整hash/commit和输入门通过。

PM独立核对158份最终交付、92原始公开记录、旧2137/951/405/24/331/326目标及18个其他项目安装文件。runtime候选始终为r2原hash，无业务源码修订；已验109/109等离线测试不重复。game0、持久禁用、精确本包pair已移除且workspace可恢复；所有证据保留，不使用剩余预算。

验收：`evidence/spike-001b/project-lead-runtime-review-2026-10-23.md`，只读复现`Verify-ProjectLead-runtime.ps1`。原handoff已关闭，r1/r2和runtime报告保持封存。普通Continue仍是实验control，非正式产品隔离。三稳定点的已投影RNG均未改变，不宣称RNG消费路径或一般确定性通过。

## DONE — PERF-001 Phase0：录制性能测量合同与离线管线验证

前置：001B及DET-001控制已完成。2026-10-26 D-090签发 `handoffs/PERF-001.md`：共同测量壳、空观察、候选记录三模式，分开frame hitch/分配/采集/序列化/I/O；验证离线统计与公开DTO有界管线，提交最小idle/map/card pilot草案。

2026-10-26 D-091验收ACCEPT_PERF_MEASUREMENT_CONTRACT。64项/5输入/18块/9000正式样本/6写出完整通过；PM独立重算统计、11源码绑定及349行/5方法IL。未重跑23功能/build/计时或读取玩家数据。首块约104ms、后续38–42ms，不认稳态/游戏性能，原sealed交付保留。复现及结论 `evidence/perf-001/project-lead-phase0-review-2026-10-26.md`。

## IN_PROGRESS — PERF-001 Phase1：三模式最小候选离线构建

D-091在同 `handoffs/PERF-001.md` 追加范围，独立PERF001/candidate-r1实现共同测量壳/空观察/候选采集编码写盘。只读固定IL、离线build/合同及Harmony dummy非干扰；精确patch/真实成功和idle边界/跨场景帧源/冻结与writer收尾必须落实。

安装/配置/当前玩家数据/人工游戏仍HOLD。拟议Custom同seed/无modifier的3模式pilot只为方法可用性，不代表Standard、持续I/O负载或G-02。候选经PM验收后同包再决定有限人工pilot；不重跑DET或新建泛化INV。

## DONE — DET-001 Phase0：首次重洗牌与偏差检测

2026-10-23验收`ACCEPT_PHASE0_MECHANISM_AND_DETECTOR / REVISE_NEOW_ELIGIBILITY`。PM独立核对51文件/6公开输入、11检测/15流程案例及Release0 warning/error，重取5份IL同hash、原六fixture/001B158目标保持。确认自然重洗的触发/Run.Shuffle覆盖及十卡9步；普通十卡在R3重洗仍是带资格前提推导，不是真实运行。

用户关于涅奥必有遗物的最新说明使“仅燃烧之血”门不可满足，原草案不直接作runtime合同；修订在同包下一候选阶段进行，旧交付封存。验收：`evidence/det-001/project-lead-phase0-review-2026-10-23.md`。

## DONE — DET-001 离线候选：兼容涅奥资格及五点重洗轨迹

2026-10-23验收`ACCEPT_OFFLINE_CANDIDATE / RUNTIME_HOLD`。73交付/32源码身份及sealed同hash独立build、Release0 warning/error、68/68、11 metadata patch/unpatch、18回调审计及真实Harmony同形dummy通过。完整九步state、事件序号、token规范化和实际资格读取已落实；GoldenPearl＋BurningBlood最小白名单经固定IL核对，但不保证每局提供。旧51/6/158与索引保持。

验收：`evidence/det-001/project-lead-candidate-review-2026-10-23.md`。DLL273408 bytes / SHA-256 `B83F8C3140EFF3CD1532A5C97D2C8E9D3E2AADDCD44F862E456E084AD04DE0CA`，manifest304 bytes / SHA-256 `187EAC0FDB0A65CC4CA2408092D872B17B7B8CEC44BFF598603635F6F0AF4681`，产物/source/原报告封存。无真实生产CardModel回调或重洗/A-B结果，不把synthetic/dummy当runtime PASS。

## DONE — DET-001 受控运行：实际R3重洗与五点A/B

2026-10-24最终验收D-089：`ACCEPT_SINGLE_SAMPLE_RNG_REPLAY_EQUIVALENCE / CLOSED`。同r3/schema2、af99/seed20FRCAL12Q8Y/GoldenPearl＋BB/SHRINKER_BEETLE_WEAK，一个room-before CP的A人工四操作与fresh B自动四操作，五完整semantic逐字节/结构全等，真实重洗E0…E4规范化全等；完整九步state/counter/digest、原Task及成员/最终牌堆链接成立。

PM重验最终331文件、18旧索引列示4657目标和6输入、178份原记录及业务链；game0、持久禁用、两个精确可恢复安装文件回收、其他18项目文件保持通过。配置/backup/native/private状态用已绑定收据，不重读个人/当前账号。只读复现`evidence/det-001/runtime/Verify-RuntimeDelivery.ps1`，最终验收`evidence/det-001/project-lead-runtime-review-2026-10-24.md`；索引hash `C89647402FF43D777F8778137B7D64C71021593280F39F7CC98F171F4D412E38`。

正式Arm1/3，剩余预算不用，DET001及历史probe不再运行，r4未实施。源/产物/原始/失败/准备证据保留；中断后的精确文件回收未重放配置，不restore/rebaseline。一般场景/多次稳定性、纠偏、seek、性能或完整独立Replay会话未证明，STATE/PERF/产品不自动解锁。D-081…D-089过程见DECISIONS与sealed runtime报告，不再列为待执行步骤。

## BACKLOG — STATE-001：强记录状态密度与恢复实验

前置：DET-001 能稳定检测并注入代表性偏差；产品已确认强记录及版本/模组边界。

目标：比较状态指纹、房间/回合/决策点完整检查点、逐操作增量状态等候选组合，不预先决定最终格式。

必须测量：

- 正常录制的平均开销与最长 hitch，尤其是地图操作和卡牌结算热路径。
- 每层/每局文件增长和写盘行为。
- 房间内与跨房间恢复延迟。
- 注入偏差后的自动恢复成功率、恢复后再次校验和后续继续播放。
- 模拟 schema/content 缺失时的不可恢复失败边界。

验收产物：推荐的最小权威状态集合与检查点密度，明确哪些结论已验证、哪些仍是假设；同时给 NAV-001 和 Replay 数据契约提供输入。

## BACKLOG — NAV-001：双向导航可行性实验

前置：STATE-001 给出可恢复的权威状态与检查点候选方案。

目标：在 v0.111 的自有最小 replay 探针中，比较“从头快速重演”和“检查点恢复后重演”到目标语义操作的延迟、状态一致性、录制开销与文件增长；不在实验前承诺最终数据结构。

验收产物：至少覆盖当前房间内逐操作后退、结构节点跳转与跨房间路线节点定位；给出可接受延迟阈值、检查点候选边界、失败模式，以及是否足以满足 MVP 的结论。

## BACKLOG — TIME-001：分类播放节奏实验

前置：SPIKE-001 形成稳定语义操作流；K2/K2a 已完成。

目标：定义少量稳定的操作类别，验证分类间隔 config、全局速度倍率、暂停与 Step 组合时的用户可理解语义；验证相对时间戳是否足以支持原始节奏，并验证聚焦/暂停/加载/可交互/阻塞检测的准确性和热路径开销。

## BACKLOG — PORT-001：可移植 Replay 与安全导入验证

前置：STATE-001 后形成相对稳定的最小 Replay envelope/元数据草案；无需先完成跨版本或其他 gameplay mod 兼容。

目标：证明 replay 可在相同精确游戏构建、Replay Mod/格式版本及 gameplay mod 环境的另一台机器上导入播放，并把外部文件作为不可信输入处理。

必须覆盖：

- 正常跨机器复制、管理页导入、重复导入和文件名冲突。
- 格式/schema/integrity 校验，以及损坏、截断、超限和未知版本文件。
- 游戏构建、Replay Mod/格式和 gameplay mod 环境不匹配的可理解诊断。
- 路径穿越、任意输出路径、意外嵌入字段/载荷和覆盖已有文件等安全反例。
- replay 中本机绝对路径、用户名或其他非必要身份信息的隐私检查。

验收产物：一组可移植 fixtures、安全反例和导入结果报告；明确哪些字段是跨机器必需、哪些必须剔除或脱敏。

## BACKLOG — COMPAT-001：v0.107 / 目标版本兼容矩阵

目标版本已决定为 public-beta v0.111。前置：PROD-001 明确首版支持边界，并完成所需 hook 的定向定位。

只记录 hook/API/UI 差异与影响面，优先识别 v0.111 目标路径上的 break；不默认用兼容层同时支持 v0.107。

## FUTURE-1 — MULTI-001：多人 Replay 目标与技术调查

启动时机：单人 MVP 完成并通过验收后；这是 MVP 后第一产品优先级。

目标：先定义多人 Replay 的玩家流程和信任模型，再调查权威状态、双方语义操作、网络事件、同步/断线、隐私及性能边界。不得仅把两份单人操作日志拼接后宣称多人回放成立。

当前阶段只需保证：单人格式决策若会不可逆地封死多人扩展，必须记录该风险；不得为了未验证的多人需要扩大当前 MVP 或增加无证据的热路径成本。

## FUTURE-2 — XVER-001：跨版本 Replay 大型目标定义

启动时机：单版本 MVP 完成并通过验收后；当前不实施。

目标：基于首版积累的带版本/内容元数据 fixtures，定义明确的版本组合、语义兼容标准和候选技术路线。该任务必须拆成调查、迁移/适配实验、兼容矩阵和玩家失败体验，不能作为单个“小兼容任务”交付。

当前阶段只需保证：Replay 文件保存精确游戏构建、Replay 格式版本、启用模组/版本及可取得的内容标识；任何版本不匹配都有明确诊断。

## FUTURE-3 — BRANCH-001：隔离的可游玩 Replay 分支

启动时机：多人和跨版本方向之后，且必须重新完成产品定义；当前不实施。

目标：从 Replay 某位置创建独立可游玩分支，保留原 Replay 上下文并允许返回。必须定义分支存档、成就/统计、接管后录制和失败恢复；明确拒绝不可逆地把 Replay 转换为普通对局。

## FUTURE-4 — MODCOMPAT-001：Gameplay Mod Replay 兼容协议

启动时机：单版本、原版内容 MVP 稳定后；当前不实施。

目标：研究其他 gameplay mod 如何声明兼容性、提供稳定内容 ID、额外语义操作、权威状态序列化/恢复与版本迁移。优先选择少量代表性模组建立协议，不以逐个硬编码所有模组为默认路线。

当前阶段只需保证：记录全部启用模组及版本；录制/播放 gameplay mod 环境不匹配时明确诊断，不静默播放。

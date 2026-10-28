# SPIKE-INV005A：startup timing/barrier probe

> 历史状态（2026-09-13）：本工作包已以 `FAIL_SAFETY` 结束并退役。原 artifact 自报 `FAIL_GATE`，但 probe 对 out-of-scope profile payload 产生派生审计信息，安全分类优先。禁止重跑、修补或外传 restricted artifact；下一任务仅为静态 `INV-006`。详见 `project-management/investigation-reports/SPIKE-INV005A-report.md` 与 D-036。

任务类型：受限主菜单运行实验；需要项目发起人手动启动游戏

## 任务目标

在不安装 `SaveManager` override、不构造 isolated candidate、不启动 run、也不调用任何 progress mutation/save 的条件下，验证以下显式事件能否组成稳定的 live-state capture gate：

1. `InitializeGraphicsPreferences` 已被 probe 观测到 entry 与正常 exit；
2. `NGame.GameStartupComplete` 已成功完成；
3. original `SaveManager.CurrentRunSaveTask` 为 null 或已完成；
4. 已观测的相关同步 writer 均已退出，异步 store task 均已完成；
5. gate 建立后到人工复核之间，original manager/三组件状态和受保护文件保持稳定。

本任务只建立 startup timing/barrier，不验证 direct-write isolation、Replay session、transaction、reconstruction 或 replay fidelity。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/DECISIONS.md` 中 D-034、D-035
4. `project-management/handoffs/INV-005.md`
5. `project-management/investigation-reports/INV-005-report.md`
6. `evidence/inv-005/startup-lifecycle.md`
7. `evidence/inv-005/component-writers.md`
8. `evidence/inv-005/il-query-output.txt`
9. `project-management/investigation-reports/SPIKE-INV004B-report.md`

## 固定环境

- 游戏：public-beta `v0.111.0`，commit/branch `41cef1ea`
- `sts2.dll`：9,757,184 bytes
- SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 测试 profile：模组存档 1 号；Continue 仅为 active-profile 隔离哨兵，全内容解锁和既有文件仍受保护
- Steam Cloud：测试前由项目发起人明确确认关闭
- 启用模组：只允许新的 SPIKE-INV005A probe；关闭旧 probe、RunReplays、BaseLib、RitsuLib 与其他 gameplay mod
- 游戏 GUI 只由项目发起人手动操作；Implementation Codex 禁止使用 Computer Use 或替代 GUI 自动化

不得读取、复制、备份、枚举或切换其他 profile，结论也不得外推到其他槽。

任一固定身份不符时 `BLOCKED_IDENTITY`，不得开始观测。

## Phase 0：probe 设计门禁

在写 probe 前，用 INV-005 的固定证据确认以下 instrumentation 点；若需要新的 IL 查询，只允许 hash-gated 窄查询：

1. `NGame.InitializeGraphicsPreferences` 的 Harmony prefix/postfix/finalizer，可记录 entry、正常 exit、异常和调用次数。
2. `NGame.GameStartupComplete` 的读取与 await 方式；不得阻塞主线程同步等待。
3. `SaveManager.CurrentRunSaveTask` 的 original-manager 读取方式。
4. Settings/Prefs/Progress 顶层 save 方法的 entry/exit/finalizer，用于维护 active synchronous writer counter。
5. 相关 concrete `ISaveStore` 的 `WriteFile`、`WriteFileAsync`、rename/delete/timestamp 操作审计点；异步返回值必须跟踪到 completed/faulted/cancelled，不能只记录 method return。
6. original manager 及其 store runtime identity 的安全识别方式；不得替换、包装或重新初始化真实 store。

如果无法在不改变调用语义的前提下观察 graphics callback completion，结果为 `BLOCKED_INSTRUMENTATION`，不得用固定等待替代。

## Phase 1：实现边界

新建独立目录：

- `experiments/SPIKE-INV005A/`
- `evidence/spike-inv005a/`

只允许观测与只读 fingerprint：

- probe 的 Harmony patches 必须在 mod 初始化期间安装，并记录 patch-active milestone。
- graphics prefix/postfix/finalizer 记录 monotonic sequence、UTC、Godot process frame、managed thread、original manager identity，以及 callback 前后 Settings raw/canonical hash；不记录完整 save payload。
- 主菜单 `_Ready` 只记录 milestone、附加诊断 UI，并异步等待 `GameStartupComplete`；不能把 `_Ready` 本身当作 gate。
- 记录 `GameStartupComplete` completed/faulted/cancelled。
- 记录 original `CurrentRunSaveTask` 在 menu-ready、startup-complete、graphics-exit、gate-open 和人工 verify 时的 null/status。
- 维护同步 component writer active counter，并跟踪已观测异步 store task；gate-open 时必须为零/终态。
- store audit 至少记录 sequence、operation、相对逻辑路径、store/manager identity、sync/async、task final status、payload length/hash（如安全可得），不得写完整 payload或 Steam/user identity。
- 对 Settings、Prefs、Progress 记录 raw/canonical hash；对 profile1 受保护文件记录存在性、长度、mtime、SHA-256 和 inventory。

严格禁止：

- `SaveManager.MockInstanceForTesting` 或任何 global override；
- candidate/mock manager 构造；
- `RunManager.SetUp*`、Load/Launch/CleanUp、scene transition 或 replay fixture/event access；
- `UpdateProgressAfterCombatWon`、`SaveProgressFile` 或任何主动 save/mutation 调用；
- 改动 live Settings/Prefs/Progress；
- 为制造事件而调用 `InitializeGraphicsPreferences`；只观察 production 自然调用；
- 把“等待 N 帧/N 秒后没变化”用作成功条件。

超时只允许作为 `STOP_MISSING_EVENT` 的停止机制，不能产生 PASS。

## Gate 状态机

### 1. `WAITING_EVENTS`

等待 production 自然产生 graphics entry/exit，并等待 `GameStartupComplete`。UI 必须保持 Verify 操作禁用。

### 2. `GATE_OPEN`

仅当以下条件在同一检查点同时成立：

- patch 在 graphics entry 前已经 active；
- 至少一次 graphics callback 已正常 exit，且没有仍在执行的 graphics call；
- `GameStartupComplete` 已成功完成；
- original manager identity 与启动记录一致；
- `RunManager.State == null`；
- original `CurrentRunSaveTask` null/completed；
- active synchronous writer counter 为 0；
- 所有已观测 async store tasks completed，且无 fault/cancel；
- 三组件均已加载并能取得 fingerprint；
- 受保护文件/inventory baseline 已成功建立。

在 gate-open 瞬间固定 manager/component/file baseline，并启用一次 `Verify Stable Gate` 操作。不得自动进入下一技术实验。

### 3. `VERIFY`

项目发起人看到 Gate Ready 后点击一次 Verify。probe 立即重新检查全部 gate 条件，并比较 gate-open 至 verify 之间：

- original manager identity；
- Settings/Prefs/Progress raw/canonical hashes；
- 受保护文件 identity 与 profile1 inventory；
- 是否出现新的 component/store writer operation；
- 是否出现新的 graphics callback entry 或未完成 async task。

人工等待时长不是 barrier 证据；它只提供 gate-open 后的稳定性复核。PASS 的依据仍是显式事件与 task/writer 状态。

## 人工测试请求

probe 构建、静态检查和安装准备完成后，Implementation Codex 必须先向项目发起人提交一份精确测试请求，内容只能是：

1. 确认游戏仍为 v0.111.0，Steam Cloud 已关闭，使用模组存档 1 号。
2. 确认只启用 SPIKE-INV005A；不得启用旧 probe 或其他 gameplay mod。
3. 启动游戏并停留主菜单；不得进入 Continue、单人、多人、profile 选择或设置页面。
4. 等待 probe UI 显示 `Gate Ready`；在此之前不点击 Verify。
5. 显示 `Gate Ready` 后点击一次 `Verify Stable Gate`，随后不再操作，回传 UI 最终分类。
6. 达到终态后立即停止操作；不得进入 run、设置或奖励页面。按 probe 提示保持主菜单供证据收集，或在 Implementation Codex 明确通知后正常退出。

请求必须写明预期结果、异常时停止条件、目标后动作和用户需回传的最小信息。不得要求用户分析日志或手动操作文件。

## 保护与退出边界

- 正式诊断窗口从保护 baseline 建立持续到 Verify 终态落盘。
- 窗口内任何受保护文件内容/identity/inventory 变化均为 `FAIL_SAFETY`；不得 rebaseline。
- normal exit 已知会同步重写 Settings/Prefs/Progress 相关文件。诊断终态之后的退出写入必须单列为 post-exit baseline，不得混入 gate PASS，也不得因 hash 不变声称没有 write。
- probe 自身证据只能写入 workspace 的 `evidence/spike-inv005a/`，不能经过游戏 save store。
- 若无法区分 probe evidence I/O 与 game save-store audit，停止为 `BLOCKED_INSTRUMENTATION`。

## 结果分类

### `PASS_GATE`

只有 Gate 状态机全部成立，Verify 比较稳定，且所有安全保护项通过时成立。它只允许 Project Lead 编写新的 live-seeded direct-write sentinel，不代表 transaction/session isolation PASS。

### `STOP_MISSING_EVENT`

在合理的失败超时内未观察到 production graphics entry/exit，或 startup task 未完成。记录等待时长和已有事件后停止；不得重试或改用 quiet frames。

### `FAIL_GATE`

显式事件均出现，但 gate 无法同时满足，或 gate-open 后发生组件 hash、manager identity、writer/task 状态变化。保留首个失败事件并停止。

### `FAIL_SAFETY`

出现 override、run/scene、主动 save/mutation、真实存档内容/inventory 变化或越界 profile 访问。立即停止。

### `BLOCKED_IDENTITY` / `BLOCKED_INSTRUMENTATION`

固定身份或只读观测前提不满足；不得运行或降级标准。

## 交付物

1. `project-management/investigation-reports/SPIKE-INV005A-report.md`
2. `evidence/spike-inv005a/README.md`
3. 单份机器可读 runtime artifact，包含固定身份、完整 milestone 顺序、graphics/startup/task/writer/store audit、gate/verify fingerprints、保护文件比较、终态和首个失败点
4. `experiments/SPIKE-INV005A/README.md`、最小源码与 Release 构建说明
5. 如新增静态查询，提交 hash-gated 工具和最小文本结果

报告严格区分 Confirmed、Inferred、Unknown，并且只推荐一个下一动作：

- `PASS_GATE`：建议 Project Lead 编写新的 live-seeded direct-write sentinel；
- 其他结果：建议一个更窄的观测缺口或停止 transaction 候选。

## 项目管理文件边界

Implementation Codex 不得修改 `project-management/CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md` 或 Roadmap。只提交上述 probe、evidence 与 investigation report，由 Project Lead 验收后更新状态。

## 停止条件

- 固定 build/profile/mod/Cloud 身份不符。
- instrumentation 必须改变 production save/manager/callback 语义才能工作。
- 首个 safety failure 或 gate failure。
- 已得到任一终态；不得在同一工作包中安装 override、构造 Candidate B 或继续 direct-write。

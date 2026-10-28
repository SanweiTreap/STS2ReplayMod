# SPIKE-INV006A：scope-safe outer-cloud timing gate

> 历史状态（2026-09-13）：本工作包已以 `COMPLETE — PASS_SCOPE_SAFE_GATE` 通过并退役。禁止再次运行；下次启动前须关闭 `SPIKEINV006A`。下一任务为独立 `SPIKE-INV006B`，本 probe 不得修改后继续 direct-write。

任务类型：全新受限主菜单运行实验；需要项目发起人手动启动游戏

## 任务目标

用一个不 patch 任何 save store、不读取任何 path/payload/profile 文件的全新 probe，验证下列显式事件能否在 public-beta v0.111.0 上组成可重复的 pre-override timing gate：

1. probe patch 在 production graphics callback 前生效，且 `InitializeGraphicsPreferences` 至少一次正常退出；
2. 捕获零参数 `NGame.DoCloudSync()` 返回的唯一 outer `Task`，并观测到其任一 terminal status；
3. `GameStartupComplete` 到达 terminal，主菜单 ready；
4. original `SaveManager` identity 未变化，`RunManager.State == null`，original `CurrentRunSaveTask` 为 null 或 terminal；
5. gate-open 后人工 Verify 仍满足同一组事件/identity 条件，没有第二次 cloud/graphics 生命周期或 post-seal event 泄漏。

本任务只验证 timing gate 和 observer safety。不得安装 override、构造 candidate、建立组件/存档 fingerprint、调用 progress/save、启动 run，或实现 direct-write sentinel。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/DECISIONS.md` 中 D-035、D-036、D-038
4. `project-management/investigation-reports/INV-005-report.md`
5. `project-management/investigation-reports/SPIKE-INV005A-report.md`
6. `project-management/investigation-reports/INV-006-report.md`
7. `evidence/inv-006/production-cloud-task-layers.md`
8. `evidence/inv-006/scope-safe-audit-contract.md`

旧 `SPIKE-INV005A` 只作为失败反例。不得打开或处理其 restricted runtime artifact，不得复制旧 probe 后局部修补，也不得恢复其 global store/leaf patches。

## 固定身份与新命名

- 游戏：public-beta `v0.111.0`，commit/branch `41cef1ea`
- `sts2.dll`：9,757,184 bytes
- SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 新 mod/probe id：`SPIKEINV006A`
- 新源码目录：`experiments/SPIKE-INV006A/`
- 新证据目录：`evidence/spike-inv006a/`
- 新 artifact schema id：`spike-inv006a.timing.v1`

任一 identity 不符时 `BLOCKED_IDENTITY`。不得改用其他 build、沿用旧目录/schema 或覆盖旧证据。

## 固定运行环境

- 测试 profile：模组存档 1 号；Continue 只作为测试槽 UI 哨兵，全内容解锁仍受保护。
- Steam Cloud：运行前由项目发起人明确确认关闭；probe 不自行读取 Steam/user/profile 数据来验证。
- 启用模组：只允许 `SPIKEINV006A`；关闭旧 probe、RunReplays、BaseLib、RitsuLib 与其他 gameplay mod。
- 游戏 GUI 只由项目发起人手动操作；Implementation Codex 禁止使用 Computer Use 或任何替代 GUI 自动化。
- 不得进入或切换其他 profile，结论也只覆盖本次 profile1 测试环境。

## 强制 instrumentation surface

production probe 只允许下列观测面：

1. `NGame.InitializeGraphicsPreferences` 的 zero-argument prefix/postfix/finalizer：只记录 entry、normal exit 或固定 fault-site enum；不得格式化异常。
2. zero-argument `NGame.DoCloudSync()` 的 postfix：捕获 `__result Task` 的 runtime identity，并注册一次 terminal continuation。只记录 `RanToCompletion`、`Faulted` 或 `Canceled` enum，不读取 `Exception`。
3. `GameStartupComplete` 作为后续 lifecycle marker；不得把它单独解释成 cloud barrier。
4. 主菜单 ready 回调、original `SaveManager` runtime identity、`RunManager.State == null`、original `CurrentRunSaveTask` null/terminal status。
5. probe 自有 UI 的一次 Verify 操作与单份 workspace artifact 写入。

明确禁止：

- patch `ISaveStore`、`CloudSaveStore`、`SteamRemoteSaveStore`、component save methods 或任何 path-bearing save API；
- 访问 `__args`、path、payload、save serialization、component hash、文件 inventory 或 exception text/stack/data；
- 记录 Steam/user identity、profile token、wall-clock、文件路径、per-profile/global out-of-scope count；
- `SaveManager.MockInstanceForTesting`、任何 manager/store replacement、candidate/mock manager；
- `RunManager.SetUp*`、Load/Launch/CleanUp、scene transition、fixture/replay event；
- progress mutation/save 或为制造事件而主动调用 production callback；
- 用 quiet frames、固定等待或“主菜单看起来稳定”替代 outer task terminal。

## Scope-safe core 与静态门禁

必须把 `evidence/inv-006/scope-safe-audit-contract.md` 作为规范验收标准，而不是建议。

- 每个 patch、continuation、timer、UI callback 和普通 artifact-request callback 的第一 observer action 都必须先检查 `sealed/enabled`，再访问 `__result`、manager、分配 state、取 sequence/timestamp 或格式化任何对象；提交事件前在锁内再次检查。
- terminal classification 通过一次原子 CAS 产生唯一 winner；winner 后立即阻止新事件和 continuation 记录，再从闭合集合生成 artifact。
- 只有 terminal CAS winner 可以持 opaque winner ticket 调用一次 `WriteClosedArtifact`；writer 首先验证 ticket 与 sealed 状态，只读取已经复制的闭合集合。其他调用及普通 post-seal artifact request 必须在 snapshot/output-path 访问前返回。
- 已注册 continuation 在 seal 后执行必须 fast-return。
- terminal-owned artifact writer 失败后保持 sealed，只显示固定 `BLOCKED_ARTIFACT_WRITE`，不得换目录或回退到 game/user/save 路径。
- 生产程序集不得包含 path canonicalizer、payload summarizer、hash helper、global store patch 或 unused save-audit code。

在构建运行版本前，必须提交自动化静态检查，证明 production probe 中不存在以下符号/字符串引用：`ISaveStore`、`CloudSaveStore`、`SteamRemoteSaveStore`、`__args`、`SummarizeArguments`、save/profile logical paths、payload hash/length 字段。检查 source 和构建产物；误报可列出，但不得靠忽略整个文件/assembly 通过。

## Synthetic/unit-level contract tests

不得使用任何真实存档或 restricted artifact。必须用合成对象覆盖修订后 `scope-safe-audit-contract.md` 的全部 15 项 negative/race cases，并在报告中逐项映射。

对本 probe 已彻底移除的 path-bearing surface，路径类 case 可以由独立 test-only contract fixture 验证；该 fixture 不得进入 production probe assembly。运行 assembly 的静态检查必须证明这些 path/payload helper 不存在。

除上述 15 项外至少验证：

- zero-argument outer task 捕获一次且仅一次；
- outer task 三种 terminal status 都只表示 settled，不读取 exception；
- pending outer task 永远不能打开 gate；
- outer task terminal 后才允许与 graphics/startup/menu/manager 条件汇合；
- seal 与同步执行 continuation 竞争时仍只有一个 terminal winner；
- Verify 不能重新打开已失败/封存的 session。

所有测试须先于安装运行 probe 通过；失败时 `BLOCKED_INSTRUMENTATION`，不得让项目发起人启动游戏。

## Gate 状态机

### `WAITING_EVENTS`

等待 production 自然产生 graphics normal exit、outer-cloud task returned + terminal、startup marker terminal 与 menu ready。Verify 按钮必须禁用。

### `GATE_READY`

仅当以下条件在同一锁内快照中成立：

- patch-active sequence 早于首次 graphics entry；
- graphics normal-exit count 至少 1，active count 为 0，无 graphics fault；
- 恰好捕获一个 `DoCloudSync` outer task，且为任一 terminal status；
- `GameStartupComplete` terminal，menu ready；
- original manager identity 从首次捕获至今不变；
- `RunManager.State == null`；
- original `CurrentRunSaveTask` 为 null 或 terminal；
- 尚无 terminal failure，event set 未 seal。

此时固定 gate snapshot 并启用一次 Verify。outer task `Faulted`/`Canceled` 本身不是 gate failure；只能记录 status enum。`GameStartupComplete` 不能替代缺失或 pending 的 outer task。

### `VERIFY`

项目发起人点击一次后，在同一锁内复核：outer task identity/status、graphics counts、startup/menu marker、manager identity、run state 和 current-run task status均仍满足；没有第二个 `DoCloudSync`、新的 graphics entry 或未授权事件。

复核成功后原子 seal 为 `PASS_SCOPE_SAFE_GATE`。人工等待时间不是成功证据，只是 gate-open 后的一次稳定性复核。

## Artifact 白名单

只允许单份 `spike-inv006a.timing.v1` JSON，字段必须是 INV-006 contract 的子集：

- schema/probe version 与固定 game/DLL/probe identity；
- terminal classification、first-failure-site enum；
- monotonic sequence 与 elapsed monotonic ticks；
- patch-active、graphics、outer-cloud returned/terminal、startup marker、menu ready、gate ready、verify、terminal 等 event enum；
- outer/startup/current-run task status enum；
- original manager runtime identity、run-state-null、graphics entry/exit/active counters；
- gate/verify 条件的布尔结果。

不得包含 path、payload、length/hash（固定 DLL/probe identity 除外）、文件 inventory、exception message/stack/data、Steam/user identity、profile token、wall-clock 或任何 out-of-scope activity 标记/统计。

artifact 只通过 probe 自有 .NET I/O 写入固定 workspace `evidence/spike-inv006a/`。先写同目录临时文件再原子替换；失败不得更换位置。不得经过游戏 save store。

## 人工测试请求

Implementation Codex 完成源码、测试、Release build、identity/static audit 和安装准备后必须停下，向项目发起人提交严格按 `TEST_PROTOCOL.md` 编写的单轮请求。请求至少包含：

1. 目标仅为 outer-cloud timing gate，不验证存档隔离或 replay。
2. 确认 v0.111.0、Steam Cloud 关闭、当前为模组存档 1 号、只启用 `SPIKEINV006A`。
3. 启动游戏并停留主菜单；不得进入 Continue、单人、多人、设置、profile 选择或任何 run。
4. 等待 probe UI 显示 `Gate Ready`；此前不点击 Verify。
5. 显示后只点击一次 `Verify Stable Gate`，随后立即停止所有操作并回传最终 UI classification。
6. 若出现 probe 错误、游戏错误页、第二个 mod、身份不符、按钮提前可用或任何非预期界面，立即停止，不重试。
7. 达到终态后保持主菜单，不退出游戏，等待 Implementation Codex 明确通知；不得领取奖励或进入任何游戏流程（本轮本应无战斗）。

不得要求用户读取日志、分析文件或手动检查其他 profile。

## 结果分类

- `PASS_SCOPE_SAFE_GATE`：Gate Ready 后单次 Verify 通过，artifact schema/safety/static audit/synthetic tests（含 winner-ticket 负向 case）全部满足。只允许 Project Lead 另发 live-seeded direct-write sentinel。
- `STOP_MISSING_OUTER_TASK`：未捕获唯一 outer task，或 bounded timeout 时仍 pending。不得用 startup/menu/quiet time 替代。
- `FAIL_EVENT_ORDER`：patch 太晚、graphics fault/重复、outer task 重复、manager identity 改变、run state 非 null、current-run task 不符合或 Verify 复核改变。
- `FAIL_SAFETY`：任何 global store/path/payload/profile/save access、override、run/scene、主动 mutation/save、越界 artifact 字段或 post-seal event。
- `BLOCKED_IDENTITY`：固定 game/DLL/probe identity 不符。
- `BLOCKED_INSTRUMENTATION`：测试、静态扫描、无语义改变的 patch 前提或 closed-set proof 不成立。
- `BLOCKED_ARTIFACT_WRITE`：固定 workspace artifact 原子写失败；保持 sealed，不换位置。

首个 terminal classification 后立即停止；不得在同一工作包中修补并再次运行。

## 交付物

1. `project-management/investigation-reports/SPIKE-INV006A-report.md`
2. `experiments/SPIKE-INV006A/README.md`、新源码、测试工程与 Release build instructions
3. `evidence/spike-inv006a/README.md`
4. 单份符合白名单的 runtime artifact（若进入运行）
5. synthetic test 结果、15-case mapping、source/binary forbidden-surface audit
6. build identity、安装产物 identity 与错误 DLL 的 `BLOCKED_IDENTITY` 负向证据

报告严格区分 Confirmed、Inferred、Unknown，并只给一个下一建议：

- PASS：建议 Project Lead 签发全新的 live-seeded direct-write sentinel；
- 其他结果：建议停止或给出一个不扩大数据/运行范围的静态缺口。

## 项目管理边界

Implementation Codex 不得修改 `project-management/CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md` 或 Roadmap。只提交本工作包规定的 experiment、evidence 与 report，由 Project Lead 验收后更新状态。

## 停止条件

- 固定 identity、测试、静态 forbidden-surface audit 或 artifact 写入门禁失败。
- 需要读取 restricted artifact、真实 profile/save/cloud 数据或 patch global store 才能继续。
- 首个 runtime terminal classification 已产生。
- 任一 FAIL/BLOCKED 后不得重试或进入 direct-write。

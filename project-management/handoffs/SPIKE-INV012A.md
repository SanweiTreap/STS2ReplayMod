# SPIKE-INV012A — A4 后继窄诊断（仅 Phase 0 离线准备）

状态：`READY_PHASE0_ONLY`（Project Lead，2026-09-27）。本文件**不授权**安装/启用模组、启动 STS2（含 `--headless`）、请求玩家进行游戏内测试或读取当前用户存档。Phase 0 验收后须由 Project Lead 另行决定是否签发运行阶段。

## 目标与唯一假设

准备一个全新身份、仅观察的 v0.111.0 诊断探针，使未来一次经批准的 profile1 人工运行能够区分：A4 类似情形中原业务进程是否持续 tick、A capture 被哪个 readiness 拒绝或 observer exception 阻止，以及正常退出时两个已知可能 writer 的调用是否发生。当前只验证探针自身合同、目标 patch 和观测不干扰生产方法；**不验证** A/B 重建、存档隔离、原 A4 的历史实际调用或文件内容差异。

先读 `CURRENT_STATUS.md`、`TEST_PROTOCOL.md`、`INV-012-report.md`、`evidence/inv-012/project-lead-review.md`、`deadline-telemetry-contract.md`、`storage-topology-writers.md` 和 `REF-003-HarmonyPatch-assessment.md`。固定游戏 DLL v0.111.0 / SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`；Harmony 按本地 2.4.2.0 核对。其他版本一律 fail closed。

## Phase 0 允许工作

1. 新建唯一 `SPIKEINV012A` 源码身份、manifest 候选、schema 和离线证据目录，不复用 A4 的 ID、artifact/recovery/安装文件或任何业务流程；旧 probe 只读，不修改、构建、安装或运行。可复用经过源码审计的纯工具函数，但不得继承 A4 的 deadline 状态机错误。
2. 用固定 DLL/IL 确定必要 patch 的精确签名及先后；优先观察 `NRun._Notification`、`NGame.Quit`、`RunManager.CleanUp/WriteReplay`、`SettingsSaveManager.SaveSettings`、`CombatReplayWriter.WriteReplay` 的 entry/exit，以及 ticker attach/first tick/阶段。若某目标不能安全只读 patch，记录缺口并停止该项，不扩大到全局 file/store hook。
3. 设计独立于 `_Process` 的有界 monotonic deadline 与原进程 durable telemetry。人工 Ready 等待无 timeout；进入受测阶段后，每个生产保存、checkpoint seal、capture 阶段各自有限时，严格处理恰好到期、迟到 callback、terminal race 和 restart provenance。仅记录 attempt/process nonce、stage、计数、字段名/布尔、异常类型/位置、录制标志和 writer 调用前后元数据；不得记录 seed、房间/卡牌、文件内容、绝对路径、账号 ID、异常消息或 stack trace。记录须限速、限量、create-new，不可覆盖；证据丢失时禁止宣称 no-tick/no-writer。
4. 所有 Harmony patch 保持 observer-only：不 skip original、不改参数/`__result`/`Task`/异常、不使用 Transpiler 或改变异常的 Finalizer。observer 自身失败被隔离为探针证据失败，不得改变 production 成功/fault/cancel。展示源代码和编译后 IL 的 non-interference audit；离线 patch/unpatch smoke 不得调用真实存档 API。
5. 完成合成/race tests：baseline 后永不 ready、ticker 不挂载或不 tick、tick 后逐字段拒绝、observer 抛错、证据写入失败、deadline 前/恰好/后、迟到 callback、terminal 单次提交、跨进程来源区分、writer 进入但未正常返回、录制标志 false/true、无 writer 调用、重复 callback、限量/脱敏。对每个测试说明 oracle 和断言，报告 Release build 与 smoke 结果。
6. 形成未来运行阶段的**草案**：固定 account/modded/profile1 存储拓扑与三段保护窗口；模组启用准备变化与业务窗口分别建基线；只加载本探针，不加载 A4/AscensionUnlocker/其他旧测试 probe；Steam Cloud 现场确认；profile1 是测试槽，其他 profile 不读不切。仅请求用户手动操作，目标达到后立即停止，不领奖、不继续楼层。此草案不构成执行授权。

## 明确禁止与停止

- 不创建 A/B reconstruction、Continue B、直接 progress write、内存 manager override、replay event dispatch、奖励/战斗操作或自制快照；`SPIKE-001B`仍阻塞。
- 不读取、复制或重设当前 account/profile/save/replay/settings/cloud/recovery 内容；不访问其他 profile；不覆盖既有证据。运行日志或路径若必须用于未来设计，只使用固定 IL、历史已封存的最小元数据与路径模板。
- 不以“正常退出可能写入”预先放宽 `settings.save`/`latest.mcr` 的内容保护。Phase 0 只能提出字段/调用/长度/完整性判据及未来如何观测，不能追认 A4，也不能自行决定允许差异。
- 不使用 Computer Use/GUI 自动化，也不自行启动正式 STS2，无论有无窗口。离线独立 Godot 模拟只可在不引用/加载 STS2、隔离 `user://` 与外部连接、并明确标记非生产证明时进行；不是必需项。
- 发现 patch 需要改变 production Task/返回值或诊断无法在严格数据白名单内成立时，停止并报告 `BLOCKED_DESIGN`。Phase 0 构建/测试失败可以修改当前新 probe 代码直至满足离线合同；这是离线实现授权，**不含**任何游戏运行重试授权。

## 交付与验收

- 新探针 Phase 0 源码、固定版本身份清单、精确 patch target/IL 对照、静态数据流与隐私审计、合成测试及 patch/unpatch smoke、构建/二进制 hash；不得复制到 live mods 目录。
- `project-management/investigation-reports/SPIKE-INV012A-Phase0-report.md` 和 `evidence/spike-inv012a/README.md`：Confirmed/Inferred/Unknown、失败/停止情况、运行阶段测试请求草案及保护窗口。不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md`。
- Project Lead 只有在 Phase 0 的目标 patch、安全与数据白名单、deadline/telemetry/race、安装生命周期和保护预声明均可审计后，才考虑授权独立 runtime phase。Phase 0 PASS 本身不允许安装/启动、不证明 A4 首因，也不解锁 successor A/B。

# SPIKE-INV012A Phase 0 离线报告

日期：2026-09-27  
状态：`OFFLINE_GATES_PASS / PROJECT_LEAD_REVIEW_REQUIRED`。此状态只表示本报告列出的离线构建、合成测试和 patch smoke 通过；**未安装、未启用、未启动 STS2，也未请求玩家执行运行测试**。没有 A/B、Continue B、存档隔离或 A4 历史首因结论。`SPIKE-001B` 仍阻塞。

## 范围与固定身份

已先阅读 `CURRENT_STATUS.md`、`TEST_PROTOCOL.md`、`INV-012-report.md`、`evidence/inv-012/project-lead-review.md`、`deadline-telemetry-contract.md`、`storage-topology-writers.md`、`REF-003-HarmonyPatch-assessment.md`。旧 A4 仅作源码/已封存文档的只读参照；其代码、构建和安装均未修改。

新身份是 `SPIKEINV012A`，Harmony owner 为 `STS2ReplayMod.SPIKEINV012A.Phase0`，manifest 候选在 [`SPIKEINV012A.json`](../../experiments/SPIKE-INV012A/SPIKEINV012A.json)。入口在 [`Probe.cs`](../../experiments/SPIKE-INV012A/Probe.cs)，核心状态机在 [`DiagnosticCore.cs`](../../experiments/SPIKE-INV012A/Core/DiagnosticCore.cs)，证据 schema 在 [`diagnostic-record.schema.json`](../../evidence/spike-inv012a/diagnostic-record.schema.json)。本地固定 `sts2.dll` 为 9,757,184 bytes，SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`，`release_info.json` 为 `v0.111.0` / `41cef1ea`；本地 Harmony 程序集版本 `2.4.2.0`。入口在 patch 安装前核对上述四项，不符即返回。

最终 Release 候选为 [`artifacts-phase0-r5/SPIKEINV012A.dll`](../../experiments/SPIKE-INV012A/artifacts-phase0-r5/SPIKEINV012A.dll)，50,688 bytes，SHA-256 `C49EC368E70731A324BAB090E04AECB28F476FDFB6F3CABD5674E45AACA447F5`；同目录 manifest SHA-256 `6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD`。早期候选 `artifacts-phase0/`、`artifacts-phase0-r2/`、`artifacts-phase0-r3/` 保留作修订来源，**不得用于未来安装**。未复制任何文件到 live mods。

## 固定方法、IL 与观察顺序

[`Invoke-Phase0IlAudit.ps1`](../../evidence/spike-inv012a/Invoke-Phase0IlAudit.ps1)先校验游戏 DLL hash、长度和 Harmony 版本，再从固定 DLL 读方法签名和 IL，并检查编译后 probe 的 16 个回调。最终输出为 [`phase0-il-audit-r5.txt`](../../evidence/spike-inv012a/phase0-il-audit-r5.txt)。下表 token 均由该脚本复核；Prefix 位于原方法进入前，Postfix 只在原方法正常返回后执行，不能把未见 Postfix 自动判作文件写入失败。

| 精确目标 | token | 固定 IL 锚点与意义 |
| --- | --- | --- |
| `NMainMenu._Ready()` | `0x06007475` | 启动后菜单观察/loaded-set 门；`INV-005` 已证实 mod 初始化在菜单创建前被等待，菜单 Ready 仍不等于全部 startup 静止。 |
| `NGame._Input(InputEvent)` | `0x06005acd` | F8 仅触发人工 arm；不修改输入事件。 |
| `SaveManager.SaveRun(AbstractRoom,bool): Task` | `0x06000739` | 方法体返回原 `Task`；仅当 arm 后、目标地图坐标匹配、`preFinishedRoom == null` 且 `saveProgress == true` 时观察。 |
| `NRun._Notification(int)` | `0x06005b19` | `what==1006` 时 IL `000a–0010` 调 `RunManager.CleanUp(false)`。 |
| `NGame.Quit()` | `0x06005abe` | 显示字段可在 SaveSettings 前更新；IL `00dd` 保存 Settings，`00f3/00fd/0107` 顺序为 prefs、progress、profile。 |
| `RunManager.CleanUp(bool)` | `0x06000c66` | IL `008f` 读取 `IsRecordingReplay`；为 true 才在 IL `0098` 调 `WriteReplay(true)`。 |
| `RunManager.WriteReplay(bool)` | `0x06000c65` | IL `000a` 求 profile-scoped 路径，`0018` 调 native writer。路径参数不进入探针回调。 |
| `SettingsSaveManager.SaveSettings()` | `0x06000aad` | IL `002f` 走 `ISaveStore.WriteFile`，`0034` 正常返回。 |
| `CombatReplayWriter.WriteReplay(string,bool)` | `0x06001290` | 直接 `FileAccessStream` 写 native replay；`stop` 为 true 时 IL `00ca` 停录。方法内部会捕捉写盘异常，所以 Postfix 仅证明方法正常返回，不证明文件完整。 |

局部调用链分别是 `NRun notification → CleanUp → (IsRecordingReplay ? WriteReplay → CombatReplayWriter : 跳过)` 与 `NGame Quit → SaveSettings → SettingsSaveManager.SaveSettings`。两个 Godot Node 的退出通知**整体先后仍未知**；探针分别在两条通知的正常返回处落一份受限 writer 汇总，不能用静态 IL 指认 A4 那次文件变化。

## Deadline、telemetry 与隐私合同

人工 `ManualReady` 无 timeout。目标 room-before `SaveRun` 入口开始 30 秒生产保存期限；原 `Task` 成功后启动新的 30 秒“探针自有 checkpoint 元数据封存”期限；封存后启动新的 30 秒 capture 期限。此处 checkpoint 元数据仅是探针记录，**不读取、不复制、不验证游戏 checkpoint 字节**。每个阶段持有不可变 `(attempt, process, stage, Stopwatch 起点, 时长, generation)`；timer 每 100 ms 独立于 `_Process` 检查。锁内在成功转阶段之前检查 `now >= deadline`，恰好到期由 timeout 胜出；迟到/重复回调和旧 generation 不能恢复阶段。capture 完成需 20 个字段全部 true、连续 30 帧。一次 terminal 只提交一次；capture terminal 后不再采 readiness，独立的退出 writer 汇总仍可在正常退出时保存，但不能改写 capture 结论。

证据为 `user://SPIKEINV012A/<fresh-attempt>/NN.json`，每个文件 `FileMode.CreateNew` 且 flush；只使用随机 attempt/process/boot nonce，不复用 A4 artifact。最多 12 条常规诊断、2 条退出 writer 汇总和 1 条 capture 完成或 terminal 封存记录（总量不超过 16），每条最多 4 KiB。首个 tick 立即落盘；capture heartbeat 至多每 5 秒一次；逐字段拒绝只落首次项，后续计数及 first/last frame 折入 terminal 摘要。磁盘写失败只发一次无路径的 `SPIKEINV012A evidence_failure` fallback，内部 `EvidenceComplete=false`；缺证据时不能声称 no-tick/no-writer。`capture-ready` 也使用保留封存槽，避免限量丢失成功标记。

[`Readiness.cs`](../../experiments/SPIKE-INV012A/Readiness.cs)在 arm 时从**当时内存中的** map 选择与 A4 相同规则的第一可达普通 Monster，并将 seed、角色、楼层、坐标等仅保存在进程内用于 `targetIdentityStable` 布尔比较；没有将这些值写入 JSON、console 或 exception 文本。20 个字段的落盘值限于名称、`True/False/Missing/Exception`、异常类型/固定 site。`Patches.cs` 的 native writer callback 故意没有接收 string 路径参数；writer 汇总仅含固定方法名、进入/正常返回次数及顺序、录制/stop 布尔。没有 save/replay/settings 内容、绝对路径、账号 ID、seed、房间/卡牌、异常 message 或 stack trace。Record 白名单由核心固定，schema 禁止额外字段；探针仅读游戏 DLL 与其 release metadata 作身份门，并将证据写到自有目录。若汇总超限，标记证据失败，而不是输出超长或敏感值。

要推断“capture 阶段没有 `_Process` callback”，未来必须同一 attempt/process 的 capture `stage-start` 与 timeout terminal 都在、其 tick 差为零、first-tick 证据与该差相容、terminal `Flag=true` 且没有 fallback。跨进程的零 tick 一律不能代替原进程证据。要推断“正常退出未见目标 writer”，还须正常退出后的 writer-summary、已核对的 patch/loaded set 和完整证据；若通知没有正常返回、证据写失败或进程异常结束，结果为 Unknown。

## 非干扰审计与离线验证

源码 [`Patches.cs`](../../experiments/SPIKE-INV012A/Patches.cs)的 16 个回调均为 `void`、只读参数；唯一 `Task __result` 按值传入，`Probe.SaveReturn` 仅注册 `ContinueWith` 读取 success/fault/cancel 状态，不替换、不 await、不读取 fault 的异常消息。每个回调经 `Probe.Safe` 捕获 observer 的 `Exception`，失败转为探针证据终态。没有 Prefix 返回 `false`、`ref/out __result`、Transpiler 或改变生产异常的 Finalizer。编译后 IL audit 强制检查 16 个 callback 全为 void、无 byref 参数或写参数指令、无 Finalizer/Transpiler/Task 结果设置调用，并列出 `Probe.SaveReturn/Safe/Tick/Initialize` 的调用 IL。唯一 Godot scene graph 变化是将**探针自有** ticker 子节点附到 `NGame` 并使其 processing；不调用生产 save、store、replay dispatch 或 manager override。

Release 构建 0 warning/0 error；[`contract-test-output-r5.txt`](../../evidence/spike-inv012a/contract-test-output-r5.txt) 为 **28/28**；[`patch-smoke-output-r5.txt`](../../evidence/spike-inv012a/patch-smoke-output-r5.txt) 为 **9/9 patch/unpatch**，仅安装/卸载 Harmony 包装，未调用任何真实 STS2 目标方法或存档 API。独立 dummy [`noninterference-smoke-output.txt`](../../evidence/spike-inv012a/noninterference-smoke-output.txt) 验证 observer 自抛异常时原方法仍执行，原 `Task` 身份、success/fault/cancel 和同步异常均保持。

合成测试 oracle 与断言如下；每项均在隔离纯 .NET 模型或临时探针目录中运行，不加载 STS2：

| 测试组 | 独立 oracle / 主要断言 |
| --- | --- |
| manual Ready、save wait、ticker 未挂/不 tick、baseline 后永不 ready | FakeClock 明确推进；Ready 不设期限，三个受测阶段各按 30 秒终止，零 tick 不依赖 `_Process`。 |
| 首 tick、20 字段逐一拒绝、observer 异常 | 字段白名单逐项置 false；首 tick 恰一条，拒绝只含字段名，异常只含类型/site 并 terminal。 |
| evidence 写失败、字段/token 非法、限量及 capture 封存 | 失败 sink 返回 false；precheck/arm 写失败阻断 Ready；首 tick 瞬时写失败后不得封存 capture 成功；非法字段阻断，非法 token 脱敏；create-new、4 KiB 门及 16 条上限，capture 完成记录不被 quota 吃掉。 |
| 期限前/恰好/后、迟到 callback、旧 timer、terminal/真实并发 race | FakeClock 与 `Parallel.Invoke`；`>=` 期限由 timeout 胜出，100 次竞争只一条 terminal，迟到/重复 callback 不改分类。 |
| 30 连续帧、跳帧、save fault/cancel、独立 seal 期限 | 第 30 帧才成功，跳帧重置；原任务 fault/cancel 分类不同；seal 阶段有独立到期。 |
| 跨进程 provenance、writer 进未返、flag false/true、无 writer、terminal 后退出汇总 | 不同 nonce 不能续前 attempt；汇总分别保留进入/返回计数、布尔与顺序，capture terminal 不被 writer 改写。 |

## Confirmed / Inferred / Unknown 与停止边界

**Confirmed（离线）**：固定 DLL/IL 签名和所列局部调用顺序；Harmony 2.4.2.0；最终 Release 构建、静态回调审计、28/28 合成合同、9/9 patch/unpatch、dummy non-interference smoke。Phase 0 未接触当前 account/profile/save/replay/settings/cloud/recovery 内容，未读取其他 profile，未启动游戏。

**Inferred**：若未来运行 loaded set 与固定身份门均通过，ticker 与 writer 汇总的字段足以区分多数 readiness 拒绝和两个已知退出 writer 的调用资格。性能、Godot 退出时两个 Node 的实际顺序以及任何文件内容因果仍需受控运行证据。

**Unknown / 需 Project Lead 处理**：真实运行中 ticker attach/first tick、20 字段的值、observer 实际异常、录制标志、两个退出通知及 writer 调用、内部吞掉的 replay 写盘失败、完整 account/profile 保护差异。探针不读取 native 文件长度/完整性或 settings 字段；其 writer summary 只说明调用，不证明字节变化来源或可允许。同步磁盘 I/O 的操作系统级永久阻塞无法由进程内 timer 保证有界持久化；此时缺少封存证据必须归 Unknown。Phase 0 无设计缺口需要扩大到全局 file/store hook；若 Project Lead 要求在探针内证明实际文件完整性或在文件系统永久阻塞时仍保证 durable deadline，则现有严格白名单不能满足，应判 `BLOCKED_DESIGN` 并另议边界。

最初离线 smoke 查出 `CleanUp(bool graceful)` 和 `WriteReplay(bool stopRecording)` 的参数名不能凭猜测绑定；已改为不接收无关参数或以 Harmony 参数索引绑定并重新通过。dummy smoke 首次仅有测试调用计数断言笔误，已修正重测。r3 补强 `PatchAll` 中途异常时按本 probe owner 尝试卸载部分 patch；r5 再修复“瞬时证据写失败后仍可能先写 capture-ready”及 precheck/arm 失败误报 Ready，并补回归测试。每次候选均重新构建，最终 r5 的 IL 审计、contract 与 patch smoke 全部通过。旧 artifact 保留；没有运行阶段失败或游戏进程重试。

## 未来运行阶段测试请求草案（**不是执行授权**）

**目标**：只在获批的 modded profile1 人工场景中观察原业务进程 ticker/阶段、A 类 capture readiness 或 observer 阻断、录制标志和正常退出 writer 调用；不做 A/B、不点 Continue B、不解释 A4 历史字节。

**安装/启用门**：先保持新候选未安装，由项目发起人手动禁用 A4、AscensionUnlocker 和全部退役测试 probe 并正常退出；以无新 probe 的验证启动日志或范围明确的持久 enabled-ID 配置证明禁用已保存，再退出。随后另行盘点 live mods 中各项目 DLL/manifest 的 hash 与 enabled 状态，只有获批的 `SPIKEINV012A` r5 可安装；启动后在菜单边界验证 loaded set 恰为该 ID、固定 DLL/Harmony、modded profile1、Steam Cloud 现场关闭。模组启用造成的设置变化必须在**准备窗口**独立建基线，不可并入业务窗口。任何身份、profile、Cloud、loaded set 或证据门不符，停止，不执行 F8/业务动作。安装与启动仍须新的 Project Lead 授权。

**三段保护窗口和判据**（仅设计，尚不采集当前文件）：`<U>=user://`，`<S>=<U>/steam/<account-id>`，`<M>=<S>/modded`，`<P>=<M>/profile1`；这些是路径模板，绝不落盘账号数字。保护 inventory/hash/length/mtime 覆盖 `<S>/settings.save` 及备份/tmp、`<M>/profile.save` 及备份/tmp、`<P>/saves/{current_run,progress,prefs}.save` 及备份/tmp、`<P>/replays/latest.mcr`。settings 是 account-local，profile selector 属于 modded account，三类 saves 属于 profile1，native replay 直写本地且绕过普通 `ISaveStore`；实际 `user://` 根须当次只读确认。窗口 1 为“准备变化结束后的业务 baseline → capture 或 timeout”，只预声明目标 room-before production save 对 `current_run.save` 的限定变化，其他内容/inventory 差异阻断；窗口 2 为“capture/timeout → 正常退出”，分别记录 `NGame.Quit/SettingsSaveManager` 与 `NRun/CleanUp/WriteReplay/native writer` 调用前后，**调用并不允许** `settings.save` 或 `latest.mcr` 内容变化，两者如变仍 FAIL，既定 mtime-only 项须 hash/length 不变；窗口 3 为“退出后 → 下一次启动前/启动后只读门”，仅在另行批准下一进程时观察 startup 变化，不执行 Continue，不把它并入窗口 1/2 或 B reload 差异。每窗独立 seal，不覆盖旧基线；profile1 以外不读不切。

**人工动作草案**：项目发起人在获批、已建业务 baseline 的 profile1 Act 1 标准地图停在目标前，按 F8 一次，确认通用 `READY_A_NODE` 标记；只点由 arm 规则选出的第一可达普通 Monster 节点，随后立即停手，等待 `CAPTURE_READY_STOP_INPUT` 或 `PROBE_TERMINAL_STOP_INPUT`。不出牌、不 End Turn、不打开选择、不领奖励（金钱、卡牌、药水、遗物均不领）、不继续楼层。目标标记出现或 30 秒无标记时保持停手并按批准的正常退出步骤退出；异常、存档/Cloud/loaded-set 门失败立即停止。回传只需通用标记、探针自有 `NN.json`、各保护窗口的 inventory/hash/length/mtime 摘要及人工观察，不回传 save/settings/replay payload、账号数字或其他 profile。达到目标后不再游玩。

**未来字段/长度/完整性决策门**：若 Project Lead 考虑允许退出显示字段或 native replay 的限定差异，须在运行前另定 `SettingsSave` 中可变显示字段的精确投影与 unchanged 字段断言、native replay 允许长度范围/反序列化完整性/调用与时点关联、备份/tmp inventory 规则和失败处理。当前探针只观察调用，不提供该字节 oracle；A4 的 `settings.save`/`latest.mcr` 差异维持 FAIL，不能追认。



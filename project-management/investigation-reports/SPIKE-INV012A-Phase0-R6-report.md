# SPIKE-INV012A Phase 0-R6 离线修订报告

日期：2026-09-28  
状态：`OFFLINE_GATES_PASS / PROJECT_LEAD_REVIEW_REQUIRED / RUNTIME_HOLD`。这是同一 `SPIKEINV012A` 探针的离线修订；未安装或启用模组，未启动 STS2，未读取当前玩家存档/设置/replay/cloud，也未请求游戏内测试。A/B 与 `SPIKE-001B` 仍阻塞。

## 范围与 r5 差异

执行前阅读了 [`CURRENT_STATUS.md`](../CURRENT_STATUS.md)、[原工作包](../handoffs/SPIKE-INV012A.md)、[r5 报告](SPIKE-INV012A-Phase0-report.md)、[Project Lead 复核](../../evidence/spike-inv012a/project-lead-phase0-review.md) 和 [R6 工作包](../handoffs/SPIKE-INV012A-Phase0-R6.md)。r5 候选、manifest 和既有证据均原样保留。

| 复核缺口 | r6 精确修订 | 离线 oracle |
| --- | --- | --- |
| `stage-start` 写失败后还有 `save-entry` / `save-complete` | [`DiagnosticCore.cs`](../../experiments/SPIKE-INV012A/Core/DiagnosticCore.cs) 的 `Start` 在同一状态锁内返回是否仍可继续；两个调用方仅在 true 时写业务记录。 | 一次 `stage-start` 失败、下一次 sink 写恢复：仅一条 terminal，terminal 为业务 lane 最后一条；分别覆盖 ProductionSave 与 CheckpointSeal。正常记录和 terminal 后独立 writer-summary 仍成立。 |
| 身份门前写 boot；身份拒绝或 ticker owner 缺失时静默 | [`PrebusinessGate.cs`](../../experiments/SPIKE-INV012A/Core/PrebusinessGate.cs) 先检查**完整** loaded set、manager 存在、modded profile1，再调用 attempt factory；异常也只输出 `SPIKEINV012A PRECHECK_BLOCKED`。[`Probe.cs`](../../experiments/SPIKE-INV012A/Probe.cs) 仅在通过后创建证据目录；拒绝在本进程锁定 F8。通过身份门但 `NGame.Instance` 无效或 ticker 无法附加时封存 `PrebusinessOwnerUnavailable` terminal，并输出 `SPIKEINV012A PROBE_TERMINAL_STOP_INPUT`。 | 不符/异常身份的合成入口没有调用 factory、没有探针目录、没有 Arm/业务调用，日志只有固定无 payload 标记；owner 缺失或附加异常只有 prebusiness terminal，无 `manual-ready` / `save-entry`。 |

`Precheck(true)` 现在只在身份门通过、attempt 创建后写入。没有新增 patch、生产方法调用或文件 payload。目标 9 个、16 个 callback、readiness 20 字段、30 连续帧、各阶段 30 秒、writer 白名单、证据记录 schema、create-new/限量和保护窗口保持 r5 合同。

## 候选与全量离线验证

新候选：[`artifacts-phase0-r6/SPIKEINV012A.dll`](../../experiments/SPIKE-INV012A/artifacts-phase0-r6/SPIKEINV012A.dll)，**52,224 bytes / SHA-256 `CA1DAF85B5C12AE043A2BF20E7D9026DA894D336F649913A7E1D1BE2C69C1720`**；同目录 [`SPIKEINV012A.json`](../../experiments/SPIKE-INV012A/artifacts-phase0-r6/SPIKEINV012A.json)，**375 bytes / SHA-256 `6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD`**。身份仍为 `SPIKEINV012A` / Harmony owner `STS2ReplayMod.SPIKEINV012A.Phase0`。固定游戏 DLL v0.111.0 SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`，Harmony `2.4.2.0`。

r5 DLL 仍为 50,688 bytes / SHA-256 `C49EC368E70731A324BAB090E04AECB28F476FDFB6F3CABD5674E45AACA447F5`；r5 manifest hash 仍为 `6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD`。r5 仅作历史离线证据，两个版本均未获安装许可。

| 门 | r6 结果与独立输出 |
| --- | --- |
| Release build | 0 warning / 0 error；[`build-output-r6.txt`](../../evidence/spike-inv012a/build-output-r6.txt)。 |
| 纯 .NET 合同 | **35/35**；r5 的 28 项加 7 项针对阶段写失败、正常 writer lane、身份拒绝/异常及 ticker owner 缺失/附加异常的测试；最终输出 [`contract-test-output-r6-final.txt`](../../evidence/spike-inv012a/contract-test-output-r6-final.txt)。最初同为 35/35 的 [`contract-test-output-r6.txt`](../../evidence/spike-inv012a/contract-test-output-r6.txt) 保留；此后仅给测试临时目录清理增加绝对路径边界检查并重跑。 |
| 精确目标 patch/unpatch | **9/9**，未调用目标方法；[`patch-smoke-output-r6.txt`](../../evidence/spike-inv012a/patch-smoke-output-r6.txt)。 |
| dummy 非干扰 | observer 自抛异常后原方法、Task 身份及 success/fault/cancel、同步异常均保持；[`noninterference-smoke-output-r6.txt`](../../evidence/spike-inv012a/noninterference-smoke-output-r6.txt)。 |
| 固定 DLL / 编译后 IL | 9 个目标、16 个只读回调，hash gate 通过；[`phase0-il-audit-r6.txt`](../../evidence/spike-inv012a/phase0-il-audit-r6.txt)。其 SHA-256 与 r5 相同 `CF6BF14BF67648FE674083846EABAFB546F8304E2AA31324B00AF6B275B12F4F`，因为原审计列出的目标/回调与四个 Probe 方法均未改变；另以 [`phase0-il-delta-audit-r6.txt`](../../evidence/spike-inv012a/phase0-il-delta-audit-r6.txt) 检查 r6 的 `Start` 布尔返回、调用方条件分支和身份判定先于 attempt factory 的编译后顺序。 |
| 数据白名单 | [`data-whitelist-audit-r6.txt`](../../evidence/spike-inv012a/data-whitelist-audit-r6.txt)：编译后 `Record` 字段与禁止额外字段的 schema 一致；native writer 两个 callback 均无 string 路径参数；仅有的两个 `File.ReadAll*` 调用都在固定游戏 DLL/release 身份门。源码复核 `EvidenceSink` 仍只写探针自有目录的 create-new JSON，日志标记不含身份或路径。 |

所有合成文件均在独立临时探针目录创建，清理前验证绝对路径在临时目录且带本测试前缀；未触碰正式 `user://` 或 live mods。验证脚本与输出均在 [`evidence/spike-inv012a`](../../evidence/spike-inv012a/README.md)，旧输出未覆盖。

## 结论边界

**Confirmed（离线）**：Project Lead 指出的两处状态/预检缺口已按上述 oracle 收束；固定版本构建、合同、目标 patch/unpatch、dummy 非干扰、编译后 IL 和数据白名单审计通过。生产 Task/返回值不被修改。

**Inferred**：在未来获准的 modded profile1 且仅加载本探针的运行中，身份拒绝应只有 `PRECHECK_BLOCKED`，ticker owner 缺失应留下 prebusiness terminal；这个推断尚未在 STS2 运行时观察。

**Unknown**：真实 ticker attach/tick、20 字段、退出通知先后及 writer 调用/文件完整性、A4 原进程首因、保护文件差异和任何 A/B 结果。同步 I/O 永久阻塞仍只能归 Unknown。r6 离线 PASS 不解锁安装或游戏测试；交回 Project Lead 复核并等待独立运行阶段决定。

## 更新后的未来人工运行草案（不构成执行授权）

仅在 Project Lead 另行签发运行阶段后，由项目发起人手动准备与操作。候选指向上列 **r6** hash；r5 不进入安装清单。准备阶段先确认 A4、AscensionUnlocker 和退役 probe 全部禁用且持久化，再逐项核对 live mods 的 DLL/manifest hash 与 enabled 状态；业务启动时 loaded set 必须恰为 `SPIKEINV012A`，固定游戏/Harmony 身份、modded profile1 与 Steam Cloud 现场关闭均须成立。模组启用产生的变化单独建准备基线，不并入业务窗口。

启动后，`SPIKEINV012A PRECHECK_BLOCKED` 表示身份门拒绝且没有 attempt；`SPIKEINV012A PROBE_TERMINAL_STOP_INPUT` 包括 ticker owner 无效或其他证据/观察终态；两者均停止，不按 F8、不做业务动作。只有明确的 `SPIKEINV012A PRECHECK_PASS` 且 ticker 附加可用，才按既有草案进入人工 Ready。随后在已建业务 baseline 的 profile1 Act 1 标准地图，手动按 F8 一次，见 `READY_A_NODE` 后仅点第一可达普通 Monster；立刻停手，等待 `CAPTURE_READY_STOP_INPUT` 或 terminal 标记。不出牌、不 End Turn、不领奖、不继续楼层；30 秒无标记或任何异常均保持停手并按届时批准的正常退出步骤处理。

保护窗口仍为 r5 预声明的三段：① 准备变化结束后的业务 baseline → capture/timeout；② capture/timeout → 正常退出；③ 退出后 → 若另获批准的下一次启动前/启动后只读门。每窗独立 inventory/hash/length/mtime 封存，覆盖 account-local `settings.save`、modded `profile.save`、profile1 的 `current_run/progress/prefs.save`、`latest.mcr` 及其 backup/tmp；profile1 以外不读不切。窗口 1 只预声明目标 production `current_run.save` 的限定变化；窗口 2 的 writer 调用并不允许 `settings.save` 或 `latest.mcr` 内容变化；既定 mtime-only 项要求 hash/length 不变。窗口 3 不执行 Continue。任何不符仍按原 FAIL/停止门处理，不追认 A4 差异，也不放宽 settings/native replay 的字段、长度或完整性判据。

若运行阶段将来获批，回传只需固定通用标记、探针自有 `NN.json`、三段保护窗口的 inventory/hash/length/mtime 摘要与人工观察；不回传存档、设置、replay payload、账号数字或其他 profile。此草案由 Project Lead 再审，不是向项目发起人发出的操作请求。

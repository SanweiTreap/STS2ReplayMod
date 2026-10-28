# INV-012 — A4 后基线捕获活性与退出写入边界

日期：2026-09-27  
范围：纯静态；建议 `READY_NARROW_DIAGNOSTIC`（仅提交 Project Lead 验收）

## 结论与证据边界

固定 v0.111.0 `sts2.dll` 为 9,757,184 bytes / SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。本次先核对长度和 hash，再以 Mono.Cecil 读取 IL；可复现脚本、method token、IL/caller 输出见 [`evidence/inv-012/`](../../evidence/inv-012/README.md)。A4 行为以只读 A4 源码、封存 arm/baseline/terminal **元数据**、[停滞记录](../../evidence/spike-001a4/phase-a-stalled-after-baseline.md)、[退出审计](../../evidence/spike-001a4/post-exit-audit.md)和 [Project Lead 最终复核](../../evidence/spike-001a4/project-lead-final-review.md)为证；既有 `TOOL-001` 导航缓存未单独作为结论证据。已读 `TEST_PROTOCOL.md` 2026-09-27 存储拓扑和无头增补。没有启动或控制游戏，未使用 Computer Use，未读当前 profile/save/replay/settings/cloud/recovery payload、其他 profile 文件，未修改、构建或运行 A4，未创建 successor probe，未覆盖封存 artifact。

A4 的唯一业务 attempt 封存了 production room-before baseline 和 recovery，却没有 A snapshot、B 或 A/B compare。可静态确定的首个 harness 缺口是：production save 完成后启动的单一 30 秒计时器在 baseline 封存时被取消，且其 expiry 回调在 baseline 存在时直接返回；所以 baseline 后即使 readiness 永不成立也不会按合同在 30 秒内 terminal。A4 那次 readiness 未完成的动态首因仍未知，不能把此缺口或无 A/B 结果写成原生重建差异。退出后的保护比较另有两项内容失败，不能事后豁免。

## 固定版本存储拓扑

完整 owner、读写 API、Cloud 和备份表见 [storage-topology-writers.md](../../evidence/inv-012/storage-topology-writers.md)。路径模板中的 `<U>` 是当前 Windows 安装的 Godot `user://` 根 `%APPDATA%/SlayTheSpire2`；`<S>=<U>/steam/<account-id>`；`<M>=<S>/modded`；`<P>=<M>/profile1`。`<account-id>` 是占位符，不是玩家身份。固定 IL `UserDataPathProvider` `0x0600086d–0x06000874` 构造 account/profile 路径，path getter `0x06000a61/0x06000a67/0x06000a6e/0x06000a97/0x06000aa9` 确定文件作用域；A4 的 `ProtectionScope` 与既有封存日志元数据交叉印证本机根。Steam 游戏安装和 mods 目录不是 `user://`。

| 范围 | 模板 | 归属和退出行为 |
|---|---|---|
| account | `<S>/settings.save`、其 `.backup/.tmp` | 本机 account 设置与模组配置；独立 local `GodotFileIo`；`NGame.Quit` 调用 SaveSettings，窗口位置等可能先更新。**不属于 profile1**。 |
| modded account | `<M>/profile.save`、其备份 | 当前 profile 选择；profile store；已初始化 profile 的正常退出保存。 |
| profile1 saves | `<P>/saves/current_run.save`、`progress.save`、`prefs.save` 及各自备份 | Continue、进度、偏好分别由 Run/Progress/Prefs manager 管理；退出保存 prefs/progress，不由 `NGame.Quit` 直接重存 current run。 |
| profile1 native replay | `<P>/replays/latest.mcr` | 游戏原生 replay/诊断写入，由 `CombatReplayWriter` 直接写本地文件；不是未来产品录像，也不走普通 `ISaveStore.WriteFile`。 |

`SaveManager.ConstructDefault` `0x06000732` 在 Steam 初始化时将 profile store 包为 `CloudSaveStore`，但 settings store 始终独立 local；`CloudSaveStore.GetFullPath` `0x0600044f` 将 replay 的实际路径交给 local store。Cloud 是否关闭仍需每次未来运行单独确认；“Cloud off”不是跳过本地保存或直接 replay writer 的证明。

## A4 时序、活性缺口与新合同

| 阶段 | A4 可核实状态 | 计时和证据结论 |
|---|---|---|
| arm | `arm.json` 2026-09-24 17:04:22 +08:00，原业务进程 PID 39508，`READY_A_NODE` | 人工选点等待没有自动 deadline，合理；目标点击后才需要有界生产保存等待。 |
| production save completion | target click 命中，room-before save 成功，战斗创建 | `ObserveCheckpointSuccessAsync` 调用 `StartDeadline("production-save-task-completed")`，单调计时 30 秒；此前从目标点击到原 Task 完成没有独立等待上限。 |
| baseline/recovery seal | `checkpoint-baseline.json` 17:11:29 +08:00；baseline 51,213 bytes，recovery 54,765 bytes；PID 39508 | validation 后 create-new 写入；同一方法紧接着 `_deadline.Complete()`。expiry 回调 `Terminal.IsSealed || Exists(BaselineFile)` 即返回。计时器因此不覆盖 A readiness/snapshot。 |
| A readiness → A snapshot | `ProbeTicker._Process → Tick → ReadinessReader.Read`，20 个 required fields、连续 30 个 process frames，同 tick recheck | source 中只有 ready 满 30 帧才调用 `CaptureA`；`TelemetryCollector` 的 first/transition/breaker/exception/periodic 都在内存，只有 snapshot/terminal 才持久化。172 秒后 A/terminal 皆无，UI 仍旧 `READY_A_NODE`。不能由此断言原进程未 tick。 |
| 原进程退出 | 用户 Alt+F4，完成日志含 `NGame.Quit` 和 Steamworks shutdown | `current_run.save` 仍为 54,765 bytes / `18D2BBAB2556A0FEB8633C0800349CE97C0D803880041CB574615E588F9D1ADB`；保护 inventory 相等，但 `latest.mcr` 内容/长度变（43,980→4,710 bytes），`settings.save` 同长度 5,582 bytes 而 hash 变；保护 FAIL。 |
| 后续无 run resume | `terminal.json` 17:21:02 +08:00，PID 50544，`FAIL_HARNESS_AFTER_BASELINE / attempt.resume` | `totalTicks=0` 属于 **PID 50544 的新进程**；不是 PID 39508 的 ticker 证据。restart 发现 formal baseline 无 A，按 A4 合同 terminal；不得重跑或据此构造 B。 |

A4 旧测试覆盖了零 tick 的计时器本体、计时器取消、race 和 terminal latch，却没有覆盖“baseline 封存取消唯一 timer + expiry baseline guard + readiness 永不成立”这一组合。新合同见 [deadline-telemetry-contract.md](../../evidence/inv-012/deadline-telemetry-contract.md)：目标 save Task、checkpoint seal、baseline 后 A capture、B Continue 后 capture 分别拥有 ticker-independent 单调上限；人工 `READY_A_NODE`、A 退出、`READY_B_CONTINUE` 不计超时。每个成功转移先核验本阶段期限，再在同一锁下完成与启动下一阶段；恰在期限上的 timeout 获胜。deadline/RoomEntered/observer exception 使用同一 compare-exchange terminal latch，terminal 后禁止继续采集。observer-only 异常只能 best-effort terminal，不得改变 production Task 的 success/fault/cancellation 语义。重启只能按封存 stage/process identity terminal，不能复用跨进程 `Stopwatch` 时间戳。纯 .NET 合成模型 [14/14 通过](../../evidence/inv-012/contract-test-output.txt)，覆盖永不 ready、前/恰好/后期限、人工等待、production save 等待、B 起点、race、observer、restart provenance 和 writer 分类；这些是合同测试，不是游戏运行证明。

下一探针的原进程最小 telemetry 必须在 baseline 后、A snapshot 前就持久：attempt/process/boot nonce、stage、monotonic start、ticker attach 与 first tick、聚合的 rejection/exception type/site、deadline completion/expiry。只保留字段名/状态/布尔与计数，不存 seed、卡牌、房间、账号、路径或 payload；按 5 秒节流，至多 16 个、每个 4 KiB 的 create-new 记录，失败则报告证据不完整。这样 `deadline 已落盘且 first-tick=0` 与 `tick>0 但 predicate 一直拒绝` 才可区分；任何写盘失败仍不得声称“原进程确实未 tick”。此诊断仅为 probe 合同，不能外推正常录制性能。

## 退出 writer：Confirmed / Inferred / Unknown

**Confirmed（固定 DLL/IL 与封存元数据）**

- `NRun._Notification(1006)` `0x06005b19` 调 `RunManager.CleanUp(false)`；`CleanUp` `0x06000c66` 在 `CombatReplayWriter.IsRecordingReplay` 为真时调用 `WriteReplay(true)`。`RunManager.WriteReplay` `0x06000c65` 定位 `replays/latest.mcr`；`CombatReplayWriter.WriteReplay` `0x06001290` 直接建目录、`FileAccessStream` 写，`stop` 参数可结束录制。end-combat、divergence 和日志收集亦存在 `WriteReplay` caller，故它不是退出独占 writer。
- `NGame._Notification(1006)` `0x06005abd` 调 `NGame.Quit`；`Quit` `0x06005abe` 可先修改 window size/target display/position，随后同步 `SaveSettings`，若 profile 初始化，再按 prefs→progress→profile 的方法体顺序保存，最后 `SceneTree.Quit`。`SettingsSaveManager.SaveSettings` `0x06000aad` 使用 local `ISaveStore.WriteFile`，`GodotFileIo` 通过 backup/tmp/rename 更新文件。A4 完成日志确认 `NGame.Quit`、显示设置保存和 shutdown；封存审计确认上述两项内容变化。
- IL 给出各回调**内部**顺序，却不能给出两个 Node 退出通知在那次关机中的整体先后，也不能证明具体文件变更与某个调用一一对应。

**Inferred（条件性生产机制）**

- 活跃 run 若已开始原生 replay 录制，退出时 `NRun` cleanup 足以解释 `latest.mcr` 被缩短/改写的**机制**；A4 真实调用方、`IsRecordingReplay` 值和写入时点没有逐调用记录，不能把其内容差异归因为已确认的 cleanup 写。
- `NGame.Quit` 的显示字段更新和 SaveSettings 足以解释同长度不同 hash 的**机制**；A4 实际变更字段和是否另有 settings writer 未经 payload diff/调用审计确认。

**Unknown**

- 原业务进程 ticker 是否 tick、哪个 readiness 字段/异常阻止 A、NRun 退出通知是否命中及其与 NGame 的真实顺序、两项内容差异的精确字段/事件归因、可能存在的额外 mod writer。封存后的 profile3 cleanup 是另一个违反 no-run 合同的进程，不纳入 A4 业务进程归因；不读、不恢复 profile3。

## 下一控制的保护窗口与决策门

| 窗口 | 必须比较 | 预先可按固定证据声明的操作 | fail closed 条件 |
|---|---|---|---|
| baseline→A snapshot（退出前） | `current_run.save` 原字节、account/modded/profile1 文件与目录 inventory、hash、length、mtime；loaded set/profile/Cloud | 只有预定 room-before `current_run.save` 到 sealed baseline；snapshot 后不得再改变 checkpoint | 任一其他内容/length/inventory 变化、未知 writer、额外 gameplay、A 缺失/超时。 |
| A snapshot→A 后正常退出 | A 时点与退出后分开比较 account settings、modded selector、profile1 saves/backups/native replay 及 checkpoint；记录调用前后 owner/flag | `NGame.Quit` 的 settings/prefs/progress/profile **调用**，以及 `IsRecordingReplay=true` 时 `CleanUp/WriteReplay` 的条件性调用，均可预声明为可能发生；mtime-only 的既定三项仍须 hash/length 不变 | 调用资格不自动允许内容差异。`settings.save` 或 `latest.mcr` 内容变化，若无前置字段/字节和调用合同，仍 FAIL；其他非白名单变化亦 FAIL。 |
| 退出后→B 启动前/Continue 前 | 新进程加载前后单独记录 account/profile inventory 与 hash/length/mtime、Cloud/loaded set、checkpoint raw bytes、Continue canonical 与 reload count | 已确认启动路径可能读/初始化/迁移；若实际写入，需事先单列合同与时点 | 不能将退出变化或 startup 写入并入 B 的 `NumReloads+1`；保护或 checkpoint 不符即阻断 B。 |

**Project Lead 决策请求（未来工作包，当前不改门槛）：** 是否允许在 A 后退出窗口将可证明由 `NGame.Quit` 产生的限定显示字段变化、以及可证明由 `NRun.CleanUp/WriteReplay` 产生的限定 native `latest.mcr` 变化纳入预声明差异；需先确定字段/长度/完整性规则、调用证据和保护集合是否调整。A4 已发生的两项内容差异仍是 FAIL，不能追认。下一次独立、安全、最小观测应使用全新诊断身份和隔离基线，在获批的 profile1 场景中只观察 ticker/阶段心跳、`IsRecordingReplay`、NRun/NGame 退出回调和这些精确 writer 的调用前后 metadata；不做 A/B，也不读取其他 profile。只有该诊断及 Project Lead 预声明写入合同获验收后，才考虑 successor reconstruction control。

## 无头可行性与唯一下一建议

[无头可行性矩阵](../../evidence/inv-012/headless-feasibility.md)区分纯 .NET/mock、独立 Godot `--headless` 和正式 STS2 `--headless`。固定 IL `NGame.IsReleaseGame` `0x06005ab6` 恒真；`GameStartup` `0x0600b59f` 因而跳过 `autoslay → AutoSlayer.Start`。本次有界 `CommandLineHelper` caller 扫描没有证实可替代的官方 release 自动测试 CLI。`InitializeGraphicsPreferences` 遇 `headless` 只跳过显示设置应用，仍访问 `SaveManager`；startup 仍走 Steam/SaveManager 初始化。不能把无窗口当作不写用户数据或可自动跑局。三层均未启动新的 Godot/STS2 runtime 实验；只运行了明确要求的隔离 .NET 合成合同测试。

**唯一建议：`READY_NARROW_DIAGNOSTIC`。** 静态证据足以界定 deadline/telemetry 修订和两个生产 writer 的条件路径，但不足以确定 A4 原进程首因或追认保护差异。仅交 Project Lead 复核；A4 永久退役，`SPIKE-001B` 继续阻塞。



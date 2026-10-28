# SPIKE-INV012A runtime 窄诊断报告

日期：2026-10-09。**Implementation 结论：`RUNTIME_DIAGNOSIS_COMPLETE / AWAITING_PROJECT_LEAD_REVIEW`。** 本报告只回答原进程 ticker、20 字段 readiness 和正常退出 writer 调用。未执行 A/B、checkpoint 重建、seek、isolation 或 crash recovery；`capture-ready` 不是 A snapshot，`SPIKE-001B` 不因此解锁。

## 固定身份与会话边界

游戏固定为 `v0.111.0 / 41cef1ea`，`sts2.dll` 9,757,184 bytes / SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`，Harmony `2.4.2.0`。仅在 modded profile1 操作，用户在每次手动启动前确认 Steam Cloud 关闭；Cloud 状态未由机器独立读取。Implementation 未安装游戏本体、未自主启动或控制游戏 UI、未进入 unmodded profile1，也未读取生产 save/replay payload。

| 会话 | 结果 | 业务 attempt |
| --- | --- | --- |
| 仅配置会话 | 退出后只读确认 `mods_enabled=true`、modded selector=1、仅 `SPIKEINV012A` 持久 enabled；启动时曾同时加载 AscensionUnlocker 与 r6，未开局。见[配置复核](../../evidence/spike-inv012a/runtime/config-session-review-2026-10-09.json)。 | 无 |
| r6 主菜单启动 | 实际只加载 r6、`precheck=true`；无 ticker attached/first tick，0 帧。用户未开局或按 F8，正常退出；退出 hook 写出摘要。见[主菜单证据](../../evidence/spike-inv012a/runtime/formal-stage1-prebusiness-2026-10-09.json)。 | 无 |
| `runtime-r1` fresh process | 实际只加载 `SPIKEINV012A`、modded profile1；`precheck=true`、ticker attached、first tick、`PRECHECK_PASS`。用户手动进入 Standard 单人 Act1 地图，只按一次 F8，`READY_A_NODE` 后点击直接可达的最左侧普通 Monster，达到 `CAPTURE_READY_STOP_INPUT`，再经游戏 UI 正常退出。见[完整 attempt 索引](../../evidence/spike-inv012a/runtime/attempt-runtime-r1-2026-10-09.json)。 | 一次完整窄诊断 |

`runtime-r1` 独立候选为 [`SPIKEINV012A.dll`](../../experiments/SPIKE-INV012A/artifacts-runtime-r1/SPIKEINV012A.dll)，52,224 bytes / SHA-256 `B4F18ACEB2D738B630A24F57F92B07AED80786A5AAF522D6C35A165052969D6A`；manifest SHA-256 `6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD`。安装与退出后 live DLL hash 一致；r6 DLL/manifest 和 Phase 0 证据均保留。

## 真实 ticker、readiness 与退出 writer

| 观测 | 本次结果 |
| --- | --- |
| 原进程 ticker | `ticker-attached=true`，`first-tick=1`。F8 手动 Ready 时累计 6,777 帧；入房生产保存入口 8,651 帧；`capture-ready` 8,855 帧；退出 `writer-summary` 12,474 帧。ticker 在 capture-ready 后仍推进 3,619 帧，直到退出回调。 |
| 保存与 capture 阶段 | 有序记录为 `save-entry` → `save-complete` → `checkpoint-metadata-sealed` → `Capture`。最初一帧 `playerCombatStatePresent=False`；此后探针按固定 20 字段合同达到 **30 连续帧** ready，`capture-ready` 记录 `count=30`、`IsRecordingReplay=true`。日志标记顺序为 `PRECHECK_PASS` → `READY_A_NODE` → `CAPTURE_READY_STOP_INPUT`。 |
| native replay writer | `CombatReplayWriter.WriteReplay` entry=1、return=1，`stop` entry/return 均为 `true`。调用出现在 `RunManager.CleanUp` entry 与 return 之间；后者 entry=1、return=1，录制 flag 从 `true` 到 `false`。 |
| native settings writer | `NGame.Quit` entry=1、return=1；其间 `SettingsSaveManager.SaveSettings` entry=1、return=1。 |
| 未出现的精确 hook | 本次 writer-summary 未列 `NRun._Notification` 或 `RunManager.WriteReplay` 的调用；不补造调用，也不把其他已观测 writer 的记录归到它们名下。 |

退出后游戏进程数为 0，14 条探针自有 JSON 均不超过 4 KiB，未见探针 exception/evidence_failure 标记。逐条 SHA-256、日志 hash、candidate identity、用户手动动作与 writer 原始汇总见[attempt 索引](../../evidence/spike-inv012a/runtime/attempt-runtime-r1-2026-10-09.json)。writer entry/return 只证明方法调用和记录的 flags；**不证明** `latest.mcr`、`settings.save` 或任何生产文件的具体内容变化由它们造成，也不构成隔离或重建成功。

## harness 缺陷与修复

r6 主菜单记录证明身份门与 Harmony 主菜单/退出 patch 已运行，而自定义 `DiagnosticTicker : Node` 没有附着/逐帧回调。普通 .NET 模组工程没有 Godot 脚本源码生成绑定，该 Node 也未声明 `partial`；机制与 0 帧观测一致。`Console.WriteLine` 阶段标记没有进入 Godot 日志。修订只将 ticker 改为原进程 `SceneTree.ProcessFrame` 事件，将正常阶段标记改为 `GD.Print`，并把 `PRECHECK_PASS` 推迟到实际首帧证据成功写入后。20 字段、30 连续帧、生产方法参数/返回/Task、保存与 writer patch、证据 schema 和上限均未放宽。原因、代码范围、候选和[官方 Godot 机制说明](https://docs.godotengine.org/en/latest/tutorials/scripting/c_sharp/diagnostics/GD0001.html)见[修复记录](../../evidence/spike-inv012a/runtime/harness-repair-runtime-r1.md)。

离线修订验证：Release build 0 warning / 0 error；纯 .NET 合同 **38/38**（新增零帧、首帧证据失败、终态迟发的负向测试）；固定 patch/unpatch **9/9** 且不调用目标；dummy 非干扰保留原方法及 Task 成功/失败/取消/同步异常。输出分别见[build](../../evidence/spike-inv012a/runtime/build-output-runtime-r1.txt)、[合同](../../evidence/spike-inv012a/runtime/contract-test-output-runtime-r1.txt)、[patch](../../evidence/spike-inv012a/runtime/patch-smoke-output-runtime-r1.txt)、[非干扰](../../evidence/spike-inv012a/runtime/noninterference-smoke-output-runtime-r1.txt)。只使用一次已定位修复后的 fresh-process 重测；得到完整可解释结果后停止，没有消耗第二次重测额度。

## 数据边界、未知项与交接

用户在 modded profile1 测试槽新建对局、完成开场选择、进入普通 Monster 并经游戏 UI 正常退出，属于本包预声明的原生写入窗口。Implementation 只解析 account `settings.save` 的模组开关、modded selector 的 profile ID，读取固定游戏/探针 hash、过滤日志与探针自有记录；未读取、复制或比较生产 save/replay 内容，也未进入 unmodded profile1。正常测试写入可能涉及该测试槽的 current_run/progress/prefs、replay 及 account settings；本报告不判定它们的内容差异或归因。

本次没有重现或解释 A4 原进程活性首因；没有 A/B 或 checkpoint 可恢复性证据。`RunManager.WriteReplay`/`NRun._Notification` 在这次路径没有观测到，其他退出路径是否调用仍 Unknown。游戏已退出，live `runtime-r1` DLL/manifest 仍安装，持久配置仍仅启用 `SPIKEINV012A`；项目发起人下次正常游玩前应在受控配置会话中禁用探针。Implementation 不再自动开局或追加重测，现交 Project Lead 复核，并由其维护权威状态文件及后续工作包。

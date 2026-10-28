# INV-009：Replay setup/post-launch phase-aware teardown 契约

状态：`COMPLETE`

## Project Lead acceptance — 2026-09-15

- 最终分类 `ACCEPT_PRODUCTION_CLEANUP_WITH_REVISED_ORACLE` 已验收。
- Project Lead 独立复现 `Invoke-Inv009StaticQuery.ps1`：固定 DLL/R4 artifact identity、Release build 0 warning/0 error、错误 artifact `BLOCKED_IDENTITY`、253,870-byte IL输出 SHA-256 `488814C65B9CD8E56F888CFCFE50AD7BEAA890E0396F084BED3916CFBDF772D7` 与2,063-byte bounded summary SHA-256 `2B50415302BBE5D38874C6AA5CA29EAF2012FA7D4DE923AA842A8AD26762D9D0` 全部通过。
- 接受 R4 的13项core cleanup call-set为setup-fault局部PASS；不把它外推为menu restoration、restart或四阶段PASS。
- 下一任务批准全新 `SPIKE-INV008A-R5`，应用phase-aware lobby条件与post-launch scene identity gate。R4保持不可修改/重跑。

## 范围、身份与最终分类

固定 `sts2.dll` 与 R4 artifact 均通过工作包要求的 length/SHA-256 gate。可复现工具、完整 IL/metadata 输出和仅含必要白名单字段的 R4 摘要见 `evidence/inv-009/README.md`。

最终分类：`ACCEPT_PRODUCTION_CLEANUP_WITH_REVISED_ORACLE`

R4 缺少的 `RunLobby.remove_RemotePlayerDisconnected` 与 `RunLobby.Dispose` 在该 fresh-process replay setup-fault 边界均为 **Not applicable**。它们不是 replay setup 创建后漏掉的资源，而是 `CleanUp(true)` 在 `RunLobby != null` 时才承担的成对条件义务。R4 的固定 15-step oracle 把这两项错误地提升成了无条件要求。

本结论只修订 RunLobby 两项的适用条件，不降低 teardown failure 的独立分类，也不豁免其余 gate。

## Confirmed

### 1. Replay setup 不建立 RunLobby

- `RunManager` 静态构造器只 `new RunManager()`；实例构造器不初始化 RunLobby，因此 fresh process 中 `<RunLobby>k__BackingField` 为 CLR 默认 `null`。
- `SetUpReplay` 同步创建 `NetReplayGameService(netId)`，调用 `InitializeShared`，再调用 `InitializeRunLobby(netService, state, players)`，最后调用 `InitializeSavedRun` 并返回；没有 try/finally 或异步 continuation。
- `NetReplayGameService.Type` 的方法体固定返回整数 `4`；固定程序集枚举映射为 `Replay=4`。
- `NetGameTypeExtensions.IsMultiplayer` 只对 `Host=2` 和 `Client=3` 返回 true；`Replay=4` 返回 false。
- `InitializeRunLobby` 仅在 `IsMultiplayer(netService.Type)` 为 true 时构造 `RunLobby`、调用 RunManager setter，并订阅 `RunManager.RemotePlayerDisconnected`。false 分支直接越过三步。
- false 分支随后仍将当前 RunLobby getter 的值传给 `CombatStateSynchronizer` 和 `RestSiteSynchronizer`。在 R4 fresh-process 前提下该值为 null；两类 constructor 对 RunLobby 的订阅均受各自 `_runLobby != null` 门禁控制，因此不会建立 lobby 订阅。
- `NetReplayGameService.RegisterMessageHandler`、`UnregisterMessageHandler`、`Disconnect` 和两种 `SendMessage` 方法体均直接返回。因而上述 synchronizer 对 replay service 发出的 handler 注册调用不会让 service 持有 handler；这不等于一般 INetGameService 都如此。

### 2. RunLobby 唯一 storage 与写入点

- RunManager 只有一个 RunLobby backing field。getter 只 `ldfld`；private setter 只 `stfld`。
- 对固定程序集进行解码后的 IL token 扫描，唯一 backing-field 读写是 getter/setter 本身；唯一 setter caller 是 `InitializeRunLobby` 的 multiplayer true 分支。
- `InitializeRunLobby` 的 production callers 是 new/saved singleplayer、new/saved multiplayer、replay 和 test setup；这些入口共享同一条件分支。
- `InitializeSavedRun`、`Launch` 均不读取或写入 RunLobby。`Launch` 只设置 `LocalContext.NetId`、关闭 message buffering、同步调用 `RunStarted`、更新 rich presence 并返回 State。
- R4 probe 源码在 `SetUpReplay` 返回后只设置 `ShouldSave=false`、设置 `CombatStateSynchronizer.IsDisabled=true`、执行 identity/flag/writer/counter gate，然后 setup phase 立即抛出 owner fault。post-launch 路径在这之后只 preload assets、再次 gate、`Launch`、`NRun.Create`、`SetCurrentScene`、scene/state/writer gate，然后抛 fault。`rg` 对 probe 源码的 RunLobby 命中仅位于 fixed oracle/patch target 字符串，不存在 getter、setter、field、dispose 或替换调用。
- 全程序集没有第二个 RunLobby setter/direct-field writer。因此在固定代码、无反射/Harmony 外部写入的边界内，RunLobby 从 setup 返回到 Launch 后仍保持 setup 完成值。

### 3. `CleanUp(true)` 的精确条件

`CleanUp` 在 `State == null` 时立即返回。State 非空时，主体先执行 reset/UI cleanup/replay条件写入/回调退订/各 synchronizer disposer，然后：

```text
if (RunLobby != null) {
    RunLobby.remove_RemotePlayerDisconnected(RunManager.RemotePlayerDisconnected);
    RunLobby.Dispose();
}
NetService.Disconnect(None, !graceful);
```

`RunLobby.Dispose` 本身负责注销四类 lobby/game message handler、移除 net-service `Disconnected`，并在 Host 分支移除 client connect/disconnect callbacks。没有 RunLobby 对象时这些注册从未由 RunLobby constructor 建立，因此跳过不是遗留一个未清理的 RunLobby resource。

`CombatStateSynchronizer` 与 `RestSiteSynchronizer` 虽持有一个 nullable RunLobby field，但 null 分支不会订阅它们的 peer-disconnect callback；各自 Dispose 的对应退订也同样受非 null 条件控制。R4 已观察到 `RestSiteSynchronizer.Dispose`。固定 `CleanUp` 不调用 `CombatStateSynchronizer.Dispose`；在 replay service 上它的两次 message-handler 注册是 no-op，且 lobby 为 null，所以本任务没有静态证据表明 R4 因 RunLobby 两步跳过而遗留 service/lobby subscription。RunManager 仍会保留若干已完成/已 dispose 对象的字段引用直到下次 setup 覆写；这属于 production 对象复用方式，不可用 State-null 单独证明一般意义上的整图释放。

### 4. R4 reached set

R4 白名单摘要确认：phase 为 `SetupOwnerFault`；terminal 为 `FAIL_TEARDOWN_CLEANUP`；cleanup entry/return 各 1、exception 0；State null、ShouldSave false、IsCleaningUp false、Local NetId null、无 replay write。Launch、scene、map、room、frame、dispatch、action、victory、reward、progress 与 replay counters 全为 0。

固定 15 项中到达 13 项；缺失集精确为 RunLobby 的外层退订与 Dispose。结合 fresh-process default null、Replay type false 分支和边界间无 writer，这个 13-step set 与正确的 setup-fault production path 精确一致。artifact 本身没有记录 lobby identity；“当次字段为 null”是固定 runtime 前提与静态方法体的组合推断，不伪装成 artifact 直接观测。

## Object / ownership lifecycle

| 阶段 | 对象/owner | RunLobby 状态 | 建立的资源与引用 | 转移/释放义务 |
|---|---|---|---|---|
| fresh process | static `RunManager.Instance` | `null`（默认字段值） | 无 RunLobby | 进入 setup 前应显式断言 null，防止陈旧引用被误当作 replay lobby |
| `SetUpReplay` / `InitializeShared` | transaction owner / RunManager | 尚为 `null` | 建立 replay service、queues、writer、多个 synchronizer | 后续由 `CleanUp` 的无条件/各自条件 disposer 处理 |
| `InitializeRunLobby` Replay 分支 | RunManager | 保持 `null`；不赋值 | 不构造 RunLobby，不订阅 RunManager callback | 两个外层 RunLobby cleanup 调用不适用 |
| null-lobby synchronizer 构造 | CombatState/RestSite synchronizer | 各自保存 null | lobby callback 不注册；replay service handler registration 是 no-op；RestSite buffer handlers仍建立 | RestSite Dispose 仍必需；lobby-specific退订不适用 |
| `InitializeSavedRun` 返回 / setup fault | transaction owner | 保持 `null` | 无 Launch/NRun；setup 对象图完整 | 执行 core cleanup；RunLobby 两项 N/A |
| preload + `Launch` | transaction owner / synchronous RunStarted observers | 保持 `null` | Local NetId 与 Launch observers可能建立；无 RunLobby writer | core cleanup 同 setup；另审计 Launch side effects |
| `NRun.Create` + scene install / post-launch fault | transaction owner + scene container | 保持 `null` | 新 NRun 已成为 current scene；旧 menu queued-free | core cleanup后必须用 restored manager 重建 menu/替换 NRun |
| `CleanUp(true)` | RunManager | null 分支跳过两步 | 13 个原 oracle 中实际适用的 tracked call均执行；replay service Disconnect no-op | finally 仍清 flags/NetId/State/DailyTime |
| outer restore/menu | transaction owner | 不改变 RunLobby | restore original SaveManager；LoadMainMenu/Continue/protection | setup phase无 owned NRun；post-launch必须确认 NRun 不再 current scene |

## Phase-aware cleanup obligation

下表的 `Unconditional` 均以 owner 已成功完成 `SetUpReplay`、State 非空且进入 `CleanUp(true)` 为前提；它不把主体 exception 后未执行的步骤视作成功。

| 顺序/步骤 | setup owner fault | post-launch owner fault | 方法体证据/条件 |
|---|---|---|---|
| owner CAS、关闭输入/业务、writer-not-recording gate | Unconditional | Unconditional | probe owner contract；不能由 expected fault PASS 覆盖 |
| isolated task wait 1 | Unconditional | Unconditional | outer transaction obligation |
| `ShouldSave=false`; `IsCleaningUp=true`; history flag reset | Unconditional | Unconditional | `CleanUp`, State 非空后立即执行 |
| `ActionQueueSet.Reset` | Unconditional | Unconditional | `CleanUp` IL 001E–0024 |
| `CardSelectCmd.Reset` | Unconditional | Unconditional | IL 0029 |
| audio stop | Conditional(`NAudioManager.Instance != null`) | Conditional(`NAudioManager.Instance != null`) | null-conditional branch |
| overlay/capstone/map/modal cleanup | Conditional(各 singleton instance != null) | Conditional(各 singleton instance != null) | 四个独立 null-conditional branch；post-launch 也不能假设它们一定存在 |
| `CombatManager.Reset(true)` | Unconditional | Unconditional | IL 007E–0084，graceful=true |
| `WriteReplay(true)` | Not applicable | Not applicable | 两阶段 fault gate均要求 `IsRecordingReplay=false`; production branch only if true |
| ActionExecutor callback removal | Unconditional | Unconditional | IL 009D–00AF |
| `CombatReplayWriter.Dispose` | Unconditional | Unconditional | IL 00B4–00BA |
| ActionQueue/PlayerChoice/Reward/RewardsSet/RestSite/Flavor/Checksum disposers | Unconditional | Unconditional | IL 00BF–0107；R4 均已到达 |
| outer `RunLobby.remove_RemotePlayerDisconnected` | Not applicable | Not applicable | only if `RunLobby != null`; Replay fresh-process path保持 null |
| `RunLobby.Dispose` | Not applicable | Not applicable | 与上一项同一个非 null branch；不得拆开条件 |
| `NetService.Disconnect(None, false)` | Unconditional | Unconditional | IL 0136–0141；Replay implementation为 no-op 仍须到达 |
| production finally: hotkeys conditional clear；cleaning false；NetId null；State null；DailyTime null | Unconditional（finally） | Unconditional（finally） | `CleanUp` 唯一 finally；主体异常也会执行，但不补做被跳过的 disposer |
| isolated task wait 2 + SaveManager restore + original task check | Unconditional | Unconditional | outer transaction contract，step-local failure不得跳过 manager restore |
| replace current scene with restored main menu | Conditional(cleanup + manager restore成功)；无 owned NRun | Conditional(cleanup + manager restore成功)；必须替换已安装 NRun并确认它不再 current | `LoadMainMenu(false)` / `SetCurrentScene`; 这是两阶段唯一实质性 ownership 差异 |
| Continue/protection/boundary/terminal seal | Unconditional | Unconditional | 保留旧 gate；teardown failure独立优先 |

因此：两阶段的 production `CleanUp(true)` call-level 义务相同，RunLobby 两项在两阶段都不适用；完整 phase obligation **不同**，因为 post-launch 已拥有一个安装进 scene tree 的 NRun，而 setup phase 没有。不能把“复用同一 13-call set”误写成两个 phase 的全部 teardown 相同。

## Revised oracle / test delta（设计，不修改 probe）

1. 在 override/setup 前记录 `RunLobby` identity，并对 fresh-process replay gate 强制 `null`。若非 null，立即 `BLOCKED_PRECONDITION_STALE_RUNLOBBY`，不得把两个调用标 N/A。
2. setup 返回、Launch 前、Launch 返回、scene install 后、cleanup entry 各记录 RunLobby identity 与 `NetService.Type`。要求 Replay type 且所有点都仍为 null。
3. 将 tracked cleanup obligations 拆成：13 项 replay core required；RunLobby 两项为一组 `Conditional(preCleanupRunLobby != null)`。条件为 true 时两项都必须且各一次；条件为 false 时两项都必须为零。
4. 保留旧 gate：cleanup entry/normal return/exception、State/ShouldSave/IsCleaningUp/LocalNetId、no replay write、13 core calls、tasks、manager restore、boundary、Continue、12-path protection、immutable artifact，以及 teardown failure 对 expected owner fault 的优先级。
5. post-launch 另加 scene identity gate：fault 前 current scene 必须是刚建 NRun；成功恢复后 current scene 必须是新 main menu，且该 NRun 不再 current。setup phase将 NRun removal 标 Not applicable。

真实 production positive cases：

- Replay path：`SetUpReplay → NetReplayGameService.Type=Replay → IsMultiplayer=false`，pre/setup/pre-cleanup lobby 均 null；13 core calls各一次、两个 lobby calls为零，判 cleanup call-set PASS。这正是 R4 的路径。
- Multiplayer path（oracle 的条件分支正例）：`SetUpNewMultiplayer` 或 `SetUpSavedMultiplayer` 使用 Host/Client service，`IsMultiplayer=true`；必须观察新 RunLobby identity、RunManager callback subscription，并在 cleanup观察 outer退订与 Dispose各一次。该正例用于防止把 revised oracle 泛化成永久忽略 lobby disposer。

必须覆盖的 negative cases：

- pre-cleanup lobby 非 null而任一 outer退订/Dispose缺失、重复或抛出：`FAIL_TEARDOWN_CLEANUP`。
- pre-cleanup lobby null却出现任一 lobby cleanup call：失败，提示陈旧/替换 identity 或错误 instrumentation。
- Replay setup 前 lobby 非 null，或 setup/Launch/scene任一点 identity发生 null↔nonnull/替换：precondition/boundary failure，不能靠 cleanup结果洗掉。
- 13 core steps任一缺失、cleanup exception、仅 finally state-null、replay write、manager restore/menu/Continue/protection任一失败：继续保持原独立失败，不受条件 lobby规则豁免。
- post-launch current NRun 未被 menu scene替换：独立 scene teardown failure，即使 13 calls 与保护文件通过也不能 PASS。

## Inferred

- R4 artifact 未直接采样 RunLobby，但 fresh-process identity、RunManager constructor/default field、Replay type分支、程序集唯一 writer和 probe source无 writer共同支持：当次 cleanup entry 的 RunLobby 为 null。缺失的两个 patch event与 production null branch一致，是佐证而不是唯一依据。
- R4 原 terminal 应在采用上述 oracle后重解释为“setup owner fault 的 production cleanup call-set 局部 PASS”；由于原 oracle在 cleanup阶段提前阻止 menu load，R4 仍不能补证 menuReady/RefreshButtons 或 restart，不能把整个四阶段 matrix改写为 PASS。

## Unknown

- 最大 Unknown：`CleanUp` Dispose 非 null RunLobby 后不把 RunManager backing field清 null，而 non-multiplayer/replay `InitializeRunLobby` false分支也不覆盖它。静态上存在“同一进程先完成 multiplayer teardown、随后进入 replay而继承已 dispose lobby identity”的陈旧引用路径；其真实入口可达性、Dispose幂等性与后果未在本任务验证。它不适用于 R4 fresh-process 路径，但未来 oracle必须 fail closed，不能把非 null陈旧值当成合法 replay lobby。
- `RunStarted` runtime subscriber、native/deferred callbacks与反射/Harmony第三方 writer不能由固定程序集 IL穷尽。当前 probe 源码没有此类写入；结论只覆盖固定 mod set和列出的 observable boundaries。
- 固定 `CleanUp` 不调用 `CombatStateSynchronizer.Dispose` 的一般设计语义未被单独证明；对 R4 replay service，其 handler registration是 no-op且 lobby null，因此它不改变两个缺失 RunLobby调用的适用性判断。

## 唯一下一步建议

由 Project Lead 验收本报告，并签发一个新的、明确工作包来把上述 phase-aware oracle应用到后续 restart/post-launch 验证：不得重跑 R4，先把 R4 setup-fault结果登记为仅限 call-set 的局部通过；任何后续运行仍须重新授权并保留所有旧 gate，同时加入 stale-RunLobby 与 post-launch scene negative cases。

## 合规声明

本任务未启动或控制游戏，未使用 Computer Use，未读取当前 profile/save/cloud、日志 payload或任何 recovery payload；未修改、构建、安装或运行 `SPIKE-INV008A`，未删除/移动/覆盖其 artifact/recovery/evidence，未创建 R5，也未请求或继续任何人工 runtime phase。只新增了本报告与 `evidence/inv-009/` 中的静态查询证据。

# INV-008：fault/restart transaction lifecycle 静态契约

状态：`COMPLETE — READY_FAULT_RESTART_MATRIX`

## 范围与身份

本任务只做静态调查。没有启动或控制游戏，没有使用 Computer Use，没有读取任何当前 profile/save/cloud、R3 recovery payload 或其他 profile 数据，没有修改、构建、安装或运行任何旧 probe，也没有实现 fault/restart runtime probe。

固定目标程序集通过 hash gate：

- 游戏：public-beta `v0.111.0`，commit/branch `41cef1ea`；
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`；
- R3 已验收 artifact：19,953 bytes；SHA-256 `AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553`；schema/version `spike-inv007a.direct-write.v3` / `0.3.0-direct-write-r3`。本任务只读取 R3 报告与 terminal validation 摘要，没有读取 recovery payload。

静态查询工具和可复现输出见 `evidence/inv-008/README.md`。最终输出 `static-lifecycle-query-output.txt` 由上述固定 DLL 生成，包含目标方法及 async state machine IL。

## 结论摘要

静态证据足以签发一个有界、四个 fresh-process phase 的 owner-fault/restart matrix，但不授权强制崩溃：

1. setup fault 的最晚安全点是 `SetUpReplay` **完整返回后**，并且 candidate override、`RunManager.State` 绑定、`ShouldSave=false`、`CombatStateSynchronizer.IsDisabled=true` 与 `CombatReplayWriter.IsRecordingReplay=false` 均已断言；此时尚未调用 assets preload、`Launch`、`NRun.Create`、map/room、action 或 reward 路径。
2. post-launch fault 的最早充分点是 `Launch()` 返回，随后 `NRun.Create(runState)` 已由 `NSceneContainer.SetCurrentScene` 同步安装并通过 scene/state identity 断言；必须在 `GenerateMap`、synchronizer fast-forward、`LoadIntoLatestMapCoord` 和任何 process-frame/quiet-frame 等待之前注入，并再次断言 replay 尚未 recording。
3. 两点都注入 probe-owned、同步抛出的 `ControlledOwnerFaultException`，只由 transaction owner 捕获，并必然进入唯一 `finally`。这验证的是可控 owner exception，不是真实进程崩溃。
4. teardown failure 是独立 terminal class。`RunManager.CleanUp` 的 production `finally` 会清空 state，但如果其主体中途抛出，后续 disposer 会被跳过；state-null 因此不能单独证明完整 cleanup，也不能靠第二次 `CleanUp` 修复。
5. 下一进程 restart verification 只能证明“前一 owner fault 已完成 ordered teardown、正常退出后，新的进程仍有相同保护状态与 Continue”。它不能证明 override 活跃时被强杀/原生崩溃后的恢复。

## Confirmed：固定程序集方法体

### Setup 与 owner 边界

- `RunManager.SetUpReplay` 是同步方法。它先拒绝已有 state，再立即设置 `State=runState`，构造 `NetReplayGameService`，调用 `InitializeShared(... shouldSave:true ...)`，初始化 run lobby 和 saved run。它自身没有 try/finally，也不返回 task。
- `InitializeShared` 同步创建 action queue/executor、checksum tracker、replay writer 以及多组 synchronizer，注册 action/checksum callbacks，并把 `ActionExecutor` 置于 paused；最后按参数设置 `ShouldSave`。因此不能在 `SetUpReplay` 内部任意位置注入后假定对象图完整。
- 既有 007A owner 在安装 candidate override 后调用 `SetUpReplay`，随后设置 `ShouldSave=false`、禁用 combat-state sync，并在同一 async owner 的 `try/finally` 中继续。R3 已动态证明该 owner 的正常路径最终可恢复 manager、state、Continue 和 12 项 protection；R3 只覆盖 direct methods，不覆盖异常路径。

### Launch、scene 与 replay recording

- `RunManager.Launch` 是同步方法：设置 `LocalContext.NetId`、关闭 net-service message buffering、同步调用 `RunStarted` subscribers、更新 rich presence，并返回 state。
- `NRun.Create` 同步实例化 run scene、绑定传入的 `RunState` 并清空 hotkeys。
- `NSceneContainer.SetCurrentScene` 同步移除现有 children、对旧 nodes 调用 `QueueFreeSafely`，设置新 `CurrentScene`，再 add/reparent 新 scene。旧节点的真正 free 和任意 Godot deferred work 不由返回的 `Task` 表示。
- `CombatReplayWriter.IsRecordingReplay` 等价于其内部 `_replay != null`。`RecordInitialState` 才创建该对象。固定程序集中的直接 callers 是 `RunManager.EnterMapPointInternal` 与 `EnterRoomDebug` 的 async state machines。
- `RunManager.CleanUp` 若发现 recording replay，会调用 `RunManager.WriteReplay(true)`；该路径通过 `SaveManager.GetProfileScopedPath("replays/latest.mcr")` 后由 `CombatReplayWriter` 直接创建目录和写文件，不是普通 `ISaveStore.WriteFile`。所以 fault phase 必须在进入 map point/room 前停住，并以 `IsRecordingReplay=false` 作为注入前硬门禁和 cleanup 前硬审计字段。

### Production teardown 与主菜单重建

- `RunManager.CleanUp(graceful)` 在 state-null 时立即返回；否则先设置 `ShouldSave=false`、`IsCleaningUp=true`，再 reset action/card-select/audio/overlay/capstone/map/modal/combat，按条件写 replay，移除 action callback，dispose replay writer 和一组 synchronizers/lobby，并 disconnect net service。
- 它的 production `finally` 始终 clear hotkeys、设置 `IsCleaningUp=false`、清空 `LocalContext.NetId`、设置 `State=null` 并清空 daily time。
- 该 finally **不**逐项保护主体中的 disposer。主体任一步异常都会跳到 finally，后续主体步骤不再执行；finally 又使第二次 `CleanUp` 因 state-null 直接返回。因此必须记录每个关键 disposer/disconnect 是否已到达，不能用 `State==null` 代替完整 teardown。
- production `NGame.ReturnToMainMenu()` 的顺序是 fade out → preload common/main-menu assets → `RunManager.CleanUp(true)` → await `LoadMainMenu(false)`。全局 SaveManager override 的实验不能原样调用该 wrapper，因为主菜单会在 override clear 前被创建。
- `NGame.LoadMainMenu(false)` 先读取**调用当时** `SaveManager.Instance.CurrentRunSaveTask` 并等待；task fault 会被记录但不会阻止它继续。随后它创建 `NMainMenu`、clear hotkeys 并通过 `SetCurrentScene` 安装菜单。
- `NMainMenu.RefreshButtons` 读取**调用当时**的 `SaveManager.Instance.HasRunSave/LoadRunSave`，据此填充 Continue。因此原 manager identity 必须在 `LoadMainMenu` 之前恢复。
- 003A Phase B/C 与 007A R3 已动态通过“直接 cleanup → clear override/restore original → await original task → `LoadMainMenu(false)` → Continue”的正常 teardown 路径。

## Lifecycle / ownership 表

| 阶段 | 唯一 owner | 必须成立 | 仍可能存活的工作 | 转移规则 |
|---|---|---|---|---|
| timing gate | startup observer；transaction 尚未拥有 | graphics 正常退出且 idle；outer cloud task terminal；startup marker terminal；主菜单 ready；original manager identity 稳定；state null；original current-run task null/terminal | 已 terminal 的 outer/startup/current-run tasks；observer continuations已执行 | Ready 原子提交时解除 startup timeout；人工等待无 timeout |
| candidate 构造 | transaction owner | protection/recovery/prior-integrity 先通过；live-seeded candidate content/reference/store containment 通过；original 未变 | candidate 初始化必须是 memory-only；不得产生未登记 task | 任一失败在 override 前 BLOCKED/FAIL，不调用 setup |
| override 安装 | transaction owner | `SaveManager.Instance===candidate`；original identity 已保存；audit active | original manager 仍存在但不得被调用；candidate memory task应 null/terminal | 只有 owner 可继续；拒绝第二 transaction/profile change/run start/Quit/input |
| replay setup | transaction owner，同步调用 | `SetUpReplay`完整返回；state identity正确；`ShouldSave=false`；combat sync disabled；writer not recording | action/executor、writer、lobby/net service、多个 synchronizer 和 event subscriptions；无 setup task handle | setup-fault 在此立即同步 throw |
| launch | transaction owner，同步调用 | `Launch`返回；state仍绑定；candidate仍 active；writer not recording | `RunStarted` subscriber 可能同步启动未知 task/deferred callback，固定程序集没有统一枚举/取消句柄 | 不等待 frame；立即建并安装 NRun |
| scene installed | transaction owner | `CurrentScene`是刚创建 NRun 且绑定同一 runState；state/candidate仍正确；writer not recording | 旧 menu queued-free；NRun Godot lifecycle/deferred work；Launch observer work。均无统一 task/cancellation surface | post-launch fault 在此立即同步 throw；禁止 GenerateMap/LoadIntoLatestMapCoord |
| controlled fault | transaction owner | 唯一 fault token 尚未使用 | 只能是 probe-owned synchronous exception | 专用 catch 验证 type/token/site 后进入同一 finally；异常不得越过 owner |
| teardown | CAS 取得的唯一 teardown owner | audit 保持 active；新输入/callback启动被拒绝 | candidate current-run task；上述 production对象图；scene deferred work | 每步独立 try/catch/finally，失败不得跳过 manager restore/artifact |
| restart verify | 下一 fresh process 的只读 restart owner | 上一 fault artifact PASS；用户已正常退出；固定 identity/profile1/Cloud可观察状态匹配 | 不继承前进程托管对象；只验证持久状态 | 不安装 override、不建 run；封存只读 artifact后正常退出 |

## Fault 点的 pending work 契约

### Setup fault

可等待/观测：

- candidate `CurrentRunSaveTask`：注入前应为 null/terminal；teardown 前后各快照 exact task identity/status，若 pending 可有界 await，fault/cancel/timeout 均为 teardown failure但继续恢复；
- original `CurrentRunSaveTask`：timing gate 已证明 null/terminal；transaction 内若出现新 task 或 identity/status 变化即 boundary failure；restore 后在建 menu 前观测/等待 terminal；
- `CleanUp` 的一次调用、关键 reset/dispose/disconnect、state/cleaning/save flags；
- manager override clear/restore、menu ready/RefreshButtons、protection 与 artifact seal。

静态上不存在 `SetUpReplay` 自己返回的 production task。setup 已创建同步对象和 callbacks，但还没有 launch、scene、map、room、action、reward 或 replay recording。

### Post-launch fault

除 setup 项外，`Launch` 已同步调用 `RunStarted`，`SetCurrentScene` 已 queue-free 旧 menu 并把 NRun 加入树。以下没有统一可等待/取消句柄：

- 任意 `RunStarted` subscriber 自行创建的 task；
- Godot `_EnterTree/_Ready` 或 call-deferred/queued-free continuation；
- native/platform callback；
- 没有实现 `IDisposable` 或未被 `CleanUp` 显式 dispose 的 synchronizer/observer。

首轮通过“不进入下一 frame、不调用 GenerateMap/LoadIntoLatestMapCoord、输入全禁、立即 owner fault 和 cleanup”把窗口压到最小；以 scene-change、production callback、store/external boundary、cleanup disposer 事件和最终 protection/restart 观察它们。无法声称逐个取消或穷尽这些 callback。

## Ordered teardown 最小安全契约

下列顺序是不可交换的；每步均有独立 stage、开始/完成事件、异常字段和后置条件。后一步失败不能抹去前一步结果。

1. **取得唯一 teardown ownership**：single-CAS 从 operation owner 转到 teardown owner；禁止所有 probe callbacks、输入、run start、profile change 和 Quit；保留 boundary observers。后置：finally count=1、teardown-owner count=1。
2. **封闭业务动作**：确认未 dispatch event、未进入 map/room、未出牌/结束回合/胜利/奖励；确认 writer `IsRecordingReplay=false`。若为 true，分类 `FAIL_UNEXPECTED_REPLAY_RECORDING`；不得把可能的 replay direct write当作允许副作用。
3. **isolated task wait 1**：快照 candidate `CurrentRunSaveTask` identity/status；null/terminal 为成功。pending 只可有界等待，超时/取消/fault记 `FAIL_TEARDOWN_ISOLATED_TASK`，但通过 step-local catch 继续第 4 步。
4. **production cleanup exactly once**：仅当 state 非空时调用一次 `CleanUp(true)`；patch 记录 entry、return/finalizer exception、关键 reset/dispose/disconnect。后置至少为 `State==null`、`ShouldSave==false`、`IsCleaningUp==false`、`LocalContext.NetId==null`，并且无 replay write。任一主体 exception、缺失必需 disposer或 count≠1 都是 `FAIL_TEARDOWN_CLEANUP`；禁止以第二次调用修复。
5. **isolated task wait 2**：再次快照同一 candidate task surface；必须 null/terminal且没有未观察异常。无论成功失败都进入 manager restore。
6. **non-skippable override restore**：在最外层 restore-finally 中先 `ClearInstanceForTesting`，断言其恢复 original；若不匹配，只允许用已捕获的 exact original 调用一次 `MockInstanceForTesting(original)` 作为恢复动作并记录。后置必须 `SaveManager.Instance===original`。失败为 `FAIL_TEARDOWN_RESTORE_MANAGER`，禁止建主菜单，但仍尝试 protection/artifact并要求用户正常退出。
7. **original task check**：确认 original task 与 timing-gate snapshot 一致且 null/terminal；新 task 或 faulted task是 boundary/teardown failure。pending 可有界等待后再决定是否允许菜单。
8. **主菜单重建（仅 cleanup 和 restore 成功）**：调用并 await `LoadMainMenu(false)`；等待 explicit `NMainMenu._Ready`/`RefreshButtons` 事件而不是固定 quiet frames。后置：main menu scene、state null、original manager identity、Continue visible/enabled且摘要/canonical/NumReloads等于 phase baseline。
9. **保护复核**：逐项比较 12 项 allowlist 的 content hash/length/mtime 和目录 inventory；逐项记录，不用“所有副作用均覆盖”替代。任何变化即 FAIL，不恢复 backup、不 rebaseline。
10. **terminal seal**：seal observers，single-CAS 决定首个 terminal，winner ticket 以 create-new temp+atomic move 写入 phase-specific immutable artifact。只有 owner fault、完整 teardown、boundary/protection/Continue 全通过才可写 phase PASS。artifact write failure独立 `BLOCKED_ARTIFACT_WRITE`，不得覆盖或重跑。

如果 `CleanUp`、manager restore或 menu rebuild 失败，terminal 必须是 teardown failure，而不是“预期 fault 已捕获”的 PASS。若 cleanup/restore 失败，用户只能按工作包指示正常退出；不得杀进程、恢复 backup或继续下一 phase。

## Observer / side-channel 边界

### 可沿用 007A/003A

- 007A 的 fixed-identity、outer-cloud/startup/menu timing gate、Ready 后 disarm timeout、single-CAS terminal、winner-ticket artifact、attempt-specific create-new 文件与 prior-attempt integrity；
- 007A 的 profile1 12 项 protection/recovery containment 和 live-seeded independent candidate；
- identity-first `ISaveStore` mutator observer：先判断 original/candidate/unknown store，再读取参数；cloud wrapper/local/remote/mock store 的 write/delete/rename/batch/sync 类边界；
- 003A 较宽的 SaveManager operation 列表：run/progress/history/prefs/settings/profile save、current-run delete、directory delete、batch 与 profile switch；
- metric/mod-metric、daily/leaderboard submit/upload、achievement/unlock 边界；
- Quit、profile change、Continue/Singleplayer/Multiplayer 与普通 input guards；
- manager identity、state、ShouldSave、scene、CurrentRunSaveTask、Continue 与保护集合字段。

### 异常窗口必须新增

- exact controlled-fault token/site、owner catch 和 finally-entry counters；
- `Launch` entry/return/finalizer 与 `RunStarted` invocation boundary；
- `NSceneContainer.SetCurrentScene` old/new identity，NRun binding，NMainMenu ready/RefreshButtons；
- `CombatReplayWriter.RecordInitialState/WriteReplay/StopRecording` 和 `IsRecordingReplay` 前后值；replay 文件写是 direct filesystem side channel，不能只靠 ISaveStore observer；
- `CleanUp` entry/normal-return/finalizer exception，以及 action callback removal、replay writer dispose、列出的 synchronizer/lobby dispose和 net-service disconnect到达情况；
- candidate/original task runtime identity和每个采样点状态；
- post-launch 后任何 production save/store/external boundary都立即 fail closed，但 observer在 teardown完成前不能提前 seal/退出 owner finally。

### 仍不能可靠穷尽

- `RunStarted` 的运行时 subscriber 列表及其自行创建的 task；
- Godot queued-free/call-deferred/native signal 的全部队列与“队列已空”事实；
- Steam/native SDK 内部已接受但尚未对 managed observer 可见的操作；
- 非 `ISaveStore`、非已枚举 direct writer 的未来/未知直接文件 I/O。

因此验收语言只能是“列出的 observable boundaries 未命中且 protection/restart 未变化”，不能写成“所有外部副作用都被覆盖”。

## 强制进程崩溃为何不进入首轮

三种故障必须分开：

| 类别 | 是否执行 finally | 本轮用途 | terminal 含义 |
|---|---|---|---|
| probe-owned owner exception | 是；由同一 owner专用 catch捕获 | setup/post-launch fault phase | 验证受控异常进入 ordered teardown |
| teardown failure | 已进入 finally，但某 teardown step失败 | fault phase 的独立负面结果 | `FAIL_TEARDOWN_*`；不得算 owner-fault PASS |
| 强杀/原生崩溃/断电 | 否；进程可能停在任意指令 | 首轮明确不执行 | 不能从 owner-fault 或 restart PASS 推导 |

强制崩溃默认延期，理由是：它会跳过 manager restore、protection-after 和 terminal artifact seal；并可能把 direct replay/file/cloud/native I/O 留在“已提交但未观察”的状态。当前工作包又不授权任务执行者杀进程、自动恢复或 rebaseline。先用两个最小 owner-fault 点证明异常 finally 和下一进程只读 integrity，才能另行决定是否值得设计带 crash-before ledger、外部进程见证和只读 recovery decision 的独立 crash protocol。

## 下一 fault/restart runtime contract 草案

四个 phase 放在**一个新工作包**中，但每行都是独立 fresh process，按表顺序单步解锁；任一首个 FAIL/BLOCKED 后停止整个工作包。fault phase 结束后用户正常退出；紧邻的 restart phase 必须在下一进程、进入任何新 run 前执行。

| Phase | fresh-process 前置 | 注入点/异常 | 注入前固定布尔 | 禁止业务动作/需观测 pending work | teardown、验收与 artifact | 下一进程 |
|---|---|---|---|---|---|---|
| `SETUP_OWNER_FAULT` | 固定 identity；modded profile1；Cloud状态仅按用户/可观察证据确认关闭；R3 protection-after、当前 allowlist/inventory、Continue摘要、原 manager/task一致；独立 create-new recovery | `SetUpReplay`返回且 flags断言后，同步 throw 一次带 phase token 的 `ControlledOwnerFaultException` | candidate override；state绑定；ShouldSave false；combat sync disabled；writer not recording；Launch/scene/map/room/action/reward counters全0 | 不 preload、不 launch、不建 scene、不 dispatch、不输入、不胜利/奖励；观测 candidate/original task与 setup对象图 | 按 10 步 teardown；owner exception恰好一次且未越界；cleanup/restore/menu/protection/boundary全过；`fault-setup-<attempt>.json` + `recovery/setup-<attempt>/`，均不可覆盖 | PASS 后用户正常退出并执行 `RESTART_AFTER_SETUP` |
| `RESTART_AFTER_SETUP` | 必须是上一 phase 后首个新进程；固定 identity；上一 artifact PASS且 identity精确 | 不注入 fault；不安装 override、不建 transaction/run | state null；production manager；current task null/terminal；Continue/profile1/Cloud可观察条件匹配 | 不点击 Continue、不进 run、不操作 UI；只读比较上一 fault before/after/recovery/current | protection/inventory/Continue canonical/NumReloads一致；无 probe recovery restore；封存 `restart-after-setup-<attempt>.json` 后用户正常退出 | PASS 才解锁 post-launch fault |
| `POSTLAUNCH_OWNER_FAULT` | 新 fresh process；setup restart PASS；重新建立不可覆盖 baseline/recovery；其余同 setup phase | `Launch`返回且新 NRun 已被 `SetCurrentScene`同步安装后，在任何 map/room/frame等待前同步 throw 专用 token exception | setup全部布尔；Launch returned；state仍绑定；CurrentScene/new NRun identity；writer not recording；GenerateMap/fast-forward/LoadIntoLatestMapCoord/process-frame/action/reward counters全0 | 不等待完整渲染/quiet frames，不生成 map、不载入 room、不 dispatch、不输入、不胜利/奖励；观测 Launch observer、scene deferred边界和两类 tasks | 同一 10 步 teardown；额外要求 scene→cleanup→manager restore→menu 的事件顺序和 writer从未 recording/write；`fault-postlaunch-<attempt>.json` + `recovery/postlaunch-<attempt>/` | PASS 后用户正常退出并执行 `RESTART_AFTER_POSTLAUNCH` |
| `RESTART_AFTER_POSTLAUNCH` | 必须是上一 phase 后首个新进程；固定 identity；上一 artifact PASS且 identity精确 | 不注入 fault；不安装 override、不建 transaction/run | 同前一 restart phase | 同前一 restart phase | 逐项 protection/inventory、Continue canonical/NumReloads、original manager/store形态一致；封存 `restart-after-postlaunch-<attempt>.json` | PASS 后工作包完成；不自动进入 crash测试 |

每个 phase 的 terminal 至少记录：schema/probe/game/artifact/recovery identity、prior-integrity、注入前布尔、fault token/type/site/count、owner catch/finally count、operation exception、teardown stage/counters/exceptions、candidate/original task identity/status、cleanup/disposer/disconnect、override clear/restore、manager/run/scene/menu/Continue、writer recording/direct-write、逐项 boundary、12 项 protection及目录 inventory。

### PASS / FAIL / BLOCKED / 停止

- Fault PASS：只允许预期的 exact owner exception；它由 owner捕获并进入唯一 finally；所有禁止业务动作计数为0；ordered teardown、boundary、Continue和protection全部通过。
- Restart PASS：只读 identity、profile1 protection/inventory、Continue canonical/NumReloads和production manager基线均与紧邻 fault phase一致；不建立 transaction或run。
- FAIL：任意 owner exception越界；teardown step异常/缺失/乱序；writer recording或direct replay write；original/unknown manager/store；外部 submit；保护/Continue变化；未知用户业务动作；scene/run/synchronizer/task残留的已观察证据。
- BLOCKED：固定 identity、prior-integrity、artifact/recovery create-new、active profile1/Cloud可观察条件、baseline/candidate/audit覆盖或安全注入布尔不能成立。Blocked 不允许降级 gate。
- 每个 phase 的首个 terminal 即停止；不得覆盖 artifact/recovery、重跑、修复后继续后续 phase、恢复 backup或 rebaseline。Ready 后不得因人工响应慢而 timeout。

## Inferred

- 在两处建议注入点，所有 setup/launch/scene API 都已同步返回，因此 probe-owned同步 exception 会被围绕它们的同一 async owner捕获并进入 finally；这是由控制流结构支持的强推断，仍需 runtime artifact证明实际注入/catch/finally counters。
- post-launch 点在 `GenerateMap`/`LoadIntoLatestMapCoord` 前，且 writer-not-recording gate成立时，production cleanup不应进入 replay direct-write分支。静态 callers支持该边界，但运行时必须用 writer observer和 protection核实。
- 新进程不会保留 `_mockInstance` 等托管静态引用；restart phase仍只能把这一点作为进程模型常识与只读结果结合判断，不能替代对持久副作用的检查。

## Unknown / 保留风险

- 两个 owner fault phase 尚未运行，专用 exception 是否会按预期通过 Godot/TaskHelper 边界而不触发额外全局 error flow未知。
- `RunStarted` subscriber、Godot deferred/queued-free和未显式 dispose对象的完整集合无法从当前固定方法体穷尽。
- `CleanUp` 每个 disposer在这两个具体半初始化状态是否都会成功未知；这正是 matrix 的主要待验证事实，不能预先写成已确认。
- Steam Cloud内部状态与 native platform side effects不能由静态 IL完全证明；只能依赖明确可观察 gate、边界 hooks、protection和下一进程复核。
- 强制进程终止时的 crash consistency、恢复与跨进程外部见证完全未知，本轮不测试。

## 变更与验证

- 新增 `project-management/investigation-reports/INV-008-report.md`。
- 新增 `evidence/inv-008/README.md`、`StaticLifecycleQuery/` 和可复现 IL 输出。
- 查询工具 Release build 为 0 warnings / 0 errors；在 .NET 10 runtime 上读取固定 net9程序集并正常退出。没有修改、构建或运行任何旧 probe。
- 未修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md`、`ROADMAP.md` 或任何既有已验收报告。

## 唯一最终建议

`READY_FAULT_RESTART_MATRIX`

静态证据已经给出两个不进入 action/reward 的精确 fault 点、不可跳步的 teardown recovery顺序、四个 fresh-process phase及其 immutable artifact/recovery链。首轮只验证受控 owner exception与正常退出后的 restart integrity；强制进程崩溃明确延期且不能从本轮外推。

## Project Lead 验收补充 — 2026-09-15

- 独立重建查询工具为 0 warnings / 0 errors；独立临时输出仍为 132,086 bytes / SHA-256 `453C31CC5C22E0FD0C54F3D0CFC191FBDB967B2DBFF508A06A050A2A635354B8`，与封存输出逐行一致。错误程序集触发 `BLOCKED_IDENTITY`。
- 接受 `READY_FAULT_RESTART_MATRIX` 及四阶段顺序；不接受任何 crash-recovery外推。
- restart阶段的“保护一致”按 `TEST_PROTOCOL.md` 跨进程规则解释：fault transaction窗口内hash/length/mtime必须全等；用户正常退出后的下一进程允许且仅允许`settings.save`、`saves/prefs.save.backup`、`saves/progress.save.backup`在内容/长度不变时发生mtime-only变化。该变化必须显式记录为已知正常退出写入，不能表述为跨进程零写入。

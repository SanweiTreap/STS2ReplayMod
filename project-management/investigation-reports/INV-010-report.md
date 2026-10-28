# INV-010 Investigation Report

日期：2026-09-16  
状态：Complete — `READY_NEW_RECONSTRUCTION_CONTROL`  
范围：纯静态调查；未启动游戏，未读取当前 runtime/profile/save/cloud/settings/diagnostic log，未修改、构建、安装、运行或删除 `SPIKE001A2R2`

## 结论

固定方法体足以签发一个**全新 identity 的 reconstruction control**。R3 的动态首因仍为 Unknown，但不再需要先做一个只能诊断、不能捕获 A/B 的独立运行：

1. `ProbeTicker` 直接作为持久 `NGame` child，且 production 的 run/main-menu scene replacement 发生在 `NSceneContainer.CurrentScene` 下；按 Godot 生命周期，它静态上应跨 map/run/combat scene replacement 继续 `_Process`。
2. R3 没有设置显式 `ProcessMode=Always`，没有 heartbeat，也没有记录有效 parent/tree/pause/process 状态；所以现有证据仍不能证明它在战斗中实际继续 tick。
3. R3 的 readiness catch-all 把异常静默折叠为 `null`，失败帧不落盘；此外 `ExecutorIsPaused`、`CurrentlyRunningAction == null`、`CurrentRunNodePresent` 虽被记录，却没有进入 `CandidateReady`。
4. 固定 DLL 明确显示 `ActionExecutor.IsRunning` 读取 queue task completion：只有 queue task source 存在且 task 未完成时才为 true。它是 executor queue processor 的有效 quiet 信号，但不是“未来一瞬间绝不会入队”的证明，必须与 current action、pause、稳定目标身份及连续帧共同使用。
5. 最关键的时点已由方法体闭合：正常 map node entry 在创建 room 前 `await SaveManager.SaveRun(null, true)`；只有该 await 返回后才 `CreateRoom`/push/enter。新的只读 observer 可以包装这个返回 Task，在原 task 完成后立即启动 deadline，并在把完成交还 `EnterMapPointInternal` 前 stable-read、验证 `PreFinishedRoom == null`、原子封存 checkpoint 与 protection baseline。这样 baseline 早于 room creation/A，又不再误用 map 上旧的 non-null save。

唯一建议是新任务 `SPIKE-001A3`，执行本文 D 节完整契约。不得重跑或修改 001A2-R2，也不得据此直接解锁 `SPIKE-001B`。

## 输入 identity 与证据

| 固定输入 | 实际 | 结果 |
|---|---|---|
| STS2 public-beta | `v0.111.0`, commit `41cef1ea` | MATCH |
| `sts2.dll` | 9,757,184 bytes；`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` | MATCH |
| R3 完整日志 | 186,853 bytes；`175C68C3E068CC781C120CCF0D5B2870E2F2530856B07DB27500E673B77AB5D0` | MATCH |
| R3 arm | 6,841 bytes；`6308DA190E1663A023C2D9BC38C386308C3AE34F24B69C86FDC1A67759A7E197` | MATCH |
| 旧 probe DLL | 107,520 bytes；`167948B07F1342CC947B8A2D3C424234723A637ED05FEADD992222BDDD6C973C` | MATCH |

没有触发 `BLOCKED_IDENTITY`。

新增静态证据位于 `evidence/inv-010/`：

- `il-query-output.txt`：106,857 bytes；SHA-256 `C2EE5545BC51DF2A63CB815426BAC417955046877F3B41523AB76C444037F7A4`；1,377 行 hash-gated IL/caller 输出。
- `source-log-audit.md`：R3 source/sealed-log 对照及静态可排除边界。
- `lifecycle-readiness-matrix.md`：生命周期、readiness 与 checkpoint 顺序表。
- `reconstruction-contract.json`：全新运行契约的机器可读最小集合。
- `synthetic-contract-tests.txt`：20/20 PASS。

## A. Capture driver 生命周期

### Confirmed

R3 的唯一驱动链是：

```text
NMainMenu._Ready postfix
  -> EnsureTicker
  -> NGame.Instance.AddChild(ProbeTicker)
  -> ProbeTicker.SetProcess(true)
  -> ProbeTicker._Process
  -> ProbeController.Tick
  -> SnapshotCapture.TryReadiness
```

R3 source 没有对 ticker 调用 `SetProcess(false)`、`QueueFree/Free`、`RemoveChild` 或 `Reparent`。固定 DLL 的 `NGame.CurrentRunNode` 是 `RootSceneContainer.CurrentScene as NRun`；production 替换的是 scene container 的 current scene，不是 `NGame` 本身。日志又确认同一进程在 room-before write 后创建了 ActiveCombat，最后才 `NGame.Quit`。因此，单纯 map→combat scene replacement 静态上不会移除一个直接挂在 `NGame` 下的 child。

Godot 节点要持续收到 `_Process`，同时需要：节点有效、inside tree、process enabled，且 effective process mode 在当前 tree pause 状态下允许处理。以下情况会停止它：

- `SetProcess(false)`；
- effective `ProcessMode` 为 Disabled；
- mode 为 Inherit/Pausable 且 SceneTree paused；
- ticker 或其 parent 被 free/queue-free；
- remove/reparent 导致离开 tree；
- 整个 `NGame`/SceneTree 退出。

### R3 可排除 / 不可排除

| 条件 | R3 能否排除 | 依据 |
|---|---|---|
| probe 自己显式 `SetProcess(false)` | 可排除 | source 无调用 |
| probe 自己 free/remove/reparent ticker | 可排除 | source 无调用 |
| run scene replacement 直接替换 ticker parent | 可排除 | ticker parent 是 `NGame`，被替换的是 `NSceneContainer.CurrentScene` |
| NGame 在目标房间前已退出 | 可排除 | ActiveCombat 后仍记录 UI，最后才 `NGame.Quit` |
| combat 中 ticker 仍 valid/inside tree | 不可排除 | 无 heartbeat/parent/path telemetry |
| effective process mode | 不可排除 | R3 未显式设 `Always`，也未记录 inherited result |
| runtime tree pause | 不可排除 | 固定 managed DLL 未找到 `SceneTree.set_Paused` caller，但不能覆盖 native/GDScript/外部路径；R3 无 runtime 值 |
| `_Process` 调用后在 `TryReadiness` 抛异常 | 不可排除 | catch-all 静默返回 null |

### 与 SPIKE-001A 的差异

SPIKE-001A 的成功 readiness wait 是由 F9 启动的 `RestoreFixtureAsync` 所拥有，交给 `TaskHelper.RunSafely`；循环每 50 ms `Task.Delay` 一次，15 秒超时，并在超时时序列化最后一次 readiness。其 lifetime 属于一个显式 async Task，不依附 `NRun` 或某个 `_Process` callback。

R3 则没有 owner Task 或独立 watchdog：它只保留一个 Node 引用，进度完全依赖 `_Process`。如果 node 不再 process，quiet count、deadline 和 terminal artifact 都不会前进。这解释了为何旧设计会永久显示 Ready，但不是对 R3 实际 ticker 停止的动态证明。

## B. Readiness predicate 与异常面

### 固定 DLL 逐项语义

| 项 | 第一回合可操作点的预期语义 | 分类 |
|---|---|---|
| `RunManager.DebugOnlyGetState` | 直接返回当前 `RunManager.State`；应为目标 run | Confirmed method body |
| `Players.SingleOrDefault` | 单人应恰好一项；多于一项会抛 `InvalidOperationException` | Confirmed source/.NET semantics |
| `Player.PlayerCombatState` | combat-local 玩家状态，应非 null | Confirmed backing getter；时点为 inferred |
| `PlayerCombatState.Hand` | 当前 combat hand；opening draw 后可读取 | Confirmed backing getter；时点由 combat flow 支持 |
| `CombatManager.Instance` | 当前 combat singleton，应非 null | Confirmed getter exists |
| `IsInProgress` | `_turnState != null && _turnState.IsInProgress` | Confirmed method body |
| `IsOverOrEnding` | `IsEnding || !IsInProgress`，首回合应 false | Confirmed method body |
| combat/run `IsPaused` | 两个独立 pause backing flag，均应 false | Confirmed method body |
| `PlayerActionsDisabled` | combat-level input suppression，首回合输入开放时应 false | Confirmed body/callers |
| `IsPartOfPlayerTurn(player)` | CurrentSide 为 Player；若是 extra-turn list，则还需包含该 player | Confirmed method body |
| `PlayerTurnPhase.Play` | pre-play 已结束，进入玩家 play phase | Confirmed phase flow/callers |
| `ActionExecutor.IsRunning` | queue task source 的 task 尚未完成时为 true | Confirmed method body |
| `ActionExecutor.IsPaused` | executor pause backing flag | Confirmed method body |
| `CurrentlyRunningAction` | 当前 action backing reference；`AfterActionFinished` 清 null | Confirmed method body |
| `NGame.CurrentRunNode` | current scene 可 cast 为 `NRun` | Confirmed method body |

`ActionExecutor.ActionQueueChanged` 只在 `IsRunning == false` 时启动 `ExecuteActions`；`ExecuteActions` 建立 queue TCS，反复 `GetReadyAction`，直到没有 ready action后完成 TCS。因此 `IsRunning == false` **确实表示该 executor 当前 queue execution task 已完成/未活动**。它不能单独排除另一个 producer 在下一瞬间 enqueue，也不涵盖 executor paused/current action 残留，所以新 contract 同时要求：`!IsRunning && !IsPaused && CurrentlyRunningAction == null`，并连续观察 30 帧。

### R3 predicate 缺口

R3 `CandidateReady` 使用：combat in progress、not over/ending、两个 pause false、actions enabled、player turn、phase Play、`!executor.IsRunning`。它**没有使用**已经采集的：

- `ExecutorIsPaused`；
- `CurrentAction == null`；
- `CurrentRunNodePresent`；
- `HandCount`。

这不会直接解释“永远 false”（前三项缺失反而使 predicate 更宽），但说明旧 quiet contract 并不完整。

### 可能抛异常的具体 site

新 telemetry 必须给每个 site 独立编号，至少包括：

1. `RunManager.Instance` / `DebugOnlyGetState`；
2. `state.Players` 读取与 `SingleOrDefault`（多 player 明确会抛）；
3. `PlayerCombatState`、`Hand`、`hand.Cards.Count`；
4. `CombatManager.Instance` 与每个 combat getter；
5. `IsPartOfPlayerTurn` 内对 turn state/state/current side、`ReadyLock.EnterScope`、extra-turn list 的访问；
6. `RunManager.ActionExecutor` 与 `IsRunning/IsPaused/CurrentlyRunningAction`；
7. `CurrentlyRunningAction.GetType().FullName`；
8. `NGame.Instance.CurrentRunNode` 内的 `RootSceneContainer.CurrentScene`；
9. raw sample 构造/字段字符串化。

每次 catch 必须记录 `site + exception full type + message + count + first/last timestamp/tick`。R3 的 catch-all null 只能证明“sample 未返回”，不能定位上述任何一个根因。

## C. Checkpoint 与 baseline 时点

### 正常 map node entry 顺序

固定 DLL 与已验收 INV-002 合并得到：

```text
map node selection / vote accepted
  -> RunManager.EnterMapPointInternal(... saveGame:true)
     -> ExitCurrentRooms
     -> SaveManager.SaveRun(preFinishedRoom:null, saveProgress:true)
        -> (optional progress save)
        -> RunSaveManager.SaveRun(null)
        -> RunManager.ToSave(null)
        -> serialize/write current_run.save with PreFinishedRoom=null
     -> await save task completion
     -> choose/create room
     -> pause executor
     -> PushRoom / BeforeRoomEntered
     -> room.Enter / CombatRoom.StartCombat / combat setup / opening draw
     -> RunManager.RoomEntered
     -> first playable turn
```

这里的“`PreFinishedRoom` 变 null”不是修改 map 上旧 `SerializableRun` 的字段，而是 `ToSave(null)` 生成并写出一个新的 room-before save。R3 arm 读到的旧 save 为 `PreFinishedRoom != null`，所以它不能当 checkpoint；节点点击后的 50,627-byte write 才符合该生产分支的形状。

`EnterMapPointInternal` 在 IL `016A` 调用并 await `SaveRun(null,true)`，到 `01C2` 取回完成；room 直到 `0256` 才创建。这个区间是正式 baseline 的唯一正确原子边界。

### 正式 baseline contract

新的 observer 只在已 arm、exact target、`preFinishedRoom == null && saveProgress == true` 时，对 `SaveManager.SaveRun(AbstractRoom,bool)` 的返回 Task 做透明包装：

1. await 原 production task；一观察到完成便启动 monotonic capture deadline；
2. 在 wrapper 自己完成前 stable-read profile1 `current_run.save`；baseline 验证/封存耗时也计入该 deadline；
3. 要求文件存在、连续两次 identity 稳定、可解析、seed/character/act/map 与 arm target 相符、`PreFinishedRoom == null`，且内容 identity 不等于 arm-time non-null save；
4. 同一临界区立即封存 raw bytes hash、canonical hash、`NumReloads`、完整 allowlisted protection inventory/baseline；
5. 才让 wrapper Task 完成，使 `EnterMapPointInternal` 继续创建 room。

注意：production `SaveManager.SaveRun` 会 catch/log inner save exception，因此“Task completed”本身不是写成功证据；第 2–4 步验证不可省略。

`RunManager.RoomEntered` 是固定程序集存在的只读事件，但它在 push room、hooks、`room.Enter` 完成后才触发，太晚，不能用于 protection baseline。它适合作为 room-entry heartbeat/secondary deadline marker；若它先于已验证 checkpoint baseline 被观察到，必须终止为 `BLOCKED_CHECKPOINT_OBSERVATION_MISSED`，不能补建 baseline。

该方案不 patch 掉写入、不复制回存档、不 restore、不 rebaseline，只在生产 await 边界插入只读验证并短暂延后 continuation，解决了 R3 的冲突：baseline 既不取 map 上旧 non-null save，也不推迟到 input-ready 后。

## D. 下一实验的最小可观察 contract

### Identity 与一次性边界

- 新 mod ID：`SPIKE001A3`；schema：`spike-001a3.reconstruction-control.v1`；不得复用任何 001A2-R2 source/binary/artifact/recovery 名称。
- 每个 attempt 使用 `spike-001a3-r1-{attemptGuid}` artifact 与 recovery namespace，create-new、first-terminal-wins、不可覆盖。
- 001A2-R2 永久 sealed，不修改、不重建、不安装、不运行。
- 仅 `modded/profile1`；物理路径由当次 production API 解析；其他 profile 不读取、不枚举、不比较。
- Cloud 必须满足既有 production conjunction off；loaded mod set 精确为 `[SPIKE001A3]`；game/DLL/probe identity 全部先 gate。
- 沿用已验收 protection 边界、A-post/B-pre verification、normal Continue 只允许预声明的 `NumReloads +1` 与既定非语义 exit mtime 规则；不扩大 whitelist。

### Arm、baseline 与 deadline

- F8/arm 只记录目标与 pre-baseline observation；等待人工点击**永不超时**。
- 首个匹配的 production `SaveRun(null,true)` 原 Task 一完成，立即启动 30 秒 monotonic capture deadline；checkpoint 验证与 baseline 封存本身也在该 deadline 内。
- 正式 baseline 必须在 save wrapper 向 `EnterMapPointInternal` 返回之前封存。若先看到目标 `RoomEntered` 而 baseline 未封存，立即 terminal；不得事后补 baseline。
- deadline 由独立 owner Task/timer 驱动，不依赖 `_Process`。到期必须 create-new 写 `terminal`，包含 baseline identity、driver heartbeat、最后 raw sample、字段 transition/counters 与 exception summaries；不得无限 Ready。

### Driver heartbeat

Ticker 必须直接挂 `NGame`，显式 `SetProcess(true)` 且 effective `ProcessMode=Always`。至少记录：

- attach 时 parent/path、instance id、inside-tree、process enabled/effective mode、tree paused；
- 全局 first process tick；
- baseline 后 first tick；
- first combat tick；
- total tick count、combat tick count、last tick/frame/monotonic time；
- `_EnterTree/_ExitTree/TreeExiting` 或等效生命周期；
- 每个 bounded heartbeat 周期的 valid/inside-tree/parent/process mode/tree pause。

### Raw readiness 与 30 quiet frames

每个字段独立读取并保存 `{status: value|null|exception, value, site}`。保存首样本、任何字段状态变化、任何异常、每 15 tick 的 bounded periodic sample，以及 terminal 前最后样本；总事件设固定上限并保留 dropped/compacted counter，不能无限写日志。

quiet frame 定义为：正式 baseline 后，同一 target run/player/combat/executor/room identity 下，连续 30 个相邻 `_Process` callback 中，以下全部为 true且没有任一读取异常：

- state/player/player-combat/hand/combat/executor 均存在；
- combat in progress、not over/ending、combat/run not paused；
- player actions enabled、part of player turn、phase `Play`；
- executor not running、not paused、current action null；
- `CurrentRunNode` present；
- target seed/act/map/room/round identity 稳定。

任一 required field false、required object null、任一 read exception、target identity change、或 engine process-frame index 不是上一 accepted sample +1，都会把 quiet count 重置为 0，并记录具体 breaker。`HandCount` 必须记录与 A/B 比较，但不把固定正数作为通用 input-ready gate；本固定场景可另外做 scenario validation。

第 30 帧后在同一 tick 内重新读完整 raw vector；仍成立才先写 A/B snapshot。baseline 必须早已存在，snapshot 绝不能负责补建 baseline。

### 用户动作与终止

- 用户 arm 后只点击记录的首场普通弱怪节点。
- 目标点击后立即停止：不出牌、不 End Turn、不做任何 choice、不领奖、不进入下一节点。
- A 成功后立即正常退出；新进程通过 baseline/A-post/B-pre gate 后只点正常 Continue 一次，再停止所有输入。
- 任一 identity/profile/cloud/mod-set/protection/checkpoint/driver/deadline/exception terminal 立即停止，不请求修复、恢复、复制回或 rebaseline。

## Confirmed / Inferred / Unknown 汇总

### Confirmed

- 四项 fixed identity 全匹配。
- R3 唯一 capture driver、catch-all 和 predicate 字段集合如上。
- `ActionExecutor.IsRunning`、combat flags、player-turn membership、`CurrentRunNode` 的方法体语义如上。
- production room-before save 被 await，且严格早于 room create/enter。
- `RunManager.RoomEntered` 晚于 `room.Enter`，不能作为 baseline 时点。
- R3 sealed log 中 production write 与 ActiveCombat 均发生，但 baseline/A/terminal 缺失。

### Inferred

- 直接挂在 `NGame` 下的 ticker应跨 run scene replacement 存活；这是 Godot node ownership与固定 production scene container结构的联合推断，不是 R3 runtime heartbeat。
- 50,627-byte write 是目标 room-before checkpoint；调用顺序和日志形状强支持，但 R3 未在当时 parse/seal，故历史 artifact 仍不能升级为正式 baseline。

### Unknown

- R3 实际首个失败是 ticker 停止、effective process mode/tree pause、某个访问异常，还是某 predicate 长期 false。
- R3 每个 raw readiness 字段的动态值与变化序列。
- A/B reconstruction 是否等价。

这些 Unknown 都会被新 contract 的 bounded telemetry 直接区分，不再构成必须先签发 diagnostic-only probe 的关键缺口。

## 结果分类与唯一建议

**分类：`READY_NEW_RECONSTRUCTION_CONTROL`**

唯一下一任务：签发全新 `SPIKE-001A3`，严格实现 D 节 contract，完成一次 room-before checkpoint 的 uninterrupted A / normal Continue B reconstruction control。该任务仍只回答 reconstruction equivalence；不实现 capture/dispatch、快速重演、强记录、seek 或正式架构，也不直接解锁 `SPIKE-001B`。

未选择 `READY_DIAGNOSTIC_ONLY`：剩余动态首因已可在新的完整 control 中以不改变 save 的 telemetry 同时诊断，不存在一个必须先单独回答、否则无法安全建立 baseline/A/B 的事实。  
未选择 `RETIRE_NATIVE_CHECKPOINT_CONTROL`：固定方法体反而给出了明确、安全、早于 room creation 的 room-before checkpoint completion 边界。

## 工具、命令、验证与偏离

- PowerShell 7.6.4、`Get-FileHash`、`rg`。
- 复用 `evidence/inv-002/Invoke-NarrowIlQuery.ps1` 的 hash-gated Mono.Cecil 查询；INV-010 wrapper 与复现命令见 `evidence/inv-010/README.md`。
- 运行 `Invoke-Inv010StaticQuery.ps1`：成功生成 1,377 行输出。
- 运行 `Test-ReconstructionContract.ps1`：20/20 PASS。
- 只读检查所有 handoff 必读文件、R3 最终 source、固定 sealed log/artifacts；未读取当前游戏用户状态。
- 未使用 Computer Use/GUI；未启动游戏；未创建下一 runtime probe；未请求用户测试；未修改任何项目管理状态文件。
- 无范围偏离。到此满足 INV-010 停止条件，交回 Project Lead。

# INV-004 — native CombatReplay 完整性与隔离哨兵解耦调查

状态：Complete（只读静态调查）  
调查日期：2026-09-08  
固定游戏：public-beta `v0.111.0` / commit `41cef1ea`  
固定 DLL SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`  
固定 fixture SHA-256：`4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`

## 0. 结论先行

1. **fixture 完整性不是 Phase C 的首个失败边界。** 固定 fixture 的 11 个 events 不只覆盖两个回合，而是覆盖三个回合；最后三个 event 是第三回合对 combat id 2 的三次 `STRIKE_IRONCLAD`。对应 checksum ids 15/16/17 显示目标 HP `17→11→5→移出 creatures`，最后执行 action id 10 后只剩存活玩家。该文件覆盖到了原始战斗的致死动作。`[Confirmed fixture bytes]`
2. **native 格式没有 serialized “complete/victory” flag 或终止 event。** `events>0` 对任意 fixture 绝不足以证明覆盖胜利；但本固定 fixture 的末尾 full-state checksum 提供了比 event-count 更强的、fixture-specific 的胜利证据。`[Confirmed method body + fixture bytes]`
3. **Phase C dispatcher 没有偏离 official `NMultiplayerTest.RunReplay` 的 event-loop 语义。** 四种 event 分支、turn wait、combat-side gate 和特殊 action wait 均逐项一致。Probe 只在循环后额外等待 executor/sentinel，并增加 transaction/teardown。故“事件流未使战斗结束”不能归因于 probe 自创 dispatcher 差异。`[Confirmed method body/source comparison]`
4. **最窄的下一实验只有一个：direct production progress-write sentinel，不 dispatch 任何 replay event。** 复用 Phase B 已证明可进入的 isolated replay session，仅把其中的 live `Player` 与 `CombatRoom` 作为合法参数，直接按 production combat-victory call site 的顺序调用 isolated `SaveManager.Instance.UpdateProgressAfterCombatWon(player, room)` 与 `SaveProgressFile()`；观察 memory store write、原 manager/store 零调用后立即 teardown。它只回答 direct write 是否被隔离，不回答 replay 能否到胜利。`[Recommended experiment]`

本任务没有启动游戏、没有运行或修改 probe、没有解锁/重试 Phase C，也没有修改 Project Lead 管理文档或既有报告。

## 1. 证据等级与身份门禁

- `[Confirmed method body/call site]`：固定 hash `sts2.dll` 的 Mono.Cecil 方法体、字段或调用点。
- `[Confirmed fixture bytes]`：hash-gated native `PacketReader` 对固定备份的只读反序列化结果。
- `[Confirmed probe evidence/source]`：封存的 Phase C JSON 或既有 `ReplayTransaction.cs`。
- `[Inferred]`：由上述事实推导的机制候选。
- `[Unknown]`：当前静态/封存证据不足，需独立实验证伪。

当前安装路径中的 DLL 长度 `9,757,184`、ProductVersion `0.1.0+41cef1ea...`、SHA-256 均匹配。受保护备份中的 fixture 长度 `20,441`、SHA-256 亦匹配，完整消费 `20,441/20,441` bytes、trailing bytes 0。未进入 `BLOCKED_IDENTITY`。查询脚本与复现命令见 `evidence/inv-004/README.md`。

## 2. A — native recorder 与文件完整性

### 2.1 recorder 生命周期

| 时点 | 固定 DLL 行为 | 结论 |
|---|---|---|
| run 初始化 | `RunManager.InitializeShared` 构造一个 `CombatReplayWriter`，订阅 action enqueue/resume、player choice 与 checksum；`IsEnabled = !TestMode.IsOn` | writer 是 run 级对象，不是在第一次点击卡牌时创建。`Confirmed` |
| 每房间开始 | `EnterMapPointInternal` 在 room 创建/进入前调用 `RecordInitialState(ToSave(null))`；debug room 路径同样调用 | 每次调用新建 `CombatReplay`、记录版本/hash/各 synchronizer next ids 与 baseline，并清空 events；这才开始该 room 的 recording。`Confirmed` |
| 记录窗口 | 四个 recorder callback 都先检查 `IsEnabled` 与 `CombatManager.IsInProgress`，并要求 `_replay != null` | 只有 active combat 内的这些同步边界会加入 replay。`Confirmed` |
| 正常胜利 finalize | `EndCombatInternal` 一开始先令 combat `IsInProgress=false`；完成 `AfterCombatEnd` 后，非 replay net type 调 `RunManager.WriteReplay(true)` | 文件在 combat teardown 内、`AfterCombatVictory` 和 progress update 之前写出；`true` 随后把 `_replay=null`。`Confirmed` |
| 非正常/辅助 flush | `CleanUp` 在 `_replay != null` 时也 `WriteReplay(true)`；state divergence client 与 debug log zip 可 `WriteReplay(false)` | cleanup 可以把尚未胜利的当前录制写成 `latest.mcr`；snapshot write 则保留 recording。`Confirmed` |
| 实际写盘 | `WriteReplay` reset `PacketWriter`，serialize `Anonymized()`，直接以 write mode 打开精确 `replays/latest.mcr`，写 buffer 后 dispose stream | 没有 temp file、rename、atomic replace 或 replay backup。异常仅 log warning；当 `stopAfterWriting=true` 时仍会 `StopRecording`。`Confirmed` |

因此“替换 latest.mcr”的准确描述是**直接覆盖目标路径**，不是原子替换。进程/磁盘故障时存在截断或旧文件残留风险；本固定 fixture 已能完整 native parse，但这不把写入机制升级为 crash-safe。`[Confirmed + Inferred risk]`

### 2.2 哪些内容进入 `events`

- `ActionQueueSet.ActionEnqueued`：active combat 内，`GenericHookGameAction` 记录为 `HookAction(playerId, hookId, gameActionType)`；其他 action 若 `RecordableToReplay=true`，记录为 `GameAction(playerId, ToNetAction())`。基类默认 true，固定 DLL 仅 `GenericHookGameAction` override 为 false且已被特判。`[Confirmed method body]`
- `ActionQueueSet.ActionResumed`：active combat 内记录 `ResumeAction(actionId)`。`[Confirmed method body]`
- `PlayerChoiceSynchronizer.PlayerChoiceReceived`：active combat 内记录 `PlayerChoice(playerId, choiceId, result)`；local sync 与 replay receive 都会进入该 synchronizer 的 receive boundary。`[Confirmed method body]`
- checksum 不进入 `events`，而进入独立 `checksumData(context, checksum, fullState)`；初始 choice/reward/action/checksum/hook next ids 也只是 baseline metadata。`[Confirmed serialization fields]`
- combat 外的 action/choice/resume/checksum明确被 `IsInProgress` gate 丢弃；战后 gold/potion reward 因此不属于 combat events。writer 也没有订阅 reward selection 事件，只有 room baseline 中的 reward-id seed。`[Confirmed method body]`

`ActionEnqueued` callback 在 queue 的后续 cancellation guards 之前触发，所以“出现在 `.mcr`”只证明 action 曾 enqueue，不单独证明它最终执行成功；checksum 的 `lastExecutedActionId` 与 full state 才能补强这一点。`[Confirmed method body + Inferred interpretation]`

### 2.3 是否存在完整标志

`CombatReplay` serialized fields 只有 version/commit/model hash、choice/reward ids、next action/checksum/hook ids、`SerializableRun`、events 与 checksumData；`CombatReplayEventType` 只有 `GameAction/HookAction/ResumeAction/PlayerChoice`，无 victory/end-of-combat event、complete bit 或 expected terminal count。`IsRecordingReplay` 只是运行时 `_replay != null`，不会序列化。`[Confirmed method body/fields]`

所以：

- 对未知 fixture，`events>0` 明确不足；cleanup 甚至能 finalize 未胜利的录制。`[Confirmed]`
- 对本固定 fixture，末尾 checksum 17 绑定 `lastExecutedActionId=10`，敌方 creatures 从一个 `TOADPOLE` 5 HP 变为零，玩家仍为 72/80 HP；这证明最后 event 的致死 action 在原始录制中已执行。`[Confirmed fixture bytes]`
- 文件中仍没有独立的“combat teardown completed”bit；这里的胜利判断来自 full-state 语义与正常文件来源的组合，不应泛化成格式保证。`[Inferred, high confidence]`

### 2.4 固定 fixture 的有序 event 清单

所有 11 项均为 player `1590013117` 的 `GameAction`；无 hook/resume/choice event。

| # | net action | 关键字段 | 原始执行证据 |
|---:|---|---|---|
| 0 | `NetPlayCardAction` | `DEFEND_IRONCLAD`, combat-card index 2, no target | checksum action 0：block 5 |
| 1 | `NetPlayCardAction` | `DEFEND_IRONCLAD`, index 0, no target | checksum action 1：block 10 |
| 2 | `NetPlayCardAction` | `STRIKE_IRONCLAD`, index 3, target 2 | checksum action 2：target 23→17 |
| 3 | `NetEndPlayerTurnAction` | turn 1 | turn-one phase-one/two + enemy-turn checksums |
| 4 | `NetReadyToBeginEnemyTurnAction` | — | same turn boundary |
| 5 | `NetPlayCardAction` | `BLUDGEON`, index 7, target 1 | checksum action 5：25-HP target removed |
| 6 | `NetEndPlayerTurnAction` | turn 2 | turn-two phase-one/two + enemy-turn checksums |
| 7 | `NetReadyToBeginEnemyTurnAction` | — | same turn boundary |
| 8 | `NetPlayCardAction` | `STRIKE_IRONCLAD`, index 5, target 2 | checksum action 8：17→11 |
| 9 | `NetPlayCardAction` | `STRIKE_IRONCLAD`, index 1, target 2 | checksum action 9：11→5 |
| 10 | `NetPlayCardAction` | `STRIKE_IRONCLAD`, index 3, target 2 | checksum action 10：last enemy removed |

完整字段和 18 个 checksum summaries 见 `evidence/inv-004/fixture-event-inventory.json`。

### 2.5 对“两个回合还是 finalization 丢动作”的回答

固定 fixture 本来就不是两个回合：它明确含 turn 1、turn 2 的 end/ready，随后含 turn 3 的三次出牌及其逐 action checksum；最后一次为致死动作。`[Confirmed fixture bytes]`

因此针对本 fixture，可排除“11 项只对应两个回合”，也没有证据支持“recorder/finalization 丢了胜利前的后续动作”。用户 Phase C 的“两回合后第三回合不再动作”应准确表述为：**同一事件流在 probe replay session 中没有产生 fixture 末尾所记录的胜利状态**。由于 Phase C 没保存逐 event actual state、cancel outcome 或 checksum mismatch，本任务不把它升级为已确认 replay divergence。`[Confirmed + Unknown exact runtime boundary]`

## 3. B — official debug caller 与 Phase C

### 3.1 `NMultiplayerTest.RunReplay` 完整流程

固定 DLL 的 async state machine 顺序为：

1. 读取本地 commit；与 replay commit 不同只 warning。
2. `RunState.FromSerializable(replay.serializableRun)`；按 `playerIndex` 取 local net id；`RunManager.SetUpReplay`。
3. `CombatStateSynchronizer.IsDisabled=true`。
4. preload run characters 与 act；`Launch`；停音乐；`NRun.Create` 并替换 root scene；await `GenerateMap`。
5. fast-forward action/hook/choice/reward ids；load checksumData。
6. `LoadIntoLatestMapCoord(AbstractRoom.FromSerializable(PreFinishedRoom, runState))`。
7. 按 frame 等到 `ActionExecutor.IsPaused=false`。
8. 顺序枚举 events，执行下表四种分支；只有 end-turn/ready action await `FinishedExecutingActions()`。
9. 枚举结束即 `RunReplay` task 成功完成；方法不等待 combat victory、`IsInProgress=false`、enemy count、checksum 全部消费或 direct progress write，也不 teardown。

逐项对照表单独保存在 `evidence/inv-004/official-debug-caller-vs-phase-c.md`。核心结论是两边 dispatcher 相同，Phase C 还额外给了 post-loop executor 与 15 秒 sentinel 等待。`[Confirmed]`

### 3.2 候选原因与可证伪方法

| 候选 | 状态 | 证据与边界 | 最小证伪方法（未来独立 replay-fidelity 实验） |
|---|---|---|---|
| Probe 漏掉/改写某种 event branch | **Confirmed false** | 四种 branch、turn flags、side gate、特殊 wait 均与 official caller 相同；fixture 又只有 GameAction | IL/source diff 已闭合；无需重跑 Phase C |
| Probe 的 post-loop 等待导致事件提前耗尽 | **Confirmed false** | 等待发生在 enumeration 之后，只延长 observation | 无需运行 |
| official/probe 共用的 readiness 语义不足 | **Inferred candidate** | card action 只等 `CurrentSide != Enemy`；end/ready 只等 `ActionExecutor.FinishedExecutingActions`。后者在 completion source 为空时立即 CompletedTask；两边均未等 `ActionQueueSet.BecameEmpty`、player `Phase==Play` 或 quiet frame | 每个 event enqueue 前后记录 side、phase、turn flags、queue empty、executor task generation 与 cancel/execute outcome |
| 第三回合 `NetCombatCard` 映射/hand readiness 使 action 被取消 | **Unknown** | fixture 原始 card indexes 5/1/3 有成功 checksum；Phase C 没保存 reconstructed hand 或 cancel result | 在 action ids 8/9/10 前记录 resolved card id/index、pile/hand membership、energy 与 cancel reason |
| replay reconstruction 在第三回合前已与 recorded checksum 不同 | **Unknown** | fixture 提供 18 个 expected full states；Phase C 没保存 actual checksum comparison或 mismatch事件 | 在 action ids 0/1/2/5/8/9/10 后做只读 expected/actual checksum与白名单 state compare |
| transaction manager / `ShouldSave=false` 改变胜利前 dispatch | **Inferred unlikely, not ruled out** | 固定方法体只显示 `ShouldSave` gate save/run-end；manager deep-copy内容在 Phase B 验证相等，但未做无 transaction replay control | 如未来需要，仅做独立 A/B；不得把它塞回 direct-write sentinel |
| 最后三张牌均执行但 victory 超过 45 秒才发生 | **Unknown, weakened** | probe 在 enumeration 后 await executor 最多 30 秒并再观察 15 秒；用户又未看到第三回合动作。没有 final action execution marker，故不能形式化排除 | 记录 action id 10 executed、enemy count、`IsInProgress` 与 `EndCombatInternal` entry timestamps |

最可能的第一条可操作假设是**共用 caller readiness 缺口或 action cancellation**，而不是 probe/official 分派差异；但本轮不把它宣称为根因。`[Inferred]`

## 4. C — direct-write sentinel 候选

| 候选 | 能证明 | 不能证明 | replay 耦合 | 所需状态 / side effect / 停止条件 | 结论 |
|---|---|---|---|---|---|
| 经证明完整、可一击结束的 native fixture | native dispatch、reconstruction、victory callback 和 store routing 的组合成功 | 任一失败时不能区分是哪层 | 高 | 需另制/证明 fixture；会触发完整 combat teardown、achievement/reward等边界；胜利或首个 mismatch 停 | 不推荐用于隔离哨兵 |
| 有效 replay session 内人工/受控原生动作获胜 | live victory path 能命中 isolated store | 不证明记录事件可 replay；输入时序和战斗状态不确定 | 中到高 | 需人工出牌/选择；可能触发 replay writer 的 `_replay==null` 记录异常、完整 victory side effects | 不推荐 |
| transaction 内直接调用 production update + save | direct progress mutation 与 `progress.save` write 是否解析到 isolated manager/store；原 manager 是否零调用 | 不证明 `CombatManager` 自然胜利、replay reconstruction/dispatch或 checksum fidelity | **无 event-dispatch 耦合** | 用 Phase B 已证明的 live player/normal `CombatRoom`；只改 isolated progress/player discovery并写 memory store；两次调用后立即 teardown | **唯一推荐** |
| 只调用 `SaveProgressFile` | store routing | 不覆盖 `UpdateProgressAfterCombatWon` 的真实参数/状态和 mutation | 无 | 最窄但覆盖不足 | 拒绝，太窄 |
| 直接强制 `CombatManager.EndCombatInternal` / 改敌人 HP | 更接近自然 caller | 合法 victory 前置条件难保证 | 中 | 会混入 hooks、room prefinish、save、achievement、reward、scene 和 combat teardown | 拒绝，副作用面过大 |

### 4.1 唯一下一运行建议：`SPIKE-INV004A — direct progress-write sentinel`

在一个新的工作包中复用**已通过 Phase B 的 transaction/setup/ordered teardown**，但不要复用 `DispatchReplayEvents`，也不要解锁原 `phase-control.json`：

1. 重新做固定 DLL、fixture、profile1、Cloud-off、原 manager/store 与受保护文件基线门禁。
2. 安装同一 fully initialized isolated manager，`SetUpReplay` 后立即 `ShouldSave=false`，进入首个 watchable normal-combat scene；这里仅把固定 fixture 当作已验证的合法 state factory，不消费其任何 event。
3. 断言 live 参数合法：单玩家存在；`CurrentRoom is CombatRoom`；`CombatState.RunState` 与 current run identity 相同；encounter/spawned enemies 可读；room type 是该固定普通战斗；isolated `Progress` 与原对象不共享引用。
4. 记录 isolated progress canonical baseline；按 fixed production call site 的顺序调用当前 `SaveManager.Instance.UpdateProgressAfterCombatWon(player, room)`，再调用当前 `SaveManager.Instance.SaveProgressFile()`。不 patch-away write、不调用 `EndCombatInternal`、不出牌、不 dispatch replay event。
5. 必须同时观察：两个 SaveManager audit boundary 均命中 isolated manager；isolated progress canonical state按该 encounter/character 增加一次 win；memory store 对 profile1 `progress.save` 恰有预期 write且 payload 可 deserialize；原 manager/store/cloud/external-submit counters 全为 0。
6. 达成上述观测后立即执行唯一 ordered teardown；核对 manager identity、`RunManager.State==null`、Continue 与全部受保护文件 hash/length/mtime/inventory 不变。不要进入奖励、地图或下一房间。

失败/停止条件：任一身份或合法状态断言失败、任一 original/cloud/external call、写入未落 memory store、写入次数/路径异常、isolated mutation 不符、teardown/文件 identity 不符，立即 FAIL-CLOSED；不得改成“换 fixture 重试”。

### 4.2 这如何拆开两个判定

- 若 `SPIKE-INV004A` 通过，只能把“direct progress write 落入 isolated manager/store”标为 PASS，并允许之后继续 transaction 的 fault/restart coverage；**native replay 到胜利仍是独立 Unknown/FAIL-to-cover**。
- native replay fidelity 后续若获批，另建工作包，以本报告的 expected checksums 和逐 event readiness/cancel telemetry 验证 action ids 0–10；不得再承担 save-isolation sentinel。
- 若 `SPIKE-INV004A` 失败，则直接否定/收窄 transaction/store boundary，不需要解释任何 replay dispatch。

这满足“一次只验证一层”的停止条件，也不依赖盲目更换 fixture。

## 5. 最终状态与未闭合项

- Recorder/file：本 fixture 已确认覆盖第三回合致死 action；格式本身仍无通用完整标志，直接覆盖写盘也非 crash-safe。
- Dispatcher fidelity：Phase C event loop 与 official debug caller 一致；实际第三回合 action 未执行的第一原因仍为 `Unknown`，readiness/cancellation 是首要可证伪候选。
- Victory sentinel：存在可信、有界且不 dispatch replay events 的方案，因此不是 `BLOCKED_SENTINEL`；唯一推荐为 `SPIKE-INV004A` direct production progress-write sentinel。
- Phase B 仍仅是 management/setup/normal Stop 的局部 PASS；session isolation 没有升级为完整 PASS。Phase C 也没有证明 in-memory transaction 失败。

满足 INV-004 验收条件后停止；不重试 Phase C，不执行原 D/E/F，不进入 reconstruction/capture。

# INV-001 Investigation Report

## 任务

- Task ID：`INV-001`
- 任务范围：为 STS2 public-beta `v0.111.0` 的 `SPIKE-001` 最小单人录制—重放探针建立定向入口地图。
- 明确非目标：未实现 Replay Mod；未修复或修改 RunReplays；未切换 v0.107；未重演 sample；未调查完整场景、最终架构、路线图 UI、跨版本、多人、接管或 gameplay mod 协议。
- 状态：**Complete**

## 结论摘要

1. **Confirmed**：v0.111 上存在可供最小战斗探针验证的捕获入口：`CardModel.EnqueueManualPlay(Creature)`、`ActionExecutor(ActionQueueSet)` 及其 `BeforeActionExecuted` / `AfterActionExecuted` 事件、`CombatManager.TurnStarted` / `TurnEnded`。RunReplays 对应参考位于 [`PlayerActionBuffer.cs:32`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L32) 和 [`PlayerActionBuffer.cs:470`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L470)。
2. **Confirmed**：v0.111 上存在最小 dispatch 候选：用 `NetCombatCardDb.GetCard(uint)` 解析战斗卡，再调用 `CardModel.TryManualPlay(Creature)`；回合结束可调用 `PlayerCmd.EndTurn(Player, bool, Func<Task>?)`。RunReplays 参考位于 [`PlayCardCommand.cs:37`](../../references/RunReplays-may-2026/RunReplays/Commands/PlayCardCommand.cs#L37) 和 [`CardPlayReplayPatch.cs:480`](../../references/RunReplays-may-2026/RunReplays/Patches/Replay/CardPlayReplayPatch.cs#L480)。这些目标符号由 v0.111 DLL 只读反射直接确认。
3. **Confirmed**：RunReplays 的 readiness 不能原样用于 v0.111。其 `IsCombatReady()` 依赖 [`CombatManager.IsPlayPhase`](../../references/RunReplays-may-2026/RunReplays/Patches/Replay/CardPlayReplayPatch.cs#L201)，但目标 DLL 没有该属性。v0.111 候选是 `PlayerCombatState.Phase == PlayerTurnPhase.Play`，并联 `CombatManager.IsPartOfPlayerTurn(player)`、`PlayerActionsDisabled == false`、`IsPaused == false`、`ActionExecutor.IsRunning == false`；组合是否充分仍是 **Unknown**。
4. **Confirmed**：RunReplays 的继续/恢复调用也不能原样用于 v0.111。固定源码使用 `RunManager.SetUpSavedSinglePlayer`（大写 `P`，见 [`RunContinuePatch.cs:20`](../../references/RunReplays-may-2026/RunReplays/Patches/Record/RunContinuePatch.cs#L20) 和 [`RunReplayMenu.cs:568`](../../references/RunReplays-may-2026/RunReplays/RunReplayMenu.cs#L568)）；目标 DLL 只有异步 `Task SetUpSavedSingleplayer(RunState, SerializableRun)`。
5. **Confirmed**：v0.111 不再公开逐个 `Rng.Seed` / `Rng.Counter`，而固定源码多处直接访问它们（例如 [`EncounterRngTracker.cs:91`](../../references/RunReplays-may-2026/RunReplays/Utils/EncounterRngTracker.cs#L91)）。目标 DLL 提供 `Rng.ToSerializable()` 和 `RunRngSet.ToSerializable()`；序列化结果包含 counter 与四个 state 字段，因此它们是探针的状态记录候选，但最小必要 RNG 集合仍是 **Unknown**。
6. **Inferred**：最小探针不应加载 RunReplays 整体 dispatcher。其同步逐事件诊断写盘、轮询、全量状态遍历和 UI 查找会污染 `SPIKE-001` 后续性能观测；这里只借鉴语义边界和游戏 API 调用方向。

## 环境与版本

| 项目 | 结果 | 等级与证据 |
| --- | --- | --- |
| 目标游戏 | public-beta `v0.111.0`，commit `41cef1ea`，branch `v0.111.0` | **Confirmed**：只读 `release_info.json`；固定要求见 [`INV-001.md:15`](../handoffs/INV-001.md#L15)。 |
| 目标 DLL | `data_sts2_windows_x86_64/sts2.dll`，9,757,184 bytes，SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` | **Confirmed**：`Get-Item` 与 `Get-FileHash`。 |
| RunReplays 固定源码 | tag archive `may-2026`，上游短 commit `05b0edd`，根 manifest `v0.2.0`，archive SHA-256 `654B9E416158943440C9C08BAF1D4FAF78A3581D3926A64D19A4F0220027C7DC` | **Confirmed**：[`references/README.md`](../../references/README.md) 与根 [`RunReplays.json`](../../references/RunReplays-may-2026/RunReplays.json)。归档无 `.git`，故完整 commit 仍不可从本地独立复算。 |
| 源码树内第二份 manifest | `RunReplays/RunReplays.json` 报 `v0.1.2`、`has_pck: true` | **Confirmed**：[`RunReplays/RunReplays.json`](../../references/RunReplays-may-2026/RunReplays/RunReplays.json)。不能用版本字符串单独识别 artifact。 |
| Workshop artifact | item `3759973128`；manifest `v0.1.2`；DLL SHA-256 `FDE403AD981E605607BCACBF0D46F3008659195B83A6252D4B5B2710FFF12076`；ProductVersion commit `4b1fcfed256888d95e012b0af85de585eeb6734a` | **Confirmed**：[`artifact README`](../../evidence/external-artifacts/RunReplays-workshop-3759973128-v0.1.2/README.md)；本次再次运行 `Get-FileHash` / `VersionInfo`。 |
| 参考编译目标 | .NET 9；GodotSharp `4.7.0-dev.2`；Harmony `2.4.2`；BaseLib | **Confirmed**：[`RunReplays.csproj`](../../references/RunReplays-may-2026/RunReplays/RunReplays.csproj)。实际游戏 loader/runtime 完整版本 **Unknown**，本调查没有启动游戏。 |
| OS / 调查工具 | Windows；PowerShell；本机 `dotnet 10.0.1126.37416`；`System.Reflection.Assembly.LoadFrom` 只读符号检查 | **Confirmed**。没有 `ilspycmd` / `ildasm`；未反编译方法体，只检查了公开/非公开元数据签名。 |

### 目标 DLL 符号检查命令

所有下表“目标 DLL”结论均在上述 hash 上运行。核心命令形式如下：

```powershell
$dll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
Get-FileHash -Algorithm SHA256 -LiteralPath $dll
$asm = [Reflection.Assembly]::LoadFrom($dll)
$type = $asm.GetType('MegaCrit.Sts2.Core.Runs.RunManager')
$type.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly') |
  ForEach-Object { $_.ToString() }
```

本次还对 `RunState`、`ActionExecutor`、`CardModel`、`NetCombatCardDb`、`PlayerCmd`、`CombatManager`、`PlayerCombatState`、`Rng`、`RunRngSet`、`SerializableRun`、`NGame`、`NMapScreen` 和主菜单相关类型执行了同类查询，并检查参数名、是否 optional、属性和事件。

## 按八类问题的入口地图

用途标签仅表示对 `SPIKE-001` 的价值，不是最终复用决策。

### 1. 单人 run 生命周期

| 等级 | 入口、方向与版本 | 用途标签 | 证据与限制 |
| --- | --- | --- | --- |
| **Confirmed** | v0.111 `NGame.StartNewSingleplayerRun(...) -> Task<RunState>` 建立新单人 run；`RunManager.RunStarted(Action<RunState>)` 是开始事件。 | Candidate hook/concept | 目标 DLL 元数据。RunReplays 从 UI 直接调用该入口，见 [`RunReplayMenu.cs:580`](../../references/RunReplays-may-2026/RunReplays/RunReplayMenu.cs#L580)。 |
| **Confirmed** | v0.111 载入链：`RunState.FromSerializable(SerializableRun)` → await `RunManager.SetUpSavedSingleplayer(RunState, SerializableRun)` → await `NGame.LoadRun(RunState, SerializableRoom)`。 | Candidate hook/concept | 目标 DLL 元数据；RunReplays 的旧调用方向见 [`RunReplayMenu.cs:548`](../../references/RunReplays-may-2026/RunReplays/RunReplayMenu.cs#L548)，但方法名/异步语义已 break。 |
| **Confirmed** | `RunManager.RoomEntered` / `RoomExited`、`ActEntered` 提供房间/章节边界；RunReplays 已订阅前两个，见 [`ReplayDispatcher.cs:813`](../../references/RunReplays-may-2026/RunReplays/ReplayDispatcher.cs#L813)。 | Candidate hook/concept | 事件在目标 DLL 仍为 public。 |
| **Confirmed** | 结束相关候选为 `RunManager.WinRun()`, `Abandon()/AbandonInternal()`, `OnEnded(bool) -> SerializableRun`；`RunState.IsGameOver` / `RunManager.IsGameOver` 可作状态确认。 | Reference only | 目标 DLL 元数据。没有运行验证哪个入口对胜利、失败、主动放弃分别最早且只触发一次。 |
| **Inferred** | “楼层结束”没有独立已确认事件；可在 `RoomExited` 后结合 `RunState.TotalFloor/ActFloor/RunLocation` 形成结构标记。 | Candidate hook/concept | 推理链：目标 DLL 有房间事件和结构字段，但未找到 `FloorEnded` 事件。需在 spike 中打印事件顺序验证。 |
| **Unknown** | `RunStarted`、`RoomEntered`、`RoomExited`、`ActEntered` 与 `ToSave` 在新 run、continue、胜利、嵌套房间/事件战斗中的精确先后与重复次数。 | — | 必须通过 v0.111 运行探针确认，不能由符号存在推出行为。 |

### 2. 最小战斗的语义操作捕获与 dispatch

| 等级 | 层次与调用方向 | 用途标签 | 证据与限制 |
| --- | --- | --- | --- |
| **Confirmed** | **UI 输入层**：`NEndTurnButton.OnPress()`、卡牌 UI 节点存在，但它们受场景层级影响。 | Reject for spike | 目标 DLL 元数据。最小探针无需重放鼠标点击或依赖 UI 节点路径。 |
| **Confirmed** | **语义命令层（出牌捕获）**：Harmony postfix `CardModel.EnqueueManualPlay(Creature)`；从 `NetCombatCardDb.TryGetCardId(card, out id)` 取得卡 ID、从 target 取得 `CombatId`。 | Candidate hook/concept | RunReplays [`PlayerActionBuffer.cs:465`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L465)；目标 DLL 保留同签名和 DB 方法。它位于已接受手动出牌之后、action 执行之前。 |
| **Confirmed** | **game action 层（完成观察）**：`ActionExecutor.BeforeActionExecuted` / `AfterActionExecuted(GameAction)`；`GameAction.RecordableToReplay`；`PlayCardAction`、`EndPlayerTurnAction` 可分类。 | Candidate hook/concept | RunReplays [`PlayerActionBuffer.cs:75`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L75) 与 [`PlayerActionBuffer.cs:134`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L134)；目标 DLL 构造器和事件仍存在。 |
| **Confirmed** | **出牌 dispatch**：等待 readiness → `NetCombatCardDb.GetCard(id)` → `card.TryManualPlay(target)` → 由 `AfterActionExecuted(PlayCardAction)` 观察完成。 | Candidate hook/concept | RunReplays [`PlayCardCommand.cs:37`](../../references/RunReplays-may-2026/RunReplays/Commands/PlayCardCommand.cs#L37)；目标 DLL `GetCard`、`TryManualPlay` 均存在。 |
| **Confirmed** | **结束回合 dispatch**：`PlayerCmd.EndTurn(player, canBackOut:false, actionDuringEnemyTurn:null)`；以 `TurnEnded` / 下一次 `TurnStarted` 观察推进。 | Candidate hook/concept | RunReplays [`CardPlayReplayPatch.cs:480`](../../references/RunReplays-may-2026/RunReplays/Patches/Replay/CardPlayReplayPatch.cs#L480)；目标 DLL 第三个参数为 optional。 |
| **Inferred** | `EnqueueManualPlay` postfix 比 `AfterActionExecuted` 更接近玩家语义意图；后者适合确认结果，不应单独当作所有玩家选择的捕获入口。 | Candidate hook/concept | RunReplays 为避免 action 展开/重复而明确早录出牌，见 [`PlayerActionBuffer.cs:151`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L151)。需运行验证失败出牌是否会到达 postfix。 |
| **Unknown** | `NetCombatCardDb` 的 combat ID 在“从相同初始状态独立恢复”后是否稳定；若不稳定，需同时记录稳定 card ModelId、手牌位置或别的解析键。 | — | 当前只有 API 和 RunReplays 用法，没有 v0.111 跨会话观测。 |

### 3. seed、RNG、初始状态与选择结果

| 等级 | 候选字段/入口 | 用途标签 | 证据与限制 |
| --- | --- | --- | --- |
| **Confirmed** | `RunState.CreateForNewRun(..., string seed)` 与 `NGame.StartNewSingleplayerRun(..., string seed, GameMode, int, DateTimeOffset?)` 接收字符串 seed。 | Candidate hook/concept | 目标 DLL 元数据；RunReplays 传入记录 seed，见 [`RunReplayMenu.cs:611`](../../references/RunReplays-may-2026/RunReplays/RunReplayMenu.cs#L611)。 |
| **Confirmed** | `RunState.Rng : RunRngSet`；`RunRngSet.StringSeed/Seed`；`RunRngSet.ToSerializable()` / `FromSave()` / `LoadFromSerializable()`；`Rng.ToSerializable()`。 | Candidate hook/concept | 目标 DLL 元数据。`SerializableRun.SerializableRng` 和 `SerializableRunRngSet.Rngs` 可携带完整 run RNG 集。 |
| **Confirmed** | v0.111 `SerializableRng` 公开字段为 `counter`, `state0`, `state1`, `state2`, `state3`；逐 RNG 的 public `Seed/Counter` 属性不存在。 | Candidate hook/concept | 目标 DLL 元数据。固定源码的直接属性访问不可移植。 |
| **Confirmed** | 游戏原生 `SerializableRun` 包含角色、act、modifier、地图历史、已访问坐标、`SerializableRng`、`PreFinishedRoom` 等初始/恢复候选。 | Candidate hook/concept | 目标 DLL 元数据；RunReplays 在 `ToSave` postfix 接收完整对象，见 [`RunSaveLogger.cs:27`](../../references/RunReplays-may-2026/RunReplays/Patches/Record/RunSaveLogger.cs#L27)。 |
| **Confirmed** | 出牌结果至少需记录 card 解析键和可选 target；结束回合无需选择字段。任何打开选择界面的卡会产生额外语义选择，不属于本次最小场景。 | Candidate hook/concept | RunReplays `PlayCardCommand` 字段见 [`PlayCardCommand.cs:15`](../../references/RunReplays-may-2026/RunReplays/Commands/PlayCardCommand.cs#L15)。 |
| **Reject for spike** | RunReplays 强制 `UnlockState.all` / `HasSeenEncounter=true` 来改写内容池。 | Reject for spike | [`UnlockAllPatch.cs:23`](../../references/RunReplays-may-2026/RunReplays/Utils/UnlockAllPatch.cs#L23)。它改变玩家进度语义，不能作为最小确定性证明。 |
| **Inferred** | 对 SPIKE-001，记录完整 `SerializableRunRngSet` 比只记 seed 更能隔离“相同 seed、不同已消费 counter”问题。 | Candidate hook/concept | `RunRngSet.ToSerializable` 直接支持；RunReplays 自己用 checkpoint counter 诊断 divergence，见 [`RngCheckpointLogger.cs:8`](../../references/RunReplays-may-2026/RunReplays/Utils/RngCheckpointLogger.cs#L8)。 |
| **Unknown** | 最小战斗从原生 save 恢复时哪些 RNG/初始字段已由 `SerializableRun` 完整覆盖；战斗内 action 边界是否需要另存 combat RNG/手牌/敌人 AI state。 | — | 需要独立恢复并对比序列化 RNG 与首回合状态。 |

### 4. 回合、房间、楼层结构节点

| 等级 | 稳定身份/边界候选 | 用途标签 | 证据与限制 |
| --- | --- | --- | --- |
| **Confirmed** | 回合边界：`CombatManager.TurnStarted/TurnEnded(Action<CombatState>)`；`CombatState.RoundNumber` 可作回合编号。 | Candidate hook/concept | 目标 DLL 元数据；RunReplays 订阅见 [`CardPlayReplayPatch.cs:93`](../../references/RunReplays-may-2026/RunReplays/Patches/Replay/CardPlayReplayPatch.cs#L93)。 |
| **Confirmed** | 房间边界：`RunManager.RoomEntered/RoomExited`；当前房间为 `RunState.CurrentRoom`，房间类型为 `AbstractRoom.RoomType`。 | Candidate hook/concept | 目标 DLL 元数据；RunReplays 使用方向见 [`ReplayDispatcher.cs:821`](../../references/RunReplays-may-2026/RunReplays/ReplayDispatcher.cs#L821)。 |
| **Confirmed** | 结构身份字段：`RunState.CurrentActIndex`, `ActFloor`, `TotalFloor`, `CurrentMapCoord`, `RunLocation`, `CurrentRoomCount`, `CurrentMapPointHistoryEntry`；`RunLocation` 含 `MapLocation(actIndex, coord)` 和可空 `roomId`；`MapCoord` 含 `row/col`。 | Candidate hook/concept | 目标 DLL 元数据。 |
| **Inferred** | spike 可用 `(actIndex, map row, map col, roomId, totalFloor, combat round)` 作为诊断用复合位置，而不承诺这是最终格式的永久 ID。 | Candidate hook/concept | 这些字段直接存在；稳定性需运行验证。 |
| **Unknown** | 事件内战斗、返回原房间、胜利房间等 room stack 情况下 `roomId` / `CurrentRoomCount` 的稳定性；`TotalFloor` 在 `RoomEntered` 与 `RoomExited` 的增量时点。 | — | 不在 INV-001 扩展场景验证。 |

### 5. 状态序列化、装载与恢复

| 等级 | 入口与限制 | 用途标签 | 证据与限制 |
| --- | --- | --- | --- |
| **Confirmed** | 捕获候选：`RunManager.ToSave(AbstractRoom) -> SerializableRun`。 | Candidate hook/concept | 目标 DLL 元数据；RunReplays postfix 见 [`RunSaveLogger.cs:17`](../../references/RunReplays-may-2026/RunReplays/Patches/Record/RunSaveLogger.cs#L17)。 |
| **Confirmed** | 装载候选：`RunState.FromSerializable(save)` → await `SetUpSavedSingleplayer(state, save)` → await `NGame.LoadRun(state, save.PreFinishedRoom)`。 | Candidate hook/concept | 目标 DLL 元数据。固定源码方向正确但 API 名称/await 过时，见 [`RunReplayMenu.cs:562`](../../references/RunReplays-may-2026/RunReplays/RunReplayMenu.cs#L562)。 |
| **Confirmed** | RunReplays 在每次 save 时另复制 live `run.save`，并在加载 replay 前覆盖正常 save slot。 | Reject for spike | [`RunSaveLogger.cs:120`](../../references/RunReplays-may-2026/RunReplays/Patches/Record/RunSaveLogger.cs#L120) 与 [`RunReplayMenu.cs:530`](../../references/RunReplays-may-2026/RunReplays/RunReplayMenu.cs#L530)。这会改变外部状态且与 MVP 的退出隔离冲突。 |
| **Inferred** | 原生 `SerializableRun` 是建立最小 fixture 的首选候选，但只能当“游戏原生 run 边界恢复”，不能推断为任意 action 的完整快照。 | Candidate hook/concept | `SerializableRun` 字段广，但目标 DLL 符号不能证明它保存战斗中所有 transient state。 |
| **Unknown** | `ToSave(CurrentRoom)` 是否可在首回合输入就绪点安全调用，以及恢复后是否返回同一手牌、敌人、RNG counters 和 UI readiness。 | — | 这是本报告建议 SPIKE-001 最先验证的阻塞点。 |

### 6. UI / game readiness

| 等级 | 信号候选 | 用途标签 | 证据与限制 |
| --- | --- | --- | --- |
| **Confirmed** | 战斗存在/终止：`CombatManager.IsInProgress`, `IsOverOrEnding`。 | Candidate hook/concept | 目标 DLL public properties。 |
| **Confirmed** | 玩家可操作阶段候选：`Player.PlayerCombatState.Phase`，枚举含 `None/Start/AutoPrePlay/Play/AutoPostPlay/End`；`CombatManager.IsPartOfPlayerTurn(player)`。 | Candidate hook/concept | 目标 DLL 元数据。它们是 v0.111 对已删除 `IsPlayPhase` 的候选替代，不等于已验证等价。 |
| **Confirmed** | 阻塞候选：`CombatManager.PlayerActionsDisabled`, `CombatManager.IsPaused`, `ActionExecutor.IsRunning/IsPaused/CurrentlyRunningAction`；状态变化事件 `PlayerActionsDisabledChanged`; turn events。 | Candidate hook/concept | 目标 DLL 元数据。 |
| **Confirmed** | 操作本身的最终守门：`CardModel.IsPlayable`, `CanPlay`, `CanPlayTargeting` 或 `TryManualPlay` 返回值。 | Candidate hook/concept | 目标 DLL 元数据；RunReplays dispatch 以 `TryManualPlay` bool 决定 consume/retry，见 [`PlayCardCommand.cs:68`](../../references/RunReplays-may-2026/RunReplays/Commands/PlayCardCommand.cs#L68)。 |
| **Reference only** | RunReplays 还用 3 个 quiet frame、多个 in-flight flag、定时 retry 和 5 秒 watchdog。 | Reference only / Reject for spike | [`CardPlayReplayPatch.cs:331`](../../references/RunReplays-may-2026/RunReplays/Patches/Replay/CardPlayReplayPatch.cs#L331) 与 [`CardPlayReplayPatch.cs:520`](../../references/RunReplays-may-2026/RunReplays/Patches/Replay/CardPlayReplayPatch.cs#L520)。这些是经验性机制，不能当作 v0.111 readiness 事实。 |
| **Inferred** | 最小验证 gate 可先组合：`IsInProgress && !IsOverOrEnding && !IsPaused && !PlayerActionsDisabled && IsPartOfPlayerTurn(player) && player.PlayerCombatState.Phase == Play && !executor.IsRunning`，最后用 `TryManualPlay` 结果判定。 | Candidate hook/concept | 所有单个符号已确认；组合正确性未确认。 |
| **Unknown** | 上述 gate 是否覆盖抽牌动画、异步选择器、relic/power hook、加载画面；quiet frame 是否必要。 | — | 留给 SPIKE-001 的失败观测，不扩展到 TIME-001。 |

### 7. 录制 / dispatch 热路径风险

下表的“代码存在”是 **Confirmed**；对 hitch 的实际贡献均只标 **Inferred performance risk**，没有写成根因。

| 代码证据 | 风险分类 | 等级 | SPIKE-001 用途 |
| --- | --- | --- | --- |
| `DiagnosticLog.Write` 在全局 `_lock` 内用 `StreamWriter(AutoFlush=true)` 每行写盘；见 [`DiagnosticLog.cs:30`](../../references/RunReplays-may-2026/RunReplays/Utils/DiagnosticLog.cs#L30)、[`DiagnosticLog.cs:60`](../../references/RunReplays-may-2026/RunReplays/Utils/DiagnosticLog.cs#L60)、[`DiagnosticLog.cs:72`](../../references/RunReplays-may-2026/RunReplays/Utils/DiagnosticLog.cs#L72)。 | 同步 I/O + lock | **Confirmed code / Inferred performance risk** | Reject for spike：正常热路径只保留内存计数或批量诊断。 |
| `PlayerActionBuffer.Record` / `RecordCardPlayEarly` 每个语义动作调用 `DiagnosticLog.Write`，见 [`PlayerActionBuffer.cs:277`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L277) 与 [`PlayerActionBuffer.cs:442`](../../references/RunReplays-may-2026/RunReplays/PlayerActionBuffer.cs#L442)。 | 热路径同步日志 | **Confirmed code / Inferred performance risk** | Reject for spike。 |
| `ReplayDispatcher.TryDispatch` 每次调用写诊断、重算 dispatchable types；`ExecuteNext` 再次重算并多次写日志，见 [`ReplayDispatcher.cs:899`](../../references/RunReplays-may-2026/RunReplays/ReplayDispatcher.cs#L899) 和 [`ReplayDispatcher.cs:968`](../../references/RunReplays-may-2026/RunReplays/ReplayDispatcher.cs#L968)。 | 日志 + 重复状态扫描 | **Confirmed code / Inferred performance risk** | 只借鉴状态机概念，不加载整套 dispatcher。 |
| 固定 0.5 秒 polling 调用 `GetDispatchableTypes`; 变化时构造 `GameStateSnapshot`、序列化 JSON、枚举所有可用命令，见 [`ReplayDispatcher.cs:540`](../../references/RunReplays-may-2026/RunReplays/ReplayDispatcher.cs#L540)。 | 周期轮询 + 序列化 + 全量遍历 | **Confirmed code / Inferred performance risk** | Reject for spike。 |
| `GameStateSnapshot` 遍历 deck、hand、draw/discard/exhaust、powers、enemies、potions、relics，见 [`GameStateSnapshot.cs:59`](../../references/RunReplays-may-2026/RunReplays/GameStateSnapshot.cs#L59)。 | 全量遍历/分配 | **Confirmed code / Inferred performance risk** | 最小校验只取白名单字段。 |
| `GetAvailableCommands` 遍历手牌、活敌并逐一调用 `CanPlayTargeting`，见 [`ReplayDispatcher.cs:101`](../../references/RunReplays-may-2026/RunReplays/ReplayDispatcher.cs#L101)。 | 组合遍历/游戏逻辑调用 | **Confirmed code / Inferred performance risk** | Reject for spike。 |
| 每次 `RunManager.ToSave` 都构造全 action snapshot、`File.WriteAllText` 完整覆盖 replay、再 `File.Copy` save，见 [`RunSaveLogger.cs:46`](../../references/RunReplays-may-2026/RunReplays/Patches/Record/RunSaveLogger.cs#L46)。 | 全量序列化 + 同步完整写/复制 | **Confirmed code / Inferred performance risk** | Reject for spike；只在明确 fixture 边界执行一次。 |
| 多个非战斗 command 使用 `FindChildren("*")`、`GetChildren()` 和逐节点反射，例如 [`ClaimRewardCommand.cs:107`](../../references/RunReplays-may-2026/RunReplays/Commands/ClaimRewardCommand.cs#L107)。 | UI 全树查找 + 反射 | **Confirmed code / Inferred performance risk** | 超出最小战斗场景，Reject for spike。 |
| `RunManager.State`、`MoveToMapCoordAction._destination` 等反射在源码中多为 static cached `PropertyInfo/FieldInfo`。 | 反射 | **Confirmed code / Unknown measurable cost** | v0.111 `RunManager.State` 已是 public；探针直接访问，避免无必要反射。 |

### 8. v0.111 API / RNG / UI break

详见下一节 break 表。总体结果：**Confirmed** 的 API break 已足以解释“整体 RunReplays 不能直接作为 v0.111 spike 基底”，但**不能**据此宣称已找到历史运行失败的首个异常；现有历史日志仍未包含那个异常。

## v0.111 break 表

| 旧符号/做法（固定源码） | v0.111 目标证据 | 等级 | 对最小探针的影响 / 标签 |
| --- | --- | --- | --- |
| `RunManager.SetUpSavedSinglePlayer`，并以同步语句调用 | 目标 DLL 只有 `Task SetUpSavedSingleplayer(RunState state, SerializableRun save)`（小写 `p`，返回 `Task`） | **Confirmed** | 旧 patch target 与调用均不可原样使用；恢复链必须按 v0.111 验证并 await。Reject for spike（旧实现），新符号为 Candidate。 |
| `CombatManager.IsPlayPhase` | 目标 DLL 属性查询返回 missing；`PlayerCombatState.Phase` 与 `PlayerTurnPhase.Play` 存在 | **Confirmed** | RunReplays `IsCombatReady` 和 `GetDispatchableTypes` 不能原样移植。新 gate 只作 Candidate，必须运行验证。 |
| `Rng.Seed`, `Rng.Counter` | 目标 DLL 的 `Rng` 没有这两个 public property；有 `ToSerializable()`，返回 `SerializableRng(counter,state0..3)` | **Confirmed** | RNG tracker/checkpoint 源码不可原样编译/绑定；SPIKE 记录 serializable state，不 patch 每次 `Next*`。 |
| `RunState.FromSerializable(SerializableRun)` | 目标 DLL 同签名存在 | **Confirmed no break** | 可作为恢复候选。 |
| `CardModel.EnqueueManualPlay(Creature)`、`TryManualPlay(Creature)` | 目标 DLL 同签名存在 | **Confirmed no break** | 最小 capture/dispatch 候选未被该版本变化阻塞。 |
| `ActionExecutor(ActionQueueSet)` 与 Before/After events | 目标 DLL 同构造器与事件存在 | **Confirmed no break** | 最小 action 完成观察候选未被阻塞。 |
| `PlayerCmd.EndTurn(player, canBackOut:false)` | 目标 DLL 为三参，第三参 `Func<Task>` optional | **Confirmed compatible call shape** | 两参调用仍可绑定；继续用 turn events 验证完成语义。 |
| `NGame.StartNewSingleplayerRun` 的 gameMode/ascension 参数 | 目标 DLL 签名为 character, shouldSave, acts, modifiers, seed, gameMode, optional ascension, optional dailyTime | **Confirmed** | 固定源码的 7 参数调用形状仍可用；不代表其 act/unlock 改写可用。 |
| 主菜单通过 `%MainMenuTextButtons`、`MainMenuTextButtons/QuitButton` 等固定 NodePath 克隆/插入 UI，见 [`MainMenuButtonInjector.cs:72`](../../references/RunReplays-may-2026/RunReplays/MainMenuButtonInjector.cs#L72) | 目标 DLL 只能确认 `NMainMenu._Ready`, `MainMenuButtons`, `SubmenuStack` 等类型符号；scene NodePath 不存在于可审计的 DLL 类型签名中。既有 v0.111 运行证据确认 UI 能打开、但 replay 未进入 dispatch，见 [`INVESTIGATIONS.md:102`](../INVESTIGATIONS.md#L102)。 | **Inferred / exact break Unknown** | 整套 RunReplays UI 为 Reject for spike。不能声称已定位具体 hierarchy break；若未来 UI 任务需要，应单独在运行 scene tree 中取证。 |
| v0.111 历史会话在 `LoadRunSave` 后停止 | 现有日志没有首个异常；同尺寸/hash 身份链只能把边界收窄到 UI/载入之后 | **Unknown exact cause** | 不继续 D4–I6；SPIKE 用自有诊断点分别包围 parse/load/setup/LoadRun/readiness/dispatch。 |

## SPIKE-001 候选方案（仅一个最小场景）

### 场景

固定 v0.111 build、单人 Standard、Ascension 0、固定角色 ModelId 和固定 seed。使用一个**首章第一场弱怪、玩家首回合已经进入 `PlayerTurnPhase.Play` 的原生状态 fixture**。录制恰好两条语义操作：

1. 打出一张不打开额外选择界面的卡（优先无目标卡；若只有目标卡则记录单个敌人 target）。
2. End Turn。

若 v0.111 原生 `SerializableRun` 不能在该输入就绪点恢复同一战斗，则立即以“恢复边界阻塞”结束该 spike 分支，不扩展为自制快照系统；可退到同 seed、同前置选择的重启路径作为单独后续决策。

### 建议 hook / 调用方向

| 阶段 | 最小入口 | 记录/动作 |
| --- | --- | --- |
| fixture | `RunManager.ToSave(CurrentRoom)` 与 `RunState.Rng.ToSerializable()`（先验证可调用性） | 保存 `SerializableRun`、精确 build、角色/seed、完整 `SerializableRunRngSet`；不覆盖 live save slot。 |
| capture | postfix `CardModel.EnqueueManualPlay(Creature)` | 记录 action index、结构位置、card ModelId、hand index、combat card ID、optional target CombatId、操作前 RNG digest。 |
| completion | `ActionExecutor.AfterActionExecuted` 过滤 `PlayCardAction` / `EndPlayerTurnAction` | 记录完成或异常、操作后校验摘要；不记录所有展开 sub-action。 |
| End Turn capture | `AfterActionExecuted(EndPlayerTurnAction)`，必要时另在 `PlayerCmd.EndTurn` 语义入口作只读对照 | 只形成一个 `EndTurn` command。 |
| restore | `RunState.FromSerializable` → await `SetUpSavedSingleplayer` → await `NGame.LoadRun` | 每一步单独记录开始/成功/异常；不复制到普通 save 路径。 |
| readiness | 组合 v0.111 候选 gate，最后调用 `TryManualPlay` | 不使用已删除 `IsPlayPhase`，不使用 UI 节点点击。 |
| dispatch | `NetCombatCardDb.GetCard/TryGetCard` → `TryManualPlay(target)`；`PlayerCmd.EndTurn` | 每条只 dispatch 一次；失败时记录 gate、解析键、返回值，不用无限 retry/watchdog 掩盖首个阻塞。 |

### 最小记录字段

- build：game version、commit、`sts2.dll` hash、probe schema/version。
- initial：character ModelId、seed、game mode、ascension、`SerializableRun` fixture identity/hash、`SerializableRunRngSet`。
- position：`actIndex`, map `row/col`, `roomId`, `totalFloor`, combat round, semantic action index。
- play card：stable ModelId、upgraded/variant 标记（若可取）、hand index、combat card ID（作为待验证解析键）、optional target identity。
- timing：本 spike 只记单调时间用于诊断，不验证产品“原始节奏”。
- outcome：dispatch return、对应 action 完成、异常、pre/post fingerprint、RNG digest。

### 最小校验状态

为避免复刻 `GameStateSnapshot` 全量遍历，首个 probe 只比较：

- 结构位置与 combat round。
- 玩家 HP、energy、hand 中稳定 card ModelId 顺序、draw/discard/exhaust 的 ModelId 多重集。
- 敌人稳定 ModelId/CombatId（分别记录）、HP、存活状态。
- `SerializableRunRngSet` 每个已存在 RNG 的 serialized counter/state digest。
- End Turn 后是否观察到 `TurnEnded`，以及下一玩家 `TurnStarted` 或 combat ended。

该白名单只是 spike 校验，不是 MVP 权威状态方案。

### 失败观测点

1. fixture 创建：`ToSave` 是否异常/缺失战斗 transient state。
2. restore：`FromSerializable`、`SetUpSavedSingleplayer`、`LoadRun` 中第一个失败步骤。
3. restored readiness：目标位置、RNG digest、手牌/敌人是否与录制起点相同。
4. card resolution：ModelId/hand index/combat ID 哪一种首先不一致。
5. dispatch gate：哪个 readiness 条件阻塞；`TryManualPlay` 返回 false 还是抛异常。
6. completion：是否收到匹配的 `PlayCardAction`；其后 action executor 是否归于 idle。
7. end turn：是否收到 `TurnEnded` / 下一 `TurnStarted`，或 combat 已结束。
8. divergence：第一个不同的白名单字段和对应 RNG digest；不自动纠偏。

## 推断与假设

1. **Inferred**：`CardModel.EnqueueManualPlay` postfix 是比 UI click 更稳定、比任意 `GameAction.RecordableToReplay` 更接近玩家出牌语义的最小捕获点。验证方法：在 v0.111 对成功/失败/取消出牌各执行一次，确认只有成功接受的手动出牌产生一次记录。
2. **Inferred**：`PlayerCombatState.Phase == Play` + `IsPartOfPlayerTurn` 是 `IsPlayPhase` 的最接近替代，但仍需联合 disabled/paused/executor 状态。验证方法：在抽牌、可操作、卡牌结算和回合切换四个时点打印全部信号。
3. **Inferred**：完整 `RunRngSet.ToSerializable()` 足以比单 seed 更早发现 deterministic divergence。验证方法：相同 fixture 恢复两次，在每条语义操作前后比较所有 serialized RNG entries。
4. **Inferred performance risk**：RunReplays 的 always-on AutoFlush 日志和 save 时全量同步写盘可能放大历史 hitch。确认根因需要 PERF-001 对照；INV-001 没有运行 profile，不将其写成根因。
5. **Inferred**：当前可见 API break 足以拒绝“直接加载整个 RunReplays 做 spike”，但不足以说明历史 v0.111 首个失败恰好是哪一项，因为没有相同 artifact/build 下的首个异常 stack。

## 仍然未知

1. v0.111 原生 `SerializableRun` 能否在首回合输入就绪点恢复完全相同的 combat transient state；这是**一个最值得继续验证的阻塞点**。
2. `NetCombatCardDb` card ID 与 creature CombatId 是否在独立恢复后稳定。
3. run/room/turn 事件的精确顺序、重复触发和特殊 room stack 行为。
4. readiness 候选组合是否充分；是否必须等待 quiet frame、动画或 selector stack。
5. seed + SerializableRunRngSet + 原生 save 是否覆盖该最小战斗的全部确定性输入。
6. v0.111 历史 RunReplays 失败的首个异常和具体 UI hierarchy break；本任务按 handoff 不恢复 D4–I6。
7. 任何完整场景覆盖、最终快照密度、导航、时序体验和性能预算；它们属于后续 waves。

## 做了什么

- 按要求依次阅读 `CURRENT_STATUS.md`、`MVP.md`、`EXPERIMENT_PLAN.md`、`handoffs/INV-001.md`、`HANDOFF_TEMPLATE.md`。
- 核对固定源码 archive、两份 manifest、Workshop artifact、ProductVersion、目标 `release_info.json` 与 DLL hash。
- 只按八类问题查阅 RunReplays 的 recorder、command、dispatcher、RNG、save/restore 和 readiness 相关文件。
- 用只读 .NET reflection 检查 v0.111 目标 DLL 类型、方法、参数、属性、事件；没有修改游戏目录。
- 将源码做法分为 Candidate / Reference only / Reject for spike，并形成一个最小场景。

## 变更

- 修改文件：仅新增 `project-management/investigation-reports/INV-001-report.md`。
- 行为变化：无。
- 是否引入兼容性或性能风险：无运行时代码、无项目实现、无游戏文件修改。

## 验证

### 执行命令/步骤

- `Get-FileHash -Algorithm SHA256`：目标 DLL、RunReplays archive、Workshop RunReplays/BaseLib DLL。
- `Get-Item` / `VersionInfo`：目标 DLL 大小、Workshop ProductVersion。
- `rg -n` 与带行号 `Get-Content`：固定源码目标符号、I/O、reflection、serialization、UI node lookup。
- `[Reflection.Assembly]::LoadFrom(...); GetType/GetMethods/GetProperties/GetEvents/GetParameters`：v0.111 符号核对。
- 检查报告以外的受保护项目文件未被编辑。

### 实际结果

- 目标 build/hash 与 handoff 一致。
- 八类入口均有 Confirmed/Inferred/Unknown 结论。
- 已确认一个 v0.111 最小 capture 入口与一个 dispatch 候选。
- 已确认三项直接影响最小链路的 API/RNG break：saved setup、combat play phase、per-RNG properties。
- 性能项均保持为代码风险/假设，没有宣称根因。

### 未执行项与原因

- 未启动游戏、未运行 replay：本任务是定向静态调查，不是 SPIKE-001。
- 未反编译方法体：本机无 `ilspycmd/ildasm`；公开/非公开元数据签名已足够回答入口与 break，方法行为保留为 Unknown。
- 未切换 v0.107、未复现历史 sample、未修复 RunReplays：明确非目标。

## 偏离任务范围

无。

## 停止条件检查

| handoff 停止条件 | 结果 |
| --- | --- |
| 八类入口均有 `Confirmed/Inferred/Unknown`，无空白类别 | **满足** |
| 每个关键判断可追溯到固定版本证据 | **满足**：固定源码文件/行、目标 hash 和反射命令/符号均已记录 |
| 至少一个 v0.111 capture 入口和一个 dispatch 候选，或证明阻塞 | **满足**：`EnqueueManualPlay` / action events；`GetCard + TryManualPlay` / `PlayerCmd.EndTurn` |
| 性能风险未误写为已证实根因 | **满足**：全部标为 Inferred performance risk |
| `SPIKE-001` 足够具体，可由另一名 Implementation Codex 独立执行 | **满足**：唯一场景、hook、字段、校验和失败观测点已列明 |

**全部停止条件均满足；INV-001 到此停止。**

## 建议下一步

执行边界明确的 `SPIKE-001` 第一段：只验证同一 v0.111 hash 下，首回合输入就绪点的 `SerializableRun` fixture 能否独立恢复到相同结构位置、手牌/敌人摘要和完整 RNG digest。只有恢复基线一致后，才加入一张普通卡与 End Turn 的 capture/dispatch；失败则报告首个 restore/readiness 阻塞，不扩展为正式 recorder/player。

# INV-002 Investigation Report

## 任务

- Task ID：INV-002
- 任务范围：对 STS2 public-beta v0.111.0 的正常 Continue、RunState 生命周期、普通 run save 写入、room/combat 重建与可用隔离入口做静态、只读调查。
- 明确非目标：未启动游戏；未运行 setup/restore/load-save/replay；未触碰游戏存档；未修改 SPIKE-001A；未实现 capture、restore、dispatch、seek 或 replay。
- 状态：**Complete**（仅指 INV-002 的静态调查已回答 handoff 问题；SPIKE-001B 仍为 BLOCKED，本文不宣布解锁。）

## 环境与证据来源

### 固定目标身份

- 游戏安装：`C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`
- `release_info.json`：version `v0.111.0`，branch `v0.111.0`，commit `41cef1ea`，date `2026-08-13T17:39:18-07:00`
- 目标程序集：`C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll`
- 文件大小：9,757,184 bytes
- ProductVersion：`0.1.0+41cef1ea4657c524aa50e870df009e56337e8c32`
- SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`

该版本与 INV-002 handoff 固定目标完全一致，**未触发 environment drift 停止条件**。下文所有方法体/调用点结论只绑定此程序集与此 SHA-256。

### 工具与证据

- OS：Windows NT `10.0.26200.0`
- PowerShell：7.6.4
- .NET SDK：8.0.411
- IL reader：Mono.Cecil 0.11.5.0（本机已安装 Rider 附带版本）
- Mono.Cecil 路径：`C:\Program Files\JetBrains\Rider\r2r\2025.1.3R\CB7459855B94C993D768417A5CF3FF0\NetCore\Mono.Cecil.dll`
- Mono.Cecil SHA-256：`9C2908709DA6761E9B5B9D4D46102D65851145BAC987787D6C5A05FFE5689487`
- 窄范围查询工具：`evidence/inv-002/Invoke-NarrowIlQuery.ps1`；它在读程序集前强制校验固定 SHA-256。
- 既有运行证据（未在本任务重跑）：`evidence/spike-001a/runtime/fixture.run.json`、`fixture.expected.json`、`restore-attempt-04.json` 与 `SPIKE-001A-report.md`。

未安装任何额外工具或依赖，未导出完整反编译源码树。关键命令及复现方式见 `evidence/inv-002/README.md`。典型查询为：

```powershell
& 'evidence/inv-002/Invoke-NarrowIlQuery.ps1' -Mode Disassemble -Pattern 'RunManager/<SetUpSavedSingleplayer>d__171::MoveNext'
& 'evidence/inv-002/Invoke-NarrowIlQuery.ps1' -Mode FindCalls -Pattern 'SetUpSavedSingleplayer|LoadRun|CleanUp'
```

## 1. 正常主菜单 Continue 的实际顺序

以下均为 **Confirmed method body/call site**：

1. `NMainMenu._Ready()` 调用 `NMainMenu.RefreshButtons()`。
2. `RefreshButtons()` 检查 `SaveManager.HasRunSave`，存在时调用 `SaveManager.LoadRunSave()`；后者转发到 `RunSaveManager.LoadRunSave()`，经 migration manager 从 `CurrentRunSavePath` 读取 `SerializableRun`。`CurrentRunSavePath` 由当前 profile id 与字面量 `current_run.save` 组成。读取结果缓存于 `_readRunSaveResult`。
3. `NMainMenu.OnContinueButtonPressed()` 检查缓存结果成功且 `SaveData != null`，进入异步 `OnContinueButtonPressedAsync`。
4. `NMainMenu/<OnContinueButtonPressedAsync>d__64.MoveNext()`：禁用 Continue、停止菜单音乐；用缓存的 `SerializableRun` 调 `RunState.FromSerializable(save)`；然后 `await RunManager.SetUpSavedSingleplayer(runState, save)`。
5. setup 返回后，菜单做 FadeOut；随后 `ReactionContainer.InitializeNetworking(new NetSingleplayerGameService())`，再 `await NGame.LoadRun(runState, save.PreFinishedRoom)`。
6. `NGame/<LoadRun>d__155.MoveNext()`：预载角色/act 资源；`RunManager.Launch()`；`RootSceneContainer.SetCurrentScene(NRun.Create(runState))` 完成 run UI 场景切换；生成 map；`AbstractRoom.FromSerializable(preFinishedRoom, runState)`；`RunManager.LoadIntoLatestMapCoord(room)`；最后恢复 map drawings 并清除 pending 状态。
7. `LoadIntoLatestMapCoord()` 在已有 visited coord 时调用 `EnterMapCoordInternal(coord, preFinishedRoom, saveGame:false)`；最终 `EnterMapPointInternal` 使用传入的 pre-finished room，或在其为 null 时创建/roll 新房间。
8. 外层 setup/load 异常处理会调用 `RunManager.CleanUp(true)` 并显示 load error。

顺序要点：**反序列化 RunState → setup（其中先发生 reload-count 写盘）→ FadeOut → networking → LoadRun → Launch → NRun UI 场景 → map/room 重建**。所以 setup 并不是无副作用的“纯状态安装”，UI 切换也不发生在 setup 之前。

影响：状态等价取决于后续 room 重建而非只看 `RunState.FromSerializable`；普通存档隔离必须在调用 setup 前解决；该链值得作为后续对照基线，但不能原样用于普通 profile 的实验。

## 2. `SetUpSavedSingleplayer` 为何写普通 `current_run.save`

### 具体调用路径

**Confirmed method body/call site**：

```text
NMainMenu.OnContinueButtonPressedAsync
  -> RunManager.SetUpSavedSingleplayer(runState, save)
     -> SaveManager.IncrementNumReloads(save, NetGameType=1, forceInTest=false)
        -> RunSaveManager.SaveRun(SerializableRun save, isMultiplayer=false)
           -> ISaveStore.WriteFileAsync(CurrentRunSavePath, serializedBytes)
              CurrentRunSavePath = <current profile>/current_run.save
```

`NetSingleplayerGameService.get_Type()` 返回值 1；`NetGameTypeExtensions.IsMultiplayer()` 只把 2–3 视为 multiplayer，因此该路径选择 singleplayer 的 `current_run.save`。

`SetUpSavedSingleplayer` 的方法体先检查 `State`，再设置 `State`，随后立即 await `IncrementNumReloads`。之后它调用 `InitializeShared(..., shouldSave:true, ...)`、`InitializeRunLobby` 和 `InitializeSavedRun`。

### `ShouldSave=false` 覆盖与未覆盖范围

- **Confirmed method body/call site**：`RunSaveManager.SaveRun(AbstractRoom preFinishedRoom, ...)` 的 live-state 重载首先检查 `RunManager.ShouldSave`；false 时直接返回。它覆盖的是“从当前 live RunState/room 调 `RunManager.ToSave(...)` 再保存”的常规路径。
- **Confirmed method body/call site**：`IncrementNumReloads` 不读取 `RunManager.ShouldSave`。在正常（TestMode off）环境，它增加 `SerializableRun.NumReloads`，等待前一 save task，开 save batch，直接调用 `RunSaveManager.SaveRun(SerializableRun, ...)`。后一个重载也不检查 `ShouldSave`。
- **Confirmed method body/call site**：即使调用前外部把 `ShouldSave=false`，`SetUpSavedSingleplayer` 仍先走上述直接序列化写盘路径，随后 `InitializeShared` 又把运行上下文初始化为 `shouldSave:true`。
- **Confirmed method body/call site**：`forceInTest=false` 不是“不保存”；它仅表示在 TestMode on 时不强制执行该写盘。正常 TestMode off 仍写。
- **Unknown**：没有在所查程序集内找到 `SetUpSavedSingleplayer` 的参数、生产上下文开关或 sibling saved-singleplayer 入口能够跳过 reload-count 写入。

因此 SPIKE-001A 的普通 save 变化不是 `ShouldSave` 竞态的合理解释，而是绕过该 gate 的明确调用分支。既有 SPIKE 日志把变化时间定位在 setup 内；本任务的静态调用点解释了该观测，但本任务没有重新运行验证。

影响：该入口对普通存档隔离为负面；在未提供独立 profile/store 前，不值得直接进入新的普通 profile 恢复实验。

## 3. RunState 所有权、teardown 与 `State is already set.`

### 来源与触发条件

- **Confirmed method body/call site**：字符串 `State is already set.` 来自 `RunManager.SetUpNewSingleplayer`、`SetUpSavedSingleplayer`、saved/new multiplayer、`SetUpReplay` 与 `SetUpTest` 的入口 guard。触发条件就是 singleton `RunManager.State != null`；这些入口不支持覆盖现存 state。
- **Confirmed method body/call site**：`SetUpSavedSingleplayer` 在通过 guard 后立即把传入 `RunState` 赋给 singleton `RunManager.State`。`Launch()` 在稍后才设置 local net id、解除网络 buffering、发出 RunStarted；它不是 state 所有权的起点。

### 官方 teardown 边界

- **Confirmed method body/call site**：`RunManager.CleanUp(bool)` 是明确 teardown。若 `State == null` 则返回；否则先 `ShouldSave=false`、`IsCleaningUp=true`，重置 action/card-select、音频 loop、overlay/map/modal 与 `CombatManager`，处理 replay writer，并注销/Dispose action、choice、reward、rest、flavor、checksum、lobby/network 等同步器；最终清热键、清 cleaning flag、`LocalContext.NetId=null`、`State=null`、`DailyTime=null`。
- **Confirmed method body/call site**：正常 `NGame.ReturnToMainMenu` 的顺序是 FadeOut → 预载 common/main-menu assets → `RunManager.CleanUp(true)` → `NGame.LoadMainMenu(false)`。
- **Confirmed method body/call site**：`NRun._Notification(1006)`（scene tree exit 通知）调用 `CleanUp(false)`。此外 timeline、continue 异常路径也调用 CleanUp。
- **Confirmed method body/call site**：`RunManager.ExitCurrentRooms()` 循环调用 `ExitCurrentRoom()`；后者在每个 room 上依次执行 synchronizer 的 `BeforeLeavingRoom`、从 state pop room、await `AbstractRoom.Exit`、触发 `RoomExited`。它是 room 层级退出，不等同于清空 RunManager 全局 state。
- **Inferred**：SPIKE-001A 同进程第二次 setup 发生在旧 run 仍持有 `RunManager.State` 时，因而命中 guard；仅把 setup 重排到旧 NRun 仍活着的时点不能满足生命周期要求。
- **Unknown**：直接在活动战斗中单独调用 `CleanUp` 后立刻 setup，而不走 ReturnToMainMenu 的 fade/asset/scene teardown，是否会遗留 Godot node、事件或 UI 引用；静态方法体不能证明该非正常顺序安全。

结论：有官方 reset API，但有方法体证据支持的完整生产边界是 **ReturnToMainMenu/scene teardown 包围的 CleanUp**，不是任意时点的 `CleanUp → setup`。这解释异常，却不足以推荐同进程替换实验。

## 4. `PreFinishedRoom`、`ToSave(CurrentRoom)` 与 `LoadRun` 的语义边界

- **Confirmed method body/call site**：`RunManager.ToSave(AbstractRoom room)` 保存 run 级内容，包括 acts/maps、玩家持久 deck、relic bag、完整 `RunRngSet`、visited coords、map history、时间等。参数为 null 时写 `PreFinishedRoom=null`；非 null 时只写 `room.ToSerializable()`。
- **Confirmed method body/call site**：`CombatRoom.ToSerializable()` 只记录 room/encounter id、`IsPreFinished`、reward proportion、parent-event 信息、encounter custom state 和额外奖励。它不保存手牌、抽/弃/消耗堆、combat turn、action queue 或完整 creature/combat state。
- **Confirmed method body/call site**：进入新 map point 的 `EnterMapPointInternal` 在创建房间前、且 `saveGame=true` 时调用 `SaveManager.SaveRun(null, true)`。这是官方“房间前”结构性 checkpoint。
- **Confirmed method body/call site**：`CombatManager.EndCombatInternal` 先 `room.MarkPreFinished()`，再 `SaveManager.SaveRun(room, saveProgress:false)`。这是官方 non-null `PreFinishedRoom` 的明确生产写点，语义是已结束/预完成房间，以便加载结果/奖励流程。
- **Confirmed method body/call site**：`AbstractRoom.FromSerializable` 对 combat room 调 `CombatRoom.FromSerializable`，后者重新创建 encounter 与 `CombatRoom`，只恢复上述薄信息；`NGame.LoadRun` 再把它送入最新 map coord。
- **Inferred**：`ToSave(CurrentRoom)` 在中途战斗机械上可产生 `IsPreFinished=false` 的 room descriptor，但这不是中途战斗快照。SPIKE fixture 正是 `room_type=monster`、`is_pre_finished=false`，且缺少所有瞬时 combat pile/turn 字段。将它加载会启动一次新战斗，而非恢复捕获帧。

所以三个边界分别是：`PreFinishedRoom=null` = 房间前 checkpoint；官方 non-null 且 pre-finished = 房间结果 checkpoint；中途 `ToSave(CurrentRoom)` = run 级状态加薄 room 描述，**不具备中途状态等价语义**。

## 5. combat reconstruction 与 14 项 diff

### 静态调用链

以下是 **Confirmed method body/call site**：

```text
NGame.LoadRun
  -> AbstractRoom.FromSerializable
     -> CombatRoom.FromSerializable
  -> RunManager.LoadIntoLatestMapCoord
     -> EnterMapCoordInternal / EnterMapPointInternal
        -> CombatRoom.EnterInternal
           -> (IsPreFinished ? StartPreFinishedCombat : StartCombat)
              -> CombatState.CreateCreature
                 -> Creature.SetUniqueMonsterHpValue(..., runState.Rng.Niche)
              -> CombatManager.SetUpCombat
                 -> Player.PopulateCombatState(runState.Rng.Shuffle, combatState)
                    -> clone persistent deck into draw pile
                    -> CardPile.RandomizeOrderInternal
                       -> ListExtensions.UnstableShuffle
                          -> Rng.NextInt once per Fisher-Yates iteration
              -> CombatManager.AfterCombatRoomLoaded
                 -> turn loop -> StartTurn -> SetupPlayerTurn
                    -> CardPileCmd.Draw(opening handDraw, ..., true)
```

具体 RNG 证据：

- `CombatRoom.EnterInternal` 对 `IsPreFinished=false` 走 `StartCombat`，不是 `StartPreFinishedCombat`。
- `CombatState.CreateCreature` 给 enemy 使用 `runState.Rng.Niche`；`Creature.SetUniqueMonsterHpValue` 通过 `NextItem`/`NextInt` 取 HP 值。单只敌人会消费一次 Niche。
- `Player.PopulateCombatState` 从持久 deck 克隆新 draw pile，然后用 `Shuffle` RNG 调 `RandomizeOrderInternal`。
- `ListExtensions.UnstableShuffle` 对 n 张牌执行 n-1 次 `Rng.NextInt`。fixture 的 deck 为 10 张，故重建时再消费 9 次 Shuffle。
- `SetupPlayerTurn` 计算 opening `handDraw` 并调用 `CardPileCmd.Draw`，从新洗出的 pile 抽手牌。

### 与 SPIKE-001A 观测的对应

- **既有运行证据**：fixture expected 为 Niche counter 1、Shuffle counter 9；跨重启实际为 2 与 18；其余 RNG entries 未报告 counter 差异。fixture 是单敌人、10 张初始牌组。
- **Inferred（由方法体与既有观测精确吻合）**：恢复把捕获时已经完成一次建怪/洗牌后的 RNG 状态再次送入 fresh combat setup，因此额外消费 Niche +1、Shuffle +9。随后 opening draw 基于第二次洗牌，解释手牌两项、draw-pile 一项及两个 RNG entry 的 counter/state/digest 共 14 项 diff。
- **Unknown**：本任务没有新加逐调用 RNG trace，因此不能把既有运行时每一次 counter 增量直接归因到某一动态 invocation；上述是强静态因果解释，而不是本任务新运行验证。

该边界直接否定“中途 `ToSave(CurrentRoom)` 经 LoadRun 可获得状态等价”的假设，且使房间前 checkpoint 成为唯一有原生语义支持、值得下一次最小运行验证的候选。

## 6. 隔离上下文、存储层与 `shouldSave=false` 候选

| 候选 | 分类与方法体证据 | 适用性结论 |
|---|---|---|
| Profile-scoped run path | **Confirmed method body/call site**：`RunSaveManager.CurrentRunSavePath` 读取 `IProfileIdProvider.CurrentProfileId`，再组成 `current_run.save`；另有 `StaticProfileIdProvider` 构造重载。 | 支持“不同 profile 路径”的静态事实；但生产 active-profile 切换及 Steam cloud 是否完全不影响原 profile，**Unknown**，需运行前安全预检。 |
| `ISaveStore` abstraction | **Confirmed method body/call site**：`SaveManager`/`RunSaveManager` 构造器接受 `ISaveStore`；生产 `ConstructDefault` 使用 `GodotFileIo`，Steam 初始化时再包 `CloudSaveStore`。 | 存储可替换是事实；未找到生产的 non-persistent provider。不能仅凭接口宣布已有安全隔离入口。 |
| `MockGodotFileIo` / `SaveManager.MockInstanceForTesting` | **Confirmed method body/call site**：TestMode default 构造使用内存字典型 `MockGodotFileIo("user://test")`；`MockInstanceForTesting` 只是替换全局静态 `_mockInstance`；程序集内未找到其调用者。 | 测试专用、全局替换；没有 production restore caller 证据，不推荐把它当官方玩家运行上下文。 |
| `SetUpNewSingleplayer(..., shouldSave, ...)` | **Confirmed method body/call site**：该参数进入 `InitializeShared`；然后 `InitializeNewRun()`、`GenerateRooms()`。 | 是 shouldSave=false 的正确“新 run”创建路径，但不是 saved-state continue，也不执行 `InitializeSavedRun`；不解决本任务 restore 语义。 |
| `SetUpTest(..., shouldSave, ...)` | **Confirmed method body/call site**：参数进入 `InitializeShared`，随后 test-specific lobby、新 run 初始化；所查程序集无外部 caller。 | 测试入口，不是正常 Continue，且不加载 saved run；不推荐。 |
| `SetUpReplay` / `NetReplayGameService` | **Confirmed method body/call site**：不调用 `IncrementNumReloads`，但以 `shouldSave=true` 初始化；使用 `CombatReplay.serializableRun`、replay net service（type 4）及 `InitializeSavedRun`；唯一 caller 是 debug `NMultiplayerTest.RunReplay`。 | 它是 combat replay/debug 上下文，不是通用 saved-singleplayer 隔离入口；也无普通 save 全程不写的证明。不能假设 RunReplays 架构正确。 |
| Multiplayer/spectate/tutorial | **Unknown / out of scope**：未发现可直接证明适用于单机任意 run 恢复且非持久的生产入口。 | 符号名不足以支持实验，不扩展调查。 |

综合结论：程序集已有“路径按 profile 分段”和“存储层可注入”的构件，但**没有查到同时满足正常 saved-state 语义、明确 non-persistent、且有 production caller 的现成入口**。

## 7. 结论分类及决策影响

| 重要结论 | 分类 | 状态等价 | 普通存档隔离 | 是否进入下一运行实验 |
|---|---|---|---|---|
| Continue 的 setup 早于 UI/room load，且 setup 直接增加 reload count 并保存 | Confirmed method body/call site | setup 不是纯安装 | 原入口不安全 | 仅可在已证明隔离的 profile 中作对照 |
| `ShouldSave` 只 gate live-room save，不 gate `SerializableRun` direct save | Confirmed method body/call site | 无直接改善 | 外部 false 无效 | 不再测试同一技巧 |
| setup guard 要求 `RunManager.State == null`；CleanUp 最终清空 state | Confirmed method body/call site | 正确 teardown 是必要条件 | Cleanup 本身不提供 save 隔离 | 不选“只换调用顺序”为下一候选 |
| 中途 `CombatRoom.ToSerializable` 不含 piles/turn/combat state | Confirmed method body/call site | 无法中途等价 | 无直接影响 | 否决现有中途 fixture restore 架构 |
| 官方 room-before save 使用 `PreFinishedRoom=null` | Confirmed method body/call site | 与 fresh room reconstruction 语义相容 | 仍需隔离 profile | **唯一推荐候选** |
| Niche +1、Shuffle +9 来自第二次建怪与 10-card shuffle | Inferred，受已查方法体与既有运行证据共同支持 | 解释 14 diff | 无直接影响 | 用下一实验证伪/确认边界 |
| 单独 `CleanUp → setup` 是否 scene-safe | Unknown | 可能遗留节点/事件 | 未解决 | 不推荐 |
| 独立 profile + cloud 的端到端隔离是否可靠 | Unknown | 无直接影响 | 最大安全 Unknown | 作为下一实验的硬性前置检查 |

## 尚未解决的问题

1. **最大 Unknown**：在生产 Steam/cloud 环境中，切换到明确的 disposable profile 后，正常 Continue 的所有写入是否严格限定在该 profile，而不会触碰原 profile 的 `current_run.save` 或触发跨 profile cloud 副作用。静态路径支持 profile 分段，但不能证明完整运行时行为。
2. room-before checkpoint 经完整正常 Continue 后，首个可操作 combat snapshot 是否与不重启的 baseline 完全一致；静态语义支持，尚未运行验证。
3. `CleanUp` 脱离 ReturnToMainMenu/scene teardown 单独使用是否安全；没有必要冒险回答，因为它不是推荐候选。
4. replay/test storage 构件能否组合成 production-safe isolated restore；当前无 caller 证据，且组合它们会扩成新架构工作，不在 INV-002 内。

## 唯一推荐的下一步实验

**候选：房间前原生 checkpoint 的正常 Continue 等价性，一次性 A/B 实验；不做中途 restore，也不实现快速重演架构。**

### 假设与边界

在一个新建、无真实进度的 disposable 官方 profile 中，让游戏自然产生 `PreFinishedRoom=null` 的房间前 `current_run.save`。A 组在不重启下记录进入该房间后第一个 `candidateReady` snapshot；B 组从同一房间前 checkpoint 经**正常主菜单 Continue**进入相同房间，在相同 readiness 条件记录 snapshot。只比较现有 SPIKE 等价性字段（position、player piles/HP/energy、enemy、RNG、readiness），允许排除 `NumReloads` 与时间戳等不在 snapshot 内的元数据；不出牌、不 replay action、不 seek。

### 为何不触碰普通 save

- 实验只能在一个由 Project Lead/用户明确确认的 disposable profile 中进行；不得复制、覆盖或加载真实 profile 的 ordinary save。
- 启动前必须解析并记录 ordinary profile 与 disposable profile 的两个绝对 `current_run.save` 路径，确认 profile id/path 不同，并记录 ordinary 文件 SHA-256、长度、mtime。
- 若无法在启动前证明两个路径不同，**不启动实验**。实验后立即重新校验 ordinary 文件；任何 hash/长度/mtime 变化都视为隔离失败并停止。
- 已知 Continue 会改写 disposable profile 自己的 `current_run.save`（reload count）；这是预期且限定在一次性 profile，不得描述为“无写盘”。

### 可证伪结果与停止条件

- 通过：B 组 snapshot 与 A 组在规定字段逐项相等，且 ordinary profile 文件三项身份检查不变。
- 证伪：出现任何状态 diff，或 ordinary profile 文件变化；立即判定该候选失败，不追加修补链。
- 停止：目标版本/hash 不符；profile 路径隔离无法预先证明；出现非预期 save 路径；任一 setup/load 异常；完成首个 A/B compare。任何一项发生即停止，不继续 dispatch/capture 架构实现。

该实验直接验证本调查最高价值的语义不确定性：**RNG 必须在 room setup 之前被保存，才能让正常 reconstruction 只消费一次**。它不验证中途任意状态恢复，也不解锁 SPIKE-001B。

## 变更

- 新增 `project-management/investigation-reports/INV-002-report.md`。
- 新增 `evidence/inv-002/Invoke-NarrowIlQuery.ps1` 与 `evidence/inv-002/README.md`。
- 行为变化：无；仅报告与只读 IL 查询工具。
- 兼容性/性能风险：无运行时代码变更。
- 未修改 Project Lead 管理文档或 SPIKE-001A 源码。

## 验证与停止条件

- 已核对 release metadata、DLL ProductVersion、大小与 SHA-256；与固定目标一致。
- 已用 hash-gated Mono.Cecil 查询逐项检查上述方法体与 call site；未仅凭方法名下结论。
- 已将静态方法体、对既有日志的推断、以及未知项分开标注。
- 未启动游戏，未触发任何 probe 热键，未运行任何恢复/写盘实验，未读取或修改游戏用户存档。
- 未触发 environment drift、缺工具需安装、目标方法无法定位等停止条件。
- INV-002 验收停止条件已满足；到此停止，不实现推荐实验。

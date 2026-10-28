# INV-011 Investigation Report

日期：2026-09-17  
状态：Complete — `READY_SUCCESSOR_RECONSTRUCTION_CONTROL`  
范围：纯静态调查；未启动或控制游戏，未读取当前 profile/save/cloud/settings/diagnostic log，未修改、构建、安装或运行 `SPIKE001A3`，未创建 successor probe

## 结论

固定 DLL 的方法体足以闭合 A3 所需的四个 floor 语义，并足以制定不干扰 production continuation 的 observer 契约。令：

- `H = ArmPredecessorFloor`：arm 时已进入/完成的前驱 map point 数，即 `MapPointHistory` 各 act entry 数量之和；
- `C = CheckpointFloorReached`：目标节点 room-before、`PreFinishedRoom=null` checkpoint 的 `SerializableRun.FloorReached`；
- `A = uninterrupted A` 在目标房间首个规定 readiness 点的 `RunState.TotalFloor`；
- `B = normal Continue B` 在同一 readiness 点的 `RunState.TotalFloor`。

则固定 production 路径给出：

```text
C = H
A = H + 1
B = H + 1
A = B = C + 1
```

这里的 `+1` 不是经验猜测：`RunState.TotalFloor` 与 `SerializableRun.FloorReached` 都直接定义为 `MapPointHistory` 各 act list 的 `Count` 之和；room-before save 先复制 history，目标 room 只在 save await 返回、room create 后才调用一次 `AppendToMapPointHistory`。normal Continue 先从 checkpoint 恢复同一份 history，再以 `saveGame=false`、`PreFinishedRoom=null` 进入保存的最后 coord，并同样追加一次目标 history。

`ActFloor = target MapCoord.row + 1` 在 A/B 都于进入 map point 开始时设置，但它是 act 内地图行语义，不能代替全局 history count。跨 act 时两者显然可不同。

A3 的单一 `TargetIdentity.TotalFloor` 设计必须拆分。successor 最小模型是：

1. `ArmPredecessorFloor`：arm 时从 live `RunState.TotalFloor` 与 arm-time non-null save `FloorReached` 两个独立事实取得；二者必须存在、非负且相等。
2. `CheckpointFloorReached`：capture 前为显式 `Unbound`，production Task 成功后读取 candidate checkpoint；只有验证其等于 `ArmPredecessorFloor` 后才能转为 `Bound`。
3. `ExpectedPlayableFloor`：arm 时以 checked addition 推导为 `ArmPredecessorFloor + 1`；A/B readiness 均只与此字段比较。

observer 也必须改为单向观察：original production Task 成功后，任何 checkpoint 验证、证据写入、terminal 写入或 deadline/RoomEntered observer failure 都只能使 probe first-terminal-wins 并停止采集，wrapper 仍须成功完成；original production Task 自己的 fault/cancel 则原样传播。由此 observer-only failure 不会再使 `EnterMapPointInternal`、`TravelToMapCoord` 或 `MoveToMapCoordAction` fault。

唯一建议为 `READY_SUCCESSOR_RECONSTRUCTION_CONTROL`。这只说明静态契约已足以供 Project Lead 决定是否另行签发全新 identity 的 successor reconstruction control；本文不创建该 probe，也不解锁 `SPIKE-001B`。

## 固定输入 identity

全部固定输入精确匹配，未触发 `BLOCKED_IDENTITY`：

| 输入 | 长度 | SHA-256 |
|---|---:|---|
| `sts2.dll` | 9,757,184 | `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` |
| `SPIKE-001A3-report.md` | 12,947 | `A29135E80C600D06DF82961C5B6FAD8B7338A035CFA53A110D7648E3EC032F96` |
| `terminal-r2-analysis.md` | 3,071 | `AD3B02817FD773A5C947B211EE4FF83A3899FCA51198E4C4C605088A1CE42A47` |
| R2 `arm.json` | 8,603 | `73F013BAF0219724DDCA0117389480848F7DA77FA42E2997CC8A1BD79546871D` |
| R2 `terminal.json` | 1,359 | `7AE1D74A9C1EAAC07BC221571C1432DA12223F29BC45479EB14CD19C097C3DA9` |
| completed log | 209,772 | `DAE7A4A1B9356B702F6F17E3898F667D4C9FEABAB4D3443413A87C5BA3A1C105` |
| `CheckpointReader.cs` | 2,513 | `0E688E9AD4BEBDBDCB96B16C97D00299F60F8F67704D53CA35FFBAEBAE574DED` |
| `ProbeController.cs` | 28,537 | `791F906C0EB44252C76BE62B7257E84808EF359D8CDF12C9F4D1A0A4038FD614` |
| `ProbeModels.cs` | 8,631 | `6FB47A2593F92079E4597C77654DACD262A2B26B125288ED9CC9E5C0555A78F4` |

DLL ProductVersion 为 `0.1.0+41cef1ea4657c524aa50e870df009e56337e8c32`，与固定 public-beta `v0.111.0` / commit `41cef1ea` 一致。

## A. 四个 floor 语义

### A1. floor 字段的定义

**Confirmed method body**：

- `RunState.TotalFloor`：`RunState.get_MapPointHistory()` 后，对每个 act 的 `IReadOnlyList<MapPointHistoryEntry>.Count` 求和。
- `SerializableRun.FloorReached`：对 serialized `List<List<MapPointHistoryEntry>>` 中每个 inner list 的 `Count` 求和。
- 两者都不是独立存储/自增字段；`FloorReached` 没有 setter 参与 room-before save。
- `RunState.AppendToMapPointHistory(pointType, roomType, modelId)` 确保当前 act list 存在，构造一个新 `MapPointHistoryEntry`，然后只向当前 act list 追加一次。
- `RunManager.ToSave(room)` 在 IL `01A2–01D6` 从 live `RunState.MapPointHistory` 复制各 act list；`PreFinishedRoom` 到 IL `027D–028A` 才按参数 null/non-null 写入。因此 `FloorReached` 完全由复制时已有 history 决定。

### A2. A 路径的目标选择、coord、ActFloor、save 与 append 顺序

**Confirmed method body/call site**：

```text
MoveToMapCoordAction
  -> NMapScreen.TravelToMapCoord
  -> RunManager.EnterMapCoord(targetCoord)
     -> RunState.AddVisitedMapCoord(targetCoord)
        -> targetCoord becomes CurrentMapCoord / last VisitedMapCoords
     -> EnterMapCoordInternal(targetCoord, preFinishedRoom:null, saveGame:true)
        -> EnterMapPointInternal(actFloor = targetCoord.row + 1,
                                 pointType, null, true)
           -> State.ActFloor = row + 1
           -> ExitCurrentRooms
           -> await SaveManager.SaveRun(null, true)
              -> RunManager.ToSave(null) copies history H
              -> SerializableRun.FloorReached = H
              -> PreFinishedRoom = null
           -> create/roll room
           -> AppendToMapPointHistory(...) exactly once
           -> EnterRoom / room.Enter / RoomEntered
           -> first playable readiness
```

关键 IL 顺序位于 `RunManager/<EnterMapPointInternal>d__192.MoveNext()`：

- `IL_006E–0074`：写 `ActFloor`；
- `IL_0163–016A`：调用 `SaveRun(null,true)`；
- `IL_01C0–01C2`：await 成功返回；
- `IL_024D–0256`：null room 分支创建房间；
- `IL_026F–0287`：仅 `preFinishedRoom == null` 时 append history；
- `IL_03A2–03FC`：进入房间。

因此 A checkpoint 已包含目标 coord，却尚未包含目标 history entry。这解释了 sealed A3 evidence 的合法组合：target coord 为 `row=2,col=0`，而 arm predecessor save 的 `FloorReached=2`；coord 与 floor 本来就不是同一计数器。

### A3. normal Continue B 如何恢复 history、coord 与 floor

**Confirmed method body/call site**：

1. `NMainMenu/<OnContinueButtonPressedAsync>d__64` 先调用 `RunState.FromSerializable(save)`，再 await `SetUpSavedSingleplayer`，最后把 `save.PreFinishedRoom` 交给 `NGame.LoadRun`。
2. `RunState.FromSerializable` 在 IL `00C6–00D2` 复制 `VisitedMapCoords`，在 `00E8–00FE` 复制 `MapPointHistory`。所以刚反序列化完成时，`RunState.TotalFloor == save.FloorReached == C`。
3. `SetUpSavedSingleplayer` 与 `InitializeSavedRun` 不调用 `AppendToMapPointHistory`；reload count write 不改变 history。
4. `NGame.LoadRun` 在创建 NRun/生成 map 后调用 `AbstractRoom.FromSerializable(preFinishedRoom,state)`；null checkpoint 得到 null room，再调用 `LoadIntoLatestMapCoord(null)`。
5. `LoadIntoLatestMapCoord` 取 `VisitedMapCoords` 最后一项并调用 `EnterMapCoordInternal(coord,null,false)`。
6. `EnterMapCoordInternal` 仍传 `actFloor=coord.row+1`；`EnterMapPointInternal` 因 `saveGame=false` 跳过第二次 room-before save，但因 `preFinishedRoom==null` 仍创建 room 并 append 一次目标 history。

所以 B 的 pre-room state floor 为 `C`，可操作点为 `C+1`。它与 A 的 append 次数和相对边界相同。

### A4. 四者关系及证据等级

| 量 | 静态语义 | 关系 |
|---|---|---|
| `ArmPredecessorFloor` | arm 时已进入/完成前驱 map point 的 history count | `H` |
| room-before `CheckpointFloorReached` | target coord 已 visited，但目标 history 尚未 append | `C = H` |
| uninterrupted A playable `TotalFloor` | checkpoint 后创建 target room，并 append 一次 | `A = H + 1` |
| normal Continue B playable `TotalFloor` | 从 checkpoint history H 恢复，再 append 一次 | `B = H + 1` |

**Confirmed**：方法体定义和 A/B 调用顺序支持全部等式。  
**Unknown（只针对已封存 A3 attempt 的动态值）**：A3 没有记录 arm live `RunState.TotalFloor`，也没有 baseline/A/B snapshot，因此无法把该 attempt 的 live `H` 或 A/B 数值升级为运行时已观察事实。sealed arm 只确认 predecessor save `FloorReached=2`。这个证据缺口不要求 floor-only 动态诊断，因为 successor 可以在 arm 同时读取 live/save 两个独立事实并 fail closed。

## B. Target identity 最小数据模型

A3 的核心设计错误不是只少了一个赋值，而是一个字段跨越了两个不同边界：

- checkpoint validation 需要 `CheckpointFloorReached == ArmPredecessorFloor`；
- playable readiness 需要 `RunState.TotalFloor == ExpectedPlayableFloor`；
- 二者相差一个尚未/已经 append 的 target history entry。

successor 最小模型：

| 字段 | 取得时点 | 独立验证 | 是否 capture-later |
|---|---|---|---|
| `ArmPredecessorFloor` | arm | live `RunState.TotalFloor` 与 non-null arm save `FloorReached` 均 present/non-negative/相等 | 否 |
| `CheckpointFloorReached` | production Task 成功后的临时 checkpoint candidate | 必须等于 `ArmPredecessorFloor` 后才从 `Unbound` 转为 `Bound` | 是，显式 optional/state |
| `ExpectedPlayableFloor` | arm | checked(`ArmPredecessorFloor + 1`)，A/B readiness 分别验证 | 否；derived，不由 capture 回填 |

还需将身份分层：

- arm immutable：seed、character、act、target row/col、map point type、predecessor floor、expected playable floor；
- A 后可绑定的 semantic identity：实际 room model 与实际 encounter id；B 必须分别匹配；
- phase-local continuity：RunState/player/combat/executor/room 等 CLR/Godot object identity，只用于单个 A 或 B 的连续 30 帧，不跨进程比较。

任何未知值都必须是 `Unbound`、`Missing` 或 `ReadException` 等显式状态。禁止 `-1`、空字符串、`default(MapCoord)`、`"<null>"` 或把 snapshot normalization 的空/零/null 当作可参与相等判断的数据。

## C. Observer non-interference

### C1. A3 为什么污染 production continuation

A3 Harmony postfix 把 production `SaveRun` 返回的 `Task` 替换为 `WrapProductionSaveAsync(original)`。A3 wrapper 在 original 成功后执行 checkpoint capture/validation；observer catch 调用 `Seal(...)` 后又 `throw`。因此本次 `ValidateTarget` 的 observer-only `InvalidDataException` 使 replacement Task fault。

production `EnterMapPointInternal` 在 `IL_016A` 取得这个 Task，并在 `IL_01C2` 调 `GetResult()`；fault 使其 async builder `SetException`。随后 `TravelToMapCoord`/`MoveToMapCoordAction.ExecuteAction` await 该任务，后者也 `SetException`。这与 sealed log 中 `MoveToMapCoordAction` 的 AggregateException 及用户黑屏观察一致。

### C2. successor 分支契约

| 原 production Task | observer/terminal | wrapper 对 production 的结果 | probe 结果 |
|---|---|---|---|
| success | observer success | `RanToCompletion` | 可继续 baseline/A |
| success | validation、stable read、protection、recovery、baseline artifact 等 observer work 失败 | **仍 `RanToCompletion`** | first-terminal-wins；吞掉 observer-only exception；停止全部 A/B 采集 |
| success | terminal writer 自身失败 | **仍 `RanToCompletion`** | in-memory sealed；只做 bounded fallback log/UI；writer exception 吞掉 |
| fault | terminal write 成功或失败 | 原 production fault 原样传播 | 只做 best-effort production-fault terminal；writer 不能替换原异常 |
| canceled | terminal write 成功或失败 | 原 production cancellation 原样传播 | 只做 best-effort production-canceled terminal；writer 不能把 canceled 改成 fault/success |

实现顺序必须是：先单独 await original；只有 original 成功才进入一个完全受控、catch-all 的 observer block。observer block 自己抛出的 `OperationCanceledException` 也属于 observer-only failure，不能把成功 production Task 变成 canceled。original fault/cancel 分支可以 best-effort 写 terminal，但随后必须传播 original outcome；terminal writer exception 永远吞掉。

### C3. deadline 与 RoomEntered 竞态

- deadline、checkpoint observer、RoomEntered guard 与任何 terminal site 共用一个 compare-exchange latch；只有第一个 winner 获得一次 artifact-write ticket。
- loser 不写第二 terminal、不改变 classification、不创建 baseline/recovery、不启动或继续 readiness/A/B。
- 若 deadline 在 checkpoint validation 中先赢，observer 在下一个 seal point 看到 terminal 后直接返回 success，不再 throw。
- 若 observer failure 先赢，它 best-effort terminal 后返回 success；production 可以继续创建/进入 room，但 `RoomEntered` callback 因 terminal 已 sealed 立即返回。
- “production 继续”只说明游戏原调用链不被 probe 破坏；实验已终止。terminal 后禁止 baseline、recovery、quiet accumulation、A/B snapshot 与 compare。

该契约直接证明 successor 不会因 observer-only failure 使 `MoveToMapCoordAction` fault：replacement wrapper 在 original success 分支没有可逃逸的 observer exception。

## D. placeholder 与校验顺序审计

完整表见 `evidence/inv-011/placeholder-validation-audit.md`。主要结论：

1. `TargetIdentity.TotalFloor=-1` 在绑定前参与 checkpoint equality，是本次已确认 terminal 根因。
2. seed/character/room/RNG 的多个 `?? ""`、readiness 的 `"<null>"`、checkpoint coord 的 `LastOrDefault()`、snapshot `MaxHp=-1` 与 bool fallback 都把未知/失败状态折叠成可比较值。
3. B prefix 未验证 `PreFinishedRoom`、saved `FloorReached` 与 `FromSerializable` 后 pre-room `TotalFloor`。
4. `PositionSnapshot.EncounterId` 实际再次写入 `room.ModelId`，room 与 encounter 语义被混用。
5. R2 row/col 已一致；R1 的 `(row,col)` / `(col,row)` 缺陷不再存在于固定 R2 source。
6. object identities 正确地不跨 A/B 比较，但应移出 semantic snapshot，明确标为 phase-local continuity。
7. snapshot normalization 用 empty/zero/null 排除非语义字段虽有意为之，但专用 semantic projection 比“写入 sentinel 后比较”更安全可审计。

successor 的强制 validation order 已写入机器契约：arm 双事实 → derived playable floor → await original → 临时 checkpoint candidate → 完整 validation → bind checkpoint floor/baseline → A semantic bind → B pre-room floor gate → B readiness → semantic compare。

## 合成 contract tests

`evidence/inv-011/Test-SuccessorContract.ps1` 对机器契约执行了 40/40 PASS：

- 正向：`H=2, C=2, A=B=3`；跨 act history `18→19`，证明不能用 row+1 替代 TotalFloor。
- 负向：拒绝 `-1`、arm live/save floor 不等、missing checkpoint floor、把 playable floor 用作 checkpoint floor、A/B 错 floor、double bind、bind 前 readiness。
- placeholder：禁止 empty/default/sentinel；room/encounter 分离；object identity phase-local。
- observer：success/success、success/observer-failure、terminal-writer failure、production fault、production cancel。
- ordering：production completion 先于 observer、capture→validate→bind、baseline→A→B-pre→B。
- race：deadline/RoomEntered 共用 CAS；terminal 后 production 可继续而实验停止。

这些测试验证的是静态 contract 数据与状态机，不是 runtime probe。未来若 Project Lead 签发 successor，其实现测试还必须用真实 completed/faulted/canceled `Task` 验证 returned Task status、原异常 identity/cancellation semantics 与 terminal writer failure 隔离。

## Confirmed / Inferred / Unknown

### Confirmed

- 所有固定 identity 匹配。
- `TotalFloor` 与 `FloorReached` 都由 `MapPointHistory` count sum 派生。
- A room-before save 发生在 target history append 前，且 target coord/ActFloor 已设置。
- A null-room路径 append 一次 history 后才 enter room。
- normal Continue 复制 saved history/coords，以 last coord、null room、`saveGame=false` 重建，并 append 一次 history。
- A3 observer validation异常会从 replacement Task 逃逸，沿 production await 链使 map action fault。
- A3 placeholder/validation-order 与 room/encounter 混用如审计表所列。

### Inferred

- arm 时 live predecessor floor 与已有 non-null predecessor save floor 在正常、无额外 map entry 的固定场景应相等；方法体顺序支持该解释。successor 不依赖此推断，必须实际读取两个事实并要求相等。
- sealed A3 checkpoint 的实际 `FloorReached` 应仍为 2；terminal artifact 未保存 candidate baseline identity，但 terminal analysis 已固定该只读解析结果。本文不把它升级为新运行观测。

### Unknown

- A3 已封存 attempt 的 live arm `RunState.TotalFloor`、A/B playable 数值与 reconstruction equivalence；A3 没有记录这些数据。
- successor 若被另行实现，其具体 runtime loader/Harmony 行为仍需自己的安全门禁与一次受控运行验证。

上述 Unknown 不阻止可执行静态契约：所有未知 runtime 值都有独立 present/equality gate，不再靠 sentinel 或先绑定后证明。

## 结果分类与唯一下一建议

**`READY_SUCCESSOR_RECONSTRUCTION_CONTROL`**

理由：四个 floor 语义、normal Continue 的 pre-room/append 顺序、target 最小模型、observer non-interference、placeholder 审计与 regression contract 均已闭合，不存在必须先动态观察才能安全比较 A/B 的 floor 事实；固定 production 语义也提供了可比较的 A/B 边界。

该建议不直接解锁 `SPIKE-001B`。是否签发全新 successor probe 只由 Project Lead 决定。

## 工具、命令、产物与偏离

工具：PowerShell 7、`Get-FileHash`、`rg`、Mono.Cecil 0.11.5（经既有 hash-gated `evidence/inv-002/Invoke-NarrowIlQuery.ps1`）。

主要命令：

```powershell
& 'evidence\inv-011\Invoke-Inv011StaticQuery.ps1'
& 'evidence\inv-011\Test-SuccessorContract.ps1'
Get-FileHash -Algorithm SHA256 -LiteralPath <fixed-input-or-deliverable>
rg -n --glob '*.cs' -- '-1|\?\? ""|<null>|LastOrDefault|default' 'experiments\SPIKE-001A3'
```

交付物：

- `evidence/inv-011/Invoke-Inv011StaticQuery.ps1`
- `evidence/inv-011/il-query-output.txt`
- `evidence/inv-011/successor-contract.json`
- `evidence/inv-011/placeholder-validation-audit.md`
- `evidence/inv-011/Test-SuccessorContract.ps1`
- `evidence/inv-011/synthetic-contract-tests.txt`
- `evidence/inv-011/README.md`
- 本报告

偏离：无。没有使用 Computer Use；没有启动游戏；没有读取当前用户状态；只读取 handoff 明确允许的 sealed A3 evidence；没有修改、构建、安装、启用或运行 A3；没有创建 successor probe；没有修改受保护项目管理状态文档。到此停止并交回 Project Lead。

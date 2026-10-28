# SPIKE-INV004A：direct progress-write 隔离哨兵

> 历史状态（2026-09-10）：本工作包已以 pre-override isolated-manager content-equivalence FAIL 安全结束。不得重试或继续；后续仅执行 `SPIKE-INV004B` 诊断。详见 `project-management/investigation-reports/SPIKE-INV004A-report.md` 与 D-033。

任务类型：受限运行实验；需要项目发起人手动执行游戏内步骤

## 任务目标

复用 SPIKE-INV003A Phase B 已验证的 in-memory `SaveManager` transaction、native replay state factory 与 ordered teardown，但不分派任何 replay event。在隔离 replay session 的正常普通战斗状态中，直接调用当前 `SaveManager.Instance` 上的 production `UpdateProgressAfterCombatWon(player, room)`，随后调用 `SaveProgressFile()`，验证 progress mutation 与 `progress.save` write 是否只落入 isolated manager/store。

本实验只回答 direct progress-write 的隔离边界，不回答自然 combat victory、achievement call、replay fidelity、reconstruction 或完整 session isolation。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/handoffs/SPIKE-INV003A.md`
4. `project-management/investigation-reports/SPIKE-INV003A-report.md`
5. `project-management/investigation-reports/INV-004-report.md`
6. `evidence/inv-004/README.md`
7. `evidence/inv-004/official-debug-caller-vs-phase-c.md`
8. `evidence/spike-inv003a/runtime/phase-b-transaction.json`
9. `experiments/SPIKE-INV003A/ReplayTransaction.cs`

## 固定环境

- 游戏：public-beta `v0.111.0`，commit `41cef1ea`
- `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 测试 profile：模组存档 1 号；这是专用测试槽，Continue 仅为 active-profile 隔离哨兵，全内容解锁状态及全部基线文件仍受保护
- Steam Cloud：当轮测试前由项目发起人确认关闭
- fixture：受保护备份中的 20,441-byte native `.mcr`，SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`
- 启用模组：只允许本 probe；RitsuLib、RunReplays、BaseLib、旧 SPIKE probe 与其他 gameplay mod 均关闭

用户真正的普通对局位于其他槽。本任务不得读取、复制、备份或切换到其他槽，结论也不得声称已直接验证那些普通对局。

任一身份不符时 `BLOCKED_IDENTITY`，不得运行 transaction。

## 封存与复用边界

- 不修改或解锁 `evidence/spike-inv003a/phase-control.json`。
- 不覆盖 `experiments/SPIKE-INV003A/`、其 artifact、runtime JSON 或报告。
- 新建独立 `experiments/SPIKE-INV004A/` 与 `evidence/spike-inv004a/`；可以复制经审查的 Phase B transaction/audit 代码，但必须记录来源和实质差异。
- 不重复 Phase A，也不重新宣称 Phase B；fixture 在本实验中只作为合法 replay state factory。

## Phase 0：实现前静态门禁

在固定 DLL 上重新确认并记录：

1. `CombatManager.EndCombatInternal` 中两个 SaveManager 调用的相对顺序是 `UpdateProgressAfterCombatWon(localPlayer, room)`，中间存在 achievement check，之后才 `SaveProgressFile()`。
2. 本 probe **不调用** `EndCombatInternal` 或 achievement helper；它仅依次调用上述两个 SaveManager public methods。因此它不是完整 production victory call-site replay。
3. `UpdateProgressAfterCombatWon` 对固定普通房间会改变哪些 isolated progress 字段，以及是否改变 live replay player 的 `DiscoveredEnemies`；对重复 enemy model 应给出预期 delta，不得只比较“JSON 有变化”。
4. `SaveProgressFile` 的 profile path 解析到 isolated store 中的预期相对路径，写入 payload 可反序列化回与 mutation 后等价的 progress。
5. 现有 audit 是否覆盖这两个 manager boundary、isolated/original store、Cloud/Steam store 与外部 achievement/metric/leaderboard/daily submit 边界。

若无法在调用前给出确定的预期 mutation 或合法参数条件，报告 `BLOCKED_PRECONDITION`，不要运行。

## Phase 1：probe 实现

### 1. 保护基线

复用并修正 SPIKE-INV003A 最终 preflight 语义：

- 主菜单、`RunManager.State == null`、active profile1 与 Continue 哨兵；
- 固定游戏/probe/fixture 身份与唯一启用模组；
- 等待真实 manager 当前 save task；
- 记录真实 manager/store/provider identity；
- 记录 profile1 全树、account `profile.save` 与真实 account-scoped `settings.save` 的 path、hash、length、mtime 与 inventory；
- 只备份 profile1 文件到新 evidence 目录，逐文件校验副本 hash。

不得读取、备份或切换其他 profile。

### 2. 隔离上下文

从 SPIKE-INV003A Phase B 的已审查实现复用：

- fully initialized、`MockGodotFileIo`-backed、非共享引用的 isolated `SaveManager`；
- setup 前安装 `MockInstanceForTesting(isolated)` 并断言 identity；
- native `SetUpReplay` 返回后立即设置并断言 `ShouldSave=false`；
- preload/launch/map/room load 后到达首个稳定、可观看的普通战斗 scene；
- transaction 不可嵌套，不允许 profile change、Continue、Singleplayer、Multiplayer 或 Quit。

### 3. Direct-write sentinel

不得读取或 dispatch `replay.events`。到达稳定 scene 后，调用前必须全部断言：

- 当前 `SaveManager.Instance` 就是 isolated manager；
- 单玩家存在，当前 room 是固定 fixture 对应的普通 `CombatRoom`；
- `room.CombatState.RunState` 与当前 replay run identity 一致；
- encounter 与 `SpawnedEnemies` 可读，room 不是 elite/boss；
- isolated Profile/Settings/Prefs/Progress 与原 manager 不共享引用；
- 原 store 与 external submit counters 为 0；
- isolated progress canonical baseline、目标 encounter/character stats、各 spawned enemy stats 与 replay player `DiscoveredEnemies` baseline 已记录。

随后仅执行：

1. `SaveManager.Instance.UpdateProgressAfterCombatWon(player, room)`；
2. 对照 Phase 0 的预期 delta 验证 isolated progress mutation 与允许的 replay-player mutation；
3. `SaveManager.Instance.SaveProgressFile()`；
4. 验证 isolated store 对预期 profile1 `progress.save` 恰有预期的一次 write，payload 可反序列化且与 mutation 后 progress 等价；
5. 验证两个 SaveManager audit boundary 的 target 都是 isolated manager，原 manager/store/cloud/external-submit counters 仍为 0；
6. 立即进入 teardown，不等待自然胜利，不进入奖励、地图或下一房间。

不得把跳过的 `AchievementsHelper.CheckForDefeatedAllEnemiesAchievement` 描述为已验证。若 Update 方法内部产生预期之外的外部 side effect，立即 FAIL。

### 4. Ordered teardown

继续使用同一个 finally owner：

1. 标记 restore 开始并禁止新操作；
2. 等待 isolated current save task；
3. `RunManager.CleanUp(true)` 一次；
4. 再次等待 isolated task；
5. 清除 override，恢复并断言原 manager identity；
6. 等待原 manager task；
7. 恢复原 manager 后才 `LoadMainMenu(false)`；
8. 等待新主菜单稳定，确认 Continue 可见；
9. 比较全部受保护文件 identity 与 inventory。

任一步异常都继续尽力恢复原 manager，但本实验记为 FAIL。

## 人工测试协议

Implementation Codex 不得使用 Computer Use 或任何 GUI 自动化。必须按 `TEST_PROTOCOL.md` 分轮请求：

1. Phase 0/preflight 人工确认；
2. 唯一 direct-write runtime phase。

每轮给出测试目标、开始条件、精确点击步骤、预期界面/日志、停止条件、禁止动作、目标后的动作和需回传证据。项目发起人只负责启动游戏与点击 probe 入口；本实验不要求出牌或结束回合。

运行 phase 中不得点击卡牌、结束回合、战斗奖励、地图节点或 Continue。probe 达成 sentinel 后应自动立即 teardown；用户只观察并等待回到主菜单。若出现任何奖励界面，不得领取金币、卡牌、药水或遗物，立即按测试请求的停止条件报告。

## PASS

必须全部满足：

- 没有 dispatch 任何 replay event，没有调用自然 victory/achievement 路径；
- 两个指定 SaveManager 方法均命中 isolated manager；
- isolated progress 与允许的 replay-player mutation 精确符合 Phase 0 预期；
- isolated `progress.save` 恰有预期 write，payload 可读且内容等价；
- 原 manager/store/cloud 与 external submit counters 全为 0；
- ordered teardown 完成，原 manager identity 恢复，`RunManager.State == null`；
- Continue、全解锁状态及所有受保护文件 hash/length/mtime/inventory 不变；重建主菜单后可见状态正常。

PASS 只允许写为：**direct production progress-update/save methods 在本固定 isolated session 中被路由至 memory manager/store。** 不得写成自然胜利、achievement、replay fidelity 或完整 session isolation 已通过。

## FAIL / BLOCKED

以下任一立即 FAIL 并停止：

- original/cloud/external boundary 被调用；
- manager target、write path/count、payload 或 mutation delta 不符；
- live 参数非法、共享引用、意外 side effect 或 audit blind spot；
- teardown 异常、manager/state 残留或受保护文件变化。

身份/合法前置条件无法建立则 BLOCKED。不得改为 dispatch events、人工出牌、强制敌人死亡、调用 `EndCombatInternal`、换 fixture 或 patch-away write。

## 明确非目标

- 不重试或修复 SPIKE-INV003A Phase C。
- 不调查 readiness/cancellation，不比较 checksum，不验证 replay 到胜利。
- 不执行原 D/E/F，不把故障矩阵并入本实验。
- 不实现 capture/dispatcher、reconstruction、产品 UI、seek、状态纠偏或架构。
- 不引入 RitsuLib，不读取其他 profile。

## 交付物

- probe：`experiments/SPIKE-INV004A/`
- evidence：`evidence/spike-inv004a/`
- 报告：`project-management/investigation-reports/SPIKE-INV004A-report.md`

报告必须包含：固定身份、代码复用差异、调用前合法性、精确预期/实际 mutation、write path/count/payload、audit counters、teardown 顺序、全部保护 identity，以及明确受限的 PASS/FAIL/BLOCKED 结论。

Implementation Codex 不得修改 Project Lead 管理文档或既有报告。

满足一个完整 PASS、首个可审计 FAIL 或 BLOCKED 后立即停止。

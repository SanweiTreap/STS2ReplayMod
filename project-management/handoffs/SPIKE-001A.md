# SPIKE-001A Implementation Codex Handoff

状态：Ready  
发出日期：2026-08-27  
父实验：Wave 1 / `SPIKE-001`  
后续任务：`SPIKE-001B`（当前 Blocked）

## 任务目标

在 STS2 public-beta `v0.111.0` 中创建一个 disposable developer probe，只回答一个问题：

> 游戏原生 `SerializableRun` 能否从首章第一场弱怪、玩家首回合输入就绪点，独立恢复到与 fixture 相同的结构位置、战斗白名单状态和完整 RNG digest？

本任务不录制、不重放玩家操作。恢复基线不成立时，后续 capture/dispatch 没有可信起点，因此必须在 `SPIKE-001B` 前单独验证。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/MVP.md`
3. `project-management/EXPERIMENT_PLAN.md`
4. `project-management/investigation-reports/INV-001-report.md`
5. `project-management/HANDOFF_TEMPLATE.md`

INV-001 中的 API 仅是静态候选；不得把 `ToSave` 可调用、恢复链符号存在或 readiness 字段存在写成运行时已验证事实。

## 固定环境

- 游戏：public-beta `v0.111.0`
- commit：`41cef1ea`
- `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 场景：单人 Standard、Ascension 0、固定角色、固定 seed、首章第一场弱怪、玩家首回合进入 `PlayerTurnPhase.Play`
- 模组环境：只启用 probe 及其必要依赖；不得启用 RunReplays 或其他 gameplay mod

报告必须记录角色、seed、实际启用模组/版本和 probe artifact hash。若游戏版本/hash 已变化，停止并报告环境漂移，不继续测试。

## 实现边界

- 将实验性源码、项目文件和说明放在 `experiments/SPIKE-001A/`；明确标记 disposable，不建立正式 Replay Mod 架构。
- 可以实现最小 developer-only 触发方式来执行“生成 fixture”和“载入 fixture”，但不得建设正式菜单、Replay 管理页或通用命令系统。
- 使用 v0.111 候选链：`RunManager.ToSave(CurrentRoom)`；恢复时分别观测 `RunState.FromSerializable`、await `RunManager.SetUpSavedSingleplayer`、await `NGame.LoadRun`。
- 每个阶段独立记录开始、成功、异常与耗时，确保能定位首个失败步骤。
- 只在明确的 fixture/restore 边界批量写诊断；不得引入逐帧或逐 action `AutoFlush` 日志。
- 不复制 RunReplays dispatcher、UI、watchdog、全量状态扫描或 live save 覆盖做法。

## 存档与文件安全

- 不得覆盖、移动或删除玩家普通 `current_run.save`、profile、run history 或其他 live save slot。
- Fixture 和结果只写入明确的 probe 专用目录；报告给出绝对实际路径和清理方法。
- 若目标 API 无法在不写普通 save slot 的情况下验证，立即停止为 Blocked，说明观察到的调用边界；不得自行备份后覆盖，也不得要求玩家承担未说明的存档风险。
- 不运行跨版本 fixture，不从 RunReplays sample 转换 fixture。

## Fixture 与白名单状态

在输入就绪点、调用 `ToSave` 前后均确认场景未推进。保存 fixture 时记录 expected：

- 精确 build、probe schema/version、角色、seed、game mode、ascension、fixture hash。
- 结构位置：act index、map row/col、room ID（如有）、total floor、combat round。
- 玩家：HP、energy、手牌稳定 Card ModelId 的有序序列。
- 牌堆：draw/discard/exhaust 中稳定 Card ModelId 的多重集；排序只用于规范化比较，不改变游戏状态。
- 敌人：稳定 ModelId、CombatId（分别保存）、HP、存活状态。
- 完整 `RunRngSet.ToSerializable()`：每个存在的 RNG 的名称/键及 `counter/state0/state1/state2/state3`；生成规范化 digest，并保留可读摘要。
- readiness 观测值：`Phase`、`IsPartOfPlayerTurn`、`PlayerActionsDisabled`、`IsPaused`、executor running/current action，以及 probe 能安全取得的加载状态。

白名单只是本实验的相等性判据，不是 MVP 权威状态格式。不要因为发现额外状态而扩大记录范围；将疑点写入 Unknown。

## 恢复试验

从同一 fixture 至少完成两次相互独立的恢复尝试，其中至少一次应在完整退出并重新启动游戏后进行。每次恢复：

1. 验证 fixture build/hash/schema 与当前环境一致。
2. 分步骤执行恢复候选链，记录首个失败或 await 完成。
3. 等待到明确的输入就绪候选状态；不得用无限 retry 或 watchdog 抹去首个阻塞。
4. 采集 actual 白名单和 RNG digest。
5. 输出机器可读 diff：字段路径、expected、actual；同时报告 readiness 中阻塞的具体信号。

如果运行测试必须由项目发起人手动操作，先完成可构建 artifact 和一份不含歧义的短测试步骤；将任务标为 Partial/Waiting for runtime evidence，不得凭编译成功宣称恢复成功。

## 成功、失败与停止条件

### 通过

- 两次独立恢复均完成候选链。
- 两次的结构位置、白名单战斗状态和完整 RNG digest 都与 expected 一致。
- 报告包含可审计日志、fixture/result 文件、probe artifact hash 和复现步骤。

### 有价值的失败

以下任一结果也可作为实验完成，但不会解锁 `SPIKE-001B`：

- `ToSave` 在该边界失败或明显推进/破坏状态。
- `FromSerializable`、saved setup 或 `LoadRun` 出现可重复首个失败。
- 恢复完成但白名单或 RNG digest 可重复不一致。
- 无法在不触碰普通 live save 的情况下验证。

失败时保存最小证据并立即停止。不得自行实现完整快照、修补游戏状态或改换其他房间以追求成功。

### 无效结果

- 只编译、不运行。
- 只恢复一次且没有可复查 expected/actual。
- 仅凭画面相似判断成功。
- 通过覆盖普通 save slot 或启用 RunReplays 得到结果。

## 明确非目标

- 不捕获或 dispatch 出牌、End Turn 或任何玩家操作。
- 不实现状态纠偏、后退、seek、播放节奏、正式 Replay 文件、管理页或路线图。
- 不测试第二种角色、敌人、房间、选择界面或多个版本。
- 不做性能优化或 PERF-001；只避免明显污染后续基线的日志/I/O 设计。
- 不决定最终检查点、序列化或项目架构。

## 交付物

- 实验源码与构建说明：`experiments/SPIKE-001A/`
- 运行证据：`evidence/spike-001a/`
- 报告：`project-management/investigation-reports/SPIKE-001A-report.md`

报告使用 `HANDOFF_TEMPLATE.md`，并额外包含：环境身份、两次恢复矩阵、expected/actual diff、首个失败点、普通 save 未被修改的验证、清理方法，以及以下二选一结论：

- `Restore baseline passed; recommend unblocking SPIKE-001B`
- `Restore baseline not established; keep SPIKE-001B blocked`

Implementation Codex 不得修改 `CURRENT_STATUS.md`、`ROADMAP.md`、`TODO.md`、`DECISIONS.md`、`MVP.md` 或 `EXPERIMENT_PLAN.md`；这些由 Project Lead 验收后维护。

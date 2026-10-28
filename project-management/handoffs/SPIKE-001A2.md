# SPIKE-001A2 Implementation Codex Handoff

状态：Retired / superseded by `SPIKE-001A2-R2.md`；不得执行  
发出日期：2026-08-28  
触发证据：`INV-002-report.md`  
任务类型：受限运行实验；需要用户执行游戏内步骤

> 2026-09-04 状态更新：不得按本文当前版本执行。新增 MVP 要求规定正式 Replay 从主菜单独立入口进入，不切换 profile、不占用普通 Continue。本文的 disposable profile + normal Continue 只可能保留为重建语义 control；等待 `INV-003` 后由 Project Lead 修订或取代。

> 2026-09-15 最终状态：本工作包已由 `project-management/handoffs/SPIKE-001A2-R2.md` 取代。旧文中的 ordinary/disposable 双 profile、profile 切换、其他槽保护读取与 restore helper 均与当前测试协议冲突，任何 Implementation Codex 都不得按本文运行。

## 任务目标

在 STS2 public-beta `v0.111.0` 中，只使用一个由用户明确指定的新建 disposable 官方 profile，对同一个房间前原生 checkpoint 做一次正常 Continue A/B 等价性实验：

- A：不重启，从房间前 checkpoint 自然进入首章第一场弱怪，在第一个输入就绪点记录 snapshot。
- B：从同一个房间前 checkpoint 经主菜单正常 Continue，进入相同房间，在相同 readiness 条件记录 snapshot。

本任务只回答：官方 `PreFinishedRoom=null` 房间前保存边界能否在正常 Continue 后重建出与 uninterrupted baseline 相同的首个可操作战斗状态，同时 ordinary profile 是否保持不变。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/EXPERIMENT_PLAN.md`
3. `project-management/investigation-reports/SPIKE-001A-report.md`
4. `project-management/investigation-reports/INV-002-report.md`
5. `project-management/handoffs/SPIKE-001A.md`
6. `project-management/HANDOFF_TEMPLATE.md`

复用 SPIKE-001A 的 snapshot 字段、readiness 定义、构建方法和日志规范；不得继承其中已否决的中途 `ToSave(CurrentRoom) → SetUpSavedSingleplayer → LoadRun` 恢复链。

## 固定环境

- 游戏：public-beta `v0.111.0`，commit `41cef1ea`
- `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 场景：首章第一场弱怪，玩家首回合第一个输入就绪点
- Steam Cloud：实验前必须由用户确认关闭；若开启则停止
- 模组：只允许本实验 probe 及其必要依赖；记录精确 manifest、DLL hash 和实际启用列表

版本、DLL hash、模组环境或 profile 身份任一不符时，不执行运行实验。

## Phase 0：只读隔离预检

先实现或复用窄范围工具，输出但不修改：

1. 当前 selected profile id，以及游戏实际解析出的 profile/save 根路径。
2. 用户指定的 ordinary protected profile id 与 disposable profile id。
3. 两者 `current_run.save` 的绝对规范化路径；路径不存在时也记录 expected path。
4. ordinary protected profile 下所有已存在 `current_run.save` 的 SHA-256、长度和 UTC mtime；同时记录相关目录清单，避免只保护一个误判路径。
5. disposable profile 对应文件的同类基线。
6. Steam Cloud 状态与当前启用模组列表；不能可靠读取时要求用户明确确认并在报告中标记证据类型。

硬性停止条件：

- 用户没有明确指出哪个 profile 是 disposable、哪个是 ordinary protected；
- 两个 profile id 相同；
- 两个规范化 save 路径相同、互为重解析/链接目标，或路径归属无法证明；
- 无法建立 ordinary 保护基线；
- Cloud 未确认关闭；
- 发现预期外的 save root、同步器或 active profile 漂移。

Phase 0 不得创建、复制、覆盖、删除或恢复任何 save。Implementation Codex 完成预检工具后，把精确步骤交给用户执行；没有用户返回的预检证据，不得进入 Phase 1。

## Phase 1：准备最小 probe

允许在 `experiments/SPIKE-001A2/` 创建窄范围 probe，优先复用 SPIKE-001A 的只读 snapshot、RNG digest、readiness 和结构化日志代码。

probe 只允许：

- 识别并记录房间前原生 `current_run.save` 的身份与 `PreFinishedRoom=null`；
- 在第一个 `candidateReady` 点记录 A/B snapshot；
- 复制 disposable profile 的房间前 checkpoint 到本任务 evidence 目录作为只读 fixture，或在用户明确步骤下仅恢复到同一个 disposable profile；
- 比较 snapshot 和保护文件身份；
- 明确提示 `READY_FOR_A`、`READY_FOR_B`、`PASS`、`FAIL` 或停止原因。

不得 patch 游戏保存逻辑、伪造原生 checkpoint、替换 `SaveManager`/`ISaveStore`、调用中途 setup/load、自动操作 UI、出牌或结束回合。

任何写回 disposable save 的 helper 都必须：

- 只接受 Phase 0 已固定的单个绝对文件路径；
- 每次写入前再次验证 resolved path 仍属于 disposable profile 根目录；
- 拒绝 glob、目录递归、ordinary profile 路径和未知目标；
- 保留写入前后 hash 与来源 fixture hash；
- 由用户在游戏完全退出后显式触发，不由 probe 后台自动执行。

如果不需要写回即可从同一 checkpoint 完成 A/B，优先不实现写回 helper。

## Phase 2：一次性 A/B 运行步骤

由 Implementation Codex 给用户一份逐步测试清单，但不得代替用户操作游戏。测试必须遵守：

1. 用户切换并确认处于 disposable profile；probe 再次记录 active profile/path。
2. 在固定 seed/场景中，让游戏自然产生房间前 `PreFinishedRoom=null` checkpoint。
3. 保存该 checkpoint 的只读证据与 hash；不得从当前战斗调用 `ToSave(CurrentRoom)` 代替。
4. 不重启进入房间，在首个 `candidateReady` 记录 A snapshot；不出牌。
5. 退出游戏并验证 checkpoint/fixture 身份。若退出过程改变实验源且没有已验证的 disposable-only fixture，则停止，不临时设计新恢复链。
6. 从同一个原生房间前 checkpoint 启动游戏并使用主菜单正常 Continue；不调用自制 setup/load。
7. 在相同 readiness 条件记录 B snapshot；记录完成后立即停止，不出牌。
8. 逐项比较 A/B，并重新读取 ordinary protected profile 基线。

只允许一个有效 A/B compare。环境错误、操作错误或日志缺失可以在未进入 B 前修正；一旦得到首个完整 compare，无论 PASS/FAIL 都停止。

## 比较字段与判定

至少沿用 SPIKE-001A：

- 结构位置：act、floor、map coord、room/encounter id；
- readiness：原字段、连续 quiet-frame 计数和 snapshot 时机；
- 玩家：HP、max HP、energy、hand/draw/discard/exhaust 的有序 card identity；
- 敌人：CombatId、类型、HP/max HP、alive/dead/escaping 等既有字段；
- 完整 `RunRngSet`：每个 entry 的 stable name、seed/state/counter（可取得项）和 digest；
- checkpoint：`PreFinishedRoom`、文件 hash、长度和 mtime；
- 环境：active profile id/path、游戏和 probe hash。

允许排除：`NumReloads`、wall-clock timestamp、日志序号以及报告中明确列出的非游戏语义元数据。不得为了得到 PASS 临时扩大排除列表。

判定：

- `PASS`：A/B 所有规定状态字段逐项相等，且所有 ordinary protected 文件的 hash、长度、mtime 和目录身份均未变化。
- `FAIL_STATE`：任一规定状态字段不同；报告首个 diff 和完整 diff，不追加修补链。
- `FAIL_ISOLATION`：任一 ordinary protected 文件或受保护目录身份变化，或出现未预期 save path；立即停止。
- `BLOCKED`：无法证明隔离、无法取得同一房间前 checkpoint、版本/环境不符或工具无法获得规定证据。

## 明确非目标

- 不记录或 dispatch 玩家操作，不出牌，不结束回合。
- 不测试第二场战斗、事件、商店、奖励、Boss 或多场景覆盖。
- 不验证中途战斗恢复、快速重演、seek、后退或强记录。
- 不设计 Replay 文件格式、正式 recorder/player、UI 或最终架构。
- 不使用 `CleanUp → setup` 实验，不 patch reload-count save，不替换存储层。
- 不触碰、复制、覆盖、删除或恢复 ordinary protected profile 的任何 save。
- 不解锁或实施 SPIKE-001B；只能在报告中说明前置是否可能满足。

## 交付物

- probe 源码与构建说明：`experiments/SPIKE-001A2/`
- 运行证据：`evidence/spike-001a2/`
- 报告：`project-management/investigation-reports/SPIKE-001A2-report.md`

报告必须区分：运行确认、源码/IL 确认、推断、未知；列出全部变更、命令、用户步骤、环境身份、路径保护证据、A/B diff 和停止原因。

Implementation Codex 不得修改 `CURRENT_STATUS.md`、`ROADMAP.md`、`TODO.md`、`DECISIONS.md`、`MVP.md` 或 `EXPERIMENT_PLAN.md`。

## 验收停止条件

- 已有一个完整 PASS/FAIL compare，或在进入运行前给出可审计 BLOCKED 原因；
- ordinary protected profile 未被实验写入；
- 没有出牌、dispatch、快速重演或架构扩张；
- 报告已落盘。

满足任一终局后立即停止，将结果交给 Project Lead。不要自行进入 SPIKE-001B。

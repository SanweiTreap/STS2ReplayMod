# SPIKE-001A2-R2 Implementation Codex Handoff

状态：Retired after inconclusive R3; superseded by static `INV-010`;不得执行或重跑  
发出日期：2026-09-15  
任务类型：受限 reconstruction control；需要用户手动执行游戏内步骤

## 任务目标

在 STS2 public-beta `v0.111.0`、固定模组存档 1 号中，对同一个由游戏正常产生的房间前 `PreFinishedRoom=null` checkpoint 做一次 A/B 对照：

- A：不重启，从地图自然进入首章第一场弱怪，在玩家首回合第一个输入就绪点采集 snapshot；
- B：新进程从 A 使用的同一 checkpoint 通过主菜单正常 Continue，在同一输入就绪点采集 snapshot。

唯一问题是：正常 Continue 能否把原生房间前 checkpoint 重建为与 uninterrupted A 相同的首个可操作战斗状态。

本任务是 reconstruction control，不是 Replay 观看会话，不证明 session isolation。B 中 production `IncrementNumReloads → SaveRun` 的已知写入是实验对象的一部分，必须单独审计，不能称为“零写入”。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/EXPERIMENT_PLAN.md`
4. `project-management/investigation-reports/SPIKE-001A-report.md`
5. `project-management/investigation-reports/INV-002-report.md`
6. `evidence/spike-inv008a/project-lead-r6-final-review.md`
7. 已退役 handoff `project-management/handoffs/SPIKE-001A2.md`，只用于了解历史；其中 profile 切换、ordinary/disposable 双 profile 与 restore helper 设计全部失效，不得执行。

## 固定环境与权限

- 游戏：public-beta `v0.111.0`，commit `41cef1ea`。
- `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- 只使用模组存档 1 号。它是全内容解锁的专用测试槽；其 Continue 是测试状态，不是用户真实普通对局。
- 不读取、复制、切换、比较或修改其他 profile。
- Steam Cloud 必须关闭；无法确认则停止。
- 游戏只由用户手动启动和操作。禁止 Computer Use 和任何 GUI 自动化。
- `SPIKEINV008A` 必须在安装/启用新 probe 前由用户手动禁用并正常退出。不得删除 008A 源码、artifact、recovery 或证据。
- 运行时只允许本 probe `SPIKE001A2R2`；必要依赖必须在 Phase 0 明列并经 Project Lead 契约允许，不能默认加入 BaseLib/RitsuLib。

Implementation Codex 可以在 `experiments/SPIKE-001A2-R2/` 内实现、修改、测试和构建 probe，并精确安装自己的 DLL/manifest。不得修改正式产品源码或项目管理状态文件。

## Phase 0：离线实现与门禁

建立全新 mod id、schema、artifact 目录和文件名；不得复用或覆盖旧 `SPIKE-001A2`/`SPIKE-001A` artifact。

probe 只允许：

- 在主菜单稳定就绪后确认版本、精确 loaded-mod set、active profile1 与 Cloud-off；
- 只读识别 profile1 当前 run checkpoint，并验证 `PreFinishedRoom == null`；
- 在 checkpoint baseline、A snapshot、A 退出后 checkpoint verification、B pre-Continue、B snapshot 和最终审计点写入本任务 evidence artifact；
- 复用 SPIKE-001A 已有 snapshot/readiness/RNG 读取思路，但必须适配固定 v0.111.0，且不能调用已否决的中途 `ToSave(CurrentRoom) → SetUpSavedSingleplayer → LoadRun` 链；
- 向用户显示稳定 `READY_*`、terminal 与停止说明。

至少完成：

- schema/serialization 和 create-new artifact tests；
- checkpoint identity、phase ordering、exact-mod-set、profile1-only、Cloud-off、first-terminal-wins tests；
- A 后 checkpoint 内容变化、B 前 checkpoint 不同、B 中非白名单 save diff、snapshot diff 的负向测试；
- Ready 后取消人工等待 timeout 的测试；
- source/binary forbidden-surface audit；
- Release build 0 warning/0 error 与安装 identity 验证。

不得 patch/替换 `SaveManager`、`ISaveStore` 或 production save 行为，不得自动 Continue、自动选择地图节点、出牌或结束回合，不得实现 restore/rebaseline helper。

## Harness repair envelope

在首次正式 A baseline 建立前，允许最多 2 次纯 pre-business harness 修订：只可修复 UI 展示、loader timing、artifact writer、字段采集或静态识别错误。每次必须提升 revision/attempt identity、保留旧证据并证明尚未建立正式 checkpoint baseline、未进入战斗、未调用 Continue、未触发任何 production save。

一旦建立正式 checkpoint baseline、进入 A 战斗、读取 B Continue，或发生任何业务/保护状态不明，repair envelope 立即终止并交回 Project Lead。不得自行重跑、恢复或 rebaseline。

## Phase 1：用户建立实验 checkpoint（正式基线之前）

Implementation Codex 先提供逐步人工测试请求。用户可在 profile1 中主动结束/替换原有测试 Continue，并创建一个固定 seed 的新单人 run；这是明确的 fixture 准备，会改变测试槽，不计入受测窗口。

目标是停在 Act 1 地图、尚未点击第一场普通弱怪节点的位置。probe 必须确认原生 `current_run.save` 可读取、`PreFinishedRoom == null`，并记录 seed、角色、act/floor/map node、文件长度/hash/UTC mtime及规范化的 profile1 路径。随后才原子建立正式 baseline：

- profile1 的完整目录 inventory；
- `current_run.save` 的原始 checkpoint identity；
- 其他固定保护文件的 hash/length/mtime；
- Continue canonical 与 `NumReloads`。

若准备阶段发生意外操作（包括进入错误节点、领取奖励或改变进度），必须记录并在 baseline 建立前重新准备；baseline 建立后不得 rebaseline。

## Phase 2A：uninterrupted A

用户仅执行：点击已记录的第一场普通弱怪节点，然后停止输入。

probe 在以下 readiness 同时成立并持续规定 quiet frames 后采集 A：玩家首回合、可正常交互、无待处理 action、手牌已发完、敌人/玩家状态稳定。沿用 SPIKE-001A 的具体 readiness 字段，并把各字段及 quiet-frame 计数写入 artifact，不能只写总布尔值。

snapshot 至少包括：

- act、floor、map coord、room/encounter stable id；
- 玩家 HP/max HP/energy；
- hand/draw/discard/exhaust 的有序 card identity，以及影响牌实例语义的可取得字段；
- 敌人的 stable identity、CombatId、HP/max HP 与 alive/dead/escaping 状态；
- 完整可取得的 `RunRngSet` entry stable name、state/counter/digest；
- readiness 原始字段与 snapshot tick/frame。

采集成功后 UI 必须明确显示：立即停止，不出牌、不点 End Turn、不打开选择界面、不领取任何奖励；由用户按请求正常退出游戏。

退出后 Implementation Codex 只读验证 A 使用的 `current_run.save` 内容仍与正式 checkpoint baseline 精确一致。mtime-only 也不自动接受；必须逐项报告。若内容或路径/inventory改变，分类 `BLOCKED_SOURCE_CHANGED_AFTER_A` 并永久停止，不实现恢复链、不进入 B。

## Phase 2B：normal Continue B

只在 A snapshot 完整且 A 后原 checkpoint 内容仍精确一致时解锁。使用全新进程；probe 必须在用户点击 Continue 前重新确认 active profile1、exact loaded-mod set、Cloud-off 以及 checkpoint hash/length与 A baseline一致。

用户只点击主菜单正常 Continue，随后不再输入。不得由 probe 调用 setup/load 或模拟按钮。

在与 A 完全相同的 readiness 条件采集 B snapshot。采集后立即停止：不出牌、不点 End Turn、不打开界面、不领奖；按 Implementation Codex 请求正常退出。

已知 production Continue 会执行 `IncrementNumReloads → SaveRun(SerializableRun)`。因此 B 阶段允许的持久变化只有：

- profile1 `current_run.save` 的 `NumReloads` 精确增加 1；
- 只有在 Phase 0 已通过固定 schema/IL 证明且报告明确列出的非游戏语义序列化元数据变化。

不得把整个 `current_run.save` 从保护集合中移除。必须解析 before/after 并证明除预声明字段外 canonical-equal。其他 profile1 文件内容/长度/inventory 不得变化；跨正常退出的 mtime 判断仍遵守 `TEST_PROTOCOL.md` 的固定白名单。任何额外差异为 `FAIL_ISOLATION_OR_PROTOCOL`。

## A/B 判定

预先允许从状态比较排除：`NumReloads`、wall-clock、artifact/log sequence、纯观察 tick/frame。不得在看见 diff 后扩大排除列表。

- `PASS_RECONSTRUCTION_EQUIVALENT`：A/B 所有规定游戏语义字段和完整可取得 RNG digest 相等；B save diff 仅含预声明允许字段；profile1 其他保护状态通过。
- `FAIL_RECONSTRUCTION_DIFF`：环境与协议有效，但任一规定游戏语义字段不同。记录首个 diff 和完整 diff，立即停止；不得追加修补/重演链。
- `BLOCKED_SOURCE_CHANGED_AFTER_A`：A 后无法证明同一 checkpoint 仍可供 B 使用。
- `FAIL_ISOLATION_OR_PROTOCOL`：出现非白名单持久变化、路径/profile漂移、额外 gameplay 操作或不明确的业务状态。
- `BLOCKED_ENVIRONMENT`：版本、hash、Cloud、loaded mod、profile、checkpoint 或证据门禁不满足。

只允许一个有效 A/B compare。得到任一 terminal 后不得再次运行本 probe。

## 明确非目标

- 不验证 Replay transaction、direct progress write 或 fault/restart；这些已有独立证据。
- 不测试中途战斗恢复、第二种房间、自然胜利、奖励、事件、商店或 Boss。
- 不 capture/dispatch 玩家操作，不实现快速重演、强记录、纠偏、seek、后退、正式 UI 或文件格式。
- 不切换/读取其他 profile，不恢复 checkpoint，不修改 production save 逻辑。
- 不调查 `Invalid Task ID`，除非它在本实验中与 terminal 形成直接时序因果；否则只记录为旁支现象。
- 不解锁或实现 `SPIKE-001B`。

## 交付物

- 源码与构建说明：`experiments/SPIKE-001A2-R2/`
- 不可覆盖证据：`evidence/spike-001a2-r2/`
- 报告：`project-management/investigation-reports/SPIKE-001A2-R2-report.md`

报告必须区分 Confirmed / Inferred / Unknown，列出环境与 binary identity、全部用户动作、baseline 建立时点、A/B readiness 原始字段、完整 diff、save canonical diff、保护审计、偏离与停止原因。

Implementation Codex 不得修改 `CURRENT_STATUS.md`、`ROADMAP.md`、`TODO.md`、`DECISIONS.md`、`MVP.md` 或 `EXPERIMENT_PLAN.md`。

## 交付与停止

Phase 0 完成后先回传静态验证和精确人工测试请求；不得自行启动游戏。每次需要运行时，明确告诉用户：动作、目标、预期结果、目标后立即停止动作、需回传内容和失败停止条件。

任一 terminal、结果不明或 repair envelope 终止后立即停止并交回 Project Lead。不得自行进入 `SPIKE-001B`。

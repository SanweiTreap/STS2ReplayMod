# SPIKE-001A3 Investigation Report

更新日期：2026-09-17  
状态：TERMINAL — `BLOCKED_CHECKPOINT_VALIDATION`；matching production Task completion后 harness floor gate失败；禁止修补或重跑

## 当前结论

全新 `SPIKE001A3` 的 Phase 0 实现、Release 构建、synthetic tests、INV-010 原 contract tests、source/IL audit 和安装生命周期方案均已完成。项目发起人完成 retired-probe disabled 验证后，A3 release pair 已 create-new 安装；游戏未由 Implementation Codex 启动或控制。R1 的 pre-business target-coordinate缺陷按 repair envelope修为R2；R2 随后命中 matching production save Task，但在正式baseline前因 floor target验证顺序缺陷进入永久 terminal。

因此当前只能确认静态契约、安装/loader gate、production null-checkpoint observer命中及terminal证据；不能声称 reconstruction equivalent/different。正式baseline、recovery、A/B snapshot均未建立。handoff 的 repair envelope 已终止，A3不得修改、替换、重跑、restore或rebaseline，也不得自行进入 `SPIKE-001B`。

## 固定 identity

- 游戏：STS2 public-beta `v0.111.0`，commit `41cef1ea`。
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- INV-010 report：SHA-256 `59D52227F5146253CEF41C5E93EDBC5AC4195EEE754CD4DCE6F39000F5FC61E0`。
- INV-010 contract：SHA-256 `54D99A392E5B113E78F770984100549E486A19D06145469E1D22BC54F1B02CF1`。
- 新 probe：ID `SPIKE001A3`；build `0.1.0-r1`；schema `spike-001a3.reconstruction-control.v1`。
- namespace：`spike-001a3-r1-{attemptGuid}`，artifact 与 recovery 均 create-new。

## Confirmed（Phase 0）

- Release build 0 warnings / 0 errors。
- A3 synthetic tests 30/30 PASS；INV-010 原 20 项 contract tests 20/20 PASS。
- 真实固定 `evidence/inv-010/reconstruction-contract.json` 已作为完整正向 preflight fixture；其 hash、runtime ID、schema、quiet frames、deadline 与 required field 数量均通过。
- 编译 IL 顺序确认 observer 先 await production Task，再启动 deadline，再 stable capture、写 create-new recovery 与 baseline；wrapper 参数为 by-ref `Task` result。
- compiled binary 没有 production setup/load/save/delete/profile-switch/restore mutation call。
- ticker 是直接 `NGame` child，显式 `ProcessMode=Always`、`SetProcess(true)`，实现 enter/exit/tree-exiting 与 bounded heartbeat。
- readiness 逐 site 记录 `value/null/exception`，异常聚合保留 type/message/count/first/last tick/time；event 与 heartbeat 均有固定上限、drop counter 与独立 last raw sample。
- 30 连续帧包含 required booleans/null/exception、target/object identity、engine process-frame continuity，且第 30 帧同 tick 完整重读。
- A deadline 从匹配 production save Task completion 开始；B deadline 从正常 Continue setup entry 开始。人工 Ready/arm 等待不启动 deadline。
- deadline owner 不依赖 ticker；零 tick与中途停 tick 测试均能触发 terminal callback。
- missing/unparseable checkpoint、Task fault/cancel、RoomEntered-before-baseline、first-terminal-wins、create-new 冲突、保护 inventory diff、B canonical 非白名单 diff均有负向测试。
- Release pair：DLL 169,984 bytes / `565981122880F11913976380738DAEB1A5E81D95B94E364BA90ECFE7670E504D`；manifest 349 bytes / `ADCE78A98561C06CD40C8DB86870644118C4A3484D6165CFD161CB9C0260C6A9`。

## Inferred

- 基于固定 production IL 与 compiled wrapper order，成功 runtime observer 应能在 `EnterMapPointInternal` continuation 创建房间前封存正式 baseline；仍需实际加载后的 runtime evidence 才能升级为动态 Confirmed。
- `ProcessMode=Always` 与 direct `NGame` ownership静态上满足跨 scene replacement 的持久 ticker 要求；实际 parent/path/inside-tree/tree-pause/first combat tick 尚待首次运行 heartbeat。

## Unknown

- A3 是否能在实际 mod loader/Harmony 生命周期中成功加载并显示 precheck UI。
- matching production save 是否被观察、两次 stable read 与 native parse 是否通过、正式 protection baseline 是否在 30 秒内封存。
- 每个 readiness field 的实际动态值与 exception summary。
- A/B snapshots、save canonical diff、protection audit 与 reconstruction equivalence 全部 Unknown。

## 用户动作 / attempt / revision

- Phase 0 用户动作：无。
- Runtime attempts：0。
- Pre-business code revisions：0/2；Phase 0 离线实现修正不计 runtime repair envelope。
- A3 已按 sealed release pair create-new 安装；没有 replacement。
- 旧 `SPIKE001A2R2` 仅作为只读历史证据；未修改、重建、安装、启用或运行。

## Checkpoint、baseline、telemetry 与 snapshots

- Checkpoint 命中：未运行。
- 正式 baseline 原子边界：仅完成代码/IL验证，未运行。
- Driver heartbeat：无 runtime evidence。
- Raw readiness / exception summaries：无 runtime evidence。
- A/B snapshots / full diff：无。
- Save canonical diff：无。
- Protection audit：无正式运行窗口。

## 安装与启用 gate

安装前只读 live inventory 显示 A3 不存在，retired project IDs 为 `SPIKE001A2R2`、`SPIKEINV004B`、`SPIKEINV005A`、`SPIKEINV006A`、`SPIKEINV006B`、`SPIKEINV007A`、`SPIKEINV008A`。

项目发起人已完成两次正常退出。completed log 为 260,175 bytes / SHA-256 `E50CD7818C18518F72CC97E5B4CC20007D404506B03BA2C9ABC19575FF29EFD2`，证明七个 retired project probes 均 persisted disabled且未加载。该日志同时显示 `AscensionUnlocker` 被加载。

游戏停止后，A3 pair 已 create-new 安装并复核为 DLL `56598112...0E504D`、manifest `ADCE78A9...60C6A9`；所有 pre-existing mod 文件保持 byte-identical。首次 A3 验证启动必须禁用 `AscensionUnlocker`、启用 A3，并由 runtime gate确认 exact loaded set `[SPIKE001A3]`。

首次 installed runtime precheck 已完成：completed log 184,512 bytes / SHA-256 `0F326987B4C6F5DBACE125B1D7A43B5E7642B8BCCA45692716E816BF1DC60FDB`；final loaded IDs 精确为 `[SPIKE001A3]`；UI/log 为 `READY_FIXTURE_PREP`；无 `BLOCKED_*`；正常退出。没有创建 attempt directory 或任何 business artifact。

## 偏离与停止原因

- 无工作包范围偏离。
- 没有启动游戏、读取当前 profile/save、切换 profile、恢复或 rebaseline。
- 当前按 handoff 的分阶段协议暂停在 `READY_FIXTURE_PREP`，只请求 pre-arm fixture 准备；不得提前 arm、建立 baseline 或进入 Phase B。

完整 Phase 0 命令、结果与安装计划见 `evidence/spike-001a3/phase0-validation.md`。

## 首次 fixture arm 澄清（User-observed runtime result）

- 用户明确确认：Ironclad、Act 1、floor 4 普通问号事件已经完成结算，已回到地图；floor 5 为尚未进入的第二个普通弱怪节点。
- 用户在该地图画面按 F8 后观察到 `BLOCKED_ENVIRONMENT`。此前“事件可能尚未完成”的推断被用户事实否定，不再沿用。
- completed log：194,582 bytes；SHA-256 `3AC9F63775AFB393973953E7CE15EEA47DD91E9A76591CF239C93116D1CE956A`。日志记录 `arm-time map checkpoint must be the existing non-null save`，随后正常 Quit/shutdown。
- 没有 `spike-001a3-r1-*` 目录、attempt、protection baseline、recovery、candidate、override、snapshot 或业务调用；本次仅为 pre-business fixture gate，不消耗唯一 A/B compare。

固定 DLL 的只读 IL 解释了现场结果：`EventRoom.OnEventStateChanged(EventModel)` 在参数不是 `AncientEventModel` 时于 `IL_0008` 直接返回；只有 Ancient event 且所有同步事件均 finished 时，才在 `IL_0045` 调用 `MarkPreFinished()`，并以 `this, true` 调用 `SaveManager.SaveRun(AbstractRoom,bool)`。因此普通问号事件完成后不会用 non-null pre-finished event save 覆盖进入节点前的 `PreFinishedRoom=null` checkpoint。用户关于“普通问号结算不形成该类退出检查点”的判断得到方法体支持。

该澄清不改变 A3 的业务假设、允许 I/O、保护边界或 PASS/FAIL 标准。handoff 明确允许在 arm 与正式 baseline 前准备第一幕普通弱怪节点；因此应把 floor 5 弱怪本身作为 fixture preparation，完成战斗及明确允许的奖励/返回地图操作，以取得 `CombatManager.EndCombatInternal` 的 non-null pre-finished combat save，再从后续可达普通弱怪中选择正式 target。若该路线没有后续可达普通弱怪，则退出并用新 run 准备，不得降低 arm-time non-null gate。

用户随后完成 floor 5 战斗，但下一直接可达节点为问号，因此该位置仍不能 arm。实现的 target predicate 精确为 `MapPointType.Monster`（普通怪地图节点，非 Elite/Boss），没有按 encounter pool 区分前三场 weak encounter。当前 run 只有在后续可选路线存在连续 `Monster -> Monster` 时才能继续准备：先完成前一个 Monster 生成 non-null combat checkpoint，再把后一个 Monster 作为正式 target。

项目发起人随后在新 run 完成了合适的连续普通怪 fixture，并观察到 `READY_A_NODE`。只读 artifact 验证确认 attempt `spike-001a3-r1-e370aaa23e494808b401f59ac59dd3cb` 已 create-new，且仅存在 `arm.json`：目标为 `Act 1 row 6 col 2 Monster`，seed `MJNVMGQT6KHA`，角色 `CHARACTER.IRONCLAD`；environment failures为空，loaded set精确为 `[SPIKE001A3]`，active profile为modded/profile1，Cloud gate通过。arm-time save为稳定可解析的 non-null checkpoint（48,446 bytes，SHA-256 `CAF342DC5E844A24B0B938A45F1299DFA473836FDBD912BCA462E2E6EFC75461`，`NumReloads=0`）。此时尚无正式 baseline、recovery、snapshot或terminal；Phase A node-click已解锁但尚未执行。

用户点击 R1 记录的目标后进入普通战斗，但 UI 未变化。只读检查确认 production null checkpoint 已写入，实际 visited coord 为 `row=2,col=6`，而 R1 arm target 为 `row=6,col=2`。根因是 R1 `FindCoord` 以错误的 `(row,col)` 参数顺序调用真实签名为 `(col,row)` 的 `ActMap.GetPoint`；postfix 与 RoomEntered guard 因而均未匹配。R1 attempt 仍只有 `arm.json`，没有 matching production Task completion observation、baseline、recovery、snapshot、deadline、terminal或正式业务窗口。用户未进行战斗动作并正常退出；完整 completed log 已封存为 203,843 bytes / `D227F46D...36BB3`。

该缺陷符合 handoff 的 checkpoint observer 未命中 repair envelope。第 1 次 pre-business revision 已完成：R2 build `0.1.0-r2`、schema `spike-001a3.reconstruction-control.v2`、namespace `spike-001a3-r2-{guid}`。唯一行为修复是保持 point lookup 与 `MapCoord` 的 `(col,row)` 顺序，并增加非对称 `(col=6,row=2)` regression。Release 0 warnings/errors；A3 tests 31/31；INV-010 tests 20/20；全部 source/IL audit通过。R2 pair为 DLL 170,496 bytes / `D3FE311B...2E7700`，manifest 352 bytes / `44720A49...EF0233`。

游戏停止后 R2 已替换 A3 自身 installed pair；其他 mod 文件不变，R1 release pair、arm-only attempt与log均保留。R2 尚未 runtime smoke，正式 baseline/A/B依旧不存在。完整修复证据见 `evidence/spike-001a3/r2-coordinate-repair.md`。

R2 主菜单 runtime smoke 随后通过：completed log 186,142 bytes / `1336AD9B...94211B`；final loaded set精确为 `[SPIKE001A3]`，UI/log为 `READY_FIXTURE_PREP`，无 `BLOCKED_*`，正常退出，且没有创建任何 R2 attempt。R2 fixture preparation现已解锁；正式 baseline/A/B仍不存在。

R2 attempt `spike-001a3-r2-10f3c963d34547f588ce5d4f1ea96379` 成功 arm `Act 1 row 2 col 0 Monster`；arm artifact确认schema v2、build r2、seed `3ELSPVNWJQQG`、Ironclad、profile1、loaded set `[SPIKE001A3]`及non-null predecessor save。用户点击唯一目标后出现黑屏并观察到 `BLOCKED_CHECKPOINT_VALIDATION / checkpoint.baseline / checkpoint seed/character/act/floor/map does not match armed target`，随后不进行任何战斗输入并正常退出。

终态已闭合：matching production null checkpoint为49,393 bytes / `ACE27172...1C1CB8C`，seed、character、act、`PreFinishedRoom=null`与last visited `row=2,col=0`均匹配。失败字段是 floor：arm target把 `TotalFloor`固定为`-1`；`ValidateTarget`先要求captured `FloorReached == -1`，而代码只有验证通过后才把target floor更新为captured值。因此合法checkpoint必然在更新前失败。attempt只含`arm.json`与`terminal.json`；无baseline/recovery/snapshot。completed log已封存为209,772 bytes / `DAE7A4A1...A1C105`。

由于 matching production Task completion 已被观察到，handoff明令 repair envelope立即终止。本结果分类为harness terminal，不是production reconstruction FAIL；reconstruction equivalence/difference保持Unknown。完整终态证据见 `evidence/spike-001a3/terminal-r2-analysis.md`。

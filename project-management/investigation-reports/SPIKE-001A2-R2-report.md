# SPIKE-001A2-R2 Investigation Report

更新日期：2026-09-16  
状态：Stopped — `FAIL_ISOLATION_OR_PROTOCOL`（operator disposition）；未建立正式 baseline，A/B compare 未执行；repair envelope 2/2 已耗尽

## 结论

本工作包没有得到 reconstruction equivalent / different 结论。R3 在正确环境中完成 pre-baseline arm，用户随后进入固定首场弱怪且未进行任何战斗输入；production 已创建 `SHRINKER_BEETLE_WEAK` 的 active combat，但 probe 等待约五分钟仍停留在 `READY_A_NODE`。它没有封存正式 `PreFinishedRoom=null` checkpoint baseline、没有写 A snapshot，也没有写 R3 terminal artifact。

由于正式 A 战斗已经进入，且两次 pre-business repair allowance 均已使用，handoff 要求立即永久停止本 probe。不得修订、重跑、恢复、rebaseline 或进入 B。最终 operator disposition 为 `FAIL_ISOLATION_OR_PROTOCOL`：A 进入后的业务状态不明确；该分类没有伪装成 probe 自己生成的 terminal artifact。

因此，`SPIKE-001A2-R2` 对“正常 Continue 是否重建出与 uninterrupted A 相同的首回合状态”的答案仍为 **Unknown**，不能解锁 `SPIKE-001B`。

## 固定环境与 artifact identity

- 游戏：STS2 public-beta `v0.111.0`，commit `41cef1ea`。
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- 最终 probe：ID `SPIKE001A2R2`，version `0.1.2-r3`，schema `spike-001a2-r2.reconstruction-control.v3`。
- R3 DLL：107,520 bytes；SHA-256 `167948B07F1342CC947B8A2D3C424234723A637ED05FEADD992222BDDD6C973C`。
- R3 manifest：355 bytes；SHA-256 `9206B520380132511DAD2AD57F279D92519A2F400702ED0BC00B83A0F312BA1D`。
- R3 运行 PID：7396；用户正常退出后已确认进程结束。
- 实际 loaded mod set：精确为 `[SPIKE001A2R2]`；日志记录 `Loaded 1 mods`。
- active profile：`modded/profile1`，profile ID 1；未读取、切换或比较其他 profile。
- Cloud gate：arm artifact 记录 account cloud `false`、app cloud `true`；production conjunction `cloudOff=true`，环境 failures 为空。
- 最终运行完整日志：186,853 bytes；SHA-256 `175C68C3E068CC781C120CCF0D5B2870E2F2530856B07DB27500E673B77AB5D0`。

## 用户动作与运行序列

1. 用户手动禁用 `SPIKEINV008A` 并正常退出；随后只启用 `SPIKE001A2R2`。
2. R1 loader 失败后用户正常退出；R2 smoke 后用户正常退出。
3. 用户在 R2 新建 Ironclad A0、seed `LY5SL6XM72T5`，完成捏奥选择并停在第一场弱怪前。F8 因当时 checkpoint 的 `PreFinishedRoom` 非 null 而正确显示 `BLOCKED_ENVIRONMENT`，随后退出。
4. 安装最终 R3 后，用户通过正常 Continue 返回同一准备状态，在 Act 1 地图、第一场弱怪前按一次 F8。UI 显示 `READY_A_NODE`。
5. R3 arm 创建时间为 `2026-09-16T03:38:11.4937987Z`。它记录 Ironclad、Act 0、floor 1、row 0/col 3、seed `LY5SL6XM72T5`；此时只是 pre-baseline arm，`PreFinishedRoomNull=false`。
6. 用户只点击已记录的第一场普通弱怪节点，随后停止输入；没有出牌、End Turn、选择或领奖。
7. production 写入 50,627-byte `current_run.save`，随后日志明确记录 `Creating NCombatRoom with mode=ActiveCombat encounter=SHRINKER_BEETLE_WEAK.`。
8. 用户等待约五分钟，UI 仍为 `READY_A_NODE`，随后按 Implementation Codex 请求正常退出。日志记录 `NGame.Quit called`，进程结束。

## Confirmed

### Phase 0 / 静态契约

- 最终 R3 Release build 为 0 warnings / 0 errors；synthetic tests 16/16 PASS。
- source/binary forbidden-surface audit PASS；最终 Harmony patches 只有 `NMainMenu._Ready` 与 `NGame._Input`。
- probe 不调用 `SetUpSavedSingleplayer`、`SetUpReplay`、`NGame.LoadRun`、`RunManager.ToSave`、production save/write/delete、profile switch 或 `SaveManager` replacement；不自动 Continue、不自动选节点、不模拟战斗输入。
- create-new artifact、phase/process ordering、exact mod set、profile1-only、Cloud-off、first-terminal-wins、A source change、B-pre mismatch、固定 `NumReloads +1`、非白名单 save change、snapshot diff 与 Ready 后无 timeout 均有测试覆盖。

完整离线证据见 `evidence/spike-001a2-r2/phase0-validation.md` 与 `phase0-il-contract.md`。

### Repair history

- R1：Harmony 试图 patch 不存在的 `NGame._Process`，initializer 抛出 `Undefined target method`。该失败早于 Ready、战斗、baseline 和 Continue；使用 repair 1/2。保留日志 SHA-256 `37CDF063C4FA751FD71257E3FAEC7A81FB2E9193EF4E70096054A735918A432A`。
- R2：改用挂在持久 `NGame` 下的自有 `ProbeTicker`。主菜单 smoke 通过；日志 SHA-256 `BD579C715AA6C4905DD4EB9C4C6C84155EBA46E4B99D32A5A44D0EF3A8A053DB`。
- R2 正式尝试：F8 发现地图 checkpoint 的 `PreFinishedRoom != null`，生成 `terminal-r2.json` 的 `BLOCKED_ENVIRONMENT`。没有正式 baseline、战斗、A/B snapshot 或 B Continue。该时序识别错误使用 repair 2/2；完整日志 SHA-256 `2F11D25C3413364DB0A9AF3AC0BE577328734A5A2D6529CF2861706FD5FEE8D1`。
- R3：F8 只 arm；设计为进入节点后、首个可操作 readiness 时要求并封存原生 null checkpoint，然后写 A。所有业务比较规则不变。没有剩余 repair allowance。

### R3 pre-baseline arm

唯一 R3 JSON 是 `arm-a-r3.json`：6,841 bytes；SHA-256 `6308DA190E1663A023C2D9BC38C386308C3AE34F24B69C86FDC1A67759A7E197`。

arm-time map checkpoint：

- path：profile1 `saves/current_run.save`
- length：50,791 bytes
- SHA-256：`8B33ACF71B0D488D57880E04A15DE1AC2D9C47DE3E6AC6877A24EF6A24650D04`
- canonical SHA-256：`3BDCD57F1AA1FE8404E55849C45C9DB2533500B71DEB74BB6D52599D104ECF98`
- `NumReloads=1`
- `PreFinishedRoomNull=false`

这不是 handoff 所定义的正式 checkpoint baseline。

### A-readiness stall 与停止

- 节点点击后，production save 的内容 identity 变为 50,627 bytes / SHA-256 `4ACD00603DC0F947D2E3969A486E142B963BD10E70ADB1A32E40C531BD360A39`。
- production 已创建 `ActiveCombat` 的 `SHRINKER_BEETLE_WEAK` 房间。
- 等待期间没有生成 `checkpoint-baseline-r3.json`、`snapshot-a-r3.json`、`post-a-checkpoint-verification-r3.json`、`b-precontinue-r3.json`、`snapshot-b-r3.json`、`final-audit-r3.json` 或 `terminal-r3.json`。
- 退出前 phase 仍为 AwaitA；返回/退出路径再次显示 `READY_A_NODE`，随后日志记录 `NGame.Quit called`。
- 没有正式 baseline，因此没有有效 A；B 从未 arm，用户从未执行 B Continue；有效 A/B compare 次数为 0。

完整运行证据见 `evidence/spike-001a2-r2/r3-a-readiness-stall-godot.log` 和 `r3-a-readiness-stall.md`。

## Inferred

- 50,627-byte production write 紧邻 map vote，且先于 active combat room creation；结合固定 INV-002 顺序，它很可能就是 R3 预期读取的 room-before checkpoint。然而 probe 没有在正式 capture 点读取、验证并封存它，故不能把此推断升级为正式 baseline。
- R3 没有 rejected-frame telemetry；持续 `READY_A_NODE` 表明 state machine 没有完成连续 30 个 candidate-ready frames。可能原因包括 ticker 未在 combat 中继续调用、`TryReadiness` 内异常被其 catch 静默转为 null，或至少一个 readiness predicate 一直为 false。这些候选无法由现有证据互相排除。
- 用户报告进入战斗并等待约五分钟，加上 production active-combat 日志，足以证明目标房间已创建；不足以证明每个 raw readiness 字段曾同时满足。

## Unknown

- 没有成功 snapshot，因此 A 的 raw readiness 字段（phase、player-turn membership、actions-disabled、pause flags、combat flags、executor flags/current action、hand count、quiet frames、frame ticks）全部 Unknown。
- 正式 baseline 的 `PreFinishedRoomNull`、canonical payload、protection inventory 与建立时点均 Unknown；正式 baseline 实际上从未建立。
- A 的位置、HP/energy、牌堆顺序/卡牌实例语义、敌人状态和完整 RNG entries/digest 未捕获。
- B 未运行，所以 B readiness 与 snapshot 全部 Unknown。
- 没有 A/B semantic diff，没有首个 semantic diff。
- 没有 B save canonical diff；预声明的唯一允许项 `NumReloads +1` 未被运行验证。
- 没有正式 baseline protection audit，也没有 A-post/B-pre protection audit。arm 前后可见的 production save、backup 与 replay 变化不能替代这些缺失的正式 artifact。
- reconstruction equivalent / different 仍为 Unknown。

## Protection observations（非正式审计）

R3 arm 捕获了完整 profile1 inventory。正常退出后的只读观察显示：

- `current_run.save.backup` 等于 arm-time map checkpoint content（50,791 bytes / `8B33...50D04`）。
- `current_run.save` 为节点点击后的 50,627-byte content（`4ACD...0A39`）。
- `replays/latest.mcr` 从 arm 时 2,042 bytes / `E651...C0D` 变为 5,981 bytes / `69EA...E1AE`。
- profile1 history 文件内容/identity 未变。
- `prefs.save` 与 `progress.save` 内容未变；正常退出使对应 backup mtime 前进。

这些变化位于 pre-baseline arm 与缺失的 formal-baseline capture 之间或正常退出阶段。由于正式 protection baseline 没有建立，不能把它们判为 A 后污染，也不能据此解锁 B。

## 偏离、分类与停止原因

预期：用户点击第一弱怪节点后，probe 在首回合可操作状态连续 30 quiet frames，先封存原生 null checkpoint，再写 A 并显示 `READY_EXIT_AFTER_A`。

实际：active combat 已创建，但约五分钟后仍显示 `READY_A_NODE`；无 baseline、A 或 terminal artifact。该偏离发生在 A 战斗进入之后，属于 handoff 所述“结果不明/业务状态不明”。repair envelope 既因 2/2 用尽而关闭，也因进入 A combat 而独立关闭。

停止规则：

- operator disposition：`FAIL_ISOLATION_OR_PROTOCOL`
- probe-emitted terminal：无
- 重跑/修复/rebaseline：禁止
- B Continue：禁止
- `SPIKE-001B`：不得解锁
- 后续决定：交回 Project Lead

## 文件与交付物

- 实现：`experiments/SPIKE-001A2-R2/`
- 证据：`evidence/spike-001a2-r2/`
- 最终报告：本文件
- 未修改 `CURRENT_STATUS.md`、`ROADMAP.md`、`TODO.md`、`DECISIONS.md`、`MVP.md` 或 `EXPERIMENT_PLAN.md`。

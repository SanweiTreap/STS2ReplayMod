# 当前项目状态

更新：2026-10-31。**CONTENT-006进行中，未验收整包。** Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)。当前候选/原片/步骤以[本包报告最新结果](investigation-reports/CONTENT-006-report.md)及其安装收据为准；不逐轮复制统计。

## 已确认的可用基础

- 固定v0.111.0；五角色A0首战代表机制、猎人后续普通弱怪/群怪录制与资源承接已有样本。中断前缀和新attempt独立封存，未把旧操作拼成新胜利。
- 原生出牌/用药/丢药/选择与恢复后续播；手动`>`细进、`>>`整项、Auto，Back恢复已访问整项/完整开场。选择页不是独立Seek点，无独立Play/Pause。详见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)。
- r5/r6的format10一步片/14步普通群怪片独立观看、小刀生成/使用后Back、胜利续播/Stop通过；r4 GrandFinale所测在途Back数据/视觉通过。均为版本/样本结论，不是全部内容或发布性能保证。
- 已完成房间跨进程续接、未完普通房间同进程新attempt已有证。观看原manager与所比较主Continue/progress恢复；r6备份文件变化单列，不称全部文件零写入。

## 当前正在完成

- A/B/C已按上述样本形成闭环；主要剩D敌方召援及未完战斗跨进程身份/新attempt。
- 实现侧最新r8报告：008准备修复后可正常开局，确有新进程、真实三次援军和50步胜利片；但自动分组仍误拆。009已安装，修正原生保存/加载表示的登记，**自动路径待实测**，显式旧索引校正不能替代它通过。
- r8召援录制、最新修版等仍待阶段验收；下一沿现成片核跨生成Back/再召/后点恢复/胜利续播，再以极短实际存退/重启补009自动关联。不重录50步或已答旧矩阵。精确索引与已确认/待测以报告为准。
- r7准备阶段失败/current_run缺失事实保留，猎人r6恢复点与后续测试恢复点仍在；不自动恢复，具体位置见报告。

## 随后的方向与主要缺口

CONTENT-006收束后，优先讨论[整局外壳与首条非战斗链](technical-notes/RUN-REPLAY-PLAN.md)：整局→房间访问→房间操作，先领奖/选路/营火/下一战斗。尚未签发该实现，不等全部战斗内容才考虑骨架。

全场景/难度/精英Boss/玩家失败、孵化复活/全部组合、跨房间导航、通用权威文件恢复/纠偏、未访问Seek、原始/分类节奏/倍速、路线图/录像库/导入与发布性能仍待。当前主要是观看内存恢复和房间前原生checkpoint，STATE-001只有旧单点冷恢复证明。多人MVP后优先，跨版本其次；接管不纳入MVP。

## 工作入口

- [任务与授权](handoffs/CONTENT-006.md)、[TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[工作流](WORKFLOW.md)。PM按阶段更新，普通bug由Implementation自主完成，不逐轮审批。
- 源码`src/ReplayMod`；固定反编译`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。历史按问题查报告/DECISIONS，不默认全读。
- D-092备份后自主实验/安装/短测授权保持；用户手动操作游戏，换DLL先正常退出，无Computer Use/替代GUI自动化/自行headless；Cloud按实况，另一账号排除。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)只存源码/文档，原始数据/备份/反编译留本地。Git一个index写入者、精确路径、不reset/clean/force；不擅自新建对话或改模型。

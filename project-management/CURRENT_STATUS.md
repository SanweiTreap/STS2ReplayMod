# 当前项目状态

更新：2026-10-31。**RUN-001进行中，005/r5首条原生链已通过，尚未整包验收。** Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)。范围见[工作包](handoffs/RUN-001.md)，当前版本/证据以[报告最新摘要](investigation-reports/RUN-001-report.md)为准。

## 已确认的可用基础

- 固定v0.111.0；五角色A0首战代表机制、猎人后续普通弱怪/群怪录制与资源承接已有样本。中断前缀和新attempt独立封存，未把旧操作拼成新胜利。
- 原生出牌/用药/丢药/选择与恢复后续播；手动`>`细进、`>>`整项、Auto，Back恢复已访问整项/完整开场。选择页不是独立Seek点，无独立Play/Pause。详见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)。
- CONTENT-006/r5/r6的format10一步片/14步普通群怪片独立观看、小刀生成/使用后Back、胜利续播/Stop通过；r4 GrandFinale所测在途Back数据/视觉通过。CONTENT-006按代表样本DONE_WITH_LIMITATIONS，三鼠召援前后恢复/原生重做及真实跨进程新attempt通过；旧失败与未测边界保留在其报告。
- 已完成房间跨进程续接、未完普通房间同进程及真正重启后的新attempt已有证；中断前操作不拼接。观看原manager与所比较主Continue/progress恢复；r6备份文件变化单列，不称全部文件零写入。

## 当前RUN-001阶段结果

- PM核对最新摘要与r5关键结果：同一真实录制路线分别以`>`细进和`>>`整项完成“首战胜利→金币/领卡→地图→营火升级Bash→选路→下一战”。用户确认正常，仅按该路线验收本段。
- 两轮后战完整初态/RNG匹配并执行原生Defend+，没有加载后场checkpoint或注入expected；后战是一步中断片，不是后战胜利。登记任务收束、Stop后原manager与所比较Continue/Progress恢复通过。
- 当前005保留；Auto整链、奖励/营火选择中Stop、休息、跳卡和合法保存点真正退出/新进程Continue仍待。同包先补Auto/Stop，再短样本合并其余，不追加旧五角色/长战斗矩阵。
- r3地图派生动作拒绝、r4按钮吞点击等失败原件保留。最近交接game0，用户正常游玩新增进度已补存保持，无restore；旧包及当前恢复点位置见各报告。

## 随后的方向与主要缺口

先完成RUN-001剩余分支与退出/重启验证，不将手动升级路线外推为全包通过。沿[整局规划](technical-notes/RUN-REPLAY-PLAN.md)，随后逐类接宝箱/商店/事件/跨幕，尚未派发；新非战斗Back/跨房间Seek仍禁用，已有战斗Back保持。

全场景/难度/精英Boss/玩家失败、孵化复活/全部组合、跨房间导航、通用权威文件恢复/纠偏、未访问Seek、原始/分类节奏/倍速、路线图/录像库/导入与发布性能仍待。当前主要是观看内存恢复和房间前原生checkpoint，STATE-001只有旧单点冷恢复证明。多人MVP后优先，跨版本其次；接管不纳入MVP。

## 工作入口

- [当前任务与授权](handoffs/RUN-001.md)、[TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[工作流](WORKFLOW.md)。PM按阶段更新，普通bug由Implementation自主完成，不逐轮审批。
- 源码`src/ReplayMod`；固定反编译`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。历史按问题查报告/DECISIONS，不默认全读。
- D-092备份后自主实验/安装/短测授权保持；用户手动操作游戏，换DLL先正常退出，无Computer Use/替代GUI自动化/自行headless；Cloud按实况，另一账号排除。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)只存源码/文档，原始数据/备份/反编译留本地。Git一个index写入者、精确路径、不reset/clean/force；不擅自新建对话或改模型。

# 当前项目状态

更新：2026-10-31。**RUN-002已授权启动：宝箱/商店/问号＋SL尝试归属，尚无本包新运行结论。** Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)。当前范围见[工作包](handoffs/RUN-002.md)；起点RUN-001/006按代表样本DONE_WITH_LIMITATIONS，身份/旧证据见其报告。

## 已确认的可用基础

- 固定v0.111.0；五角色A0首战代表机制、猎人后续普通弱怪/群怪录制与资源承接已有样本。中断前缀和新attempt独立封存，未把旧操作拼成新胜利。
- 原生出牌/用药/丢药/选择与恢复后续播；手动`>`细进、`>>`整项、Auto，Back恢复已访问整项/完整开场。选择页不是独立Seek点，无独立Play/Pause。详见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)。
- CONTENT-006/r5/r6的format10一步片/14步普通群怪片独立观看、小刀生成/使用后Back、胜利续播/Stop通过；r4 GrandFinale所测在途Back数据/视觉通过。CONTENT-006按代表样本DONE_WITH_LIMITATIONS，三鼠召援前后恢复/原生重做及真实跨进程新attempt通过；旧失败与未测边界保留在其报告。
- 已完成房间跨进程续接、未完普通房间同进程及真正重启后的新attempt已有证；中断前操作不拼接。观看原manager与所比较主Continue/progress恢复；r6备份文件变化单列，不称全部文件零写入。

## RUN-001验收与停点

- PM核报告及关键运行/源码片段：两条铁甲A0首幕普通短路线形成整局/访问归属与同一观看会话顺播。r5的`>`/`>>`领卡/升级链、r6 Auto及营火选牌中Stop、r9实际奖励选牌中Stop各有样本，不把单次运行当全矩阵。
- r10从首战Auto经跳卡、营火HEAL、选路进入后战，核对完整初态/RNG并完成两场胜利，再播放真正Continue后记录的第二场奖励/跳卡/回地图。状态由中间原生决策产生，没有加载后场checkpoint或注入expected；终点关Auto留画面，手动Stop后任务收束、原manager及所比较Continue/Progress恢复通过。
- r8真正新进程精确匹配已观察存盘，房间访问数保持、原场重进entries增加，既有操作/两片原件不变，只追加后续奖励。006补记Continue的操作边界并复用原生历史摘要刷新；旧文件只兼容无歧义已完成战斗重进，不承诺任意界面或多次SL。
- r3地图拒绝、r4按钮吞点击、r9历史摘要差异等失败原件保留，修复不追改。交接时game0，用户新进度与当前已知文件补存保持，无restore；本包无必需补测，不自动卸载，精确恢复点见报告。

## 随后的方向与主要缺口

用户已批准D-138/RUN-002合批：先分清成功save/load锚点与房间尝试，接宝箱/购物删牌/问号实际房型、事件选择及内部战斗返回。D-137先实现最终延续路线，保留全部尝试供后续完整经历模式；当前完整SL分支仍待实现。商店/宝箱/非战斗问号回到入口是用户经验，需短实测核对；含战斗问号分阶段验证。具体Unknown见[整局规划](technical-notes/RUN-REPLAY-PLAN.md)。新非战斗Back/跨房间Seek仍禁用，已有战斗Back保持。

全场景/难度/精英Boss/玩家失败、孵化复活/全部组合、跨房间导航、通用权威文件恢复/纠偏、未访问Seek、原始/分类节奏/倍速、完整路线图/录像库管理/导入与发布性能仍待。当前主要是观看内存恢复和房间前原生checkpoint，STATE-001只有旧单点冷恢复证明。多人MVP后优先，跨版本其次；接管不纳入MVP。

## 工作入口

- [当前任务与授权](handoffs/RUN-002.md)、[TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[工作流](WORKFLOW.md)。PM按阶段更新，普通bug由Implementation自主完成，不逐轮审批。
- 源码`src/ReplayMod`；固定反编译`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。历史按问题查报告/DECISIONS，不默认全读。
- D-092备份后自主实验/安装/短测授权保持；用户手动操作游戏，换DLL先正常退出，无Computer Use/替代GUI自动化/自行headless；Cloud按实况，另一账号排除。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)只存源码/文档，原始数据/备份/反编译留本地。Git一个index写入者、精确路径、不reset/clean/force；不擅自新建对话或改模型。

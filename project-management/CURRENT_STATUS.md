# 当前项目状态

更新：2026-10-31。**RUN-001已授权启动：整局外壳＋首条非战斗链，尚无新运行结论。** Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)。起点CONTENT-006/009按代表样本DONE_WITH_LIMITATIONS；当前新包范围见[工作包](handoffs/RUN-001.md)，版本/证据由其任务报告维护。

## 已确认的可用基础

- 固定v0.111.0；五角色A0首战代表机制、猎人后续普通弱怪/群怪录制与资源承接已有样本。中断前缀和新attempt独立封存，未把旧操作拼成新胜利。
- 原生出牌/用药/丢药/选择与恢复后续播；手动`>`细进、`>>`整项、Auto，Back恢复已访问整项/完整开场。选择页不是独立Seek点，无独立Play/Pause。详见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)。
- r5/r6的format10一步片/14步普通群怪片独立观看、小刀生成/使用后Back、胜利续播/Stop通过；r4 GrandFinale所测在途Back数据/视觉通过。均为版本/样本结论，不是全部内容或发布性能保证。
- 已完成房间跨进程续接、未完普通房间同进程及真正重启后的新attempt已有证；中断前操作不拼接。观看原manager与所比较主Continue/progress恢复；r6备份文件变化单列，不称全部文件零写入。

## 本阶段验收与停点

- PM核对报告及r9/r10关键证据，A/B/C/D既定代表样本闭环：后续普通战斗持续录制/真实资源承接、独立片段/完整片观看、Continue分组与一般敌方召援首条通路。
- r9：三鼠50步原片独立原生播放、生成前Back/再召、生成后已访问点恢复、同模型目标/击杀、胜利后Back/重做及Stop通过。仅为样本功能通过，不承诺所有召唤/变形复活或发布性能。
- r10：真正退出/新进程Continue，009登记原生写盘与读入的不同表示，自动保持同Run/同场、attempt递增；两份独立中断片开场相同，没有该局显式校正。r8旧错拆FAIL不追改。
- CONTENT-006不再补测/自动恢复。交接时游戏已退出、是未完三鼠测试局，正常新局存盘变化已记录并补存；r7准备失败/current_run缺失历史、猎人r6及后续恢复点保持，位置见报告。

## 随后的方向与主要缺口

用户已要求启动D-136/RUN-001：[整局→房间访问→房间操作](technical-notes/RUN-REPLAY-PLAN.md)，真实记录并在同一观看会话原生播放“战斗胜利→领奖/选卡→选路→营火休息或升级→下一普通战斗”。下一战状态/RNG需由中间决策自然产生，不能载入后场expected冒充链条。先短路线验证/简单节点展示；具体房间恢复时点与异步门控仍待实施验证，不先开串行INV。宝箱/商店/事件/跨幕随后接，本包不承诺任意跨房间Seek。

全场景/难度/精英Boss/玩家失败、孵化复活/全部组合、跨房间导航、通用权威文件恢复/纠偏、未访问Seek、原始/分类节奏/倍速、路线图/录像库/导入与发布性能仍待。当前主要是观看内存恢复和房间前原生checkpoint，STATE-001只有旧单点冷恢复证明。多人MVP后优先，跨版本其次；接管不纳入MVP。

## 工作入口

- [当前任务与授权](handoffs/RUN-001.md)、[TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[工作流](WORKFLOW.md)。PM按阶段更新，普通bug由Implementation自主完成，不逐轮审批。
- 源码`src/ReplayMod`；固定反编译`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。历史按问题查报告/DECISIONS，不默认全读。
- D-092备份后自主实验/安装/短测授权保持；用户手动操作游戏，换DLL先正常退出，无Computer Use/替代GUI自动化/自行headless；Cloud按实况，另一账号排除。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)只存源码/文档，原始数据/备份/反编译留本地。Git一个index写入者、精确路径、不reset/clean/force；不擅自新建对话或改模型。

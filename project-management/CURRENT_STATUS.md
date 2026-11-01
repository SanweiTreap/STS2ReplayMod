# 当前项目状态

更新：2026-11-01。**RUN-002进行中，007内部战斗完整顺播已有证，完成子战斗SL原生重建分支实施中、未实测。** Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)。范围见[工作包](handoffs/RUN-002.md)，版本/证据见[报告最新摘要](investigation-reports/RUN-002-report.md)；非整包验收。

## 已确认的可用基础

- 固定v0.111.0；五角色A0首战代表机制、猎人后续普通弱怪/群怪录制与资源承接已有样本。中断前缀和新attempt独立封存，未把旧操作拼成新胜利。
- 原生出牌/用药/丢药/选择与恢复后续播；手动`>`细进、`>>`整项、Auto，Back恢复已访问整项/完整开场。选择页不是独立Seek点，无独立Play/Pause。详见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)。
- CONTENT-006/r5/r6的format10一步片/14步普通群怪片独立观看、小刀生成/使用后Back、胜利续播/Stop通过；r4 GrandFinale所测在途Back数据/视觉通过。CONTENT-006按代表样本DONE_WITH_LIMITATIONS，三鼠召援前后恢复/原生重做及真实跨进程新attempt通过；旧失败与未测边界保留在其报告。
- 已完成房间跨进程续接、未完普通房间同进程及真正重启后的新attempt已有证；中断前操作不拼接。观看原manager与所比较主Continue/progress恢复；r6备份文件变化单列，不称全部文件零写入。
- RUN-001两条铁甲A0首幕短路线已按样本收束：领卡/跳卡、休息/升级、手动/Auto、合法完成保存点真正重启续录及两类选择中Stop；详见其报告，不外推全部内容/组合。

## 当前RUN-002阶段结果

- 宝箱r6/r8/r9已按白星样本验收：连续入口Load、相同/不同选择独立尝试与旧事实保留、最终领取/不拿、后战自然衔接及待选Stop/重开通过。D-139实际账户输入有判别验证，只更新隔离Progress、严格核局内资源/RNG；旧r4与首次seen来源未知保留。
- 商店r11/r13/r14已按代表样本验收：购物/折扣、删牌取消确认、真实入口SL改购而不混旧交易、自然后战及货架Stop/重开通过。D-139上下文在生成前应用，严格核发现列表/库存/价格/资源/RNG；删牌窗口中途Stop未单测。
- PM核报告及r17/r18/r19/r20关键证据，接受普通事件首增量：浴场多页/同名重复选项原生状态/RNG匹配，真实入口SL新尝试保留旧事实；ABSTAIN改选与DARK原生删牌自然产生后战初态/RNG及两场胜利，未载入后战checkpoint或注入expected。Unknown→Event走原生Roll，不强制房型。
- r20暗门删牌未确认时Stop及同进程重开一步Stop通过，取消任务正常收束、原context恢复；Complete=false是计划内提前退出。浴场初始选项Stop、商店删牌在途Stop、特殊布局/私有事件状态/派生遗物与额外奖励仍未全测，内部战斗/返回尚未接。
- r21旧片Unknown→Monster及LIGHT随机升级已验证；r24假人第二档自然入战、6步胜利、原生返回/随机升级与资源/RNG通过，未用子战斗checkpoint/expected。内部战斗Back、切换在途Stop等细分边界尚未单测，不外推所有内部事件布局。
- Implementation报告r25两次完成子战斗Continue均自动返回、无需重战；实际Load回到胜利保存时而非post-progress live，并重建事件共享状态。D-140允许仅在有证据的此类SL边界，用播放自身深冻结的原生保存快照重建Run/场景，保持隔离manager/账户输入；新分支未实测，不能沿用r24 PASS。
- 旧r15/r4等失败与原片保留。当前恢复点/正常进度依r25封存与报告为准，已补存无restore；旧Progress.backup变化单列，不泛称全部观看零写入。

## 随后的方向与主要缺口

当前优先复用旧录核D-140完成子战斗真实SL：原生保存点来源、重建前后状态/RNG与父事件返回/升级、任务收束/Stop；不重录长路线或重复已答矩阵。原包自主修测，D-139账户输入与世界Load分开处理。D-137最终路线先行，完整经历模式留后续；非战斗Back/跨房间Seek仍禁用。其余规划见[整局规划](technical-notes/RUN-REPLAY-PLAN.md)。

用户留存的未来方向：[原始界面输入/简化等效与Auto节奏](technical-notes/PLAYBACK-MODES.md)。现有商店开关等操作记录更接近原始输入过程；暂按实现方便继续，不要求现在拆分模式/补时间线或加验收，不排为紧接下一包的必做任务。

全场景/难度/精英Boss/玩家失败、孵化复活/全部组合、跨房间导航、通用权威文件恢复/纠偏、未访问Seek、原始/分类节奏/倍速、完整路线图/录像库管理/导入与发布性能仍待。当前主要是观看内存恢复和房间前原生checkpoint，STATE-001只有旧单点冷恢复证明。多人MVP后优先，跨版本其次；接管不纳入MVP。

## 工作入口

- [当前任务与授权](handoffs/RUN-002.md)、[TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[工作流](WORKFLOW.md)。PM按阶段更新，普通bug由Implementation自主完成，不逐轮审批。
- 源码`src/ReplayMod`；固定反编译`C:/Example/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。历史按问题查报告/DECISIONS，不默认全读。
- D-092备份后自主实验/安装/短测授权保持；用户手动操作游戏，换DLL先正常退出，无Computer Use/替代GUI自动化/自行headless；Cloud按实况，另一账号排除。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)只存源码/文档，原始数据/备份/反编译留本地。Git一个index写入者、精确路径、不reset/clean/force；不擅自新建对话或改模型。

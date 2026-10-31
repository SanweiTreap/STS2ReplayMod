# 当前项目状态

更新：2026-11-01。**RUN-002进行中，003宝箱首增量按白星代表样本通过，商店/问号待接。** Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)。范围见[工作包](handoffs/RUN-002.md)，版本/证据见[报告最新摘要](investigation-reports/RUN-002-report.md)；非整包验收。

## 已确认的可用基础

- 固定v0.111.0；五角色A0首战代表机制、猎人后续普通弱怪/群怪录制与资源承接已有样本。中断前缀和新attempt独立封存，未把旧操作拼成新胜利。
- 原生出牌/用药/丢药/选择与恢复后续播；手动`>`细进、`>>`整项、Auto，Back恢复已访问整项/完整开场。选择页不是独立Seek点，无独立Play/Pause。详见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)。
- CONTENT-006/r5/r6的format10一步片/14步普通群怪片独立观看、小刀生成/使用后Back、胜利续播/Stop通过；r4 GrandFinale所测在途Back数据/视觉通过。CONTENT-006按代表样本DONE_WITH_LIMITATIONS，三鼠召援前后恢复/原生重做及真实跨进程新attempt通过；旧失败与未测边界保留在其报告。
- 已完成房间跨进程续接、未完普通房间同进程及真正重启后的新attempt已有证；中断前操作不拼接。观看原manager与所比较主Continue/progress恢复；r6备份文件变化单列，不称全部文件零写入。
- RUN-001两条铁甲A0首幕短路线已按样本收束：领卡/跳卡、休息/升级、手动/Auto、合法完成保存点真正重启续录及两类选择中Stop；详见其报告，不外推全部内容/组合。

## 当前RUN-002阶段结果

- PM核报告及r6/r8/r9关键证据：真实连续入口Load保留同选择/不同选择的独立尝试，旧操作/原片不变；宝箱最终路线能领取或改为不拿，不把旧领取拼入。
- D-139在r6有判别性验证：旧首战未seen、新实际环境已seen，load与操作前Progress仅应用到隔离manager，不恢复局内资源/RNG，严格白星领取回放通过。r4原始FAIL及首次seen来源未知保持，不因新通过追改旧片。
- r7/r8不拿分支真实后战checkpoint为163金币/无白星，整链两场原生胜利、后战初态/RNG与终点Stop通过，无后场checkpoint/expected注入。r9白星待选中Stop及同进程重开一步后Stop通过，提前退出的Complete=false符合计划，不当整链失败。
- 仅白星普通宝箱代表样本；其他遗物/附加奖励/嵌套领取选择仍未全测。003保持，宝箱无需旧矩阵补测；交接game0，原进度第4层已完成未领奖，最新已知文件/新进度补存保持，无restore，具体恢复点见报告。

## 随后的方向与主要缺口

同包继续商店购物/删牌/取消与SL，再接问号实际房型、事件多阶段/选牌及内部战斗返回；沿D-138自主修测，不逐候选审批或重复宝箱长路线。D-139输入上下文方案复用，其他内容/时点仍按真实结果验证。D-137最终路线先行，完整经历模式留后续；非战斗Back/跨房间Seek仍禁用。其余规划见[整局规划](technical-notes/RUN-REPLAY-PLAN.md)。

全场景/难度/精英Boss/玩家失败、孵化复活/全部组合、跨房间导航、通用权威文件恢复/纠偏、未访问Seek、原始/分类节奏/倍速、完整路线图/录像库管理/导入与发布性能仍待。当前主要是观看内存恢复和房间前原生checkpoint，STATE-001只有旧单点冷恢复证明。多人MVP后优先，跨版本其次；接管不纳入MVP。

## 工作入口

- [当前任务与授权](handoffs/RUN-002.md)、[TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[工作流](WORKFLOW.md)。PM按阶段更新，普通bug由Implementation自主完成，不逐轮审批。
- 源码`src/ReplayMod`；固定反编译`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。历史按问题查报告/DECISIONS，不默认全读。
- D-092备份后自主实验/安装/短测授权保持；用户手动操作游戏，换DLL先正常退出，无Computer Use/替代GUI自动化/自行headless；Cloud按实况，另一账号排除。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)只存源码/文档，原始数据/备份/反编译留本地。Git一个index写入者、精确路径、不reset/clean/force；不擅自新建对话或改模型。

# 当前项目状态

更新：2026-11-03。**RUN-004按代表样本验收为DONE_WITH_LIMITATIONS，保留build-004。** 精英合法起点→Boss→原生换幕→Orobas/RadiantPearl→第二幕普通胜利，以及换幕Loading中Stop/重开已通过；D-147两次SL重建暗亮仍是暂留呈现偏差。当前无新增实现/必测，下一步先与用户讨论播放方式。证据/限制见[报告最新摘要](investigation-reports/RUN-004-report.md)。Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)，停在交接点。

## 已确认的可用基础

- 固定v0.111.0；五角色A0首战代表机制、猎人后续普通弱怪/群怪录制与资源承接已有样本。中断前缀和新attempt独立封存，未把旧操作拼成新胜利。
- 原生出牌/用药/丢药/选择与恢复后续播；手动`>`细进、`>>`整项、Auto，Back恢复已访问整项/完整开场。选择页不是独立Seek点，无独立Play/Pause。详见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)。
- CONTENT-006/r5/r6的format10一步片/14步普通群怪片独立观看、小刀生成/使用后Back、胜利续播/Stop通过；r4 GrandFinale所测在途Back数据/视觉通过。CONTENT-006按代表样本DONE_WITH_LIMITATIONS，三鼠召援前后恢复/原生重做及真实跨进程新attempt通过；旧失败与未测边界保留在其报告。
- 已完成房间跨进程续接、未完普通房间同进程及真正重启后的新attempt已有证；中断前操作不拼接。观看原manager与所比较主Continue/progress恢复；r6备份文件变化单列，不称全部文件零写入。
- RUN-001两条铁甲A0首幕短路线已按样本收束：领卡/跳卡、休息/升级、手动/Auto、合法完成保存点真正重启续录及两类选择中Stop；详见其报告，不外推全部内容/组合。

## RUN-004验收与当前停点

- PM核r6连续链与自算恢复关键证据/diff、r7换幕Stop证据及RunActs实际Task等待：铁甲A0，暗港精英→瀑布巨人Boss→唯一原生奖励投票/换幕→第二幕Orobas第二项RadiantPearl→ExoskeletonsWeak胜利，62战斗动作及12路线操作匹配，后两战初态/RNG自然生成。未读后幕checkpoint/注入expected；r2精英Back/重做、r3/r4真正跨进程Continue续录沿已有代表证据累积，不重跑旧矩阵。
- r7公开Stop在SetActInternal完成/第二幕地图已生成而EnterAct场景未完成时触发；8个owner任务完成、0取消/故障后恢复原manager/context及所比较Continue/Progress。再开同Boss原生一步匹配后Stop通过；三次Complete=false为计划内提前退出，不当失败或完整链重测。r2误报、r5完成Continue差异原FAIL保持。
- D-146只对有真实完成战斗Save/Continue锚点使用本次播放自算保存边界，两处精英/Boss恢复有证；不外推任意SL、第二幕内Continue、全部敌人/多阶段/古神/角色或完整通关。准备局Opening=null，所测连续链从精英合法起点，不当从涅奥整局已连通。非战斗Back/跨房间Seek仍禁用。
- D-147最终路线应剪掉SL/Continue重建观看过程，两次暗亮是暂留呈现偏差，后台必要恢复另算；未实施隐藏过场或拆播放模式，不当视觉目标已接受。本包按用户允许暂缓的边界收束，之后讨论展示方式。
- 交接game0、保留004唯一pair，无restore/另账号读取。原生point12第二幕首普通完成未领奖，root/三片不变；r7补存380数据文件与51已知当前文件至RUN004-post-r7-current-20261103-000240，相对r6仅settings.save.backup变化、无缺失，不称全账户快照或全部文件不变。

## RUN-003历史验收与停点

- PM核r1/r3/r5关键证据与相关源码diff：铁甲Standard A0、首幕新叶单张变形样本，奖励前冻结真实新局输入及Progress/Prefs，在隔离manager内调用原生新局工厂；原生候选、选牌、结果/RNG及自然首战初态、8步胜利匹配。未载首战checkpoint补演奖励，未注入expected；胜利Back/末击原生重做通过。
- 开局选择中Stop/重开、首战Loading/Opening中公开Stop与再次重开通过，已提交原生加载实际收尾、任务退出后恢复原manager/context及所比较Continue/Progress。r5是计划内提前Stop，不当006完整路线重测；r3完整链、r4音频接受及r5退出修复分别累积，旧失败不追改。
- 主页单一回放入口、整局列表/详情、新片从开局观看、旧局合法起点完整播放有实证；缺开场不补历史，分场/开发入口保留，mods/ReplayMod文件夹实际识别已确认。坏条目隔离有合同/代码证据，坏条目与独立分场按钮的全部实际GUI/焦点组合未专测。
- 准备只在显式开发开关用原生DebugOption替换新叶选项，该输入已录入/重做；自然无准备候选、其他涅奥奖励/选择窗口、多张变形、Neow SL及五角色新开局未实测。涅奥非战斗Back/选择页Seek/跨房间导航仍禁用。
- 交接game0、保留006，无restore。当前真实进度point4首战完成未领奖，root/clip保持；r5补存81数据文件及51已知当前文件至RUN003-post-r5-current-20261102-144015，相对r4仅settings.save.backup变化、无缺失，不称全账户快照或全部文件不变。

## RUN-002历史验收与停点

- 宝箱/商店按代表样本通过：连续入口SL、相同/不同选择独立尝试、最终领取/不拿或改购、折扣/删牌确认取消、后战自然衔接及待选/货架Stop。D-139实际账户输入仅更新隔离Progress、严格核局内资源/RNG；旧r4与首次seen来源未知保留。
- 普通事件的浴场多页/同名选项、入口SL改选、DARK删牌及后战、删牌未确认Stop通过；Unknown原生判定为Event/Monster、LIGHT随机升级各有证据。商店删牌在途Stop、浴场初始窗口Stop及特殊布局/额外奖励等仍未全测。
- 内部战斗普通顺播与D-140已测窄SL分支通过：先核战斗，再用本次播放原生保存边界的深冻结自算快照重建Run/父栈，保持同一隔离manager与严格核对，不读录制后战checkpoint/expected。009原生淡入后升级/飞回呈现获接受，008/r26视觉FAIL保持。
- PM核r29/r30及关键修复diff：r29/009两次完整路线自然进入下一普通战斗并胜利、内部三次Back/重做通过；其中快速队列两次FAIL保留。010将接受时列表位置与执行时位置分开，仍按同一原生TargetId/稳定CombatId严格核目标及结果/RNG，不禁原生排队/加卡名等待。r30三次实中旧重排条件、快速队列/胜利Back重做/重开Stop通过。
- PM核r31：新Run已绑定、原生Load未结束时公开Stop路径接受请求；已提交加载安全完成，同一隔离manager，任务/scene收束后恢复原context；随后重开原生一步Stop通过。两个等待升级显示任务取消不当动画完整，计划内Complete=false不当整链失败或本轮全链重测。
- 固定v0.111.0、单人A0首幕铁甲代表样本累积通过，旧失败不追改。商店删牌在途Stop、其他遗物/附加奖励、套购/自定义事件布局/全部切换时机仍未全覆盖。非战斗Back/跨房间Seek、跨幕、精英Boss、完整经历播放与通用文件恢复未开放。
- 交接game0，原进度point12下一普通战斗完成未领奖，root/4片与modded1本轮不变，无restore；modded2三项正常进度更新早于本次观看进程，单列并补存，不称全账户不变。settings/backup及旧轮备份变化依各报告保留；恢复点见r31终稿。

## 随后的方向与主要缺口

RUN-004已收束，不自行继续扩内容或追加测试。下一步先与用户讨论两种播放方式及SL呈现：路线看哪些尝试、界面过程是否简化、时间如何推进分别定义，不能把“最终路线”误当必须展示Continue重建；D-147当前目标已明确剪掉该观看过程，具体无缝衔接方案尚未实现。不自动派发新模式或隐藏过场任务，见[讨论材料](technical-notes/PLAYBACK-MODES.md)与[整局规划](technical-notes/RUN-REPLAY-PLAN.md)。

用户留存的未来方向：[原始界面输入/简化等效与Auto节奏](technical-notes/PLAYBACK-MODES.md)。现有商店开关等操作记录更接近原始输入过程；暂按实现方便继续，不要求现在拆分模式/补时间线或加验收，不排为紧接下一包的必做任务。

其余场景/难度/精英Boss与后续幕/玩家失败、孵化复活/全部组合、跨房间导航、通用权威文件恢复/纠偏、未访问Seek、原始/分类节奏/倍速、完整路线图/录像库管理/导入与发布性能仍待。当前主要是观看内存恢复和原生保存边界，STATE-001只有旧单点冷恢复证明。多人MVP后优先，跨版本其次；接管不纳入MVP。

## 工作入口

- D-141安装整理已完成，RUN-003实际确认目录识别：现用RUN-004/004固定`<游戏>/mods/ReplayMod/`、无根目录第二份；9套退休探针/不用的SBWEI/旧日志仍在游戏外归档。版本身份按当前报告/收据，见[安装维护](maintenance/MOD-INSTALLATION.md)。
- [当前任务与授权](handoffs/RUN-004.md)、[TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[工作流](WORKFLOW.md)。PM按阶段更新，普通bug由Implementation自主完成，不逐轮审批。
- 源码`src/ReplayMod`；固定反编译`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`。历史按问题查报告/DECISIONS，不默认全读。
- D-092备份后自主实验/安装/短测授权保持；用户手动操作游戏，换DLL先正常退出，无Computer Use/替代GUI自动化/自行headless；Cloud按实况，另一账号排除。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)只存源码/文档，原始数据/备份/反编译留本地。Git一个index写入者、精确路径、不reset/clean/force；不擅自新建对话或改模型。

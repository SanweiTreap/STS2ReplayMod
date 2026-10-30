# 当前项目状态

更新：2026-10-30。**CONTENT-006进行中（D-134）**：005/r3已证早晚两片原生完整观看/Back/胜利/Stop，以及已完成房间实际写→读→Continue；GrandFinale在途Back等待/声效/花瓣缺陷另列，007已装待同片短测。未来第4场/强普通/D及未完room attempt仍待，本包未完成。

## 玩家现在能用到什么

固定STS2 public-beta v0.111.0、仅ReplayMod；五角色A0首幕首排普通战斗的代表机制已有自动录制→文件封存→独立原生回放→Back/续播→退出证据，还不能录制/观看整局。
- CONTENT-006/001新增六份实际猎人录制（两局各三场、含首排后），95操作/10选择、胜利自动保存与生产读回通过；全部为Weak遭遇，不等于新片多场观看或所有普通强怪通过。
- 自动记录出牌、喝药、丢弃药水、结束回合和已接入的选择；胜利自动保存，从普通最近入口打开。
- 前进与Back后再前进继续原生牌效、动画、选择和RNG；已访问完整操作点用观看内存恢复，开场另用房间前checkpoint干净重入。
- 已测星星/锻造/可变牌、球队列/累计值、Osty成长/死亡重召/首次创建及Doom致胜。原生牌效复用不等于所有卡/遗物/药水组合都已验证。
- 观看退出后的原manager及所比较Continue/进度恢复，订阅按所测路径清理；不是所有异常/崩溃/Cloud环境的普遍安全证明。

## 本轮验收与实际边界

PM读取真实用户最终反馈、各轮报告/封存摘要与关键源码；未重复构建或重跑已回答矩阵。详见[CONTENT-005报告](investigation-reports/CONTENT-005-report.md)。

| 部分 | 已测机制与证据 |
| --- | --- |
| A 共用输入/旧资源 | 002/r1–r2：旧储君及新格式7片，丢药、初始选择、星星/锻造/复制费用、原生回放与恢复退出 |
| B 故障机器人 | 004/r3–r4：球容量/顺序/累计值/随机流、HUD/订阅恢复及玩家回合末球致胜Back续播；满槽跨步Back原日志缺口保留 |
| C Necrobinder | 008/r8–r9与009/r10：默认Osty/Doom逻辑、死亡重召血条；009/r11–r12：初始无宠物、5次跨不存在回退再原生创建、未来ID/新对象/显示订阅、胜利Back与退出重开 |
| D 开场 | 008/r7工具箱：可见原生入房间/开窗、精细/整段推进、开场整体Back、完整胜利Stop；ChoicesParadox未取实际样本 |
| E 在途Back | 011/r14：5次真正in-flight EndTurn Back，退役518–545ms、总到达564–645ms，用户体验通过 |

r14的14次原生结果/RNG包含Retiring6（5次被撤销EndTurn的自然收尾及Stop收束的最后9），5次恢复匹配；最后9在Stop收束中完成Doom胜利，Stop9无Error/Outstanding、Auto关/计划清空，原context恢复、Osty/牌监听余量0，无mod/late timer fault。不把最后一次计为Stop前普通前进。[r14摘要](../evidence/content-005/runtime-verification-r14.json)。
原生5类通用caller已实际绑定，退役只释放已审、当前会话/代/room的纯pacing计时；业务/Hook/抽牌完成回调及未知等待保持，正常播放/录制与原游戏速度不改。约半秒玩家准备仍保留；不是任意时刻瞬时取消。
011/r14没有独立Seek/短重开，010/r13相应证据仅属旧版；新旧触发时点未严格配对，不报固定提速百分比。006空场景、007隐藏入场、008/r9血条及010/r13体感失败均保留，未追判成功；引擎原始诊断不抹除。

## 当前控制（D-131/D-133）

- 默认Auto关、手动等待，没有独立Play/Pause。`>`仅手动等待时推进下一精细阶段，到点再等；选择页宿主Task未完仍可等待/浏览。
- `>>`立即请求完整操作，连点累计FIFO；选择中补完当前宿主，不自动追加下一张牌。Auto开连续；关Auto后已开始整动/显式队列自然收尾。
- Back清旧计划并关Auto，撤当前/最近完整操作；Seek清旧计划、按请求时Auto继续或等待。片尾关Auto，Stop清理退出。
- 首次可出牌0之前另有完整开场目标；Back开场以checkpoint重入，先显露新room、可见地自然走到初始选择后等待，无选择则到0。`>`细进，单次`>>`补完开场止0不出操作1。
- 选择页面不是直接Seek点/独立memento；普通选牌Back撤宿主，初始选牌Back撤整段开场。[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)为准。

## 尚未完成

- 后续普通战斗已有猎人录制样本，强普通/新片独立观看、Continue正确分组修复与未完成房间attempt仍待；更多难度、精英/Boss/事件战斗、奖励/商店/地图语义回放与整局，以及一般召唤/孵化/复活换形、玩家失败和全部内容组合未完成。
- 通用文件权威恢复/纠偏、未访问位置直接Seek。当前主要是观看内存；STATE-001仅证明首回合首Defend后单点冷恢复/受控纠偏，不代表所有持久化状态点。
- 原始/分类配置节奏、全局倍速、下一回合/楼层导航、时间轴/路线图、录像库与可移植导入。时间戳已落盘，原始节奏播放尚未完成。
- 发布性能、长战斗/整局容量与异常恢复；256操作/4MiB等原型上限不是发布保证，原PERF小pilot不替代预算。用户拖牌取消/凋萎互卡根因未复现。
- 多人为MVP后第一大型方向、跨版本第二；不可逆接管不纳入当前MVP。

## 当前推进：CONTENT-006

[工作包](handoffs/CONTENT-006.md)/[报告](investigation-reports/CONTENT-006-report.md)。首幕A0普通战斗持续录制/结果承接、Continue、逐场观看与三鼠召援同工程分段推进，相关bug同包修，无新INV/逐步审批。

001/r1真实两局各三份猎人Weak格式9胜利片，95操作/10选择、生产读回、资源承接与新进程后续录通过。首次Continue误拆/原生保存退出卡住保留。用户明确两局各三场、第二局SL；005显式校正journal视图/计数、六原片/catalog字段/native current_run未改，不当成原自动分组根因证明。r1首次差异具体字段仍Unknown；两组各1/2/3实际索引读回见校正收据，未来第4场仍待。

004/r2三次新片独立Initial/退出与所比较Continue/progress恢复通过，仅1原生操作；原生pause-save-and-quit在0关闭capstone/解除暂停，169.33ms后closed，用户确认正常。按此入口/点位验收，不要求日志微调后重跑，也不泛化全部在途/选择出口。

005/r3两进程662事件/668数据文件已封存，精确ArchiveRelative为runtime/content-006-r3-20261030-212412，以[摘要](../evidence/content-006/runtime-verification-r3.json)为准。第二局第1/第3场各从自身checkpoint Initial匹配，两片都实际完成全前缀到原生胜利。22结果/RNG=正常17/Retiring5，13Back状态/RNG/模式/清计划匹配；9终局均操作6（正常4/退役5），是两片首次/重做，不是九份独立整片。最后又Back，Stop4/5无Error/Outstanding、Auto关/Pending0，原manager及所比较Continue/progress恢复；最终AllOperationsMatched=false不否定此前胜利，也不冒充Stop在EOF6。无observer/playback/late-timer fault，原引擎诊断保留。

B的已完成房间写→读→Continue闭环有实证：005新进程21:06:48.396成功write→21:06:53.263 load→21:06:56.645后续Continue同键/canonical3fd匹配、FrozenLoad true、PreFinished true。PM核原摘要/时序，不用显式旧索引校正替自动关联证明；未完成房间newattempt、新强普通/D及未来第4场仍无样本。B该所测续接和C早晚原生观看/Back/胜利/Stop的数据逻辑闭环接受，完整产品/所有加载形态不外推。

GrandFinale体验缺陷仍有效：五次真在途Back退役156.8–1397.6ms、总499.2–1752.1ms，四次较长约1.46–1.75秒；稳定Back不混入，非配对时点不算固定百分比。花瓣/已撤销声效不能靠数值匹配判过。007复用纯timer退役，绑定真实PlayCard根Pending/room/gen，派生自动动作继承有效owner，原牌效/伤害/Hook/RNG完整执行；仅已审共享pacing及GrandFinale唯一1.625秒演出Wait，未知业务等待保持。两种factory视觉登记到统一清理，ExitTree取消自身视觉CTS；音效仍原生Play（保留pitch RNG/池）并按撤销owner线性音量0/精确id退役，不StopAll/全局加速/重写牌动画。006未运行即补007，不记用户新FAIL。

007只有构建0警告/错误与既有4组纯timer合同；新caller/owner、节点尾迹/音效池与实际延迟均待实测。PM核关键源码和固定音量API、报告/摘要/安装收据，不重复构建/读取个人存档。D格式10与准备已装但原生召援/新目标/跨生成Back/再召/旧点恢复/清理仍Pending，通用文件冷恢复未实现。

下一只同片推进5/6，整项出GrandFinale演出中Back 2–3次，核真正caller/释放、遮罩/已撤销声效/花瓣清理及结果RNG；最后正常操作6胜利Stop退出。不重录两局六片、不重跑已答B/C或004菜单退出。然后自然停点继续强普通/未来第4场、D及未完room attempt范围；整局外壳不加码。

## 整局外壳的时间安排（规划，未派发）

用户提出路线/问号/营火/宝箱/跨层应由统一外壳承载。当前RunId/战斗列表仅是地基，后场checkpoint承接战外结果不等于已记录前面的决策。PM建议紧接CONTENT-006优先做整局/房间访问骨架与“领奖→选路→营火→下一战斗”原生闭环，再逐类接宝箱/商店/事件、跨幕与跨房间导航；不等所有战斗覆盖完成。当前短测与本包四项目标保持，[规划与未知项](technical-notes/RUN-REPLAY-PLAN.md)已记录，尚未签新实现工作包。

## 工程、恢复点与协作

- 当前安装CONTENT-006/build-007，SHA256 `9BCECE95FF90DE51CB3A06B3E9B5F8069838C1E10F139D0FA43602D22926F0FF`，[收据](../evidence/content-006/preparation/build-007-1ee95b73.json)核pair/仅ReplayMod/modded1/安装时game0；候选不等于体验PASS。006/005及此前pair/config/六原片/失败与CONTENT-005/011恢复pair保留，无restore。
- 工程[src/ReplayMod](../src/ReplayMod/README.md)，当前代码/报告d298cd0；PM两份阶段性原文已单独提交6bd01e5。固定反编译C#：`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`，日常查源码，有歧义再查IL。
- 完整恢复点沿`C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，后续当前文件/安装pair补存见报告/收据，不称更新后的完整账号快照。未整包恢复，用户有新进度先补备份，另一账号不纳入。
- Cloud沿本批用户确认关闭；r3后补已知51文件/3,966,529 bytes至原恢复点CONTENT006-post-r3-current-20261030-212652，逐hash读回，不称完整新账号快照/云端保证。PM不重复读个人存档，不扫描另一账号/恢复旧基线。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)保存源码/文档；原始录像/日志/备份、依赖和游戏反编译缓存仅留本地。Git仅一个index写入者，提交精确路径，不reset/clean/force。[协作规则](REPOSITORY.md)。
- Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)接续CONTENT-006；PM提交工作包后移交Git index窗口，维护状态/规划/验收，不承担大规模源码实现；不新建用户对话或覆盖模型。
- D-092备份后允许同包查源码/hook/普通修复/安装和请求人工短测；默认modded1，unmodded1不再是禁止读取复制的禁区。实验权限不降低成品Continue隔离目标，不恢复逐步审批。
- 用户手动启动/操作游戏，不Computer Use、替代GUI自动化或自行启动headless；换DLL前正常退出。Cloud按实际确认，不自动更改；保留可用安装，不自动卸载。[WORKFLOW](WORKFLOW.md)/[TEST_PROTOCOL](TEST_PROTOCOL.md)。

## 长期记忆

- [TODO](TODO.md)、[Roadmap](ROADMAP.md)、[MVP](MVP.md)、[决策](DECISIONS.md)、[覆盖材料](technical-notes/CONTENT-COVERAGE.md)与[调查台账](INVESTIGATIONS.md)保存目标/计划；逐轮细节在报告和Git，不把历史Pending当当前待办。
- PLAYBACK-003按其样本收束，恢复后新卡登记监听已修；旧PLAYBACK-001/build-003仍Unknown，未补判。PLAYBACK-002固定前22、旧CONTENT/STATE/DEMO的版本/单点限制均保持，不能拼成当前版本全矩阵。

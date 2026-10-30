# 当前项目状态

更新：2026-10-30。**CONTENT-006进行中（D-134）**：001/r1六场录制/资源承接及新进程后续录有证；004/r2新片初态/退出恢复和原生保存退出0点位通过，无Back/完整胜利。当前005已装，旧误拆按用户两局说明显式校正索引；自动写后Continue/未来第4场/强普通/D仍Pending。本包未完成。

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

[工作包](handoffs/CONTENT-006.md)/[报告](investigation-reports/CONTENT-006-report.md)。首幕A0普通战斗持续录制/结果承接、Continue、逐场观看及三鼠召援同工程分段推进，相关bug同包修，无新INV/逐步审批。

001/r1三进程535事件录成两局各三份猎人格式9胜利片，95操作/10选择，生产精确读回通过，Preparation均null。后场HP/金钱/牌组承接，第一局牌组12→15→16，新进程Continue后的下一场录成。全Weak不冒充强普通；战外变化不全归因为领奖。首次错组/退出卡住、六原片与549封存数据保留。

用户确认实际两局各三场、第二局在问号/战后SL，PM直接读取原答。005据此及r1记录显式校正Replay journal：9ec7…延续3fd0…、场次偏移1；实际文件视图读回为两组各1/2/3，六原片/catalog原字段和native current_run内容未改，索引前后留存。这不是自动分组根因修复或Godot列表渲染证明；原始错组仍FAIL、首次实际变动字段Unknown。alias回归canonical、未来第4场/attempt计数只属源码/10组合同，原生待证。[校正收据](../evidence/content-006/run-index-correction.json)。

004/r2共82事件，三次battle-list独立打开新格式9（33a9…一次、ad557…两次），Initial均匹配；仅1完整原生操作/结果匹配，Stop1/0/0。三次Error null、原manager与所比较Continue/progress恢复；无Back/完整胜利/新录制/强普通/D。第三次原生pause-save-and-quit在0真实关闭capstone、Paused true→false、未普通保存，169.33ms后closed/隔离通过，用户确认正常。只验此入口/点位，不泛化全部在途/选择/异常出口。[r2摘要](../evidence/content-006/runtime-verification-r2.json)，86文件封存，原引擎诊断保留。

B仍缺闭环：r2有实际成功write与随后同键load，但Continue发生在write之前，没有该write后的下一次Continue。冻结加载DTO/成功保存实际store回读/captured RunId仍需后半验证；不能用首读001旧键、用户身份澄清或显式索引校正替自动写→读→Continue通过。未完成普通房间newattempt未取样。

005沿自然修订将退出日志等待字段按调用者显式传入，PauseMenu真/global业务fallback假，业务控制流不变；004已过点位不为日志重测。003仅运行前静态修订，不记用户FAIL。D格式10/身份/roster/AI-RNG/节点监听与可选三鼠准备已装，只有代码/离线；原生召援/新怪目标/跨生成Back/新对象再召/旧点恢复/清理仍Pending，通用文件冷恢复未实现。

下一先核005菜单两局各三场，优先第二局第1/第3场（均6操作）合并完整胜利/Back再做/Stop与切片隔离；当前局Continue地图存退→再Continue存退，取真实write之后的绑定。之后自然下一普通强怪/预期第4场及D，未遇召援另短用可选准备。不重录六片/旧五角色矩阵，不重复已过菜单退出，无新审批，整局外壳不加码。

## 整局外壳的时间安排（规划，未派发）

用户提出路线/问号/营火/宝箱/跨层应由统一外壳承载。当前RunId/战斗列表仅是地基，后场checkpoint承接战外结果不等于已记录前面的决策。PM建议紧接CONTENT-006优先做整局/房间访问骨架与“领奖→选路→营火→下一战斗”原生闭环，再逐类接宝箱/商店/事件、跨幕与跨房间导航；不等所有战斗覆盖完成。当前短测与本包四项目标保持，[规划与未知项](technical-notes/RUN-REPLAY-PLAN.md)已记录，尚未签新实现工作包。

## 工程、恢复点与协作

- 当前安装CONTENT-006/build-005，SHA256 `07CE397A9450ACB0D15E4B4B0630B7343B07668448F5EB7CE9A914ECE57E3E3F`，[收据](../evidence/content-006/preparation/build-005-f239a8aa.json)核pair/仅ReplayMod/modded1/安装时game0。004及更早pair/config、六原片/错组及CONTENT-005/011恢复pair保留，无restore。
- 工程[src/ReplayMod](../src/ReplayMod/README.md)，当前代码/报告bdb73e3，004退出5f4557f、分组读写6e9099d、D初始83a8e5e。固定反编译C#：`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`，日常查源码，有歧义再查IL。
- 完整恢复点沿`C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，后续当前文件/安装pair补存见报告/收据，不称更新后的完整账号快照。未整包恢复，用户有新进度先补备份，另一账号不纳入。
- Cloud沿本批用户确认关闭；r2后补已知51文件/3,966,529 bytes至原恢复点CONTENT006-post-r2-current-20261030-205256，逐hash读回，不称完整新账号快照/云端保证。PM不重复读私人存档，不扫描另一账号/恢复旧基线。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)保存源码/文档；原始录像/日志/备份、依赖和游戏反编译缓存仅留本地。Git仅一个index写入者，提交精确路径，不reset/clean/force。[协作规则](REPOSITORY.md)。
- Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)接续CONTENT-006；PM提交工作包后移交Git index窗口，维护状态/规划/验收，不承担大规模源码实现；不新建用户对话或覆盖模型。
- D-092备份后允许同包查源码/hook/普通修复/安装和请求人工短测；默认modded1，unmodded1不再是禁止读取复制的禁区。实验权限不降低成品Continue隔离目标，不恢复逐步审批。
- 用户手动启动/操作游戏，不Computer Use、替代GUI自动化或自行启动headless；换DLL前正常退出。Cloud按实际确认，不自动更改；保留可用安装，不自动卸载。[WORKFLOW](WORKFLOW.md)/[TEST_PROTOCOL](TEST_PROTOCOL.md)。

## 长期记忆

- [TODO](TODO.md)、[Roadmap](ROADMAP.md)、[MVP](MVP.md)、[决策](DECISIONS.md)、[覆盖材料](technical-notes/CONTENT-COVERAGE.md)与[调查台账](INVESTIGATIONS.md)保存目标/计划；逐轮细节在报告和Git，不把历史Pending当当前待办。
- PLAYBACK-003按其样本收束，恢复后新卡登记监听已修；旧PLAYBACK-001/build-003仍Unknown，未补判。PLAYBACK-002固定前22、旧CONTENT/STATE/DEMO的版本/单点限制均保持，不能拼成当前版本全矩阵。

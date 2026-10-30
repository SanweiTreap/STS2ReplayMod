# 当前项目状态

更新：2026-10-30。**CONTENT-006进行中（D-134）**：A/B/C首个候选build-001已实现、离线检查通过并安装，三场连续录制/Continue/列表的原生实测Pending；D三鼠召援尚未接通。本包未完成。CONTENT-005按所测范围DONE_WITH_LIMITATIONS，011可用pair与既有结果保留，不追加旧矩阵。

## 玩家现在能用到什么

固定STS2 public-beta v0.111.0、仅ReplayMod；五角色A0首幕首排普通战斗的代表机制已有自动录制→文件封存→独立原生回放→Back/续播→退出证据，还不能录制/观看整局。
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

- 后续房间/Continue/更多难度、精英/Boss/事件战斗、奖励/商店/地图与整局；一般敌方召唤/孵化/复活换形、玩家失败及全部内容组合。
- 通用文件权威恢复/纠偏、未访问位置直接Seek。当前主要是观看内存；STATE-001仅证明首回合首Defend后单点冷恢复/受控纠偏，不代表所有持久化状态点。
- 原始/分类配置节奏、全局倍速、下一回合/楼层导航、时间轴/路线图、录像库与可移植导入。时间戳已落盘，原始节奏播放尚未完成。
- 发布性能、长战斗/整局容量与异常恢复；256操作/4MiB等原型上限不是发布保证，原PERF小pilot不替代预算。用户拖牌取消/凋萎互卡根因未复现。
- 多人为MVP后第一大型方向、跨版本第二；不可逆接管不纳入当前MVP。

## 当前推进：CONTENT-006

用户已授权[CONTENT-006](handoffs/CONTENT-006.md)：首幕A0普通战斗持续录制（含后续强怪/群怪）、正常奖励后的状态承接、Continue后的录制、按局/楼层选择旧战斗独立观看，以及三鼠敌方召援的原生前进/跨生成Back。同工程分段交付，普通相关bug同包修，无新INV/逐步审批。

001实现格式9的逐场/attempt元数据、Replay自有存档指纹journal分组、独立checkpoint及轻量战斗列表；正常旅行与Continue的saveGame:false入口均已接入。5组真实journal/Codec/library合同、7组开场政策及旧Osty生产读回通过，未运行游戏生命周期/节点，不能据此称多场/Continue/隔离通过。PM核源码、报告与安装收据，未重复构建/未读私人存档。[本包报告](investigation-reports/CONTENT-006-report.md)。

Implementation已请求一段正常三场游玩：前两场领奖，第二场后回地图存退并退出进程，新启动Continue完成第三场后退出；先核实际文件/奖励承接/分组，再安排较早/较晚列表回看。无需PM另批或重复一轮。D目前只定位TwoTailedRatsNormal/TwoTailedRat，未扩一般敌方roster；如本轮遇召援按未完成机制同包处理、保留素材，不把用户正常游玩算协议错误或偷偷排除此目标。

这比原“两场”候选更完整，但不等于整局地图/奖励语义回放、跨房间Seek、Boss/孵化/复活/跨幕、其他难度或通用冷恢复。Continue重进须有真实房间前起点并标newattempt，不能拼未完操作；至少验证下一受支持战斗续录。实际方案/覆盖待Implementation验证，任务细节以工作包为准。

## 工程、恢复点与协作

- 当前安装CONTENT-006/build-001，SHA256 `061EC2212B4E0A6B30D7153FA6FCBD8BC89E1D887935EE1823128BF46F572593`，[收据](../evidence/content-006/preparation/build-001-61568ba9.json)核pair/仅ReplayMod/modded1/game0；这不是原生运行证明。CONTENT-005/011可用pair及旧片/失败保留，任务间build编号勿混。
- 工程[src/ReplayMod](../src/ReplayMod/README.md)，CONTENT-006代码/首报0990558。固定反编译C#：`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`，日常直接查源码，有歧义再查IL。
- 完整恢复点沿`C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，后续当前文件/安装pair补存见报告/收据，不称更新后的完整账号快照。未整包恢复，用户有新进度先补备份，另一账号不纳入。
- 本批用户向Implementation确认Cloud关闭、无新进度；001前沿原恢复点补存已知51文件/3,953,223 bytes，报告及`evidence/content-006/pre-abc-current-files.json`记录。PM未为重复核验重读存档；不称新增完整账号快照或云端保证。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)保存源码/文档；原始录像/日志/备份、依赖和游戏反编译缓存仅留本地。Git仅一个index写入者，提交精确路径，不reset/clean/force。[协作规则](REPOSITORY.md)。
- Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)接续CONTENT-006；PM提交工作包后移交Git index窗口，维护状态/规划/验收，不承担大规模源码实现；不新建用户对话或覆盖模型。
- D-092备份后允许同包查源码/hook/普通修复/安装和请求人工短测；默认modded1，unmodded1不再是禁止读取复制的禁区。实验权限不降低成品Continue隔离目标，不恢复逐步审批。
- 用户手动启动/操作游戏，不Computer Use、替代GUI自动化或自行启动headless；换DLL前正常退出。Cloud按实际确认，不自动更改；保留可用安装，不自动卸载。[WORKFLOW](WORKFLOW.md)/[TEST_PROTOCOL](TEST_PROTOCOL.md)。

## 长期记忆

- [TODO](TODO.md)、[Roadmap](ROADMAP.md)、[MVP](MVP.md)、[决策](DECISIONS.md)、[覆盖材料](technical-notes/CONTENT-COVERAGE.md)与[调查台账](INVESTIGATIONS.md)保存目标/计划；逐轮细节在报告和Git，不把历史Pending当当前待办。
- PLAYBACK-003按其样本收束，恢复后新卡登记监听已修；旧PLAYBACK-001/build-003仍Unknown，未补判。PLAYBACK-002固定前22、旧CONTENT/STATE/DEMO的版本/单点限制均保持，不能拼成当前版本全矩阵。

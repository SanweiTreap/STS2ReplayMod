# 当前项目状态

更新：2026-10-30。**CONTENT-005按所测范围DONE_WITH_LIMITATIONS**，保留已安装build-011。用户确认r14“这次效果很不错。过了！”，游戏已正常退出；本包不再追加候选或矩阵，当前无已派发的下一任务。

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

## 下一项建议（未派发）

优先讨论把“只录首战”扩到同一真实对局连续两场普通战斗：各房间真实起点/奖励后状态承接、自动封存与独立回看，先形成日常游玩可用增量。它不等于整局语义回放；敌方召援（三鼠等）仍是另一重要机制候选。先后/范围待讨论，不创建新对话或自动启动新实现/测试。

## 工程、恢复点与协作

- 当前安装CONTENT-005/build-011，SHA256 `E222994401F1B3BD89465E540645031BF7214C531EE05F5A81F9D40FD7F321A9`，[安装收据](../evidence/content-005/preparation/build-011-ef2b7c26.json)；009可用pair、010及更早失败/原片保留，任务间build编号勿混。
- 工程[src/ReplayMod](../src/ReplayMod/README.md)，本包报告提交2618324。固定反编译C#：`C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src`，日常直接查源码，有歧义再查IL。
- 完整恢复点沿`C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，后续当前文件/安装pair补存见报告/收据，不称更新后的完整账号快照。未整包恢复，用户有新进度先补备份，另一账号不纳入。
- [公开仓库](https://github.com/SanweiTreap/STS2ReplayMod)保存源码/文档；原始录像/日志/备份、依赖和游戏反编译缓存仅留本地。Git仅一个index写入者，提交精确路径，不reset/clean/force。[协作规则](REPOSITORY.md)。
- Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)本包已结束写入/测试，等待下一目标。PM管状态/规划/验收，不承担大规模源码实现；新建用户对话须明确要求。
- D-092备份后允许同包查源码/hook/普通修复/安装和请求人工短测；默认modded1，unmodded1不再是禁止读取复制的禁区。实验权限不降低成品Continue隔离目标，不恢复逐步审批。
- 用户手动启动/操作游戏，不Computer Use、替代GUI自动化或自行启动headless；换DLL前正常退出。Cloud按实际确认，不自动更改；保留可用安装，不自动卸载。[WORKFLOW](WORKFLOW.md)/[TEST_PROTOCOL](TEST_PROTOCOL.md)。

## 长期记忆

- [TODO](TODO.md)、[Roadmap](ROADMAP.md)、[MVP](MVP.md)、[决策](DECISIONS.md)、[覆盖材料](technical-notes/CONTENT-COVERAGE.md)与[调查台账](INVESTIGATIONS.md)保存目标/计划；逐轮细节在报告和Git，不把历史Pending当当前待办。
- PLAYBACK-003按其样本收束，恢复后新卡登记监听已修；旧PLAYBACK-001/build-003仍Unknown，未补判。PLAYBACK-002固定前22、旧CONTENT/STATE/DEMO的版本/单点限制均保持，不能拼成当前版本全矩阵。

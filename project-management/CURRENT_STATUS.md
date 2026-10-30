# 当前项目状态

更新：2026-10-30。**CONTENT-005进行中**：A共用输入、B机器人、D开场按各自样本闭环验收；当前008保持，**下一C骨妹代表录制/回放**，尚无Osty/Doom运行结论。D的008/r7可见开场/Back/完整胜利/Stop通过，ChoicesParadox与独立短重开未测；006/007失败保留。

## 玩家现在能用到什么

已有受支持首战的自动录制→普通最近入口→原生完整播放→后退续播→退出重开闭环，还不能录制/观看整局。

- 自动记录出牌、用药、结束回合及已接入的选择，胜利自动保存；普通入口不再固定HunterClip或前22项。
- 原生牌效、选择界面、动画和RNG继续执行；直接恢复已访问的完整操作边界后可原生续播，不播放可见的从头追赶。
- 手动等待/Auto自动推进两模式，公开原生播放器没有独立Play/Pause。选择阶段可细进/浏览，但不是直接Seek目标。
- 所测胜利Back、重复恢复后生成并使用小刀、Stop及短重开通过；退出后原manager及所比较Continue/进度恢复。此结论限于已测路径。

## 本轮验收与版本边界

PM核报告、真实用户确认及r8封存732原事件：3次recent打开，50次原生结果/RNG匹配（含1次导航退役中的自然收尾），3次真实胜利，7次恢复状态/意图匹配。修复后2次补5个登记监听、5次附加0；最终恢复后生成小刀ID18/19，generation10的操作10/11各匹配一次并实际用牌。Stop在21/11/1，均无Error/Outstanding，监听清理0/5/5→0。见[r8摘要](../evidence/playback-003/runtime-verification-r8.json)。

r6/r7的恢复后选牌失败保留：PM已核完整异常栈及固定源码，确认胜利解绑NetCombatCardDb登记监听，旧memento恢复ID表但未重挂监听。007补缺失原生回调/退出纯退订，不重编号、不跳过核对；实际修复已由r8验证。它不代表所有可变状态/事件订阅都已覆盖。

008按用户要求在播放末端清Auto及待播余量，显示“播放结束”；PM读代码差异和更新后的4组终点合同。运行证明仍来自007，不称008另有真人矩阵；低影响结束状态小改无需重复整场。既有引擎启动/退出诊断不抹除。

## 当前控制规则（D-131）

- 打开默认Auto关、手动等待；`>`仅手动等待时推进下一细阶段，到点再等。选择页虽宿主Task未完也可等待；忙/Auto开时不新增Fine。
- `>>`点击即请求完整操作，连点累计FIFO；选择页补完当前宿主的剩余选择/牌效，不自动追加下一张牌。
- Auto开连续推进；关Auto停止自动追加，已开始整动和显式队列自然收尾，不另保留执行中Pause功能。无需Play或隐式WholeContinuation。
- Back清旧计划、按完整操作恢复后转手动/Auto关；Seek清旧计划，按请求时Auto继续或等待。片尾自动关Auto，Stop退出清理。
- D-130及更早冲突规则留作历史，当前以[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)和D-131为准。

## 尚未完成的产品目标

- 固定STS2 v0.111.0，仅ReplayMod；候选已接五角色A0首幕首排普通战斗、无额外run modifiers，Continue不录。三旧角色及机器人代表组合已验，Necrobinder仍未实测，不能称五角色或全内容运行完成。
- 后续房间、精英/Boss、事件战斗、奖励/商店与整局；Necrobinder/Osty/Doom实际覆盖、更广球/宠物、新单位/一般复活换形、玩家失败等。
- 全卡牌/遗物/药水覆盖。原生模型负责效果，但输入、身份、私有状态、订阅与恢复仍需按共用机制补齐；丢弃药水是已确认的输入漏项。
- 通用文件强记录/纠偏及未访问位置的直接Seek。当前后退主要用观看内存；STATE-001只证明新进程读取首回合首张防御后的一个文件状态点及受控纠偏，不是每步完整文件恢复。
- 原始/配置节奏播放、速度控制、回合/楼层导航、时间轴/路线图与录像库。时间戳已记录，不代表原始节奏模式已实现。
- 发布性能、长战斗/整局容量；当前256操作/4MiB等原型上限和小pilot不算发布保证。用户报告的拖牌取消/凋萎互卡根因仍未复现。

## 当前任务：CONTENT-005 五角色基础战斗支持

用户在两角色合批安排后明确要求开始，签发[CONTENT-005](handoffs/CONTENT-005.md)给现有Implementation。开头短补共用输入/旧三角色实际缺证（初始选择、储君资源/可变牌、遗物药水、丢弃药水），随后同包推进故障机器人的球与Necrobinder的Osty/Doom；旧片优先复用，不先做全面旧内容回归。宠物必要单位身份/生命周期与Doom致死后的恢复纳入，其他召援/多阶段Boss后续。

用户现确认当前这轮测试结束就改开场，D-133排为下一项：横幅尾不额外阻断就绪控制、开场选择接>/>>、增加可Back到的完整战斗开场。现有人工轮先收束，不把尚未开始的全部角色矩阵作为前置；同包D段已发给Implementation，无需再批。选择页仍非独立Seek，重入原生开场的恢复方式/耗时待实测，具体见体验规格。

D的007/46e1121准备历史：移除初始化前gate，干净重入后到选择Ready才揭罩；保留新manager/旧loop退休及精确核对。离线合同通过后进行了r6，逻辑与视觉结果见下，不能继续把“待首轮”当当前状态。

007/r6已核[最终摘要](../evidence/content-005/runtime-verification-r6.json)：92事件/94数据文件封存，2次重入含选择中Back，其中一次原生再选后Initial/0匹配；1次结果/普通Back和Stop0比较状态恢复，Godot正常退出。无全胜利/短重开，视觉被用户拒绝。PM核遮罩链与原生room Setup顺序，确认真实入场被罩住且OneShot/Shake静默至ChoiceReady；规格已补“房间进入→原生选牌入场→等待”的可见过程。选择中Back仍撤宿主/整开场，不新增选择Seek。

D的008/r7按工具箱片DONE_WITH_LIMITATIONS：PM核报告/[封存摘要](../evidence/content-005/runtime-verification-r7.json)，未重计204原事件。3次重入先显露room（ChoiceReady=false/原生效果允许/未加请求），后选择Ready；用户确认可见入房间/原生开窗和选择中Back。3次Initial匹配且未带op1，随后11原生结果/RNG及真实胜利；Stop11无Error/Outstanding、Auto关/清计划、原context恢复，4旧loop退休、牌监听5/5/5/0→0。显露事件约287–345ms、选择Ready约1627–1696ms只是本轮观测，不作发布保证。无独立短重开/ChoicesParadox/Osty样本；原片不变，报告fea9ca6，008保持，接回C。

D的006/r5已核[摘要](../evidence/content-005/runtime-verification-r5.json)：131事件、3工具箱/1Defect打开；首开Fine/Whole完成初始选择核对且未带操作1，首项丢药/普通Back0有1次结果匹配。两次开场Back均停在StartCombatInternal前裸场景，用户拒绝；重入后没有新请求/再选择/Initial重新核对，不追判通过，327/295ms也不是正确落点的成功耗时。4次Stop比较状态恢复、134数据文件封存。预载Invalid Task ID诊断保留，未认与空场景同因。007修复仍待实测，不因无异常验收D。

仍限当前单人A0首战，分段交付；两角色各少量代表录像走录制/普通入口/原生前进/关键Back续播/退出，权威文件恢复缺口如实区分。D-131控制保持，D-092允许同包修复、备份安装并直接请求用户人工测试；不另建前置调查或逐步审批。上一轮只讨论的限制已由本次“开始”指令解除，未创建新对话。

A的002/r1：PM核[报告](investigation-reports/CONTENT-005-report.md)与[运行摘要](../evidence/content-005/runtime-verification-r1.json)，未重复构建/重计278原事件。旧储君参考入口完整13项、两次胜利Back12再做，15结果/RNG、3胜利、2匹配恢复；Stop13无Error/Outstanding、原context恢复、监听Remaining0。新格式7片76f55fed77ab448b9ff318b9d50eff98录成11项/2选择并胜利封存/生产读回；首项Slot2丢药仅令两处药水槽位置空，工具箱初始选择及星星/王剑复制/跨牌堆锻造事实已有文件证据。r1未打开新片，后续r2补此闭环，不将旧参考入口冒充新recent证明。r1摘要/报告提交1b7b31b。

A的r2已按所测范围验收：PM读真实用户“测试完毕，符合预期”、报告和[封存摘要](../evidence/content-005/runtime-verification-r2.json)，未重计276原事件。两次recent、16结果/RNG、4匹配Back（0/0/9/8）、1真实胜利，丢药与锻造恢复后原生再做通过；Stop11/1无Error/Outstanding、计划清空、原context恢复，牌堆监听0/5→0。两次开场原生选择自动完成后初态匹配。开场横幅/选择控制及整体回退现按D-133排队，具体见[体验规格](technical-notes/PLAYBACK-EXPERIENCE.md)；恢复方式/延迟待验证，不追判本次A失败。

B/C基础源码B提交ccbbcde、C提交1235aaa，004准备3a7f711；B恢复/显示/订阅已有r4运行证明，C的Osty/Doom路径仍仅代码/离线、未取样。球/单位权威冷文件恢复未实现。003曾安装但未发测，004仅调整准备提示以避免提前胜利漏机制；不把它与后续D-133候选混用。

B的004/r3–r4已按样本验收：r3格式8片dfa22d43c501492786b19cacee1411c7录成9项，含暗球12→20、槽3→5、随机球流0→2与玻璃/冰ID5/6。r4的[封存摘要](../evidence/content-005/runtime-verification-r4.json)/报告经PM复核，未重计697原事件：2次recent、28结果/RNG、18匹配恢复、4真实球胜利；三次胜利Back8后以新信号/原生阶段续播，未重跑开场/回合开始。18球显示恢复及20次退场监听零余量；Stop9/1无Error/Outstanding，原context恢复，原片不变。满槽激发第3项只有首次前进原日志，跨该项Back/重做仅用户反馈，保留边界而不补旧矩阵。此证明限玩家回合末电球致胜，不代替C的Doom敌方回合结算。花粉核心旧bug/全遗物及冷文件恢复未覆盖；报告已随b095f5a保存。

材料已交[内容覆盖清单](technical-notes/CONTENT-COVERAGE.md)和[契约/候选批次](technical-notes/BATTLE-COVERAGE-PLAN.md)：固定源码索引只作检索，不当覆盖率。知识恶魔、三鼠召援、产卵虫/卵、千足虫、实验体已定位，尚非Replay运行PASS。多人为MVP后首要大型方向，跨版本亦在MVP后；接管游戏不纳入当前MVP。

## 工程、恢复点与工作方式

- 工程：[src/ReplayMod](../src/ReplayMod/README.md)；固定版本反编译C#留本地，日常直接查源码，IL仅在有歧义时核对。
- 当前安装CONTENT-005/build-008，SHA256 `792B181B68CDB8267F3565EDC09B030352E019C63472623482FED3E5C3200024`，[收据](../evidence/content-005/preparation/build-008-e48f6522.json)，D工具箱样本已实测，C待测。007/006失败及对应pair/日志、004/002/旧PLAYBACK-003/008恢复pair保留；任务间编号勿混用。
- [公开Git仓库](https://github.com/SanweiTreap/STS2ReplayMod)保存源码/文档；原始证据、存档录像、备份、依赖及反编译缓存留本地，见[协作规则](REPOSITORY.md)。
- 完整恢复点沿`C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，A后51当前文件/候选pair已补存，见本包报告，不冒充更新后的完整账号快照。未整包恢复；用户有新增进度时先备份，另一账号不纳入。
- Implementation：[Replay Mod 后退一步](codex://threads/01a17814-d254-7df0-8c01-affe43d0e04c)，当前CONTENT-005；PM管状态/规划，不承担大规模源码实现。
- D-092有效：备份后允许较大胆的mod/hook与同包普通修复，不为每个失败退役代码或另开调查/审批。默认modded1；unmodded1不再禁止读取/复制，备份权限不降低成品Continue隔离目标。
- 游戏由用户手动启动/操作，不用Computer Use、替代GUI自动化或自行启动headless；换DLL前退出，Cloud由用户现场确认，不自动改。保留可用安装，不自动卸载；见[WORKFLOW](WORKFLOW.md)/[TEST_PROTOCOL](TEST_PROTOCOL.md)。

## 证据与长期记忆入口

- [PLAYBACK-003报告](investigation-reports/PLAYBACK-003-report.md)与[工作包](handoffs/PLAYBACK-003.md)记录逐版本/逐轮边界；r1–r7失败/未知不追认成功，不续跑旧probe。
- 历史PLAYBACK-001/build-003仍Unknown，未读其结果补判；PLAYBACK-002/build-003按D-127前22项验收保留。它们与当前PLAYBACK-003任务编号不同。
- 旧内容/状态结论见CONTENT-001至004、DEMO-004/005、STATE-001报告；逐轮细节查报告、DECISIONS及Git历史，不把旧控件待办当当前执行清单。
- [TODO](TODO.md)、[MVP](MVP.md)、[Roadmap](ROADMAP.md)、[决策](DECISIONS.md)、[调查台账](INVESTIGATIONS.md)保存长期规划。

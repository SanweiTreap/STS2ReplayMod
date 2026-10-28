历史快照：D-092精简/放宽授权前的项目管理文档；不作为当前执行权限。

# 当前项目状态

更新日期：2026-10-26

## 工作恢复

2026-10-24暂停，2026-10-26用户恢复。DET-001保持关闭；PERF-001 Phase0现已独立验收为ACCEPT_PERF_MEASUREMENT_CONTRACT（D-091）。同包Phase1 `PERF001/candidate-r1` 离线构建已派发原Implementation，工具确认接收，状态IN_PROGRESS；工作包 `handoffs/PERF-001.md`。安装/配置/玩家数据读取/游戏运行继续HOLD，不请求用户测试。

用户报告已在modded侧切到profile1并关闭所有模组，记录为准备信息而非持久/actual loaded/Cloud/进程门已通过。本阶段不读当前玩家数据。未来对照必须保持modded上下文，不能以全部关闭模组启动而切入个人unmodded1。正常游玩后的配置/测试槽不要求符合旧基线，未来按当时状态准备，不restore旧档。

## 当前阶段

阶段 1：关键技术假设验证。

当前仓库尚未包含 Replay Mode 的正式实现源码。INV-002至INV-012、003A局部会话管理/Stop、007A-R3 direct progress writes、008A-R6 controlled owner-fault/normal-restart及012A窄诊断已验收。A5初始重建与001B两操作控制已关闭；**DET-001现已通过一个真实首次自然重洗样本的五点/四操作/完整RNG状态A/B等价，并完成安装收尾，D-089正式关闭。** 其他场景、性能、强记录恢复、seek和完整产品会话仍未证明。旧A2/A3/A4不重分类，A4动态活性首因仍Unknown。

`SPIKE-INV012A` **runtime已验收为ACCEPT_DIAGNOSTIC_PASS**。r6主菜单启动虽identity通过却0 ticker帧；在同包内窄修为SceneTree.ProcessFrame与GD.Print，独立runtime-r1候选在真实fresh process完成普通Monster入房、20字段30连续帧capture-ready与正常退出writer观察。PM直接核对14份原始记录hash/白名单/同进程provenance/monotonic顺序，独立重跑.NET9合同38/38与dummy非干扰；candidate/bin/live身份一致，r5/r6产物保留。

012A收尾已验收：两次attempt共17份记录归档hash匹配，两个精确live安装文件已移除，workspace候选和原始证据保留。收尾时settings的enabled条目未改；随后A5准备已在原账号关闭该残留。文件存在、持久启用和实际loaded仍分别验证。

`SPIKE-001A5`有效attempt为`3e6aaa0160e441c293b5ac4a0030b6d0`，固定v0.111.0/41cef1ea、原账号modded1、only A5 runtime-r1、Standard Ironclad A0、seed JFZS2H4V4XN2、Act index0/row1 col2、CORPSE_SLUGS_WEAK。相同`PreFinishedRoom=null` checkpoint hash `BD4E2D804E5929224301BFE0AB564BC1322B3E7B2FFE8A987C8DE4A0D734B100`，Floor1→两端playable Floor2/round1。A退出后、B配置后及fresh B主菜单前输入仍同字节；正常Continue B的同输入门与完整capture/commit成立。

用户已持续授权PM消息协调（D-069）；用户说明实现对话已由gpt-6 sol high切到gpt-6.1 sol xhigh，按用户报告记录，消息不覆盖模型设置。PM只维护文档与离线验收，不实现探针或自主启动游戏。

A process=`92576:639281931959340480`，fresh B=`105216:639281953241414060`；两端均20字段全True/30连续帧/max gap34ms，在各自20s期限内commit，无terminal。两份8088-byte semantic同hash `DF34448C39CCEBC0DAE1F1C76006656D11BF00766A0B3A1EDB2A15EA506B4717`，PM独立逐字节和JSON结构比较相同，Differences=[]。405份交付文件及旧索引24/331/326份hash通过。正式已Arm2/3：旧轮用户目标前中断、第二轮有效；不追加第三轮。验收及只读复现：`evidence/spike-001a5/project-lead-runtime-review-2026-10-22.md`。

A5收尾已验收：用户报告B正常退出，PM复核进程=0、两个本包live文件不存在、18个其他旧probe文件不变。workspace source/sealed pair、原始证据及private active/checkpoint保留。持久disabled和剩余29份installed manifests均disabled以绑定收据/脚本为据，PM未重读账号settings/private备份；按当前配置下次启动预计进入unmodded树，未启动验证。**下次项目测试必须先建立有效modded配置，A5不得再运行。**

另一账号始终不纳入任务，不读取/改动/删除，也不要求用户先删除。10月21日某旧日志归属的绝对prefix比较已被纠正：`user://`逻辑路径不匹配不能证明另一账号；实际A/B进程后来使用account token/ActiveUser/旧probe scope重新匹配，验收不依赖该旧推断。

唯一个人保护目标仍是**unmodded profile1**，PM/Implementation未读取或改写它。A5/001B普通Continue只是可消耗测试槽control，不是正式Replay架构。正式headless未验证，INV-013不执行；001B已完成并收尾，不再运行。

## 当前下一步与验收门

**DET-001已于2026-10-24最终验收并关闭：`ACCEPT_SINGLE_SAMPLE_RNG_REPLAY_EQUIVALENCE / CLOSED`（D-089）。** 一个真实固定版本/内容样本从同room-before CP，经A人工和fresh B自动Defend→EndTurn→Defend→EndTurn，五个完整稳定点与五个自然重洗内部边界一致。正式Arm1/3，剩余预算不用，不再运行DET001；拟议r4/准备Continue未实施。

样本为v0.111.0/41cef1ea、原账号modded1/only DET001、candidate-r3/schema2、attempt af99c3b9c7bf411882d580c2f3565408、seed20FRCAL12Q8Y、Ironclad A0、GoldenPearl＋BurningBlood/原十卡、SHRINKER_BEETLE_WEAK/row1col0。A process58312:639283459045976987与fresh B81240:639283673562637803，CP Floor1→playable2。五semantic为7545/7544/7584/7584/7583 bytes，逐字节及结构全等；两端各四条实际accepted/completed，B四dispatch均在前一点stable后，20字段/30连续帧窗口成立。

真实自然重洗E0…E4、原Shuffle/Draw Tasks成功；每端10实例成员/语义、hook顺序、S3/E1、E2/S4和最终手/抽堆链接成立，五规范化内部边界全等。Run.Shuffle9→18及完整四state/digest匹配，PM另以独立BigInteger九步校核，不只比较counter。A87/B90＋activation1共178份原记录、63/59事件及完整commit/hash链已验，补上001B样本实际未消费已投影RNG的缺口。

最终331文件索引`evidence/det-001/runtime-delivery-2026-10-24.json`，SHA-256 `C89647402FF43D777F8778137B7D64C71021593280F39F7CC98F171F4D412E38`。PM执行公开只读Verify-RuntimeDelivery通过，18份旧索引列示4657目标及6额外输入保持；不重建或重跑144。最终验收`evidence/det-001/project-lead-runtime-review-2026-10-24.md`，PM final validation同目录。Implementation最终报告/索引封存，不由PM改写。

收尾e8dd68e34a294cce9bfdde1e54084531通过：当前game0，DET001/退役probe持久disabled，两个exact安装DLL/json已移除且workspace可恢复，其他18项目文件保持；剩余29 installed项有效启用0以已绑定收据为据，PM未重读当前账号/private/native。首次移除因只读属性中断时配置已禁用；Finish只精确回收两文件，未重放配置变更，独立收尾核验通过。源码/sealed pair、private CP/备份、全部原始/失败/准备证据保留。下次预计unmodded只是配置推断，未启动验证；下一次项目测试须另建有效modded配置，不接触个人槽。

A退出→B准备→fresh B Continue前CP45534 bytes/hash `AD987823B7FBA7260542A6951DEB3594928EF717B0942E141B9956FB1AA80DD2`保持。normal Continue后native45864 bytes/hash `8ACC64483C20D284F37F78F518EBA3AEC7FD445791E9D283CA31242CA952379F`、NumReloads0→1在声明reload/save窗口；B结束/退出/离线收尾保持，私有原CP始终保持。不声称330-byte差异仅来自该字段，也不声称全程零写入、restore或产品隔离通过。

当前只确认本样本的冻结投影/四操作/首次自然重洗可重演；其他卡组/遗物/敌人/版本、多次重复稳定性、未投影状态、真实偏差后的恢复纠偏、性能、seek、时序和完整独立Replay会话仍未证。其他五项Neow仅离线资格证据。r1泛型harness故障按r2窄修证据记录；旧renderer loss根因及早期F8无记录原因仍Unknown，不把关闭另一游戏与Arm成功认作因果。普通Continue是可消耗测试槽control，不是最终玩家流程，阶段1未完成。

PERF-001 Phase0已验收：64项/5公开输入、18块/9000正式原始样本/6文件的内容顺序及排空通过，PM独立重算9块统计、核对11源码绑定并复现349行/5方法固定IL。未重新build/23功能测试/计时或读取玩家数据，原始64项交付保持。验收 `evidence/perf-001/project-lead-phase0-review-2026-10-26.md`，只读复现同目录Verify-ProjectLead-Phase0.ps1。

代理早期Memory/Queue约104ms、后续38–42ms，尚未建立稳态；约7.6KB DTO代理没有live采集/真实引擎帧/持续磁盘压力，不是FPS或G-02。D-091只放行独立候选离线实现三模式/轻量采样/实际边界/自有冻结及有界writer，不安装、不配置、不请求游戏。Custom同seed无modifier仅有静态入口依据，实际解锁/内容/loaded/modded1仍待后续runtime门。STATE/NAV/产品不自动解锁。

## 产品方向

制作一个真正面向玩家使用的《Slay the Spire 2》本地 Replay Mode：尽量不影响正常游戏性能地记录单人对局，之后允许玩家观看 replay。多人 Replay 不属于当前 MVP，但已确定为 MVP 完成后的第一产品优先级。

首条已确认的 MVP 要求：模组启用后，受支持的单人对局默认自动录制，不要求玩家逐局手动开启；同时提供关闭自动录制的全局设置。完整产品基线见 `MVP.md`。

第二条已确认要求：性能是默认自动录制能否发布的门槛；完整单人场景覆盖仍是产品目标。首版若有临时缺口，必须明确标注并继续补齐，不能把“性能优先”解释为长期只支持少量场景。

第三条已确认要求：正常录制不显示常驻状态图标或实时文件大小；仅在录制关闭、启动/中途失败或遇到明确不支持场景时通知玩家。

Replay 管理页的 MVP 支持删除记录和打开文件所在目录；自定义命名延期，首版使用角色、时间、seed、进度/结局等元数据自动命名。

Replay 管理与观看必须从主菜单专门入口进入，产品层级与“单人游戏”“多人游戏”并列。正式玩家流程不得要求创建/切换 Replay profile；已有单人 Continue 在打开列表、观看、停止、失败和重启后必须保持原状。底层隔离机制尚未决定。

项目运行测试默认使用已全内容解锁的modded profile1专用测试槽；经工作包明确指定，也可使用modded profile2/3。三者的状态变化按实验有效性管理，不再作为必须恒定的个人进度保护目标。唯一须保护的个人对局在**不加载模组的profile1**，实验不得切入或改动它。Implementation Codex 不使用 Computer Use 或其他 GUI 自动化控制游戏，而是提交包含具体动作、目标、预期结果、目标后动作、回传证据和停止条件的测试请求，由项目发起人手动启动和操作游戏。完整协议见 `TEST_PROTOCOL.md`。

观看侧已确认暂停/继续、按玩家操作 Step、速度调整和停止退出均属于 MVP。项目发起人进一步将“前进/后退”列为重要能力，并要求出牌间隔、选卡界面停留等不同操作节奏可通过 config 分类型调整；Counter-Strike demo 系统作为产品体验参考。

前进/后退的 MVP 产品粒度已确认包含三层：上一/下一玩家操作、上一/下一结构节点、可点击或拖动时间轴。UI 采用双层导航方向：房间内操作时间轴负责微观定位，类似成绩结算页的路线图负责房间/楼层宏观定位。参考截图已固定到 `evidence/product-references/`。

MVP 将记录语义操作时间戳，并同时提供两种节奏：默认的分类 config 压缩节奏，以及按记录间隔调度操作的“原始节奏”。时间戳是否足以达到预期精度仍是待验证假设；当前不承诺逐帧复原动画/渲染时序。

原始节奏的计时边界已确认：保留游戏聚焦、未暂停、非加载且可正常交互时的玩家思考时间；排除暂停菜单、窗口失焦、加载画面和可识别的模组/系统阻塞。

从 replay 当前状态接管并继续游戏明确不属于 MVP。RunReplays 式不可逆转换（接管后 replay 直接变成普通对局且无法返回）被记录为不采用的产品行为；未来若重新考虑接管，必须先定义分支隔离、返回 replay、存档与统计语义。

MVP 采用“强记录”方向：除语义操作外记录足够的权威状态用于校验和恢复。发现可恢复 divergence 时自动用记录状态纠偏、继续播放并输出结构化错误日志；只有记录状态本身无法读取/恢复时才停止。具体状态范围与密度仍是待验证设计，不能牺牲录制性能门槛。

不可恢复兜底已确认：权威状态本身无法读取或恢复时停止并显示明确错误。跨版本播放不属于 MVP，而是 MVP 完成后的独立大型目标；MVP 仍必须记录精确构建、Replay 格式和模组/内容元数据，为未来兼容保留基础。

其他模组的 MVP 边界已确认：正式保证原版内容、Replay Mod 及必要依赖；其他 gameplay mod 兼容延期。Replay 必须记录全部启用模组及版本，gameplay mod 环境不匹配时明确警告或拒绝；纯 UI/外观模组只做尽力兼容。

同环境跨机器分享与导入属于 MVP：玩家可把 replay 文件复制给另一台机器并从管理页导入；正式保证限于精确游戏构建、Replay 格式/模组版本和 gameplay mod 环境匹配。外部 replay 按不可信数据处理，必须校验格式、完整性、大小和环境，限制写入路径，并避免携带非必要的本机路径/用户身份信息。

MVP 后大型方向的优先级已确认：多人 Replay、跨版本播放、隔离且可返回 Replay 的可游玩分支、其他 gameplay mod 正式兼容。这个顺序不改变当前单人 MVP 的范围。

`MVP.md` 已更新为 Baseline v1.1：在原端到端闭环和 G-01 至 G-11 Gate 上，补强了主菜单独立 Replay 会话与普通 Continue 全程隔离要求。`PROD-001` 已完成，后续技术实验不得降低该产品边界。

## 已确认（来自项目发起人的既有研究）

- 在正式版 v0.107 下，RunReplays 的 sample replay 可以启动并正常播放。
- 该结果证明“记录语义操作并重新驱动 STS2 游戏逻辑”的路线基本可行。
- RunReplays 当前播放节奏机械，主要记录动作，没有完整保留玩家真实操作时间。
- Replay 可以暂停、Step、停止并从当前位置接管游戏。
- RunReplays 没有真正的后退或 seek。
- 正常录制存在严重性能问题，地图操作和卡牌结算附近尤其明显。
- RunReplays 可作为 hook、command、游戏交互方式等方面的参考实现。
- 在 public-beta v0.111 下已确认存在 API/UI compatibility break，包括 Rng API 与部分 UI scene hierarchy 变化。
- 项目发起人当前运行完整版本 `v0.111.0`；其电脑可在 v0.107 与 public-beta 间切换，但每次切换需重新下载约 300 MB 游戏文件。
- 按项目发起人的玩家群观察，其本人、朋友及国内大部分活跃玩家当前使用 public-beta v0.111。该项是目标用户观察，不是正式用户统计。
- STS2 默认将加载模组与不加载模组的存档隔离；项目发起人记得可能存在用于互通的第三方模组，但名称和行为尚未验证。
- 按项目发起人观察，进行中对局不能从高版本直接降级读取；从低版本升级后可以继续，但已删除卡牌会变为 `0` 费“抽 1、消耗”的占位符，已修改卡牌会直接采用新版本定义。
- 模组启用/关闭可在游戏设置第一页的 `Modding` 栏目中调整。
- Steam 分支切换默认保留已安装模组文件、启用状态和配置；保留的模组可能因目标游戏版本 API 变化而无法正常工作。
- 项目发起人此前在 v0.111.0 下观察到 RunReplays 可以加载并打开界面、但无法正常开始游戏流程。本地 2026-08-19 `diagnostic.log` 显示两个使用 9,757,184-byte `sts2.dll` 的会话只到达 replay UI/载入尝试；02:50 游戏 DLL 被替换为 9,364,480 bytes，随后 02:51 会话才成功 replay 和录制。结合当时切换分支的背景，高置信度推断成功会话属于 v0.107，而非 v0.111.0。
- 当前游戏安装目录为 `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`，Project Lead 已确认可只读访问。
- 8 月 19 日的 RunReplays 测试仅启用了 RunReplays 与其依赖 BaseLib；没有启用其他模组。日志中也未发现其他已知模组的 patch owner，可排除其他 gameplay mod 作为该轮兼容性/性能观察的直接干扰源。
- 项目发起人确认严重卡顿也发生在仅启用 RunReplays 与 BaseLib 的环境中。
- 8 月 19 日性能测试时 RunReplays replay overlay 处于显示状态；同时 artifact 启用了 verbose/dispatcher 与 always-on diagnostic logging。
- 8 月 19 日测试使用游戏的独立 modded save tree（本地证据指向 `modded\profile2`）；Steam Cloud 处于关闭状态。
- v0.107 成功测试的 UI 入口记忆为：主菜单进入 RunReplays 的独立页面，在对应记录上点击 `Load Save` 可进入该楼层状态。源码确认 `Load Save` 是恢复/继续存档，不会启动 replay dispatcher；真正自动播放由 `Replay to floor …` 或 `Replay Floor` 触发。历史日志另行确认当时确实发生过 `StartReplay` 和 command dispatch，但具体点了两个 replay 按钮中的哪一个尚不能恢复。
- 项目发起人记得在 v0.107 下试过多个自动播放场景，至少包括一次“一层弱怪”和一次 Boss；其余场景、Boss 身份、章节与 seed 不记得。该表述保留为运行记忆，不把它擅自绑定到某个 sample。
- 2026-09-04 起，运行实验固定使用模组存档 1 号；项目发起人已新建该 profile 并完成全内容解锁，可用于广泛场景测试。
- 所有游戏内测试由项目发起人手动执行。Implementation Codex 禁止使用 Computer Use 或替代 GUI 自动化；必须先提出具体测试动作、目标、预期结果、停止条件和证据需求。
- 项目发起人于 SPIKE-INV003A 期间澄清：modded profile1 的 Continue 是测试用隔离哨兵，不是个人普通连胜对局。此前“其他槽不得读取或切换、modded profile1进度必须恒定”是当时工作包的保守实验边界，不得追溯改写旧结果。
- 2026-09-28最新澄清：普通profile1明确指**不加载模组的profile1**，用于个人连胜，是唯一须作为个人存档保护的槽；modded profile1仍是专用测试槽，modded profile1/2/3均允许项目实验使用，模组开关可自由调整。项目发起人平时会用`AscensionUnlocker`进入modded profile2/3；该事实不意味着未来诊断必须加载或必须禁用此模组。实验仍须为所测假设保留必要的phase-local baseline、artifact身份和停止条件，但不应把modded槽的预期变化误报为个人存档损坏。
- 项目发起人要求后续人工测试 prompt 明确目标后的立即停止动作，尤其写清是否禁止领取战斗奖励。基线前意外进度变化可记录后废弃旧基线并重建；受测 phase 内的变化不能通过 rebaseline 消除。

## 已确认（仓库与上游证据）

- RunReplays 原始公开仓库为 `https://github.com/boardengineer/RunReplays`。
- 本地已固定 Git tag `may-2026` 的源码归档；GitHub 显示对应短 commit 为 `05b0edd`，上游 manifest 版本为 `v0.2.0`。
- 该快照声明 MIT License，manifest 声明依赖 BaseLib 且 `affects_gameplay: true`。
- 该快照包含 90 个文件，包括 command、record/replay patches、replay engine/UI 以及四组嵌入式 sample replay 数据。
- 该快照的 `.csproj` 目标为 .NET 9，并引用本地 STS2 DLL、BaseLib、GodotSharp `4.7.0-dev.2` 和 Harmony `2.4.2`。
- 官方 v0.111.0 公告发布于 2026-08-14，并说明 8 月 27 日不会发布下一 beta patch，之后的等待也可能更久；这支持“v0.111 将形成数周稳定调查窗口”，但不是精确一个月的冻结承诺。
- 当前本地 `release_info.json` 将游戏构建固定为 `v0.111.0`、commit `41cef1ea`、branch `v0.111.0`；本地 `sts2.dll` SHA-256 为 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- Steam Workshop item `3759973128` 为当前 RunReplays artifact：Workshop manifest `v0.1.2`，DLL SHA-256 `FDE403AD981E605607BCACBF0D46F3008659195B83A6252D4B5B2710FFF12076`，ProductVersion 指向源码 commit `4b1fcfed256888d95e012b0af85de585eeb6734a`。
- Steam Workshop item `3737335127` 为 BaseLib `v3.4.5`，DLL SHA-256 `AD2F89E43E8B31DEBFAB65D783353D9429EBA59A2CFE904FF933A894CE79D32E`。
- RunReplays 的 Workshop manifest、对应源码 manifest 和运行日志分别报告 `v0.1.2`、`v0.2.0`、`0.1.0`；版本字符串不足以唯一识别 artifact。
- `INV-001` report 已通过验收；完整报告位于 `investigation-reports/INV-001-report.md`。八类入口均区分 Confirmed/Inferred/Unknown，且性能风险未被写成已确认根因。
- Project Lead 已在目标 DLL hash 上独立复核：`ToSave`、`FromSerializable`、异步 `SetUpSavedSingleplayer`/`LoadRun`、`EnqueueManualPlay`/`TryManualPlay`、`NetCombatCardDb`、`PlayerCmd.EndTurn`、ActionExecutor/turn events、readiness 单项字段以及 RNG serialization 签名均存在。
- 已确认三类直接相关的 v0.111 break：saved setup 名称/异步语义变化、`CombatManager.IsPlayPhase` 消失、逐实例 `Rng.Seed/Counter` 不再公开。它们说明旧实现不能原样作为 v0.111 探针，但不等于已定位历史运行失败的首个异常。
- SPIKE-001A 确认 `ToSave(CurrentRoom)` 在观察白名单上可无变化地产生 fixture；但同进程 setup 被 `State is already set.` 拒绝。
- 跨重启候选恢复链能到达可操作战斗，但出现 14 项手牌/牌堆/RNG diff；`Niche` counter `1→2`，`Shuffle` `9→18`。因此候选 readiness 成立不等于恢复状态等价。
- `SetUpSavedSingleplayer` 在测试调用中改写普通 `current_run.save`，即使先设置 `ShouldSave=false`。该调用链不满足 Replay 观看会话的存档隔离要求。
- `INV-002` 已通过验收。正常 Continue 的关键顺序为：反序列化 `RunState` → saved setup（先增加 reload count 并直接保存）→ fade/networking → `LoadRun`/`Launch` → run UI → map/room 重建。
- `ShouldSave=false` 只 gate 从 live room 构造 save 的常规重载；`IncrementNumReloads` 直接调用 `SaveRun(SerializableRun)`，该路径不检查 `ShouldSave`。因此 SPIKE-001A 的普通 save 改写已有方法体级解释。
- `CombatRoom.ToSerializable()` 只保存 encounter/room 的薄描述，不保存手牌、牌堆、回合、action queue 或完整 creature/combat state。中途 `ToSave(CurrentRoom)` 经正常 load 会重新建怪、洗牌和抽起手牌，不是中途状态恢复。
- SPIKE-001A 的 `Niche +1`、`Shuffle +9` 与“单敌人重新生成 + 10 张牌重新洗牌”的 v0.111 调用链精确吻合；该动态归因仍标为强推断，因为 INV-002 没有新增逐 RNG 调用 trace。
- 官方房间前保存点使用 `PreFinishedRoom=null`，与 fresh room reconstruction 的语义相容；这是当前唯一获准进入下一受限运行实验的原生恢复候选。
- `RunManager.CleanUp` 是明确 teardown，但只有完整 ReturnToMainMenu/scene teardown 顺序有生产调用证据；任意活动战斗中直接 `CleanUp → setup` 的安全性仍未知，不进入下一实验。
- 项目发起人确认独立播放会话为 MVP 要求：Replay 在主菜单有与单人/多人并列的入口，观看不应另开/切换专用游戏进度槽，也不得覆盖正常关卡进度。
- RitsuLib 的公开仓库与固定 v0.5.18 源码表明它是 MIT 许可的 STS2 共享框架，覆盖生命周期、Harmony patch、持久化/`InMemory` mod data、设置 UI、运行时 UI、诊断和兼容门控；其主构建包含 API 0.111.0，但不提供游戏 Run/save 隔离。
- `INV-003` 已验收：v0.111 主菜单按钮和 submenu 分派为固定实现，没有通用 peer-entry 注册 API；Replay 入口需要 Godot 节点注入与 Harmony/等效 hook，管理页可在 `RunManager.State == null` 时独立存在。
- `RunManager.SetUpReplay` 不调用 `IncrementNumReloads`，使用 no-op `NetReplayGameService`，但唯一 caller 是 debug `NMultiplayerTest.RunReplay`，输入为 `CombatReplay`，并把 `InitializeShared(... shouldSave:true ...)` 写死。
- `ShouldSave=false` 能阻止 live run save 和部分 run-end progress/history/stats 分支，但不能阻止 combat victory 的 `UpdateProgressAfterCombatWon → SaveProgressFile` 直接写入。
- 游戏没有 per-run `SaveManager` 注入；`SaveManager.MockInstanceForTesting` 提供全局 override 构件。事务式 memory manager + `ShouldSave=false` + ordered teardown 是当前唯一获准运行验证的组合候选，仍只是推断。
- RitsuLib 已固定为 v0.5.18、commit `f224961a9392e010335da092240b90ee8235317f`、MIT，并有 STS2 API 0.111.0 target。它没有主菜单 peer-entry API，`SaveScope.InMemory` 只隔离自己的 mod data；暂不作为 MVP 必要依赖。
- `Alchyr/ModTemplate-StS2` 已在 commit `55ca2c606e6c78dd39689a5cf979b243a49652e7`、template package `2.5.2` 上完成工具评估。它是 Alchyr 维护的社区 `.NET new` 模板；即使由游戏内/官方入口链接，也不能据此称为 Mega Crit 维护的官方代码。
- 该模板的 Empty Mod 提供 `net9.0`、Godot.NET、`ModInitializer`、Harmony、STS2 路径发现、manifest、DLL/PDB 复制和 PCK export 的工程骨架，适合作为未来正式工程脚手架参考，但不提供 replay、seek、确定性恢复或 SaveManager isolation。
- 模板默认使用 BaseLib 和 ModAnalyzers 的浮动 NuGet 版本、`min_game_version: 0.107.0`、build 后写入 live mods 目录，且 PCK export 允许 warning 后继续；这些默认值不得原样进入 Replay Mode 的可复现/安全构建。
- SPIKE-INV003A Phase A 已确认：主菜单 peer 中可注入独立 management-only 页面；打开/关闭期间 `RunManager.State == null`、原 `SaveManager` 身份不变，12 个受保护文件 identity 无变化。
- SPIKE-INV003A Phase B 已确认：深拷贝初始化的 in-memory `SaveManager` transaction 可通过 native `SetUpReplay` 进入可观看 combat scene，并在用户 Stop 后按 ordered teardown 恢复原 manager、清空 run state、重建主菜单；本轮原 manager/store 写删与外部 submit counter 为 0，12 个受保护文件无变化。
- SPIKE-INV003A Phase C 已确认：固定 v0.111 native `.mcr` 可完整反序列化并含 11 个 replay events；这些事件使战斗自动执行约两个回合，但在第三回合开始后耗尽，未达胜利，故没有观察到 `UpdateProgressAfterCombatWon` 或 isolated `progress.save` write。teardown 与受保护文件检查仍通过。
- Phase C 的工作包 FAIL 是“必需哨兵未覆盖”，不是已观察到 save isolation 泄漏。根据首个 FAIL 停止规则，D/E/F 未执行。
- INV-004 已确认固定 fixture 的 11 个 events 覆盖三个回合；最后 action id 10 是致死攻击，checksum 17 中敌方已从 full-state creatures 消失。固定 fixture 并非只录了两个回合。
- native `CombatReplay` 格式没有 serialized victory/complete flag 或终止 event；`events>0` 不能作为通用完整性证明，cleanup 也可能 finalize 未胜利录制。本 fixture 的致死 action/checksum 证据只适用于该固定文件。
- Phase C dispatcher 与 v0.111 `NMultiplayerTest.RunReplay` 的四类 event 分支和等待语义一致；官方 debug caller 自身也不等待 victory 或 checksum 完整消费。readiness/action cancellation 是首要推断，但尚无逐事件运行证据确认根因。
- `UpdateProgressAfterCombatWon` 与 `SaveProgressFile` 可在合法 isolated replay combat state 中作为无 event-dispatch 的 direct-write sentinel；这只验证两个 production SaveManager 方法的隔离路由，不等于验证自然胜利、achievement 或 replay fidelity。
- SPIKE-INV004A 在 pre-override isolated-manager content-equivalence gate 失败：`CreateIsolatedManager` 从受保护磁盘文件初始化候选 manager 后，其 Settings/Prefs/Progress 至少一项的 JSON 表示与当前 live manager 不等；当前证据没有记录究竟是哪一项或哪些字段。
- 该失败发生在 transaction audit、`MockInstanceForTesting`、`SetUpReplay`、scene launch 和两个 progress 方法之前；因此不能称为 direct-write isolation FAIL，也没有形成一次可用于证明“原 store 零调用”的完整 audit session。
- SPIKE-INV004A 的安全停止证据通过：未进入战斗、未读取或分派 replay events、未安装 override、未产生 isolated/真实存档写入；原 manager 身份保持不变、`RunManager.State == null`、Continue 可见，12 个受保护文件及 inventory 零变化，原 progress canonical hash 前后均为 `975456DA161A5B992260C2A7F3D23B0FEECBE7AAE73F7162962A57F78912C4C5`。
- 相同的 disk-rehydrate + JSON equivalence 构造曾在 SPIKE-INV003A Phase B 通过、在 SPIKE-INV004A 的新会话失败。这只确认“磁盘重建 manager 必然等于当前内存 manager”不是稳定前提；尚不能区分 live/disk 漂移、初始化默认值或迁移、序列化顺序差异与不完整 clone。
- SPIKE-INV004B 本轮 Candidate A 未复现历史差异：Settings、Prefs、Progress 的 raw/canonical JSON 均逐组件相等、结构 diff 为 0；因此只能排除该固定状态下稳定必现的 disk-rehydration 缺陷，不能定位 SPIKE-INV004A 的历史差异。
- SPIKE-INV004B Candidate B 以当前 live Profile/Settings/Prefs/Progress 序列化状态播种全新 mock store，经正式初始化后，三组件 raw/canonical 均相等；双方各遍历到 367 个可变节点，共享引用为 0。该证据只覆盖本次固定 build/profile/main-menu snapshot。
- 两个 candidate 的 manager 与所有捕获的 store 均独立，初始化 I/O 全部封闭在各自 `MockGodotFileIo`；未安装全局 override，未建立 run/scene，未读取 replay event，也未调用 progress mutation/save。
- SPIKE-INV004B 诊断窗口内原 manager identity、live 三组件 hash、12 个受保护文件及 profile1 inventory 均不变。正常退出后 `settings.save`、`prefs.save.backup`、`progress.save.backup` 的 mtime 更新但长度/hash 不变；这是诊断窗口外的 production exit 生命周期现象，不能归因于 candidate，也不能表述为“退出全程没有真实文件写入”。
- INV-005 修正 production 初始化顺序为 Settings → 启动并等待 cloud sync → Profile → Progress → Prefs；此前 SPIKE-INV004B Phase 0 所述 profile → settings → prefs → progress 不是 production startup 顺序，但不影响 probe 内部 candidate 初始化结果。
- Settings、Prefs、Progress 的常规 `Save*` 与底层 `ISaveStore.WriteFile` 均为同步 `void`。启动 cloud sync 的内部异步写全部被 `GameStartup` gate，`LoadMainMenu` 也在创建主菜单前等待 `CurrentRunSaveTask`；静态闭合调用集中没有确认会跨入主菜单的 component save task。
- INV-005 找到两类可造成 session-dependent live/disk 漂移的 production 路径：localization/display repair 可修改 live Settings，non-editor startup 可把 `PrefsSave.FastMode` 从 `3` 修正为 `2`；二者都可能等到后续保存或退出才落盘。它们是历史 SPIKE-INV004A 差异的候选解释，不是已确认根因。
- `InitializeGraphicsPreferences` 由 `Callable.CallDeferred` 排队，无 task handle、signal、pending counter 或 flush API；它可能修正 live Settings。`NMainMenu._Ready` peer entry 在 `GameStartupComplete` 之前可用，而仅等待 `GameStartupComplete` 仍不能从静态 IL 证明该 deferred callback 已执行。
- 正常退出同步保存 Settings/Prefs/Progress；local temp-write/rename、`.backup` 替换与 cloud timestamp sync 提供了三个“内容不变但 mtime 更新”文件的精确 production 机制。它足以解释现象形状，但真实一次退出的动态归因仍为推断。
- SPIKE-INV005A 运行中 patch-active 先于唯一一次 graphics callback entry；callback 正常 exit，随后 `NGame.GameStartupComplete` 为 `RanToCompletion`，original manager identity 保持不变且 `CurrentRunSaveTask` 始终为 null。这些是局部 timing 证据，不构成完整 gate PASS。
- 首个 observed Steam remote `WriteFileAsync` leaf task 在主菜单前 fault，production 保留本地文件；artifact 最终记录大量 nested leaf task fault，而 outer cloud/startup 仍可完成。当前 gate 把“所有被 patch 到的 leaf task 必须成功”当作条件，已证明与 production 容错层级不一致。
- SPIKE-INV005A 未安装 manager override、未构造 candidate、未启动 run/scene、未调用 progress/save，也未建立 gate baseline 或启用 Verify；因此 Settings/Prefs/Progress gate fingerprint 与 profile1 保护窗口结果均未知。
- SPIKE-INV005A 的 instrumentation 在判断 `_enabled` 之前先解析路径并计算 write payload hash，且首个 async fault 只被 latch 到 GateFailures，直到 startup/graphics 条件齐全才真正 Finish/Stop。结果是 probe 持续记录并派生了 out-of-scope profile 数据；工作包最终以 `FAIL_SAFETY` 控制，原 artifact 的 `FAIL_GATE` 字段只保留为机器原始结果。
- 越界 artifact 只包含逻辑审计元数据及 length/SHA-256 派生值，没有原始 save payload。它被标记为 restricted local evidence，不得发布、作为 fixture 或复制到外部报告；是否另行清理需项目发起人显式决定。
- INV-006 已验收为 `READY_SCOPE_SAFE_TIMING`。固定 IL 证明 `GameStartup` 保存 `DoCloudSync` 返回的 outer task，只轮询 `IsCompleted` 而不取 result/status/exception；因此 success、fault、cancel 都满足 production admission。Steam uninitialized 时 production 也可能在 outer task 仍 pending 时提前继续，该分支不能被 future probe 称为 cloud settled。
- `CloudSaveStore` 的 per-file wrapper 会捕获预期 remote leaf exception、记录本地内容保留并正常完成，所以 Steam leaf fault 可以存在于成功的 manager task/`DoCloudSync` 之下。leaf status 是实现细节，不是 startup gate。
- `GameStartupComplete` 在正常 startup 和 wrapper catch 路径都可被置为完成，只是后续 lifecycle marker，不能单独证明 cloud work 收束。future probe 必须直接捕获零参数 `DoCloudSync()` 返回 task 并等待任一 terminal status。
- scope-safe timing gate 不需要 global save-store patch、path、payload、component fingerprint 或 profile 文件。INV-006 已定义 pre-argument sealed/enabled check、single-CAS terminal、event-lock 闭合集合、post-seal continuation fast-return、artifact 字段白名单和 synthetic negative/race cases。Project Lead 验收时修正了一处契约歧义：只有 terminal CAS winner 持 opaque ticket 可在 seal 后对 closed snapshot 写一次 artifact；普通 post-seal artifact request 仍必须立即返回。
- SPIKE-INV006A 已验收为 `COMPLETE — PASS_SCOPE_SAFE_GATE`。单次人工运行产生 10 个连续事件，唯一 outer task `Pending → RanToCompletion`，graphics 恰好 `1 entry / 1 normal exit / 0 active`；Gate 与 Verify 的 manager/task identity、task status 与 11 项条件一致，无第二 lifecycle 或 post-seal event。
- SPIKE-INV006A runtime artifact 为 3,554 bytes、SHA-256 `7F45652CC5AB7524CB3444B99FA8AF8AF5EFF0E47EB433A3306C2FAE44B52823`；Project Lead 独立解析通过 schema/forbidden-term/order 检查，并重跑 preflight 得到 23/23 tests、0 warning/0 error、source/binary audit 与 build identity 全部通过。
- 006A 未读取 save/profile/path/payload，未安装 override、构造 candidate、进入 run 或调用 progress/save。它只证明固定 v0.111.0 环境中的 pre-override timing gate，不构成 save isolation PASS；probe 已退役，必须在下次启动前关闭且不得重跑。
- SPIKE-INV006B 已验收为 `COMPLETE — BLOCKED_PRECONDITION`。closed artifact 为 17,487 bytes、SHA-256 `98B91618F67E7AD5E651C1BC1B41515040A599976FC237E8FC24279134538511`；事件 1–17 连续，timing gate、12-file protection baseline、recovery backup 与 live-seeded candidate 全部通过。
- 006B candidate 的 Profile/Settings/Prefs/Progress raw/canonical 均等价，递归 shared mutable reference 为 0；事件到达 `OverrideInstalled`，但未到达 `SceneReady`。`UpdateProgressAfterCombatWon`、`SaveProgressFile` 与 candidate memory write 计数均为 0，故本结果既不支持也不否定 direct-write isolation。
- 006B teardown 完整：cleanup 一次、原 manager 恢复、run state 清空、Continue 可见；12 个保护文件与 profile1 inventory 前后无变化，12 份 recovery backup 与记录 hash 一致。Project Lead 独立重跑 preflight 得到 12/12 tests、0 warning/0 error、source/binary audit、70-method patch smoke 与固定 identity 全部通过。
- `RunState` 是至少八个 setup/scene/fixture 断言共用的固定失败枚举。用户看见 combat 与日志中的 room marker 只让 post-load 断言成为首要推断，不能唯一定位失败子条件；旧 probe 不允许为诊断而重跑。
- INV-007 已验收为 `COMPLETE — READY_NEW_SENTINEL`。Project Lead 独立复现窄 IL 输出 160,808 bytes / SHA-256 `A36EDD5BE0499162B297CDC6C1BA23547990352D38FC7B1BA27937E1AD4EEBAE`，fixture contract 1,081 bytes / SHA-256 `BBCF28AE5D6B7F6652751C6D15476F0E243DFB43EB0A21EBD648B2B6265EE0BB`，checker build 为 0 warning/0 error。
- 两个 direct progress 方法不要求固定 IRONCLAD、两只 TOADPOLE、encounter 双 getter 引用/ID 对齐、external run-state identity、`ShouldSave` 或 executor/frame readiness。`ShouldSave=false` 与 candidate/run identity 仍可作为 transaction safety/setup integrity 单独检查，不能称为 progress method requirement。
- `UpdateAfterCombatWon` 的最小数据来自实际 player/character、CombatState/Encounter、RoomType、可枚举 enemy IDs、candidate Progress 及可能写入的 `DiscoveredEnemies`；`SaveProgress` 只需 candidate progress/migration/profile provider/memory store，并内部吞并 write exception，所以必须靠 write audit 与 payload canonical equivalence 验证。
- SPIKE-INV007A R1 closed artifact 为 3,108 bytes / SHA-256 `D38EC65F8380DDC2AFF1C8F6EB9E7FDEBEAB1D706424C4471C36DAAD0488ACA2`。它记录 `GateReady` 后约 120 秒的 `STOP_MISSING_TIMING_GATE / TimeoutCallback`，但没有 `RunAccepted`、protection、candidate、audit、sentinel 或 teardown；这是 harness lifecycle 错误，不是 timing gate 或 direct-write FAIL。
- 后一进程中的人工 Run 已创建 12 份 restricted pre-action recovery backup，但固定 `runtime-artifact.json` 已被 R1 占用，writer 正确拒绝覆盖，UI 显示 `BLOCKED_ARTIFACT_WRITE`。该 attempt 的真实业务 terminal、direct calls、boundary、teardown 与 after-protection 结果均没有被保存，必须视为 unknown。
- “首个终态停止”规则已细化：若是明确的 pre-business harness 缺陷，可在 Project Lead 授权下保留 probe id、提升 revision/schema 并使用 attempt-specific evidence；若业务可能已执行但 artifact 缺失，则重试前必须以新进程只读比较 prior recovery baseline，任何内容/inventory 差异立即阻塞。
- SPIKE-INV007A R2 closed artifact 为 19,259 bytes / SHA-256 `4B48A75F650A687F206255F2425140B3F640277E3D62F21495F99CC6086B691F`；事件 1–22 连续且 monotonic。timing、prior-attempt gate、candidate 等价/独立、scene load、direct arguments与 protection baseline均通过。
- R2 中 `UpdateProgressAfterCombatWon` 在 candidate上恰好调用一次，save/write为0，original/unknown manager/store及 external boundary均未出现。这构成 direct-update routing的局部正面证据，但不构成 mutation correctness或完整 direct-write isolation PASS。
- R2 oracle使用 `ProgressState.ToSerializable()` 后直接 `ApplyExpected`。固定 IL确认 EncounterStats/EnemyStats只被装入新 outer list，nested stats/FightStats仍共享；oracle在production前已改变candidate，故 expected/actual hash差异不能归因于production update错误。
- R2 teardown全部通过：需要时cleanup一次、original manager恢复、run state null、Continue可见；12个 protection before/after完全一致，12份R2 recovery与source hash一致。灰色/残缺scene是load后约3.4 ms即teardown的伴随现象；`Invalid Task ID`尚无证据与mutation mismatch相关，不扩大R3范围。
- SPIKE-INV007A R3 已验收为 `PASS_DIRECT_WRITE_ISOLATED`。artifact 为 19,953 bytes / SHA-256 `AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553`；24 个事件连续且 monotonic，terminal failure site 为 `None`。
- R3 detached oracle 在 production 调用前确认 parse 成功、candidate/DiscoveredEnemies canonical 未变、shared mutable references 为 0、pre-call audit 为 0；随后 candidate update/save/write 精确为 `1/1/1`，expected/actual canonical hash 同为 `F25CA6D8523112C5699A24CEA374380A76CE3645C5EA080EB857EB7C2AE5F2E3`。
- R3 唯一 write 为同步 exact logical profile1 progress write，payload 可反序列化且 canonical 等价；未观察到 original/unknown manager/store 或 external boundary。cleanup、原 manager 恢复、run state null 与 Continue 均通过。
- Project Lead 独立复核 R3 的 12 项 protection before/after 全字段一致，12 份 recovery 的实际长度/hash 与 artifact 记录一致。该 PASS 只覆盖固定 isolated Monster session 中的两个 direct methods，不覆盖自然 victory、achievement、fault/restart、replay fidelity 或完整 session isolation。
- INV-008 已验收为 `READY_FAULT_RESTART_MATRIX`。Project Lead 独立重建查询工具为 0 warning/0 error，重新生成的 132,086-byte IL 输出 SHA-256 仍为 `453C31CC5C22E0FD0C54F3D0CFC191FBDB967B2DBFF508A06A050A2A635354B8`，错误程序集触发 `BLOCKED_IDENTITY`。
- 固定 IL 支持两个最小 fault 点：setup fault 在 `SetUpReplay`完整返回并收紧`ShouldSave`/sync/writer门禁后、Launch前；post-launch fault在`Launch`返回且新NRun同步安装后、GenerateMap/room/frame/action前。两者只验证owner捕获的同步异常。
- `RunManager.CleanUp` 的 production finally会清空若干全局状态，但主体中的reset/dispose/disconnect不是逐项finally保护；任一前序异常可跳过后续清理。因此teardown failure必须独立于预期owner exception分类，不能以`State==null`单独判PASS。
- 下一矩阵固定为四个依次解锁的fresh-process phase：setup owner fault → restart verification → post-launch owner fault → restart verification。强杀、native crash、断电和未执行finally的恢复不在本轮，restart PASS不得外推为crash recovery。
- SPIKE-INV008A v1 Phase 0已通过并由Project Lead独立复核：34/34 tests、Release 0 warning/error、source/binary audit与112-method Harmony patch/unpatch smoke均通过；新安装pair为DLL 215,552 bytes / SHA-256 `4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717`，manifest 372 bytes / SHA-256 `FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4`。
- v1首次artifact `setup-owner-fault.json` 为2,359 bytes / SHA-256 `56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48`，只记录启动事件后以`BLOCKED_IDENTITY`停止；日志确认007A与008A同时loaded。其protection/candidate/recovery/fault/audit/teardown字段全部为空。
- 用户禁用007A后，新进程日志确认只loaded 008A；但v1 phase slot已被前一artifact占用，`invalid-phase.json`（1,479 bytes / SHA-256 `A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F`）只记录`PatchActive → BLOCKED_PHASE_ORDER`，业务字段仍为空。
- 因两次均在正式protection/candidate/override前停止，v1分类为`PREBUSINESS ENVIRONMENT BLOCK / NO FAULT-RESTART CONCLUSION`。按evidence-preserving revision规则批准同id窄R2；v1两份artifact永久保留。

## 尚未确认或尚不知道

- 性能 Gate 的量化预算、导航延迟预算、文件大小/导入上限等数值；必须由实验给出证据并另行接受。
- 是否在 MVP 后、跨版本播放大型目标之前另行提供 v0.107 的独立同版本构建；当前无此计划。
- RunReplays 各模块中哪些可直接复用、哪些只应借鉴、哪些必须重做。
- 当前 Workshop artifact 对应源码已固定，但此前 v0.107 成功测试是否使用完全相同的 DLL hash 仍未由历史 artifact 证明。
- 严重录制性能问题的根因、量级、触发条件与可接受性能预算。
- 语义操作记录是否足以覆盖所有单人分支、随机性、异步 UI 与中断场景。
- 回放确定性所需的初始状态、RNG 状态、版本信息和外部模组信息。
- 房间前 `PreFinishedRoom=null` checkpoint 经正常 Continue 后，第一个输入就绪 snapshot 是否与不重启 baseline 完全等价。
- disposable 官方 profile 在生产 Steam/save 环境中是否能端到端隔离所有写入；静态证据只确认 run path 按 profile id 分段，未证明运行时和 cloud 副作用完全隔离。
- 游戏程序集虽有可注入 `ISaveStore`、测试用内存 store 和若干 replay/test setup 构件，但尚未发现同时满足 saved-singleplayer 语义、明确 non-persistent 且有 production caller 的现成入口。
- 事务式全局 memory `SaveManager` 已在正常 Stop、固定 isolated direct progress write和R6两个controlled owner fault/紧邻normal restart路径通过本阶段受控门；强制进程崩溃、任意生产异常与其他版本/机器的恢复行为仍未知。
- SPIKE-INV004A 中究竟是 Settings、Prefs、Progress、profile identity 的哪一部分发生不等价，以及差异是值差异还是非规范 JSON 顺序差异，仍未知；SPIKE-INV004B 未复现该差异。
- 单次 006A/006B 运行已证明 `InitializeGraphicsPreferences`、outer cloud、startup/menu marker 可汇合成稳定 gate；该顺序在其他机器、Steam 状态或后续游戏构建上的可重复性仍未知，不能外推为通用 production 保证。
- 在其他机器、Steam 状态或后续游戏构建中，timing gate、live-seeded candidate 与 direct-write isolation 是否保持成立仍未知；当前动态证据固定于 v0.111.0 与本机 profile1 测试槽。
- 007A R1 之后、R2 之前的丢失终态 attempt 是否执行了两个 direct methods、内部业务终态为何，均因 artifact 冲突不可恢复；不能从 `BLOCKED_ARTIFACT_WRITE` 反推 PASS 或 FAIL。
- SPIKE-INV006B 的精确 `RunState` 失败子条件仍不可恢复；但哪些断言属于 production 最小数据需求、隔离安全条件或非必要 fixture 身份限制，已由 INV-007 静态分开。
- 某次 Steam invalid remote call 的唯一外部原因仍未知；可能条件不能仅凭 Cloud disabled、client 状态或静态 IL 唯一归因，但 timing gate 已不需要诊断 leaf 原因。
- live-seeded candidate 在未来会话和 transaction 安装瞬间是否仍稳定满足精确等价，仍未知；虽然静态证据没有发现跨越主菜单的 component save task，isolated replay session 是否意外启动自己的 `CurrentRunSaveTask` 仍须后续 transaction audit。
- 固定 native `.mcr` 已确认含第三回合致死 action，probe 与官方 debug dispatcher 也无 event-loop 差异；运行时为何没有执行至该记录状态仍未知，需未来独立 replay-fidelity 实验记录 readiness、action cancellation 与 actual/expected checksum。
- 所有 Steam/cloud/platform/metrics/第三方 mod side channel 是否都能被下一实验的 audit points 完整观察；静态调用图不能证明无外泄。
- native `CombatReplay`/`SetUpReplay` 能否扩展到 MVP 所需的任意 singleplayer Replay baseline；当前只确认 debug combat replay primitive。
- RitsuLib 外围能力的实际性能、版本维护成本与玩家依赖接受度；不影响下一核心隔离实验。
- Alchyr ModTemplate 的 ModAnalyzers 覆盖、跨平台 PCK 可复现性，以及正式 Replay UI 是否需要 PCK 仍未知；采用脚手架不等于采用其默认 BaseLib 依赖。
- `NetCombatCardDb` card ID 与 Creature CombatId 在独立恢复后是否稳定；在 SPIKE-001A 通过前不验证。
- v0.111 readiness 候选字段的组合语义与 quiet-frame 需求；单个字段存在不代表组合已验证。
- 游戏焦点、暂停、加载、可交互状态与模组/系统阻塞能否被低开销且可靠识别；时间戳与游戏 UI readiness 的协调规则。
- MVP 后若重新考虑接管，如何处理返回 replay、分支存档、成就、统计和接管后录制；当前不展开设计。
- 双向导航所需的状态恢复机制及可接受延迟；检查点加快速重演仍只是待验证候选。
- 强记录需要哪些状态、以何种密度记录，以及完整快照/增量/指纹如何组合，才能同时满足纠偏、导航、性能与文件大小目标。
- Replay 文件格式、兼容性策略、损坏恢复与隐私边界。
- 可移植 Replay envelope、完整性机制、导入大小上限、重复文件策略和隐私字段白名单。
- MVP 后跨版本播放的技术路线、支持矩阵、迁移责任和成功标准；该项明确不在当前 milestone 内设计完成。
- gameplay mod 如何声明 replay 兼容状态、提供自定义状态序列化/恢复或版本迁移；不属于 MVP。
- RunReplays 在 v0.111.0 中失败的精确机制与首个异常；现有日志把失败边界收窄到 replay UI 打开/载入之后，但未记录显式异常。
- v0.107 成功测试中触发真正自动播放的精确按钮是 `Replay to floor …` 还是 `Replay Floor`；项目发起人确认无法回忆，当前不为该低价值细节专门切换版本重测。
- v0.107 的任一 sample 是否曾在无人干预下自动播放到其目标终点；项目发起人不记得，现有证据仅确认多个场景能够启动并执行一段 replay。
- 多人模式所需的权威状态、网络事件与同步机制；该项暂不进入当前 milestone。

## 当前风险

- **自有链路证据缺失**：RunReplays 的参考源码、artifact 与历史证据已固定，但本项目尚没有在 v0.111 上可重复的最小录制—重放 fixture。
- **恢复起点风险**：原生 run save 可能不包含首回合输入就绪点的全部 transient combat state；若恢复基线不一致，capture/dispatch 成功也不能证明确定性重放。
- **存档副作用风险**：测试表明 `SetUpSavedSingleplayer` 可绕过 `ShouldSave=false` 改写普通 `current_run.save`；任何新恢复实验必须先证明隔离路径或停止。
- **房间前恢复假设**：静态调用链支持 `PreFinishedRoom=null` 是合适的 fresh-room checkpoint，但是否能在正常 Continue 后得到完全相同的首个可操作状态仍需 A/B 运行证据。
- **产品—实验错位风险**：disposable profile + normal Continue 可以验证重建语义，但不满足最终独立 Replay 会话。若不先调查 session/save 隔离，可能围绕错误生命周期搭建播放器。
- **Replay fidelity 风险**：INV-004 已把 direct-write 隔离哨兵从 replay-to-victory 中拆出，但固定 native event loop 为何未执行到已记录的致死状态仍未知；未来必须用 readiness、cancel outcome 和 actual/expected checksum 单独诊断，不能把 SPIKE-INV004A 的结果外推为重放正确。
- **共享库依赖风险**：RitsuLib 可能减少 UI、生命周期和诊断工作，也增加版本、加载顺序和必要依赖兼容面；采用范围必须逐能力验证，不能整库能力想当然。
- **脚手架默认值风险**：社区模板能减少工程样板，但浮动依赖、live-directory 自动部署、旧 `min_game_version` 和容错式 PCK export 会削弱构建可复现性与发布门禁；正式工程必须先重写这些默认项。
- **全局事务风险**：`SaveManager` override 不是 per-run scope。Replay transaction 期间所有 game save consumer 都被重定向；延迟任务、shared object、异常顺序或第三方 callback 可能越过恢复边界。
- **manager 基线一致性风险**：SPIKE-INV004A 证明磁盘重建的候选 manager 不一定与当前 live manager 等价。若不先定位差异并建立独立、精确的 live-state clone，安装全局 override 可能从 transaction 开始就改变设置或进度语义。
- **生命周期写入归因风险**：正常退出会在内容不变时更新三个受保护文件的 mtime。后续隔离实验必须区分 baseline production 启动/退出写入、transaction 内写入和延迟越界写入，不能仅凭 mtime 变化判定 candidate 泄漏，也不能因 hash 不变就声称没有 write。
- **deferred callback 边界风险**：`InitializeGraphicsPreferences` 在执行时读取当前 `SaveManager.Instance`。若它延迟到 override 之后，可能把原 manager 应接收的 display repair 应用到 isolated manager；必须显式观测 callback 完成，不能用主菜单可见、`GameStartupComplete` 或固定 quiet frames 单独代替。
- **探针前置条件错配风险**：006B 已证明过严的 fixture-specific gate 会让实验测到 probe 自身约束而非存档隔离。INV-007 已完成静态拆分；SPIKE-INV007A 必须通过 forbidden-surface audit 保证这些 gate 没有被带回新实现。
- **人工等待与 attempt 身份风险**：Ready 后仍存活的 startup timeout 会把人的响应时间误判成 gate failure；固定 artifact 文件名又会让后续进程的真实终态无法保存。R2 必须在 Ready 原子提交时解除 timeout，并让每个 attempt 的 artifact/recovery 不可覆盖且身份独立。
- **Probe安装/启用生命周期风险**：新DLL/manifest安装后可能在首次启动默认enabled，而已完成probe仍保留持久化enabled状态；若先安装再让用户关闭旧probe，会在配置UI可操作前同时加载并消耗one-shot phase artifact。必须在安装新probe前完成旧probe禁用与持久化验证，并把wrong-mod-set precheck与业务artifact分离。
- **loaded-mod观察时机风险**：`ModManager.GetLoadedMods()` 在当前mod initializer执行时尚未发布最终集合；initializer中的空/部分结果不能用作唯一模组门禁。检查仍须早于profile/protection/override，但必须位于有证据支持的post-loader生命周期边界。
- **基线来源错配风险**：recovery副本若未保留源mtime，只能验证字节身份，不能充当时间戳oracle；历史artifact缺失的字段不能在运行时被折叠为普通false。mod enabled状态又属于`settings.save`内容，因此跨probe lineage不能同时要求改变启用配置和历史settings字节恒等。初始证据链与phase-local before/after必须分离。
- **oracle 别名风险**：production对象的 `ToSerializable`/projection可能只复制容器而共享nested mutable objects。expected计算必须用serialization round-trip或显式deep clone，并在被测调用前复核candidate canonical与shared-reference count；否则实验会由oracle自身制造差异。
- **调查工具数据最小化风险**：全局 store patch 即使不修改 save，也可能通过读取参数、hash payload 或记录路径越过 profile 范围。新的 timing gate 已禁止整个 store/path/payload instrumentation surface；其他 future audit 若确需该表面，仍须另行批准并在任何参数处理前完成 scope gate。
- **性能风险**：录制路径已知有严重卡顿，但尚无 profile 数据，直接设计架构可能围绕错误根因优化。
- **同步诊断 I/O 假设**：对应源码启用 verbose/dispatcher 编译标志，always-on `DiagnosticLog` 在锁内使用 `AutoFlush=true` 逐行写盘；它可能放大热路径卡顿，但尚未通过关闭/替换日志的对照实验确认贡献度。
- **版本漂移**：v0.107 与 v0.111 已出现兼容性断裂，必须在调查中标注版本。
- **跨版本状态语义风险**：游戏自身允许部分低版本对局升级，但会替换已删除/修改的卡牌；Replay 不能因此默认宣称跨版本语义一致。
- **范围风险**：双向导航已经进入 MVP；多人、跨版本、可游玩分支和 gameplay mod 正式兼容必须保持为 MVP 后独立里程碑，不能因战略优先级而提前混入当前实现。
- **确定性风险**：sample replay 成功不等于任意实际对局都可可靠复现。
- **外部文件风险**：MVP 支持导入他人 replay，解析、解包、路径处理和资源上限必须按不可信输入设计。

## 当前有效决策

- 早期优先技术调查、产品边界和分阶段验证，不急于决定最终代码架构。
- RunReplays 是参考实现，不默认继承其整体架构。
- 未经实验验证的技术判断必须保留为假设。
- 当前优先完成单人 replay MVP；多人 Replay 是 MVP 后第一产品优先级，但当前只记录未来约束，不提前实现。
- 当前调查与 MVP 规划以 public-beta v0.111 为目标版本；v0.107 用作历史成功基线和兼容性对照，不自动承诺双版本支持。
- 不为恢复 v0.107 历史测试中的精确 replay 按钮专门安排重测；只有当后续调查确实依赖该区别时才重新打开此项。
- 产品定义先于进一步的 RunReplays 深挖。先完成第五轮 J/K/L，形成 MVP 玩家闭环与排除项；随后只针对 MVP 的 hook、确定性、性能、时序和 v0.111 兼容风险做定向源码调查或自有最小实验。
- 不计划 fork RunReplays 后直接演进；这不预先排除依法复用某个经验证的独立部件，但任何复用都必须由具体需求和证据触发。
- MVP 的录制入口采用默认自动录制：模组启用后自动记录受支持的单人对局，并允许玩家通过设置关闭。
- 当录制性能与首版覆盖发生硬冲突时优先满足性能门槛，但继续以尽可能覆盖全部单人场景为目标；临时排除项必须可见、可追踪。
- 正常录制采用无常驻 HUD 的静默体验；只对关闭、失败和不支持等例外状态做玩家可见提示。
- 首版 replay 管理支持删除和打开所在目录；不支持自定义命名，改用元数据自动生成可识别名称。
- MVP 基本观看控制包含暂停/继续、按玩家操作 Step、速度调整和停止退出。
- 前进/后退是重要产品能力；播放间隔应按操作类别提供 config。Counter-Strike demo 用作交互参考，不直接套用其底层实现。
- 双向导航的 MVP 包含逐操作、逐结构节点和可拖动/点击时间轴三个层级；界面采用“房间内操作时间轴 + 全局路线图”的双层组织。
- MVP 记录语义操作时间戳；默认使用分类 config 节奏，并提供原始节奏模式。时间戳精度与异步 UI 协调需要实验验证。
- 原始节奏只计算游戏聚焦、未暂停、非加载且可交互的时间，保留其中的玩家思考，排除暂停/失焦/加载和可识别阻塞。
- MVP 不支持从 replay 当前位置接管游戏；停止回放不会把 replay 不可逆地转换为普通对局。
- Replay 采用强记录方向；可恢复 divergence 自动纠偏并继续，同时输出结构化日志。该方向不构成任意跨版本兼容保证。
- 权威状态不可恢复时停止并明确报错。跨版本播放不属于 MVP，而是后续独立大型目标；首版必须保存未来调查所需的版本/内容元数据。
- MVP 正式支持原版内容、Replay Mod 及必要依赖；其他 gameplay mod 兼容延期。所有启用模组/版本进入元数据，环境不匹配不得静默播放。
- MVP 支持同环境跨机器分享和管理页导入；导入文件必须经过完整性、环境、安全与隐私校验。
- MVP 后大型方向排序为：多人 Replay、跨版本播放、隔离的可游玩 Replay 分支、其他 gameplay mod 正式兼容。
- `MVP.md` Baseline v1.1 是当前产品范围基线；技术实验失败若要求缩减 Gate，必须经过显式产品复审，Implementation Codex 不得自行降低范围。
- 实验按 `EXPERIMENT_PLAN.md` 执行：定向参考地图 → v0.111 最小探针 → 性能/确定性 → 强记录 → 导航 → 时序/安全导入。
- INV-001 已验收，阶段 0 完成。Wave 1 拆为 SPIKE-001A 恢复基线和 SPIKE-001B capture/dispatch；后者硬依赖前者通过。
- SPIKE-001A 以有价值的失败完成。当前调用链被否决，SPIKE-001B 继续阻塞；在新的安全恢复实验通过前不得推进 action replay。
- INV-002 已于 2026-08-28 验收通过；接受其静态结论和唯一候选，但不把静态分析当成运行时恢复成功证据。
- 最终 Replay 观看必须是主菜单独立、非进度性会话；普通 Continue 在观看前后保持不变。该要求补强并取代仅在“停止时不写存档”的较弱理解。
- INV-003 已验收；SPIKE-001A2 结论为 Retain + revise，仅作为独立 reconstruction control，不能承担 session isolation 证明。
- RitsuLib v0.5.18 的外围能力保留为 Adopt candidate，但暂不作为 MVP 必要依赖；核心隔离使用直接游戏 API/Harmony 验证。
- Alchyr ModTemplate 固定为 `ADOPT_AS_SCAFFOLD_REFERENCE`：只在正式实现工程搭建时借鉴 Empty Mod 骨架，并固定依赖、分离 build/deploy、收紧 PCK 失败门禁；当前不安装、不迁移 experiments，也不因模板默认值引入 BaseLib 或决定架构。
- 社区 `GlitchedReme/SlayTheSpire2ModdingTutorials` 本地归档已评估为 `ADOPT_AS_REFERENCE_INDEX / NO_CURRENT_DEPENDENCY_CHANGE`。其 0.107→0.111 migration ledger 与 0.111 Hook Trigger Order 可显著降低版本 break 和 recorder capture surface 的静态定位成本；RitsuLib lifecycle/node/config/notification 能力提升为后续 observer/UI 外围候选，但未证明语义覆盖、性能或 session isolation，当前不加入 A2-R2 或核心依赖。归档 SHA-256 为 `1957F102973BCA1F8F089D8F25C0D70F31220091371CE33657F000D732083721`；评估见 `investigation-reports/REF-002-community-modding-tutorials-review.md`。
- SPIKE-INV003A 已按首个可审计 FAIL 结束并验收：接受 Phase A/B 的局部正面证据和 Phase C 的覆盖失败，不接受完整 session isolation PASS，也不否定 transaction 候选。
- INV-004 已验收；SPIKE-INV004A 已以 pre-override equivalence FAIL 安全结束。direct-write 尚未执行，不得把结果写成存档泄漏或 transaction 候选失败。
- SPIKE-INV004B 已以 `PARTIAL_DIAGNOSED` 验收：接受 Candidate B 本轮的精确等价与引用独立证据，但历史 Candidate A 差异未复现/定位，因此不解锁 direct-write sentinel。
- INV-005 已验收，唯一建议为 `REVISE_WITH_TIMING_GATE`。静态证据未发现跨越主菜单的未收束 production component writer，但不能证明 earlier `CallDeferred(InitializeGraphicsPreferences)` 已在 peer entry 可用前执行；等待 `NGame.GameStartupComplete` 仍不足以替代该 callback 的显式完成观测。
- SPIKE-INV005A 已以 `FAIL_SAFETY` 验收并退役；机器 `FAIL_GATE` 不能覆盖越界审计这一控制性失败。不得重跑、修补后继续或用其中局部顺序证据解锁 direct-write。
- INV-006 已验收并批准全新 `SPIKE-INV006A`。该 probe 只捕获零参数 `DoCloudSync` outer task 与 graphics/startup/menu/manager 状态，禁止 global store、path、payload 和 save/profile 访问；必须使用新 ID、目录、schema，并在安装前通过 contract tests 与 forbidden-surface audit。
- SPIKE-INV006A 已通过并退役；接受其 gate 作为后续实验 precondition，但不外推为 save/session isolation。
- SPIKE-INV006B 以 `COMPLETE — BLOCKED_PRECONDITION` 验收并退役：接受 timing gate、保护集合、candidate 等价/独立与 teardown 的正面证据；因两个 direct methods 和 memory write 均为 0，不接受任何 direct-write isolation 结论。下一任务只批准纯静态 `INV-007`，不得修改或重跑 006B。
- INV-007 已以 `READY_NEW_SENTINEL` 验收。下一任务只批准全新 `SPIKE-INV007A`：保留 timing/candidate/protection/boundary/teardown 安全门禁，按实际 runtime 参数独立计算 mutation oracle，删除固定角色/怪物身份、enemy count、encounter 重复对比与 executor/frame gate。不得修改或运行旧 probe。
- SPIKE-INV007A 原始 attempts 以 `INVALID_HARNESS_ATTEMPTS / NO DIRECT-WRITE CONCLUSION` 结束。接受 R1 pre-click timeout artifact 和其后丢失终态 attempt 的 recovery backup 为历史证据，不接受任何业务结论；当时只批准 `SPIKE-INV007A-R2` 所列的窄 harness 修订与一次受控运行。
- SPIKE-INV007A-R2 以 `VALID TERMINAL — ORACLE_ALIASING` 验收：接受 direct update candidate routing、boundary、teardown与保护集合局部PASS；拒绝把 `FAIL_MUTATION`解释为production失败，save仍未测试。下一任务只批准 `SPIKE-INV007A-R3`的detached-oracle窄修订。
- SPIKE-INV007A-R3 以 `PASS_DIRECT_WRITE_ISOLATED` 验收并永久封存。接受固定 session 中 direct update/save/唯一同步 memory write、payload、boundary、teardown 与 protection PASS；不得外推为自然 victory、fault/restart、replay fidelity 或完整 session isolation。下一任务只批准纯静态 `INV-008`，先制定 fault/restart 安全契约。
- INV-008 以 `READY_FAULT_RESTART_MATRIX` 验收。下一任务只批准全新 `SPIKE-INV008A`：四个fresh-process phase顺序验证setup/post-launch controlled owner fault及各自restart integrity；teardown failure独立分类，强制崩溃延期。跨进程mtime只采用`TEST_PROTOCOL.md`固定三项whitelist，不允许把正常退出写入称为零写入。
- SPIKE-INV008A v1以`PREBUSINESS ENVIRONMENT BLOCK / NO FAULT-RESTART CONCLUSION`结束。接受Phase 0，保留两份immutable blocked artifact；不接受任何fault/teardown/restart结论。批准`SPIKE-INV008A-R2`仅修复安装生命周期、configuration precheck、revision/artifact/recovery identity与prior-attempt gate，四阶段业务契约不得变化。
- SPIKE-INV008A-R2以`PREBUSINESS_PRECHECK_TIMING_DEFECT / NO R2 FAULT-RESTART CONCLUSION`结束。R2 precheck artifact固定为242 bytes / SHA-256 `2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916`；同进程日志证明实际只loaded 008A。批准R3只移动loaded-set观察点、提升identity并保留原业务矩阵；同时试行有次数上限的pre-business harness修复授权。
- SPIKE-INV008A-R3以`VALID ARTIFACT / IMPOSSIBLE PRIOR GATE / NO OWNER-FAULT OR RESTART CONCLUSION`结束。artifact为3,740 bytes / SHA-256 `5C8E49B98F7BC9A647033F779DC3CACCEBE3D840214E4D95995EBADB7C0B13F0`；loaded-set修复通过，但在Run按钮前封存`BLOCKED_PRIOR_INTEGRITY`。Project Lead只读hash复核确认profile1十项与AccountSelector内容仍全部等于007A-R3，唯一内容差异为包含mod启用配置的`settings.save`。另确认mtime与Continue两项gate实现本身不可满足。R3永久停止；批准R4采用fresh phase-local baseline。
- SPIKE-INV008A-R4以`VALID BUSINESS TERMINAL / FAIL_TEARDOWN_CLEANUP / ROOT CAUSE UNRESOLVED`结束并永久封存。artifact为23,241 bytes / SHA-256 `40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040`。setup fault点、owner catch/finally、保护集合、Continue、run state清理和manager恢复均通过；Launch及所有scene/map/room/action/reward/replay/progress路径均未进入。固定15步cleanup oracle只到达13步，缺少`RunLobby.remove_RemotePlayerDisconnected`与`RunLobby.Dispose`。`CleanUp`的IL表明二者受`RunLobby != null`条件控制，故当前只确认cleanup契约失败，不能提前确认production cleanup缺陷。restart phase未解锁。
- INV-009以`ACCEPT_PRODUCTION_CLEANUP_WITH_REVISED_ORACLE`验收。固定IL确认`NetReplayGameService.Type=Replay(4)`且`IsMultiplayer`只接受Host/Client；`InitializeRunLobby`的Replay分支不构造、不赋值、不订阅RunLobby，程序集唯一setter caller也只在multiplayer true分支。故R4 fresh-process路径的两个lobby调用均Not applicable，13项core call-set局部通过。setup/post-launch的`CleanUp(true)`调用义务相同，但post-launch完整teardown还必须替换已安装NRun scene。最大剩余风险是production cleanup dispose lobby后不清空backing field，可能使同进程multiplayer→replay继承stale lobby；R5须在所有边界对非null/identity变化fail closed。
- SPIKE-INV008A-R5以`VALID BUSINESS TERMINAL / MENU OBSERVER ORDERING DEFECT / NO RESTART CONCLUSION`结束并永久封存。artifact为25,790 bytes / SHA-256 `9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9`。lobby oracle、13项core cleanup、fault/catch/finally、manager restore、main-menu scene identity、protection与Continue均通过。固定IL与probe source确认`RefreshButtons`在production `_Ready`体内先调用，其postfix因probe仍保存旧menu identity而拒绝事件；随后`_Ready` postfix才更新identity。combined await异常又使两个local结果均保持false。日志`Invalid Task ID`发生在最初主菜单完成之后、RunAccepted约265秒之前，不是本次teardown timeout的直接原因。
- SPIKE-INV008A-R6 以 `PASS_CONTROLLED_OWNER_FAULT_RESTART_MATRIX` 验收并永久封存。四个连续 fresh-process phase 依次为 setup owner fault PASS、restart integrity PASS、post-launch owner fault PASS、restart integrity PASS；四份 artifact hash chain、两条 recovery chain、双 menu signal、phase-aware cleanup、manager/scene restoration、profile1 protection 与 Continue 均通过，业务计数均为零。该结果与 007A-R3 的 direct-write 证据共同解除 `SPIKE-001A2-R2` 的受控隔离前置阻塞，但不得外推为 crash recovery、自然胜利、完整 replay dispatch 或正式 MVP session isolation。Project Lead 复核见 `evidence/spike-inv008a/project-lead-r6-final-review.md`。
- 旧 `project-management/handoffs/SPIKE-001A2.md` 已退役，不得执行其 disposable/ordinary 双 profile、profile 切换或 restore helper 方案。唯一有效的新任务是 `project-management/handoffs/SPIKE-001A2-R2.md`：固定 profile1，在 baseline 建立前允许用户准备新测试 run；A 后必须证明原 checkpoint 内容未变，B 只允许 production normal Continue 已知的 `NumReloads +1` 及预声明非语义元数据变化。
- SPIKE-001A2-R2 已以 `ACCEPT_STOP / INCONCLUSIVE_HARNESS_OBSERVABILITY_FAILURE` 验收并永久退役。R3 只生成 pre-baseline arm；点击目标节点后 production 写入 room-before save 并创建 `SHRINKER_BEETLE_WEAK` ActiveCombat，但约五分钟仍无 baseline、A 或 terminal，B 从未运行。`TryReadiness` 会把异常静默转为 null，失败帧也不保存字段；现有证据无法区分 ticker lifetime、字段异常或 predicate 恒 false。operator `FAIL_ISOLATION_OR_PROTOCOL` 只是停止桶，不是隔离泄漏证据。Project Lead 复核见 `evidence/spike-001a2-r2/project-lead-final-review.md`。
- INV-010 已以 `READY_NEW_RECONSTRUCTION_CONTROL` 验收。固定 DLL 确认 map node entry 会 await `SaveRun(null,true)`，且只在 await 完成后创建 room；新 observer 可在把 Task completion 交还 production continuation 前 stable-read、验证并原子封存 `PreFinishedRoom=null` checkpoint 与 protection baseline。`ActionExecutor.IsRunning=false` 是当前 queue task 已完成的有效信号，但必须与 executor unpaused、current action null、current run node、target identity及30个连续process frames联合使用。R3的动态首因仍Unknown，但新契约用persistent heartbeat、逐字段null/exception telemetry和不依赖ticker的30秒deadline覆盖该缺口。
- 全新 `project-management/handoffs/SPIKE-001A3.md` 已签发。它固定profile1、第一幕普通弱怪、production room-before checkpoint、uninterrupted A与normal Continue B，只允许一个有效compare；正式baseline前授予最多两次窄pre-business harness revision，业务边界后任何harness失败都必须停止。PASS只进入Project Lead对`SPIKE-001B`的复审，不自动解锁capture/dispatch。
- SPIKE-001A3 已以`ACCEPT_STOP / INCONCLUSIVE_HARNESS_CHECKPOINT_IDENTITY_FAILURE`验收并永久退役。R1的row/col缺陷在pre-business envelope内修为R2；R2命中matching production `SaveRun(null,true)`，checkpoint的seed/character/act/null-room/coord均正确，但arm target以`TotalFloor=-1`占位，`ValidateTarget`在绑定captured floor前先要求相等，故必然`BLOCKED_CHECKPOINT_VALIDATION`。无baseline、recovery或A/B snapshot，有效compare仍为0。observer异常还逃逸到`MoveToMapCoordAction`并伴随黑屏，下一契约必须保证observer-only failure不污染production continuation。复核见`evidence/spike-001a3/project-lead-terminal-review.md`。
- 固定DLL的新增静态复核表明`RunState.TotalFloor`由`MapPointHistory`计数派生，而目标room history在room-before save之后才append；checkpoint floor与A/B playable floor可能是不同语义，不能直接用一个字段或猜测`+1`。纯静态`project-management/handoffs/INV-011.md`已签发，先闭合四个floor语义、target模型、Continue恢复和observer异常分支。
- INV-011已以`READY_SUCCESSOR_RECONSTRUCTION_CONTROL`验收。确认`CheckpointFloorReached = ArmPredecessorFloor`，且A/B playable `TotalFloor = CheckpointFloorReached + 1`；`ActFloor=row+1`是独立act内语义。target必须拆分arm-known、checkpoint capture-later、A-bound room/encounter与phase-local object identity，禁止numeric/string/default placeholder参与PASS。production success后的observer-only failure必须被probe吞掉并停止实验，不得fault map action；production自身fault/cancel原样保留。Project Lead重跑40/40 contract tests及2,307行IL输出，hash一致。复核见`evidence/inv-011/project-lead-review.md`。
- 全新`project-management/handoffs/SPIKE-001A4.md`已签发，继承A3已验证的baseline/readiness/protection框架并强制落实INV-011全部audit；A4仍只允许一个有效A/B compare，PASS不自动解锁001B。
- `project-management/handoffs/TOOL-001-local-decompilation-cache.md`已作为non-blocking工具任务签发：生成workspace外、按DLL hash/version分目录、可重复验证的本地反编译导航缓存；反编译C#不进入仓库且不取代原DLL/hash-gated IL证据。
- SPIKE-001A4已以`ACCEPT_STOP / INCONCLUSIVE_POST_BASELINE_HARNESS_FAILURE`验收并永久退役。唯一r2业务attempt已命中production room-before save并封存正式baseline/recovery，`current_run.save`退出后raw bytes不变；但A snapshot、B与compare均为0。源码确认baseline seal后误完成deadline，无法对A readiness无界等待产生30秒终态；原进程ticker/readiness动态首因未知。退出后`replays/latest.mcr`和`settings.save`内容变化，保护比较FAIL且原因未归因。后续独立进程证明A4持久disabled；该cleanup进程加载AscensionUnlocker并进入profile3游玩/写入，属于no-run核验协议FAIL，不覆盖A4业务terminal，也不允许检查其他profile。复核见`evidence/spike-001a4/project-lead-final-review.md`。A4保持disabled，禁止再运行或恢复。
- `TOOL-001`已验收：固定v0.111.0/hash外部缓存manifest及3,538个C#导航文件存在；反编译输出仍为non-authoritative，正式结论必须回到固定DLL/IL。报告见`investigation-reports/TOOL-001-report.md`。
- 纯静态`project-management/handoffs/INV-012.md`已签发，审计A4后基线deadline、原进程telemetry持久性和退出写入/protection边界；不得运行游戏、访问当前存档或创建successor probe。
- 2026-09-27按用户建议调整测试工作流：最初的固定v0.111.0导航源码显示`settings.save`是Steam账号级/本机设置，modded profile1的`current_run.save`、`progress.save`、`prefs.save`及`replays/latest.mcr`为profile作用域；A4的逻辑路径计算符合现有运行路径，但跨正常退出要求所有内容恒等的oracle过粗。`NGame.Quit`保存Settings、`NRun`退出可触发`RunManager.CleanUp`并写原生replay；这些机制随后由INV-012固定DLL/IL复核，但A4具体内容差异的调用/字段仍未知。Godot支持`--headless`，但正式v0.111.0 `autoslay`入口被`!IsReleaseGame()`门禁排除；headless不等于存档/Cloud隔离。`TEST_PROTOCOL.md`已加存储分类与离线/无头分级。
- INV-012已以`ACCEPT / READY_NARROW_DIAGNOSTIC`验收：固定v0.111.0 IL证实account/modded/profile1存储拓扑及`NGame.Quit/SaveSettings`、`NRun.CleanUp/WriteReplay`条件性writer；A4 baseline seal后取消唯一30秒timer，原业务进程的ticker/readiness动态首因与两项内容变化的精确调用方仍未知。Project Lead独立重跑14/14合成合同测试。`settings.save`/`latest.mcr`内容变化不事后豁免，A4仍无有效A/B。验收见`evidence/inv-012/project-lead-review.md`。
- HarmonyLib `[HarmonyPatch]`适合作为新诊断的只读方法观察入口；本地程序集为2.4.2.0。它不是存档隔离、deadline或无头自动化方案；新工作包限制为observer-only Prefix/Postfix，不改生产Task/结果/异常，详见`investigation-reports/REF-003-HarmonyPatch-assessment.md`。
- `project-management/handoffs/SPIKE-INV012A.md`已签发，**仅Phase 0离线**：新身份、固定IL patch审计、原进程有界持久telemetry/独立deadline、合成/race与patch/unpatch测试，以及未来人工运行草案。未获准安装/启用、启动STS2或读取当前用户状态；Phase 0通过后仍须Project Lead另行批准运行阶段。
- SPIKE-INV012A Phase 0 r5 的离线证据已独立复核：Release 0 warning/error、28/28合成测试、9/9 patch/unpatch和dummy non-interference通过；9目标/16回调的IL审计重跑输出hash与r5一致。但`DiagnosticCore.Start`在证据失败封存后仍可能由调用方追加业务记录；`MainMenuReady`在profile/loaded-set身份门前写boot，门失败或owner缺失时静默返回。分类为`OFFLINE_EVIDENCE_ACCEPTED / RUNTIME_HOLD`，不是生产runtime PASS。复核见`evidence/spike-inv012a/project-lead-phase0-review.md`。
- 同一探针的`project-management/handoffs/SPIKE-INV012A-Phase0-R6.md`只批准离线窄修：终态后不追加业务记录、身份门前不写attempt文件、失败与owner缺失有明确prebusiness标记，保留r5候选及证据。r6经Project Lead再审前不得安装/运行。`.config/dotnet9`仅余空目录，不清理也不影响本阶段。
- `SPIKE-INV012A` Phase 0-r6已以`ACCEPT_PHASE0_R6 / READY_RUNTIME_PREFLIGHT`验收：r6 DLL 52,224 bytes / SHA-256 `CA1DAF85B5C12AE043A2BF20E7D9026DA894D336F649913A7E1D1BE2C69C1720`；Project Lead独立重跑Release 0 warning/error、35/35合同、9/9 patch/unpatch、dummy非干扰和三份IL/白名单审计，输出hash均与封存一致。两处r5缺口已关闭；不外推为真实ticker/writer或A/B。复核见`evidence/spike-inv012a/project-lead-phase0-r6-review.md`。
- 当时只批准`project-management/handoffs/SPIKE-INV012A-RUNTIME-PREFLIGHT.md`的只读配置/保护预检：新probe未安装时确认当前modded profile选择、退役probe/AscensionUnlocker持久enabled状态、固定候选/manifest与保护窗口可满足。该预检已完成，其原保护前提随后被2026-09-28用户澄清修订；历史记录不构成当前下一任务授权。
- `SPIKE-INV012A`运行前只读预检以当时r6合同的`ACCEPT_BLOCKED_CONFIG`验收：Project Lead独立重读两个精确配置文件的范围过滤字段与hash，确认`last_profile_id=3`、`mods_enabled=true`、唯一持久enabled ID为`AscensionUnlocker`，游戏进程为0；r6仍未安装，固定身份通过，Steam Cloud当前状态未知。固定IL显示启动先按selector初始化profile作用域，再读Progress/Prefs。2026-09-28新授权使“触碰modded profile3”不再是个人数据安全阻断，但r6代码的精确身份门依旧不接受该配置；旧预检报告作为历史快照保留，不外推为当前环境不可用。复核见`evidence/spike-inv012a/preflight/project-lead-review.md`。
- 旧`project-management/handoffs/INV-013.md`已标记`SUPERSEDED_UNEXECUTED`：其目标是避免任何modded profile3接触并调查禁用`AscensionUnlocker`的配置安全路线，该保护前提已被最新用户说明撤销。不得执行旧工作包；未来环境准备应缩小到诊断有效性与unmodded profile1边界，不再追求全面禁止modded槽状态变化。
- 所有当前与后续运行 handoff 遵循 `TEST_PROTOCOL.md`：默认modded profile1，若任务明确指定则可用modded profile2/3；unmodded profile1不得进入或改动。Codex只准备/分析，项目发起人手动运行游戏。
- 测试prompt不得混淆两棵存档树：modded profile1 Continue是实验哨兵，不是个人连胜；**unmodded profile1 Continue才是用户要求保护的真实普通对局**。当前modded实验结果不能外推为已直接验证unmodded profile1安全。现场用户澄清必须记录并覆盖旧假设，改变运行合同则交回Project Lead评审。

## 下一项最值得做的任务

下一优先规划PERF-001的离线测量合同，先定义无探针基线、空观察/候选记录对照及地图/卡牌/空闲指标，避免用DET诊断I/O或人工等待代替性能证据；当前未派发/批准新实现或游戏运行。DET-001/A5/001B关闭，不再测试；STATE-001权威恢复/后退能力和完整独立会话仍需独立证据，不认定阶段1完成。个人槽/另一账号继续排除。

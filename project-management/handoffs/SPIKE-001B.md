# SPIKE-001B 最小出牌与结束回合capture和dispatch

签发：2026-10-22；关闭：2026-10-23。状态：`CLOSED / ACCEPT_TWO_ACTION_TRAJECTORY_CONTROL`。实际两操作/三点比较及收尾已验收，不再执行下文历史授权或使用剩余预算。Implementation报告/交付索引保持封存，PM验收见文末。

继续现有「完成 SPIKE-INV012A 工作包」实现对话，不创建新对话、不覆盖模型。先读CURRENT_STATUS、WORKFLOW、TEST_PROTOCOL、A5 PM最终验收及本包，按需参考INV-001/002与已固定源码/IL，不完整重做旧调查。A5所有source、sealed候选、报告、索引与private证据只读，不复活旧probe。

## 唯一问题

在同一room-before checkpoint重建的战斗中，能否记录玩家实际成功执行的一张普通卡和一次EndTurn，并在fresh正常Continue B中只按这两条语义操作驱动游戏，使初始、出牌后、下一玩家回合三个稳定点的规定语义投影分别与A相同？

这仍是单房间开发者探针，不是正式Replay Mode或隔离方案。普通Continue占用可消耗modded1仅是实验control；正式产品不占用正常Continue、不切Replay profile的要求不变。不加入胜利/奖励、第二个房间、选择界面、seek、强记录纠偏、播放UI、倍速、跨版本或多人。

## 身份和复用

- 新Mod ID `SPIKE001B`，独立owner/private namespace/attempt；source `experiments/SPIKE-001B/`，evidence `evidence/spike-001b/`，报告`project-management/investigation-reports/SPIKE-001B-Phase0-report.md`。
- 固定v0.111.0 / 41cef1ea、sts2.dll SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`、Harmony2.4.2.0。Phase0只用固定DLL、项目公开fixture和synthetic数据，不读取A5 private raw checkpoint。
- 复用A5正确floor关系、匹配production SaveRun后的before-room封存、Task非干扰、真实ProcessFrame、独立deadline、有界诊断和语义投影；拷贝必要模块到新身份，不改A5，不重构通用测试框架。
- 默认原A5测试账号/modded1、正式only B；未来配置/安装另待候选验收。本包不调查另一账号，也不以删除其目录为前提。当前所有mods disabled，不启动游戏探路。

## 操作与状态合同

1. A新准备Standard Ironclad A0普通Monster入口，沿用A5 seal和初始30帧门；不得假定换seed也必然重建等价。B必须先通过本attempt初始投影相等，才能分派。
2. 第一条为无额外选择、不会结束战斗的普通卡，优先Defend/同类简单防御牌。用固定content/API明确选择规则和合法性，不假定起手必有某卡；给出失败时停手或Arm前准备条件，不在业务中盲换卡。第二条为一次EndTurn，之后只等下一玩家回合稳定，不再出牌。
3. 记录逻辑层**实际接受的操作**，不是鼠标按下、动画或任意失败尝试。窄查固定v0.111的manual-play/EndTurn调用链，明确hook、签名、成功/排队/执行完成语义；不靠名称猜测。取消、拒绝、失败请求不算accepted事件，重复hook不重记。
4. event至少含序号/type、稳定phase/round、card model及相应手牌定位/有效属性指纹、必要目标语义身份。跨进程不使用object reference或未经证明稳定的临时ID定位；B先验证匹配，再取自己的live实例调用已确认的正常游戏操作API。
5. B仅允许CardPlay和EndTurn各分派一次。不得直接改HP/牌堆/RNG、跳过production效果、替换结果或额外FromSerializable/setup/load；B自己的事件不回灌成A录制。非法输入、缺字段、重复/越界/未知type应被拒绝。
6. 保存初始、出牌后、EndTurn及敌人阶段结束后的三个稳定语义snapshot，每步都比较，不只比最终摘要。复用完整A5投影；Round检查按明确phase设为1/1/2，不能保留固定Round1导致永远不Ready，也不能简单删除round约束。目标位置/floor/room/encounter及30相邻帧、20字段、250ms门保持，读字段异常和missing不填默认值。
7. readiness/action-complete需证明本条被接受、效果已收束，不能只因队列一瞬为空就推进。定义每阶段独立deadline、最后阻断字段与终态；人工等提示不设超时，不逐帧序列化/写盘。单调时间可辅助顺序诊断，本包不实现MVP原始节奏。

## A/B输入与非干扰

后续拟采用A不断线入房、手动一牌/EndTurn后停手退出；same checkpoint exact bytes才允许fresh B正常Continue。预先核对这两个动作及正常退出的native写入边界；如果会使B输入变更，指出具体条件，不恢复/覆盖native，不临时扩大checkpoint类型。

checkpoint raw/path只在private目录，公开只导出规定投影/metadata/hash；数据范围、大小、schema/版本和解析上限在Phase0冻结。其他账号和个人unmodded profile1不触及。不复制十二份recovery，不继承无关全文件恒等保护。

匹配room-before保存的Task wrapper沿用A5唯一例外，保持success/fault/cancel及原异常语义；新增观察hook的失败不得改变production参数/结果/Task或逃逸。只有明确的B分派阶段允许调用那两个已核实操作API，不引入其他生产调用、全局manager/store override或replay框架。

## 离线验收与权限

- 在当前包内窄查新用API/IL，列出Confirmed/Inferred/Unknown。能直接定位和修复的harness bug自行修，不新增INV或逐次请示；真正未解决的业务/安全边界以具体证据交PM，不堆笼统疑问。
- 完整正向合同须包含seal→A初始→accepted CardPlay→post-card→accepted EndTurn→next-round→A完整提交→fresh B相同输入→两次dispatch/逐步compare。使用A5真实公开投影与synthetic后继状态，标明来源；不得将synthetic链当作运行通过。
- 有区分力负向测试：未接受/重复操作、card定位歧义或牌序变更、目标缺失、B回录、错round/phase、未收束/永不Ready、单个HP/牌属性/RNG state差异、输入变更、超时/写失败/terminal race。expected由独立serialization建立，不污染live/候选图。
- 设计明确的fresh-A与ready-B门：只有A操作日志和三个快照完整commit才允许B。用户中断/未完成Arm不能自动走B；保留原attempt，不以清旧active或修改业务binary作为常规重开手段。首终态结束本attempt，不永久锁死整个lineage。
- Release无warning/error，相关合同、patch/unpatch、真实dummy非干扰与新调用白名单审计通过。使用已有.NET9工具，不安装系统SDK；未变化的游戏API证据不重复整库查询。
- 提交候选/manifest/源码hash、读写与event/phase/比较合同、固定API证据、可复现验证、未来逐步人工请求。人工方案说明唯一卡/目标、何时停手、A退出和fresh B先后，避免模糊“打一会儿”。

Phase0内允许有限范围自行修harness并保留sealed证据，不能扩大业务问题或降低标准。后续配置/安装/人工运行及有限修复会在**同一工作包**放行，不为每个checkbox新建任务；现在交回PM验收离线候选后停止，尚无运行权限。

## PM复核后的Phase0-r2动作诊断窄修

2026-10-22分类`ACCEPT_OFFLINE_CORE / REVISE_ACTION_DIAGNOSTICS / RUNTIME_HOLD`，见`evidence/spike-001b/project-lead-phase0-review-2026-10-22.md`。r1核心、固定API、完整synthetic链与19份source身份已验，唯一必须修的是动作阶段观察证据不足，不是另查production根因或重做整个探针。

- 复用现有Queue/BeforeExecuted/CardEffect/EndReady/AfterFinished/Cancel/Choice回调，冻结有限的**最后已观察**动作摘要：seq/type/step/round/PlayerTurn（present时）、当前观察site、Executed/效果是否观察到及其已观察Task状态、End-ready转换、Finished/取消/选择/有无production exception等必要状态。只保存已有事实，不能把未观察到填成当前一定失败，也不让诊断改变accepted标准。
- timer只读Flow内复制/冻结的诊断DTO，不访问live Action/card/player/Task或其他Godot对象。终态保留最后site/flags，不能被reset覆盖；字段/record使用明确白名单和大小/条数上限，不保存object refs、NetId、完整Action/模型、异常自由文本/stack。最多两个动作，按有限阶段变化写入即可，不做逐帧日志或额外通用trace平台。
- 对Queue/CardEffect/EndReady/AfterFinished等已有受控契约拒绝，至少持久化有限site和可区分code，不再全部只剩`observer-InvalidDataException`。意外生产异常不能拿message当可信code，也不得被observer抛回production。没有新生产patch/API效果调用，原Task/参数/结果不变。
- 新回归必须实际回读持久事件/terminal，区分queued-only超时、执行但未见效果、已见效果但无AfterFinished、End-ready已见但无完成，以及受控拒绝site；测试最后观察冻结、无共享引用、重复通知/条数上限、writer失败与terminal竞态。尽可能经过实际使用的诊断adapter，而非仅手工构造期望JSON；不用无engine的生产target制造测试崩溃。
- 新诊断不能让未accepted操作成为成功；全部88项核心语义、三点Round1/1/2、floor、20字段30相邻帧250ms、deadline、两次dispatch/早期差异停分派、完整ReadyB/hash/CP门保持。补首玩家PlayerTurn初值/Increment/SwitchSides关系的窄IL说明，避免把Round或synthetic turn混作真实初值；不新发INV。

可自行窄修并补测试/版本化合同，不为代码编辑逐次申请。保留r1 sealed pair、报告、951文件交付索引及其目标原hash；r2使用独立工作副本或等价可复现保存方案，仍是同一SPIKE001B而非新probe/任务。诊断格式按实际变更版本化，不和旧输入混用。执行Release、完整正向/相关回归、受影响patch/dummy/白名单审计；未改的游戏API不重复调查。

提交`SPIKE-001B-Phase0-R2-report.md`、r2候选/hash、增量证据及复现命令后等PM验收。本节不授权账号配置写入、安装/启用、读玩家状态或游戏运行，尚无业务attempt被消耗。

## 已授权runtime 配置安装和人工A/B

2026-10-22 PM验收`ACCEPT_PHASE0_R2 / AUTHORIZE_TWO_ACTION_RUNTIME`，见`evidence/spike-001b/r2/project-lead-review-2026-10-22.md`。下列步骤在同一工作包顺序完成，不为每步再问PM；到游戏动作时由Implementation提出明确请求并等用户回传。禁止Computer Use、其他GUI自动化或自主启动STS2/正式headless。

### 唯一候选和数据合同

- 源/候选：`experiments/SPIKE-001B-r2/`、`artifacts-phase0-r2/SPIKE001B.dll/json`；同ID SPIKE001B，owner `STS2ReplayMod.SPIKE001B.TwoActions.phase0-r2`，private revision phase0-r2/schema2，manifest0.1.0-phase0-r2。
- DLL200192 bytes / SHA-256 `F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7`；manifest367 bytes / SHA-256 `22F4DAFF17705B748B69C6EB16EA089D361D868AA373266CF8F3C185A6AFDD65`。安装及每次启动前核对固定v0.111.0/41cef1ea、game hash与Harmony2.4.2.0。
- 业务/人工步骤沿用已封存`evidence/spike-001b/data-contract.md`、`manual-plan.md`，本节放行其流程并采用r2身份/诊断格式。r2增量见`r2/diagnostic-contract.md`、`player-turn-contract.md`；旧草案的DRAFT/HOLD不再作为当前阻断，另建实际请求，不覆盖sealed文档。
- 允许所选原账号modded1的native current_run只读及本轮probe private读写，native≤4MiB、一般JSON/semantic≤1MiB、diagnostic4096/8192 bytes及32次动作摘要/Flow等上限维持。raw checkpoint/path/完整配置备份只留private，公开只选metadata/semantic/events；不整private目录glob导出，不读另一账号或个人unmodded1。

### 游戏关闭时配置和安装

1. 进程=0、候选/固定游戏/manifest身份通过，盘点本轮本地及Workshop已安装manifest的id/source、已知旧probe pair。001B目标不存在则create-new安装；已存在只接受本包同hash，其他hash不盲盖。A5等旧probe不重新安装/启用。
2. 仅选原A5测试账号：当前Steam ActiveUser应唯一对应目录，并与已保留的原probe账号scope一致。允许只在内存读取已知A5 `native-path.private.json`的账号路径元数据作scope比对，不读其raw checkpoint/存档、不改旧private文件；不要因多账号目录存在就检查或清理另一份。不按最近日志/mtime选账号；不一致只确认当前登录，不追问另一份删除情况。
3. 授权范围明确的离线配置：原账号精确settings.save的mod_settings.mods_enabled=true、本地SPIKE001B精确source启用，其他当前installed local/Workshop及全部退役probe明确disabled；modded/profile.save.last_profile_id=1，已是1则不写。只改这些字段，保留mod记录，非mod值及selector其他字段canonical不变；不编辑native/进度/Cloud或unmodded selector。
4. 当前schema/source规则按既有固定证据和实际配置确认，不猜写。需改文件先create-new完整备份到SPIKE001B自有private config-backups，写前重验game=0/原hash无并发变更，写后范围比较与回读；公开仅mod IDs/source/布尔、selector及hash，不输出账号数字/绝对私有路径/完整配置。无法保证范围就停在离线准备。
5. 有效安装/持久启用仅B、modded selector1通过后才请用户现场确认Cloud关闭并手动启动；不能先零有效mod启动再进设置启用。配置与actual loaded分别核对，身份拒绝且未Arm可退出离线修正，不消耗正式记录；不得试启动猜账号。

### A 手动录制两条操作

1. 用户手动启动只停modded1主菜单；实现者核对本进程actual only SPIKE001B、固定candidate、profile/原账号scope、first-tick与`PRECHECK_PASS READY_A_ARM`，通过后才允许新局。不复用旧进程或A5 precheck。若最新journal已经存在，先确认是fresh A还是完整A可用的B，不能误用中断A。
2. 按`manual-plan.md`新Standard单人Ironclad A0，允许用正常UI放弃/覆盖该modded测试槽旧Continue；不触及个人槽。可在正常建局UI支持时使用已知A5 seed JFZS2H4V4XN2，不改存档固定seed。完成Neow到地图停手，确认后F8一次，按READY_A_NODE与当前地图说明唯一direct Monster再点击一次；截图仅在文字无法唯一对应时需要。
3. 等seal及初始point0稳定capture、无terminal、READY_A_CARD。实现者核对最左合格未升级Defend的实际hand index/model/完整指纹，再请求用户只打当前左起第index+1张到自身、立即停手。无合格牌、拒绝/选择/胜利/死亡/异常就停止，不盲换牌或重点击。
4. 只有A.action-1成功、post-card point1完整capture、READY_A_ENDTURN通过，才请用户正常结束回合一次并停手。保持聚焦；不再打牌/EndTurn/F8、不暂停、药水、领奖或下一节点。等敌人阶段和Round2 point2；动作与capture各自20s，约30s无完成/终态就检查有限证据并安排正常退出，不无界等或补动作。
5. A完成须核对两条accepted、三snapshot/capture、A.complete及A.ready-B proof全部hash/phase/candidate/CP绑定、终态不存在，以及A_COMPLETE_STOP_INPUT。动作intent、Requested或Finished观察旗标本身都不是accepted成功；Card须原效果Task成功＋原Action完整成功，End须scheduled turn/实际ready转换＋原Action成功。诊断头部的Round/PlayerTurn是已绑定前状态，不冒充回调当时live值。
6. A完整后用户保存并退出到主菜单，再退出游戏。进程0后检查本轮native/private CP exact bytes仍seal，A所有commit完整且candidate不变；变化则阻断B，保留本attempt交PM，不restore/rebaseline。B准备允许同范围mod配置回读/修正，但修正后再检查同CP，不修改A的journal/记录。

### B fresh正常Continue和两次逻辑分派

1. 上述A门通过后可直接请求fresh B，不再等PM签下一phase。用户重新现场确认原账号/Cloud关闭，手动启动只到主菜单；核对same candidate/CP、fresh process、actual only B/modded1、一次B.claim及READY_B_CONTINUE。
2. 用户只正常Continue一次，保持聚焦并停手。明确告知接下来卡牌和EndTurn由probe自动调用正常逻辑API，用户不补任何输入。初始稳定点相等才分派Card，post-card相等才分派End；正常Continue的reload保存是预声明B写入，不误作输入门失败。
3. 核对各点compare、B.dispatch意图与B.action成功记录、观察site/flags、B.complete及trajectory-complete、无terminal。三个点完整相同、两端各两条成功操作和完整窗口才是完整两操作SEMANTIC_EQUIVALENT；不能只数dispatch intent文件或把通用Finished旗标当成功。此版最终文件为B.complete及compare-point-0/1/2，不要求A5式compare.json。
4. 初始差异：记录ComparedPoints=1、操作0，只能说明该新样本初始重建不同，没有验证动作轨迹。post-card差异：比较2点/操作1，EndTurn必须未分派；next-turn差异：三点/两操作但不等。所有有效差异都停，不追求PASS重打，不伪称未完成点已验证。terminal/缺commit则无有效完整轨迹结论。
5. 任一有效答案或停止条件后，用户按A相同正常退出路径结束。仍禁止出牌/EndTurn/领奖/下一节点，不Alt+F4/强杀。B退出后不要求native继续等于A原始字节，因为Continue已正常保存reload；不得恢复文件。

### 有限修复和重准备

最多**3条已Arm正式记录**：首次完整A/B，外加最多2次fresh完整重测，用于已定位harness窄修，或明确记录的用户中断/不合格fixture重新准备。中断/缺牌不伪称harness或production失败，旧attempt保留；revision变化不重置上限。配置precheck未Arm不占业务数，但未知账号/版本/数据副作用不能靠反复启动试错。

实现者可在同包直接修ticker/提示/观察点、诊断/deadline/race、证据生命周期及已证明的等价身份/序列化实现错误，包含baseline之后；无需为每次编辑/安装再问PM。先定位并保存旧source/候选/日志，新candidate与可复现版本证据，补针对回归/相关Release/patch/dummy/白名单审计；游戏关闭、旧live pair匹配且workspace可恢复时，只替换本包两个文件，再请求fresh完整A/B。A与B同candidate/同本轮CP，不拼失败A和新B，不覆盖旧artifacts；普通用户中断优先用现有journal fresh-A，不换binary。

不得降低accepted成功、Round1/1/2、floor/readiness/30帧250ms、投影或ReadyB/CP门，不新增Game效果API、manager/store override、修RNG/HP或自动UI。唯一已有SaveRun wrapper和B的两条正常操作API例外继续有效。不合格/失败的具体原因只能据证据报告，通用observer错误不能凭猜测归因为缺牌。未知production fault/副作用、输入变化、个人槽风险、无法保持标准、已有有效答案或预算用尽时交PM，不扩张场景。

### 交付和收尾

提交`SPIKE-001B-runtime-report.md`及attempt/build/commit/比较/退出索引，分别列每点结果、intent/accepted/完成计数及证据来源、input/provenance、用户动作与Cloud报告、准备/业务/退出写入窗口和预算。公开只导出本包白名单；raw checkpoint/path/完整config备份始终private。交PM独立验收，不自动解锁PERF/DET/产品能力。

游戏退出后可不可覆盖归档公开文件；持久禁用001B/退役probe，并允许移除**本包安装、hash精确匹配、workspace可恢复**的两个DLL/manifest，不递归清理或动A5/其他probe/任何证据。恢复非probe开关时仅按实际准备记录，绝不重新启用退役probe；无有效mods就明确预计下次unmodded，不为确认启动。若保留installed pair须disabled；不改另一账号或验证个人槽安全。

## PM验收关闭

2026-10-23：`ACCEPT_TWO_ACTION_TRAJECTORY_CONTROL / CLOSED`。唯一Arm1/3，seed EBJVET20T859、SHRINKER_BEETLE_WEAK，人工A与fresh正常Continue B的Defend＋EndTurn均实际accepted且完成，三点Round1/1/2逐字节相同。158份交付/92原始记录、同CP/fresh provenance、完整commit、六稳定窗口及收尾通过；候选r2未改。PM直接复核game0、两个精确live安装文件已移除且workspace可恢复、18个其他项目安装文件保持，持久disabled等以封存收据/脚本范围为据。

验收：`evidence/spike-001b/project-lead-runtime-review-2026-10-23.md`；只读复现`evidence/spike-001b/Verify-ProjectLead-runtime.ps1`。源码/所有证据保留，不重放准备/收尾、重装或重跑001B。三点中已投影RNG未变化，一般随机性、完整战斗及产品隔离未证明；下一DET最小合同另签，当前无额外游戏测试。

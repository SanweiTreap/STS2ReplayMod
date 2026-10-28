# SPIKE-001A4 Implementation Codex Handoff

状态：Ready  
发出日期：2026-09-17  
任务类型：受限运行实验；A3 successor reconstruction control

## 任务目标

使用全新identity在固定v0.111.0、modded profile1和第一幕普通怪节点中，对同一个production room-before checkpoint比较：

- **A**：checkpoint写入后不中断地进入目标战斗，在首个稳定可操作点捕获语义状态；
- **B**：新进程从主菜单正常Continue同一checkpoint，在相同可操作点捕获语义状态。

只回答A/B reconstruction equivalence。不得实现capture/dispatch、Replay session、快速重演、强记录、纠偏、seek、后退或正式架构。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/handoffs/SPIKE-001A3.md`（只读历史契约）
4. `project-management/investigation-reports/SPIKE-001A3-report.md`
5. `evidence/spike-001a3/project-lead-terminal-review.md`
6. `project-management/investigation-reports/INV-011-report.md`
7. `evidence/inv-011/successor-contract.json`
8. `evidence/inv-011/placeholder-validation-audit.md`
9. `evidence/inv-011/project-lead-review.md`
10. `project-management/TEST_PROTOCOL.md`

`SPIKE001A3`全部source/binary/artifact/recovery均只读且永久sealed。可以把A3 source作为阅读参考，但不得原地复制后继续使用A3 identity或目录。

## 固定identity

- 游戏：public-beta `v0.111.0`，commit `41cef1ea`。
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- INV-011 report：`7BE386861AC14433F4C61FB42A93C3C9879AF6B2B7B0E42D6463C2F50CAF2DBB`。
- successor contract：`1527728A8481D98D357D4484D7A3269153F2F9A918B2657A02A3C9096511CFB7`。
- placeholder audit：`C5CF56B9C11CF4D43893ADE5E4C5E73AD9F0DDBEDAD262787AE5F6C609258843`。
- 新mod ID：`SPIKE001A4`。
- 新schema：`spike-001a4.reconstruction-control.v1`。
- artifact/recovery namespace：`spike-001a4-r1-{attemptGuid}`，create-new、不可覆盖。

任一固定输入不符则`BLOCKED_IDENTITY`。源码位于`experiments/SPIKE-001A4/`，证据位于`evidence/spike-001a4/`，报告为`project-management/investigation-reports/SPIKE-001A4-report.md`。

## 冻结的floor与target模型

arm时必须独立捕获并验证：

- live `RunState.TotalFloor`；
- non-null predecessor save的`FloorReached`；
- 二者均present、非负且相等，绑定为`ArmPredecessorFloor`；
- `ExpectedPlayableFloor = checked(ArmPredecessorFloor + 1)`；
- non-empty seed、present character、zero-based act、present target coord `(row,col)`与typed map point。

room-before checkpoint capture时：

- `CheckpointFloorReached`初始必须是显式`Unbound`，不能用numeric sentinel；
- stable-read/parse candidate后，先验证其floor等于`ArmPredecessorFloor`，再一次性绑定`CheckpointFloorReached`；
- checkpoint的seed、character、act、last visited coord、`PreFinishedRoom=null`、与arm-time non-null save不同等原门禁继续保留；
- baseline只在全部validation通过后封存。

A/B playable readiness只比较`RunState.TotalFloor == ExpectedPlayableFloor`。不得用checkpoint floor或`ActFloor`替代。B setup前必须证明save `FloorReached`、`RunState.FromSerializable(save).TotalFloor`和sealed `CheckpointFloorReached`相等，并证明last visited coord、seed、character、act及null pre-finished room一致。

## placeholder与语义身份修订

必须完整落实INV-011 audit，不得只修floor：

- unknown/missing使用typed state，不得以`-1`、empty、`<null>`、default struct或伪造fallback参与PASS比较；
- checkpoint要求exactly one player、non-empty seed/character、`VisitedMapCoords.Count>0`；
- A capture绑定present、non-empty的actual room model identity与actual combat encounter identity；二者不得互相复制；B必须匹配A绑定的语义值；
- runtime state/player/hand/combat/executor/room object identity只用于各自phase内30帧连续性，不能进入跨进程A/B semantic projection；
- snapshot required card model id、RNG seed、room/encounter、creature fields若missing/exception必须阻止capture，不得用CLR type、`-1`或推导bool冒充；诊断fallback必须与权威字段分离；
- A/B比较使用专门的semantic projection，不通过把非语义字段改成空/零/null来“normalize”。

## observer non-interference

wrapper必须通过真实`Task`测试并满足：

- production success + observer success：wrapper `RanToCompletion`，实验继续；
- production success + observer validation/evidence failure：best-effort first-terminal，停止所有实验采集，但wrapper仍`RanToCompletion`；observer异常不得逃逸到map action；
- production fault：best-effort terminal后保留原fault/exception semantics，terminal writer失败不得替换；
- production canceled：best-effort terminal后保持Canceled及原cancellation semantics；
- terminal writer失败：仅memory latch + bounded fallback log/UI，永远不改变production outcome；
- deadline与RoomEntered共用一个compare-exchange terminal latch，loser不写artifact、不改classification、不影响production。

terminal后production可以继续；实验必须停止。禁止baseline/recovery、readiness accumulation、A/B snapshot和comparison继续发生。

## Phase 0：实现与离线门禁

以A3已验证的persistent ticker、逐字段telemetry、30连续process frames、ticker-independent 30秒deadline、protection/A-post/B-pre/save diff为基础，但使用全新A4实现与identity。

除INV-011的定向修订外，A3安全边界不得放宽。必须完成：

- Release build 0 warning/0 error；
- INV-011 40项contract tests继续通过；
- concrete wrapper使用真实completed/faulted/canceled `Task`验证status、exception identity和writer-failure分支；
- floor positive/negative/overflow、capture-before-bind、double-bind、readiness-before-bind、checkpoint-vs-playable混用测试；
- missing/default/empty字段、room/encounter分离、phase-local identity、semantic projection测试；
- zero/mid-run ticker、deadline/RoomEntered race、terminal后禁止采集、create-new冲突测试；
- compiled IL/source audit证明observer-only catch不重新throw到production continuation。

先回传Phase 0结果、安装/启用计划与精确人工请求；Implementation Codex不得自行启动或控制游戏。构建与live安装为两个gate。安装前必须让用户在A4尚未安装的进程中确认A3及全部retired probes disabled并正常退出，再按`TEST_PROTOCOL.md`验证持久化loaded set。

## 有限harness repair envelope

同一A4 Implementation Codex task内最多允许2次窄revision，保留旧artifact/log并提升build/schema/namespace。允许范围：precheck/UI、ticker/deadline/terminal writer、typed telemetry、observer hook未命中，以及**已命中production completion但尚无baseline/recovery/snapshot的纯observer实现缺陷**，后者必须同时证明：

- original production Task成功，wrapper也RanToCompletion；
- map action未因probe fault/cancel；
- terminal完整、用户无额外gameplay输入并正常退出；
- 没有baseline、recovery、A/B snapshot或结果不明；
- 只观察到正常fixture/checkpoint与退出边界，没有未知profile/inventory变化；
- 下一attempt使用新fixture与新namespace，不restore或复用旧checkpoint。

一旦baseline或recovery任一创建、A capture开始、出现保护差异/未知production outcome/证据缺失，repair envelope立即终止。不得修改业务假设、场景、保护集合、允许I/O、floor关系或PASS标准。

## Phase A：uninterrupted capture

用户可在arm前于profile1准备连续普通怪fixture；准备不属于正式窗口。Implementation Codex必须逐步说明允许完成哪些房间/奖励以及何时停在目标节点点击前。不得假定当前Continue位置。

arm成功后，用户只点击记录的普通怪节点并停止全部输入：不出牌、不End Turn、不打开选择、不暂停、不领奖、不进入下一节点。

production room-before save成功后，observer在放行continuation前验证并封存checkpoint/baseline。随后在同一target与phase-local identities稳定的30个相邻process frames后捕获A；required field任一false/null/exception或frame不连续均reset并记录breaker。

A成功后UI要求立即保持不操作并正常退出游戏，不使用Save & Quit。退出后只读证明`current_run.save`与sealed checkpoint raw bytes精确一致，并按`TEST_PROTOCOL.md`审计完整profile1 protection。失败为`BLOCKED_SOURCE_CHANGED_AFTER_A`，不得restore/rebaseline。

## Phase B：normal Continue capture

仅A与A-post通过后解锁。新进程中在点击前验证exact identities、loaded set `[SPIKE001A4]`、profile1、Cloud-off、checkpoint raw identity、protection、null run/combat state和上述B pre-room floor/coord/seed/character/act契约。

用户只点击主菜单正常Continue一次，随后不再输入。probe不得自行调用setup/load或模拟按钮。以与A相同的30-frame contract捕获B，并正常退出。

Continue允许的持久变化仅为`NumReloads +1`及Phase 0由固定IL预声明的非游戏语义字段；不得事后扩表。必须证明其他save canonical字段及profile1保护内容/length/inventory满足既定边界。

## 判定

- `PASS_RECONSTRUCTION_EQUIVALENT`：A/B semantic projection、card/enemy状态与完整可取得RNG digest相等；save/protection只含预声明允许差异。
- `FAIL_RECONSTRUCTION_DIFF`：协议有效但任一规定语义字段不同；记录首个及完整diff后停止。
- `BLOCKED_CHECKPOINT_*`、`BLOCKED_CAPTURE_DEADLINE`、`BLOCKED_SOURCE_CHANGED_AFTER_A`、`BLOCKED_ENVIRONMENT`：按精确failure site停止。
- `FAIL_ISOLATION_OR_PROTOCOL`：正式baseline后有非白名单持久变化、profile/path漂移或额外gameplay动作。
- `FAIL_HARNESS_AFTER_BASELINE`：baseline/recovery之后probe失败或证据不完整；不得写成production reconstruction FAIL。

只允许一个有效A/B compare。任一业务terminal后永久停止A4，不得restore、rebaseline或自行进入`SPIKE-001B`。PASS也只交回Project Lead复审。

## 交付与禁止事项

- 源码与build说明：`experiments/SPIKE-001A4/`
- 证据：`evidence/spike-001a4/`
- 报告：`project-management/investigation-reports/SPIKE-001A4-report.md`

不读取/切换其他profile，不测试其他房间、胜利、奖励、Boss或强制crash；不调查无直接因果的旁支日志。不得修改受保护项目管理状态文档。每次人工运行请求必须满足`TEST_PROTOCOL.md`八项格式。

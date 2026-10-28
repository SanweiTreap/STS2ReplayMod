# SPIKE-INV008A：controlled owner-fault 与 restart isolation matrix

历史状态：v1因旧007A仍enabled而在业务前`BLOCKED_IDENTITY`，随后因artifact锁定`BLOCKED_PHASE_ORDER`；无fault/restart结论，禁止重跑。当前唯一授权见`project-management/handoffs/SPIKE-INV008A-R2.md`，下文契约仅作为R2必须保持不变的业务基线。

任务类型：新的一次性运行探针；四个 fresh-process phase 依次解锁

## 背景与唯一目标

`SPIKE-INV007A-R3` 已通过固定 isolated Monster session 中的 direct progress update/save/write 隔离。`INV-008` 又用固定 v0.111.0 方法体把未验证的异常边界收窄为两个同步 owner fault 点，并证明 production `RunManager.CleanUp` 的 finally 只保证部分全局字段复位：任一主体 disposer 抛出会跳过后续 disposer，不能只凭 `State==null` 判 teardown 完整。

本任务只验证同一 transaction 机制在两处 **probe-owned、owner 捕获的同步异常** 后能否完成 ordered teardown，以及正常退出后的下一 fresh process 是否保持持久状态等价。它不是 crash test，不验证自然 victory、replay fidelity、action dispatch 或正式架构。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`，特别是分阶段实验、attempt identity、oracle isolation与跨进程restart比较
3. `project-management/DECISIONS.md` 中 D-041 至 D-045
4. `project-management/investigation-reports/INV-008-report.md`
5. `evidence/inv-008/README.md` 与 `static-lifecycle-query-output.txt`
6. `project-management/investigation-reports/SPIKE-INV007A-report.md`
7. `evidence/spike-inv007a/r3-terminal-validation-results.txt`
8. `experiments/SPIKE-INV003A/` 与 `experiments/SPIKE-INV007A/` 中 timing、candidate、audit、artifact、teardown 源码；只能作为已验证构件参考，不得修改或重跑旧 probe

## 固定身份与新身份

- 游戏：public-beta `v0.111.0`，commit/branch `41cef1ea`
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- R3 artifact：19,953 bytes；SHA-256 `AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553`
- INV-008 IL output：132,086 bytes；SHA-256 `453C31CC5C22E0FD0C54F3D0CFC191FBDB967B2DBFF508A06A050A2A635354B8`

创建全新 mod/probe id `SPIKEINV008A`、源码目录 `experiments/SPIKE-INV008A/`、evidence目录 `evidence/spike-inv008a/` 和新 schema。不得覆盖、修改、构建、安装或运行 003A/007A；不得复用其 artifact/recovery 目标。可以在新源码中最小复制已验证构件，但报告必须列出来源与改变。

身份不符即 `BLOCKED_IDENTITY`。安装只能在游戏进程不存在时新增 008A 自己的 DLL/manifest；不得覆盖其他模组文件。首次运行前必须让用户关闭所有 retired probe，包括 `SPIKEINV007A`，仅启用 008A 与明确必要依赖，并记录实际启用列表。

## Phase 0：离线实现与门禁

在请求用户启动游戏前必须完成：

- fixed hash gate、Release 0 warning/error、source/binary forbidden-surface audit、Harmony patch/unpatch smoke；
- transaction owner、single-CAS teardown owner、winner-ticket artifact 与 create-new/no-overwrite 语义；
- Ready 原子提交后解除 startup timeout，人工等待永不触发 missing-event terminal；
- 复用并测试 006A/007A 的 outer-cloud/graphics/startup gate、live-seeded independent candidate、profile1 12项 protection/recovery containment、identity-first store/boundary observer；
- 新增 INV-008 要求的 Launch/scene、writer-recording/direct replay write、controlled-fault token、owner catch/finally、cleanup entry/return/exception、关键 disposer/disconnect、task identity/status 与 menu ready/RefreshButtons观察；
- 用 synthetic tests 覆盖两个 exact fault点、错误 token/site、异常越过 owner、每个 teardown step fault、cleanup中段异常导致后续 disposer缺失、manager restore fallback、menu禁止条件、phase artifact已存在、phase顺序错误、restart只读、已知/未知mtime变化、post-seal callback和并发terminal。

observer 必须先按 sealed/enabled 和 manager/store identity 判断，再决定是否读取参数；越界调用不得记录path/payload/hash/次数。不得重新引入 SPIKE-INV005A 的越界审计方式。

## 四阶段状态机

阶段严格为：

1. `SETUP_OWNER_FAULT`
2. `RESTART_AFTER_SETUP`
3. `POSTLAUNCH_OWNER_FAULT`
4. `RESTART_AFTER_POSTLAUNCH`

每阶段都在新游戏进程中运行，拥有独立不可覆盖 artifact。两个 fault phase 还必须拥有独立 pre-action recovery 目录。下一阶段只能在上一 artifact 完整、identity正确且 terminal为 PASS 时解锁；任一 FAIL/BLOCKED/INVALID 后停止整个工作包，不自动进入后续 phase。

不得使用可被手工改写的裸 phase selector 绕过顺序。下一阶段由已封存 artifact chain与固定 identity决定。

## Phase A：SETUP_OWNER_FAULT

### 注入前必须成立

- 固定身份、profile1、Cloud关闭的用户确认、timing gate、prior integrity、12项 protection/recovery与live-seeded candidate全部通过；
- candidate override 已安装且 identity正确；original manager/task snapshot已固定；
- `SetUpReplay` **完整返回**；run state绑定正确；`ShouldSave=false`；combat-state synchronization disabled；`CombatReplayWriter.IsRecordingReplay=false`；
- Launch、NRun scene、GenerateMap、room load、process frame、dispatcher、action、victory、reward和所有progress update/save/write计数为0。

随后立即同步抛出一次带固定phase/attempt token的 `ControlledOwnerFaultException`。异常只能由transaction owner专用catch接收，然后进入唯一finally；不得越过TaskHelper/Godot边界。

禁止assets preload、Launch、scene创建、玩家输入、出牌、结束回合、奖励或replay event。用户只负责在主菜单进入008A页面并点击一次明确的 `Run Setup Fault`，之后不得操作UI，等待自动teardown和terminal。

## Phase B：RESTART_AFTER_SETUP

Phase A PASS并由用户从恢复后的主菜单**正常退出游戏**后，下一次启动自动执行只读验证：不要求点击按钮，不安装override，不构造candidate，不建立run/scene，不创建protected-file recovery。

必须核对 Phase A artifact/recovery identity、active profile1、production manager、state null、original/current task、Continue canonical/`NumReloads`、12项文件内容/长度和目录inventory。记录全部mtime差异。

restart PASS允许且仅允许以下三项出现hash/长度不变的mtime-only变化：

- machine `settings.save`
- profile1 `saves/prefs.save.backup`
- profile1 `saves/progress.save.backup`

这三项只能归类为“与已确认正常退出链相容”，不得称为零写入。其他mtime变化或任一内容/长度/inventory/Continue/`NumReloads`变化即FAIL。自动封存 `restart-after-setup.json` 后显示terminal，用户不得进入Continue或其他run，并正常退出。

## Phase C：POSTLAUNCH_OWNER_FAULT

Phase B PASS后的新进程重新建立独立baseline/recovery和candidate。先满足Phase A全部setup门禁，再：

- `RunManager.Launch()` 同步返回；
- 新 `NRun` 已由 `NSceneContainer.SetCurrentScene` 同步安装，CurrentScene/NRun/runState identity正确；
- writer仍未recording；
- `GenerateMap`、synchronizer fast-forward、`LoadIntoLatestMapCoord`、process-frame/quiet-frame等待、room/action/victory/reward/progress全部为0。

在这些条件后立即同步抛出唯一 `ControlledOwnerFaultException`。不得等待完整渲染或下一frame；灰色/不完整scene不是本任务验收项。用户只点击一次 `Run Post-launch Fault`，随后不操作UI并等待自动teardown。

## Phase D：RESTART_AFTER_POSTLAUNCH

Phase C PASS且用户正常退出后的下一进程自动执行与Phase B相同的只读验证，artifact固定为 `restart-after-postlaunch.json`。相同的三项mtime-only whitelist适用；不安装override、不建run。PASS后工作包结束，不自动进入crash测试或reconstruction。

## Ordered teardown 与独立失败分类

fault phase必须按 INV-008 的10步契约实现，并至少保持以下不可交换关系：

1. single-CAS取得唯一teardown ownership，阻止新输入/profile change/run/Quit，但observer保持至保护复核；
2. 验证无业务动作且writer未recording；
3. candidate `CurrentRunSaveTask` 第一次快照/有界等待；
4. state非空时调用一次且仅一次 `RunManager.CleanUp(true)`，记录entry、正常return/finalizer exception及关键reset/dispose/disconnect是否到达；禁止第二次cleanup修复；
5. candidate task第二次快照/等待；
6. 最外层non-skippable restore-finally先clear override并断言original；仅在clear未恢复时允许用启动前捕获的exact original恢复一次；
7. original task identity/status检查；
8. 只有cleanup与manager restore都成功才可 `LoadMainMenu(false)`，并等待explicit main-menu ready/RefreshButtons；
9. 比较fault phase自身before/after的12项hash/长度/**mtime**与inventory，必须全字段相等；
10. seal observer，以winner ticket写phase-specific create-new artifact。

预期 owner exception只表示注入成功，不足以PASS。cleanup、task、restore、menu、protection、artifact任一步异常/缺失/乱序必须使用独立 `FAIL_TEARDOWN_*`/`BLOCKED_ARTIFACT_WRITE` 等分类，并覆盖owner-fault成功。即使teardown失败，仍须尽最大努力完成non-skippable manager restore与只读protection/artifact；随后要求用户正常退出并停止，不启动下一phase。

## Boundary 与 PASS 条件

Fault phase PASS必须同时满足：

- exact fault token/type/site/count为1；owner catch与finally各恰好一次，异常未越界；
- 所有禁止业务动作、progress methods、writer recording/direct replay write均为0；
- original/unknown manager/store与列出的external submit边界为0；
- candidate/original task满足契约；cleanup及全部必需disposer/disconnect到达；
- original manager恢复早于main menu构造；state null；Continue visible/enabled且canonical/`NumReloads`等于baseline；
- fault phase 12项保护before/after含mtime完全一致，inventory一致；artifact/recovery完整且不可覆盖。

Restart phase PASS只证明紧邻fault phase完成teardown并正常退出后的持久状态一致；采用上述mtime规则。它不证明强杀、原生crash、断电或未执行finally时的恢复。

任何可观察 original/unknown boundary、外部submit、direct replay write、保护内容变化、Continue变化、teardown残留或阶段外用户动作立即FAIL。不能把observer未覆盖的Steam/native/Godot队列写成“所有side channel已证明安全”。

## 人工协作与停止规则

- Implementation Codex不得启动或控制游戏，不得使用Computer Use或其他GUI自动化。
- 安装和每一phase开始前，按 `TEST_PROTOCOL.md` 给用户一份只覆盖当前phase的具体动作、预期文字、禁止动作、terminal后动作与回传证据。
- Fault phase达到terminal并回到主菜单后，用户立即停手并正常退出；不得点击Continue、进入设置、切profile、领取奖励或重跑。
- Restart phase由probe在主菜单ready后自动只读执行；用户不点击任何游戏菜单，看到terminal后正常退出。
- 任一phase首个terminal后不得覆盖、重跑或现场修补。FAIL/BLOCKED时不继续后续phase，不恢复backup、不rebaseline、不杀进程；把证据交回Project Lead。

## 明确不做

- 不强杀游戏进程、不注入native crash、不测试断电/crash recovery。
- 不dispatch replay events、不自然胜利、不调用achievement helper或direct progress sentinel。
- 不进入map/room，不出牌，不结束回合，不领取奖励。
- 不调查灰色UI、`Invalid Task ID`、replay fidelity、reconstruction、capture/dispatch、性能、seek或正式产品UI。
- 不读取或切换其他profile，不使用RitsuLib，不采用旧probe作为运行依赖。

## 交付物

1. `experiments/SPIKE-INV008A/` 源码、测试与构建/安装说明
2. `evidence/spike-inv008a/README.md`、preflight/static audit/build/install证据
3. 每个已解锁phase唯一不可覆盖artifact；两个fault phase各自独立recovery目录
4. `project-management/investigation-reports/SPIKE-INV008A-report.md`

报告必须逐phase区分Confirmed/Inferred/Unknown，列出用户观察、机器artifact、固定identity、fault token、teardown步骤、boundary、protection和restart mtime解释。Implementation Codex不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md`、`ROADMAP.md` 或既有已验收报告。

## 工作包终态

- 四phase全部PASS：`PASS_CONTROLLED_FAULT_RESTART_ISOLATION`。
- 任一业务/teardown/protection失败：保留首个精确 `FAIL_*`，停止。
- 任一身份、prior-integrity、artifact/recovery、安全门禁不能满足：`BLOCKED_*`，停止。

即使四phase PASS，结论也只覆盖可控 owner exception + ordered teardown + 正常退出后的restart integrity。Project Lead仍须单独复审是否足以把transaction候选判为当前session-isolation阶段通过；不得自动解锁crash test、SPIKE-001A2或SPIKE-001B。

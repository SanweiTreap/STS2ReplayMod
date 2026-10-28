# SPIKE-INV008A-R2：安装生命周期修复后的 controlled fault/restart matrix

任务类型：同一008A源码的窄harness revision；保留v1全部证据，重新执行原四阶段业务契约

## Project Lead 处置

008A v1没有执行任何fault/restart业务phase。首次启动因`SPIKEINV007A`与`SPIKEINV008A`同时enabled，在profile/protection/candidate/override之前封存`BLOCKED_IDENTITY`；第二次启动虽只有008A loaded，但v1 phase artifact已存在，因此只封存`BLOCKED_PHASE_ORDER`。两份artifact都没有protection baseline、candidate、recovery、fault、audit或teardown。

这不是transaction、fault point或teardown的有效FAIL。Project Lead按`TEST_PROTOCOL.md`的evidence-preserving revision规则，批准保留相同`SPIKEINV008A` mod id/source tree，提升revision/schema并使用全新的R2 artifact/recovery chain。v1 artifact与所有旧证据永久保留。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`，特别是“Probe安装与启用状态生命周期”
3. `project-management/DECISIONS.md` 中 D-045、D-046
4. `project-management/handoffs/SPIKE-INV008A.md`
5. `project-management/investigation-reports/SPIKE-INV008A-report.md`
6. `evidence/spike-inv008a/README.md`
7. 两份固定v1 artifact

## 必须保留的 v1 身份

- `setup-owner-fault.json`：2,359 bytes；SHA-256 `56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48`
- `invalid-phase.json`：1,479 bytes；SHA-256 `A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F`
- v1 installed/source DLL：215,552 bytes；SHA-256 `4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717`
- v1 manifest：372 bytes；SHA-256 `FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4`
- R3 prior artifact、recovery及003A/007A源码/证据身份继续沿用原handoff固定值

v1 artifact、phase0 evidence、003A/007A全部源码/证据不得删除、覆盖、移动、改名或重写。当前没有008A recovery目录；不得伪造或补建v1 recovery。

## 已确认的根因与当前外部状态

- 首次日志`godot2026-09-15T13.47.40.log`确认加载两个mods：`SPIKEINV007A`与`SPIKEINV008A`。
- 后续日志确认`SPIKEINV007A`已持久化为disabled，只有`SPIKEINV008A` loaded；当前游戏进程已停止。
- 这只证明最近一次进程的loaded集合，不替代R2安装前和首次运行时的再次门禁。

## 仅允许的 R2 修改

1. probe version提升为明确R2；schema提升为`spike-inv008a.owner-fault.v2`。manifest的`version`必须改为有效SemVer `0.2.0`，不得继续使用游戏日志已警告为invalid的suffix版本；artifact内部可保留更具体的probe revision字符串。
2. 四个业务artifact改为不可覆盖的R2专属名称：
   - `setup-owner-fault-r2.json`
   - `restart-after-setup-r2.json`
   - `postlaunch-owner-fault-r2.json`
   - `restart-after-postlaunch-r2.json`
3. fault recovery使用新的R2专属目录；v1路径不得成为R2目标。
4. phase policy忽略v1业务结果但必须先验证两份v1 artifact identity、内容表明pre-business字段均为null、且不存在v1 recovery。任一不符`BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`。
5. 新增独立configuration precheck层：wrong loaded-mod set不得占用R2业务artifact，只能写唯一attempt-specific、create-new的`precheck-modset-r2-<attempt>.json`，其中只记录bounded ID集合/数量和失败枚举，不读取profile/path/payload。它不得建立protection、candidate、override或run。
6. handoff预授权：若且仅若precheck因loaded-mod set失败、所有business字段均未建立，用户可正常退出、修正enabled状态并重新启动precheck；旧precheck evidence必须保留。任何其他BLOCKED/FAIL没有该权限。
7. R2首次业务phase前增加prior-integrity：当前profile1受保护内容/长度/inventory、Continue canonical/`NumReloads`必须等于固定R3 protection-after；仅允许测试协议列出的三项content-identical mtime变化。任一内容或其他mtime差异阻塞，不恢复/rebaseline。
8. 安装脚本只可在游戏进程不存在、installed 008A pair精确匹配上述v1 hash、最新验证日志确认007A disabled且只加载008A时，用R2产物替换**008A自己的两个安装文件**。不得修改007A文件或enabled配置。

除此之外不得改变四phase业务契约、fault点、teardown顺序、observer、protection、mtime规则、PASS标准或用户动作。不得通过降低“唯一启用probe”要求来修复。

## 必须新增的 tests/audit

- v1两份artifact identity和pre-business-null contract；
- manifest version为有效SemVer，运行日志不再出现008A invalid-version warning；
- v1 artifact存在时R2选择`SETUP_OWNER_FAULT`，不产生phase-order failure；
- wrong mod set产生独立precheck artifact且R2业务artifact不存在；修正后新precheck可进入Ready；
- precheck artifact逐attempt不可覆盖，不能被误认作phase PASS；
- loaded set必须精确为`SPIKEINV008A`，不得接受007A或其他项目probe；
- R2四artifact/recovery完全独立且create-new；
- R3→current prior-integrity的已知三项mtime whitelist与其他差异负例；
- 保留原34项tests、112-method Harmony smoke、source/binary forbidden-surface audit、Release 0 warning/error。

## 安装生命周期门禁

R2 build完成后、替换live pair前必须按顺序：

1. 确认游戏进程不存在。
2. 精确验证v1 installed pair与workspace artifacts hash一致。
3. 只读验证最近一次完整启动日志：007A是`Skipping ... disabled`，loaded列表恰好只有008A；如果证据缺失，先请求用户在**不安装R2**的情况下启动v1一次，只观察主菜单后正常退出，不点击probe，再复核日志。
4. 只有上一步通过才替换008A自己的DLL/manifest；旧v1 workspace artifact和evidence保持不变。
5. 安装后不自行启动游戏。向用户请求R2 Phase A时再次要求确认Steam Cloud关闭、profile1不变，并明确007A保持disabled。

不得删除007A live files；当前只需保持disabled。工作包最终完成后，008A live pair默认保留但要求用户disabled；任何删除由以后Project Lead另行授权。

## R2 运行矩阵

原`SPIKE-INV008A.md`的四phase、注入点、teardown、boundary和人工规则全部保留，仅将artifact/recovery身份替换为R2名称：

1. `SETUP_OWNER_FAULT_R2`
2. `RESTART_AFTER_SETUP_R2`
3. `POSTLAUNCH_OWNER_FAULT_R2`
4. `RESTART_AFTER_POSTLAUNCH_R2`

每次只请求一个已解锁phase，均为fresh process。fault phase用户只点击一次对应Run按钮，之后不操作；restart phase在main-menu-ready后自动只读执行。每个terminal后用户正常退出。任一业务phase FAIL/BLOCKED立即停止；只有configuration precheck wrong-mod-set享有上述预授权修正。

Ready后不得保留人工响应timeout。不进入map/room/frame/action/reward/progress，不强杀进程，不调查UI渲染。

## 交付物

1. 更新`project-management/investigation-reports/SPIKE-INV008A-report.md`，保留v1历史并新增R2实现/运行结果
2. 更新`evidence/spike-inv008a/README.md`
3. R2 preflight/tests/build/static audit/install/enablement证据
4. 所有configuration precheck evidence与已解锁R2 phase artifact/recovery

Implementation Codex不得修改`CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md`、`ROADMAP.md`、`TEST_PROTOCOL.md`或其他既有报告。

## 验收与停止

四个R2业务phase全部PASS才得到`PASS_CONTROLLED_FAULT_RESTART_ISOLATION`。即使PASS，也不覆盖强杀/native crash、replay fidelity或完整产品session isolation。

本R2达到任一非precheck业务terminal后不得重跑同一phase。若仍发生pre-business harness缺陷，保留证据并交回Project Lead，不得自行产生R3。

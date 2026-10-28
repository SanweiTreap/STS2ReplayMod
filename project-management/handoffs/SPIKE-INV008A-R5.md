# SPIKE-INV008A-R5 — phase-aware owner-fault / restart matrix

## 角色与目标

你是本实验 lineage 的 Implementation Codex。继续使用现有 `experiments/SPIKE-INV008A/`，将 INV-009 已验收的 phase-aware teardown oracle 应用于全新 R5 evidence chain，并按四个 fresh-process phase 顺序完成：

1. `SETUP_OWNER_FAULT_R5`
2. `RESTART_AFTER_SETUP_R5`
3. `POST_LAUNCH_OWNER_FAULT_R5`
4. `RESTART_AFTER_POST_LAUNCH_R5`

本任务只验证 controlled owner exception 下的 transaction teardown/restart isolation，不实现 Replay Mode 产品功能。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/DECISIONS.md` 中 D-045、D-048、D-049、D-050
4. `project-management/investigation-reports/INV-008-report.md`
5. `project-management/investigation-reports/INV-009-report.md`
6. `project-management/investigation-reports/SPIKE-INV008A-report.md`
7. `evidence/inv-009/README.md`
8. `evidence/spike-inv008a/setup-owner-fault-r4.json`
9. 现有 008A source/tests/audit/install scripts

## 固定身份

- 游戏：public-beta `v0.111.0`，commit/branch `41cef1ea`
- `sts2.dll`：9,757,184 bytes，SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- R4 artifact：23,241 bytes，SHA-256 `40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040`
- INV-009 IL output：253,870 bytes，SHA-256 `488814C65B9CD8E56F888CFCFE50AD7BEAA890E0396F084BED3916CFBDF772D7`
- INV-009 bounded summary：2,063 bytes，SHA-256 `2B50415302BBE5D38874C6AA5CA29EAF2012FA7D4DE923AA842A8AD26762D9D0`

任一固定身份不符，停止为 `BLOCKED_IDENTITY`。不得寻找替代输入。

## 允许修改范围

允许在现有 008A 工程中修改完成 R5 所必需的 probe source、tests、audit和精确install tooling；允许测试、构建，并在游戏停止且installed 008A pair精确匹配已知R4 identity后，只替换008A自己的DLL/manifest。

仅允许下列语义变化：

- 新 R5 schema/revision/version、四份R5 business artifact、R5 invalid/precheck artifact及独立R5 recovery路径；
- RunLobby identity/type observation与phase-aware cleanup oracle；
- post-launch NRun/current-scene ownership与replacement gate；
- 与上述变化直接对应的synthetic/integration tests、UI phase labels和artifact字段；
- 安装/前驱identity链中加入INV-009与R4固定证据。

不得改变fault注入点、transaction owner、timing gate、SaveManager candidate/override、`ShouldSave=false`、sync-disable、writer-not-recording、allowed I/O、12项protection、Continue、boundary、task wait、manager restore或owner-fault异常类型/次数。

## R5 teardown contract

### RunLobby gate

- 在pre-setup、`SetUpReplay`返回后、Launch返回后（post-launch phase）、NRun安装后（post-launch phase）及cleanup entry记录：NetService type、RunLobby null/identity。
- 本实验固定为fresh-process Replay：所有适用采样点都必须是`Replay`且RunLobby为null。
- 任一pre-setup非null：`BLOCKED_PRECONDITION_STALE_RUNLOBBY`，必须发生在candidate/override/run/recovery/fault之前。
- setup后的任一非null或null/identity变化：独立boundary failure；不得靠后续cleanup洗掉。

### Cleanup calls

- 原13项replay core tracked calls继续全部required且各一次。
- `RunLobby.remove_RemotePlayerDisconnected`与`RunLobby.Dispose`组成不可拆分条件组：
  - pre-cleanup lobby为null：两项都必须为0，标Not applicable；
  - pre-cleanup lobby非null：两项都必须各1次，否则`FAIL_TEARDOWN_CLEANUP`。
- 不得实现为永久忽略两个lobby调用，也不得仅以State null/manager restored替代call obligations。
- cleanup entry/normal return/exception、finally flags、no replay write、task waits、manager restore、Continue/protection/boundary与teardown-failure优先级全部保留。

### Scene ownership

- setup fault：确认fault前没有owned NRun；menu restore仍必须正常完成并通过原menu/Continue gate。
- post-launch fault：fault前current scene必须是本phase刚创建/安装的NRun；teardown后current scene必须是新main menu，且旧owned NRun不再是current scene。
- scene replacement失败必须独立分类，即使core calls与protection通过也不能PASS。

## Phase 0 必须通过

1. 固定身份与全部旧artifact/recovery/evidence不可变检查。
2. R5 schema/artifact/recovery全部create-new；不得覆盖v1/R2/R3/R4。
3. Release build 0 warning/0 error；Harmony patch/unpatch smoke通过。
4. source/IL forbidden-surface audit通过。
5. 全部旧tests保留；新增至少覆盖：
   - 真实Replay null-lobby 13-call positive path；
   - nonnull lobby时两项各一次positive path；
   - nonnull时缺一、重复、抛出；
   - null时意外出现任一lobby call；
   - pre-setup stale lobby；setup/launch/scene各边界null↔nonnull或identity replacement；
   - 13 core任一缺失及cleanup exception仍失败；
   - post-launch owned NRun未被main menu替换；
   - R4真实artifact在revised call-set evaluator中只得到`CORE_CALL_SET_PASS`，不能伪造menu/restart/full-phase PASS。
6. 游戏停止；只有008A持久化enabled，旧probe disabled；安装前验证，安装后复核。

Phase 0 完成后停止并向用户提交首阶段人工测试请求。Codex不得启动或控制游戏，不使用Computer Use。

## 四阶段执行与停止规则

- 每个phase使用独立fresh process和create-new artifact；上一phase完整PASS才解锁下一phase。
- 用户手动启动/操作/退出游戏。每次请求必须写清具体动作、目标、预期页面/终态、达到目标后立即停止、不得领取任何奖励、应回传的artifact/日志摘要。
- owner-fault phase中任一业务FAIL/BLOCKED后立即停止整个R5，不恢复backup、不rebaseline、不继续restart或后续phase。
- restart phase仍使用D-045既定三项mtime-only whitelist；任何内容、inventory、Continue/NumReloads或非白名单mtime变化均失败。
- 四phase全PASS后停止并交回Project Lead；不得自行解锁crash、SPIKE-001A2或SPIKE-001B。

## 有界 harness repair authority

在首次R5 business artifact建立前，Implementation Codex可自行修复最多2次明确的pre-business harness缺陷，不必为每次小改返回Project Lead，前提是：

- 机器证明candidate/override/run/recovery/fault、profile/protection business read均未发生；
- 旧证据不可变；
- 只改本handoff允许范围，不放宽PASS或安全边界；
- 每次提升内部build revision并使用attempt-specific precheck evidence；
- 修复、tests、build、精确install和用户下一次手动启动请求全部留痕。

一旦建立protection/recovery/candidate/override/run，或产生任何business artifact，此授权立即终止；任何缺陷都必须停止并交回Project Lead。该授权不允许Codex操作游戏。

## 交付物

- 更新 `project-management/investigation-reports/SPIKE-INV008A-report.md`
- 更新 `evidence/spike-inv008a/README.md`
- R5 source/tests/audit/install证据
- 每阶段create-new artifact、必要的bounded validation与recovery identity记录
- 最终回报：状态、完成phase、首个失败点或四phase PASS、保护/Continue/manager/scene证据、最大Unknown、唯一下一建议

不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md` 或本handoff。

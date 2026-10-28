# SPIKE-INV008A-R6 — restored-menu callback ordering repair

## 目标

继续使用 `experiments/SPIKE-INV008A/`，只修复 R5 已确认的 restored-menu observer 顺序缺陷与独立signal报告，然后以全新R6证据链依次执行：

1. `SETUP_OWNER_FAULT_R6`
2. `RESTART_AFTER_SETUP_R6`
3. `POST_LAUNCH_OWNER_FAULT_R6`
4. `RESTART_AFTER_POST_LAUNCH_R6`

R5不得修改或重跑。本任务不实现产品功能，不调查无时间因果的`Invalid Task ID`。

## 必读

- `project-management/CURRENT_STATUS.md`
- `project-management/TEST_PROTOCOL.md`
- `project-management/DECISIONS.md` D-045、D-050、D-051
- `project-management/handoffs/SPIKE-INV008A-R5.md`
- `project-management/investigation-reports/SPIKE-INV008A-report.md`
- `evidence/spike-inv008a/project-lead-r5-menu-observer-review.md`
- `evidence/spike-inv008a/setup-owner-fault-r5.json`

固定R5 artifact：25,790 bytes，SHA-256 `9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9`。游戏DLL身份沿用R5：9,757,184 bytes，SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。不符即`BLOCKED_IDENTITY`。

## 唯一允许的语义修订

1. R6 schema/revision/version、四份R6 business artifact、独立R6 recovery/precheck/invalid路径及前驱identity链。
2. 在`NMainMenu._Ready` prefix中，仅当transaction active时登记restored-menu candidate identity；不得在prefix宣称ready。
3. `RefreshButtons` postfix仅接受同一candidate identity，并完成refresh signal。
4. `_Ready` postfix仍是menu-ready完成点，同时确认与candidate identity相同；错误/重复/替换identity必须失败。
5. bounded wait结束或抛出后，分别读取并落盘menu-ready与refresh task的真实状态；一个缺失不能把另一个已完成状态写成false。failure site必须精确区分`MenuReady`、`RefreshButtons`、identity/order或真正的`MainMenu` load exception。

禁止增加`TaskWaitSeconds`、删除任一signal、改为轮询/固定delay、接受错误menu identity，或用current-scene/Continue替代callback gate。R5的fault点、lobby oracle、13项core cleanup、scene ownership、tasks、manager、boundary、protection、Continue、mtime与停止规则全部冻结。

## Phase 0

- 保留全部旧artifacts/recovery/evidence，R6全部create-new。
- Release 0 warning/0 error，Harmony patch/unpatch和forbidden-surface audit通过。
- 保留旧tests，并新增production真实顺序测试：`_Ready prefix → RefreshButtons body/postfix → _Ready postfix → wait`必须PASS。
- 必须有以下negative tests：缺prefix、错误candidate、Refresh来自旧menu、仅menu完成、仅refresh完成、两者均缺失、重复callback、postfix identity替换、真正LoadMainMenu exception；分别验证落盘状态与failure site。
- 用固定R5 artifact证明只能重解释observer缺陷，不能伪造R5 full-phase/restart PASS。
- 游戏停止、007A disabled、实际只允许008A；仅在installed R5 pair身份精确时替换008A自己的DLL/manifest。

Phase 0完成后停止，向用户提交`SETUP_OWNER_FAULT_R6`人工测试请求。Codex不得启动或控制游戏，不使用Computer Use。

## 执行和授权

- 四阶段均为fresh process，上一阶段完整PASS才解锁下一阶段。
- 用户手动操作；每次请求写明动作、目标、预期、目标后立即停止、不领取奖励、回传证据。
- 首个business FAIL/BLOCKED后停止，不恢复backup、不rebaseline、不继续下一phase。
- 在首次R6 business artifact前，可在上述唯一允许范围内自主修复最多2次pre-business harness缺陷，并提升build revision、保留attempt evidence。任何protection/recovery/candidate/override/run/fault或business artifact建立后，该授权立即失效。
- 四阶段全PASS后交回Project Lead；不得自行解锁crash、SPIKE-001A2或SPIKE-001B。

## 交付

- 更新`project-management/investigation-reports/SPIKE-INV008A-report.md`
- 更新`evidence/spike-inv008a/README.md`
- R6 Phase 0、安装、每阶段artifact/validation/recovery identity证据
- 最终说明完成phase、首个失败或四阶段PASS、callback顺序与两个signal状态、manager/scene/protection/Continue结果、最大Unknown和唯一下一建议

不得修改`CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md`或本handoff。

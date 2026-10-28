# SPIKE-INV007A-R3：detached mutation oracle 修复后的受控尝试

任务类型：对现有 007A 的第二次窄 harness 修订；经 Project Lead 授权一次新的人工运行

## 当前结论与唯一目标

R2 是有效落盘的 terminal，但 `FAIL_MUTATION` 由 oracle 与 candidate 共享 nested stats 对象造成。R2 已局部证明 direct `UpdateProgressAfterCombatWon` 在 candidate manager 上恰好执行一次且无 boundary/write；它没有证明 production mutation 正确或错误，也没有执行 `SaveProgressFile`。

R3 只修复 expected-state 的深度隔离并再次执行原有业务契约：用真正 detached 的 expected progress 计算一次 mutation oracle，证明 oracle 计算没有预先改变 candidate，再调用一次 update 与一次 save。不得改变 scene、参数、candidate、boundary、teardown、保护范围或 PASS 标准。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md` 中 attempt revision 与“实验 oracle 隔离”
3. `project-management/DECISIONS.md` 中 D-041 至 D-043
4. `project-management/handoffs/SPIKE-INV007A.md`
5. `project-management/handoffs/SPIKE-INV007A-R2.md`
6. `project-management/investigation-reports/SPIKE-INV007A-report.md`
7. `evidence/spike-inv007a/runtime-artifact-r2.json`
8. `experiments/SPIKE-INV007A/LiveSeedCandidate.cs`
9. `experiments/SPIKE-INV007A/DirectWriteTransaction.cs`
10. `evidence/inv-007/il-query-output.txt` 与固定 DLL 中 `ProgressState.ToSerializable()` 的窄 IL

## 必须保留的 R2 证据与身份

- R2 artifact：19,259 bytes；SHA-256 `4B48A75F650A687F206255F2425140B3F640277E3D62F21495F99CC6086B691F`
- R2 schema/version：`spike-inv007a.direct-write.v2` / `0.2.0-direct-write-r2`
- R2 recovery：`evidence/spike-inv007a/restricted-recovery/r2-20260914T091555.1356848Z/`，12 files
- R2 installed DLL：174,592 bytes；SHA-256 `5447E69F11041639ED9DD92E152D1B78B7A9A6A6025906C7BDD9900929021ACD`
- R2 manifest：370 bytes；SHA-256 `AA910BF2376FE5642372FF8C64E8F3138708F843BA2335112611F3E954E32869`

R1 artifact、R1/lost-attempt recovery、R2 artifact 和 R2 recovery 均不可删除、覆盖、移动、改名或重写。任一 identity 不符即 `BLOCKED_IDENTITY`。

## 已确认的 R2 根因

R2 使用 `CaptureProgress(candidate)` 后直接 `ApplyExpected(expected, ...)`。`CaptureProgress` 调用 `candidate.Progress.ToSerializable()`。固定 v0.111.0 IL 显示 `ToSerializable()` 对 EncounterStats/EnemyStats 只对 dictionary values 执行 `Concat(...).ToList()`；列表容器是新的，但内部 `EncounterStats`、`EnemyStats` 及其 `FightStats` 对象仍与 candidate 共享。`ApplyExpected` 对 existing stats 增加 wins，因此在 production update 前已改变 candidate。

该根因足以直接修订，不需要新静态调查，也不得把灰色背景、未完成渲染或 `Invalid Task ID` 纳入本工作包。

## 仅允许的 R3 修改

继续使用同一 `SPIKEINV007A` mod id 和源码目录。必须：

1. probe version 提升为明确 R3 revision；schema 提升为 `spike-inv007a.direct-write.v3`；artifact 固定为不可覆盖的 `runtime-artifact-r3.json`；recovery 使用新 R3 attempt 目录。
2. 先捕获 candidate baseline canonical JSON/hash；通过 `JsonSerializationUtility.FromJson<SerializableProgress>` 或等价 JSON round-trip 从该 JSON 构造 detached expected；parse 失败使用独立 oracle-precondition failure。
3. `ApplyExpected` 只能作用于 detached expected 和独立的 discovered set。
4. 在 production update 前再次 canonicalize candidate，并硬性证明它与 oracle 计算前 baseline 完全相等；player `DiscoveredEnemies` 也必须不变。任何变化分类为 `BLOCKED_ORACLE_ALIASING`，不得调用 production update。
5. 通过递归 mutable-reference 检查或覆盖全部可变 stats/FightStats 图的等价检查，证明 expected 与 candidate progress graph 共享可变引用为 0。artifact 只记录 bounded count/boolean，不记录 payload。
6. 只有 oracle non-interference gate 通过，才沿用原路径调用一次 update、验证 mutation，再调用一次 save、验证唯一 write。
7. prior-attempt integrity gate 改为固定 R2 artifact/recovery identity，并在 Ready 前把当前 allowlisted 内容/inventory 与 R2 protection-after 及 recovery 逐项核对。差异即 `BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`，禁止恢复或 rebaseline。
8. 安装脚本仅可在游戏进程不存在且 installed pair 精确匹配上述 R2 hash 时替换 007A 自己的 DLL/manifest。

除这些点外不得修改业务逻辑。尤其不得通过把 expected 改成 actual、放宽 canonical equality、忽略 wins 字段、调用 production 两次、事后减一或删除 mutation gate 来制造 PASS。

## 必须新增的测试与审计

- 用已有 EncounterStats + EnemyStats + FightStats（wins 至少为 1）的 candidate fixture 证明：旧 `ToSerializable` 浅结构会共享引用并污染 candidate；R3 JSON round-trip 不会。
- 对 detached expected 执行 `ApplyExpected` 后，candidate canonical 与 player discovered baseline 完全不变。
- expected/candidate recursive mutable shared-reference count 为 0。
- expected mutation 对 encounter 和每个 enemy 各恰好 +1；重复 enemy ID 必须按 production 实际枚举次数处理，不可擅自 distinct。
- oracle parse failure、alias/non-interference failure 在 update audit count仍为 0 时 fail closed。
- 保留原 27 项 R2 tests，以及 update 后零 write、swallowed save rejection、exact one-write acceptance 和 duplicate-write rejection。
- source/binary audit 禁止 production 前从 `CaptureProgress(candidate)` 得到可变 expected 后直接 `ApplyExpected`；必须证明 JSON round-trip 与 pre-call baseline recheck 存在。
- 保留 timeout race、attempt-specific artifact、identity-first observer、schema whitelist、Release 0 warning/error 和 70-method Harmony smoke。

## R3 人工运行与停止

Phase 0、安装 identity 和 prior-attempt integrity 全部通过后，由 Implementation Codex 提交精确人工请求。项目发起人只点击一次 Run，之后不出牌、不结束回合、不领取奖励、不操作 UI，等待自动 teardown。不得使用 Computer Use；Implementation Codex 不得启动游戏。

Ready 后不设置人工 action timeout。首个 R3 terminal 后立即停止且不再重试。只读取 R3 artifact、R3 allowlisted protection/recovery 与固定旧证据。

## 验收边界

R3 沿用原业务分类，并新增/保留 `BLOCKED_ORACLE_PARSE`、`BLOCKED_ORACLE_ALIASING`、`BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`。

`PASS_DIRECT_WRITE_ISOLATED` 必须同时证明：oracle 计算前后 candidate 未变且无共享可变引用；update 恰好一次；实际 canonical 等于 detached expected；save 前 write 为 0；save 恰好一次；candidate memory store 恰好一次同步 exact progress write；payload canonical 相等；original/unknown/external boundary 为 0；teardown 和保护集合通过。

即使 PASS，也只覆盖固定 isolated Monster session 中的两个 direct methods，不代表自然 victory、achievement、fault/restart、完整 session isolation 或 replay fidelity。

## 交付物

1. 更新 `project-management/investigation-reports/SPIKE-INV007A-report.md`，保留所有旧 attempt 历史并新增 R3
2. 更新 `evidence/spike-inv007a/README.md` 的 attempt 索引
3. R3 test/build/static-audit/install/prior-integrity 结果
4. R3 独立 recovery 目录
5. 若运行，唯一 `runtime-artifact-r3.json`

Implementation Codex 不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md` 或 Roadmap。

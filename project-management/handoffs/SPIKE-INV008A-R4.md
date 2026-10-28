# SPIKE-INV008A-R4：可满足的 phase-local baseline 与 controlled fault/restart matrix

任务类型：同一008A实验lineage的新revision；修复R3不可满足的prior gate，不改变owner-fault业务契约

## Project Lead结论

R3 artifact有效，但prior-integrity gate不可满足：它把recovery副本mtime当作源mtime；它要求固定007A-R3 artifact中不存在的Continue结构；它还要求在项目已按协议改变mod enabled状态后，当前`settings.save`仍与历史字节完全相等。R3没有建立protection/candidate/override/run/recovery/fault或执行任何业务动作，因此没有owner-fault/restart结论。

R3必须永久封存。R4不是R3重跑，而是以新schema/artifact/recovery建立一个可满足的baseline contract。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`，特别是“基线来源与可满足性”
3. `project-management/DECISIONS.md` 中 D-045 至 D-048
4. `project-management/handoffs/SPIKE-INV008A.md` 与 R2/R3 handoff
5. `project-management/investigation-reports/SPIKE-INV008A-report.md`
6. `evidence/spike-inv008a/project-lead-r3-prior-integrity-review.md`
7. v1、R2、R3全部固定artifact与validation

## 固定R3证据

- `setup-owner-fault-r3.json`：3,740 bytes；SHA-256 `5C8E49B98F7BC9A647033F779DC3CACCEBE3D840214E4D95995EBADB7C0B13F0`。
- 必须验证terminal为`BLOCKED_PRIOR_INTEGRITY`且protection/candidate/recovery/fault/audit/teardown/restart全部为null。
- R3 business artifact以外的R3后续artifact与R3 recovery必须不存在。
- R3 installed pair开始身份：DLL 235,008 bytes / SHA-256 `2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01`；manifest 353 bytes / SHA-256 `1B0ECD1E95639DA0B95EB84576C17D4D0CAB67D70D2E15496702533A028D22A3`。

## R4 baseline契约

### 历史证据门禁

- 继续验证v1、R2、R3 artifact/recovery的固定identity与pre-business字段，不把其中任何失败terminal解释为业务PASS。
- 初始profile1 inventory与十个active-profile文件的长度/hash必须与007A-R3固定protection-after一致；AccountSelector内容也必须一致。忽略这些历史文件的mtime，因为这一步只证明内容链。
- `settings.save`不得与007A-R3历史字节比较。R4进入前只固定Project Lead post-R3-exit identity：5,238 bytes / SHA-256 `2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F`。若不同则停止并报告，不自行读取差异、恢复或重新固定。

### phase-local基线

- 第一fault phase在GateReady前只要求当前Continue可见、enabled并成功捕获完整`ContinueEvidence`；不与007A-R3的Continue比较。
- 用户点击Run后、任何candidate/override/setup之前，立即捕获12项当前source identity与profile inventory并创建R4 recovery。该fresh snapshot是本phase唯一before基线。
- fault teardown后必须把当前12项与fresh source snapshot做严格content/length/mtime/inventory比较，并把fresh Continue与teardown Continue逐项比较。
- recovery copy只用于hash/length恢复证据，不用于mtime比较。artifact必须保留fresh source snapshot自身记录的mtime。
- 紧邻restart phase从前一R4 fault artifact读取`protectionAfter`/source mtime与`teardown.continueAfter`；只允许`TEST_PROTOCOL.md`三项content-identical mtime变化。

## 必须修复的Continue与UI证据

- R4 artifact必须记录GateReady时的`continueBefore`，至少包含Visible、Enabled、NumReloads、CanonicalSha256；不能只保存一个boolean。
- 如果Continue捕获失败，分类必须区分not visible、not enabled、missing save data、NumReloads mismatch与canonical mismatch；不得只输出`continueEqual=false`。
- 页面在terminal之后首次打开时必须读取sealed session并显示真实terminal/failure site；不得显示`WAITING_EVENTS`。补充“terminal before page creation”的回归测试。

## Identity与运行矩阵

- manifest合法SemVer `0.4.0`；schema `spike-inv008a.owner-fault.v4`，内部revision含R4 build identity。
- 使用R4专属四business artifact、invalid artifact、precheck与recovery名称，全部create-new；R3路径不得成为写入目标。
- loaded-set检查继续位于已验证的post-loader/pre-profile边界，后续gate revalidation保持exactly `[SPIKEINV008A]`。
- setup fault、restart-after-setup、post-launch fault、restart-after-postlaunch四阶段的fault点、owner catch/finally、teardown、boundary、禁止行为、人工步骤与PASS标准继承原始008A契约。

## Tests与静态门禁

- 用实际固定007A-R3 artifact运行schema compatibility test，确认缺失Continue不会被当成可比较值；R4第一phase使用fresh Continue。
- 用实际007A-R3 recovery验证copy mtime与artifact source mtime不同，断言mktime oracle只能来自artifact source snapshot。
- 正向集成测试必须使用固定v1/R2/R3 artifact schema与上述Project Lead content identities，证明R4完整prior gate可达GateReady。
- settings历史差异、active-profile内容差异、inventory差异、fresh Continue捕获失败、restart非白名单mtime分别有独立负例。
- 保留原43项适用测试，并重新执行Release 0 warning/error、source/IL audit、112-method Harmony patch/unpatch smoke。

## 修改与运行授权

Implementation Codex可在同一个原Implementation task中修改`experiments/SPIKE-INV008A/`、测试、构建并在游戏停止且installed R3 pair精确匹配时只替换008A自己的DLL/manifest。不得修改007A、enabled配置、历史artifact/recovery或Project Lead文档。

本R4在第一次人工启动前允许离线反复修复至全部门禁通过。人工启动后，只有纯loaded-set precheck失败仍可按attempt-specific证据修正配置；任何prior-integrity、protection、candidate、override、run、fault或业务terminal均停止并交回Project Lead，不授权R4内部业务重试。Implementation Codex不得启动/控制游戏或使用Computer Use。

## 验收

四个R4业务phase全部PASS才得到`PASS_CONTROLLED_FAULT_RESTART_ISOLATION`。即使PASS，也不覆盖强杀/native crash、自然victory、replay fidelity或完整产品session isolation。

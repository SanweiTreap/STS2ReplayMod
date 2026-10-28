# SPIKE-001B Phase0-r2 动作诊断窄修报告

日期：2026-10-22。状态：`PHASE0_R2_COMPLETE / READY_FOR_PM_REVIEW / RUNTIME_HOLD`。关闭PM指出的动作瞬态仅留内存、受控拒绝归并为generic observer异常的缺口；业务核心保持原标准。实际两操作轨迹仍未运行验证。未写配置、未安装/启用、未读玩家状态/private raw、未启动游戏或请求人工新动作；业务attempt=0，验证时游戏进程0。

## 1. 独立身份与候选

仍为同一Mod ID **SPIKE001B**，不是新probe任务。独立源码 `experiments/SPIKE-001B-r2/`；原r1源码/报告/sealed pair及951个索引目标全部原hash保留。

| 项 | r2交付 |
| --- | --- |
| owner | STS2ReplayMod.SPIKE001B.TwoActions.phase0-r2 |
| private revision / schema | phase0-r2 / **2**；新增诊断格式不与r1混用 |
| manifest version | 0.1.0-phase0-r2 |
| sealed DLL | `experiments/SPIKE-001B-r2/artifacts-phase0-r2/SPIKE001B.dll`，200192 bytes |
| DLL SHA256 | `F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7` |
| manifest | 同目录SPIKE001B.json，367 bytes |
| manifest SHA256 | `22F4DAFF17705B748B69C6EB16EA089D361D868AA373266CF8F3C185A6AFDD65` |
| 固定环境 | v0.111.0 / 41cef1ea，sts2.dll 9757184 bytes / 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9；Harmony2.4.2.0 |

最终验证收据：`evidence/spike-001b/r2/validation-86fb9841bfe84dd1a1ac1de1e12dd300/receipt.json`；21份source/工程/manifest/脚本等hash在其中，交付总索引为 `evidence/spike-001b/r2/phase0-r2-delivery-2026-10-22.json`。

## 2. 修复内容

新增小型 `Core/ActionObservation.cs`，由真实ActionBridge现有Queue/BeforeExecuted/CardEffect/EndReady/AfterFinished/Cancel/Choice回调与离线持久测试共同使用。摘要只含nullable seq/type/before/round/PlayerTurn、最后site、已观察flags、最后TaskStatus、AfterFinished时是否见异常、受控拒绝site/code和序号。

效果Task仅在既有CardEffect/AfterFinished回调游戏线程读取Status；timer只读Flow内复制/冻结的scalar DTO。没有新patch、Task continuation、轮询live Task、模型序列化或逐帧动作日志。false表示尚无正向观察，null表示NoSample；摘要不把旧Task状态当作当前状态，也不单凭Finished旗标判accepted。

Flow为发生变化的摘要发有限 `action-observed` 事件，重复摘要不写；最多32次/Flow、两动作共用该上限。正常Card及End各5次（Binding/Requested/Executed/Effect或EndReady/Finished）。原4096-byte event、8192-byte外层record及capture heartbeat上限保持。所有后续事件与terminal保留最后摘要；首终态冻结，quiet reset不清动作事实，后续回调/写失败/竞态不能改写第一终态或继续accepted/dispatch。

blocker由最后事实区分BeforeExecuted、CardEffect、EndReady、AfterFinished、EffectTaskSuccess、ActionCommit、Cancelled、Choice、Rejected等；尚无摘要时仍明确NoSample/generic。不存在逐帧动作trace或新通用框架。

## 3. 受控拒绝与非干扰

原接受表达式继续检查，探针自己的Require通过私有ControlledRejection传递编译期code；纯Core校验器在明确无game getter范围以调用点指定的code转换拒绝。三处原null-coalescing拒绝改为等价的present检查并给受控code。

Guard只信任该私有标记类型，不根据传入异常message判可信code。未知探针literal为UnexpectedContract；其他异常（包含message恰为`card-effect-origin`的外来InvalidDataException）统一UnexpectedObserver。只保存有限site/code，不保存message/stack/Action.ToString/NetId/对象reference/模型payload。未定位production故障不作根因结论。

新Guard仍不向production抛观察异常，不改参数/原Task/返回结果；诊断writer失败使证据门终止。原SaveRun匹配调用的唯一Task wrapper不变，动作不新增wrapper。

## 4. 实际持久回读证据

新增回归不是手写期望JSON，而是经过实际使用的ActionObservation→Flow→PrivateStore，再读回Event/terminal。最终验证目录中 `fixtures/action-diagnostics-r2/four-waits-readback.json` 汇总四种不同持久结果：

| 停点 | 最后site / 事实 | blocker |
| --- | --- | --- |
| queued-only超时 | Queue；Requested=true、Executed=false、TaskStatus=null | BeforeExecuted |
| executed未见效果 | BeforeExecuted；Executed=true、CardEffectSeen=false | CardEffect |
| effect已见无AfterFinished | CardEffect；CardEffectSeen=true、Finished=false，最后Status=WaitingForActivation | AfterFinished |
| EndReady已见无完成 | EndReady；EndReadySeen=true、Finished=false、TaskStatus=null | AfterFinished |

第三个用例在观察后实际将测试Task完成，terminal仍保留旧WaitingForActivation，证明timer没有读live Task。四者均deadline-Action，但持久摘要可区分；未据此写成accepted成功。

其他新增持久回读覆盖Queue/CardEffect/EndReady/AfterFinished的site/code、fault与异常presence、取消/选择、未知/同名外来异常过滤、终态冻结/无共享、重复10000次无新增IO、32条/bytes上限、普通writer/terminal writer/拒绝writer失败、writer/deadline/callback race、完整A/fresh-B三点两操作正向和r1格式拒绝。真实Harmony新dummy通过受控拒绝路径，确认exact Task、参数、success/fault/cancel不受影响，不调用无engine生产target。

## 5. 保持不变与审计

增量审计逐份核实以下r1源码**完全相同**：Core Contracts/Trajectory/Projection/FieldReader、NativeCheckpoint、LiveRead、AttemptValidation、Patches。Probe只改owner与terminal DTO导出；PrivateStore只改revision/schema。Flow的16个业务方法/Window主体相同；ActionBridge原12个Contract.Require接受表达式在规范化helper名称后相同，三处null拒绝等价保留。

因此accepted/完成、两次dispatch、B不回录、三点Round1/1/2、floor、20字段/30相邻帧/250ms、deadline、完整投影、生产Task、fresh-A及完整ReadyB/hash/CP门均未放宽。r1游戏引用审计规则继续通过：**159引用、相对r1新增0、7 targets/8 void callbacks**；B唯一效果callsite范围不变，timer whitelist不变，DTO字段只有nullable整数/bool/enum，无live引用或自由文本。

未变化API不重查。新增固定IL仅查PlayerTurn：构造初值1；Increment以+1写回；SwitchSides在IL_00aa调用Increment，普通敌方→玩家侧同时Round+1，extra turn只对相应玩家加PlayerTurn而不加Round。详见 `evidence/spike-001b/r2/player-turn-contract.md` 及最终player-turn-il/callers。synthetic turn=1仍明确为合成前驱，实际首玩家值/回调/下一回合收束未观测。

## 6. 最终离线结果与保留

| Gate | 结果 |
| --- | --- |
| Release | 0 warning/error，已有SDK9.0.311/runtime9.0.13 |
| 合同 | **109/109 = 原88 + 新21** |
| 实际patch/unpatch | **7/7**；未调用生产target |
| 原dummy / 动作诊断dummy | 均PASS |
| 新增范围审计 / PlayerTurn窄IL | PASS |
| r1保留 | **951/951**长度/hash不变，原报告/pair/source原样 |
| A5保留 | runtime405/405、旧24/331/326全部原hash |
| 实际001B runtime | 未执行，业务attempt=0，HOLD |

最终完整正向链仍用A5真实公开初始投影＋synthetic native和后继，不读private raw，不将其当作实际轨迹证明。所有测试目录唯一且不覆盖。开发期106项/107项输出保留；在107项后复查发现按异常message查表可被同名外来异常误认，已改为私有受控类型/纯Core指定code并补两项回归，最终109。改前r2候选及受影响source另保留在 `validation-2631b5d5a52a458eaaab65f22b0c21d1/before-trusted-rejection-fix/`；这不是新runtime候选或业务attempt。

## 7. 复现与交回

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001B-r2/Invoke-OfflineValidation.ps1'
```

命令产生唯一r2验证目录，运行完整合同、两个dummy、7/7 smoke、复用未变API审计和r2诊断/业务保留审计，只补PlayerTurn窄IL，并核对r1/A5封存目标。诊断格式合同见 `r2/diagnostic-contract.md`；未来人工方案沿用r1已复核草案，仅在后续获同包运行授权时使用r2身份/格式。

请Project Lead独立复核最后观察语义、受控拒绝可信来源、四种持久停点、writer/终态/竞态与原成功门保持，再决定是否放行后续配置/安装/人工测试。本轮不修改权威状态、不请求用户运行；当前实际两操作轨迹、实际首玩家PlayerTurn、性能/隔离/完整Replay等仍未证明。

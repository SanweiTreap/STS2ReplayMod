# SPIKE-001A5 Phase0-r2 增量报告

日期：2026-10-09。状态：`OFFLINE_COMPLETE / AWAITING_PM_REVIEW / RUNTIME_HOLD`。

执行依据：[原handoff第6节](../handoffs/SPIKE-001A5.md)与[PM r1复核](../../evidence/spike-001a5/project-lead-phase0-review-2026-10-09.md)。本轮完成 capture 失败诊断窄修；**实际 A/B=0，重建等价性仍 Unknown**。没有修改权威状态文件。

## 1. 修订与保留

r1 在失败帧 reset 后丢失20字段，又在 terminal 前 reset 最后frame/count。r2 将 quiet window 与诊断累计值分开：保留所属 capture phase、callback总数、最后实际 Engine frame/monotonic、20个白名单字段状态、首个 blocker、首个 Exception字段及 reset 前后count。无样本明确为 null/NoSample。deadline 到达后的 Tick 先终止，保留截止前最后样本。

`LiveRead.Read` 用新的小型 `FieldReader` 对每个既有 readiness getter归类；原True条件和依赖关系不变。getter异常只留下白名单字段与Exception状态，不保存异常message/type/stack或生产引用。False/Missing仍reset，Exception仍立即terminal。phase-local引用tuple仍只用于30帧门，不进入持久诊断。

capture内增加每秒至多一次的 heartbeat，延迟poll不补发。20s phase最多20次尝试，Event≤4096 bytes、envelope≤8192 bytes、heartbeat总量≤163840 bytes；正常准点poll实际19次，第20秒先超时。terminal Event与terminal摘要分别最多一次，各≤8192 bytes。Tick不逐帧写盘；ReadyArm/ReadyB/AwaitSave不写heartbeat。timer只读Flow快照并通告terminal，不读live模型。

第一terminal在锁内冻结诊断；终态后不能取ready window、继续采集或commit。诊断写失败fail-closed，terminal自身写失败也不递归或重试。两个固定terminal文件是事件与摘要，同一终态只有一次；若存储失败，不能保证有持久terminal文件，但不会因此提交Ready/capture/compare。

30相邻Engine帧、20字段、250ms门与10/15/5/20/20s deadline长度保持原标准。floor关系、checkpoint范围、Task wrapper和语义比较不变。`Contracts.cs`、`Projection.cs`、`NativeCheckpoint.cs`、`Patches.cs`逐字hash一致；`LiveRead`的capture/reflection/guard/build/RNG部分逐字一致。其余源码变化仅为Flow诊断、readiness getter分类、schema/revision/owner及有界诊断写入、相应离线测试。

修改前保存10份r1源码与原manifest，见[archive-index](../../evidence/spike-001a5/r1-source-before-r2/archive-index.json)。r1已封存候选、manifest、报告、README、脚本、24文件交付索引及全部指向文件均保留原hash。两次r2完整验证都核对24/24交付文件与11/11存档文件；没有重做012A收尾，也没有改旧probe。

## 2. 独立身份

| 项目 | r2交付 |
| --- | --- |
| Mod ID / manifest | SPIKE001A5 / 0.0.2-phase0-r2 |
| Harmony owner | STS2ReplayMod.SPIKE001A5.RoomBefore.r2 |
| Metadata / private root | schema 2 / user://SPIKE001A5/phase0-r2/ |
| DLL | artifacts-phase0-r2/SPIKE001A5.dll；143360 bytes |
| DLL SHA-256 | `8DFB0C4AE6393F61D178EBA7A380690F0574BBE27C69246BB53B214306C5BF09` |
| Manifest | artifacts-phase0-r2/SPIKE001A5.json；362 bytes |
| Manifest SHA-256 | `F6A3230D5827A0B010C3BF2F572781A13959E9052253C54C200AB87019328759` |

候选复制采用create-new，封存与最终验证bin身份一致。r2不读取r1 active namespace，B metadata schema/revision降级被实际负向测试拒绝。原r1的sealed pair不改动。

固定游戏仍为v0.111.0/41cef1ea，sts2.dll 9757184 bytes、SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`，Harmony2.4.2.0。增量审计沿用r1的固定API/保存顺序证据，未重复调查未变的production方法。

## 3. 完整离线验证

最终输出：[validation-r2-9a4b77aebb6b4b4185037b60f7595d08](../../evidence/spike-001a5/validation-r2-9a4b77aebb6b4b4185037b60f7595d08/receipt.json)。已有专用.NET SDK9.0.311/runtime9.0.13，无系统安装。

| Gate | 结果 / 证据 |
| --- | --- |
| Release | 0 warning / 0 error；release-build.txt |
| 合同 | 122/122；保留原99项，新增21项诊断回归与2项实际B schema/revision拒绝；contracts.txt |
| 完整正向链 | synthetic native-schema Arm→seal→A capture→fresh B输入→compare仍可达，包括真实Task封存先于caller建房、持久private读回；不是production A/B。 |
| Patch/unpatch | 4/4；无production目标被调用；实际disarmed ObserveSave保留原Task；patch-smoke.txt |
| Harmony dummy | 非匹配Task不变；匹配success/fault/cancel与observer抛出不干扰；dummy-smoke.txt |
| 增量审计 | 四个patch签名与138个游戏/Godot member引用集合与sealed r1相同；无新patch、finalizer、transpiler、custom Node或禁止production调用；timer仅Flow.Poll/AnnounceTerminal；诊断类型/byte caps与冻结业务源码通过；phase0-il-audit-r2.txt |
| r1保留 | 24/24旧交付文件与11/11源码/manifest存档hash；r1-preservation.json |
| 草案工具 | Test-SealedInput-r2.ps1仅PowerShell AST语法核对通过，未执行其payload读取。 |

新回归从实际PrivateStore的create-new文件读回Event后断言，既非仅看内存stage，也非只断言常量：False/Missing持续阻断、指定getter Exception及敏感异常文本排除、已有quiet count超时、reset前后样本、deadline后callback、A/B heartbeat限频/条数/byte caps、不补发、不逐帧写盘、Ready等待无IO、正常30帧window、诊断clone不共享、终态冻结与无后续写入、malformed字段名白名单、heartbeat/首帧/final commit/terminal写失败，以及真实Task下terminal/tick/writer race。

最小持久判别见[no-tick-versus-never-ready.json](../../evidence/spike-001a5/validation-r2-9a4b77aebb6b4b4185037b60f7595d08/fixtures/capture-diagnostics-r2/no-tick-versus-never-ready.json)：

| 同为deadline-CaptureA | 零采样回调 | 持续handPresent=False |
| --- | --- | --- |
| Callbacks | 0 | 1999 |
| SampleStatus | NoSample=0 | Sampled=1 |
| Frame / sample monotonic | null / null | 1999 / 19990ms |
| LastSample / blocker | null / null | 20字段 / handPresent |
| heartbeat数 | 19 | 19 |

另一个持久例保留deadline前17连续帧及QuietBefore=16/QuietAfter=17；getter Exception例在首个managerPresent=False之外独立记录phaseIsPlay Exception。所有fixture标记SYNTHETIC，不含当前玩家数据。Callbacks表示截止前进入Flow.Tick的采样次数；它不能单独证明SceneTree是否发过被更早环境门拒绝的信号。最后样本也不代表每一历史帧状态。

## 4. 复现与证据

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001A5/Invoke-OfflineValidation-r2.ps1' -Dotnet 'C:/Users/sanwe/.dotnet/dotnet.exe'
```

脚本每次生成新的validation-r2-GUID目录。开发试跑及此前完整验证保留；最终候选对应上述最终receipt。293份最终验证文件包含245个实际持久synthetic事件，总量194733 bytes；这些是多个独立fixture，不是单个runtime phase输出量。交付索引覆盖最终验证全目录、r2合同/脚本/草案/报告、12份当前源码、sealed pair及r1源码存档。

版本化交付：[数据合同](../../evidence/spike-001a5/data-contract-r2.md)、[人工草案](../../evidence/spike-001a5/manual-request-draft-r2.md)、[seal只读核对工具](../../evidence/spike-001a5/Test-SealedInput-r2.ps1)、[增量审计脚本](../../evidence/spike-001a5/Invoke-Phase0Audit-r2.ps1)、[README-r2](../../experiments/SPIKE-001A5/README-r2.md)、[交付索引](../../evidence/spike-001a5/phase0-r2-delivery-2026-10-09.json)。原r1文档继续保留历史内容，不将其旧schema描述当作当前源码合同。

## 5. 交回PM

本轮游戏进程=0；A5 live DLL/manifest均不存在。没有写settings/config，没有安装或启动A5，没有读取当前production checkpoint，没有请求用户进行游戏内测试。unmodded profile1未触及。012A移除文件不证明settings中的enabled残留已经关闭；下次项目启动前的真实有效modded配置仍由PM在同包验收后安排。

请PM验收r2诊断修订与独立候选；实现端在交付后停手。配置、安装、人工A/B和有限运行修复仍属后续同包授权。r2离线通过不证明实际getter环境、checkpoint退出后仍相同、首个运行A/B可达或重建等价；这些仍待运行。

# SPIKE-001A5 Phase0 报告

日期：2026-10-09。提交状态：`PHASE0_COMPLETE / READY_FOR_PROJECT_LEAD_REVIEW`。本轮完成012A收尾与A5离线候选；没有安装/启用A5、读取当前production checkpoint、启动游戏或请求新的人工游戏测试。**有效运行A/B仍为0，重建等价性仍Unknown。** 权威状态文件由Project Lead维护，本报告不代替PM验收。

## 1. 012A 收尾

[closeout收据](../../evidence/spike-inv012a/runtime/closeout-receipt-2026-10-09.json)与[可复现脚本](../../evidence/spike-inv012a/runtime/closeout-2026-10-09.ps1)记录：

- r6主菜单attempt `b1822328e87145a1b9b5ba5181ae06de` 的3条、runtime-r1 attempt `96f2ac6729fb4ce49ac690d4bfabd968` 的14条，按原索引逐份hash核对，create-new归档到各自`records-<attempt>/`；17/17匹配，原probe目录保留。
- 游戏进程=0、workspace runtime-r1可恢复、live精确hash匹配后，仅移除`mods/SPIKEINV012A.dll`及`.json`。runtime-r1 DLL hash B4F18ACEB2D738B630A24F57F92B07AED80786A5AAF522D6C35A165052969D6A、manifest 6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD。
- 更新当前实验README与runtime README的历史完成说明；r5/r6/runtime-r1候选、原报告和原始证据保留。r5/r6没有修改或重建。
- 没有读写settings/profile/save，没有恢复checkpoint。**移除文件不等于修改enabled条目**；settings可能仍留SPIKEINV012A enabled。下次项目启动须重新安排有效modded配置，日常游玩也须明确当前实际加载/存档树，不能凭残留ID推断modded生效。

已读[PM runtime验收](../../evidence/spike-inv012a/runtime/project-lead-runtime-review-2026-10-09.md)，沿用`ACCEPT_DIAGNOSTIC_PASS / NO_RECONSTRUCTION_CONCLUSION`，停止追加012A测试。

## 2. 独立候选

源目录：[experiments/SPIKE-001A5](../../experiments/SPIKE-001A5/README.md)。Mod ID=`SPIKE001A5`，Harmony owner=`STS2ReplayMod.SPIKE001A5.RoomBefore.r1`；schema=1、revision=`phase0-r1`。

| 文件 | bytes | SHA-256 |
| --- | --- | --- |
| [封存 DLL](../../experiments/SPIKE-001A5/artifacts-phase0-r1/SPIKE001A5.dll) | 130,560 | 1F4A6DEC9B50A361D70D7113453C544951FCA3BF6DB164B15175BA36A91AFC1B |
| [封存 manifest](../../experiments/SPIKE-001A5/artifacts-phase0-r1/SPIKE001A5.json) | 362 | 3DFEDDC0CFC4B6829669BF1D49CBD53B6389D065EF39E7A9EDA13D9556AEB84E |

固定游戏：v0.111.0 / 41cef1ea；sts2.dll 9,757,184 bytes / 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9，Harmony2.4.2.0。候选与最后验证bin相同，源码逐份hash与validation receipt一致。没有复制旧controller/protection/recovery框架；只继承已验证的ProcessFrame、20字段、30帧及floor合同思路。

## 3. 实现与固定IL结论

唯一业务：同一production `PreFinishedRoom=null` checkpoint，A不间断进入与fresh process正常Continue的B，在规定语义投影上是否等价。

Arm先绑定live TotalFloor与existing non-null native save FloorReached的相等事实。`Binding`只在匹配production save成功、稳定native bytes解析与身份/floor验证后产生；A/B playable floor明确派生为checkpoint floor+1。room.ModelId与room.CombatState.Encounter.Id分别取得、分别匹配，引用tuple只在phase内30帧使用。

四个patch：NMainMenu._Ready、NGame._Input(InputEvent)、SaveManager.SaveRun(AbstractRoom,bool)、RunManager.SetUpSavedSingleplayer(RunState,SerializableRun)。没有额外production调用；mainmenu使用已证明的SceneTree.ProcessFrame订阅，首个真实回调及证据写入成功后才Ready。Timer只访问Flow和有限证据，不从非游戏线程读取live模型。

精确固定IL结果在[validation IL](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/phase0-il-audit.txt)，可用[审计脚本](../../evidence/spike-001a5/Invoke-Phase0Audit.ps1)复现：

| 边界 | 固定方法 / 结论 |
| --- | --- |
| 前驱可满足性 | StandardActMap `.ctor` token06004a3c将StartingMapPoint设为(center col,0)；AssignPointTypes起点Ancient。EventRoom.OnEventStateChanged token06000e30先核对所有Ancient events finished，IL0046 MarkPreFinished，IL0052 SaveRun(this,true)。因此完成Neow并落盘可提供合法非空前驱；默认无须额外准备战斗/奖励。实际Arm仍验证。 |
| before-room seal | EnterMapPointInternal state-machine token06009782等待SaveRun(null,true)后，才CreateRoom/AppendToMapPointHistory。自动ordering gate确认三个offset顺序，完整awaiter IL保留。目标调用返回wrapper，observer封存/验证完成后才将wrapper成功完成。 |
| floor | RunState.TotalFloor和SerializableRun.FloorReached都以MapPointHistory各行count求和；FromSerializable保留history/visited coords，saved setup pre-room floor不append，正常房间进入append一次。沿用INV-011，不用行号/ActFloor替代。 |
| 保存返回语义 | SaveManager.SaveRun底层异常被catch，CurrentRunSaveTask在finally清null；成功Task后仍须以实际new native checkpoint验证成功。 |
| Continue | prefix只观察正常传入state/save，并在NumReloads increment/save之前重新核对原native bytes、完整载入save canonical和restored pre-room facts；不调用FromSerializable/setup/load。 |
| RNG | RunRngSet全12key与PlayerRngSet全3key，counter/state0..3完整读取；CombatState没有独立Rng字段；CreateCreature token06005704分配MonsterModel独立Rng，实例state也纳入。 |
| oracle | CardModel成本/动态变量getter有lazy cache写入，捕获绕开缓存初始化；SavedProperties.From可保留nested引用，立即serialize/canonicalize再roundtrip。已审计相关getter/serialization/fields，guard限定于数据合同列明观察范围。 |

**生产Task identity会改变**，只限目标保存调用。wrapper用TCS保留原success/fault/cancel状态、fault异常对象与全部inner faults、cancel token；observer/证据失败吞掉并终止实验。取消异常中的Task引用可变化；不声称该对象恒等。正常调用保持原Task。I/O会延后匹配map action的完成；没有sleep、取消production、setup/load或native写入/restore。

Deadline独立：Startup10s、production save15s、seal5s、A/B capture各20s；每100ms独立timer，Ready人工等待没有deadline。seal后启动新的A capture计时；成功commit前重新检查deadline。终态第一者胜出，后续tick/seal/capture不能提交；不可取消的in-flight I/O可结束但不产生有效capture。

## 4. 完整离线验证

最终不可覆盖validation：`validation-r1-62f17c8d3be44aad8facac94c20f2b53`。

| Gate | 结果 / 证据 |
| --- | --- |
| toolchain | 既有专用.NET SDK9.0.311、runtime9.0.13，[toolchain](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/toolchain.txt)。不改系统SDK/runtime。 |
| Release | 0warning / 0error，[build](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/release-build.txt)。 |
| 合同 | **99/99**，[contracts](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/contracts.txt)。 |
| 正向链 | 实际固定DLL JsonSerializationUtility产生/解析synthetic native schema，含existing nested history/rooms/player stats；Arm→seal→30帧A→私有持久化→fresh store/process facts→正常Continue观察合同→30帧B→完整semantic compare可达。[positive trace](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/fixtures/synthetic-positive-trace.json)。 |
| 负向合同 | unbound顺序、missing/negative/mismatched floor、checkpoint null/non-null、players/seed/coord/room/encounter、source/candidate/A commit变化、same process、required schema/RNG state缺失、oracle existing nested variable alias、牌序、energy单字段和RNG单state diff均被识别。 |
| 活性/竞态 | seal后无tick及never-ready deadline、真实Timer polling、首帧证据失败/耗时不Ready、全20字段逐一false reset、30相邻Engine帧门、frame/wall gap/ref tuple reset、evidence commit失败、terminal first/race、deadline赢seal race。 |
| Task边界 | 真实TCS pending Task证明caller未越过seal；原fault对象、多fault、Faulted OCE、cancel token、observer/terminal writer异常分别验证。 |
| patch/unpatch | **4/4**，不调用production target；实际disarmed ObserveSave保持Task、无custom Node ticker。[smoke](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/patch-smoke.txt)。 |
| dummy非干扰 | 实际Harmony dummy postfix包装matched返回Task，nonmatching reference恒等、成功/失败/取消与throwing observer均保持合同。[dummy](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/dummy-smoke.txt)。 |
| 静态范围 | 4个callbacks/target，只有SavePatch的`ref Task __result`；没有finalizer/transpiler、额外production save/load/setup、input commands或原生文件写入/恢复；精确API/IL补齐。[audit summary](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/audit-summary.txt)。 |
| 身份收据 | [receipt](../../evidence/spike-001a5/validation-r1-62f17c8d3be44aad8facac94c20f2b53/receipt.json)含全部源码/候选/manifest hashes；候选另有封存与交付索引。 |

fixture来源是**SYNTHETIC**，使用实际serializer/schema/parser；没有将其宣称为可直接游戏Continue的production fixture，未在离线调用真实FromSerializable/setup/room进入。live restored facts用合同输入模拟，实际演化关系由固定IL支持；Godot lifecycle/真实存盘/最终游戏A/B仍须获准运行验证。程序集加载出现Sentry GDExtension未加载提示，离线正常跳过，不代表启动了Godot或游戏。

开发期修复及原失败保留说明见[development-repairs](../../evidence/spike-001a5/development-repairs.md)。所有修复发生在候选封存之前，没有复活或改动A4及旧probe。

复现命令（不启动STS2）：

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001A5/Invoke-OfflineValidation.ps1'
```

会新建一个validation目录；如只验已构建候选，可用现有专用.NET9执行`tests/bin/Release/net9.0/Offline.dll contracts <新的fixture输出目录>`、`patch`、`dummy`。受测DLL引用固定本机game程序集，CLI不调用游戏initializer或production目标。

## 5. 数据合同与人工交接

冻结[数据/读写合同及语义表](../../evidence/spike-001a5/data-contract.md)，[逐步人工草案](../../evidence/spike-001a5/manual-request-draft.md)。private raw checkpoint和native account路径不进入普通evidence；导出只选semantic/无敏感metadata/events、hash。正常准备、Continue reload保存、退出writer为分开的预声明窗口；B输入仍必须与A seal**字节一致**，变化即阻断，没有restore/rebaseline。

语义至少含position/room/encounter/round/floor、HP/maxHP/energy、四ordered牌堆model/upgrade/有效属性、enemy语义及完整run/player/monster已调查RNG。明确排除NumReloads、process/timestamp/ref/UI metadata，未知或missing不填默认后比较。结果限制在本投影与该首个操作点，不推出正式Replay架构或其他房间/隔离/恢复能力。

建议PM验收后放行同包配置/安装、最短Neow准备与一个完整A/B。若A正常保存并退出使native不同，按合同阻断B，交回PM；不为凑compare恢复旧字节。候选验收前停止在此，不发出实际人工请求。运行修复预算仍由PM最终确定，建议初次A/B加至多两次定位harness后的fresh完整attempt。

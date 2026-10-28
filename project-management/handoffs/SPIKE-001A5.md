# SPIKE-001A5 — 房间前checkpoint A/B control

签发：2026-10-09；最终验收2026-10-22。状态：`COMPLETE / ACCEPTED / CLOSED`。有效实际A/B为SEMANTIC_EQUIVALENT，Differences=0；收尾已验收，停止A5，不再执行下文历史启动/安装/运行授权。

以下第0至8节为已执行合同，保留历史，不据此重跑或恢复native。最终验收：`evidence/spike-001a5/project-lead-runtime-review-2026-10-22.md`。两个精确live文件已移除、A5持久disabled，源码/sealed/private证据保留；当前有效enabled集合据收据为空，下一项目实验先重建modded配置。001B仅另行批准Phase0离线，不自动解锁运行。

## 唯一问题与分工

同一个production `PreFinishedRoom=null` room-before checkpoint：不中断进入首个稳定操作点的A，与fresh process从主菜单正常Continue后的B，规定的语义状态是否等价？

只做一个有效A/B compare，不实现录制分派、Replay session、强记录、seek或正式产品架构；不外推其他房间或存档隔离。继续现有「完成 SPIKE-INV012A 工作包」实现对话，不新开对话、不指定模型覆盖；用户已说明改为gpt-6.1 sol xhigh，模型说明不是证据。

先读CURRENT_STATUS、WORKFLOW、TEST_PROTOCOL、本包和012A PM runtime验收；按需参考INV-011 successor-contract/placeholder audit、INV-012存储/活性合同及A4的SnapshotCapture/SemanticProjectionBuilder。旧工作包只是历史技术参考；不能继承其“modded全文件恒等/基线后一律退役”规则。旧probe永久只读，不修改、编译、安装或运行。

## 0. 先完成012A收尾，不启动游戏

- 将r6主菜单attempt `b1822328e87145a1b9b5ba5181ae06de` 的3条JSON、runtime-r1 attempt `96f2ac6729fb4ce49ac690d4bfabd968` 的14条JSON，从精确probe-owned目录不可覆盖地归档到`evidence/spike-inv012a/runtime/records-<attempt>/`；逐份核对既有索引hash。不得搜索或归档其他profile/生产payload。
- 游戏进程=0，workspace runtime-r1候选身份可恢复，live `mods/SPIKEINV012A.dll` hash=B4F18ACEB2D738B630A24F57F92B07AED80786A5AAF522D6C35A165052969D6A、manifest=6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD时，可移除这**两个精确安装文件**，不递归删除、不碰旧probe或原始artifact。
- 不编辑profile/save/settings、不恢复checkpoint，不为了禁用再启动已完成probe。清理不代表settings里的enabled条目已改变；说明文件已移除、配置条目可能残留。下一次项目游戏启动前必须重新安排有效modded配置，不能以残留enabled ID当作实际加载证明。用户若要日常游玩，明确告知此区别。
- 提交简短closeout收据；更新当前实验README的历史/已完成提示，避免仍把runtime-r1称为“未获安装许可”。不修改旧报告/证据内容或权威状态文档。

## 1. 新身份与复用范围

- 新mod ID `SPIKE001A5`，独立Harmony owner/schema/attempt命名；source `experiments/SPIKE-001A5/`，evidence `evidence/spike-001a5/`，报告`SPIKE-001A5-Phase0-report.md`。
- 固定v0.111.0 / 41cef1ea，sts2.dll 9,757,184 bytes / SHA-256 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9；Harmony2.4.2.0。
- 优先复用已证明的SceneTree.ProcessFrame观察、GD.Print标记、首帧后Ready、独立monotonic timer与有限证据设施。只拷贝必要模块并改为新identity，不复制整套旧保护框架，不搞通用测试平台重构。
- 默认modded profile1、正式实际只加载A5，Cloud现场关闭。Phase0只用离线fixture/mock/固定DLL，不读取当前玩家状态；unmodded profile1始终不触及。

## 2. 冻结的floor/身份关系

```
CheckpointFloorReached = ArmPredecessorFloor
A.PlayableTotalFloor = B.PlayableTotalFloor
                     = CheckpointFloorReached + 1
```

Arm捕获live TotalFloor与前驱save FloorReached，两者present、非负且相等后绑定；checkpoint floor先Unbound，production save成功后解析、校验再绑定，不能用-1/default/empty占位参加比较。地图行号/ActFloor不替代TotalFloor。

checkpoint要求exactly one player、seed/character/act/目标coord匹配、VisitedMapCoords present、PreFinishedRoom=null。A实际绑定room model与encounter语义ID，B匹配两者；不得互相复制或用CLR类型当model ID。对象reference只用于各phase连续性，不进入跨进程语义比较。

选择最短**可满足前驱存档条件**的Act1普通Monster路径。首排若不满足，不假定可Arm；通过固定IL/fixture明确是否需先完成一个准备房间，再在草案说明必要的奖励与停手点。准备写入不属于A/B业务差异，不强制固定seed或无意义重打多个房间。

## 3. checkpoint封存与生产非干扰

在匹配target的production `SaveRun(null,true)`成功后、caller继续创建房间前封存该次checkpoint及验证身份，不靠普通异步ContinueWith猜测时序。保留独立ProductionSave、Seal、A Capture、B Capture deadline；baseline封存不能取消capture计时。

若这个before-room边界必须返回observer wrapper Task，**仅允许匹配目标调用的实验包装**，需明确记录Task identity会变化；production success/fault/cancel结果及原异常语义保持，observer验证/写证据异常不得改变成功map action。其他调用保持原Task。不得阻塞睡眠、取消production或调用新的setup/load方法。用真实Task测试边界/race，且受测业务不能被probe异常污染。

Phase0冻结精确读写合同：后续仅允许所选modded1的native current_run，以及probe-owned checkpoint副本/语义snapshot/元数据。原生checkpoint若含用户标识，完整副本限private probe目录，不进公开报告或普通evidence；只导出hash/必要语义。checkpoint副本只是证据，**不授权恢复或覆盖native save**。

不建立十二份recovery、不跨历史比较所有settings/progress/prefs/replay字节。native测试准备、normal Continue的reload计数保存、正常退出writer是预声明正常行为；在独立窗口记录，不作为个人数据损坏。但B输入必须仍是A封存的同一checkpoint，字节变化不能静默忽略。

## 4. A/B计划与语义投影

运行方案（依第7节已放行）：A新建测试局→地图Arm→点唯一target→封存checkpoint→20字段、phase-local身份稳定的30相邻ProcessFrame→capture A→停手并正常退出。无出牌/EndTurn/奖励/下一节点。退出后验证native current_run仍为sealed checkpoint；不等则阻断B，不自动restore/rebaseline。

B新进程→检查exact environment与checkpoint bytes→用户主菜单正常Continue一次→在相同30帧门capture B→正常退出→语义compare。新probe不自行驱动Continue/setup/load。B之前可只读观察production FromSerializable/SetUpSavedSingleplayer以校验pre-room floor/coord；不得额外重建run冒充正常路径。

Phase0明确投影至少覆盖：目标位置/room model/encounter/round/TotalFloor；角色HP/maxHP/energy；hand/draw/discard/exhaust的顺序与card model/升级/有效属性；敌人集合的规定语义字段；完整可取得的run RNG seed、stable keys、counter/state0..3与digest。核实player/combat独立RNG是否包含，列明范围，不声称覆盖所有未调查状态。

单独列出NumReloads、process IDs、timestamps、object refs等非玩法元数据及其排除理由，不能把未知字段归零以制造等价。解析失败/missing/exception不参与PASS。用可分辨的一字段差异证明比较器不是只比seed/手牌数；expected/actual不得共享可变对象。

## 5. Phase0验收与有限修复

- Release构建无warning/error；环境命令优先已有专用.NET9工具runtime，记录实际SDK/runtime，不改系统安装。
- 证明完整正向A seal→A capture→fresh B→compare的合同可达；fixture标记synthetic/production来源，不把mock通过当作runtime。覆盖floor bind顺序、缺失字段、room/encounter分离、跨process identity、source changed-before-B。
- 关键回归：baseline后无tick/never-ready必在deadline终止；首帧证据失败不Ready；保存原Task fault/cancel及observer异常不干扰production；terminal/race只一次、不能继续采集；全fields30连续帧门；跨phase无共享mutable graph；frame gap reset。
- 受影响patch/unpatch和dummy非干扰，精确IL补齐新用API；不重新调查全部DLL或另开INV。交付可复现命令、候选/manifest hash、读写/数据合同、语义投影表、逐步人工草案。
- Phase0内可自行窄修harness并保留已封存候选/证据，不为每处代码缺陷请求PM。后续运行建议预算为初次A/B，加至多两次已定位harness修复后的fresh完整A/B attempt；不能复用失败A与新B凑compare。最终运行授权以PM对本合同/候选的验收为准。

交付后停止，等待PM候选验收；不安装A5，不请求用户打开游戏。下一目标是**一个有效A/B答案**，不是更多辅助调查或功能。

## 6. PM验收后的Phase0-r2窄修

2026-10-09验收见`evidence/spike-001a5/project-lead-phase0-review-2026-10-09.md`。r1已独立通过99/99、4/4、dummy与固定IL审计，012A归档/清理已完成。不要重复012A收尾，也不要因该小缺口创建调查包。仅修以下诊断问题：r1的失败帧不保留字段状态，terminal又在Reset后写入，导致no-tick与持续never-ready无法从记录区分。

- 分开quiet-window reset与诊断累计值。保留capture所属phase、callback计数、最近Engine frame/monotonic sample、20个命名字段状态及首个阻断字段；无样本用明确null/NoSample，不能伪造False或-1作为真实采样。计数与最后样本在terminal冻结前不得清零。
- 选用terminal摘要加有限heartbeat，或等效的有界持久方案。capture内至多每秒一次heartbeat，单个20s phase设置明确条数/bytes上限；不逐帧写盘，不在人工Ready等待中持续写文件。timer只读取Flow内冻结诊断值，不读取live模型，不能增加新的production patch。
- Exception保留哪个白名单字段失败，不导出自由文本、stack trace、模型值或reference。terminal只封存一次；最后诊断和terminal写入失败时仍fail-closed，不继续Ready/seal/capture/compare commit，不递归无限写入。沿用第一终态胜出规则。
- 新增有区分力的回归：零capture回调超时；有回调但单个False/Missing长期阻断；指定getter Exception；已有连续计数后deadline；heartbeat限频/大小；terminal与tick/writer race；证据写失败。至少从持久事件断言no-tick和never-ready不同，而非只检查内存stage。
- 不改30相邻Engine帧/20字段/250ms门、deadline长度、floor关系、checkpoint读写范围、Task wrapper或语义投影。不改或放宽比较结果。

允许自行修实现、测试及新revision脚本。新候选使用独立revision/私有目录和版本化数据合同；不与r1持久格式混用。修改前保存r1受改源码的精确副本；已封存DLL/manifest、报告、24文件交付索引及其指向的证据全部保持原hash，r2新增相应合同/人工草案/脚本，不覆盖r1。无需为源码编辑逐次请示。

执行Release与相关合同回归，维持完整正向链；重跑受影响patch/dummy/白名单审计。未改动的固定游戏API不重新调查。提交r2候选身份、增量报告和复现记录，随后等PM验收；本节**不授权配置写入、安装、玩家状态读取或游戏运行**。后续运行将合并配置/安装/A/B与有限修复，避免再次切碎步骤。

## 7. 已授权的配置安装和人工A/B

2026-10-09 PM验收为`ACCEPT_PHASE0_R2 / AUTHORIZE_MANUAL_AB`，见`evidence/spike-001a5/project-lead-phase0-r2-review-2026-10-09.md`。当前唯一业务问题及第2至4节标准不变。以下步骤可在同一对话顺序完成，不再逐checkbox请求PM授权；到游戏操作时向用户发简短具体请求并等待其结果。

### 候选和数据范围

初始sealed pair为`experiments/SPIKE-001A5/artifacts-phase0-r2/`：DLL 143360 bytes / SHA-256 `8DFB0C4AE6393F61D178EBA7A380690F0574BBE27C69246BB53B214306C5BF09`；manifest 362 bytes / SHA-256 `F6A3230D5827A0B010C3BF2F572781A13959E9052253C54C200AB87019328759`，version `0.0.2-phase0-r2`。schema2、owner r2、private revision phase0-r2。游戏/Harmony身份沿用第1节，安装前与每次启动前重新核对。

冻结数据/语义范围见`evidence/spike-001a5/data-contract-r2.md`；手动步骤沿用`manual-request-draft-r2.md`，其“未授权”是Phase0历史状态，本节放行其业务流程并收紧首次启动配置方式。不要修改sealed草案以覆盖历史，另建实际请求。native raw/path始终仅在private probe目录，普通evidence选择性导出公开metadata/events/semantic；不整目录glob导出。只读核对工具`Test-SealedInput-r2.ps1`现在允许在A退出后执行。

### 游戏关闭时先准备配置和安装

1. 游戏进程必须为0；候选、游戏和manifest身份通过。盘点已知项目probe的精确live pair和mod enable/source记录，不修改旧probe文件。A5目标pair若不存在则create-new安装；若已存在只接受同hash，其他hash不猜覆盖。
2. 允许读取当前Steam账户的精确`settings.save`及`modded/profile.save`，路径按INV-012/preflight既有规则解析、拒绝reparse/多账户歧义。只导出mods_enabled、mod ID/source/is_enabled、modded last_profile_id及hash；不得输出账户数字、原始settings、绝对账户路径或解析异常payload。不得读unmodded selector/profile1或其他profile payload。
3. 用户已允许自由调模组开关和使用modded槽。本包授权游戏关闭时做**范围明确的离线配置修改**：`mod_settings.mods_enabled=true`；本次本地A5精确source启用；其他已安装local/Workshop模组及全部旧probe禁用；`modded/profile.save.last_profile_id=1`（已是1则不写）。不得修改任何非mod account设置或selector其他字段，不删除mod记录，不编辑游戏存档/Cloud。
4. 修改前将仅这两个实际需改的配置create-new备份到private probe配置目录，完整内容不进公开evidence。schema/source枚举须由当前文件及固定游戏规则确认，不猜字段或来源值。验证变更严格落在上述字段、其他canonical值未变，写后重新读取；记录过滤差异和hash。写前再次确认进程=0及源hash未被并发修改。无法保证范围/有效source则停在离线准备，不先打开游戏探路。
5. 安装与配置核实后，本地A5 pair存在/精确匹配，mods_enabled=true、modded selector=1且有效启用集合应只有本次A5；无其他local/Workshop副本绕过(id,source)禁用。**不得零有效模组启动后再进设置启用A5**，不得凭已移除012A的enabled残留证明modded。首次启动前请用户现场确认Cloud关闭；安装文件/持久配置与实际loaded集合仍分别验证。

配置字段/schema已知时直接完成，不另发调查。配置错误且未Arm可正常退出、离线修正后再次做precheck，保留记录，不占正式A/B预算。若探针已产生terminal/active业务记录，不能拿原attempt继续；按下述修复规则处理。

### A和B依次进行

- 首次用户手动启动，只停modded profile1主菜单。确认候选身份、当前log的actual loaded only A5、`PRECHECK_PASS READY_A_ARM`，才允许建局。额外mod/身份拒绝就退出修配置，不能继续Arm。
- A按草案最短路径：新建Standard单人Ironclad A0、完成Neow到地图并停手。允许替换测试槽Continue，不涉及个人unmodded槽。实现者确认准备窗口后才请求F8一次；根据`READY_A_NODE`与当前地图说明唯一Monster坐标/图标，不能让用户猜“第一只”。只有该目标点击一次，然后保持聚焦、不再输入。
- 目标进入后不出牌、不EndTurn、不暂停、不领奖、不点下一节点。`A_CAPTURED_STOP_INPUT`必须结合A.semantic/A.capture/A.complete、seal、window与无terminal确认。20s capture deadline不包含地图人工等待；若战斗出现后约30s仍无capture/terminal，由实现者检查当前有限证据并安排正常退出，不继续无界等待或重试输入。
- A有效capture后，用户游戏内保存并退出到主菜单，再主菜单退出游戏。进程=0后验证native仍是sealed exact bytes。若变化，阻断B并报告本attempt无有效比较；不恢复native或改oracle以强行继续。
- A完整提交、source字节未变、配置/候选未变及Cloud重新确认后，可以直接请求fresh B，无需再等PM发包。B主菜单先确认fresh process、一次B.claim、`READY_B_CONTINUE`，才请用户正常Continue一次并停手。探针不自行setup/load；Continue后的NumReloads原生保存是预声明B写入，不据此否定已核实的B输入。
- B同一30帧门取得snapshot；必须结合无terminal、A.complete、B.capture和`compare-complete`核实`SEMANTIC_EQUIVALENT`或`SEMANTIC_DIFFERENT`。两者都是有效答案，得到一个即停止，不为追求等价重测。用户按A相同路径正常退出。

### 有限harness修复权限

运行预算：首次完整A/B，加最多**2次已定位harness窄修后的fresh完整A/B**；最多3个正式业务attempt。Phase0修订不占此预算。即使baseline已建立也允许证据明确的harness修复，不再因单纯代码bug永久退役整条A5。

可自行修当前A5的提示/Arm、ticker/字段采样、诊断/deadline/状态机、证据生命周期、已证明的等价身份绑定/序列化实现错误。先定位原因、保留旧源码/日志/终态，再做最小改动、新candidate/revision/私有namespace，补有区分力回归与相关Release/patch/非干扰审计；游戏关闭、原installed hash精确匹配且workspace可恢复时可替换本包自己的两个安装文件。完成这些门后允许直接向用户请求新的完整A/B，不为每次编辑/安装再问PM。A与B必须同候选/同checkpoint；失败A不能与新版本B拼接，不覆盖active/artifact，不恢复存档。

不得降低20字段/30帧/250ms、floor或语义标准；不得新增production方法调用、manager/store override、操作分派、游戏自动化或扩大数据范围；仅已有匹配保存的Task wrapper例外继续有效，不扩展其行为。真实语义差异不是harness失败；source在退出后变化、未定位production fault/副作用、无法保持原标准、涉及个人槽、业务答案已得到或预算用尽时，停手交回PM。启动后版本改变亦停止，不自行换game build。

### 交付和收尾

提交`SPIKE-001A5-runtime-report.md`、attempt/build索引、有限元数据/语义差异、A/B commit与输入身份验证、准备/业务/退出窗口及修复预算使用。明确已确认、Unknown、为何有效/无效；无snapshot/commit不写production reconstruction FAIL。PM维护权威状态，不自行解锁SPIKE-001B。

游戏退出后可不可覆盖归档本probe公开白名单记录；raw checkpoint/path/private配置备份不导出。可移除本包实际安装且hash精确匹配、workspace可恢复的**两个精确DLL/manifest**，不递归删除，不动旧probe/源码/evidence。若保留则持久禁用A5；关闭旧probe残留enabled条目也在本包配置授权内。恢复测试前的非probe模组开关时仍不得重新启用退役probe；配置变更不为此触发额外游戏启动，记录下次启动有效mod状态，不宣称已验证个人槽安全。

## 8. 10月21日续跑和账号范围说明

旧attempt `861af3a9e6d449acbd192b0a8d3c2f56`成功Arm后，用户在目标点击前要求正常退出并随后要求继续。记录为`USER_INTERRUPTED_BEFORE_TARGET / NO_VALID_AB`，不是harness/production故障；旧active、arm、事件原样保留，不允许续接B。已Arm正式记录1/3，下一轮未Arm；用户中断不虚报为harness修复，identity revision不重置上限。

接受同ID/schema2的独立`runtime-r1` namespace用于fresh完整A/B。当前源副本`experiments/SPIKE-001A5-runtime-r1/`，manifest version `0.0.3-runtime-r1`，owner `STS2ReplayMod.SPIKE001A5.RoomBefore.runtime-r1`，private root `user://SPIKE001A5/runtime-r1/`。DLL 143360 bytes / SHA-256 `AB97852918D5291B5D38E4D80C199C38A71F154A94EC982F5CE70510ACB52596`；manifest363 bytes / SHA-256 `EF4B1D6FADA613C88CA850C9E4AC800C0376E181669335C6FC7CD87732A0A421`。业务代码只存在两处identity字符串替换，PM独立125/125、4/4、dummy/IL通过；第7节全部业务门继续有效。A与B必须同一runtime-r1候选及本轮seal，使用对应的`runtime/Test-SealedInput-runtime-r1.ps1`只读核对工具，不混用r2输入。

目前新增第二Steam账号目录。目录多不等于必须删除，也不是允许检查另一账号的理由。当前只操作原A5测试账号：用户的当前账号说明、Steam ActiveUser的唯一目录匹配及既有probe账号scope应一致，无法唯一确定就停在配置前；不得按最近日志、目录mtime或简单取第一个目录选账号。解析、配置备份/变更/回读仍限制为原账号第7节允许的两个配置及字段，完整账号信息/私有路径不导出，另一账号settings/selector/payload不读取或修改。

用户在PM对话最新说明“另一份存档不用管，准备自行删除相关配置”。这不是Codex删除授权，不是删除已完成或已切回原账号的事实证据。另一份的处理完全排除在任务外；不要求用户先删目录才测试，不重复询问其删除情况。原实现对话若仍需当前登录账号确认，只保留这一必要确认；匹配通过便按既有同包授权继续，不另申请INV/审批。每次新进程仍确认Cloud/固定game/live/only-A5/modded1/首帧Ready，不能复用旧进程通过记录。

复核与派发反馈见`evidence/spike-001a5/project-lead-progress-review-2026-10-21.md`。本说明不改变业务标准、I/O或个人槽边界；旧结果不追认，SPIKE-001B保持未解锁。

最新进展：用户已在原对话明确确认切回原测试账号且本轮不切换，三重匹配和配置回读通过。按`runtime/prelaunch-A2-runtime-r1-2026-10-21.json`已解锁fresh主菜单请求；Cloud需当前现场确认，新进程actual loaded/profile/first-tick/Ready通过前仍不得Continue、新建局或F8。无需再次确认另一份配置是否删除。

## 9. 最终验收与结束

2026-10-22 PM验收attempt `3e6aaa0160e441c293b5ac4a0030b6d0`：相同room-before checkpoint、fresh B、两端20字段30帧和完整commit成立，8088-byte semantic逐字节/JSON结构相同，Differences=[]。正式记录2/3（旧轮用户中断、新轮有效），不使用剩余一轮扩大范围。125/125等既有离线证据保持，405份runtime交付及旧索引保留通过。

游戏已退出；两个精确安装文件移除、18个其他旧probe文件不变；private checkpoint/active及workspace可恢复。无有效enabled mod为本次收尾配置事实，不启动游戏验证unmodded树，也不将其说成个人槽安全已验证。结论只覆盖固定样本和冻结首操作点投影，不覆盖后续操作、完整隔离/Replay/性能。A5关闭，新任务见SPIKE-001B Phase0。

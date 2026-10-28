# DET-001 runtime 完成报告

定稿：2026-10-24。结果：**`SEMANTIC_EQUIVALENT / CLOSED_READY_FOR_PROJECT_LEAD_REVIEW`**。同candidate-r3、同room-before CP的fresh A/B完成五稳定点、四实际操作、首次自然重洗五内部边界及完整九步RNG证明，逐点与规范化内部记录全等。正式Arm **1/3**，不追加样本。

最终收尾已独立核验：游戏进程0、DET001及退役探针持久disabled，本包两个exact/recoverable安装文件已移除，其他18项目文件不变，剩余29项installed manifests无有效启用项。178份本轮白名单原记录及私有CP完整保留。源码与r1/r2/r3 sealed产物、旧准备/失败/索引均保留；原native未由Codex写回、restore或rebaseline，另一账号与个人unmodded profile1未访问。

PM已独立复核业务样本为`ACCEPT_SINGLE_SAMPLE_RNG_REPLAY_EQUIVALENCE`，见`evidence/det-001/project-lead-B-complete-validation-2026-10-24.json`；该复核明确未覆盖最终收尾。现交回最终收尾与交付索引验收，不能把业务验收提前称为最终closeout验收。

## 最终样本与覆盖

| 项目 | 实际值 |
| --- | --- |
| 固定游戏 | v0.111.0 / 41cef1ea，game DLL SHA256 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9 |
| 候选 | candidate-r3 / schema2 / DLL281600 bytes，20A7E5BEB203E29D5A691086A33AAC7641B6BD4B16C2FB8D8C88DE935A4A8446 |
| Attempt | af99c3b9c7bf411882d580c2f3565408 |
| A / fresh B | 58312:639283459045976987 / 81240:639283673562637803 |
| 场景 | 原测试账号modded1，仅DET001；Standard单人Ironclad A0；seed20FRCAL12Q8Y；BurningBlood＋GoldenPearl；原5Strike/4Defend/1Bash |
| 目标 | Act index0 / row1 col0 / SHRINKER_BEETLE_WEAK；room-before floor1 → playable floor2 |
| 原CP | 45534 bytes / AD987823B7FBA7260542A6951DEB3594928EF717B0942E141B9956FB1AA80DD2 |
| 操作 | Defend → EndTurn → Defend → EndTurn；两端各4条accepted/completed，B四dispatch |
| 观察 | A63/B59连续单调事件；A87＋B90＋activation1＝178份白名单原记录 |

| 点 | Round/PlayerTurn | HP/能量/格挡 | 手/抽/弃/耗 | A/B MaxGap(ms) | 比较 |
| --- | --- | --- | --- | --- | --- |
| S0 | 1/1 | 80/3/0 | 5/5/0/0 | 34/9 | 全等 |
| S1 | 1/1 | 80/2/5 | 4/5/1/0 | 10/9 | 全等 |
| S2 | 2/2 | 80/3/0 | 5/0/5/0 | 34/9 | 全等 |
| S3 | 2/2 | 80/2/5 | 4/0/6/0 | 10/13 | 全等 |
| S4 | 3/3 | 78/3/0 | 5/5/0/0 | 11/9 | 全等 |

全部窗口20字段true、30相邻真实帧、gap≤250ms；完整资格、floor、动作定位、原效果完成和hash链通过。E0…E4各端成员/顺序及S3/S4牌堆链接成立，原Shuffle和Draw Task均RanToCompletion。两端实际Run.Shuffle9→18，分别从各自E1推导九步，四个64bit state及counter/digest逐项匹配；不以counter变化代替完整证明。规范化轨迹SHA256：`5C7A1005EAB6C74018FB68764B10DC08EA0E9C57236B0C780D2686BA11868436`。

## 退出、写入窗口与限制

A退出后、B准备前后及fresh B Continue前，native/private均与原CP逐字节相同。正常Continue的reload/save是预声明B写入窗口：num_reloads0→1，native45864 bytes / `8ACC64483C20D284F37F78F518EBA3AEC7FD445791E9D283CA31242CA952379F`；B结束、正常退出后及离线收尾前后均保持该值。私有原CP始终保留，不声称全程零原生写入。

收尾token `e8dd68e34a294cce9bfdde1e54084531`：完整private配置备份、实际原准备非probe flags/global还原，其他配置字段canonical保持、modded selector1未写。首次收尾在exact文件移除处因只读属性中断；配置已禁用、两文件均未移除。保留中断收据和原脚本，独立Finish只用精确LiteralPath/Force移除两文件，不重复配置变更；最终Verify-R3Closeout通过。

主要入口：`evidence/det-001/implementation-runtime-handoff-2026-10-24.md`、`evidence/det-001/runtime-delivery-2026-10-24.json`、`evidence/det-001/runtime/Verify-RuntimeDelivery.ps1`。公开复核只读workspace；含账号scope/私有备份核对的Verify-R3Closeout单独保留，不要求PM重读账号或native。

结论限当前固定版本、原测试modded1、GoldenPearl原十卡及一个弱甲虫的受控样本。其他五项Neow仍只有离线资格证据；不外推多次稳定性、任意卡组/遗物、未投影状态、崩溃恢复、隔离Replay、headless、性能或正式产品架构。r1故障、旧渲染停止/输入不明原因和未Arm准备偏差原样保留，未伪记为业务失败或已解决。STATE/PERF/产品不解锁，无后续游戏测试请求；当前配置下下次预计unmodded，仅配置推断，未启动验证或进入个人槽。

## 历史执行记录

以下保留各阶段当时状态。历史0/3、无Arm或待B的文字不覆盖上面的最终结果；既有不可覆盖原始收据不回写。

## 准备与主菜单

`evidence/det-001/runtime/README.md`及准备/独立回读收据记录原账号scope、固定game/Harmony、精确r1安装、only有效DET001/modded1。用户明确确认本次Cloud关闭、原账号、只停主菜单。进程35708:639283364837019961，attempt43c5d6b004e344318374bbb410cdf6eb：actual only DET001、modded1、first-tick/READY_A_ARM通过，Arm/activation无。

## r1 新局准备失败

User-observed：新建单人局点击确认后、尚未见Neow，出现EntryPointNotFoundException。机器有界当前日志相同异常，首帧为 `ListExtensions.StableShuffle_Patch1`，调用者 `StandardActMap.AssignRemainingTypesToRandomPoints`。固定IL确认该caller调用StableShuffle<MapPoint>，并非目标CardModel自然重洗。未Arm、未seal、无动作/目标RNG记录；不能称为Neow资格不合格或业务重放差异。

按请求用户通过游戏内流程正常退出，机器核对game0。`runtime/stopped-r1-43c5d6b004e344318374bbb410cdf6eb.json`保留有限白名单堆栈及过程；raw log/native/账号路径未导出。第一次停止收据包装的数组bool检查失败后，仅重读有限日志并生成收据，未重放任何准备变更。r1 source/sealed pair/73索引及全部旧证据保持。

## 已定位的harness原因与修复范围

固定IL `runtime/shared-generic-fixed-il.txt` 显示StableShuffle<T>含ToList/Sort/generic List访问和UnstableShuffle<T>；地图使用同一共享引用泛型。新离线 `experiments/DET-001-generic-repro/` 只用新建List<string>与SerializableRng私有对象，静态检查其路径无engine/manager/store；不启动游戏、不读账号。

独立进程结果：baseline正常排列、counter46→48；仅安装r1实际PermutationObservationPatch则EntryPointNotFoundException、counter仍46；提前warmup后同样失败。Guard在原泛型trampoline之前不能修复泛型上下文。旧metadata装卸/不实际使用泛型字典的dummy没有覆盖这一故障，不追溯改写原验收。

Harmony2官方[泛型边界说明](https://harmony.pardeike.net/v2/articles/patching-edgecases.html)亦说明引用泛型共享及类型上下文可能丢失；本次归因以固定IL、实际堆栈及上述隔离复现为主。

D-081允许已定位观察绑定harness窄修。拟建独立candidate-r2：取消全局StableShuffle<CardModel>方法补丁，只在固定非泛型Shuffle状态机的唯一原StableShuffle<CardModel> call前后插入只读观察，保留原call/参数/返回/分支/EH及所有原指令；不替换Shuffle/Draw Task、不改变9步/成员/五点四操作/资格/CP/readiness门，不新增Game效果API。需相关回归、Release、实际patch/非干扰和白名单通过后，才在game0、exact r1 live/workspace可恢复门下换本包pair，使用fresh新attempt，不拼接旧A/B。

## r2验证及精确替换已完成

上述窄修已完成。独立源码`experiments/DET-001-candidate-r2/`；r1任一源码/产物/73索引目标均未编辑。新合同差异与索引在`evidence/det-001/candidate-r2/`。保留完整原call/所有原指令、相同参数/返回及EH；E1/E2仍紧夹实际原排列调用，未放宽观察标准。

`validation-final-v1`：Release0 warning/error，72/72；11实际target metadata patch/unpatch（原共享泛型方法不再patch，改为唯一非泛型Shuffle.MoveNext）；15普通callbacks及1精确callsite transpiler。真实字符串/整数utility在全r2补丁下结果/返回对象/私有RNG计数正确，实际同形caller保留泛型上下文、原返回/owner、throwing观察者及原同步异常引用。9条新增IL、全部原指令按原顺序各保留一次，缺失/重复/带label call拒绝。Cecil审计r1其余全部业务源码只差owner/revision，原3效果API/Task/timer/投影/资格/五点四操作/CP/RNG/差异门逐字保持。r1隔离失败cold/warm均保留，旧73/51/6/158只读核验保持。

新sealed DLL276480 bytes，SHA256`D4A5DDACA5C75A6D09899420CE83F26391975003A07B887B37C6E4A4642CB3FA`；manifest304 bytes，SHA256`187EAC0FDB0A65CC4CA2408092D872B17B7B8CEC44BFF598603635F6F0AF4681`。owner/private revision均candidate-r2，schema1，A/B必须新同候选。

`runtime/r2-preparation-9847580493c94c6d801eae5720dd9e7f.json`记录一次性精确替换：先game0/原scope/r1 installed及workspace exact匹配/已验r2门，settings完整private备份；只移除固定mods目录下本包两份exact可恢复r1文件并create-new安装r2。按当前有效(id,source)关系补回缺失disabled行/only DET001；已有记录和非mod/其他mod字段canonical保持，selector1未写，其他18项目文件hash保持。不读写native或另一账号/unmodded。

独立只读`Verify-R2Preparation.ps1`通过，收据`r2-preparation-verified-9847580493c94c6d801eae5720dd9e7f.json`；未重放任何mutating脚本。当前installed/persistent only DET001与实际r2 loaded分开：后者仍未观察。下一步只请用户原账号/Cloud关闭、fresh启动停主菜单；预检通过后再另发一局合格Neow准备，不连续跳步。正式Arm仍0/3。真实CardModel/状态机scope/R3重洗/A-B仍Unknown，STATE/PERF/产品未推进。

## r2 fresh主菜单通过

用户明确回传原账号、Cloud关闭、主菜单保持运行且无其他操作。当前进程31028:639283378408584778，新attempt cf96aace8b6f4d338ddf26c0803ecf93；Verify-R2AMainMenu只读确认r2 exact pair、original scope/当前log token匹配、actual仅DET001/modded1、first-tick/READY_A_ARM，Arm/activation/terminal均无。收据与两份白名单事件已保存，不导出raw log/native。此门仅解锁一局Standard Ironclad A0的GoldenPearl准备，未证明地图生成/Neow已成功，更未解锁F8或业务。

## Neow未提供与用户新增准备授权

用户到达Neow、未提供GoldenPearl，未选别项或重开。r2实际新局因此已越过r1地图生成故障点；仍没有目标观察。用户随后直接允许反复重开找金珍珠，覆盖此前仅准备一局限制，业务门及Arm预算不变；已记录`user-neow-clarification-2026-10-23.md`，先分最多5次的准备批次，不自动UI，不盲选别项。

用户提出当前有LavaRock。固定IL证实它只在首幕Boss奖励满足owner/未触发条件时追加两RelicReward，五个draw/shuffle hooks继承默认；当前普通房间不会触发该奖励效果。此项作为后续最小资格修订建议交PM，当前r2白名单明确仍为GoldenPearl，未现场改DLL或放宽。继续当前已验候选的GoldenPearl准备；若5次无选项或异常，停手回报，不假定已合格。

## D-082：六选一资格已完成，r3准备通过

用户随后因多次未出要求考虑华美发束、轰鸣海螺、熔岩石、飞鞋、钓鱼竿；最新明确回传约十次、每次未选、正常退出。按实际报告记录，不把此前批次上限5误记为实际次数。`runtime/r2-exit-before-r3.json`机器核对原scope/game0、exact r2 pair和r1/r2/r3 activation均无，Arm0/3；未读native/他账号/个人unmodded。

PM D-082直接允许同包静态核查和独立r3。五项均有固定方法体/正常Neow路径及中文资源身份；加入精确白名单，保留GoldenPearl。仍恰有BurningBlood加一个，四个状态型选项必须未使用/未触发；海螺仅普通Monster，飞鞋只直接Children，钓鱼竿/发束不打完战斗或领奖。详见`candidate-r3/neow-eligibility-matrix.md`及独立`DET-001-candidate-r3-report.md`。

schema2新增必填Neow实例资格，实际getter/完整ToSerializable、每点一致和CP完整实例canonical绑定；新增5只读游戏引用。r2非泛型callsite观察/原三个B效果/Task/SaveRun/timer/五点四操作/九步/readiness/floor/首差异门保持。最终validation-final-v2 Release0 warning/error、144/144、11 metadata targets、15 callbacks及1未变transpiler；r2 72/r1 73/DET51/输入6/001B158身份保持。均为离线synthetic/metadata/dummy，真实目标执行未证明。

sealed DLL281600 bytes / SHA256 `20A7E5BEB203E29D5A691086A33AAC7641B6BD4B16C2FB8D8C88DE935A4A8446`；manifest304 / `187EAC0FDB0A65CC4CA2408092D872B17B7B8CEC44BFF598603635F6F0AF4681`。独立82文件索引SHA256 `DBF5CF55CAE072811E7C919B269D2A8C3ED50F98E79453E1DA67817CB43B7563`。owner/private均candidate-r3，schema2，旧revision保持。

Install-R3仅执行一次，prep `938852a077b14aaaa47113c13f94c673`：先game0/原scope、sealed82文件及exact live r2/workspace可恢复门，完整settings私有create-new备份；仅移除本包两份exact文件并create-new安装r3，其他18项目文件hash保持。设置仅按当前有效(id,source)关系保留/补disabled行和only DET001开关，其余字段canonical不变；modded selector1未写，native/Cloud/进度/FTUE/他账号/个人unmodded未触及。

独立`Verify-R3Preparation.ps1`及`r3-preparation-verified-938852a077b14aaaa47113c13f94c673.json`通过：30 installed manifests有效only DET001、modded1、备份/哈希/原scope正确、game0/Arm0。一次性Install-R3不得重放，只读核验可复用。实际loaded及本次Cloud尚待fresh人工启动核对。

下一请求仅确认原账号/Cloud关闭后fresh停主菜单。预检通过才发**一局**Standard Ironclad A0准备，可选六个精确允许项中任一；没有就停手，不再roll。不得提前Continue、新建或F8；A/B仍必须same r3/same新CP，不拼r2。真实资格/CP、目标CardModel scope、E0…E4/九步/五点四操作及A-B仍Unknown，正式预算仍最多3，STATE/PERF/产品未解锁。

## r3 fresh主菜单通过

用户明确确认原账号、启动前Cloud关闭、仅停主菜单保持运行且未做其他操作/无异常。process40320:639283404179499846，attempt2f3f488f9b31435c9468f330a8432cfc，actual仅DET001/candidate-r3/schema2及原账号modded1，first-tick seq1/670ms、ready-A-arm seq2/710ms；无arm/terminal/activation。公开precheck收据及两事件已归档，原始日志/私有路径未导出。

最初外部核验在process-events阶段中断且原因未记录，保持Unknown。添加仅扩展trap诊断的独立v2后，同一进程、同一全部标准只读复核通过，未改候选/配置/门或要求重启；见r3-main-menu-readback-note.md。原准备索引及脚本保持。

仅发request-08-r3-one-neow：单局Standard单人Ironclad A0，六个精确允许项任选其一，初始十卡，到Act1地图立即停；无允许项/无法辨认/异常停手报告，不重开，不提前F8或点节点。真实Neow资格仍待实际读取验证。

PM后续D-083独立验收为ACCEPT_R3_OFFLINE_AND_PREPARATION / CONTINUE_AUTHORIZED_RUNTIME，无返修或新增批准门。PM独立144/144、same-hash build及固定IL复现等以project-lead-candidate-r3-review-2026-10-23.md为据；不把此验收当目标生产callback/重洗PASS。现场主菜单已按上述后续收据通过，当前保持等待已提出的request-08用户回传，不重复request-07、安装或测试。

## 未Arm：Continue输入偏差已确认

用户报告选择轰鸣海螺到地图、原十卡、未点节点/F8。BeforeArm只读核对发现同进程terminal seq3/283788ms、unexpected-continue，未发F8；请求正常退出后机器game0，四份白名单事件/terminal已归档，Arm/seal/activation无，仍0/3。

固定IL证明新建入口走SetUpNewSingleplayer，Continue走SetUpSavedSingleplayer（探针精确prefix）；用户澄清实际**点了Continue**，因此分类PRE_ARM_INPUT_DEVIATION / EXPECTED_CONTINUE_GUARD。最初探针误报假设撤销，不改候选/门，不新建r4，不重复144。海螺实际资格仍未被Arm验证；本次无业务A/B，不称production故障。详见runtime/r3-continue-input-clarification.md及停止收据。

game0下配置检查发现正常化删除10个installed记录，only显式DET001/global true/selector1。按已授权范围create-new完整private备份，仅补disabled及窄核开关，其余字段canonical保持；exact r3 pair复用未写、18其他项目文件保持，selector未写，native/他账号/个人unmodded未读写。一次性Reconcile-R3PreparationAfterContinue已执行，prep12aa71ad59f74c24b991b04409cb8d8e，独立Verify-R3Preparation通过；不得重放mutation。

下一request-09仅fresh主菜单/原账号与当次Cloud确认。通过后仍按原定“单人游戏→新游戏”准备首次新建单局六选一，无允许项停报不roll；不会用Continue替代新建或清除旧终态。标准及预算不变。

## 输入偏差后的fresh主菜单通过

用户当次明确确认原账号、启动前Cloud关闭、只停主菜单保持运行，未Continue/新建/F8或其他输入，无异常。Verify-R3AMainMenu-v2使用新prep12aa71ad59f74c24b991b04409cb8d8e，一次通过：process41124:639283412764620936 / attempt635edc7caa37489f8a79bc15734fc0cf，exact r3/schema2、actual only DET001/modded1/原scope及首帧READY_A_ARM；arm/terminal/activation无。收据precheck-pass-635edc7caa37489f8a79bc15734fc0cf及两白名单事件已归档，raw log/native未导出或读取。

仅发request-10-r3-explicit-new-run：从单人游戏进入新局设置、Standard铁甲A0确认新建，不能用Continue；六项任选一个/初始十卡/到地图停手，无允许项停报不roll。正式0/3及其余门不变。

## 六选一未提供与最新用户有限roll授权

用户回报“打开后发现没有合适的遗物”。机器Capture-R3NoEligibleOption确认同process41124:639283412764620936 / attempt635edc7caa37489f8a79bc15734fc0cf、exact r3/初始事件保持、无terminal/Arm/seal/activation。准备不足为用户报告，实际选项名称Unknown；未验证现场Neow资格，非production失败。

按当时D-082先停报并请求正常退出。用户随后报告退出，并明确提出“不能让我直接roll吗？现在这么多遗物还是很好roll的”。机器game0/Arm0确认。按最新直接人类授权，已改为同一进程最多5局的一批准备：单人新建，不Continue；六项任一出现立即选择/保留十卡/到地图停手，若无则正常退主菜单后新建下一局，不用每局关闭游戏/回传；5局均无/认不准/异常停报。F8及所有业务gate不变。详见user-authorized-reroll-batch-2026-10-23.md，最新授权已通知PM，不为相同范围再等确认。

退出后的配置再次正常化为20记录/缺10个installed disabled行，global true/selector1/显式only DET001。新一次性Reconcile-R3BeforeAuthorizedBatch完整备份、仅窄补开关，prep30d7dfb5a50e49d4893b4b45b5aab2e2独立回读通过；exact pair未写/重装、18其他项目文件保持，native/他账号/个人unmodded未读写。候选/source/82索引不改、未重复144。

下一request-11仅fresh主菜单，当次原账号/Cloud确认和actual only DET001/modded1/首帧通过后再给整批动作。不因roll授权跳过启动gate、放开其他遗物或增加Arm预算。

## D-084有限批次已开始

PM已直接核对用户最新原话并将D-084写入权威handoff，覆盖旧单局/no-roll准备限制，无需重复批准。request-11用户回复“好了”（其上下文要求原账号、启动前Cloud关闭、仅主菜单）。机器一次通过新process31328:639283418247885573 / attempt1282373a69274578ac9ed62931ae031f：exact r3/schema2、actual only DET001/modded1/原scope、first-tick/READY_A_ARM，无terminal/Arm/activation，累计0/3；原始precheck及两事件已归档。

外部核验器仍输出旧one-run/no-reroll文字，不编辑旧脚本/收据；新增r3-batch-gate收据以D-084明确绑定该身份门并允许最多5局。仅发request-12整批动作，不逐局审批/关整个游戏或做配置会话；成功到图/五局无/认不准/异常再回传。F8/节点/四操作及资格/CP/RNG等门保持。

## 珍珠开局回传，但准备进程发生变化

用户回报已roll到珍珠、Act1地图、初始十卡、无节点/F8/异常。BeforeArm按原批次process门正确拒绝：当前是58312:639283459045976987 / af99c3b9c7bf411882d580c2f3565408，不能继续使用原31328/1282373...身份。未发F8，不认定为候选故障。

Inspect-R3PreparationProcessChange已窄只读重建当前r3/actual only DET001/modded1/原账号scope、当前first-tick/ready-A-arm及完整candidate/process/attempt绑定；无terminal/Arm/seal/claim，跨revision无activation，正式0/3。两事件及r3-process-reconstruction收据保留，未读native或改任何live输入。

新进程准备前外部主菜单门未执行，本次Cloud/原账号/重启和次数待用户补充。已请用户保持地图并补充；当前F8 HOLD。已将当前全新attempt/全新A-CP且不拼旧业务的具体恢复方案交PM判定，详见runtime/r3-preparation-process-change-hold.md。不让用户无据重复刷珍珠，亦不自行降低身份/业务标准。

PM已签发D-085条件式未Arm来源补验：可保留当前af99珍珠局，不要求退出重刷；当前不等于F8放行。等待已提出的用户澄清，本次账号/启动前Cloud、正常新建非Continue、无节点/F8/其他业务动作等关键条件满足后，再只读复核当前same process/无终态/Arm/CP/预算并create-new恢复收据，才请求F8。次数可约数/Unknown，若超过5如实记录偏差，不伪记≤5，不单独因此销毁未Arm fixture；不再继续roll。新A/CP/B不得引用旧进程业务，所有原门不变，无需改probe或重测144。裁定来源project-lead-pre-arm-process-recovery-2026-10-23.md，当前仍待用户信息。

## D-085来源补齐与当前before-Arm恢复通过

用户先报告疑似中途闪退后重启；对本次原账号/启动前Cloud和新建来源的必要补问，明确回答“对。当前是新建对局”，并推测Steam被挤下号。次数与旧闪退阶段Unknown，不再为次数重复追问；本次启动来源及此前无节点/F8/当前异常回传绑定在r3-restart-clarification收据。

有限旧日志核对先以原1282 precheck的84313-byte prefix SHA精确匹配归档，再读剩余320 bytes，确有D3D12 device lost in buffer_map / HRESULT0x887a0005 / DXGI_ERROR_DEVICE_REMOVED。仅公开renderer marker/码/hash，不搬raw/设备细节；根因Unknown，不能确认Steam挤号因果，亦不归因于probe业务。此为旧未Arm准备进程的渲染停止，当前新进程没有此已列fatal标记；不声称崩溃恢复已验证。

Verify-R3RecoveredCurrentSource再次确认same58312/af99、exact r3/actual onlyDET001/modded1/原scope、自身两启动事件、无terminal/Arm/seal/claim、所有revision无activation、正式0/3和本次关键用户来源。create-new r3-before-arm-recovery-af99...为RECOVERED_PRE_ARM_NEW_A_ALLOWED，明确外部门是准备后补齐、旧数据不复用、当前实际资格待原getter。按照D-085发request-13仅F8一次并停地图，READY_A_NODE及真实arm核对后才发唯一点击。无需改候选/重装或重跑144，不继续roll。

## F8回传后尚无记录，聚焦纠正

request-13用户报告一次F8、保持地图/无节点/无异常，但两次只读检查仍仅初始seq1/2，无arm/seal/terminal/activation；有界current log只有READY_A_ARM，无READY_A_NODE或已列fatal。当前hash绑定日志增量仅FocusIn/Out/后台帧率等，不证明按键本身到达；不能算资格失败或已Arm。

用户澄清同时玩其他游戏，“可能是没有聚焦”。记可能性，不硬判实际键未按或候选输入缺陷。最新same58312/af99、exact r3/原scope、初始事件hash及所有正式预算复核仍0/3。create-new r3-f8-focus-retry-gate，request-14仅切回STS2地图/聚焦后F8一次并保持前台停手；不点节点、不清记录或改门。后续资格及目标仍由原r3实际读取决定。

request-14后用户回传“好了”，机器仍仅初始两事件、无arm/seal/terminal。用户后续说明F8不需Fn、没有系统功能反应，推测可能碰到F9，并明确要求可以再试一次。再次same process/初始事件hash/无Arm及终态/预算0复核，create-new explicit-key-retry收据，request-15仅看准F8/聚焦后一下并停手。输入交付原因仍Unknown，不宣称候选已坏或资格已失败，不清除任何记录、改门或消耗虚构Arm。

## 最新：r3首次正式Arm及唯一节点通过

用户主动报告关闭另一游戏后又按F8。Capture-R3FirstArm只读核对同process58312:639283459045976987 / attemptaf99c3b9c7bf411882d580c2f3565408、exact sealed/live r3、原scope及actual onlyDET001/modded1，通过原r3真实Arm和READY_A_NODE。此次产生armed seq3/1771659ms、arm.json及唯一activation0001，跨revision累计正式**1/3**；无terminal/seal/claim。此前0/3和无Arm收据属于历史时点，不再代表当前状态。

arm SHA256 `47DB25765D1C51830A1B03F770DDDFED6C8D09E35CB6BA2ECDCE8F245E5747BE`。实际资格：shuffle FTUE=true，DebugTopAbsent=true，无modifiers，249金币，BurningBlood+GoldenPearl，原未升级5Strike/4Defend/1Bash；无附魔、Affliction、额外重放/耗尽标志或关键词。GoldenPearl完整Persistent含floor_added_to_deck=1及精确id，Counter/Flag均null。前驱hash70A3842AB76564B364EB35DBB97E53F5B073588B901131A92414D1323E55DE89，seed20FRCAL12Q8Y/act0/start row0col3/floor1；唯一目标为实际直接Children中的普通Monster row1col0。

白名单arm、armed事件及activation副本已create-new归档，详细收据r3-first-arm-af99c3b9c7bf411882d580c2f3565408.json。未导出private路径/原生存档/整日志，未改候选或设置、未控制游戏。关闭另一游戏和本次成功存在时间先后，输入交付根因仍Unknown，不作因果结论。

拟议r4输入观察/准备Continue分支不执行，保留现r3和当前fixture，沿既有业务门继续。request-16仅请用户点击起点虚线直接连接的第一排最左普通Monster一次并停手；CP/seal/S0、weak encounter及Defend分布尚待点击后的原记录，未证明A或重洗通过，不提前请求出牌。

## r3目标进房、CP封存及S0通过

用户确认只点击目标一次、保持聚焦且无其他业务输入/异常。same58312/af99下原匹配SaveRun成功之后出现production-save-start→seal-start→checkpoint-sealed→A-capture-start，seq4…11单调完整，封存CP45534 bytes / SHA256 `AD987823B7FBA7260542A6951DEB3594928EF717B0942E141B9956FB1AA80DD2`；private CP与当前原账号modded1 native双读raw均相同。CP roomNull=true/floor1/row1col0、playable floor2，完整Neow实例及十卡序列化绑定通过。原始private/native未公开复制。

S0 semantic SHA256 `78C840628A1AC54F7256B1EF303895658F0B938B17F330AF8AA666CAFE9BDAB1`。实际ENCOUNTER.SHRINKER_BEETLE_WEAK，R1/playerturn1，80/80HP、3能量、0格挡，手5/抽5/弃0/耗尽0，初始十卡及Arm资格未变、手牌和抽牌堆均有简单Defend。20 readiness字段全true、连续69932…69961共30帧、maxgap34ms；窗口2176886…2177853ms。CP/seal/S0与事件白名单已归档，详细收据r3-S0-gate-af99c3b9c7bf411882d580c2f3565408.json。

原READY_A_CARD sequence1/index0based1，手牌顺序Strike/Defend/Strike/Defend/Defend。独立readback核对完整选牌属性、handhash和fingerprint后，request-17只打从左第2张Defend一次并停手；不结束回合或追加牌。首个动作尚未发生，A/重洗/B未完成。

本次外部核对脚本首次因JsonNode不存在GetPropertyValue方法在导出前中断；改为索引器并显式枚举canonical对象后，同一输入及同一业务标准只读通过，未重放安装/配置或原游戏效果。此为readback工具修正，不分类为候选或production失败。

## 第1张Defend实际成功，S1通过

用户确认只打左起第2张Defend一次，无其他业务输入或异常。same58312/af99/r3下A.action-1记录sequence1/CardPlay/R1/playerturn1、BeforeHash精确S0、index1/完整handhash和fingerprint与S0 gate相同。事件seq12…21完整单调；action-accepted-success seq18/20033590ms保留Requested/Executed/CardEffectSeen/Finished=true、EffectTaskStatus=5(RanToCompletion)、Cancelled/Choice/ProductionExceptionObserved=false、Rejection=0，后续S1点已完成。未把用户点击或格挡变化单独当作原Task成功证据。

S1 semantic SHA256 `9BEF32909EB1C7154B183003BD49FAE191902A6C924703070A3A1CE51BA6032E`，R1/playerturn1/floor2/同弱甲虫，80HP、2能量、5格挡，手4/抽5/弃1/耗0。20字段全true、605829…605858共30连续帧、maxgap10ms；全部Arm资格未变，Run/Player/敌实例完整RNG均与S0相同。private CP和当前native双读仍精确AD9878…CP；无terminal、无第2动作或S2。正式仍1/3。

白名单A.action-1、S1 capture/semantic及截至seq21事件已归档，收据r3-S1-gate-af99c3b9c7bf411882d580c2f3565408.json。外部通用readback初次对JsonNode数组使用JSON解析的Int64索引得到null，已明确cast Int32；使用已公开S0复现/核对同fingerprint后按同门只读通过，未改候选或游戏状态。request-18仅正常点击结束回合一次，然后停手等待R2/S2，不追加动作；A/首次自然重洗/B仍未完成。

## 第1次结束回合实际成功，R2/S2通过

用户确认只结束回合一次，敌方行动/自动抽牌结束后停手，无其他输入或异常。Capture-R3AIntermediate -Point2一次只读通过：same58312/af99/exact r3，A.action-2为sequence2/EndTurn/Before1/R1/playerturn1/精确S1 hash；累计2个accepted-success，最新Requested/Executed/EndReadySeen/Finished=true，无cancel/choice/production exception/rejection。CardEffectSeen=false/EffectTaskStatus=null是EndTurn记录实际值，不冒称Defend效果Task记录。

S2 semantic SHA256 `36217D89C02E60726CC7864F11DE8247DE31DDD2409494E9C062384ADC3CBDA6`，同弱甲虫/R2/playerturn2/floor2，80HP/3能量/0格挡，手5/抽0/弃5/耗0，Arm资格保持。20字段true、612984…613013连续30帧、maxgap34ms；Run/Player/敌实例完整RNG相对S1均未变，不能将结束回合当作已消费Shuffle或已达到自然重洗。private/current native双读仍原CP，无terminal/第3动作/S3，正式1/3。

白名单action2/S2及截至seq34事件已归档，详细r3-S2-gate-af99c3b9c7bf411882d580c2f3565408.json。R2手牌Bash/Defend/Strike/Strike/Strike，原READY_A_CARD sequence3/index1；完整handhash985EACE13D737433EBDD1FDF6FF94D1604B4ABA52A6CFB31912EA3DAD345AF74，Defend fingerprint9CF13CCCB12D4C8E25C5B961B6BBED77DDBF07127A07DDFDA620AC55C2E4AFDC。request-19只打从左第2张Defend一次并停手，不结束回合；五点已3、四操作已2，首次自然重洗/A完成/B仍待证。

## 第2张Defend实际成功，S3通过

用户确认只打当前左起第2张Defend一次、保持聚焦，无其他操作或异常。Capture-R3AIntermediate -Point3一次只读通过：same58312/af99/exact r3，A.action-3为sequence3/CardPlay/Before2/R2/playerturn2、精确S2 hash；index1/handhash/fingerprint与S2下一locator一致。累计3次accepted-success，最新Requested/Executed/CardEffectSeen/Finished=true、原EffectTaskStatus5(RanToCompletion)，cancel/choice/production exception均false、rejection0。

S3 semantic SHA256 `4B203C5229C395214E228931416686E7A749E51C0F14536C875BB10A5A3E8668`，R2/playerturn2/floor2/同弱甲虫，80HP/2能量/5格挡，手4/抽0/弃6/耗0；Arm完整资格保持。615115…615144连续30帧，20字段true、maxgap10ms。Run/Player/敌实例完整RNG相对S2均未变；private/current native双读仍原CP。无terminal/第4动作/S4，正式1/3。

action3/S3及截至seq44事件白名单归档，r3-S3-gate-af99c3b9c7bf411882d580c2f3565408.json。原READY_A_ENDTURN sequence4已确认；request-20仅最后一次结束回合，然后停手等敌方与自动抽牌结束。该动作预期触发R3首次自然重洗，但真实CardModel非泛型callsite/E0…E4/完整九步/S4与A完成尚未发生，不能提前记通过。B及退出窗口仍待后续门。

## A实际完成：五点四操作、首次自然重洗完整九步通过

用户确认只做最后一次结束回合，敌方与自动抽牌结束后停手，无其他输入或异常。Capture-R3AComplete一次只读通过same58312/af99/exact r3/原scope，四动作完整合法序列及实际accepted/finished，五点R1/R1/R2/R2/R3且playerturn一致、全部20字段/30连续帧/maxgap≤250ms，完整Neow/十卡资格保持。正式累计1/3，无terminal/B.claim；当前private和native双读仍同CP AD987823B7FBA7260542A6951DEB3594928EF717B0942E141B9956FB1AA80DD2。

S4 semantic SHA256 `6EF4ECD4217E6C5CE72E4F5482495BFD369979F63E7E434DC8F169804BCE1276`，同弱甲虫/R3/playerturn3/floor2，78HP/3能量/0格挡，手5/抽5/弃0/耗0；618803…618832连续30帧，maxgap11ms。真实自然重洗E0(seq54/20427909ms)为Draw.Hand/NaturalShuffle、R3/playerturn3/floor2、draw0/discard10/hand0；E1/E2/E3成员、完整牌语义、序列/原owner和排序后输入、hook不干扰均通过原合同，E4(seq59/20429146ms)原Shuffle与Draw Task皆RanToCompletion。

实际Run.Shuffle counter **9→18**。E1完整状态(18065651617753005797,6792841934923828101,12905686910000899779,2506653915806142117)，E2完整状态(9470418008651068351,3779149685614934387,9778423686327157155,2810268027915766851)，与九步kernel逐位一致。E1绑定S3完整Shuffle entry，E2绑定S4；E3前5张精确等于S4手牌、后5张等于抽牌堆。此为当前受控真实CardModel作用域首次自然重洗证据，不能外推任意卡组/遗物/版本或多次稳定性。

A.shuffle hash `8957C3B0430C7647BE3C9AE992464F5882CDF58EE4A315B8EBADBA49C9CB0C23`；A.complete hash `85C1E4A8B7392A4994BA98666802BB3483F3C4E05828CA1E01AD319199EE217B`及A.ready-B的完整5 snapshot/5 capture/4 action/shuffle/complete hash链通过。seq1…63完整单调、trajectory-complete和A_COMPLETE_STOP_INPUT已观察。外部核对只加载sealed r3的detached DTO/Core/AttemptValidation纯方法，不调用Probe、NativeCheckpoint parser、engine/Harmony或生产效果，不重跑144项/重建候选。

白名单五点/四动作/E0…E4/shuffle/complete/ready及全部63事件均归档，详细r3-A-complete-gate-af99c3b9c7bf411882d580c2f3565408.json。request-21只请正常保存退出回主菜单再退出整个游戏；机器game0及原生/private精确CP、配置和fresh B门尚待后续核对。A退出与B/A-B等价当前仍Unknown，不提前记整包PASS。STATE/PERF/产品不推进。

## A正常退出及fresh B准备通过

用户明确确认正常保存退出→主菜单→退出游戏，无其他操作/异常、尚未重启。Capture-R3AExit核对game0、原scope、exact r3与87份冻结A记录、完整ReadyB/hash链及native/private CP双读保持45534 bytes / AD987823B7FBA7260542A6951DEB3594928EF717B0942E141B9956FB1AA80DD2；无terminal/B.claim，正式1/3。退出不改变本轮room-before源。公开r3-A-exit-af99c3b9c7bf411882d580c2f3565408.json只含白名单/哈希。

游戏将设置正常化为20记录，global true、modded selector1、显式仅DET001；有效installed30项中10个disabled记录缺失。按D-081 B阶段明确授权，Prepare-R3B仅执行一次，prep85fcd1acee6a42bebdf1f713e9f7ce5d：完整settings/path元数据私有create-new备份，现有行/其他字段canonical保持，只窄补disabled与only DET001；最终32条记录覆盖30项当前installed及2条原准备保留的disabled旧来源行，不猜固定总数。selector未写，18其他项目文件保持，exact r3 pair复用不重装/写入。修改前后再次核对87份A、native/private CP精确相同。

独立Verify-R3BPreparation -Preparation85fcd1acee6a42bebdf1f713e9f7ce5d一次只读通过，记录r3-B-preparation-verified同token：game0/原scope、完整备份、30有效installed关系only DET001/modded1、非mod及其他mod字段保持、候选与其他文件保持、A/CP/无terminal/无claim/预算1全部通过。新B核对器按真实已Arm1工作，不使用旧要求0 Arm的A准备核验；一次性Prepare-R3B不得重放。

request-22只请用户当次确认原测试账号/启动前Cloud关闭，手动fresh启动停modded1主菜单；不Continue/新建/F8或改设置。actual loaded、新进程、B.claim及READY_B_CONTINUE仍待核对，通过才另请Continue一次。B业务和A-B等价仍Unknown，不将prepared等同loaded。

## fresh B主菜单预检通过

用户当次明确确认原测试账号、启动前Cloud关闭、只停modded1主菜单，无Continue/新建/F8/profile或设置变化、无异常。Verify-R3BMainMenu一次只读通过：fresh B process81240:639283673562637803，不同于A58312:639283459045976987；same candidate-r3/attemptaf99、exact pair、actual only DET001/modded1，当前log token与原scope一致。

87份A原记录与公开冻结副本及完整A/ReadyB证明保持，private/current native双读仍原CP AD987823B7FBA7260542A6951DEB3594928EF717B0942E141B9956FB1AA80DD2。原MainMenu BInput后一次B.claim绑定新进程；first-tick seq1/556ms、ready-B-continue seq2/580ms和READY_B_CONTINUE，B仅这三份新记录，无terminal/point0，正式1/3。公开claim/两事件及r3-B-main-menu-af99c3b9c7bf411882d580c2f3565408.json已归档，无raw log/native/path导出。

request-23仅请用户正常Continue一次，随后完全停手保持前台；四操作由现probe正常逻辑API分派，用户不手动补动作。每点/边界首差异仍冻结停止自有后续分派；当前B尚未Continue，A-B等价Unknown。正常Continue的reload/save为预声明B写入窗口，此后不能使用仍要求current native等于A原CP的BSource/准备核验来判业务，private封存CP及A身份必须继续保持。

## B实际完成：五点及五内部边界全等

用户确认正常Continue一次后完全停手，保持聚焦，无手动业务输入/设置变化/异常，并观察到两次Defend和两次EndTurn自动执行。Capture-R3BComplete一次只读通过fresh B81240/同r3/attemptaf99/原scope；B主菜单来源、一次claim、87份冻结A及private原CP保持。未调用BSource或要求Continue后native仍等于A。

B四份dispatch与四份actual accepted action在完整序号、BeforeHash、Round/playerturn、唯一hand locator/指纹和目标上精确匹配A；四个实际accepted-success均Requested/Executed/Finished=true，无cancel/choice/exception/rejection，Defend原效果Task皆RanToCompletion，EndTurn的EndReadySeen=true。每次request在前一点stable capture之后，dispatch总数4，无第5条。B五点S0…S4 full semantic hashes逐点与A完全相同，compare-point-0…4均SEMANTIC_EQUIVALENT、Differences=[]；R1/R1/R2/R2/R3、playerturn/floor/完整资格及20字段30帧窗口均通过。

两端真实自然重洗各自E0…E4完整，原Shuffle/Draw Task皆RanToCompletion；分别从本端E1任意实际entry推导9步，E2完整四state/counter/digest均匹配，不仅比较counter。B本例亦9→18及与A相同完整state；本端10成员/完整牌语义、E2→E3顺序及S3/S4和最终手/抽堆链接通过。五边界去掉本地token和时间后的规范化内容逐项全等，完整NormalizedTrace亦相同。B.shuffle SHA256 `2096850DB0CD734A152B26D5A1A209D6B3F8EB9255954B2C703D34F1C9CBF891`。

B.complete为SEMANTIC_EQUIVALENT、ComparedPoints5/DispatchIntents4/AcceptedCompleted4/ObservedBoundaries5，全部5 snapshot/5 capture/4 action/shuffle hash链通过；59份B事件连续单调、trajectory-complete及B_COMPLETE_STOP_INPUT SEMANTIC_EQUIVALENT已观察。无terminal/stable/internal difference文件，正式1/3。90份B白名单（含4dispatch/5compare）全部归档；收据r3-B-complete-gate-af99c3b9c7bf411882d580c2f3565408.json。

正常Continue后native稳定双读45864 bytes / SHA256 `8ACC64483C20D284F37F78F518EBA3AEC7FD445791E9D283CA31242CA952379F`，不同于原CP45534 bytes，属于预声明reload/save窗口；不声称零原生写入，也未写回/restore/rebaseline。private原CP与A仍保持，Codex未写native/设置/候选或控制游戏。本结论限本固定版本/原测试modded1/GoldenPearl/原十卡/本弱甲虫/五点四操作的首次自然重洗样本，不外推其他配置、多次稳定性、隔离恢复或性能。

request-24只请求正常保存退出→主菜单→退出游戏。B退出、持久禁用/本包pair回收、离线最终索引和PM验收尚未完成；不因业务全等省略收尾，不安排额外游戏测试，STATE/PERF/产品仍未解锁。

## 最终B退出与离线收尾（2026-10-24）

用户确认B按正常游戏内流程退出、无额外操作/异常且未重启。Capture-R3BExit机器game0、178份白名单源/公开副本、原private CP及其scope绑定通过；native45864 bytes/hash8ACC64483C20D284F37F78F518EBA3AEC7FD445791E9D283CA31242CA952379F，双读稳定且与B结束观察相同。num_reloads原CP0、退出后1，声明窗口如实保留，不把native与A不同误判为重放差异。

Closeout-R3仅执行一次，token e8dd68e34a294cce9bfdde1e54084531。原准备完整private备份SHA42325CB3760063A83A9DAD3158A2FAE2C2F980B1DF235C291E6BE6263FB85D14证实原非probe flags全部false/global true；按原flags/global窄恢复，DET001及退役probe强制disabled，完整新backup留private，现有非mod/其他mod字段canonical保持、selector1不写。设置20→32行，只补disabled来源/退役行，未启用新mod。

配置成功后，默认Remove-Item在只读live文件处IOException。中断时两份exact文件均仍在、ReadOnly且可独占读取；原脚本与r3-closeout-interruption收据保留，不再运行Closeout-R3。Finish-R3Closeout只复核已disabled/backup/源数据后以精确LiteralPath/Force回收两个workspace可恢复文件，未重放settings变更。其他18项目文件与其余29 installed manifests保持；所有有效启用项0。scope/native/private/178份证据在收尾前后相同。

独立Verify-R3Closeout -Closeout e8dd68e34a294cce9bfdde1e54084531通过，收据r3-closeout-verified同token：game0、完整backup、原非probe flags恢复、其他字段保持、selector未写、persistently disabled、exact pair absent、18文件及178记录保持、native/CP不写回。最终settings SHA256 F37718E7C8EA506A9D9CD51531D7FAAC9384ABE3D2CCEABD5522F41E8A010D14。

Capture-LegacyPreservation公开workspace只读核对18份旧/准备索引，4657个索引目标与6份额外输入长度/hash全部保持；包括r3 82、r2 72、r1 73、DET Phase0 51、001B runtime158、A5 runtime405及旧phase/build证据。没有为未改候选重跑144或重建。最终Seal-RuntimeDelivery创建独立日期索引/摘要，公开Verify-RuntimeDelivery复核全部最终文件、旧索引、五点四操作/完整RNG及closeout绑定；私有scope的收尾证明和公开复核边界分开。

本报告及implementation-runtime-handoff-2026-10-24.md与runtime交付封存，交Project Lead最终验收。不再安排启动或游戏内动作，STATE/PERF/产品未推进。顶部最终结论取代历史阶段待办，原不可覆盖收据不改。

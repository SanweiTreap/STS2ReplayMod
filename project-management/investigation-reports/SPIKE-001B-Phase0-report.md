# SPIKE-001B Phase0 报告

日期：2026-10-22。状态：`PHASE0_COMPLETE / READY_FOR_PM_REVIEW / RUNTIME_HOLD`。

按新工作包完成独立 `SPIKE001B` 候选：捕获一张明确定位的普通 Defend 和一次 EndTurn 的 accepted 成功语义操作，在 fresh 正常 Continue B 只分派这两条操作，比较初始、出牌后、下一玩家回合三个稳定点。**离线完整链通过；001B 实际轨迹等价性尚未验证。** 未安装/启用、未读玩家状态/私人checkpoint、未写配置、未启动游戏或提出新人工测试请求。

## 1. 候选与环境

| 项 | 交付 |
| --- | --- |
| source | `experiments/SPIKE-001B/`，独立于已关闭 A5 |
| Mod ID / owner | SPIKE001B / STS2ReplayMod.SPIKE001B.TwoActions.phase0-r1 |
| private revision / schema | phase0-r1 / 1；追加式 activation journal，独立 attempt |
| sealed DLL | `experiments/SPIKE-001B/artifacts-phase0-r1/SPIKE001B.dll`，185856 bytes |
| DLL SHA256 | `D66F55B72C3A70D3AE897A478B62BD04ADC768A8F12D4A5987A0109E75286942` |
| manifest | `experiments/SPIKE-001B/artifacts-phase0-r1/SPIKE001B.json`，367 bytes，version0.1.0-phase0-r1 |
| manifest SHA256 | `94AFAD66D06A689DF862F7E675DBF4E817B040288A7882931452D7B8E1EF0932` |
| 固定游戏 | v0.111.0 / 41cef1ea，9757184 bytes / `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` |
| Harmony / 工具 | 2.4.2.0；已有 .NET SDK9.0.311/runtime9.0.13，不安装 SDK |

最终验证：`evidence/spike-001b/validation-phase0-r1-96898c45a1504f6a92ee90f39e3e8135/receipt.json`。交付文件长度与hash由 `evidence/spike-001b/phase0-delivery-2026-10-22.json` 统一索引；源文件/manifest hash亦在该验证收据中。

## 2. 核心实现

沿用 A5 before-room production 保存成功后封存、floor绑定、真实 ProcessFrame、20字段30连续Engine帧/250ms、独立deadline、有界诊断、完整语义投影和 Task 非干扰。Projection.cs、FieldReader.cs、NativeCheckpoint.cs、LiveRead capture/guard/build/reflection tail、ProductionObserver tail 只有 namespace 改名，delta audit 逐份核对。

新增 `Core/Trajectory.cs` 维护 Initial/PostCard/NextTurn 的 Round1/1/2与两操作顺序，不沿用固定Round1到下一回合，也不删除round检查。Action成功后才启动该点capture，不能因队列短暂为空推进。每个action20s、每个capture20s独立；人工Ready等待不计时。首终态、证据写失败、无tick/never-ready诊断沿用并补充Action blocker。

`ActionBridge.cs` 在正常 RequestEnqueue 前绑定当前完整前状态和唯一 Action；订阅 BeforeExecuted/BeforeCancelled/BeforePausedForPlayerChoice/AfterFinished。卡须对应 non-auto OnPlayWrapper 的原Task成功，完整Action Finished且Exception=null；EndTurn须scheduled turn匹配并观察实际未ready→ready，再完成Action。取消、拒绝、额外选择/故障不写accepted，重复通知不重记。新增观察器均Safe、void，不改生产Task/参数/结果。

选牌固定为普通未升级DefendIronclad，cost1、Block5且无额外属性，优先最左合格slot。记录完整有序手牌hash＋精确index/model/有效卡指纹＋Self:null，再由B取自己的live实例。多个同名牌可精确按slot区分；缺少完整locator、牌序/属性变化、目标不符、无合格牌均拒绝，不盲换牌。NetCombatCard和对象reference只用于当前phase的来源核对，不作为跨进程ID/事件payload。

B初始比较通过才保留第1条一次性dispatch reservation，再正常TryManualPlay(null)；post-card比较通过才保留第2条并RequestEnqueue(new EndPlayerTurnAction)。B自身accepted记录标B，不回录A。不直接改HP/牌堆/RNG，不额外setup/load/FromSerializable，无全局manager/store override、Replay UI、seek/纠偏/倍速/性能或多人扩展。

## 3. 生命周期与完整commit

每次Arm向独立 `activations/0001.json`…不可覆盖追加；最新attempt未完整、已terminal或已claim B时，下个进程可创建fresh A并保留旧attempt。无需删除active或改binary，用户中断不改写为production/harness失败。

`A.complete.json`绑定三个snapshot、三个capture、两条accepted的hash和candidate/attempt/process/checkpoint；最终deadline检查和`A.ready-B.json`成功proof写入位于同一终态锁内。只有两者存在、无terminal/B.claim，且全部内容/window/hash/phase/floor/round校验通过，才READY_B_CONTINUE。只有Arm、部分日志/快照或complete而无ready-proof均不能走B。B还校验exact private/native输入、fresh process；Continue prefix再验证恢复前房身份。A/B不能跨attempt或candidate拼接。

B逐点输出compare。初始不同不分派，post-card不同不继续EndTurn；早期差异会记录ComparedPoints/DispatchedOperations，不伪称三个点全部完成。只有三个规定点全部相同才完整SEMANTIC_EQUIVALENT；任何有效差异立即停手，不为追求等价重测。

## 4. 固定新API与写入边界

权威依据是最终验证目录内 hash-gated `actions-il.txt`、`completion-il.txt`、`native-boundary-il.txt`，解释见 `evidence/spike-001b/new-api-contract.md`。

- TryManualPlay=true只证明请求通过；PlayCardAction仍可不在Hand、取消或失败。accepted必须额外确认实际效果和Action完成。
- EndPlayerTurnAction scheduled turn不匹配时也返回CompletedTask，所以观察false→true ready转换，不能只等Task成功。
- 正常UI EndTurn同样构造EndPlayerTurnAction并入正常队列；PlayerCmd/本地actions disabled由生产Action自己处理，不由probe额外调用。
- 普通两操作链没有直接SaveRun；胜利EndCombatInternal会SaveRun(room,false)，排除胜利/领奖。A正常退出后native预期保留before-room，但仍必须exact-byte验证；变化就BLOCK_B，不restore。
- B正常Continue的IncrementNumReloads直接经RunSaveManager保存是已声明生产写入；输入门先完成，NumReloads不进语义投影。业务期额外SaveManager.SaveRun会终止观察且保持原生产返回，不宣称监控全部writer。

新增21引用均在白名单，总159（原A5 138引用全部保留）。唯一新增效果调用的3个callsite（含End构造）只存在B DispatchNext。7 targets/8 void callbacks，无transpiler/finalizer、生产字段setter或额外Task wrapper；timer只Poll和AnnounceTerminal，不读live状态。

## 5. 离线验证结果

| Gate | 结果 |
| --- | --- |
| Release | 0 warning/error |
| 合同 | **88/88** |
| 真实 Harmony patch/unpatch | **7/7**，不调用生产目标 |
| dummy非干扰 | PASS；save success/fault/cancel及观察异常；新增observer异常保留exact Task/参数/结果 |
| 新API/调用审计 | PASS；固定DLL、21引用、受限B dispatch、投影/Task保留、timer/patch范围 |
| A5保留 | runtime405/405、旧r1 24/24、r2 331/331、runtime-r1 build326/326长度/hash通过 |
| 实际001B runtime | **未执行** |

完整正向合同使用实际A5**公开**初始投影（8088bytes、hash DF34448C…）、固定schema生成的synthetic前驱/checkpoint和synthetic两后继。完整覆盖seal→初始→accepted Card→post-card→accepted End→round2→A完整commit/ready-proof→fresh B同输入→两次dispatch/三点compare及store回读。这不是实际游戏轨迹，也未读取A5 private raw fixture。

负向覆盖：未执行/效果未成功/取消/选择/重复请求或成功；请求后偷换同名Defend slot；locator缺失/越界、牌序/属性/目标变化、无合格牌/能量不足；未知类型/错误序号/phase/round/turn、B回录/未reservation/重复dispatch；初始或post-card单HP/卡属性/RNG state差异必须停止后续分派；checkpoint/候选/commithash/schema/revision/attempt/process输入不符；部分A、中断fresh A、B重复claim；无tick/never-ready/异常字段、gap/identity重置；独立deadline、慢commit、各writer失败及terminal/tick/commit race。expected由独立serialization建立，无共享mutable oracle。

## 6. 开发期修正与保留

初次action dummy在无engine的.NET进程里经Probe.Safe进入GD.Print native绑定，测试进程0xC0000005退出，未启动STS2。已在dummy设置synthetic Flow并屏蔽native marker sink，仍调用真实Safe；最终dummy通过，见 `development-dummy-finding.txt`。这属于离线测试隔离修正，未放宽生产成功标准。

首次全套87/87后复查发现ReadyB proof写失败发生于Flow已Complete之后，会缺少明确terminal；将proof发布纳入同一终态/最终deadline锁并新增区分性回归，最终88/88。旧87项验证目录和原candidate/受影响source保留于 `validation-phase0-r1-09f383f33dc7472e9037b4293035f8b4/before-ready-proof-fix/`；原开发日志/IL也保留，没有覆盖sealed A5。Phase0未消耗任何实际业务attempt。

## 7. 数据合同与未来人工方案

`evidence/spike-001b/data-contract.md`冻结owner/schema、路径、读写窗口、event/定位、phase/round、完整投影与排除项、private/public边界和JSON/文件/诊断上限。`evidence/spike-001b/manual-plan.md`是**未执行草案**：主菜单身份门→新A地图/F8唯一节点→等初始证据再请求唯一Defend slot→等post-card再EndTurn一次→Round2完整commit后正常退出→same native bytes门→fresh B主菜单核对→用户Continue一次后B自动两步逻辑分派→比较后正常退出。

用户手动启动/操作；Implementation不使用Computer Use、GUI或正式游戏headless。当前无有效enabled mods，下次项目测试必须先依同包新授权建立有效modded环境；本Phase0未改其配置或验证启动。不会要求处理另一账号，亦未读取个人unmodded1。

## 8. 复现与交回

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001B/Invoke-OfflineValidation.ps1'
```

该命令产生唯一验证目录，保存toolchain/Release/合同/patch/dummy/新IL/审计与receipt，不改旧记录。sealed pair可与重新构建hash比较。最终交付索引包含源码、脚本、候选、报告、合同、人工草案和开发/最终证据，逐文件核对长度/hash。

请Project Lead独立验收候选、accepted/完成语义、三点Round合同、A完整commit/fresh-A门和未来人工草案，再决定是否同包放行后续配置/安装/运行。已确认仅限固定API与离线合同可达性；实际两操作轨迹、其他seed/内容/版本、性能、独立会话隔离、完整Replay、seek、纠偏和crash recovery仍Unknown。当前不继续游戏实验，不维护或自行修改权威状态。

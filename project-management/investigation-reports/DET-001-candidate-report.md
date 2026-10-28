# DET-001 candidate-r1 离线候选报告

日期：2026-10-23（Asia/Shanghai）。Implementation 交 Project Lead 独立验收。

**结果：READY_OFFLINE_CANDIDATE_FOR_PROJECT_LEAD；RUNTIME_HOLD；正式 attempt=0。** D-079 授权的独立候选、资格修订、离线验证和交付完成。尚无实际 DET 自然重洗结果，未请求或启动新游戏。001B 已验收的旧运行结论没有重分类。

## 1. 输入与边界

依据 CURRENT_STATUS、DET-001 handoff 最新“已授权离线候选构建”、`evidence/det-001/project-lead-phase0-review-2026-10-23.md` 及用户关于 Neow 遗物的澄清。封存 Phase0 51文件/6公开输入、001B158文件、A5/更早 probe 不修改，不依赖旧 probe DLL，不复制 bin/obj 或旧证据树。

只在 `experiments/DET-001-candidate/` 与 `evidence/det-001/candidate-r1/` 新建候选/证据，并写本报告。当前账号、Steam/private/log/profile/recovery 未读取；设置、enabled、live mods 未写；未安装工具、启动 STS2/headless、调用引擎效果或请求用户游戏操作。PM 文档由 PM 维护，本轮未改 CURRENT_STATUS/TODO/DECISIONS/handoff。

## 2. 候选身份

| 项 | 值 |
| --- | --- |
| Mod / assembly | DET001 |
| manifest version | 0.1.0 |
| revision / owner | candidate-r1 / STS2ReplayMod.DET001.FirstReshuffle.candidate-r1 |
| 设计 private | user://DET001/candidate-r1/，schema1；不读旧001B schema2/DET离线envelope |
| DLL | 273408 bytes；SHA-256 B83F8C3140EFF3CD1532A5C97D2C8E9D3E2AADDCD44F862E456E084AD04DE0CA |
| manifest | 304 bytes；SHA-256 187EAC0FDB0A65CC4CA2408092D872B17B7B8CEC44BFF598603635F6F0AF4681 |
| sealed pair | experiments/DET-001-candidate/artifacts-candidate-r1/DET001.dll、DET001.json，只读 |
| 固定输入 | sts2.dll 9757184 bytes / 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9；v0.111.0 / 41cef1ea；Harmony2.4.2.0 |

最终 build/bin/test 加载身份与 sealed DLL 一致。源码起始13文件复制/身份转换单列 `source-origin.json`；后续变更仅新候选，最终 receipt 与小型索引绑定其完整源码。

## 3. Neow 可满足资格

旧“仅 BurningBlood”门撤销，新最小白名单**恰有 BurningBlood＋GoldenPearl**。固定 IL 确认 GoldenPearl 的 AfterObtained 仅取得150金币；其所有相关 draw/shuffle hooks 继承 AbstractModel 无效果/恒等实现。它在正常 Neow.PositiveOptions，可在被提供时从普通 UI 取得且保留初始十卡；CursedPearl 等分支可能排除它，不能承诺每次提供，未要求现在 roll。BurningBlood 仅在战斗胜利后治疗，受测窗口排除胜利/领奖。

Arm 前及稳定点实际读取 FTUE、relic IDs、run modifiers、完整 Deck、gold、debug forced-top；每点额外读取 PlayPile/PlayerTurn。不得以 synthetic basicPassives/FTUE 默认通过。普通5 Strike/4 Defend/1 Bash、未升级及无 enchantment/affliction/replay/exhaust-next/keywords；S0 两半至少各一个合格 Defend，初始 powers 空；S2 重验合格 Defend；E1 拒绝同键不同完整指纹。CP 序列化 Deck/Relics/Gold 同 Arm sidecar 对齐。资格不足为有限 `FIXTURE_NOT_ELIGIBLE`，不是 production 效果失败。

HP/gold 不硬编码。本离线 synthetic 输入的80/249只是 fixture 取值，另测HP79/gold399可接受。详见 `candidate-r1/neow-eligibility.md` 及 final-v3 的窄查 IL/继承元数据。

## 4. 有限实现及证据

五点 Round/PlayerTurn=1/1、1/1、2/2、2/2、3/3，动作=Defend、EndTurn、Defend、EndTurn，动作Round/Turn=1/1、1/1、2/2、2/2；不是仅改 marker。对应 Hand/Draw/Discard=5/5/0、4/5/1、5/0/5、4/0/6、5/5/0，Exhaust0。floor 始终 CP+1，room/seed/coord/角色/encounter、20字段/30相邻帧/gap≤250ms 和完整投影保留。四 accepted/completed 仍依原Action/原效果Task或实际EndReady，而不是意图。原 Projection/FieldReader/ActionObservation/ActionBridge/Patches 仅独立 namespace；旧唯一匹配 SaveRun wrapper 原样保留。

新增只读 E0…E4：目标 R3 正常五张手抽 scope→自然 Shuffle→同 List<CardModel>/Run.Shuffle 的同步排列前后→initial=false hook 后→两个原Task成功。E1实际counter/state0…3作为起点递推九步，对 E2 比较完整state、counter+9和一致digest；不固定9→18。还将E1与S3、E2/E3与S4 stream/成员/有序牌堆关联。局部token只检本端原成员，A/B剥离token/时间后比较完整语义和排列。

Flow 为边界分配严格递增事件序号，monotonic ms仅要求非递减，允许同步同毫秒。边界分别持久化原始DTO及规范化输入，最终完整ShuffleTrace纳入 Complete/ReadyB hash 链。B主菜单需全部五点四动作、资格、有效trace、same CP字节/candidate/fresh process，一次claim后才可 Continue。

稳定差异阻断下一自有分派；边界首差异不可覆盖，记录稳定覆盖/intent/accepted-completed及原Task最后状态，未返回=UnknownNotReturned。在途production继续，后续未观测的消费/完成明确Unknown，不取消、不跳过，不再等价commit。终态和下一自有分派由同一Flow锁串行。达到四操作硬上限、R3缺事件、任务/写失败、无资格等均不能补操作。

原Shuffle/Draw Task只在创建scope的game thread读取Status，不wrapper/await/continuation/GetResult，不改异常、参数或结果；timer只读冻结Flow DTO。scope禁止嵌套，终态后下个game frame清理。新增prefix/postfix不skip original，3个finalizer原样返回原异常。

## 5. 最终离线验证

最终凭据：`evidence/det-001/candidate-r1/validation-final-v3/receipt.json`。Release **0 warning / 0 error，68/68通过**；测试源码与DLL身份均绑定到receipt。

- 完整typed正向：实际公开001B S0的独立副本，把Stomp完整替换为普通Strike；构造兼容Neow/十卡sidecar、真实类型native序列化fixture，seal→5稳定点/4accepted→E0…E4→Complete/ReadyB→相同CP/freshB→逐点/边界比较。原含Stomp输入另列不合格反例，未冒充runtime。
- 完整RNG oracle：独立BigInteger mask/shift实现与候选ulong实现交叉；E1 counter46→55（从公开state先推进37步），已知state(1,2,3,4)单步向量；counter+9但state错误且digest自洽、额外消费、stale digest均拒绝。
- seq3/4 Round2、最终Round/Turn3、缺失FTUE/relic/投影、额外modifier/debug/PlayPile、不合格十卡/不同指纹排序平局、无重洗/初始化或错误owner/stream、成员替换、逆序/倒退事件、同毫秒合法事件、跨进程不同token、pending/fault/cancel/offthread均有案例。
- 五个稳定点逐一早差异停止下一分派；内部首差异冻结、Unknown状态和在途语义；四操作硬限、CP变化/fresh/candidate/Ready/完整数/hash链、CP资格绑定、连续帧/gap/期限、写失败/Ready失败/终态与commit及dispatch竞态均有检查。
- **真实11目标patch/unpatch metadata smoke通过**（包括closed StableShuffle<CardModel>），18 callbacks静态检查；真实Harmony同形dummy保留Task对象/参数、pending/fault/cancel状态及原同步异常，并验证共享引用泛型下无关T/owner隔离；实际3个finalizer也直接验证原异常引用不变。实际List<CardModel>类型guard可在无engine下测试；未执行真实生产CardModel effects。
- Cecil审计：相对sealed001B新增14个引用全为读取；无直接游戏字段写/游戏setter/额外效果调用。唯一效果仍为正常 TryManualPlay(null)、EndPlayerTurnAction构造、RequestEnqueue三处，位于B的串行dispatch closure。原完整投影、SaveRun wrapper保留，shuffle adapter无Task介入，timer whitelist通过。
- 旧DET Phase0索引hash `63E825150344678937B0525FD19B39B41DC6B553C1F34E7BC2CEAA878D6E8464`，51文件/6输入及绑定源码保持；旧001B索引hash `1605625D8DB968423EE5455B20A956EDD801023BF24378DA5B71F2E8A037BF9C`，158文件逐项保持。没有重编旧probe或重复旧109整套。

Synthetic E2排列使用独立反序fixture来测试成员/有序语义比较，**不是声称从该RNG得出的实际Fisher-Yates排列**；九步state oracle与跨A/B排列比较是两个独立门。development-v1/v2、validation-final-v1/v2和旧Neow查询保留，不作为最终源码/产物身份，避免覆盖开发过程。

## 6. 未证部分及交接

离线不能安全执行引擎依赖的CardModel生产路径，未强行调用。真实R3的Draw/Shuffle回调时机、fromHandDraw/实际阶段scope可达性、closed-generic生产callback、敌人造成的动态牌实例差异、完整目标两端结果均仍Unknown。这些属于PM验收后首次有限runtime门；metadata patch成功和dummy不替代它们。该诊断含I/O，没有性能结论，也没有产品/STATE/PERF推进。

交付：独立sealed pair、小型 `evidence/det-001/candidate-r1/delivery-2026-10-23.json`、修订资格/合同、未放行人工草案、验证与只读复现入口。报告请求PM验收离线候选及最小Neow白名单，再决定是否另放行同包配置/安装/人工A/B。本轮停在离线收尾。

```powershell
# 只读现有交付与旧公开证据
& ./experiments/DET-001-candidate/Verify-Delivery.ps1
# 新目录完整离线重现，保留所有既有结果
& ./experiments/DET-001-candidate/Invoke-OfflineValidation.ps1
```

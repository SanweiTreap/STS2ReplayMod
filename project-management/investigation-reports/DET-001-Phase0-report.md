# DET-001 Phase0：首次自然重洗与偏差检测

2026-10-23。结论 **READY_FIRST_RESHUFFLE_CONTRACT**。Phase0离线目标完成，正式attempt **0**，**RUNTIME_HOLD**。依据D-078及DET-001 handoff；001B继续CLOSED。候选构建/配置安装/人工运行等后续权限待PM同包放行。

## 已回答的问题

1. 普通Draw取draw首张，只有下一次抽牌请求看到draw空且discard非空，ShuffleIfNecessary才await教学检查及自然Shuffle。初始化PopulateCombatState→RandomizeOrderInternal走另一条UnstableShuffle(initial=true)；抽牌/换回合/新手牌本身不是重洗证明。
2. 自然Shuffle调用player.RunState.Rng.Shuffle。001B已有RunRng.Shuffle Counter/State0..3及digest，目标stream覆盖完整；缺的是精确调用来源、同owner和前后采样归因。10卡排列恰好9次NextInt，每次固定版本恰好一步Xoshiro kernel，因此应观察counter+9及完整9步state转移，不能只看seed/counter。
3. 实际输入为discard有序ToList＋draw HashSet内容；本目标draw为空，排除后者顺序风险。StableShuffle先按CardModel.CompareTo排序，再Fisher–Yates；比较键为ModelId(Category/Entry Ordinal)和upgrade，**不含完整有效属性/实例身份**。同键不同指纹可能受tie顺序影响，首fixture拒绝该类；原始有序输入/完整指纹/局部实例对应和RNG前状态仍记录。
4. 普通铁甲固定10张（5 Strike/4 Defend/1 Bash）、5抽且无额外效果时，R2取完余下5张，R3第一张触发首次自然重洗。数学最短2次EndTurn；建议有限流程在R1/R2各一张指定普通Defend再EndTurn：**4操作、2次EndTurn、到R3、5稳定点**。缺牌/FTUE/特殊passive/牌组变化等分类不合格，不靠十几回合盲等。
5. 稳定点Round/PlayerTurn为1/1、1/1、2/2、2/2、3/3，保持same CP/fresh B、floor=CP+1、位置/room/encounter、原20字段30相邻帧/gap≤250ms。另在自然Shuffle及同步StableShuffle内取有界瞬时前后证据，原Shuffle/Draw Task成功且R3最终稳定后才算目标完成。内边界不是30帧交互点。

固定证据及offset详见 `evidence/det-001/fixed-il-findings.md`，完整阶段/停止/分窗/非干扰合同见 `evidence/det-001/first-reshuffle-contract.md`。

## Confirmed / Inferred / Unknown

| 类别 | 结论和依据 |
| --- | --- |
| Confirmed，固定IL | draw→必要时重洗分支、Run.Shuffle owner、排序/排列及每调用counter/state关系、初始shuffle不同入口、教学/hook/debug边界 |
| Confirmed，公开样本 | 001B三点H/D/Discard分别5/5/0、4/5/1、5/0/5，Shuffle.Counter始终9；实际R2尚无下一次抽牌请求，不是已验证重洗 |
| Confirmed，旧投影/新离线输出 | 原投影覆盖目标RNG；三组实际同点相等及四类独立合法变更被比较器检出，损坏输入单独拒绝；六原fixture和旧158交付hash不变 |
| Inferred，有明确窄前提 | 满足普通10张/5抽/无额外影响的未来局在R3首次重洗，推荐4操作可到；玩家弱怪可生存经验支持场景选择，不保证任意敌人/起手安全 |
| Unknown，后续必须实证 | 当前测试槽是否符合牌组/FTUE/被动资格；closed-generic observer实际Harmony可达/Task非干扰；真实两端9步推进及五点轨迹是否等价 |
| 未调查 | 药水/印牌/复制/适应打击具体content ID与RNG路线，全部内容/其他版本/重复确定性、性能、纠偏/导航/正式Replay隔离 |

玩家约500小时经验按INVESTIGATIONS原来源保留。集合唯一决定顺序不是无条件结论：还需此前完整RNG状态、比较键/同键实例及hooks/debug条件。Shuffle和CombatCardGeneration具名getter确实取不同enum stream，但没有查具体印牌实现，不能把所有生成行为都归入后者。用户可roll特殊开局的能力当前未使用/未请求。

## 离线检测证据

复用已封存r2 `Projection.Compare/Thaw/Validate`，DLL SHA256 `F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7`；仅纯Core，不重建旧probe，不执行生产效果。DET测试envelope schema1显式包装原无schema头的semantic，wrong-schema测试拒绝这个envelope，不虚构旧semantic自带Schema。

最终运行 `evidence/det-001/validation-final-v3/`：Release **0 warning/error**，**11个检测案例＋15个synthetic流程案例**，另有准备资格、独立RNG向量、无alias及分派停止门。具体输出如下。

| 输入 | 判定 | 具体差异/拒绝 |
| --- | --- | --- |
| 实际001B A/B同点三组 | Equal | 0路径，原SHA256对应原7520/7519/7559-byte投影 |
| 独立副本HP80→79 | SemanticDifferent | `$.Player.Hp`，1路径 |
| 手中Strike/Defend完整对象换位 | SemanticDifferent | 两slot的Model/Persistent/Variables五字段各异，共10路径；先证明两个完整指纹不同 |
| Defend Block.Enchanted5→4 | SemanticDifferent | `$.Player.Hand[1].Variables[0].Enchanted`，1路径；仅schema-valid合成状态，不冒充生产可达效果 |
| Run.Shuffle合法单步counter/state推进＋重算digest | SemanticDifferent | `$.RunRng.Entries[8].Counter/State0..3`及`$.RunRng.Digest`，6路径 |
| 缺Hp/缺State2 | InputRejected | ProjectionMissingOrMalformed，不算合法差异 |
| DET envelope schema99 | InputRejected | EnvelopeSchemaOrProjection |
| counter改动但digest过期 | InputRejected | ProjectionContractOrDigest，不用解析拒绝冒充RNG差异检测 |

RNG mutant以独立标量Xoshiro递推生成（状态向量1,2,3,4→7,0,262146,211106232532992，输出11520），独立canonical/sha重算RngSet digest；未调用游戏Rng/MegaRandom。每个candidate/expected各自serialization round-trip，JSON节点共享0，复核候选未改字段及原六文件前后hash。

15个流程案例覆盖正常synthetic链、无自然事件/只有初始化、错stream/夹杂消费、错采样先后、错Round/PlayerTurn/floor、未稳定、20s deadline、缺原Draw Task完成、无推进、非空draw输入、前一点差异后停止。独立检查第4操作后停止、terminal停止。`StarterPreparation`从合法synthetic普通起手推导R3/2End/4操作/5点；原实际001B含Stomp被正确判不符合plain starter，另验证R2无Defend、FTUE、被动和同键不同完整指纹拒绝。

这些是检测与未来门合同测试，**没有真实DET重洗运行结果**。早期开发输出validation-v1、final-v1/v2保留，最终依据final-v3及其source hashes。

## 相对001B的最小增量与运行门

只设计有限5点/4操作表及新增有界shuffle scope/Task观察、资格sidecar；没有通用多回合框架。旧Trajectory/Actions/Contract.Playable/LiveRead有Round1/2和2动作限制，后续独立DET实现须同步改成明确的1/1/2/2/3表，保留原floor/readiness/投影。原Run.Shuffle语义字段够用；补的是归因和资格，不新造RNG真值或改live state。

原production任务/参数/结果不改；新增shuffle/Draw Task仅game-thread只读成功状态、不加wrapper/continuation。同步排列前后观察不改变排序或原list，scope须同房间/owner/自然调用，timer只读冻结DTO。后续必须对实际closed-generic patch及清理/竞态做相关dummy/非干扰验证，不能由本离线合同代替。

B稳定点前差异阻止下一条自有分派。内部E1等差异发生时已经在途production不能被observer取消；分别报告稳定点/内部边界/intent/accepted/完成计数，不谎称未消费。完整PASS要两端五点及内边界相等、四accepted/完成、唯一自然重洗和完整9步推进；缺事件/错误归因/不收束是未验证而非PASS，合法差异是有效答案，不追PASS补跑。

沿用same CP、不可覆盖commit/ReadyB、A正常退出后同字节、fresh B claim/正常Continue及预声明reload窗口，不restore/rebaseline。实际动作、人手停手/退出和各阶段deadline见合同；这是草案，当前不请求任何人工游戏步骤。

## 权限、保留与交回

仅读固定程序集、导航缓存与公开workspace，写新DET evidence/离线工具。未读当前账号/private/log/profile/recovery；未配置settings/enabled/live mods，未启动STS2/正式headless/GUI或请求roll。未实现runtime mod DLL/manifest，普通Det001Offline控制台不是游戏探针。

最终验证重算旧001B交付158/158目标及索引原hash `1605625D8DB968423EE5455B20A956EDD801023BF24378DA5B71F2E8A037BF9C`；六个公开原始fixture均保持。未改或重跑已封存001B/A5，也未重复109/109等无变化测试。

复现入口 `experiments/DET-001/Invoke-OfflineValidation.ps1`，说明与窄IL重取命令见 `evidence/det-001/README.md`。小型身份索引 `evidence/det-001/phase0-delivery-2026-10-23.json` 记录源码/必要IL/最终输出/报告及只读输入身份，不收录bin/obj或复制整旧工程。结论交PM独立验收；后续候选/安装/运行在同包另放行。正式attempt0，RUNTIME_HOLD，不自动进入STATE/PERF/产品。

# MVP 技术实验顺序

更新日期：2026-10-26
状态：Approved sequencing；不代表最终架构

## 原则

- 先证明最小链路，再扩大场景和 UI。
- 每轮围绕一个可观察能力或关键问题，允许相关实现成批推进；不为每个未知先建INV，不无边界扩大整套产品。
- 所有结果固定到 STS2 public-beta `v0.111.0` 的精确构建，并区分观察、推断和未知。
- RunReplays 只提供候选 hook、命令语义和失败反例，不作为默认代码基底。
- 性能、确定性、恢复与导航是共同约束；不能分别优化后假定组合仍成立。

## 当前执行点（2026-10-26）

最新D-104：STATE-001首Defend后1点已真实落盘，并在不同进程2次零前缀恢复、三次Energy/Block偏差后重新从文件纠偏/校验，原Play继续和Step暂停保持，后续胜利/重开/退出通过。r2/r3失败保留，任务固定样本收束；当前无新包，更多状态点/多回合/生成死亡/在途异常和发布性能未验证。

以下为前序证据摘录，当前以D-104收束/待讨论下一范围、权限以D-092为准，历史合同不重新执行、不追认结果。

最新D-087：DET r3 A真实五点/四操作及首次自然重洗已独立验收，Run.Shuffle9→18/完整四state九步一致；87公开记录/63事件/hash链通过，Arm1/3。当前缺口转为退出后same CP/fresh B及两端五点/重洗比较，A/B尚Unknown，不能由A采样通过认定确定性。旧准备与r4草案不再为当前执行点。

SPIKE-001A5于2026-10-22首次通过实际room-before A/B control：固定样本首个稳定操作点的冻结投影等价，Differences=0；PM独立核对原始事件/快照、same input/fresh B/commit、405交付文件及收尾。A5已关闭，原先诊断与重建前置阻塞解除。

001B已完成实际Defend＋EndTurn的两操作捕获/分派和三点Round1/1/2比较，PM验收`ACCEPT_TWO_ACTION_TRAJECTORY_CONTROL`；唯一Arm1/3，不再运行。158交付/92原始记录、两端各2条accepted/完成、完整same CP/fresh B/hash链及收尾通过。普通Continue只是实验control，不是产品架构；不由一次成功推出一般重复稳定性。A5/001B均已关闭，另一账号/个人unmodded1排除；INV-013退役，正式headless仍未验证。

001B三个稳定点的RunRng/PlayerRng/当前敌人Rng投影均未变，尚无该样本消费推进这些RNG的证据。DET-001为此选择首次自然重洗，离线机制/检测及候选已完成；D-081放行原账号/modded1的精确配置安装及分段人工A/B，补真实R3 scope/回调、五点四操作和完整九步state证据。初次一局/资格不足先报PM，最多3条已Arm；实际结果仍Unknown。恢复留Wave3，PERF另设无诊断污染对照，不能把001B的诊断I/O/人工等待直接当产品预算。

最新准备调整D-082：用户多次未获得GoldenPearl后要求考虑另五种涅奥选项，现停止继续roll，先静态核查兼容子集并构建独立r3；r2实际越过地图故障到达Neow是局部证据，仍无目标洗牌/A-B。实验假设/九步state/五点四操作与Arm预算不变，不新增INV或产品分支。

D-083已接受r3六项限定资格、schema2实际实例/CP绑定及配置安装收据；PM独立144/144、same-hash build、三份IL/metadata和旧身份复核通过。下一步骤为fresh主菜单现场门及单局分段A/B，不再刷金珍珠，不由此认定真实目标重洗通过。

2026-10-23 DET-001 Phase0与candidate-r1已独立验收：自然重洗/Run.Shuffle覆盖和十卡9步明确，11检测/15流程及候选68/68通过；73文件/32源及11 metadata patch/18回调/dummy证据接受。白名单修为BurningBlood＋GoldenPearl，保留十卡，不保证Neow每局提供；当前不roll。集合不无条件决定顺序，印牌/复制不扩查。下一运行需另签原账号配置/安装、资格与预算；实际CardModel回调/完整重洗仍Unknown，不由离线结果替代。

## Wave 0：定向参考地图

对应：`INV-001`

只定位最小探针所需的候选入口：单人 run 生命周期、玩家语义操作、RNG/初始状态、结构节点、状态保存/恢复、UI readiness，以及已知热路径 I/O。记录 v0.111 API/UI break；不复原 RunReplays 全部行为，不切换 v0.107 重演历史细节。

退出条件：每类入口都有具体文件/符号、适用版本和“候选/反例/未知”标记；可以据此编写边界清楚的最小探针任务。

## Wave 1：v0.111 最小录制—重放探针

对应：`SPIKE-001A`、`INV-002`、`INV-003`、`SPIKE-INV003A`、`INV-004`、`SPIKE-INV004A`、`SPIKE-INV004B`、`INV-005`、`SPIKE-INV005A`、`INV-006`、`SPIKE-INV006A`、`SPIKE-INV006B`、`INV-007`、`SPIKE-INV007A`/`R2`/`R3`、`INV-008`、`SPIKE-INV008A`/`R2`/`R3`/`R4`/`R5`/`R6`、`SPIKE-001A2-R2`、`INV-010`、`SPIKE-001A3`、`INV-011`、`SPIKE-001A4`、`INV-012`、`SPIKE-INV012A`、`SPIKE-001A5`、`SPIKE-001B`；INV-013为未执行退役历史，不是当前步骤。

先由 `SPIKE-001A` 单独验证原生 `SerializableRun` 能否在首回合输入就绪点独立恢复出一致的结构位置、战斗摘要与完整 RNG digest。001A 已得到有价值的失败：测试链会改写普通 save，并重建出不同的手牌/牌堆和部分 RNG 状态。只读 `INV-002` 随后确认中途 combat serialization 是薄描述而非快照，并将房间前 `PreFinishedRoom=null` checkpoint 识别为有原生语义支持的重建候选。

2026-09-04 产品要求进一步明确：最终 Replay 必须从主菜单独立入口进入，不切换玩家profile、不占用或覆盖普通Continue。后续逐步建立了in-memory transaction、live-seeded candidate、安全startup gate与direct-method最小契约。007A R3完成direct-write验证；008A R6完成setup/post-launch controlled owner fault及各自restart verification，通过Wave 1当前受控隔离门。001A2-R2因readiness不可观测无A/B；INV-010给出production save边界、heartbeat、逐字段telemetry、独立deadline及30连续帧契约。SPIKE-001A3首次动态命中matching room-before save，但unknown floor sentinel提前校验且observer异常逃逸，仍无baseline/A/B。INV-011证明checkpoint floor等于arm predecessor floor，而A/B playable floor为其+1，并冻结typed target与observer non-interference。SPIKE-001A4首次封存正式baseline/recovery，但在A capture前停滞：post-baseline deadline被harness提前取消，退出protection另有未归因内容变化；仍无有效A/B。INV-012固定IL确认存储/退出writer合同与deadline缺口，建议先做窄诊断。`SPIKE-INV012A` Phase 0-r6离线合同已验收；预检所见selector=profile3、AscensionUnlocker启用与r6固定身份门不符。2026-09-28用户明确只有**不加载模组的profile1**是必须保护的个人连胜槽，modded profile1/2/3均可用于实验且模组开关可调整。因此未执行的`INV-013`配置安全调查退役，不再为避免modded3接触而研究离线编辑。优先按`PREP-INV012A-R6`做简短人工环境准备和只读复核，保留R6原候选；准备通过后再独立审核安装、人工诊断及后续A/B control。旧probe/attempt不追溯重判。强制crash继续延期，RitsuLib不进入核心实验。

退出检查（2026-10-23）：A5房间前重建control、001B实际accepted操作捕获/分派、收束与三点比较均已独立验收，最小正向控制链成立，可据此规划Wave2。这个窄退出门不等于任意场景或多次重复稳定性通过；后者由DET矩阵验证。隔离局部PASS、重建control和操作链路分别成立，不能外推完整产品session isolation。历史任务只保留证据，不再执行；离线/headless不替代正式STS2验证。

## Wave 2：性能基线与最小确定性

对应：`PERF-001`、`DET-001`

在同一探针上分别测量空闲、地图操作和卡牌结算的平均开销、分位数/最长 hitch、分配与写盘；同时扩大到少量具有 RNG 和选择界面的场景并主动注入偏差。

依赖：Wave1窄正向链已验收，各任务仍须独立合同和授权。两项可共享fixture/测量设施，报告分开；本轮先收紧DET最小合同，不一次并行扩大所有场景。

退出条件：得到可审计性能基线、主要热点证据、最小确定性边界和 divergence 检测证据。随后由 Project Lead 与项目发起人确认量化发布预算。

## Wave 3：强记录与恢复密度

对应：`STATE-001`

比较状态指纹、结构节点/决策点检查点、完整状态和逐操作增量等组合；验证恢复、再次校验、继续播放、录制开销、文件增长及 schema/content 缺失时的失败边界。

依赖：Wave 2 已能稳定制造并检测偏差。

退出条件：推荐最小权威状态集合与检查点候选密度；明确其性能与文件成本，以及不可恢复类别。

## Wave 4：双向导航

对应：`NAV-001`

在 Wave 3 的候选状态方案上比较从头快速重演与检查点恢复后重演，覆盖房间内逐操作后退、结构节点跳转和跨房间路线定位。

依赖：STATE-001 给出可恢复的候选方案；不得在此前先锁定最终时间轴数据结构。

退出条件：三个产品导航层级均有正确性与延迟证据，或明确指出无法满足的层级并返回产品复审；同时给出对录制性能和文件增长的影响。

## Wave 5：时序体验与可移植文件

对应：`TIME-001`、`PORT-001`

TIME-001 验证分类 config、原始节奏、全局倍率和 UI readiness；PORT-001 基于前面稳定下来的最小 envelope 验证同环境跨机器导入、安全反例与隐私字段。

依赖：时间实验需要稳定语义操作流；导入实验需要不再频繁变化的最小元数据/envelope。两项之间没有直接依赖，可以并行验证。

退出条件：播放节奏语义可实现且开销可接受；可移植 fixture 在匹配环境通过，恶意/损坏/不匹配输入被安全拒绝。

## 进入 MVP 方案设计前的总审查

只有以下条件同时成立，才从技术实验进入具体 MVP 方案与竖切拆分：

- v0.111 自有最小链路可重复。
- 性能预算已量化并获接受。
- 强记录、纠偏和不可恢复边界有证据。
- 三层双向导航达到已批准延迟，或产品范围经过显式复审。
- 时序数据模型和安全导入 envelope 有实验结论。
- 所有仍未知事项已转成受控风险或明确任务，未被写成架构事实。

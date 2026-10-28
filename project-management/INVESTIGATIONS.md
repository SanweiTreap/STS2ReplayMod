# 技术调查台账

当前权限提示：2026-10-26 D-092取代旧仅离线HOLD及个人槽禁读规则，改为备份后可恢复实验、直接源码/hook实现与同任务迭代。下列报告保留当时范围和结果，不把历史权限当当前限制；现状见CURRENT_STATUS，日常不要求逐方法IL复证。

## STATE-001首点文件恢复与纠偏样本通过

2026-10-27 D-104：正常首Defend后1点真实落盘，在不同录制/观看进程2次零前缀到达，5次独立文件应用/读回、三次Energy/Block真实差异和纠偏、后续胜利/退出通过。不同初始引用形状下用35语义锚点绑定400对象成功，整体拓扑一致不是必要条件；字段/引用/容器/RNG读回仍保留。旧r2/r3失败不追认。仅首回合单点/稳定上下文/不同录制进程，不认所有点或原生卡死救援。[验收](../evidence/state-001/project-lead-review-2026-10-27.md)。

## DEMO-005组合录播与恢复样本通过

2026-10-27 D-102：实际升级非初始牌/能力、计数遗物自动触发与用药/历史形成17步胜利文件；修复退场显示后36 forward/18 restore及槽位/计数/能量/显示匹配，完整胜利Stop与新Runtime重开短测的原context保持。旧r1两次失败及局部升级抽牌证据保留，不追认全部观看通过。只认本组合/所测边界，After投影/观看期memento仍非磁盘强记录；下一优先建议最小持久化权威恢复/纠偏闭环，未派包。[验收](../evidence/demo-005/project-lead-review-2026-10-27.md)。

## DEMO-002直接后退样本通过

最新技术补充（2026-10-27）：[原生动画/取消/抽牌时序风险](technical-notes/NATIVE-ANIMATION-TIMING.md)按用户经验、官方历史修复、本地固定代码和未验证推论分层。影响后续权威记录/因果顺序设计，不是新INV或DEMO-005前置门；当前状态仍以CURRENT_STATUS为准。

2026-10-26 D-097：DEMO-003完整首战胜利自动封存/回放及缩放/手牌显示修复按固定样本收束；最终47次forward/20次直接恢复与呈现核对、3次完整胜利退出，原比较context保持。群怪/召唤、生成/死亡逆恢复未验证，不从逻辑匹配推出任意视觉完备。旧Sludge Spinner未封存失败保留。验收 `evidence/demo-003/project-lead-review-2026-10-26.md`；当前Implementation已停，下一目标待用户讨论。

2026-10-26 D-096：观看期IdentityMemento/DirectCheckpoint在当前12操作样本中30次恢复、38次forward全匹配，含跨EndTurn重做、到12后退/退0续播；每Runtime仅1次room-load，0前缀，用户确认符合预期。记录12–52ms仅该路径计时，死亡/成员变化/任意内容充分性未证。保留当前live turn loop/信号而非恢复旧Task，选择性数据图不等于全游戏快照。验收 `evidence/demo-002/direct-r1/project-lead-review-2026-10-26.md`；该后续DEMO-003现已按D-097收束。

## DEMO-002后退基线与体验缺口

2026-10-26 D-095：真实12操作SEAPUNK片段有4次重建式后退目标/RNG匹配，11 forward/10 prefix核对，最终7/12中途Stop恢复所比较context。未播完、未跨进程/重开、8→7后未重做EndTurn。用户要求直接回上一状态而非重置后可见重演，约1.8–5.2秒路径仅算过渡技术基线。PM验收 `evidence/demo-002/project-lead-review-2026-10-26.md`；当前同任务优先播放期稳定点缓存/真实恢复，投影不等于完整快照，不宣称直接后退已实现。

## DEMO-001首个闭环

2026-10-26 D-094接受首个单房间核心闭环。真实Strike→Defend→EndTurn文件，从主菜单两次同会话加载，初态＋各步完整投影/RNG匹配，退出原manager及比较的Continue/进度保持；用户总体确认控件体验。未独立验证跨进程重开或实际中途Stop，不追认。源码 `src/ReplayMod/` 和当前已测安装保留。验收 `evidence/demo-001/project-lead-review-2026-10-26.md`；下一步最小后退，新对话接续而非新调查平台。

## PERF真实短pilot关闭

2026-10-26 D-093接受三模式最小真实路径：同build-006/同Custom场景四资格点，实际事件与原Task链完整，C2五条1636 bytes内容/顺序/写完通过。PM复算分段原帧及已计时阶段，确认收尾pair不存在、当前游戏0；备份/配置收尾依范围收据，不重做所有审计。首次阶段约4.97ms、后续约0.033–0.063ms仅线索，不认稳定增量/持续I/O或G-02。验收 `evidence/perf-001/candidate-r1/project-lead-pilot-review-2026-10-26.md`；下一步直接DEMO-001。

## PERF-001离线测量准备

2026-10-26 D-091已验收Phase0：64项/5公开输入、18块/9000正式样本/6文件的payload/order/bytes/hash与drain通过；PM另外重算9块统计，11源码绑定及5方法349行IL一致。23功能通过按原证据/源码核对，PM不重新build/计时/读玩家数据。早期104ms和后续38–42ms不认稳态；G-02/live采集/真实游戏长尾和持续写盘仍Unknown。下一步只准独立最小候选离线构建，runtime HOLD。验收 `evidence/perf-001/project-lead-phase0-review-2026-10-26.md`。

2026-10-26 D-090，用户恢复工作，签发 `handoffs/PERF-001.md`。目标是将共同测量设施、hook、采集/序列化与写盘成本区分，先用公开DTO代理验证统计及管线完整性，再给出最小idle/map/card人工pilot草案。代理不是游戏性能或最终录制架构；正式FPS/长尾、live采集和发布预算仍Unknown。当前不读取玩家数据，不构建/安装游戏候选或运行游戏。

## DET-001最终验收范围

2026-10-24 D-089。一个GoldenPearl/普通铁甲十卡/弱甲虫样本的真实首次自然重洗A-B、完整九步RNG及五点四操作通过，最终331文件/18旧索引及安装收尾已验收关闭；见`evidence/det-001/project-lead-runtime-review-2026-10-24.md`。178原记录/私有CP保留、game0/持久禁用/exact pair回收，旧失败/准备证据不改。

其他五项Neow只离线资格，其他内容/版本、多次稳定性、未投影状态、恢复纠偏、性能、seek、独立Replay会话/崩溃恢复仍未验证。D-089本身未授权下一实现/运行；当前PERF离线范围以D-090为准，不将probe诊断输出当发布预算。

## DET-001真实首次自然重洗A/B等价

2026-10-24 D-088复核。同r3/af99、原账号modded1/only DET001、GoldenPearl＋BB/普通铁甲十卡/SHRINKER_BEETLE_WEAK，A人工四操作与fresh B自动四操作五点full semantic逐字节和结构相同；两端E0…E4规范化全等、Run.Shuffle完整九步state/counter/digest及成员/顺序/最终牌堆链接成立。PM独立重验A87/B90公开文件、63/59事件、各四条实际成功/四B分派和完整hash链，接受单样本RNG重放等价。

这补上了001B实际未消费已投影RNG的缺口；并非所有随机系统/所有遗物/跨版本或多次稳定性证明。正常Continue有声明的reload/native写入，普通Continue仍是测试control，不是产品隔离方案；真实divergence后的恢复/纠偏、seek、性能未验证。仅收尾待完成，不追加运行。验收`evidence/det-001/project-lead-B-complete-review-2026-10-24.md`和独立JSON/只读复现同目录。

## DET-001实际首次自然重洗A端证据

2026-10-23 D-087。真实r3样本af99/seed20FRCAL12Q8Y/GoldenPearl＋BB/SHRINKER_BEETLE_WEAK，经Defend/EndTurn/Defend/EndTurn达到R3；五稳定点及四条实际accepted/completed、E0…E4和原Shuffle/Draw Tasks成功。Run.Shuffle9→18，PM独立BigInteger九步得到完整四state并与E2逐位匹配；成员/hook顺序、S3/E1、E2/S4及最终手/抽堆链接成立，87公开文件/63事件与提交链核对通过。

这是实际CardModel非泛型callsite/自然重洗窗口的首个受控证据，不再仅为静态推导或synthetic；但只验A采样，A退出后CP/fresh B和两端确定性待证。不能由一个样本外推所有随机系统、跨版本、纠偏、seek或一般重复稳定性。证据`evidence/det-001/project-lead-A-complete-review-2026-10-23.md`及独立校验同目录；用户经验的集合唯一决定排列等假设仍不无条件成立。

## DET-001涅奥准备条件修订

2026-10-23。用户在原Implementation对话先允许有限重开找金珍珠，随后表示多次未出，要求考虑华美发束、轰鸣海螺、熔岩石、飞鞋、钓鱼竿。D-082停止刷金珍珠并逐项核查；D-083已接受限定直接普通弱怪/R3停手/不胜负领奖窗口内的五个新增ID，加原GoldenPearl共六项。精确效果/条件见`evidence/det-001/candidate-r3/neow-eligibility-matrix.md`，不能外推一般对局无影响。

Implementation定位r1共享泛型补丁故障，r2改非泛型唯一callsite后实际到达Neow。r3继承该绑定，新增5只读引用、schema2实际Neow状态/完整序列化实例及CP逐点绑定；PM独立144/144、same-hash build、三份固定IL逐字节和metadata结构复核通过，旧身份保持。修复/Neow到达及离线资格通过不等于真实自然重洗/A-B，目标RNG仍Unknown。用户消息与范围见`evidence/det-001/project-lead-neow-scope-revision-2026-10-23.md`；本次验收及独立输出见`evidence/det-001/project-lead-candidate-r3-review-2026-10-23.md`。

## 玩家经验与DET-001洗牌候选

日期：2026-10-23。来源：项目发起人本轮直接陈述，依据其约500小时游戏经验；不是本轮机器实验或固定DLL机制证明。用于v0.111.0下一实验的候选选择，卡牌/药水名称沿用用户称呼，尚未核对content ID。

玩家观察：

- 战斗中常见随机行为包括印牌/随机生成卡牌和洗牌；印牌可来自药水或卡牌效果，例如用户所举大奖、类星体。
- 第一幕弱怪压力低，按用户经验只使用防御可以维持十几回合，适合等待自然重洗牌。这是场景可准备性判断，不是对任意敌人/任意起手的生存保证。
- 用户观察使用不同的适应打击会有差异，尽管表面都是0费攻击牌；具体条件/差异字段未描述，不把同显示属性当作卡实例完全等价。
- 若后续需要，用户可协助准备药瓶皮套及特定药水，或借助roll种器找到印牌等开局；当前不需要执行这些准备。

待验证假设：

- 用户推测洗牌后顺序只取决于弃牌堆集合。此处尚未说明“集合”是成员及数量还是包含原先顺序，也未声明是否固定seed/此前RNG进度，不能直接视为确定性契约。
- 用户推测随机生成卡牌与洗牌使用不同RNG；具体stream/owner、共享或派生关系仍待固定DLL确认。
- 复制自身是否消耗RNG未知。适应打击实例差异可能涉及卡身份/来源或其他状态；目前不将其归因于RNG，也不排除RNG。

PM决策：DET-001首选弱怪战中的首次自然重洗牌，优先普通铁甲起始配置及已验Defend/EndTurn语义。先确认触发链、RNG来源和可观测推进，再制定最短有界人工流程；药水/印牌为后续独立候选，复制/实例身份另作风险记录。下一包不穷尽随机系统，也不要求用户现在roll开局。执行见`handoffs/DET-001.md`，当前只授权离线准备。

后续澄清（同日）：用户明确“涅奥选项自身就是一个遗物，所以不拿遗物是不可能的。但是保留初始10卡是容易的”。原Phase0草案的仅燃烧之血资格撤销，保留十卡、改为固定IL核实的兼容涅奥遗物最小白名单；不放开任意被动。具体白名单尚未确认，当前不要求roll/游戏测试。

DET-001 Phase0机制/检测已独立验收（D-079）：自然Shuffle使用Run.Shuffle且原投影覆盖；先按model/upgrade排序，再由该RNG排列，十卡对应9次NextInt/9步kernel。draw抽空后的下一次抽牌请求才触发重洗。普通十卡/每回合5抽且无额外影响时，R3首次重洗是静态推导，尚无真实DET重洗结果。集合唯一决定顺序不能无条件接受；Shuffle/CombatCardGeneration具名getter分离已确认，但具体印牌/复制路径仍未调查。验收：`evidence/det-001/project-lead-phase0-review-2026-10-23.md`。

DET候选离线已验收（D-080）：固定IL选择BurningBlood＋GoldenPearl最小白名单；GoldenPearl取得150金币、相关draw/shuffle hooks无额外影响，可在正常Neow提供时选择，但不保证每局提供。完整state/资格/事件序号/token与五点四操作已实现，68离线检查和metadata/dummy通过；真实生产回调/R3重洗仍未验证。当前运行HOLD、不要求roll。验收：`evidence/det-001/project-lead-candidate-review-2026-10-23.md`。

## 调查记录格式

每项调查至少包含：日期、调查者、适用版本、问题、方法/证据、已确认结论、推断、未知项、对 Roadmap/Todo 的影响。

## 既有研究摘要（待仓库证据复核）

日期：2026-08-21（录入日期；原始调查日期未知）  
调查者：项目发起人提供  
适用版本：正式版 v0.107、public-beta v0.111

问题：RunReplays 是否证明语义操作重放路线可行，它有哪些已知能力和限制？

证据状态：目前只有项目发起人的研究结论；仓库内尚无源码、日志、录像、profile 或复现报告。

已确认（按项目发起人报告）：

- v0.107 sample replay 可启动并播放。
- 支持暂停、Step、停止以及从当前位置接管。
- 不支持真正的后退/seek。
- 节奏机械，没有完整保留真实操作时间。
- 正常录制在地图操作和卡牌结算附近存在严重性能问题。
- v0.111 存在 Rng API 和部分 UI scene hierarchy 的 compatibility break。

合理推断但未验证：

- 语义操作重放可以成为本项目路线的组成部分。
- 性能问题可能位于录制或 hook 热路径，但根因未知，不能据此选择优化方案。
- 版本变化可能需要隔离游戏 API，但尚未决定是否值得建立兼容层。

下一步：执行 INV-001，把结论关联到源码与可复现步骤。

## INV-000：定位并固定 RunReplays 上游源码

日期：2026-08-21  
调查者：Project Lead  
适用版本：RunReplays tag `may-2026`；STS2 对应版本尚未确认

问题：RunReplays 的原始项目在哪里，能否建立可审计的本地源码基线？

方法与证据：

- GitHub 搜索定位原始仓库：`https://github.com/boardengineer/RunReplays`。
- GitHub tags/releases 页面显示 `may-2026` tag、短 commit `05b0edd` 与 Version 0.2.0 release。
- 下载该 tag 的 GitHub archive，SHA-256 为 `654B9E416158943440C9C08BAF1D4FAF78A3581D3926A64D19A4F0220027C7DC`。
- 本地 `RunReplays.json` 声明版本 `v0.2.0`、BaseLib 依赖及 `affects_gameplay: true`；`LICENSE` 为 MIT。

已确认：

- 静态调查已有固定、带校验值的源码输入，共 90 个文件。
- 源码包含 commands、record/replay patches、ReplayEngine/UI、文档及四组 sample replay 数据。
- 项目目标框架为 .NET 9，并引用 STS2 DLL、BaseLib、GodotSharp 与 Harmony。

未知项：

- tag 对应的 STS2 精确版本。
- 此 tag 是否为此前 v0.107 成功运行 sample replay 的同一版本。
- 当前 master 相对该 tag 的变化；本次取得的是 archive，不含 Git 历史。
- 本地是否具备可复现该版本的游戏与 BaseLib 环境。

影响：INV-001 从 `BLOCKED` 转为 `READY`，可先完成静态模块清单；运行复现仍是条件性工作。

## EVID-001：目标版本与本地切换条件

日期：2026-08-21  
信息提供者：项目发起人；官方公告交叉核对  
适用版本：STS2 public-beta v0.111.0、正式版 v0.107

已确认：

- 项目发起人当前使用完整版本 `v0.111.0`，并可自由切换到 v0.107。
- 每次切换需要重新下载约 300 MB 游戏文件，因此频繁交叉测试有实际切换成本。
- 官方 v0.111.0 patch notes（`https://store.steampowered.com/news/app/2868840/view/671751488532383386`）发布于 2026-08-14，明确 8 月 27 日不会发布 patch，并表示下一次 patch 可能等待更久。

项目发起人观察：其本人、朋友及国内大部分玩家目前使用 public-beta v0.111。该观察用于产品优先级，尚无正式样本统计，不应写成全球玩家分布事实。

推断：v0.111 预计具有数周相对稳定窗口，适合作为当前目标版本。官方原文不构成精确一个月冻结保证。

影响：接受 D-005；COMPAT-001 的方向从“选择候选版本”调整为“识别并修复/隔离 v0.111 目标路径上的 break，并评估是否值得保留 v0.107 支持”。测试安排应成批完成同一版本工作，减少反复切换。

## EVID-002：存档隔离与跨版本对局行为

日期：2026-08-22  
信息提供者：项目发起人  
证据级别：运行观察/记忆，尚无存档样本和日志  
适用范围：STS2 v0.107 与 v0.111.0；加载模组与不加载模组模式

已观察：

- 游戏默认隔离加载模组和不加载模组时使用的存档。
- 模组可以在游戏设置第一页的 `Modding` 栏目启用或关闭。
- 同一局游戏从高版本切换到低版本后无法进入。
- 同一局游戏从低版本升级到高版本后可以进入；已删除的卡牌会变为 `0` 费“抽 1、消耗”的占位符，已有卡牌若在新版本被修改，会采用新定义。

未验证线索：可能存在让模组/非模组存档互通的第三方模组；名称、实现方式和副作用未知。

补充确认：Steam 分支切换默认保留 `mods` 目录中的物理文件、各模组启用状态与配置。保留不代表兼容，新版本仍可能使模组部分失效。

影响：Replay 文件必须记录游戏版本，并在版本不匹配时至少给出明确诊断。游戏能够升级读取存档不等于 replay command 在新版本仍保持相同语义；当前不承诺跨版本播放。

## EVID-003：RunReplays 在 v0.111.0 的失败边界（初步）

日期：2026-08-22  
信息提供者：项目发起人  
证据级别：此前运行观察，尚无日志  
适用版本：STS2 v0.111.0；RunReplays Workshop item `3759973128`，artifact 身份见 INV-001A

项目发起人报告：

- 游戏能够识别并加载 RunReplays。
- RunReplays 的界面可以正常打开。
- 某次尝试开始实际游戏流程时失败。

本地日志证据与版本时间线：

- 2026-08-19 02:39 和 02:41 两个会话使用 9,757,184-byte `sts2.dll`，与当前 v0.111.0 DLL 尺寸相同。RunReplays UI 可多次打开；一次到达 `LoadRunSave(seed='oldTZEKRYTSNT')`，但未出现 `StartReplay` 或 dispatch，随后会话结束/重启。
- 02:50 游戏 `sts2.dll` 被替换，02:51 会话报告 DLL 为 9,364,480 bytes。该会话才记录 sample `TZEKRYTSNT` floor 18 成功 `StartReplay`，并连续消费 event、map、play-card、end-turn、reward 等 command。
- 02:51 会话随后记录 Regent seed `F287UMJ7PN` 的正常 RECORD run，至少保存至 floor 2 并记录多次出牌。
- 结合项目发起人当时在 v0.111.0 与 v0.107 间切换的背景，高置信度推断 9,364,480-byte 成功会话是 v0.107；由于当时的 `release_info.json` 和 DLL hash 未保存，这仍是推断而非完整 build 身份证明。
- 成功会话中的 `StateCheck divergence` 在所见片段主要来自英文 expected display name 与中文 actual display name；数值状态仍匹配。尚不能据此认定真实 gameplay divergence。

仍然未知：触发失败的具体测试轮次、按钮/路径、是否已开始加载 sample 或存档、屏幕错误、首个异常与 stack trace。

影响：现有证据支持“v0.111.0 能加载模组和打开 replay UI，但载入/启动 replay 失败；切回推定 v0.107 后成功”。COMPAT-001 应复现 v0.111.0 的 `LoadRunSave`/`StartReplay` 边界并取得游戏主日志；同时调查 display-name 本地化造成的假 divergence。

## EVID-004：当前本地游戏构建与安装目录

日期：2026-08-22  
证据来源：本地只读文件检查  
安装目录：`C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`

已确认：

- `release_info.json`：version `v0.111.0`、commit `41cef1ea`、branch `v0.111.0`、date `2026-08-13T17:39:18-07:00`。
- `data_sts2_windows_x86_64\sts2.dll`：9,757,184 bytes，SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- 当前 `mods` 目录顶层包含 `AscensionUnlocker/`、`SBWEI.dll`、`SBWEI.pck.BCK`，未发现 RunReplays 或 BaseLib。

限制：这只描述 2026-08-22 当前磁盘状态，不能反推此前 v0.107/v0.111 RunReplays 测试时的 artifact 或启用模组集合。

影响：v0.111.0 调查现在有精确游戏 build 身份。若要复现此前 RunReplays 失败，需要先重新取得并固定当时使用的 RunReplays/BaseLib artifact，或明确选择新的测试 artifact。

## INV-001A：当前 Workshop artifact 身份

日期：2026-08-22  
证据来源：Steam Workshop 安装目录、manifest、DLL metadata/hash、RunReplays diagnostic log、上游 commit archive

RunReplays：

- Workshop item：`3759973128`
- Workshop manifest：`v0.1.2`，最低游戏版本 `0.105.0`，依赖 BaseLib `>=3.1.0`
- DLL SHA-256：`FDE403AD981E605607BCACBF0D46F3008659195B83A6252D4B5B2710FFF12076`
- DLL ProductVersion：`1.0.0+4b1fcfed256888d95e012b0af85de585eeb6734a`
- 运行日志自报版本：`0.1.0`

BaseLib：

- Workshop item：`3737335127`
- Manifest：`v3.4.5`，最低游戏版本 `0.107.1`
- DLL SHA-256：`AD2F89E43E8B31DEBFAB65D783353D9429EBA59A2CFE904FF933A894CE79D32E`
- DLL ProductVersion：`3.4.5+4a97642d7843309cdf35c46a11e3f46132cee049`

源码：

- RunReplays DLL ProductVersion 对应上游 commit `4b1fcfed256888d95e012b0af85de585eeb6734a`。
- 已下载该 commit archive，SHA-256 `2C2805358DA566694A02CBE97BCEE64A72AF18FF68C3A76EA5C140A4A0E1286B`。
- 该 commit 的仓库 manifest 为 `v0.2.0`，与 Workshop manifest `v0.1.2` 不一致。

影响：B1–B5 已完成。静态模块调查应以 commit `4b1fcfed...` 为当前主参考，`may-2026/05b0edd` 作为较早对照。所有结果必须使用 hash/commit 标识，不能只写“v0.1.2”。

## EVID-005：8 月 19 日测试的模组集合

日期：2026-08-22  
信息提供者：项目发起人；本地 diagnostic log 交叉核对

已确认：8 月 19 日测试仅启用 RunReplays 和 BaseLib，没有启用其他模组。测试时段日志未发现当前其他已知模组的 patch owner，与项目发起人回答一致。

补充确认：严重录制卡顿也发生在只启用 RunReplays 与 BaseLib 的环境中。

影响：该轮 v0.111.0 compatibility failure 不能归因于第三方 gameplay mod 冲突；录制性能问题可优先在 RunReplays/BaseLib/游戏交互链路内调查。

## PERF-HYP-001：同步诊断日志可能放大录制卡顿

日期：2026-08-23  
状态：假设，未验证  
适用 artifact：RunReplays DLL commit `4b1fcfed...`

源码证据：

- `.csproj` 定义 `RUNREPLAYS_VERBOSE` 和 `RUNREPLAYS_DISPATCHER`。
- `DiagnosticLog` 被注释为 always-on。
- `DiagnosticLog.Write` 在全局锁中写入 `StreamWriter`，且 writer 设置 `AutoFlush=true`，因此每条日志都会触发 flush。
- dispatcher、save、record、state-check 等路径存在大量诊断调用；现有 `diagnostic.log` 的短 replay 片段包含密集逐事件记录。
- 项目发起人确认 8 月 19 日性能测试时 replay overlay 同时处于显示状态。

未知：实际文件系统 flush 延迟、每个玩家操作产生的日志条数，以及它在地图/卡牌结算卡顿中的占比。源码证据不足以证明它是唯一或主要根因。

下一实验：PERF-001 首轮应独立控制两个变量：overlay 显示/隐藏，以及原始同步日志/禁用或缓冲日志。不能在同一组实验中同时改变两者后把改善归因于单一因素。记录帧时间、最长 hitch、日志行数和写入字节数。

## EVID-006：测试存档树与云同步状态

日期：2026-08-23  
信息提供者：项目发起人；本地文件时间交叉核对

已确认：

- 8 月 19 日测试时 Steam Cloud 关闭。
- 本地同时存在普通 `profile1/2/3` 和独立 `modded\profile1/2/3` 存档树。
- `modded\profile2\saves\current_run.save` 创建于 2026-08-19 02:52:26，并在成功 replay/record 测试期间持续写入到 02:55。
- `modded\profile2\replays\latest.mcr`、prefs 与 progress 也在同一时段更新。

结论：该轮测试实际使用 modded profile2；没有 Steam Cloud 同步作为当时存档变化或性能卡顿的直接变量。

## EVID-007：v0.107 成功测试中的 UI 路径与 `Load Save` 语义

日期：2026-08-24  
适用 artifact：RunReplays commit `4b1fcfed...`；历史成功运行推定为 STS2 v0.107

项目发起人明确记得：主菜单存在 RunReplays 入口，进入独立的记录页面后，在对应记录上点击 `Load Save` 可以进入游戏；其余精确按钮顺序目前记不清。

源码核对：

- `RunReplaySubmenu.cs` 创建 `My Replays` 与 `Sample Runs` 两个标签页。
- `RunReplayMenu.cs` 为记录提供 `Replay to floor …`、`Load Save` 和 `Replay Floor` 等不同操作。
- `Load Save` 调用存档恢复/继续路径；`StartReplay` / `StartReplayFromFloor` 是独立路径。前者不会加载 replay command 并启动 dispatcher，不能作为“自动播放已经开始”的证据。

历史日志核对：推定 v0.107 的 02:51 会话先对 `TZEKRYTSNT` 的较高楼层执行过 `LoadRunSave` 并返回主菜单；随后另一次操作记录 `ReplayEngine.Load`、`StartReplay(seed=TZEKRYTSNT, floor=18)` 和连续 command dispatch。因此两种路径在当时都实际使用过。

结论：此前“sample replay 成功播放”仍有日志支持，但项目发起人本次明确回忆的 `Load Save` 本身是继续存档功能，不是 replay 播放按钮。精确触发自动播放的是 `Replay to floor …` 或 `Replay Floor`，目前未知是哪一个。

影响：后续复现步骤和产品术语必须严格区分“载入状态并接管”与“启动自动 replay”；不能把 `Load Save` 写成 replay 启动步骤。项目发起人确认无法回忆精确 replay 按钮；该未知项不影响路线判断，当前不安排 v0.107 补测。

补充场景覆盖记忆：项目发起人记得当时试过多个自动播放场景，至少包括一次“一层弱怪”和一次 Boss。其余场景以及 Boss 的身份、章节、角色和 seed 均不记得。该信息说明人工观察不局限于单个普通房间，但证据不足以证明任一完整 sample 从头到尾成功，也不能把这两次观察与日志中的 `TZEKRYTSNT` floor 18 自动绑定。

完整播放边界：项目发起人不记得任一测试是否无人干预地运行到目标终点。因此 RunReplays 的历史运行证据应表述为“多个场景能够启动并执行 replay”，不能升级为“完整 sample 可稳定从头播放到尾”。本项目未来需要用明确起点、终点和 divergence 判据重新建立该证据。

## PROD-REF-001：Counter-Strike demo 作为观看体验参考

日期：2026-08-24  
性质：产品参考与架构启发，不是 STS2 技术事实

项目发起人指定 Counter-Strike demo 系统作为目标体验参考，重点关注前进、后退和播放速度。

定向资料结论：

- Valve 系 demo 资料把 replay tick 作为观看进度条的坐标，并在消息流中记录 tick 与消息类型；资料也描述 periodic full game states/full packets 的存在。
- Valve 的广播格式文档包含 tick、FULL fragment 与 keyframe interval，支持“关键状态 + 后续增量”这一导航心智模型。
- Valve 2025 年 Counter-Strike 2 TrueView 公告明确说明：播放端会重建部分原始体验，但版本不匹配时默认禁用该能力，并承认慢放时仍可能存在表现时序差异。这强化了版本元数据、重建边界和诚实标注的重要性。

来源：

- `https://github.com/skadistats/compendium/blob/master/chapters/01-the-demo-file.md`
- `https://developer.valvesoftware.com/wiki/CHILLMODEA/Drafts/Counter-Strike_Global_Offensive_Broadcast`
- `https://store.steampowered.com/news/posts/?appids=730&enddate=1762988336&feed=steam_community_announcements`（2025-11-04 TrueView）

对 STS2 的推断：可借鉴“稳定回放坐标轴、结构标记、检查点、从最近检查点重演到目标”的模型。由于 STS2 是回合制且播放间隔可配置，语义操作序号可能比墙钟秒数更适合作为底层定位轴；该推断必须通过 NAV-001 验证。

### 项目发起人的 STS2 映射提案

2026-08-24，项目发起人确认三层导航均符合预期，并提出双层 UI：房间内用时间轴表示玩家操作；房间/楼层用类似成绩结算页的路线图表示整局路径。提供的参考图已保存为 `evidence/product-references/run-history-route-reference-2026-08-24.png`，SHA-256 `E2031A5BA4CB9CF9717B409252732D77C6D9C2079CBE6C1E6CD0594F372ED834`。

产品判断：宏观路线图解决“去哪一间房”，微观时间轴解决“看哪一个操作”，二者应共享当前位置并支持互相定位。具体布局与恢复机制仍未知。

## TIMING-HYP-001：操作时间戳可能足以支持原始节奏

日期：2026-08-24  
状态：假设，未验证

产品要求：MVP 记录语义操作时间戳，默认使用分类 config 节奏，并提供“原始节奏”模式。

假设：为每个已记录玩家操作保存相对时间戳，即可在回放时近似恢复玩家操作 cadence，而不需要连续采样游戏状态。

已知边界：操作时间戳不描述动画完成、渲染帧、加载阶段或 UI readiness。若回放机器/游戏版本的表现速度不同，严格按时间触发可能在游戏尚未可交互时到期；调度器需要以 readiness 为硬条件，并把时间戳作为最早/目标触发时刻，而不是无条件执行命令。

已确认产品边界：只累计窗口聚焦、未暂停、非加载且玩家可交互的时间；排除暂停、失焦、加载与可识别阻塞。可交互状态下的长停留按真实思考保留。

待实验：单调时间源与精度、上述计时门控状态能否可靠且低开销检测、不同操作类型是否需要额外的“界面出现/消失”时序事件，以及时间戳采集对热路径性能和文件大小的影响。不能仅用间隔长度区分真实思考和系统阻塞。

## STATE-HYP-001：强记录可支持自动纠偏并继续

日期：2026-08-25  
状态：产品方向已确认；技术方案未验证

产品要求：除语义操作外记录足够权威状态。播放检测到偏差时，若记录状态可用，则自动恢复并继续；输出结构化错误日志而不默认阻断观看。

候选组成（均未决定）：

- 语义操作前后的轻量状态指纹，用于快速检测。
- 房间入口、回合边界、选择界面等安全节点的完整或可装载检查点。
- 操作间的增量状态，用于降低重演距离或直接修正局部状态。

关键矛盾：更密集的权威状态有利于纠偏与后退，但可能增加序列化、内存复制、写盘、文件大小和版本耦合；它必须满足默认自动录制的性能门槛。

不可假定：强记录不能凭自身解决缺失类型/资源、破坏性 schema/API 变化或损坏文件，也不能据此宣称跨版本兼容。

下一实验：执行 STATE-001，并与 DET-001、NAV-001共享同一组最小场景和指标，避免分别设计互相冲突的检查点模型。

## PROD-REF-002：RunReplays 不可逆接管是负面产品参考

日期：2026-08-25  
证据：项目发起人的既有运行观察；本轮不追加历史复测

观察：RunReplays 从 replay 某位置接管后，无法返回 replay；当前回放直接变成一局普通游戏。

产品结论：该不可逆模式转换不适合本项目已经确定的可导航播放器模型。接管不属于 MVP；停止观看应保留 replay 的独立性。未来若重启接管议题，必须先定义显式分支、返回路径和副作用隔离，不能直接照搬该行为。

## INV-001：v0.111 最小探针定向入口地图

日期：2026-08-27  
状态：Complete；Project Lead 验收通过  
完整报告：`investigation-reports/INV-001-report.md`

已确认的目标 DLL 身份仍为 public-beta `v0.111.0`、commit `41cef1ea`、SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。Project Lead 独立复核了下列元数据签名：

- 恢复候选链：`RunState.FromSerializable`、异步 `RunManager.SetUpSavedSingleplayer`、异步 `NGame.LoadRun`；捕获候选为 `RunManager.ToSave(AbstractRoom)`。
- 最小战斗捕获/完成候选：`CardModel.EnqueueManualPlay`、`ActionExecutor.BeforeActionExecuted/AfterActionExecuted`、`CombatManager.TurnStarted/TurnEnded`。
- dispatch 候选：`NetCombatCardDb.GetCard/TryGetCard`、`CardModel.TryManualPlay`、`PlayerCmd.EndTurn`。
- readiness 单项候选：`PlayerCombatState.Phase`、`IsPartOfPlayerTurn`、`PlayerActionsDisabled`、`IsPaused`、`ActionExecutor.IsRunning`。组合语义尚未运行验证。
- v0.111 RNG 不再公开 RunReplays 使用的逐实例 `Rng.Seed/Counter`；可取得 `Rng.ToSerializable()` / `RunRngSet.ToSerializable()`，其中 `SerializableRng` 含 counter 与四个 state 字段。

已确认的版本断裂：RunReplays 使用同步且名称不同的 `SetUpSavedSinglePlayer`、已删除的 `CombatManager.IsPlayPhase`，以及不再公开的 `Rng.Seed/Counter`。这些事实足以拒绝把整套 RunReplays 直接作为 v0.111 探针基底，但不能解释历史失败的首个异常。

性能边界：RunReplays 的逐事件 AutoFlush 日志、全量状态扫描、周期轮询、同步完整写盘与 UI 树查找均已由源码确认存在；它们对历史 hitch 的实际贡献仍只是待 PERF-001 验证的性能风险，不是已确认根因。

最关键未知：原生 `SerializableRun` 能否从首回合输入就绪点恢复相同 combat transient state、手牌/敌人摘要和完整 RNG digest。因此 Wave 1 拆为 `SPIKE-001A` 恢复基线与其后 `SPIKE-001B` capture/dispatch；001A 失败时不得自行扩展为自制快照系统。

## SPIKE-001A：首回合原生 SerializableRun 恢复基线

日期：2026-08-27  
状态：Complete；有价值的失败；Project Lead 验收通过  
完整报告：`investigation-reports/SPIKE-001A-report.md`  
证据目录：`../evidence/spike-001a/`

固定环境：STS2 public-beta `v0.111.0`、commit `41cef1ea`、目标 DLL SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`；最终会话只加载 disposable `SPIKE001A`。场景为 Ironclad、Standard A0、seed `8G50LMPKRAS0`、Act 0 第一场弱怪、首回合 `PlayerTurnPhase.Play`。

已确认：

- `RunManager.ToSave(CurrentRoom)` 成功产生 fixture；调用前后观察白名单和完整 RNG diff 为空，普通 save hash 未变化。
- 同进程恢复在 `SetUpSavedSingleplayer` 可重复遇到 `State is already set.`，说明候选链不能直接在现有 RunState 上 setup。
- 完整重启后，`FromSerializable → SetUpSavedSingleplayer → LoadRun` 均完成并达到候选 input readiness，但 expected/actual 有 14 项差异。
- 差异集中于手牌顺序、draw pile 多重集，以及 `Niche` 和 `Shuffle` RNG；完整 RNG digest 从 `E1B5ECD8…50B83` 变为 `AD2DDC08…9EA11`。画面可操作不等于状态等价。
- `SetUpSavedSingleplayer` 在 start/success 日志之间把普通 `current_run.save` 从 49,152 bytes / `B628…76C5` 改为 50,587 bytes / `1136…0CB21`。即使 probe 先把 `RunManager.ShouldSave` 置为 false，也没有阻止该写入。
- Project Lead 独立解析了 attempt 04 的阶段和 14 项 diff、复核 artifact/fixture hash，并使用 `C:\Users\sanwe\.dotnet\dotnet.exe` SDK `9.0.311` 重建：0 warnings、0 errors。默认 PATH 的 .NET 8 不足以构建 net9，这只是工具链差异。

结论：当前调用链不能作为安全、等价的首回合检查点恢复路径。`SPIKE-001B` 保持阻塞。这不证明原生 `SerializableRun` 在所有边界都不可用，也不证明游戏不存在安全恢复/隔离路径。

推断：`LoadRun` 很可能重新执行了起手抽牌/洗牌，导致 `Shuffle` 推进和牌堆变化；`Niche` 的额外消费来源仍未知。该推断尚未由方法体或阶段内采样确认。

安全提醒：截至 Project Lead 验收时，游戏 mods 目录仍存在 `SPIKE001A.dll/json`；profile1 普通 `current_run.save` 仍为实验后 hash `1136…0CB21`。未执行自动卸载或回滚。

下一调查：INV-002 只读追踪正常 continue/load、save 写入、RunState 卸载与房间重建语义，先获得方法体证据再批准下一次运行实验。

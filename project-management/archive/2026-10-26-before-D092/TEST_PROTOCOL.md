历史快照：D-092精简/放宽授权前的项目管理文档；不作为当前执行权限。

# Replay Mod 测试协作协议

更新日期：2026-10-09  
状态：Active；适用于所有 Implementation Codex 运行实验

新任务同时遵循 `WORKFLOW.md`。已封存实验按原合同，不追溯重判。具体工作包可明示有限修复、配置会话和写入窗口；本协议不再将所有baseline后harness失败一律永久退役，也不将所有modded槽/正常退出变化当作个人存档损坏。旧fault/restart任务的严格保护白名单只用于其原实验，不自动成为普通诊断前置条件。

## 测试环境与个人进度边界（2026-09-28用户澄清）

- **不加载模组的profile1**是用户的个人连胜存档，也是唯一须作为个人进度保护的槽。项目实验不得选择它作为目标，不得读取、复制、改写、恢复或清理其中的存档文件；模组开关/启动路径不能意外切入该存档树。实验结果不得声称已直接验证此槽安全，除非未来另获用户明确许可并制定新合同。
- **modded profile1**仍是默认的专用测试槽，已完成全内容解锁；其Continue可按具体实验用作隔离哨兵，但不是个人连胜对局。**modded profile2/3**也经用户授权可用于实验。每份运行任务必须明确选用哪个modded槽，不能把当前selector=3本身当作数据安全FAIL，也不需要为保护modded2/3而调查复杂的离线selector改写。
- 用户允许自由切换模组。`AscensionUnlocker`可按实验控制变量选择启用或禁用；若诊断需要精确loaded-mod集合，仍须验证实际加载结果，并把额外模组对结论的影响写清。模组开关可能改变存档树/启动路径，不得因此意外进入不加载模组的profile1。
- modded槽可以发生预期的测试进度变化；不要把它自动分类为个人存档损坏。为使结果可复现，仍按**具体假设**建立必要的phase-local baseline、artifact与备份，并区分准备/业务/退出写入。已封存的旧probe与旧attempt仍按当时的原合同判定，不追溯放宽PASS或重跑禁令。
- profile实际id、规范化目录与`current_run.save`路径应由当次游戏版本解析；不得仅凭“1号”猜测物理路径。Steam Cloud历史为关闭，但每个可能写入存档的实验仍要求项目发起人在启动前确认当前状态。

## 存储拓扑与写入合同（2026-09-27 增补）

- 不再把“存档”当作单一目录/文件。每个新 runtime handoff 在安装或启动前须先列出固定版本下的逻辑路径、实际 `user://` 根、account/profile 作用域、所有者、读取/写入调用、启动/战斗/退出窗口、Cloud 关系及期望变化。路径由 production API 与当次只读证据双重核对；反编译导航缓存仅供定位，方法体结论需固定 DLL/IL 复核。
- v0.111.0 的**INV-012 固定DLL/IL已复核拓扑**：`settings.save` 位于 Steam 用户的 account 根，是本机设置与模组配置，不属于 profile1；`modded/profile.save` 是 modded 侧当前 profile 选择；`modded/profile1/saves/current_run.save`、`progress.save`、`prefs.save` 属于 profile1；`modded/profile1/replays/latest.mcr` 是游戏自己的 replay/diagnostic 文件，不是本项目未来的产品录像。实际磁盘根在现有日志中是 `%APPDATA%/SlayTheSpire2`，而非游戏安装目录；不在文档中保留账号数字。固定证据见`investigation-reports/INV-012-report.md`和`evidence/inv-012/storage-topology-writers.md`；每次运行仍需现场只读核对实际根与当前profile选择。
- 将“被测业务窗口”“退出清理窗口”“下一次启动窗口”分开建基线与比较。实验有效性仍需按任务范围检查 account 与所选modded profile 两层，但不能把已知会在正常退出保存的文件直接定义为跨窗口必须 raw-hash 不变；允许的字段/写入必须在实验前由固定证据预声明。旧实验出现的内容差异不得事后豁免。unmodded profile1不纳入实验读取/基线范围。
- 特别注意：`settings.save` 的变动可能来自显示/模组设置，`latest.mcr` 的变动可能来自原生录制收束。两者在 SPIKE-001A4 中已观察到内容变化，但确切字段、调用时点和因果关系尚为 Unknown，须由 INV-012 收束后才可设计后继 oracle。

## 无头与离线测试分级（2026-09-27 增补）

- 优先使用不启动 STS2 的 .NET 单元/合成契约测试、固定 fixture 解析器、hash-gated IL 与 mock store。这些可由 Implementation Codex 执行，不接触玩家数据。
- Godot 自身支持 `--headless`，但无窗口不等于不启动游戏、不初始化 Steam/Cloud/SaveManager、不写 `user://`，也不等于 STS2 release 构建提供自动跑局入口。独立、无生产 STS2 进程的最小 Godot headless 工程可作为后续节点生命周期测试候选，但须先隔离其 user data 与外部连接。
- 对**正式 STS2 可执行文件**使用 `--headless` 仍属于游戏 runtime 实验；现行“Codex 不得自行启动 STS2 进程”同样适用。未经另行 Project Lead 工作包及用户明确授权，不得试跑 `--headless`、`autoslay`、`bootstrap` 或其他 CLI 参数。先静态核实 release gate、有效用户数据根、Steam/Cloud 初始化、全部写入边界、退出/超时和 mod enablement；不能把 `--headless` 当作存档隔离机制。
- Godot 通用能力的依据：[官方命令行文档](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html)与[官方 user:// 路径文档](https://docs.godotengine.org/en/latest/tutorials/io/data_paths.html)。这些文档不证明 STS2 正式构建的特定 CLI 工作流可用。

## 禁止 GUI 自动操作

- Implementation Codex 不得使用 Computer Use 组件启动或控制游戏。
- 不得用其他桌面/鼠标/键盘自动化替代 Computer Use 来点击游戏 UI。
- 不得自行启动 STS2 进程并模拟玩家操作。
- 命令行构建、只读源码/IL 调查、probe 文件准备、日志与证据分析不受此条禁止；安装/替换 probe 时仍须遵守对应 handoff 的文件与版本边界。

## 需要运行测试时的请求格式

Implementation Codex 必须暂停并向项目发起人提出一轮边界清楚的测试请求，至少包含：

1. **测试目标**：本轮只验证什么假设。
2. **开始前条件**：游戏版本/hash、启用模组、明确选用的modded profile、Cloud 状态、所需 Continue/场景和 probe 状态；确认不会切入unmodded profile1。
3. **具体动作**：按顺序写出玩家应点击什么、进入哪里、何时停止；不得只写“测试一下”。
4. **预期结果**：每一步正常情况下应看到的按钮、提示、状态或日志标记。
5. **失败/停止条件**：出现什么现象必须立即退出或停止继续操作。
6. **需要回传的证据**：界面文字、步骤结果、日志路径/片段、生成文件、时间点；只要求完成判定所必需的内容。
7. **禁止动作**：本轮不得出牌、继续下一房间、覆盖 fixture、切 profile 等具体限制。
8. **目标后的动作**：达到目标画面或日志标记后，明确要求立即停手、等待 probe、按指定 Stop，或退出到哪里；涉及战斗结束时必须明确写出是否禁止领取金币、卡牌、药水、遗物及进入地图，不能把“完成战斗”留成开放式步骤。

项目发起人负责打开游戏并执行步骤。Implementation Codex 在收到结果前不得假定测试已通过，也不得自动进入下一运行 phase。

## 分阶段实验

- 一个工作包包含多个有序 phase 时，每次只请求当前已解锁的 phase。
- 收到用户回传后先分析证据，确认通过前一 phase，才能发下一份测试请求。
- 任一 handoff 的停止条件优先；达到 FAIL/BLOCKED 后立即结束当前 attempt。只有工作包明确授权的 harness 修复或配置修正才可在新 attempt 中继续，不能现场随意发展旁支或在旧终态上接着执行。

## 无效 harness attempt 与受控修订

- “首个终态停止”保护的是证据不可覆盖、用户状态不可冒险和实验范围不可漂移；它不意味着每个明确的测试框架缺陷都必须复制成全新工程。
- 能证明为 harness lifecycle/artifact/oracle 缺陷，且处于当前 handoff 的 repair envelope 时，可在同一 probe id 下做窄修订，包括建立实验基线后发生的观察/框架缺陷。必须保留旧 artifact，记录新 build/revision identity、使用 fresh attempt 名称，并补充针对该缺陷的 regression test；只有记录结构变更才需要新 schema，不能无意义地为每次代码修复发明格式。
- 某次 attempt 的业务行为无法由证据确定时，结论为 **结果不明**。若涉及 manager override/transaction isolation 等受保护行为，应按具体工作包做必要的 prior-integrity 检查后交回 PM；不得凭空判安全。纯 observer 诊断在可消耗 modded 槽的新 attempt 可以按明确授权重新准备测试局，不需要把旧历史字节当成永久前置条件；旧差异、结论和身份全部保留，不能靠 rebaseline 消除旧 phase 的问题。
- 受控修订不得改变业务假设、PASS 标准、允许 I/O 范围或测试场景。若需要这些变化，仍应签发新工作包/新 probe identity。
- Ready 是等待人工输入的稳定状态。startup/missing-event timeout 必须在 Ready 原子提交时解除；除非 handoff 明确要求，不能把人工思考/响应时间当作 timing-gate failure。
- 同一固定 artifact 文件名不得跨进程代表多个 attempt。所有保留运行证据必须不可覆盖，并具有明确 attempt/revision identity。

### 工作包内的有限修复授权

- Project Lead 可在 handoff 中授予明确的 **harness repair envelope**。在该 envelope 内，Implementation Codex 可以自行修改、测试、构建和替换当前 probe 的代码，而不必为每个纯探针缺陷重新申请一份工作包。
- 授权覆盖工作包列明的 harness 缺陷，例如：生命周期观察点过早、ticker/Ready/timeout 竞态、观察提示、attempt-specific artifact writer、oracle isolation 或离线测试缺陷。是否已有baseline/run不是唯一判断条件；必须用证据定位到框架，而非把未知production失败宣称为harness。含实际写入/override的实验还需遵循其更严格副作用合同。
- 每次修复必须保留旧 artifact/log，增加可区分的 build/attempt identity，补充能复现该缺陷的 regression test，重新通过 handoff 要求的 build/static/smoke gate，并仅在游戏进程停止且 installed pair hash 精确匹配时替换本 probe 自己的安装文件。游戏仍只能由项目发起人手动启动。
- repair envelope 不允许改变业务假设、production行为/fault调用点、允许I/O、个人保护边界或降低PASS标准。对已定位harness的等价实现修复与抹掉不利断言不同；需记录理由与回归证据。若需要改变业务条件/oracle语义，可能触及个人数据、未知副作用或达到预算，停止并交回PM。新attempt可以按预授权重建可消耗fixture，不重写旧失败。
- 是否允许修复后直接请求下一次人工运行、以及最多允许多少次，必须由 handoff 明写；未写即不授权。Implementation Codex 应优先在同一个 Codex task 中完成同一 probe lineage 的这些修订，以保留实现上下文与证据链。

### 基线来源与可满足性

- recovery copy只证明被复制字节的身份，除非复制协议明确保留并验证源mtime，否则其文件系统mtime不得作为原文件时间戳基线。跨进程比较应使用前一phase artifact中记录的源mtime。
- 历史artifact/recovery用于证据链与已知内容身份；它不自动成为未来实验的完整当前基线。若测试准备本身必须修改某个受保护组件（例如`settings.save`中的mod enabled状态），handoff必须提前拆分“允许的准备变化”和“phase-local before/after”，不能要求互相矛盾的历史字节恒等。
- 新工作包引用旧artifact字段前，必须验证旧schema确实包含该字段及其语义。缺失字段应在离线preflight中得到明确的schema incompatibility，不得在运行时被折叠成普通`false`。
- 每个 prior-integrity gate必须有一个使用**实际固定前驱artifact/schema**的正向测试，证明所有声明前置条件同时可满足；只用手工构造的简化对象测试单项规则不足以证明整个gate可达。

## Probe 安装与启用状态生命周期

- 新 probe 的构建与安装是独立检查项，但可在同一工作包完成。旧退役probe仍必须disabled。工作包可明确允许纯配置进程中的新probe与辅助模组共存，前提是身份拒绝不会创建/消耗业务attempt、不会建立override或执行测试动作；正式业务启动仍须满足精确loaded set。
- 安装新 probe前，先盘点 live mods目录中所有项目 probe的精确 DLL/manifest identity，并明确列出本轮允许启用、必须禁用和不得触碰的 ID。文件存在与 enabled状态必须分开表述。
- retired probe先持久禁用并通过范围明确的配置或日志核对，不为此强制额外验证启动。避免在尚无新probe时先禁用最后一个辅助mod而制造意外unmodded启动。若采用上一条的预授权配置会话，可先安装新probe，再手动启用它/禁用辅助mod并正常退出；文件存在、配置enabled和runtime loaded三者分别核对。
- 不得假定新安装的 mod默认为disabled，也不得假定已经完成的旧 probe会自动失效。首次新-probe启动必须再次核对实际 loaded/enabled ID集合；检查位于 loader 已发布最终集合的边界，先于探针读取profile内容、建立baseline/candidate/override或执行业务。生产启动可能已初始化明确允许的modded测试槽，不应假定主菜单门早于所有production profile初始化。initializer 中的空/部分集合不得解释为最终配置；观察点由日志、静态调用顺序或独立lifecycle test证明。
- future harness应把纯配置错误与业务phase分开：wrong loaded-mod set可只输出固定拒绝标记而不创建attempt（r6采用此路径），或生成attempt-specific、不可覆盖的precheck evidence；两者都保持业务phase未消费。只有handoff明确预授权时，用户才可退出、修正enabled状态并重新执行precheck；保留有关记录，不用同一业务artifact锁死尚未开始的安全配置修正。
- 工作包结束后，live mods中的probe默认保留但必须由用户禁用。删除/替换安装文件只有在当前Project Lead handoff明确授权、游戏进程已停止、目标DLL/manifest hash精确匹配且workspace产物已验证可恢复时才允许；源码、artifact、recovery和报告不随安装清理删除。

## 实验 oracle 隔离

- 用于判定被测 mutation 的 expected state 必须与 system under test 的 mutable graph 完全隔离；“新列表”不等于列表元素已深拷贝。
- 若 expected 由 production 对象的 `ToSerializable`、clone 或 projection 产生，必须先通过源码/IL 或引用图检查确认嵌套对象语义；不允许只凭方法名假定深拷贝。
- oracle 计算前后必须再次比较被测对象 canonical state 和其他会被 oracle 接触的 live 集合。任何预调用变化立即分类为 harness/oracle failure，且不得调用被测方法。
- 优先从固定 baseline serialization 做 deserialize round-trip 建立 detached expected，并用含有已存在 nested stats/rows 的测试覆盖“修改已有对象”路径；只测试空集合或新增对象不足以证明隔离。
- oracle 失败不得写成 production 机制失败；已经独立观察到的调用路由、boundary 和持久化保护证据可以作为局部结论保留。

## 用户现场澄清与意外操作

- 用户在执行期间给出的最新事实说明若与 handoff 假设冲突，Implementation Codex 必须指出冲突、更新当前任务理解并在报告中记录；不得继续沿用已被否定的假设。
- 若该澄清会改变实验假设、安全边界、允许写入范围或 PASS/FAIL 标准，应停止当前 phase，交回 Project Lead 修订工作包；不得由 Implementation Codex 静默扩大或降低验收范围。
- 若在正式保护基线建立前的 fixture/场景准备中意外领取奖励或发生其他可解释进度变化，应记录具体动作和变化，废弃旧基线，并在 handoff 允许的情况下重新建立新基线；不得把准备阶段变化计为 transaction 写入。
- 若变化发生在正式基线建立后、但受测 transaction 尚未开始，当前基线失效。必须先报告并重新完成完整 preflight，不能跨越两套基线比较。
- 若变化发生在受测 transaction/phase 期间或其结束判定窗口内，不得通过“重新建立基线”消除证据；按 handoff 的 FAIL、INVALID 或停止条件处理，并保留原始日志与前后 identity。

## 结果记录

- 用户观察明确标记为 `User-observed runtime result`，与 probe 日志、文件 identity 和源码/IL 证据分开。
- 所有需跨会话保存的测试步骤、结果和缺失证据进入对应 `evidence/` 与 investigation report。
- 不把聊天中的“看起来正常”升级为状态等价、存档隔离或性能通过。

## 跨进程 restart 比较（用于明确测isolation/restart的工作包，不是所有诊断的通用门）

- 同一受测 transaction/fault phase 的正式 before/after 窗口仍须按工作包逐项比较 hash、长度、mtime 与 inventory；窗口内任何未授权变化都不能用正常退出解释。
- phase 已封存并恢复原 manager 后，用户正常退出游戏可能触发 production Settings/Prefs/Progress 保存、backup 替换或 timestamp sync。下一进程的 restart verification 必须把这种 **phase 结束后的正常退出写入** 与 transaction 内副作用分开记录。
- 固定 v0.111.0 当前只允许把 `settings.save`、`saves/prefs.save.backup`、`saves/progress.save.backup` 的 mtime-only变化视为与 INV-005 已确认退出链相容；前提是文件长度/hash、目录 inventory、Continue canonical、`NumReloads` 和其他受保护语义全部不变。该结果只能写成“无持久内容变化，存在已知正常退出时间戳更新”，不能写成“跨进程没有任何写入”。
- 上述三项之外的 mtime变化、任一内容/长度/inventory/Continue/`NumReloads`变化，或无法区分 transaction 窗口与退出窗口时，必须按工作包 FAIL/BLOCKED，不得扩大 whitelist、恢复 backup 或 rebaseline。
- restart PASS 只覆盖前一 phase 已完成 ordered teardown并正常退出后的新进程一致性；不得外推为强杀、原生崩溃、断电或未执行 finally 的 crash recovery。

# REF-002 — 社区 STS2 Modding Tutorials 适用性评估

日期：2026-09-16  
状态：Complete  
分类：`ADOPT_AS_REFERENCE_INDEX / NO_CURRENT_DEPENDENCY_CHANGE`

## 输入与身份

- 本地归档：`C:\Projects\STS2ReplayMod\SlayTheSpire2ModdingTutorials-master.zip`
- 大小：11,282,845 bytes
- SHA-256：`1957F102973BCA1F8F089D8F25C0D70F31220091371CE33657F000D732083721`
- ZIP entries：499；其中非目录文件 317。
- ZIP entry 时间统一为 2026-09-10 18:55:30 +08:00，适合作为归档生成时间线索，不等于源文件逐项修改时间。
- 对应公开仓库：`https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials`
- 2026-09-16 只读 `git ls-remote` 的 master HEAD：`c3eeb78bba4b13b125e90e8f3bdbfd8dabd1cb5f`。

归档本身不含 `.git` 元数据，不能证明对应哪个 commit；也未包含 LICENSE 文件。因此本项目只把它作为知识/索引来源，不复制其中代码或把示例作为可再分发实现。若未来需要复用代码，必须先确认具体文件的授权。

## 已确认内容

归档声明支持 `public-beta`，并主动警告早期访问期间 API 频繁变化、教程未必是最优方案。主要内容包括：

- 基础工程、Harmony patch、调试、启动参数、控制台和 Workshop 发布；
- BaseLib 与 RitsuLib 的内容、配置、存储和扩展能力；
- 0.107 至 0.111 的迁移记录；
- 基于 0.111 源码整理的 Hook/游戏流程触发顺序；
- Godot UI/节点、特效与资源相关教程。

## 对 Replay Mod 的高价值部分

### 1. 0.107→0.111 迁移表：直接采用为兼容性索引

该表明确记录了多个已经独立遇到或确认的变化：

- 0.108→0.109：`RunManager.SetUpReplay` 增加 `playerIdToLoad`；RNG 从公开 seed/counter 转为 `SerializableRng`/`ToSerializable`；
- 0.107→0.108：`IncrementNumReloads` 参数变化、profile/account path API 扩展；
- 0.110→0.111：card play flow、多人 handshake、`IModManagerFileIo`、`SaveManager.IsLoaded`/`PrefsSaveManager.IsPrefsLoaded` 等变化。

用途：作为版本升级时的候选 break checklist，以及解释 RunReplays/旧 probe 失效的索引。它不是权威证明；任何会影响实现或验收的条目仍须在固定 DLL/hash 上复核。

### 2. Hook Trigger Order：对 recorder 覆盖设计很有价值

教程按 0.111 源码整理了：

- combat start、turn start、首回合抽牌与进入 play phase；
- card play、damage、death、power、turn end；
- combat end、replay write、progress/achievement 的相对顺序；
- room/act/map、reward、gold、shop、relic、potion、rest site、orb 与 pile change 等入口；
- listener 遍历顺序以及 combat-ending guard 的绕过情况。

这能直接形成“语义操作与结构节点 capture coverage matrix”的初稿，并帮助避免把结果 hook 当成玩家意图 hook。尤其 `EndCombatInternal` 中 room wrap-up/native replay write、`AfterCombatVictory`、progress save 的相对位置，对我们区分 recorder finalize 与隔离副作用很有帮助。

限制：该章节明确标注由 AI 整理并审阅，仍可能有错误。它只能降低静态定位成本，不能代替固定 v0.111.0 DLL 的方法体证据或运行实验。

### 3. RitsuLib lifecycle events：提升为 recorder observer 候选

教程列出的 lifecycle surface 覆盖：

- `RunStarted/Loaded/Ended`；
- `RoomEntering/Entered/Exited`、Act 与 reward screen；
- combat start/victory/end、turn、card play/draw/discard/exhaust、pile movement、creature death；
- reward、gold、relic、potion；
- `RunSaving/RunSaved`、`ProgressSaving/ProgressSaved` 与 profile 生命周期。

如果这些事件在固定 RitsuLib 版本上语义完整、顺序稳定且额外开销可控，它们可能减少 Replay Mod 自行维护 Harmony patches 的数量，并为自动录制、结构节点、日志与兼容性提供统一 observer 层。

这改变的是候选优先级，不是依赖决策。教程没有证明：

- 事件能覆盖全部玩家语义操作与选择界面；
- event payload 足以重放而非只足以观察结果；
- 高频 card/pile/creature 事件的录制性能满足门槛；
- lifecycle patch 在未来游戏版本中比项目自有最小 patch 更稳定；
- Replay transaction 或普通存档隔离由 RitsuLib 处理。

### 4. Node Attachments、配置与通知：适合作为外围能力候选

- Node Attachments 可以按 vanilla parent type 在 `_Ready` 时挂载 code/scene node，并提供 duplicate policy、顺序和 sibling placement。它适合未来 combat timeline/overlay 或管理页子节点，但教程没有 `NMainMenu` 示例，也不证明它能直接创建与单人/多人并列的主菜单入口。
- Custom Config 覆盖 toggle、slider、choice、key binding 等，符合分类播放间隔、原始节奏、倍率与自动录制开关的未来需求。
- Toast Notifications 符合“正常录制静默，仅在关闭/失败/不支持时提示”的产品方向。

这些能力应在正式 UI/config 竖切时重新评估；不需要为了当前实验提前引入。

### 5. Networking/ManagedNetAction：未来多人研究的索引

教程说明 vanilla `ActionQueueSynchronizer` 的 host-authoritative 顺序，以及 RitsuLib `ManagedNetAction` 的 execution kind、可靠队列与 replayable custom action 目标。这对 MVP 后的多人 Replay 和其他 gameplay mod 扩展协议有参考价值。

它不解决原版玩家操作的捕获，也不能证明 native replay 能覆盖所有 multiplayer state。当前不进入实现。

## 低价值或当前不采用部分

- 基础工程模板与自动复制到 live `mods` 的做法不优于已评估的 Alchyr template，也不符合当前 build/install 分离和 hash-gated probe 流程。
- 教程的 wildcard RitsuLib NuGet 版本不适合可复现实验；若采用必须固定版本、包 hash 与游戏 API target。
- `ModDataStore`、`RunSavedData` 和 `SavedAttachedState` 是模组数据/游戏 save 集成，不是 Replay 文件存储、权威战斗快照或 session isolation。它们不能替代已验证的 in-memory `SaveManager` transaction。
- Data Telemetry 章节面向远端事件上传。MVP 当前只需要本地结构化诊断日志；在没有独立的隐私、同意和网络失败设计前不引入远端遥测。
- 内容创作、角色、卡牌、美术与 VFX 教程和当前技术 milestone 基本无关。

## 对当前工作的影响

- 不修改、不重建、不重新安装正在执行的 `SPIKE-001A2-R2`。
- 不改变 A2-R2 的验收标准、运行步骤或停止条件。
- 不把 RitsuLib 加入当前 probe，也不把教程观察写成已验证架构事实。
- `SPIKE-001A2-R2` 仍是当前唯一最值得完成的任务。

## 建议后续使用方式

若 A2-R2 通过，在签发 `SPIKE-001B` 前，用本教程的 Hook Trigger Order 建立一份最小 capture surface checklist，并与：

1. 固定 v0.111.0 DLL；
2. RunReplays 的 capture/dispatcher hooks；
3. 固定 RitsuLib v0.5.18 lifecycle patch 与 payload；

做窄静态对照。输出只需决定每类事件使用 vanilla hook、RitsuLib observer 或自有 Harmony patch，以及哪些事件仍缺少“玩家意图”信息。只有该对照显示 RitsuLib 能显著减少 patch 面且没有热路径/语义缺口时，才重新考虑把它变为必要依赖。

若 A2-R2 失败，先处理 reconstruction 根因；本教程不改变该阻塞关系。

## 最终结论

这份教程对项目有用，但定位是“高质量社区索引与候选 API 地图”，不是 Replay 架构或可直接复用实现。它最能减少后续 capture coverage、版本迁移和 UI/config 入口调查成本；它没有推翻目前关于 transaction isolation、RitsuLib 非必要依赖、强记录或 seek 的任何结论。

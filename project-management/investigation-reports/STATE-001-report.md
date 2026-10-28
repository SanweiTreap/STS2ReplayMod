# STATE-001 权威状态落盘与自动纠偏首点闭环报告

2026-10-27。D-103/D-092，**Implementation已完成固定首点闭环，提交PM验收**。build-003/r4真实完成正常录制非零点落盘→录制进程退出→新进程零前缀从文件直达1/14→三次能量/格挡扰动被比较检出→独立重新读/解码文件纠偏并校验→原Play自动继续真实胜利、Step保持暂停→胜利Stop/重开/中途Stop。用户确认通过，现场游戏已退出；报告区分实际运行、纯数据检查、旧失败和仍未支持范围。管理状态与验收结论由PM维护。

最终源码与安装仍为build-003/4283890C…E76A92D，无测试后新DLL变更。录制Runtime779ca0d…/PID10832与观看Runtimef8f18b96…/PID43824/start time不同；r4两次冷开、五次独立文件应用/读回（其中三次纠偏），20个真实后续操作、4次内存后退均匹配，真实胜利和两次退出原manager/所比较Continue/progress通过，游戏退出后52个modded1存档文件与正常录制后的恢复点SHA一致。没有再请求测试或扩下一能力。

## 候选和边界

STATE-001/build-001，ReplayMod 0.1.0，固定游戏SHA沿v0.111。DLL `FCE1E395BB83D5518F1B08B57FA5C09AFD46939A7D7C32A802C186918463CF10`，安装/封存pair读回一致；见 `evidence/state-001/preparation/build-001-5448a922.json`。源仍在同一 `src/ReplayMod/`，没有另建聊天/实现仓库，未自动启动游戏、headless或Computer Use。

后续build-002，DLL `C031E20C52FBF87DFD2DDB51380CE22FB52A4A97A5D866B9D9ECC0FC1D3614E5`，见 `evidence/state-001/preparation/build-002-adeb0082.json`；现已替换为build-003，DLL `4283890CF7FC01DEE08FAAD8FA46F2263F9CF4438F91BED5A09A3E9BDE76A92D`，安装收据preparation/build-003-aa508491.json。旧FCE及C031 pair/settings/selector分别保留到STATE001完整恢复点preparations/build-002-adeb0082、build-003-aa508491；未改原clip/state文件，也未更换其期望投影或hash。下述首版实现中关于初态同构和整图hash读回的描述为build-001历史；后续修订见末节。

选点为正常录制首回合第一张未升级Defend完成后，操作位置1、当前执行器空闲/玩家可操作；要求无玩家能力成员变化。可复用DEMO-005准备，避免先实现多回合、伤害/死亡或新增能力/实体的冷恢复。这一步真实改变能量、格挡、有序手牌/弃牌、CardModel结算字段及能量/出牌/格挡历史，足以区分原生0点存档、前缀重演和文件中的非零状态。

只支持这个位置的磁盘冷恢复和比较纠偏。原有内存直接后退仍独立存在；冷入口不捕获0点memento，到达1点后才建立观看期缓存，因此1→0明确不可用。尚不支持完整seek、所有稳定点、跨版本、新内容/生成/终局、在途异常或原生卡死救援。当前新路径实际可用性仍须下述人工验证。

## 初版实现与边界（build-001历史，后续修订和最终证据见后文）

- `StateGraph.cs`：分离的数据节点、字段、标量、值结构、容器及引用ID。初态按固定根/有序字段/容器建立当前上下文绑定表，文件记录初态拓扑签名及位置1的数据；新观看上下文独立建立同构绑定，不取录制对象地址。初始对象作为当前上下文锚点，新增对象仅允许明确的EnergySpent/CardPlayStarted/CardPlayFinished/BlockGained与CardPlay及已知BCL数据容器。类型注册来自受选初态与代码白名单，不从不可信字符串调用Type.GetType/任意工厂。
- `RecordedState.cs`：正常Recorder开始时绑定，首张防御真实完成后在游戏线程冻结。完整Rng使用counter/s0/s1/s2/s3；保存选定数据根中的玩家HP/block/能量、牌实例/升级/可变值/有序牌区、既有遗物/药水、原敌人/AI、run/player/monster RNG、room/combat历史与内部字段。CardPlay/历史Actor/Player/Card/History及readonly玩家回合字典恢复到新上下文引用，不重放构造历史事件或累加一遍。纯数据历史实例通过固定类型的无构造分配和字段恢复，不构造新的Monster/Card/Power模型。
- 绑定的不透明外部服务/原生初态对象留在当前上下文；根没有遍历整个RunManager/SceneTree。当前回合生命周期Id、pending信号、锁、Task、取消源、委托、节点、StateTracker与卡牌数据库订阅不写入文件，继续使用新战斗的原生运行上下文。LocString缓存不纳入。此范围依赖首回合/成员和模型形态未变，不据字段数量推称全状态完备。
- `StateFile.cs`：私有sidecar版本1、8MiB上限、内容SHA256、关联clip ID/position/game/mod/scope/After比较hash，节点/容器/字段/深度/类型/引用检查。Clip保持格式3，新增默认省略的Authority标记；没有改变旧clip或把After当恢复值。原生SerializableRun只创建初始房间外壳。
- 后台任务只编码/写已冻结DTO，不接触live graph。pending写入后原子改名；胜利封存等待任务、读回关联校验后才发布最新clip。日志分别给绑定/冻结/编码/写入耗时和bytes；没有建立新的性能平台。
- PlayerLoop的文件入口先正常建立独立初始房间/SaveManager，再直接应用位置1数据，cursor直接到1，下一真实动作是sequence2。文件应用复用DirectCheckpoint的UI/订阅整理，但`captureData:false`，没有捕获或应用其IdentityMemento。当前显示节点只作新上下文的整理；文件不保存像素/动画任务，Defend选点没有形态变化。
- 每次冷应用和纠偏都重新ReadAllBytes/JSON decode。应用后先重新冻结数据并比较完整数据图，再整理原生UI、比较LiveState实际投影。日志区分数据读回与UI后状态匹配，记录录制/观看Runtime、OS PID/start UTC、来源hash、零前缀和实际后续动作；这些日志尚待实际运行生成。
- 默认关闭的录制测试开关一次新局消费；默认关闭的扰动开关仅在位置1稳定暂停时可启用，下一次Play/Step改变观看副本Energy+1、Block+2。EnsureState实际比较发现差异后使用同一文件应用入口再校验，不在扰动helper里撤销常量。cursor仍Busy、Position不改，Playing由原控制意图保留；Stop在安全边界退出，其他位置无支持状态则明确失败/正常清理。
- `Runtime`追加已验收D02录制build的明确兼容，原MemorySession/原Continue/progress比较与奖励拦截沿用；ShouldSave=false仍不作为全写入隔离证明。

## 本次已完成的验证

Release构建0错误/0警告。`evidence/state-001/data-contracts.json`的7项纯数据契约检查通过：冻结后源对象改变不影响数据、独立对象上下文绑定/有序牌区/循环引用/readonly历史和struct、保留新生命周期/信号、再次独立解码不累加历史、坏拓扑/未知类型/悬空引用拒绝且不先改逻辑值、sidecar checksum/version/clip/position/build关联、冷到达后原Play/Step意图。它们没有执行游戏构造器，不是跨进程游戏恢复结论。

DirectCheckpoint数据复用调整对应的6项既有memento契约回归通过，见 `evidence/state-001/regression/direct-contracts.json`。原17步组合文件269735cd…的原envelope/hash/明确build兼容检查通过且原字节不变，见 `evidence/state-001/compatibility/content-compatibility-r1.json`；本次没有运行该录像或重做旧人工矩阵。

## 安装与恢复点

安装前/后均game0，固定本体hash和原活动账号通过，modded1仅ReplayMod启用。Steam Cloud沿用户当前批次已确认关闭，没有自动更改Cloud。复用 `C:/Projects/STS2ReplayMod-local/backups/DEMO005-20261027-013423-1f0ea24e59/`：51个modded1/saves文件与恢复点比较，仅progress.save.backup不同，所有主存档文件匹配；该变动副本另存 `preparations/state-001-before-build-001/`并核对来源/副本SHA。旧D02 pair/settings/selector由安装脚本保留到 `preparations/build-001-5448a922/`。无整存档恢复，无另一账号读取。

## 人工验证经过

第一段已完成：用户确认首张未升级防御、正常胜利自动保存并退出；现场game0，Runtime `779ca0d36e714f6f84ca45f923e7f644`，OS PID10832/start UTC2026-10-27T04:19:59.2007908Z，24事件/0失败，最后已回主菜单。真实录像 `604d842f5c944fe781939b2d36b94624`，14步胜利/实际种子HRQWK15DD6CE/col2双蛞蝓，第一步CombatIndex1/upgrade0 Defend，能量3→2、block0→5；其后含燃烧、耸肩+、小花第三回合和跨击杀，未使用药水。没有要求用户重录为建议种子，取点条件已满足。

sidecar 374480 bytes、409节点（400初态绑定+9新节点）、18份完整RNG；9新节点实际为四类历史、共享CardPlay及四个玩家回合字典。文件中的玩家能量2/block5与After独立核对相同；原生初始checkpoint/After没有用作恢复值。State SHA256 `51D8B921387B6F896F5244D92AF3A99941DC647CA1EF685ECFD3F2991B80BAF0`，Clip SHA见 `evidence/state-001/recording-files-r1.json`。该独立纯.NET真实文件检查通过envelope、StateFile.Decode关联、引用/历史和源文件不变，不执行游戏构造器或冷恢复。

真实路径观察到初态绑定18.6103ms、位置1冻结1.5525ms、后台编码2.8879ms、写盘0.5209ms，最终seal9.6888ms；只是一例，非发布开销边界。日志 `evidence/state-001/runtime-r1.json`；原clip/state/session私有原字节归档并读回。正常新增进度已建立完整恢复点 `C:/Projects/STS2ReplayMod-local/backups/STATE001-20261027-122340-b30c90322d/`，1278文件/132037841 bytes来源及副本SHA一致，另一账号排除，无恢复/换DLL。收据 `evidence/state-001/recording-archive-r1.json`。

现请求第二段：用户新启动，从落盘第1步打开，应直接暂停1/14、能量2/block5；启用一次STATE扰动，点Play，自动检出/从文件纠偏/继续至真实胜利，Stop。再以同一落盘入口打开，Step一次到2/14、Stop并正常退出。等待真实冷到达/纠偏/后续胜利/隔离事件核对后补报告；如果失败保留失败与局限，不用纯数据测试代替运行。

## r2冷入口失败与build-002修复

Runtime `b414550e1eb441228dbee0d511ca03bf`/PID46748/start UTC04:25:39.6541044Z，7事件。初态投影匹配后，文件应用在state-initial-binding-shape-mismatch拒绝，停在0，未执行任何prefix或后续动作，也未进行扰动/胜利/重开验证。退出日志OriginalManagerRestored和所比较Continue/progress均true、现场game0；再对STATE001恢复点的所有modded1/saves文件比较，无变更。原失败日志/私有原字节归档见runtime-r2.json、runtime-archive-r2.json及恢复点runtime/state-001-r2，不追认成功。

直接缺陷是初版依赖完整初态图的分配/别名拓扑相同，原生新局和FromSerializable没有这个契约。固定源码ModelId.Deserialize每次创建等值record，CardModel.EnergyCost又是可惰性初始化的缓存，足以使逻辑相同的图不同构；原r2没有记录首个差异节点，不能断言这一例具体只差哪一个字段。初态状态匹配并不意味着引用索引可逐号对应，故不能仅取消签名检查后继续原序号写入。

build-002在正常初始房间中显式绑定固定数据根、原Creature CombatId、NetCombatCardDb的uint牌ID及deck顺序/精确模型类型；然后按命名字段、字典逻辑键匹配其数据子对象。引用索引只在payload内部使用，不再作为新进程的分配顺序。相等ModelId允许序列化产生不同别名布局，mutable对象仍不能错误合并身份；未映射的已知纯容器、六类历史与CardPlay（共七种纯数据对象）可重新分配，未映射的模型/服务/闭包明确拒绝。公开数据getter预先物化deck/combat卡的cost/vars，不执行牌或消耗RNG。原始拓扑签名保留诊断，但不要求跨进程相同。

文件仍是原v1数据和原SHA。每次应用独立解码/绑定/写入，随后逐个实际读回所有已编码字段、容器内容、共享当前对象引用与完整RNG，UI后再比较LiveState；不再用重冻图的分配编号hash相等代替数据读回。新增authority-logical-bindings记录两边签名/count/锚点及规则。正常播放旧片按精确FCE前驱build兼容，不开放任意跨build。

Release0错误/0警告，8项纯数据检查通过（data-contracts-build-002.json），新增用真实record式值ID的共享/非共享布局差异，要求原严格图签名/count不同、逻辑锚点仍恢复正确；再次解码不重复历史，读回能检出后续扰动，错误类型根拒绝。原真实clip/state的checksum/关联/源字节/精确前驱build读取通过（recording-files-build-002.json）。没有自动运行游戏，也没有新冷恢复成功证据。安装前后game0、原账号/固定本体/仅ReplayMod/modded1核对；无正常新增进度，复用已有STATE001恢复点，未整包重复备份/恢复。现请求原片冷入口直接重测，不重录。

## r3两次容器类型拒绝与build-003修复

Runtime6015f194f6cc4a9a8b46952903190e99/PID44112/start UTC05:00:03.7548855Z，11事件、两次打开都在`state-logical-binding-unresolved:12:MegaCrit.Sts2.Core.Models.ModifierModel[]`退出，initial已匹配，位置0、未应用文件或执行prefix/扰动。两次失败后原manager及所比较Continue/progress均恢复，现场game0，所有modded1/saves文件仍与STATE001恢复点一致。原日志、失败、私有hash归档见runtime-r3.json、runtime-archive-r3.json及恢复点runtime/state-001-r3，不以“场景出现过”认定冷到达。

本次具体缺陷是类型登记只覆盖当前实际对象/字段接口，遗漏字段已知元素的一维数组。真实文件node12为0元素ModifierModel[]；固定源码RunState.FromSerializable将Modifiers.Select(...).ToList()，因此当前初态无需包含该具体数组类型。逻辑身份修复后还需要恢复这个纯容器表示，不能把已知空容器当未知模型拒绝。上一版回归没有覆盖它。

build-003仅在本地类型表已含元素类型时，用MakeArrayType登记该一维托管数组；不会Type.GetType、按payload加载程序集或实例化元素/修正器。现有字段类型、节点/容器限制与模型/服务拒绝保留。绑定预先校验全部node大小；若仍有未支持绑定，聚合数目与前8项类型，避免只给第一个而无后续诊断。

新增IReadOnlyList<抽象Modifier>在录制为空数组、观看为List的独立上下文用例：修复前纯.NET实际复现同类state-logical-binding-unresolved，修复后恢复为0元素数组且玩家/牌区正确；未知Process[]仍拒绝。Release0错误/0警告、9项纯数据检查通过，原真实clip/state/明确FCE前驱build校验通过。red/green与数据检查见container-regression-red-green.json、data-contracts-build-003.json、recording-files-build-003.json。没有进行游戏自动化，也没有build-003冷到达/纠偏运行结论。安装已读回pair/原账号/固定本体，仅ReplayMod/modded1，game0，沿当前Cloud确认与STATE001恢复点。请求继续用原片冷入口重测；稳定1/14后再做扰动/播放胜利/Stop重开。

## r4最终实际闭环

用户确认能稳定1/14、启用扰动后到胜利、Stop和重开正常且已退出。Runtime `f8f18b96b0104e95b1d8281e2c632193`，OS PID43824/start UTC05:07:00.2624573Z；录制来源PID10832/start UTC04:19:59.2007908Z，跨录制进程；其间已多次现场game0。r4共158事件，0意外失败。详见 `evidence/state-001/runtime-r4.json` 和带断言/安全差异摘录的 `runtime-verification-r4.json`，原始158事件逐文件hash归档在STATE001恢复点runtime/state-001-r4/session。原r2/r3失败未删除或改写为成功。

- 两次从主菜单正常创建独立初始房间，按35个逻辑锚点绑定400个当前对象，文件409节点/18份RNG直接恢复到1/14。记录与观看的绑定数量同为400，但拓扑签名496124…与5B38E8…不同，实际验证逻辑绑定处理了差异。首个真实执行操作是sequence2，所有20个playback-step均为2及以后；sequence1未执行，0前缀，也未以memento替代文件来源。
- 三次明确扰动均将Energy2→3与Block5→7；三次原生比较before-2确实发现差异。私有原始Expected/Actual独立展开，仅Player及Mechanics.Player中的Energy/Block四个投影路径变化，无其他字段差异，安全值摘录在验证JSON。原录像/期望未改。
- 冷开2次+纠偏3次共5次都独立读取/解码原sidecar，逐已编码字段、容器内容、当前上下文引用和18份完整RNG读回通过，UI后LiveState匹配。五次历史均为11项：原2个PowerReceived、5个CardDrawn、Defend实际4项历史，纠偏没有重复累计。恢复不是扰动helper撤销常量。
- 第一次纠偏前后Playing=true、cursor保持1，随后连续真实执行sequence2…14，全部After匹配，原生胜利task成功、position14、VictoryReached/AllOperationsMatched=true，不领奖，Stop正常返回菜单。第二次打开又直接到1，用户再次注入并Step；纠偏前后Playing=false，只执行该次Step，随后额外后退/重做。第三次注入在Play意图下自动继续，Stop在位置3的busy期间接受，等原EndTurn完整完成到4才退出。
- 总20个真实forward与4次直接内存回退均匹配（2→1两次、4→3、3→2）。第二次最终在4短Stop，AllOperationsMatched=false是主动中途停止，Error=null；不把它记为第二场完整胜利。两次closed均OriginalManagerRestored=true、NormalContinueAndOriginalProgressUnchanged=true，包含原progress文件比较。
- 五次文件应用日志耗时85.5937/46.3043/41.185/65.6415/39.1238ms，仅本次观察、不认发布性能上限。authority-file-applied里的FileSHA256字段实为重新序列化的内部Data payload SHA `570CDBBF…8FBA627`，不是envelope文件字节SHA；真实state文件byte SHA仍 `51D8B921…B80BAF0`，图数据SHA `3B48A888…D6E1DD4`。保留已测二进制不为日志字段改名再换DLL；各摘要明确区分三个hash。

游戏退出后现场game0、当前/封存/构建输出DLL SHA4283890C…一致，原clip/state实际byte SHA与r1一致；52个modded1/saves文件相对STATE001正常进度恢复点均一致（新增录制后52，之前安装前比较51）。无新增正常进度，无整备份重复/整存档恢复，无另一账号读取。最新完整恢复点仍STATE001-20261027-122340-b30c90322d，不以备份代替上述产品隔离日志和退出后比较。

## 交付与剩余边界

源码 `src/ReplayMod/`，当前sealed pair `evidence/state-001/builds/build-003/`，安装/恢复说明见README及私有恢复点RESTORE。构建/9项纯数据检查/真实文件读取证据与r1录制、r2/r3失败、r4闭环分别保留；先前候选validation.json是当时的NOT_RUNTIME_VERIFIED收据，不覆盖历史状态。PM可据最终runtime-verification-r4和本报告验收固定选点，Implementation不自行更新管理文档。

结论限首回合首张未升级Defend后的一个成员不变、无新能力点。只有这个位置支持文件冷恢复/纠偏；冷入口到1后没有0点缓存，1→0仍不支持。它依赖正常初始化的固定游戏运行上下文及明确逻辑绑定，未证明所有不透明对象隐藏字段、任意卡牌/所有稳定点、所有形态、跨生成/死亡或多回合冷恢复。已有内存后退能力不等于这些磁盘能力。未做进程杀死/在途卡死、原生凋萎竞态救援、所有写入路径、跨版本、性能边界或通用迁移。单个真实样本及3次可恢复扰动不能扩大为全状态/全场景保证。暂停/Step/Play测试开关默认关闭，正常录制没有每操作全图落盘；当前工作完成后交PM决定下一包。

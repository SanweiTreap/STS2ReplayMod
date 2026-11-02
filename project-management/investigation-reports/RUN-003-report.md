# RUN-003：涅奥奖励与整局观看入口

## 最新摘要 · 2026-11-02 · r2涅奥选择被战斗观察器误接，004修复候选待测

- r2/003会话8b0a9671共19事件，首次开局观看失败，保留FAIL。原生NGame新局工厂已从真实奖励前输入到Neow，shouldSave=false、同一隔离manager、未读首战checkpoint；首个`>`后首项Before资源/RNG及Neow候选校验通过，进入原生NewLeaf.Obtain→FromDeckForTransformation时触发`choice-reserved-outside-supported-command`，尚未完成奖励或进入首战，不能当纵向链通过。
- 原因是PlayerLoop.Open把未来首战clip的ChoiceTape.Play提前用于开局。ChoiceTape.Begin已把战外选择交RunChoices，但仍启用的战斗ReserveChoiceId观察器要求战斗scope，引发误判；7登记任务最终全部收束（1取消/2故障对应本次失败），原scene/manager及所比较Continue/Progress恢复。004只调整生命周期：涅奥前明确Clear战斗tape，真正进入首战仍由既有LoadEnteredBattle启用该clip；不放宽Reserved/Synced或原生选择/资源/RNG校验。开局增加战外选择归属断言及日志，供确认修复确实到达。
- 当前RUN-003/build-004，DLL `772A61846217C01D925C00D77203A307146633404E45ED85896E721DF8A21FB6`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`preparation/build-004-16ff947e.json`，保持mods/ReplayMod单文件夹。构建0警告0错误，保留原录制002的精确兼容；仅源码生命周期修复，不加镜像合同，不把构建当原生回归通过。004尚未游戏验证。
- r2封存23数据文件及51已知当前文件4,149,576 bytes至`RUN003-post-r2-current-20261102-130453`；相对r1仅settings.save.backup变化，无缺失，root/clip逐hash不变、native精确匹配point4首战完成未领奖。004安装后51项复核仅settings.save变化，game0，无restore/自行启动/另账号读取。证据`runtime-verification-r2.json`、`collection-r2.json`、`preparation/post-build004-check.json`；旧候选与失败保持。
- 下一仍复用2adb8306“从开局观看”：Auto关，用`>`进入变形窗口，确认前Stop；重开同一起点Auto到首战胜利，Back一次并`>>`重做末击，Stop并正常退出。无需重录，新增选择Stop/首战初态与结果/RNG、完整开局链仍待测。

## r1：新叶单张变形录制/旧路线观看通过，003候选

- r1/002会话d86ad1f2共331事件，0 observer-error/缺口/缺失操作。新Run `2adb83061a15459782c838e27caa8c0b`，Seed QL4MLA7ZADU6、铁甲Standard A0、三幕实际输入及Progress/Prefs在新局工厂前冻结。第1层原生Ancient/Neow→第2层普通缩小甲虫，4项event-choice/event-proceed/map/battle均complete，selected[1,2,3,4]，2访问/2尝试，成功原生保存点1–4，未发生Continue/猜测SL归属；真正奖励前起点已录，未将首战checkpoint当开局。
- Neow实际候选NEW_LEAF/POMANDER/NEOWS_BONES，选择索引0新叶；deck-transform在10张原生候选中最终选索引4的一张未升级打击，原生变为IRON_WAVE，牌组仍10张，新增NEW_LEAF遗物。首战checkpoint自然保留该结果，8项原生操作胜利并自动封存clip `68196feda91f42cfa9030aeba3e2c253`。资源连续性/生产读取/checksum通过；开局观看候选及首战初态/RNG重现仍未测，记录成功不代替观看通过。
- 用户纠正并由固定本体NewLeaf.CardsVar(1)确认：新叶只变形一张。此前测试步骤和UI提示“两张”有误，录制/播放逻辑一直读取原生实际选择，未硬编码变形数量；003把提示改为按原生页面选牌/确认，不再误导。历史001/002说明保留供追溯，本样本不需重录，也不拿单张新叶当树叶药膏/多张变形验证。
- 新主页/列表/详情及D-141文件夹识别已有实际使用证据：旧a51777b1详情OpeningAvailable=false/OldHistoryInvented=false，并从旧合法起点完整原生播放整条路线，AllOperationsMatched/Complete=true、0异常、原context及所比较Continue/Progress恢复；旧root hash仍480DCEA7…0796E。坏条目/全部导航/新增选择Stop与重开仍待代表检查，不外推。
- 当前RUN-003/build-003，DLL `8F1729C5B671C76949E231963DC2688E9A69F614724C47B1870FECDA9927E052`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`preparation/build-003-095709cf.json`，单文件夹mods/ReplayMod。除提示更正，仅增加002的精确记录兼容，构建0警告0错误，未改新旧录像。r1封存339文件与51已知当前文件4,150,616 bytes至`RUN003-post-r1-current-20261102-013437`，7项正常新局/录制/settings变化均保留、无缺失/restore，native精确匹配新point4首战完成未领奖；003安装后51项复核仅settings.save变化，game0。证据`collection-r1.json`、`runtime-verification-r1.json`、`checks/r1/`、`preparation/post-build003-check.json`。
- 下一复用2adb8306“从开局观看”：Auto关，`>`推进至变形选牌窗口、确认前Stop；重开同一起点Auto自然到首战胜利，Back一次并`>>`重做末击，再Stop/正常退出。核实际新局生成的Neow候选/选牌/地图和首战初态/RNG、选择中退出/任务/原context恢复，不独立打开首战片冒充开局链。

## 001/002：首候选实现与准备（当时未运行）

- D-143首段IN_PROGRESS，未有新游戏运行结论。候选把记录前移到原生NGame.StartNewSingleplayerRun输入边界：实际角色/seed/幕ID/Standard A0、实际Progress/Prefs在原生解锁生成、初始化及奖励前冻结，作为可选Opening随原RunId保存。观看在原有隔离SaveManager内调用同一新局工厂（shouldSave=false），沿原生Initialize/FinalizeStartingRelics/EnterAct到Neow；不是首战checkpoint补演，原生战斗replay写入在Setup后、Launch前关闭。此路径能否与实录候选/首战RNG完全一致仍待实测。
- Ancient共用选项任务接入Neow，复用原生EventOption/RelicCmd与已有选牌驱动；事件页面/候选身份/资源/RNG严格比较。原生古神对话用其真实点击入口逐段展示，原生Tween完成后继续，不记录逐字/鼠标轨迹。地图选路后沿同一Run进入首战，仅核clip初态/结果，不读取首战checkpoint重建。旧定向战斗准备会令开局输入明确不可用，不伪造可重现开场；首轮单独提供默认关闭的新叶准备，通过原生Ancient.DebugOption将第一项替换为NEW_LEAF，实际输入与准备标记均记录/重做，不手写奖励业务。
- 主页改为一个“回放”入口，独立整局列表/详情提供“从开局观看”（确有奖励前输入、首项Neow选择与完整首战时）或已有合法战斗起点。旧局不补开场，分场/旧片与内部战斗限制保持；坏条目逐项说明，进入页面时读索引，非每帧扫描；返回/Esc和焦点用Godot按钮原生通路。准备与旧测试入口移到开发页面。页面呈现/新目录实际识别同首轮人工启动验证，未声称已看过实际画面。
- 当前RUN-003/build-002，DLL `6FA1CE6B4D74C6290A3A264D3E7524EB29E848FE00A0BE13334738B177CDFBF1`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，安装收据`preparation/build-002-6123da21.json`，固定`mods/ReplayMod/`，根目录无第二份pair。两项目构建0警告0错误；新增开局输入往返/非法输入拒绝/旧文件不补Opening检查，既有分支与录像库合同、原RUN-002实录root/三完整片生产读取通过，见`checks/build-001/`（002未改这部分）；这些不代替原生开局/画面验证。011精确兼容保留，未改旧记录。
- 001安装后、首次启动前的源码复核发现新叶实际调用独立NDeckTransformSelectScreen，原普通deck-card处理不足；没有把它交给用户运行。002补deck-transform决策、实际选中→原生预览→CompleteSelection通路及公开输入阻挡，奖励变形业务仍走原生CardCmd。001 pair/收据保留，未测，不追认失败/通过；当前仅002供首测。
- 用户本轮确认Cloud仍关闭、无新进度。安装前51已知文件/原授权profile1保存叶已逐hash补存至`RUN003-pre-build001-20261102-002741`，与r31相同，无缺失；两次安装后51项均仅settings.save变化、game0、无restore/自行启动/另一账号读取，旧011/pair及所有失败保留。证据`preparation/pre-build001-backup.json`、`post-build001-check.json`、`post-build002-check.json`。本轮不假定旧point12以代替备份。
- 下一最短人工录制：主页回放查看旧局详情/返回→开发勾“RUN-003 涅奥新叶选牌准备”→正常新建铁甲A0，选第一项新叶并变形一张未升级打击和一张未升级防御→原生地图选最左普通首战获胜，暂不领奖，保存退出并正常关游戏。先核录制的真正奖励前输入/Neow访问与决策/自然首战；读取后再安排开局观看、选择中Stop和重开。非战斗Back/选择页Seek仍禁用；全Neow选项/奖励选择类型、Neow SL、精英/Boss/跨幕/原始与简化模式不外推或扩展。

## 实现入口与证据边界

- `RunOpening.cs`：新局工厂前实际输入、单次新叶准备、隔离原生初始化。`RunData.cs`新增可选Opening，旧无字段序列化保持不变；`RunRecording.cs`把Neow作为真实Ancient访问，选择仍归原Run/attempt。
- `RunEvents.cs`：保留普通事件通路，额外支持Neow的原生布局/对话/选项；`RunChoices.cs`在既有原生选择包装上接deck-transform的实际预览与确认，没有复制卡牌变形逻辑。
- `PlayerLoop.cs`/`RunPlayback.cs`：开局使用隔离Progress/Prefs，业务顺播到地图首战；旧起点默认逻辑保持。`ReplayLibraryUi.cs`及`Ui.cs`负责主页、整局详情与开发入口。`Prepare-Demo.ps1`补run-003，保持D-141单文件夹安装。
- 未做GUI/CUA或自行启动游戏；第一次真实工厂调用、候选生成/账户差异、选牌任务与动画、原生首战初态/RNG、观看退出与旧入口实际点击仍Unknown，依首轮日志在同包修复，不先新增调查包。

# RUN-003：涅奥奖励与整局观看入口

## 最新摘要 · 2026-11-02 · r4 BGM听测通过，开场Stop误判待006回归

- r4/005会话709a327b共40事件。用户确认进入Neow停止主菜单BGM、首战音乐和Stop回菜单音乐均正常，MenuMusicStopRequested=true，音频修复按所测通过；r3完整开局/选牌/首战RNG及胜利Back证据保持。本轮在首战尚处Opening时Stop，界面正常回菜单，但日志记录`run-cross-room-opening-navigation-disabled`，整轮保留混合结果，不把听测接受写成退出无异常。
- 根因：自然进新战斗的LoadEnteredBattle将所有opening reset回调都设为“禁止跨房间导航”；LoadOpening在Stop清理时也调用此回调。006为它提供仅NativeSeek.Stopping允许的实际清理，退出按RunPlayback的正常取消路径返回；真正Back/Seek仍拒绝。已提交的原生地图/场景Task直接等待其实际生命周期，避免Stop只退出外层包装后原生加载仍运行；开场Stop可在实际原生选择UI等待处清理（无需等其交互准备完成），继续禁止强截资产加载及回放旧任务。
- 为准确复现而非要求用户抢帧，开发区新增默认关闭的一次性“下次整局首战入场自动Stop”：复用既有NCombatRoom.OnCombatSetUp真实回调，在Loading/Opening且实例/CombatState归属匹配时调用公开PlayerLoop.Control(stop)，记录请求/接受/单次消费；后续清理记录OpeningRoomCleared及导航仍禁用。没有自行启动/操控游戏或新增探针平台。
- 当前RUN-003/build-006，DLL `AE19BDE821FBF8FD89A205709326909A90E9EE607368D63CA723A9E0EE1CB6EA`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`preparation/build-006-19283171.json`，构建0警告0错误、diff检查通过；生命周期/回调需原生实测，不加镜像合同代替。006尚未游戏验证。
- r4已恢复原manager/context及所比较Continue/Progress，10登记任务全部结束/0取消故障；封存44数据文件及51已知当前文件4,149,576 bytes至`RUN003-post-r4-current-20261102-140423`，相对r3仅settings.save.backup变化，无缺失，root/clip不变、native仍point4。006安装后仅settings.save变化，game0、无restore/自行启动。证据`runtime-verification-r4.json`、`collection-r4.json`、`preparation/post-build006-check.json`，失败原样保留。
- 下一仅核这条退出边界：开发勾首战入场自动Stop→2adb8306从开局观看并Auto，预期首战原生场景就绪时自动回菜单；重开同起点前进一步再手动Stop，正常退出。不重录/重打胜利或重复听测。通过后合并累计代表结论交PM；其他选项/Neow SL/坏条目实际交互与全部页面组合不外推。

## r3：原生开局纵向链/选择Stop通过，主菜单BGM残留005修复

- r3/004会话28ca75da共213事件、0异常。两次从奖励前输入调用原生新局工厂，CombatChoiceTapeActive=false/RunChoicesOwnSelections=true，r2生命周期修复实际通过。首次手动推进到新叶选择中Stop，7登记任务全部收束、3取消/0故障；原scene/context及所比较Continue/Progress恢复，计划内Complete=false不当失败。
- 重开后Auto沿原生Neow候选/单张变形→完成页/离开→地图→首战，3项战外资源/RNG严格匹配，首战初态/RNG自然匹配（未读首战checkpoint/注入expected），8步胜利。胜利Back到7/8后重做末击，状态/RNG及两次胜利均通过，共9次动作验证；终点Stop，10登记任务全部完成/0取消故障，整局Complete=true、原context恢复。新叶奖励前初始化/选牌/自然首战已有真实纵向证据，非独立打开首战片替代。
- 本轮仍保留音频呈现FAIL：用户听到主菜单BGM贯穿观看，日志语义通过不代替音频接受。固定原版NCharacterSelectScreen.BeginRun在进入NGame.StartNewSingleplayerRun前调用NAudioManager.StopMusic；观看直接进入工厂漏掉该外层生命周期。005在工厂前补同一个原生StopMusic，只结束前场景音乐，后续环境/战斗与回菜单音乐沿原生控制，不按涅奥/奖励手写曲目。新增MenuMusicStopRequested日志只证明调用，实际声音待用户确认。
- 当前RUN-003/build-005，DLL `2C6236C03CD535A025635F479EE171AD76B4E219294511216516DECA751B5AFA`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`preparation/build-005-6e329e4e.json`，构建0警告0错误；仅原生音乐调用与诊断字段，不新加镜像测试。r3封存217数据文件与51已知当前文件4,149,576 bytes至`RUN003-post-r3-current-20261102-133248`，相对r2仅settings.save.backup变化，无缺失，root/clip不变，native仍point4；005安装后仅settings.save变化，game0、无restore/自行启动。证据`runtime-verification-r3.json`、`collection-r3.json`、`preparation/post-build005-check.json`，旧003失败保留。
- 下一仅听测：同份2adb8306从开局观看，确认进入Neow时主菜单BGM停止；Auto自然进首战听正常战斗BGM后即可Stop，核回主菜单音乐恢复并正常退出。无需重录/重复选择中Stop、胜利与Back矩阵。全Neow选项、Neow SL、坏条目实际交互及全部页面组合不外推；阶段待音频确认后合并交PM。

## r2：涅奥选择被战斗观察器误接，004修复候选

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

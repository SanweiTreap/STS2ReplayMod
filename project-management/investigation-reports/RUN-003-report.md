# RUN-003：涅奥奖励与整局观看入口

## 最新摘要 · 2026-11-02 · build-002首测候选，待录制/游戏验证

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

# DEMO-002 后退一步实现报告

2026-10-26。当前实现结论：OBSERVED_REBUILD_BACK_AND_PARTIAL_STOP_MATCHED。用户已运行12操作短段；日志证实4次房间重建式后退、已执行的前进/前缀状态与RNG匹配及未播完Stop恢复。产品体验仍有明确缺口：用户期待直接呈现上一状态，当前可见重建/重演并未达到；不能将本轮技术路径可用等同于最终后退体验完成。PM管理文档未改。

## 本轮增量

沿用`src/ReplayMod/`原型和实验格式1，增加“后退一步（暂停时）”。播放游标在暂停稳定且p>0时同步预留重建请求，目标p-1；位置0、正在播放、正在执行、已Stop或已有排队Step时拒绝。完成后可后退。重建中禁止Step、播放及重复后退，Stop在加载/当前语义操作结束的安全边界退出。

PlayerLoop共用LoadRoom/Execute路径：等待内存保存→正常RunManager.CleanUp→移除旧NRun并等待一帧/保存排空→从原manager副本创建新的MockGodotFileIo/SaveManager→重新解析本片段checkpoint→原生SetUpSavedSingleplayer/Launch/GenerateMap/LoadIntoLatestMapCoord→核对初态→通过原TryManualPlay/EndPlayerTurnAction重演目标前缀→核对目标状态并保持暂停。原SaveManager在整个打开/重复重建期间保持分离，仅最终退出恢复。每次重建继续禁用ShouldSave及原生CombatReplayWriter。

没有手写HP/能量回填，没有反向快照、通用seek、跨房间或跨版本。清理时换掉旧NRun后再await，避免旧场景_Process使用清理后的RunManager。CleanUp hook只给本模块内部清理开放；外部退出请求仍进入正常Stop。异常沿现有路径记录并退出/恢复，不把失败伪装成目标到达。

新增少量事件：playback-control（控件、接受/拒绝、位置、忙碌/播放/Stop）；playback-back-start/arrived/stopped/failed（起点、目标、实际到达前缀、初态/目标匹配、耗时/错误）；playback-step区分forward与back-prefix。最终closed保留原manager恢复与Continue/backup/原progress比较。不会公开原生checkpoint或逐帧trace。

## 构建和离线验证

候选DEMO-002/build-001，manifest仍0.1.0，DLL SHA256：`8A7A03F9190EE81EFFD024013518B9697A7A9A12E44D1658353013F65984D6C7`。构建0警告/0错误。9项离线契约通过：原6项文件/环境/暂停与重开检查，增加完成后后退再Step/播放、重复退至0与请求互斥、未完成Stop/重建时Stop后不再分派。结果：`evidence/demo-002/contracts-r1.json`。这些检查只验证独立格式/游标，不运行游戏构造器。

精确构建锁保留，没有增加兼容白名单，没有重写旧clip hash或元数据。DEMO001旧pair仍在`evidence/demo-001/builds/build-001/`，本轮pair在`evidence/demo-002/builds/build-001/`。旧clip及其私有归档未更改。旧文件在新候选显示different-build-or-profile-or-mods属预期；本轮需重录短段。

## 备份与安装

游戏进程为0时执行Prepare-Demo，原测试ActiveUser匹配。考虑交接后可能有进度变化，直接建立新的完整恢复点，没有假定旧备份仍是最新：`C:/Projects/STS2ReplayMod-local/backups/DEMO002-20261026-200430-f036235b38/`，1269文件、131973241字节，源稳定且逐一读回hash匹配。精确被替换pair及settings/profile selector在该恢复点preparations子目录保留。未恢复存档、未改游戏本体、未读另一账号。

安装收据`evidence/demo-002/preparation/build-001-6c2dab07.json`确认installed=sealed pair，配置只启用ReplayMod、modded1，安装后游戏进程0。安装时Cloud未确认，原安装收据保留当时状态。随后用户在本对话明确回复“Cloud 已关闭，等待具体测试步骤”，确认记录在`evidence/demo-002/user-readiness.json`；没有自动修改Cloud或启动游戏。备份收据同目录；恢复办法见备份内私有RESTORE。

Prepare-Demo仅增加demo-001/demo-002证据归档选项、支持本轮恢复点名，以及将Cloud未确认作为明确的测试前条件记录；不以传入假确认绕过。Read-Demo可读取两种恢复点名，runtime目录仍沿用demo-001以保留旧片段。

## 预先提供的人工实测步骤（计划，实际轨迹见下节）

1. 用户确认Steam Cloud关闭，原账号modded1，手动启动。旧最近片段因精确ModHash不兼容应暂不可打开。正常新建铁甲A0到首场普通Monster，逐次Strike→Defend→EndTurn，每次等稳定后再操作；待下一玩家回合点击“结束片段”，正常保存退出至菜单。不领奖、不进下一房间。新建局可能替换该测试槽Continue，安装前状态已在新恢复点保存。
2. 为自然覆盖从磁盘/新进程打开，可在此正常退出游戏再手动启动一次。打开最近新片段，默认暂停0/3，确认后退禁用。
3. Step两次至2/3，每次等待结算；后退应显示重建中，到1/3且暂停；重建时Step/播放/后退应禁用。再Step到2/3。
4. 播放至完成3/3；后退应到2/3（撤掉EndTurn），再播放应完成3/3且状态一致。Stop回菜单。
5. 再打开应从0/3。Step到1/3，后退到0/3并确认后退禁用；再Step到1/3，未播完即Stop回菜单。再打开应仍从0/3；Stop退出，正常关闭游戏后告知完成。

显示失败时记录屏幕上的短原因，停止本轮普通操作，允许自动退出；若持续卡住约一分钟则正常关闭游戏并报告位置/文字，不继续普通进度。实现者读取本批事件与必要Continue比较，同任务修复重测。没有要求额外大矩阵。

## 本轮实际手动运行

用户反馈使用了比计划更复杂的操作，效果总体符合预期，同时指出当前重置对局并自动重演与其期待的直接后退不同。直接用户反馈记录与日志不能互相替代：以下仅按实际事件确认。

候选mod hash、only ReplayMod和profile1在本轮menu-ready匹配。20:50封存12操作片段`b51769abc3fc4bcb9b0335839a270543`：三轮分别Defend/Defend/Strike/EndTurn、Defend/Strike/Strike/EndTurn、Strike/Strike/Strike/EndTurn。20:51同进程打开一次，初态匹配。

| 后退 | 实际到达 | 初态/目标状态与RNG | 到达后 | 耗时 |
| --- | --- | --- | --- | --- |
| 1→0 | 0 | 匹配 | 暂停 | 1810.9456ms |
| 3→2 | 2 | 匹配 | 暂停 | 2171.6975ms |
| 2→1 | 1 | 匹配 | 暂停 | 1939.3916ms |
| 8→7 | 7 | 匹配 | 暂停 | 5175.3019ms |

1→0后再次Step到1、2→1后再次Step到2/3均匹配；随后播放实际前进至8（包含第4/8步EndTurn），暂停并8→7，证明撤回一次EndTurn后到达上一玩家回合末稳定点。最后在7/12、Busy=false时接受Stop，正常回主菜单；closed.Error=null、OriginalManagerRestored=true、NormalContinueAndOriginalProgressUnchanged=true。AllOperationsMatched=false是未播完的真实结果，不是失败被忽略。共11次forward和10次back-prefix完成核对，无state-difference/failed/observer-error事件。没有继续执行9–12，也没有8→7后再前进8的事件。

公开脱敏事件与片段摘要在`evidence/demo-002/runtime-r1.json`；完整新clip和本会话原始日志另存本轮备份的`runtime/manual-r1/`，clip复制hash读回匹配，收据`evidence/demo-002/runtime-archive.json`。没有发布原生checkpoint。读取时游戏进程0；本轮未换DLL、未恢复存档、未启动/控制游戏。

## 产品差距与真实边界

本轮交接明确指定“重建房间＋正常重演前缀”，实现符合该临时技术路径。MVP NAV-001将“检查点加快速重演”列为待验证候选，而非确定架构；当前每次从房间起点可见地重演，且目标较远时耗时上升，不能当成用户期望的“直接回到上一步”。本轮用户澄清应作为后续体验验收依据；没有擅改PM管理文档或宣称用户接受最终方案。

已证：候选构建、9项离线检查、备份/安装，以及上面这一个12操作文件中的4次重建式后退、已执行操作核对和真实中途Stop恢复。未知：完成后后退/再次前进、再次打开与跨进程打开、按钮禁用的视觉验证、重建中Stop的真实场景、任意内容覆盖。位置0和请求互斥有离线覆盖，不能据此宣称所有游戏内交互已测。

下一改动应以“点击后直接呈现上一操作稳定点，随后继续仍正确”为目标。具体采用可恢复稳定点、缓存还是快速重建需要结合实际状态恢复能力决定；隐藏当前慢速重演或缩短几帧延迟不能自动算这个目标完成。真正直接恢复需包含完整运行状态、牌序/敌方状态、RNG及继续所需的对象关系；当前对比投影不是可直接反序列化的完整快照。本轮未未经说明就扩建反向状态系统，也未将观察到的正确终点夸成即时导航已实现。

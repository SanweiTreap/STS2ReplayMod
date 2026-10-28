# SPIKE-INV003A Implementation Codex Handoff / Investigation Report

## 任务

- Task ID：`SPIKE-INV003A`
- 任务范围：验证事务式 in-memory `SaveManager`、`ShouldSave=false` 与 ordered teardown 是否能形成不触碰普通 Continue 的最窄 native replay session。
- 明确非目标：不实现 capture/dispatcher、任意 Replay 重建、room-before A/B、产品 UI、seek 或接管。
- 状态：COMPLETE；实验结果为 FAIL（Phase C direct-write victory sentinel 未命中；按首个可审计 FAIL 停止，D/E/F 未执行）

## 环境与版本

- 固定目标：public-beta `v0.111.0`，commit `41cef1ea`。
- 固定 `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- 最终 Probe：`0.4.0-transaction-probe`；DLL SHA-256 `C3F4D8723A5287EDD498F52970071D36B20AE3C704B09486B05018D75D1284DA`；manifest SHA-256 `FCEACB718053A17D76DC18CA1A493D26D24304A72D18D72B8686F95A92CD6A17`。
- 首次运行 artifact DLL SHA-256：`19B00B69521FFAB02D4412D16B09EDEC6DF9E027A06DCF4F8FFBA0F284A496B7`；因 probe 相对路径规范化缺陷未形成基线，已被修正版取代。
- 中间 artifact `0.2.0-fixture-gate` 与 `0.3.0-management-gate` 的身份和用途记录在下方时间线；它们均已被最终 Probe 取代。
- RitsuLib：不安装、不启用、不引用。

## 做了什么

- 新建 `experiments/SPIKE-INV003A/` disposable probe。
- 实现并运行 Phase 0 只读 preflight、native fixture gate、Phase A management-only 页面，以及 Phase B/C 的事务式 replay probe。
- Preflight 验证 active profile、版本/artifact、`RunManager.State`、Continue、save task 和受保护文件身份；只备份 active profile root，账户级精确文件只做只读身份记录。
- Transaction probe 深拷贝初始化 in-memory `SaveManager`，在 replay setup 前安装全局 override，调用 native `SetUpReplay` 后强制 `ShouldSave=false`，并由一个 `finally` 负责等待任务、清理 run、恢复原 manager 和重建主菜单。
- Audit 只观测 manager/store write/delete、profile/override 和外部 submit 边界，不替换正常业务结果；游戏操作均由项目发起人手动完成。
- Phase A 与 Phase B 通过；Phase C 未到达 victory direct-write sentinel，依照 handoff 在首个可审计 FAIL 后停止，未执行 D/E/F。

## 已确认结论

- 首次 F8 于 `2026-09-05T18:03:35Z` 执行，但 probe 把 profile inventory 的相对路径错误地通过 `Path.GetFullPath` 按游戏工作目录展开；`CopyProfileBackup` 的 root containment guard 检测到候选源路径越界并在第一次复制前抛出异常。
- 证据：`evidence/spike-inv003a/runtime/preflight-exception.json`；异常目标为游戏安装目录下的 `replays/latest.mcr`，并非实际复制来源。
- `protected-backup/20260905T180335.5943323Z/profile1/` 仅创建了空目录，没有复制存档文件。日志中 F8 start 与 exception 之间没有游戏 save write；没有安装 manager override、调用 run setup 或建立 replay state。
- 该次结果是 Phase 0 harness 缺陷，不是 transaction 候选的运行 FAIL/BLOCKED；保护基线仍未建立。
- 已把 inventory 规范化拆分为严格的相对路径处理，并在 copy 前额外拒绝 rooted/`..` 项；修正版重新构建为 0 warnings / 0 errors，且在确认游戏未运行后重新安装。
- 修正版于 `2026-09-05T18:09:09Z` 完成 F8 preflight，机器结果为 `AWAITING_USER_CONFIRMATION`、`blockingReasons=[]`。固定游戏版本/commit/DLL hash 全部匹配，实际只加载 `SPIKEINV003A`，artifact hash 与修正版一致。
- 只读解析得到 active profile id `1`、token `modded/profile1`、规范化根目录 `C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122\modded\profile1`；主菜单存在且 `RunManager.State == null`，Continue 可见并启用。
- Continue 摘要：Act index 0、floor 3、latest map row 3 / col 5、seed `8G50LMPKRAS0`、`NumReloads=3`、Standard/A0/单玩家；canonical JSON SHA-256 `C22F1B2C269FAA4E083B3B153C5EEC6BB108BE3ECB9956BE677AC475F710BA4D`。
- profile inventory 共 10 个文件；有效备份位于 `protected-backup/20260905T180909.3530219Z/profile1/`，所有 source/destination SHA-256 一致。随后再次只读复核，10 个原文件的 hash、长度、UTC mtime 和文件名 inventory 均与 baseline 一致。
- 原 manager runtime identity 为 `22198775`；各 profile-scoped manager 使用 `CloudSaveStore` wrapper，settings manager 使用 `GodotFileIo`。这只证明 wrapper 形态，不证明当轮 Steam Cloud 设置；仍等待用户明确确认 Cloud 已关闭及可见 profile 身份。
- profile 中存在游戏原生 `replays/latest.mcr`：4,807 bytes，SHA-256 `C611B13CA69ED09378ED92F6C145715E3506B83B1D8BBFB8C1B45C770F270FB2`，文件头记录 `v0.111.0` / `41cef1ea`。它是合法 native `CombatReplay` fixture 的强候选，但尚未由 probe 使用游戏 `PacketReader.Read<CombatReplay>()` 完整只读验证。
- `User-observed runtime result`：用户确认本轮启动前 Steam Cloud 已关闭，F8 时画面上的 active profile 确为“模组存档 1 号”，且显示的 Continue 与本次 preflight 解析对象一致。因此 Phase 0 的人工门禁已闭合。
- `User scope clarification`：用户说明 profile1 中不存在需要长期保护的真实 ordinary run；其真实普通对局位于其他存档槽。当前 profile1 Continue 只作为 active-profile `current_run.save` 隔离哨兵，profile1 的全解锁进度与全部文件仍为受保护状态。
- 该说明与原 handoff 中“在模组存档 1 号建立或指定一局 protected ordinary Continue”的语义有出入。按用户最新指示修订后，本实验仍固定 profile1、仍禁止切换 profile，并且不读取、复制或备份其他槽；最终结果不得声称直接验证了其他槽中真实普通对局，只能证明 active-profile Continue 哨兵及 profile1 进度的隔离。
- `0.2.0-fixture-gate` 在新进程中重新建立保护基线：artifact/版本/profile/Continue 均匹配，10 个文件备份 hash 全部一致；与上一基线相比只有正常退出产生的 prefs/progress backup mtime 更新，内容 hash 未变。
- F9 使用游戏进程内已初始化的 `ModelIdSerializationCache` 与 native `PacketReader.Read<CombatReplay>()` 成功反序列化 `latest.mcr`：4,807/4,807 bytes 完整消费、trailing bytes 0、version/commit 匹配、modelIdHash `1568834832`、baseline schema 20。F9 后 `RunManager.State == null`，10 个保护文件的 hash/length/mtime 和 inventory 全部不变。
- 该 fixture 的 `choiceIds=1`、`rewardIds=1`、`checksumData=1`，但 `events=0`。因此它是合法 native fixture，可覆盖 management/setup 类边界，却不能覆盖工作包要求的 direct-write victory sentinel；必须先由游戏原生 recorder 生成一个包含实际 combat events 的 fixture，不能由 probe 自建 dispatcher 或从 `current_run.save` 转换。
- `User-observed runtime result`：用户按要求以正常 Continue 完成一场战斗并回到主菜单，但习惯性领取了 14 gold 与 `POTION.BLESSING_OF_THE_FORGE`。游戏日志明确记录这两个 reward 领取；静态方法体证据表明 `CombatReplayWriter.RecordGameAction` 在 `CombatManager.IsInProgress == false` 时直接返回，因此战后 reward 操作不进入 combat replay。它们只改变隔离测试槽 profile1 的新基线状态；用户已明确该槽没有需要长期保留的真实 ordinary run。
- 随后的 F8 于 `2026-09-05T18:54:48Z` 建立新基线：Continue 为 Act 0 / floor 4 / row 3 col 5 / seed `8G50LMPKRAS0` / `NumReloads=4`，`latest.mcr` 为 20,441 bytes、SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`；profile1 十个文件的备份 source/destination hash 全部一致。
- F9 于 `2026-09-05T18:58:35Z` 将该新 fixture 原生反序列化为 `VALID_NATIVE_FIXTURE`：20,441/20,441 bytes 完整消费、trailing bytes 0、version/commit/model hash 全匹配，`events=11`、`checksumData=18`、`rewardIds=1`。F9 后 profile1 十个文件的 hash、length、mtime 全部与新基线一致，且 `RunManager.State == null`。该 fixture 已解除 playback/victory sentinel 所需的 `events>0` 阻断。
- 在准备 Phase A 时发现 preflight 对 `settings.save` 的路径理解不完整：生产 `SaveManager.ConstructDefault` 用 `GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath(null))` 作为 settings store，而 `SettingsSaveManager.SettingsPath` 是相对的 `settings.save`；实际文件因此位于 `...\\steam\\76561199141262122\\settings.save`，不是旧 preflight 检查的 `...\\modded\\settings.save`。实际文件当前为 4,658 bytes、SHA-256 `7C48E30AA05F5EC4E619EB501F8E0993DDE3CEB6EB3E2F118CE4B68F39B68287`。旧 F8/F9 均为只读，未改该文件，但最终 Phase 0 覆盖不完整，必须由修正版重新采集后才能运行 transaction。
- 源码已提升为 `0.3.0-management-gate`：修正 machine settings 精确路径与 Continue 文案，并增加 `MainMenuTextButtons` peer 中的 `Replay Isolation Probe` 入口和仅 Phase A 的管理页。打开/关闭页会比较 manager 身份、`RunManager.State` 与全部保护文件身份并写 `phase-a-management.json`；该 build 仍不包含 manager override 或 replay setup。构建为 0 warnings / 0 errors；在确认游戏退出后已安装，DLL SHA-256 `74B6811E18F1DF4DF0F56257A54789A5ADC7F6C77BF31342E17DA25C03BEE73A`，manifest SHA-256 `45E628CDF363AE326ACF267FB10F462A286A2BE9D45B0FE82B0DBF5B08A780B9`，仓库 artifact 与游戏 mods 副本逐文件一致。
- `0.3.0` 修正版 F8 于 `2026-09-08T01:17:47+08:00` 完成：`blockingReasons=[]`，active profile 1 / `modded/profile1`、主菜单、Continue、state-null 与固定 artifact 均匹配；profile1 十个文件的备份 hash 全部一致。保护集合现包含 10 个 profile1 文件、`modded/profile.save` 与真实账户级 `...\\steam\\76561199141262122\\settings.save`，共 12 个存在文件；当场复核 hash/length/mtime 零差异。新 settings baseline 为 4,658 bytes、SHA-256 `7C8609ECE787AA4E5288452949349314EC596A09D252B7A20F5E68693EFB489C`。
- `0.3.0` F9 于 `2026-09-08T01:20:36+08:00` 再次得到 `VALID_NATIVE_FIXTURE`：fixture hash `4DFB27...C631`、`events=11`、`checksumData=18`、20,441 bytes 全部消费、trailing 0、固定 version/commit 匹配、`RunManager.State == null`；F9 后全部 12 个保护文件的 hash/length/mtime 零差异。Phase A 已解锁。
- `User-observed runtime result`：用户确认 `Replay Isolation Probe` 管理页能够正常打开、显示 management-only 文案并关闭回到原主菜单，Continue 仍可见。
- Phase A 机器证据 `phase-a-management.json` 于 `2026-09-08T01:24:11+08:00` 得到 `PASS`：原 manager runtime identity `50223154` 全程保持，关闭后 identity restored=true，`RunManager.State == null`，内部 differences=0；Implementation Codex 随后独立复核全部 12 个保护文件，hash/length/mtime 零差异。Management-only phase 通过，transaction implementation 已解锁。
- Phase A 通过且用户退出游戏后，已实现 `0.4.0-transaction-probe`。transaction owner 不可嵌套/并发；从修正版 preflight 的 account-store 相对路径把 profile/prefs/progress/settings/current-run/history JSON 写入新的 `MockGodotFileIo`，随后以 `InitProfileId(null)`、`InitSettingsData`、`InitPrefsData`、`InitProgressData` 从内存 round-trip 重建完整 manager。它断言 Profile/Settings/Prefs/Progress 均不与原 manager 共享对象引用，并比较 Settings/Prefs/Progress canonical JSON 内容。
- transaction 在任何 replay setup 前安装 `SaveManager.MockInstanceForTesting(isolated)` 并断言对象 identity；从固定 `.mcr` 再次 native parse 后调用 `RunState.FromSerializable` 与 `RunManager.SetUpReplay`，返回后立即通过固定版本反射 setter 设置并断言 `ShouldSave=false`。Phase B 停在首个 watchable scene 等待唯一 F7 Stop；Phase C 只复用 DLL debug caller 的 native `CombatReplayEvent` 分发路径；D/E 分别在 state 已赋值但 launch 前、以及 watchable scene 后注入确定异常。
- 所有正常/异常路径进入同一 `finally`：标记 restore、等待 isolated current save task、若 state 非空则一次 `CleanUp(true)`、再次等待 isolated task、清除 override/恢复并断言原 manager、等待原 task、最后反射调用游戏私有 `LoadMainMenu(false)` 并等待新菜单帧。最终证据比较全部保护文件并记录 manager/state 恢复顺序。
- Audit 以 Harmony prefix 只观测、不替换正常 save 返回或业务分支：14 个 `SaveManager` write/delete/batch/profile 边界、41 个 `CloudSaveStore`/`GodotFileIo`/`SteamRemoteSaveStore`/`MockGodotFileIo` store 边界、14 个 metric/achievement/leaderboard/daily upload/unlock 边界，以及 override identity。transaction 活跃时 profile change、Quit、Continue/Singleplayer/Multiplayer 是明确 fail-closed UI gate。离线 patch smoke 成功 patch/unpatch 78 个方法。
- `0.4.0` 构建 0 warnings / 0 errors；在确认游戏退出后安装，DLL SHA-256 `C3F4D8723A5287EDD498F52970071D36B20AE3C704B09486B05018D75D1284DA`，manifest SHA-256 `FCEACB718053A17D76DC18CA1A493D26D24304A72D18D72B8686F95A92CD6A17`，artifact/installed hash 一致。
- `User-observed runtime result`：Phase B 成功进入预期 watchable combat scene；用户未进行战斗操作，按一次 F7 后几乎立即返回主菜单，Continue 可见。牌序/牌组与音频未观察，不属于 Phase B 判定范围。
- Phase B 机器证据 `phase-b-transaction.json` 于 `2026-09-08T01:55:26+08:00` 得到 `PASS`：`sceneLaunched=true`，无 operation/teardown exception、无 fail-closed/audit violation；checkpoint 顺序为 deep-copy ready → isolated manager installed → setup complete / ShouldSave false → watchable scene ready → normal Stop → original manager restored → main menu rebuilt。audit counters 只有 transaction=2、manager-override=2，原 manager/store 与 external submit 均为 0；isolated store 未发生写/delete，仅有一次 `GetFullPath` 读取辅助调用。内部 differences=0，Implementation Codex 独立复核全部 12 个保护文件 hash/length/mtime 亦零差异。Phase B 通过，`phase-control.json` 已单步推进到 C。
- Phase C 前重新执行 F8/F9：active profile、Continue floor 4 / `NumReloads=4` / canonical hash `045CBC98EF20AFAD59F66329B6C8CB8D2CE8EBCE6DCE58DF5F08036EA74EFE4B`、fixture `4DFB27...C631`、`events=11` / `checksumData=18`、state-null 与全部 12 个保护文件均保持，备份 hash 零差异。
- `User-observed runtime result`：Phase C 顺利进入目标战斗并自动进行了两个回合；第三回合开始后不再执行动作。约十几秒后探针从仍未结束的战斗自动 teardown 并返回主菜单。用户未进行任何额外输入；具体牌序不在本任务范围。
- Phase C 机器证据 `phase-c-transaction.json` 为 `FAIL`：`sceneLaunched=true`、`native-events-dispatched`，无 operation exception、无 teardown exception、无 fail-closed/audit violation，但 `victoryProgressObserved=false` 且 `isolatedProgressWriteObserved=false`。也就是说 11 个固定 native replay events 在该 native 分发路径中耗尽后，战斗仍未达到 `UpdateProgressAfterCombatWon → SaveProgressFile` 的直接写入哨兵；等待窗口到期后统一 finally 正常执行。
- 该 FAIL 不伴随保护破坏：audit counters 仍只有 transaction=2、manager-override=2，原 manager/store write/delete=0，external submit=0，isolated store write/delete=0；original manager restored=true、`RunManager.State == null`、内部 differences=0，Implementation Codex 独立复核全部 12 个保护文件 hash/length/mtime 零差异。故首个失败边界是“最窄 native fixture/事件路径不能覆盖必需 victory direct-write sentinel”，不是 save isolation 或 ordered teardown 泄漏。
- 按 handoff 明确规则“direct-write sentinel 未命中即 FAIL；首个失败后停止，不修补后继续矩阵”，`phase-control.json` 已锁为 `STOPPED`；Phase D setup failure、Phase E post-launch failure与 Phase F restart check 均未执行。最终结果不得升级为完整 PASS，也不得外推到其他存档槽中的真实普通对局。

## 推断与假设

- 无新增运行推断；transaction 候选仍沿用 `INV-003` 的待证伪假设。

## 仍然未知

- 如果扩展/更换 fixture 或改变事件完成等待语义，是否能到达 victory direct-write sentinel；本工作包禁止在首个 FAIL 后继续修补验证。
- setup/post-launch fault teardown 与跨重启检查结果，因为 D/E/F 按停止规则未执行。

## 变更

- 新增 `experiments/SPIKE-INV003A/`。
- 新增 `evidence/spike-inv003a/README.md`。
- 新增本报告。
- 未修改 Project Lead 管理文档或既有 SPIKE-001A 源码。

## 验证

- 各阶段均执行 `C:\Users\sanwe\.dotnet\dotnet.exe build experiments\SPIKE-INV003A\SPIKEINV003A.csproj -c Release`；最终 `0.4.0` 构建为 0 warnings / 0 errors。
- 最终 artifact 位于 `experiments/SPIKE-INV003A/artifacts/`，仓库副本与安装副本 hash 一致；固定身份见“环境与版本”。
- `runtime/preflight.json`、`fixture-validation.json`、`phase-a-management.json`、`phase-b-transaction.json` 与 `phase-c-transaction.json` 分别记录最终门禁和各 phase 结果。
- Phase A、B、C 前后均核对受保护文件；最终 12 个文件的 hash、length、mtime 与 inventory 没有实验内差异。
- Phase B 机器结果为 PASS；Phase C 机器结果为 FAIL，但 operation/teardown exception、audit violation、原 store write/delete 和 external submit 均为 0，原 manager 恢复且 `RunManager.State == null`。
- Implementation Codex 未启动或控制游戏；所有 F8/F9、管理页、播放及停止操作均由项目发起人手动执行。

## 偏离任务范围

- 没有扩大到 capture/dispatcher、重建控制或产品功能。
- 原 handoff 把 profile1 Continue 表述为需保护的真实 ordinary run；用户随后澄清它只是测试槽中的隔离哨兵。实验继续保护 profile1 的全部文件和全解锁进度，但结果不能外推为已直接验证其他槽中的真实普通对局。
- 生成有事件的 native fixture 时，用户在战后领取了奖励；该变化发生在新基线建立前并已记录，没有被计入 transaction 期间变化。

## 建议下一步

- Project Lead 验收本报告并决定是否另开工作包调查 native `.mcr` 为何在第三回合开始后耗尽、或调整 victory sentinel fixture。当前工作包不得继续 D/E/F、不得重试 Phase C、不得进入 reconstruction/capture/dispatcher。

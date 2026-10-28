# SPIKE-001A Implementation Codex Handoff / Investigation Report

## 任务

- Task ID：`SPIKE-001A`
- 任务范围：制作 disposable developer probe，验证 STS2 原生 `SerializableRun` 在首章第一场弱怪、玩家首回合输入就绪点的独立恢复基线。
- 明确非目标：不捕获/重放玩家操作，不实现正式 Replay 架构、UI、纠偏、seek、watchdog 或 live save 覆盖方案。
- 状态：Complete（有价值的失败；恢复基线未建立）

## 环境与版本

- 游戏版本：public-beta `v0.111.0`，commit `41cef1ea`，branch `v0.111.0`。
- `sts2.dll`：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- Probe：`0.1.0-spike`，schema `1`；最终 DLL SHA-256 `87D8C6CCC139B8F84B2ED2F260826C2D4002EA9657DD422CFDA02E8F06244FCA`；manifest SHA-256 `3F143BE100EE42FA41E15FAAFE2B12AE161A58AAC4457F44B36584653ACF6B24`。
- OS / runtime / loader：Windows NT `10.0.26200.0` x64；使用 .NET SDK `9.0.311` 构建；游戏内置 C# Mod loader/Harmony；游戏通过 Steam AppID `2868840` 启动。
- 最终有效会话实际加载模组：仅 `SPIKE001A`。`final-capture-and-attempt-03.log` 与 `final-attempt-04-cross-restart.log` 均记录 `Loaded 1 mods`；RunReplays、BaseLib、AscensionUnlocker 等均为 disabled。
- 场景：单人 Standard、Ascension 0、`CHARACTER.IRONCLAD`、游戏生成并由 fixture 固定记录的 seed `8G50LMPKRAS0`、Act 0、map row 1/col 5、`ENCOUNTER.SLUDGE_SPINNER_WEAK`、combat round 1、`PlayerTurnPhase.Play`。
- Fixture SHA-256：`7FCF9CB7142578DA3B4D735B555C22A94667A81C0366668FC1C446B5A6ED9ABC`。

## 做了什么

- 在 `experiments/SPIKE-001A/` 实现并构建一次性 probe；F8 生成 native `SerializableRun` fixture，F9 分阶段执行候选恢复链，F10 输出状态。
- 捕获前后采集结构位置、玩家/牌堆/敌人白名单、完整 `RunRngSet.ToSerializable()` 摘要和 readiness；生成 expected、actual 与机器可读 diff。
- 分别观测 `RunState.FromSerializable`、`RunManager.SetUpSavedSingleplayer`、`NGame.LoadRun` 和有限时长 readiness wait。
- 在捕获及每次恢复前后记录普通 `current_run.save` 的路径、长度、时间和 SHA-256。
- 执行同进程恢复一次，并在完整退出、通过 Steam 重启后执行跨进程恢复一次。
- 保存两次前置探针错误的证据：初版 Harmony 参数绑定错误；第二版错误假定首战 map row 为 0。最终版修正为 v0.111.0 的首战 row 1 后才生成有效 fixture。

## 已确认结论

### Fixture 捕获

- `RunManager.ToSave(CurrentRoom)` 成功返回；调用前后白名单与 RNG diff 为 `[]`。
- 捕获前后普通 save 的长度、哈希和时间完全一致。
- expected 的输入就绪条件成立：`Phase=Play`、属于玩家回合、actions enabled、未暂停、executor idle、run node 存在。

证据：`evidence/spike-001a/runtime/fixture.metadata.json`、`fixture.expected.json`、`fixture.run.json`。

### 两次恢复矩阵

| 尝试 | 独立性 | 候选链结果 | readiness | expected/actual | live save | 结果 |
|---|---|---|---|---|---|---|
| 03 | 与捕获同进程 | validate、parse、`FromSerializable`、禁用保存成功；`SetUpSavedSingleplayer` 抛出 `State is already set.` | 未进入 | 未采集 actual | 未变化 | 失败 |
| 04 | 完整退出后通过 Steam 重启，PID `57176 -> 44660` | validate、parse、`FromSerializable`、`SetUpSavedSingleplayer`、`LoadRun` 全部完成 | 1,478 ms 后就绪 | 14 项 diff | 被修改 | 失败 |

证据：`restore-attempt-03.json`、`restore-attempt-04.json`、两段最终 Godot 日志。

### 跨重启恢复的相等性

- 相同：act/map/room/floor/round、角色、HP、energy、敌人 ModelId/CombatId/HP/alive、readiness，以及除 `Niche`/`Shuffle` 外的其余 RNG 条目。
- 手牌不同：expected `[Strike, Defend, Bash, Defend, Strike]`；actual `[Strike, Defend, Strike, Bash, Strike]`。
- 抽牌堆多重集不同：expected 为 2 张 Defend / 3 张 Strike；actual 为 3 张 Defend / 2 张 Strike。
- `Niche` counter `1 -> 2` 且四个 state 均变化。
- `Shuffle` counter `9 -> 18` 且四个 state 均变化。
- 完整 RNG digest：`E1B5ECD8...50B83 -> AD2DDC08...9EA11`。
- 因此恢复后虽然画面和 readiness 均到达可操作战斗，结构/战斗白名单和完整 RNG 并不与 fixture 相等。

### 普通 save 安全边界

- 跨重启恢复前：49,152 bytes，SHA-256 `B62869B540ED37349F4B91757FB9BBB0DAC1722F2C809DD9DAC46C4B663F76C5`。
- 恢复后：50,587 bytes，SHA-256 `1136FAA3B221661111965AA443025420E797135B45C35587548D9A3A8CA0CB21`。
- Godot 日志把写入精确定位在 `RunManager.SetUpSavedSingleplayer` stage start 与 success 之间：`Wrote 50587 bytes to .../current_run.save`。
- 探针在该 stage 前已通过非公开 setter 将 `RunManager.ShouldSave` 置为 `false`，但不能阻止此写入。由此确认候选 API 不能按当前调用方式在不触碰普通 live save 的情况下验证。
- 按任务停止条件，不再重试、不覆盖/回滚 save，也不实现纠偏。

### 结论

`Restore baseline not established; keep SPIKE-001B blocked`

## 推断与假设

- 手牌和 `Shuffle` RNG 同时变化，表明 `LoadRun` 恢复过程中很可能重新执行了起手抽牌/洗牌，而不是直接保留 fixture 的战斗瞬时牌堆；这是根据 counter 与牌堆变化作出的推断，尚未通过更细阶段采样确认。
- `Niche` RNG 额外推进一次可能来自战斗节点重建期间的某个随机消费；本任务未扩大扫描范围来定位调用者。
- 用户观察到从主菜单恢复后主菜单音乐仍播放并与战斗音乐重叠。推测直接调用 `NGame.LoadRun` 绕过了正常菜单退出/音频切换流程；未做专项 instrumentation。

## 仍然未知

- 是否存在官方支持的“卸载当前 RunState 后同进程 setup”入口；当前直接调用 setup 在已有 state 时被拒绝。
- 是否存在不写 `current_run.save` 的官方恢复入口或额外保存开关。
- RNG/手牌偏移是 `SetUpSavedSingleplayer`、`LoadRun` 还是 readiness 前异步战斗初始化中的哪一个具体动作造成。
- 主菜单音乐叠播的准确音频生命周期缺口；它不影响本实验的失败判据。
- `ModManager.GetLoadedMods()` 在 probe initializer 执行时返回空集合，因此 JSON 内 `loadedMods` 为空；本次实际模组集合由 loader 完整日志独立核验。该初始化时序限制尚未修正。

## 变更

- 修改/新增：`experiments/SPIKE-001A/` 下 probe 源码、项目、manifest、README、收集脚本与 artifacts。
- 新增：`evidence/spike-001a/` 原始运行产物、失败前史和最终 Godot 日志。
- 新增：本报告。
- 行为变化：安装 disposable `SPIKE001A` 后提供 F8/F9/F10 developer hotkeys；不提供产品 UI。
- 风险：F9 的 `SetUpSavedSingleplayer` 会写普通 `current_run.save`，即使 `ShouldSave=false`。该 probe 不应继续用于恢复测试，完成验收后应卸载。
- 未修改：`CURRENT_STATUS.md`、`ROADMAP.md`、`TODO.md`、`DECISIONS.md`、`MVP.md`、`EXPERIMENT_PLAN.md`。

## 验证

- 构建：`dotnet build experiments/SPIKE-001A/SPIKE001A.csproj -c Release`，0 warnings / 0 errors。
- 捕获：有效 fixture 已生成并通过 build/schema/artifact/hash 验证；`ToSave` 前后 diff 为空。
- 恢复：尝试 03 和 04 均生成分阶段 JSON；04 明确为跨完整重启。
- 状态比较：尝试 04 生成 14 项机器可读 diff；未以画面相似代替验证。
- save 验证：捕获和尝试 03 未改普通 save；尝试 04 明确修改普通 save，因此触发停止条件。
- 未执行：第二次成功恢复；原因是第一次同进程候选链失败，跨重启恢复又同时出现状态/RNG diff 和 live save 写入，继续测试违反停止条件。

## 偏离任务范围

- Standard 模式 UI 不支持手动输入 seed，因此使用游戏随机生成的 seed，并在 fixture 中固定记录为 `8G50LMPKRAS0`。两次恢复仍来自同一固定 fixture。
- 首版测试说明错误写成预设 seed；后已按实际 Standard 流程修正。
- 用户报告的音乐叠播仅记录为 Unknown，未扩展为音频修复任务。

## 清理方法

1. 完全退出游戏。
2. 删除游戏 `mods` 目录中的 `SPIKE001A.dll` 与 `SPIKE001A.json`。
3. 可删除 `C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\SPIKE-001A` 专用运行目录；仓库内 evidence 保留审计副本。
4. 不删除或覆盖相邻 profile、save、history 文件。特别注意：本实验观察到 profile1 的 ordinary `current_run.save` 已由 setup 调用改写，探针没有对其执行回滚。

## 建议下一步

保持 `SPIKE-001B` Blocked。若 Project Lead 决定继续调查，应新开一个严格限定的 follow-up spike，只回答“是否存在不写 ordinary save 且不会重跑战斗初始化的官方恢复入口/调用顺序”；必须先解决 live save 安全边界，不能直接进入 action capture/dispatch。

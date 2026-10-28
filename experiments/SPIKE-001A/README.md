# SPIKE-001A disposable restore probe

本目录只用于 `SPIKE-001A`。它不是 Replay Mod 架构，不捕获或重放任何玩家操作，也不提供产品 UI。

## 固定范围

- STS2 public-beta `v0.111.0`，commit `41cef1ea`
- `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 单人 Standard、Ascension 0
- 角色：Ironclad
- seed：Standard 模式创建对局时由游戏生成；F8 会把该局实际 seed 固定记录到 fixture，后续两次恢复都使用同一 fixture
- 首章第一场弱怪，第一回合已经进入玩家输入就绪状态
- 只启用 `SPIKE001A`，不启用 RunReplays、BaseLib 或其他模组

探针启动时会再次检查游戏版本、commit、DLL hash 和已加载模组；任何漂移都会禁用 F8/F9 操作并写入 `environment.json`。

## 构建

需要 .NET 9 SDK：

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build .\experiments\SPIKE-001A\SPIKE001A.csproj -c Release
```

构建产物位于 `bin/Release/net9.0/`。将以下两个文件放入游戏 `mods` 目录：

- `SPIKE001A.dll`
- `SPIKE001A.json`

## 无歧义运行步骤

1. 确认游戏只启用了 `SPIKE001A`，完全退出后通过 Steam 库中的“开始游戏”重新启动（AppID `2868840`）。不要直接运行 `SlayTheSpire2.exe`。
2. 在新建的测试存档中新建单人 Standard / Ascension 0 对局，角色 Ironclad。Standard 模式不提供手动 seed 输入；使用游戏生成的随机 seed，探针会在 fixture 元数据中记录实际值。
3. 进入首章第一场弱怪（v0.111.0 内部地图坐标为 row 1；row 0 是开局 ancient/Neow 点）；等第一回合抽牌及自动效果全部结束、玩家可以出牌时，不执行任何操作。新存档教程流程没有显示独立的先古之民交互也不影响这个战斗边界。
4. 按一次 `F8`。探针会在 `RunManager.ToSave(CurrentRoom)` 前后分别采样，只有白名单状态和普通 save 文件都未变化时才落盘 fixture。
5. 按一次 `F9`，等待最多 15 秒。这是第一次独立恢复。
6. 完全退出游戏进程，再次通过 Steam 库启动；停留在主菜单，仍只启用同一 probe artifact。
7. 按一次 `F9`，等待最多 15 秒。这是跨完整重启的第二次独立恢复。
8. `F10` 只向游戏日志打印 probe 状态，不修改游戏状态。

每个恢复阶段都有独立 start/success/exception/elapsed 记录。任何异常、readiness 超时、白名单/RNG diff 或普通 save hash 变化都会停止该次尝试；探针不 retry、不纠偏。

## 输出、安全与清理

运行输出只写入 Godot 专用用户目录下的 `SPIKE-001A` 子目录。启动日志中的绝对 `Output:` 路径以及 `environment.json.outputDirectory` 是本机实际路径。

探针绝不复制、覆盖、移动或删除普通 `current_run.save`。恢复前会通过非公开 setter 把 `RunManager.ShouldSave` 置为 `false`，并在恢复前后读取普通 save 的路径、长度、时间和 SHA-256 进行验证。若无法禁用保存或文件身份发生变化，该次尝试判定失败并停止。

直接运行 exe 会使 Steamworks 报 `No appID found` 并进入启动错误流程。不要为本实验临时创建 `steam_appid.txt`；通过 Steam 启动才能保持与固定测试环境一致，并使用正确的 Steam 用户数据树。

收集仓库证据：

```powershell
.\experiments\SPIKE-001A\Collect-Evidence.ps1
```

清理时先退出游戏，然后删除游戏 `mods` 目录中的 `SPIKE001A.dll` 与 `SPIKE001A.json`；运行数据可删除 `environment.json.outputDirectory` 指向的整个 `SPIKE-001A` 专用目录。不要删除相邻 profile/save/history 文件。

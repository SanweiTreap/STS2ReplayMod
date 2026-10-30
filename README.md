# STS2 Replay Mod

《Slay the Spire 2》本地 Replay Mode 项目。目标是在正常游戏中低开销自动录制，并从独立入口观看，提供手动/自动播放、精细/整项前进、后退及后续时间轴导航。

目前为开发原型，固定目标 **STS2 public-beta v0.111.0**。**CONTENT-005五角色基础战斗支持已按代表样本阶段验收**：自动录制、独立原生回放、选择界面、已访问操作点恢复和原生续播；已测储君资源/可变牌、球、Osty首次创建/死亡重召及Doom结算。

已验收基线CONTENT-005：开场可见地自然进入选择，支持精细/整段推进及整体Back；所测EndTurn在途Back五次总到达约0.56–0.65秒，状态/RNG与退出核对通过，非发布性能保证。CONTENT-006已录成六份猎人多场普通胜利片并观察到资源承接/跨进程后续录；一次Continue分组缺陷已有修版待验证，新片独立回看与敌方召援仍待实测，整包未完成，具体版本见项目状态。

实际运行验证以A0首幕首排普通战斗的已支持机制为基线；尚未完成整局、全部内容/难度、一般敌方召唤/多阶段、任意文件点恢复或可移植导入。公开的是开发快照，不是正式发布包。当前状态/版本边界见[项目状态](project-management/CURRENT_STATUS.md)、[已验收报告](project-management/investigation-reports/CONTENT-005-report.md)与[当前任务报告](project-management/investigation-reports/CONTENT-006-report.md)。

## 目录

- `src/ReplayMod/`：当前模组及相关检查、准备工具；[构建与测试说明](src/ReplayMod/README.md)。
- `project-management/`：产品目标、状态、决策、调查报告和任务交接。
- `experiments/`：历史调查/探针源码。旧实验已结束，不能将它们直接当作当前测试模组运行。
- `tools/`：开发辅助工具，包括本地反编译缓存生成工具。
- `references/README.md`：第三方参考实现的来源、版本与许可证记录；下载的快照不提交。
- `evidence/README.md`：本地证据与仓库源码的边界；原始证据不上传。

## 构建当前模组

需要.NET 9 SDK，以及本地安装的匹配游戏版本。游戏提供的`sts2.dll`、`GodotSharp.dll`和`0Harmony.dll`从本机游戏目录引用，不包含在仓库中。

```powershell
dotnet build ./src/ReplayMod/ReplayMod.csproj -c Release
```

游戏不在默认Steam路径时：

```powershell
dotnet build ./src/ReplayMod/ReplayMod.csproj -c Release '-p:GameDir=D:/SteamLibrary/steamapps/common/Slay the Spire 2'
```

构建不等于对应版本已获游戏实测。安装与测试前先备份，游戏由用户手动启动/操作；项目工作方式见[WORKFLOW](project-management/WORKFLOW.md)。

## Git与本地数据

仓库用于保存项目源码和长期文档。构建输出、真实存档/录像、原始运行证据、恢复备份、第三方源码快照与游戏反编译缓存留在本地，不随Git提交。历史文档中部分证据链接因此只在原开发机可用；克隆仓库不会自动恢复这些数据。

Git不是游戏存档备份的替代品。仓库于2026-10-29按用户要求改为公开，展示开发源码与项目文档；原始录像/存档/日志、游戏反编译缓存和构建依赖仍仅留本地。当前开发快照不是正式发布包。

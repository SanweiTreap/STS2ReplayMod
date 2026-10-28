# STS2 Replay Mod

《Slay the Spire 2》本地 Replay Mode 项目。目标是在正常游戏中低开销自动录制，并从独立入口观看录像，支持暂停、前进、后退和后续的时间轴导航。

目前处于开发原型阶段，固定目标为 **STS2 public-beta v0.111.0**。已有单场战斗的真实录制、独立回放、选择界面和观看会话内的直接后退；铁甲、猎人、储君的部分机制组合已有实测。尚未完成整局录制、全内容覆盖、一般文件状态恢复或发布性能保证。

当前开发任务为 **PLAYBACK-001：累计Step与普通牌连续入队**。分支中的开发代码可能包含未实测完成的改动；首次导入Git是开发快照，不是正式发布版本。最新事实、限制和下一步以[项目状态](project-management/CURRENT_STATUS.md)及[当前工作包](project-management/handoffs/PLAYBACK-001.md)为准。

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

Git不是游戏存档备份的替代品。仓库初始可见性按用户最新要求设为私有；后续公开前需重新检查应公开的资料。

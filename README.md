# STS2 Replay Mod

《Slay the Spire 2》录像模组。单人对局自动录制，通过游戏内「百科大全 → 历史记录」观看。可手动推进、自动播放、后退，以及通过地图和战斗回合控件定位。

这是面向 **STS2 public-beta v0.111.0** 的开发快照。代表性单人三幕录像已从涅奥播到原生胜利详情，并可退出回同一条历史。跨房间及回合跳转已用代表样本验证，首次访问某些旧片落点仍需重建。多人、卡册导入导出与跨版本播放仍在规划。

## 构建

需要 Windows、.NET 9 SDK 和目标版本游戏。源码从本机安装读取 `sts2.dll`、`GodotSharp.dll`、`0Harmony.dll`。

```powershell
dotnet build ./src/ReplayMod/ReplayMod.csproj -c Release
```

自定义安装目录可传 `-p:GameDir=<游戏目录>`。见[构建、安装和开发测试](src/ReplayMod/README.md)。

## 仓库结构

- [src/ReplayMod](src/ReplayMod/) 是当前模组、桌宠素材及开发测试。
- [project-management](project-management/) 保留项目状态、设计、技术调查与交接。
- [experiments](experiments/) 是早期探针与验证源码的归档，日常开发从当前模组工程开始。
- [tools](tools/) 包含为本机游戏版本生成只读反编译缓存的辅助工具。
- [references](references/README.md) 记录第三方参考来源。
- [evidence](evidence/README.md) 说明本地运行证据的边界。真实存档、录像、日志、备份和游戏资源留在本地。

项目阶段与近期任务见[项目状态](project-management/CURRENT_STATUS.md)和[路线图](project-management/ROADMAP.md)。

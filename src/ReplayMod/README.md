# Replay Mod 开发

`ReplayMod.csproj` 是当前模组工程。目标游戏为 Windows 上的 STS2 public-beta v0.111.0。

```powershell
dotnet build ./src/ReplayMod/ReplayMod.csproj -c Release
```

游戏位于非默认 Steam 库时，加上 `'-p:GameDir=<游戏安装目录>'`。该目录需要含 `data_sts2_windows_x86_64` 和对应程序集。构建输出中的 `ReplayMod.dll` 与 `ReplayMod.json` 同放在游戏 `mods/ReplayMod/`，重启后从「百科大全 → 历史记录」进入有可播录像的条目。测试安装前先备份存档。

- `Art/UI001/` 是随模组打包的小鸡煲与充能球素材。
- `direct-tests/DirectContracts.csproj` 是离线开发合同检查。
- 历史实验和安装记录见[开发笔记归档](../../project-management/history/REPLAYMOD-DEVELOPMENT-NOTES.md)。早期定向准备脚本使用本地备份，已移出当前公开构建入口。

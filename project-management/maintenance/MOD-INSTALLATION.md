# 本地模组目录与历史安装归档

2026-11-01 D-141整理，2026-11-02随RUN-003验收更新。用户授权整理mod目录，明确SBWEI已不用。当前实际布局：

```text
Slay the Spire 2/mods/
  AscensionUnlocker/     # 保留目录，旧日志移出；Workshop安装未改
  GoldenCards/          # 原安装保留
  ReplayMod/
    ReplayMod.dll
    ReplayMod.json
```

目录整理时保留RUN-002/build-011原pair不改内容，未修改存档、profile选择或启用配置、未启动游戏。随后RUN-003沿此布局更新并实际确认识别；2026-11-03当前保留RUN-004/build-004，仅有一份ReplayMod manifest、无根目录第二份，版本身份见当前报告/安装收据。

归档位置：`C:/Projects/STS2ReplayMod-local/archives/mods/MODS-20261101-53fae5b1/`。移出9套已退休SPIKE的18个DLL/manifest、用户确认不用的2个SBWEI文件及159份AscensionUnlocker旧日志，共179文件/1,718,739 bytes；移动后逐hash读回。该目录另留现用pair整理前副本，`moves.json`保存精确原路径/归档路径/hash，`summary.json`为收据，`RESTORE.txt`说明恢复方式。不删除源码、实验报告、录像、项目证据或恢复点。

固定本体ModManager.ReadModsInDirRecursive递归读所有子目录的json，并以manifest所在目录查找同ID的DLL/PCK。因此每模组一个子目录有效，历史安装不能藏在mods/archive：仍可能被扫描。今后旧pair与探针只放游戏目录外；只在确需恢复时按精确映射操作，先保留新文件，不盲覆或同时放两份ReplayMod manifest。

`src/ReplayMod/Prepare-Demo.ps1`现在固定安装/备份现用pair到`mods/ReplayMod/`，收据增加InstallDirectory；发现根目录旧ReplayMod散装pair时先报明重复风险，不再制造第二份安装。脚本只做语法解析验证（0错误），本次没有运行其存档/配置准备流程或重编译DLL。

已确认：静态加载路径支持、文件/hash与唯一manifest、脚本语法；RUN-003/r1及后续人工运行已确认新布局实际识别，Prepare-Demo已支持run-003。没有为搬目录重打RUN-002业务矩阵，历史归档保持；当前是已验代表样本的开发版，尚未宣布正式发布。

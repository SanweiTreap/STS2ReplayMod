# REF-003 — HarmonyPatch 在 Replay Mod 中的适用性

日期：2026-09-27  
性质：工具评估，不是实验 gate 或架构决定。

这里把“HarmonyPatch 工具”暂理解为 HarmonyLib 2 的 `[HarmonyPatch]` 特性与运行时 patch 机制；若指另一个同名工具，需另给项目链接。当前本地 `0Harmony.dll` 的程序集版本为 `2.4.2.0`；已有 A4/008A 探针使用 Harmony patch。版本判断应以本地固定 DLL 和构建依赖为准，不混用 Harmony 3 才有的 API。

Harmony 可在不修改 STS2 原 DLL 的情况下，将 Prefix/Postfix 等观察逻辑挂到指定方法。对于下一窄诊断，它适合在固定 v0.111.0 方法入口/出口记录**调用是否发生、阶段、布尔标志与受限计数**，尤其是 `NRun._Notification`、`NGame.Quit`、`RunManager.CleanUp/WriteReplay`、`SettingsSaveManager.SaveSettings`、`CombatReplayWriter.WriteReplay`。目标是否可 patch、回调顺序和具体签名仍须逐个以固定 DLL/IL 与离线 smoke 验证。

下一诊断的安全用法：只读 Prefix/Postfix；不 skip original、不改参数/返回值/异常、不包装或替换 `Task`、不使用 Transpiler；所有 observer 代码捕获自身异常、限量持久化且不让异常逃逸到 production。patch 需有唯一 owner、可审计安装/卸载和与旧探针共存时的 loaded-set 门禁。Harmony 只能提供观测入口，不能自行隔离游戏存档、保证进程活性、解释 A4 历史调用，也不能让正式游戏无头运行变安全。以前 A3 的 observer 异常污染 production 和 A4 的计时器缺口，都不是“换用 Harmony”自动解决的问题。

来源：[Harmony 官方仓库](https://github.com/pardeike/Harmony)、[Harmony 2 patching](https://harmony.pardeike.net/v2/articles/patching.html)、[Harmony 2 annotations](https://harmony.pardeike.net/v2/articles/annotations.html)、[Harmony 2 basics](https://harmony.pardeike.net/v2/articles/basics.html)。

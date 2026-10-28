# 外部参考源码

本目录保存调查用的第三方源码快照，不代表本项目采用其架构或代码。

## RitsuLib v0.5.18

- 上游仓库：https://github.com/BAKAOLC/STS2-RitsuLib
- 项目发起人于 2026-09-04 提议评估该社区常用库。
- 上游公开说明将其定位为 STS2 共享框架，覆盖生命周期、Harmony patch、持久化/临时 mod data、设置 UI、运行时 UI、诊断和兼容辅助，并声明 MIT License。
- 固定 release/tag：`v0.5.18`；annotated tag `1be7d64b932e4da31ee5349c7c0d632999115028`；peeled commit `f224961a9392e010335da092240b90ee8235317f`。
- 固定源码归档：`STS2-RitsuLib-v0.5.18.zip`；SHA-256 `E6B599CAB85C3BFE43C061B8B1DDDC93B511EA83FE52376F916FB013FD7DF9D3`。
- 许可证副本：`STS2-RitsuLib-v0.5.18-LICENSE.txt`；MIT。
- manifest version `0.5.18`，主构建目标包含 STS2 API `0.111.0`；本次只固定源码，未安装 runtime artifact。
- INV-003 已确认：RitsuLib 可作为 lifecycle、patch diagnostics、settings、通用 UI、localization 和 compatibility gate 的候选外围依赖；没有通用主菜单 peer-entry API，`SaveScope.InMemory` 只隔离 RitsuLib mod data，不能提供游戏 `RunManager`/`current_run.save` 隔离。
- 当前决定：不把 RitsuLib 作为下一隔离实验或 MVP 的必要运行依赖；核心隔离成立后再单独评估外围采用范围。

## RunReplays

- 上游仓库：https://github.com/boardengineer/RunReplays
- 固定版本：Git tag `may-2026`
- GitHub 显示的对应 commit：`05b0edd`（当前仅取得页面展示的短 hash）
- 上游 manifest 版本：`v0.2.0`
- 归档文件：`RunReplays-may-2026.zip`
- 解压目录：`RunReplays-may-2026/`
- 归档 SHA-256：`654B9E416158943440C9C08BAF1D4FAF78A3581D3926A64D19A4F0220027C7DC`
- 获取日期：2026-08-21
- 许可证：MIT，见快照内 `LICENSE`

## 证据限制

- 这是 GitHub tag archive，不含 `.git` 历史。
- GitHub release 说明该 tag 为 Version 0.2.0，加入 BaseLib 依赖，并将模组与 sample runs 更新到当时的 latest public release。
- 尚未证明该 tag 就是此前在 STS2 v0.107 上验证 sample replay 时使用的版本。
- 不得把此快照在源码层面的发现自动外推到早期 release、当前 master 或 public-beta v0.111。

## 当前 Workshop artifact 对应源码

- Workshop item `3759973128` 的 DLL ProductVersion 指向 commit `4b1fcfed256888d95e012b0af85de585eeb6734a`。
- 固定源码归档：`RunReplays-4b1fcfed.zip`。
- 归档 SHA-256：`2C2805358DA566694A02CBE97BCEE64A72AF18FF68C3A76EA5C140A4A0E1286B`。
- 解压目录：`RunReplays-4b1fcfed256888d95e012b0af85de585eeb6734a/`。
- 该源码 manifest 为 `v0.2.0`，但 Workshop manifest 为 `v0.1.2`，运行日志自报 `0.1.0`；版本号不能单独作为 artifact 身份。

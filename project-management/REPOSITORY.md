# Git仓库与协作

更新：2026-10-29。用户最新明确要求将现有仓库改为public；已通过GitHub API修改，并以关闭凭据的git ls-remote确认公开可读。此前私有要求保留为历史，由本次指令覆盖。

- 仓库：https://github.com/SanweiTreap/STS2ReplayMod
- 所有者：SanweiTreap；可见性：public。
- 本地目录：C:/Projects/STS2ReplayMod；主分支：main。
- origin：https://github.com/SanweiTreap/STS2ReplayMod.git

首次导入是现有工作目录的开发快照，PLAYBACK-001仍在进行，不是对应构建已全部通过的发布标签。Implementation可同时继续代码工作；暂存之后的新改动保留在工作树，不能为追求clean状态reset/clean或覆盖它们。

源码、项目管理文档、历史实验源码与开发工具进入版本控制。`.gitignore`排除原始evidence、运行日志、存档录像、恢复备份、下载的第三方源码/归档、编译输出和游戏反编译缓存；references来源说明与许可证副本、evidence说明页保留。历史文档中的本地证据链接不保证在clone后可用。Git远程不替代原测试账号的存档备份。

凭据由本机Git Credential Manager管理，不写入文件、remote URL或项目文档。现有Git作者配置沿用，不改全局配置。公开前对当时677个跟踪文件/19次历史检查了排除材料路径和常见凭据格式，未发现命中；这不是全面安全审计。当前公开的是开发源码和管理文档，既有忽略范围不变，原始存档/日志/反编译缓存等不上传。用户未要求变更项目许可证，本次仅改变仓库可见性。

日常在任务自然停点提交相关变更，避免把未完成源码描述为实测发布。同一时间仅一个对话进行stage/commit/push；Git初始化与首次推送由PM协调完成后，Implementation可正常提交其任务变更。普通代码迭代不因此增加阶段审批；不force-push、不重写别人历史或清理他人工作树。

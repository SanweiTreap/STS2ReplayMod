# DET-001 candidate-r3：涅奥资格修订

2026-10-23，D-082。结论：`OFFLINE_NEOW_REVISION_VALIDATED / RUNTIME_TARGET_PENDING`。

用户报告约重开十次、每次未选择Neow选项、已正常退出。机器收据`evidence/det-001/runtime/r2-exit-before-r3.json`确认game0、原测试scope、exact可恢复r2 pair、r1/r2/r3 activation不存在、累计Arm0/3。停止继续刷金珍珠，下一次只准备一局。

## 精确范围

独立`experiments/DET-001-candidate-r3/`，Mod/assembly DET001，manifest0.1.0，owner `STS2ReplayMod.DET001.FirstReshuffle.candidate-r3`，private `user://DET001/candidate-r3/`。新增必填Neow实例资格导致runtime schema2；r1/r2/schema1源/产物/记录保持，不能拼接旧CP/A/B。

恰有BurningBlood加一个：GoldenPearl、SilkenTress、BoomingConch、LavaRock、WingedBoots、FishingRod。名称、普通Neow路径、取得/战斗hook、限定条件及固定证据详见`evidence/det-001/candidate-r3/neow-eligibility-matrix.md`。用户要求的五项全部在本次**首场普通弱怪、R3停手、不胜利/领奖、直接连接节点**窗口内可证实兼容；不推广到其他场景。

## 实现和标准

修改仅资格Core、实际读适配器、NativeCheckpoint的Neow实例绑定及revision/schema身份。实际读取四个被选遗物的使用标志/计数，完整序列化实例与CP严格绑定，各稳定点资格不变；缺字段、未知/混用/已使用及失配拒绝。无制作/取得遗物、改牌、改RNG、改存档。

r2非泛型Shuffle.MoveNext唯一原call插桩及所有业务执行源码保持（仅身份字段例外），没有回到共享泛型补丁。新增游戏引用相对r2仅5项：RelicModel.ToSerializable、IsUsedUp及LavaRock.HasTriggered、FishingRod.CombatsSeen、WingedBoots.TimesUsed；均只读。原三个正常B效果调用、SaveRun wrapper、Task观察、timer、20字段/30帧/floor、五点四操作、E0…E4/完整九步及第一差异停止保持。

## 离线验证

最终`evidence/det-001/candidate-r3/validation-final-v2/`：Release0 warning/error，**144/144**；11实际目标metadata patch/unpatch、15普通callback及1未变callsite transpiler；真实Harmony同形caller、原返回/异常/泛型上下文、私有字符串与值类型utility和原指令保留回归通过。未执行真实CardModel效果或启动engine。

新增每个ID均有完整typed资格→seal→五点/四操作→E0…E4→complete/Ready→same CP/fresh B正向（全部明确synthetic），以及错误/未知/混合遗物、无BurningBlood、CP ID/实例不符、使用标志/计数、Persistent谎报、点间实例变化负向；必填Neow字段逐项覆盖。继承原72项并保持实际Stomp样本不合格、FTUE/readiness/Task/RNG/seq3-4等负向。

prevalidation-v1与validation-final-v1仅为构建过程，保留不冒充最终交付；最终v2包含把实际多件遗物分类为准备不足的修正。原sealed r2 72、r1 73、DET51/6公开输入、001B158文件hash保持；未重跑无变化的旧业务测试，未改PM文档。

复现：现有.NET9执行`experiments/DET-001-candidate-r3/Invoke-Validation.ps1 -OutputDirectory <r3证据目录下新目录>`。无安装新依赖，无账号/private读取。产物身份及完整小型索引见`evidence/det-001/candidate-r3/delivery-2026-10-23.json`；只读校验入口`Verify-R3Delivery.ps1`。

## Confirmed / Inferred / Unknown

Confirmed：六个精确ID及名称/方法体、实际读适配器/CP绑定实现、上述离线结果、r2正常退出/Arm0。

Inferred：在保持普通十卡、直接普通弱怪和本次停止窗口时，五个新增项不会增加/改变目标重洗路径；原R3首次重洗推导仍成立。

Unknown：真实r3首帧、实际资格/CP、CardModel目标call/scope、五点四操作、真实九步推进及A/B。离线正向不是实际结果。

按D-082/D-081可在game0、原scope、exact旧live可恢复、sealed身份和有效only DET001回读通过后精确更换本包两文件，再逐阶段请求用户操作。下一局无任何允许项即停；正式Arm跨revision最多3，不增加预算，不推进STATE/PERF/产品。

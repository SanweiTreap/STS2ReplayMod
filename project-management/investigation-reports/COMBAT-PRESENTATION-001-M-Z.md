# COMBAT-PRESENTATION-001 · M–Z 源码审计

2026-11-06，v0.111.0。源码锚点相对于 `C:/Example/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src/`，怪物表行号位于 `MegaCrit.Sts2.Core.Models.Monsters/<类名>.cs`。本册覆盖 M–Z 的 61 个怪物文件的招式与表现入口，重点下钻共用命令、已知卡顿和独有表现。只读索引本机 `SlayTheSpire2.pck` 的关联场景，安装 DLL 的 SHA-256 前缀 `0861BFA1` 与固定版本一致。Spine 和 FMOD 资源内的实际播放行为以及间接 Power 分支仍可按问题补查。无运行或业务源码改动。

## 确认的共用边界

- **普通攻击可以共用。** `AttackCommand.Execute`（`Commands.Builders/AttackCommand.cs:549,582,588,622,667`）先做攻击前 hook，触发动画及其等待，再跑 `AfterAttackerAnim`、受击表现、伤害与后续 hook。`CreatureCmd.TriggerAnim:968–999` 先触发动画再做 `CustomScaledWait`。现有 `NativePhaseRetirement` 已识别这两处共用计时，收束时仍继续执行原生后半段。怪物自己写的 `Cmd.Wait` 或 helper 内等待，只在现有显式识别的调用点才受处理。把所有等待一刀切会失去时序语义。
- **贪食者有两组独立等待。** `TheInsatiable.LiquifyMove:114–144` 内的 `0.5s`、`0.75s` 分别分隔液化动画、尖叫和陷沙生效；`1s` 是插入六张牌后对本机展示的停顿，之后才写 `HasLiquified`。这三处都没有进入现有 `PlainWaitAfter` 名单，且不能省略加能力、随机插牌、预览或最终写标记。另一个链 `SandpitPower:98–122,147–199` 含吞食演出 `0.5s` 后强制击杀，以及位置更新的真实 Tween `AwaitFinished`。后者当前也不在牌堆 Tween 的加速白名单。现有日志的 2586 ms 不能直接分摊给单个调用点。
- **普通战斗和专曲胜利 Back 需要不同恢复。** 原生 `CombatManager.StartCombatInternal`（`:584–597`）按 `HasBgm` 选择专曲，随后刷新全局 `Progress`；`EndCombat` 结束时再次刷新（`:1357–1358`），`NRunMusicController.GetTrack:204–229` 在战斗房间但战斗未运行时返回 `CombatEnd`。`EncounterModel:185–187` 默认 `CustomBgm` 为空，`MechaKnightElite` 和 `InfestedPrismsElite` 沿用该默认，`TheInsatiableBoss:9` 提供专曲。`NativeSeek.cs:461–478` 胜利 Back 只在 `CustomBgm` 非空且非实验体时显式重启；`CreaturePresentation.RestoreEliteMusic:54–74` 在前后均未进二段时提前返回。普通 BGM 在这条路径缺少战斗恢复后的 `Progress` 刷新。候选是在战斗状态恢复后统一恢复原生战斗音乐，复用原生控制器；专曲和阶段参数保留独立职责。听感仍待验证。
- **曲目、曲内参数和循环音效有三个所有者。** `CreaturePresentation.MusicUpdated:76–83` 只采实验体参数；贪食者、女王、灵魂鱼、Vantom、瀑布巨人、帝王蟹等写自己的参数，重启曲目本身不等于恢复目标阶段。`SfxCmd:30–78` 把全局 `PlayLoop` 交给音频管理器，把 Creature 循环交给 `NCreature`；两者都可能跨越一次操作，需按目标状态恢复，和 `NativePhaseRetirement` 已追踪的在途 one-shot 分开处理。原生 `SfxCmd.Play` 另受 `CombatManager.IsEnding` 限制，应在判断漏声时区分该原生条件和回放退役。
- **普通 VFX、可复用节点和全局镜头也分层。** 机械骑士重劈在 `AfterAttackerAnim` 调 `NCombatRoom.RadialBlur`、`NGame.DoHitStop` 后继续原生伤害；缩小甲虫和 Vantom 也走同类镜头入口，`NativeCombatCamera` 已挂共用收束，先按同一路径核。Myte 和 SnappingJaxfruit 的专用节点接收目标，后由 Spine 事件在复用节点上定位或启动轨迹／粒子（`NMyteVfx:84–108`、`NSnappingJaxfruitVfx:123–163`）。帝王蟹右臂更由 `NKaiserCrabBossBackground:132–227` 的背景 Spine 轨持有演出，内部用带取消令牌的 `Cmd.Wait`，不经当前 `PlainWaitAfter` 挂的两参数签名。复原同一只怪的通用动画循环也不能自动复原背景轨、独立 Track 1 或未结束 Tween。

## 逐文件索引

表中「共用」表示模型内所读表现走上述通用出招、施法或 VFX／SFX 入口；原生伤害、能力、卡牌及选择任务仍按顺序执行。特殊行是源码边界和覆盖候选，尚未当作已复现的回放 bug。

| 原生模型 | 招式／表现锚点 |
| --- | --- |
| MagiKnight | `DampenMove:81–95` 能力后对白和 0.5s；`MagicBombMove:114–139` 写场景目标再共用攻击。 |
| Mawler | `:48–66` 普通攻击、Roar 施法、两段攻击，共用。 |
| MechaKnight | `:101–140` 冲锋特效、重劈独立镜头、喷火 1.5s 再加 Burn；胜利 BGM 走普通战斗分支。 |
| MultiAttackMoveMonster | `:21–36` 五段单一测试式攻击；只找到类型注册，正常遭遇未定位。 |
| MysteriousKnight | `:10–15` 继承 `FlailKnight` 招式、入场额外能力；`TheLanternKey:54` 是真实事件入口，招式沿 A–L 的基类记录。 |
| Myte | `ToxicMove:65–77` 配置 `NMyteVfx` 目标再 Cast、生成 Toxic；`NMyteVfx` 由 Spine 事件定位。 |
| Nibbit | `:96–114` Butt、Slice、Hiss，常规攻击和施法。 |
| Noisebot | `:31–67` 入场视觉初始化；Noise 生成牌入弃牌／随机入抽牌堆，预览之后等待 1s。 |
| OneHpMonster | `:18–28` 隐藏意图空招；只找到类型注册，正常遭遇未定位。 |
| Osty | `:37–67` 自身空招与原生 Animator；`OstyCmd:65` 用 `AddPet`，归友方演出。 |
| Ovicopter | `:48–99` 全局 idle 循环在入场／移除启停，产卵动画后真实生成各个 ToughEgg。 |
| OwlMagistrate | `:84–147` 飞起等待 1.25s，启用 Creature 循环；俯冲先关循环、打击后等待 1s，回地面与飞行还用不同的受击／死亡声。 |
| PaelsLegion | `:29–42` 皮肤和空招；同名遗物 `:223` 经 `AddPet`，归友方演出。 |
| Parafright | `:35–57` 入场应用 `IllusionPower`、共用攻击，`TheObscura.IllusionMove` 创建。`IllusionPower` 与 EyeWithTeeth 共用留队 `REVIVE_MOVE`；`:77–78` 还按 `IsReviving` 分流受击动画。055 只放行 EyeWithTeeth，Parafright 仍缺同机制覆盖。 |
| PhantasmalGardener | `:79–155` 皮肤与多招式缩放，`NCreature.SetDefaultScaleTo` 自有 Tween；Enlarge 先演出再生效和缩放。 |
| PhrogParasite | `:57–77` 多击 via `AttackCommand`，Infect 手动挂 `NWormyImpactVfx` 后生成卡。 |
| PunchConstruct | `:71–118` 入场能力、准备施法、单双拳共用攻击。 |
| Queen | `:114–129,166–238` 附加 Spine Track 1、对白、`queen_progress`、SoulNexus 死亡；关注双方死亡对轨和音乐的影响。 |
| Rocket | `:87–170` 帝王蟹背景右臂持有受击、死亡及攻击演出；蓄力循环和 `kaiser_crab_progress` 单独恢复。 |
| ScrollOfBiting | `:69–124` 皮肤变体、咬／多击、Cast 后效果，共用。 |
| Seapunk | `:46–88` 入场能力、踢／连踢、施法；场景脚本负责泡泡和水草。 |
| SewerClam | `:29–60` 入场能力，Pressurize 施法后叠加，Jet 共用攻击；珊瑚缩放另有 Tween。 |
| ShrinkerBeetle | `ShrinkMove:46–54` 施法后房间径向模糊再上效果，共用镜头边界。 |
| SingleAttackMoveMonster | `:19–35` 单一测试式 Poke；只找到类型注册，正常遭遇未定位。 |
| SkulkingColony | `:53–90` 入场能力，重复 Zoom 共用动作及 Inertia 分支。 |
| SlimedBerserker | `:66–90` Vomit 后生成十张状态牌、Hug 后能力，常规动画等待与牌堆结果分开。 |
| SlitheringStrangler | `:57–93` Constrict 施法，Thwack 和其余攻击共用。 |
| SludgeSpinner | `:38–94` 喷油／拍击共用；状态效果与攻击演出按原序。 |
| SlumberingBeetle | `:72–107` 入场睡眠粒子和 Creature 循环，Wake 移除睡眠效果后换动画；另有原生死亡订阅。 |
| SnappingJaxfruit | `:33–67` Creature idle 循环；Energy Orb 的目标传给复用 `NSnappingJaxfruitVfx`，由事件启动轨迹与粒子。 |
| SneakyGremlin | `:46–70` Spawned 用起身触发，后续普通撞击。 |
| SoulFysh | `:81–106,130–208` `beckon` 跟随卡堆变化，`soulfysh_progress` 随招式和死亡更新；Beckon／Gaze 有显式节奏和牌预览等待。 |
| SoulNexus | `:34–60,82–104` 死亡不淡出，死亡事件清除 Spine Track 1；其攻击共用。 |
| SpectralKnight | `:63–88` Hex 施法及两种攻击，共用。 |
| SpinyToad | `:74–87` 变刺形态、Thorns；爆炸伤害和移除 Thorns 后仍有 1s 尾停顿。 |
| Stabbot | `:29–56` 入场、一次共用攻击与减益。 |
| TenHpMonster | `:18–28` 隐藏意图空招；只找到类型注册，正常遭遇未定位。 |
| TerrorEel | `TerrorMove:110–120` 施法、0.7s 与额外 scaled wait 后执行效果；普通攻击共用。 |
| TestSubject | `:168–183,223–271,326–342` 死亡／复生阶段 BGM、缩放／配色和背后烧灼；现有 Back 已专门处理复生节奏和专曲参数。 |
| TheAdversaryMkOne | `:29–74` Artifact 入场、普通攻击和多发 Barrage，共用。 |
| TheAdversaryMkThree | `:31–76` 同族模型独立数值与多发攻击，共用。 |
| TheAdversaryMkTwo | `:33–78` 同族模型独立数值与多发攻击，共用。 |
| TheForgotten | `:37–79` 入场与 Miasma／Dread，共用动画施法及攻击。 |
| TheInsatiable | `:83–89,114–172` 三个直接等待、死亡专曲参数；另查 `SandpitPower` 的吞食、定位 Tween 和击杀边界。 |
| TheLost | `:31–70` Smog 施法、Eye Lasers 共用多击。 |
| TheObscura | `:78–110` 原生召唤 Parafright，等待后才创建；其余攻击和施法共用。 |
| ThievingHopper | `:158–160,184–246,267–288` 偷牌时独立节点和真实卡堆移除；悬浮循环、逃跑前等待 1.5s、真实逃跑。 |
| Toadpole | `:59–110` 皮肤、喷刺多击、旋转／施法，共用。 |
| TorchHeadAmalgam | `:42–95` 入场及强弱 Tackle 系列，共用。 |
| ToughEgg | `:110–173` 有条件入场和 Hatch 动画，同一对象孵化后再设最大与当前 HP；`HatchPower:18–23` 在回合边界递减。 |
| TrackerRubyRaider | `:40–56` Track 施法、Hounds 共用多击。 |
| Tunneler | `:97–146` 地下形态和骨骼定位，Below 动画后 1s 再伤害；醒来、眩晕独立姿势。 |
| TurretOperator | `:35–78` Crank、五发 Unload；多击与单次动画的共用攻击路径。 |
| TwigSlimeM | `:49–78` Clump 攻击；Sticky Shot 设骨骼目标、施法后产生冲击及状态牌。 |
| TwigSlimeS | `:23–38` 普通 Tackle。 |
| TwoTailedRat | `:148–184` 备援动画、额外 0.5s 再真实生成单位，计数、槽位和生成必须保留。 |
| Vantom | `:134–215` 单独 Spine Track 1、缩放 Tween、显式等待、房间 blur／hit-stop 及分段曲目参数。 |
| VineShambler | `:61–85` Vines 经共用 AttackCommand 按目标分别生成底部 VFX，Swipe 多段，Chomp 普通攻击。 |
| WaterfallGiant | `:160–189,294–328` 环境循环／参数、Boss 曲阶段、独立 buildup 轨；濒爆触发含真实 HP 更改，爆炸最后走 Kill。 |
| Wriggler | `:80–99` 咬击与 `NWormyImpactVfx` 工厂；Wriggle 手动挂同种效果，后生成牌。 |
| Zapbot | `:29–55` 入场视觉初始化及普通 Zap，继续与机器人共用形态区分。 |

## 场景脚本分组

| 所有权 | 从 PCK 场景追入的 C# |
| --- | --- |
| Spine 事件启动／停止的复用发射器 | `NMechaKnightVfx:135–193` 喷火和引擎；`NParafrightVfx:69–92` 与 `NTheObscuraVfx:69–92` 幻象粒子；`NPhantasmalGardenerVfx:69–92` 喷吐；`NPhrogParasiteVfx:132–170` 感染和死亡爆裂；`NQueenVfx:83–114` 攻击喷射；`NSeapunkVfx:94–117` 泡泡和水草；`NSkulkingColonyVfx:95–123` 受击／死亡／烟团；`NSlimedBerserkerVfx:105–143` 粘液；`NOilSpillVfx:143–273` 喷油和滴落；`NSpectralKnightVfx:100–135` 火焰；`NSpinyToadVfx:89–100` 爆刺；`NLostAndForgottenVfx:89–120` 共用尘粒；`NTheInsatiableVfx:144–247` 唾液和爆发；`NAmalgamVfx:216–368` 火炬、激光和死亡。需要区分目标检查点应持续的效果与被放弃招式的临时尾巴。 |
| 节点、材质与轨道上持久状态 | `NMyteVfx:84–108` 与 `NSnappingJaxfruitVfx:123–168` 持有目标，后者另有轨迹；`NSewerClamVfx:143–149` 对珊瑚缩放延后开 Tween；`NSoulFyshVfx:129–158` Tween 改两个 shader 参数；`NSoulNexusVfx:120–184` 控制三路轨迹和火焰；`NVantomVfx:182–195` 材质溶解完成后回写 Spine Track 1；`NTestSubjectVfx:409–464` 两个烧灼轨和多段 Tween；`NWaterfallGiantVfx:275–585` 按蒸汽阶段和死亡更新持续状态；`NKaiserCrabBossVfx:205–376` 以背景轨驱动粒子和爆裂。 |

通用 `VfxCmd` 或 Attack 回调有独立节点归属。上表是场景内复用节点、材质或背景轨，不会通过生成独立命中特效自动登记。对其进一步核对是否由现有节点重建自然清理、可由目标状态重放，或需共用退役入口。

优先合卷时对照四类改动：普通战斗音乐恢复、按直接调用点声明的可缩短演出等待、持久声音／多轨状态恢复、可复用节点与场景级特效退役。每类先定共用入口和会改变状态的边界，再用一个有区分力的实机样本核呈现。

## 两分册复核：计时、音乐和共用收束

范围为固定版 121 个怪物模型文件内直接调用 `Cmd.Wait` 或 `Cmd.CustomScaledWait` 的行，另核分册指出的 Power 和场景 helper。模型层共 25 个文件、54 个直接等待调用点（42 plain、12 scaled）。在现行回合撤销条件下，11 个调用点已有明确绑定，分别为 `BygoneEffigy:62,79,108`、`CeremonialBeast:183,185,201,204`、`DevotedSculptor:46,49` 和 `TestSubject:260,262`。另 43 个未列入该调用点集合。这个数不含共用 `TriggerAnim`、`AttackCommand` 或其他 helper 内部计时，也不代表 43 个运行故障。

| 未绑定调用点 | 原生动作边界与取样建议 |
| --- | --- |
| `CalcifiedCultist:82`、`DampCultist:82`、`FakeMerchantMonster:143`、`GremlinMerc:80`、`MagiKnight:95` | 对白或施法留白。保留各自触发条件、一次性对白标记和随后攻击／上能力。一个会有实际后续动作的 Cultist 样本可验证共用收束。 |
| `FrogKnight:120`、`SpinyToad:87`、`CeremonialBeast:174`、`Noisebot:67`、`SoulFysh:151,172`、`TheInsatiable:141` | 攻击收尾或生成牌预览。保持下一段原生动作顺序，Noisebot 和贪食者的随机插牌、贪食者最终 `HasLiquified` 必须发生。贪食者可覆盖预览与后续标记。 |
| `Axebot:114`、`CeremonialBeast:226,228`、`HauntedShip:76,78`、`SoulFysh:135,137`、`TerrorEel:114,116`、`TheInsatiable:118,120` | 招式先触发演出，之后上能力、生成牌或进入下一段。只释放已经确认的定时器，原生方法继续执行。贪食者样本同时跨两段演出和原生加牌。 |
| `DecimillipedeSegment:238`、`Tunneler:130`、`TwoTailedRat:183` | 多节攻击、钻地攻击、召援的先导计时。伤害或生成仍要原生提交。千足虫同时存在场景自有落石尾声，另按节点所有权处理。 |
| `FatGremlin:71,75`、`ThievingHopper:228,273,287`、`OwlMagistrate:133,147` | 计时跨对白、偷牌显示、飞行／落地或逃跑。独立核对可交互状态、持牌显示、持续音与原生 Escape。逃跑功能验证可顺带取样实际等待。 |
| `CubexConstruct:135,142,147,155,160`、`Vantom:140,161,180,187,198` | 多段计时夹在蓄力／攻击、附加轨、材质 Tween 或持续音变化之间。先审目标点持久状态和旧效果停止，再决定哪些间隔可以释放。 |

另有三种等待不能按上表的纯计时处理。`KnowledgeDemon:162` 等真实选咒并行任务；`SandpitPower:199` 等位置 Tween 完成，需保留原生信号与落点；`NKaiserCrabBossBackground:132–227` 的带令牌计时连着共享背景轨，现有 plain patch 只拦两参数重载。`StockPower:24–33` 和 `InfestedPower:24–48` 都在真实生成后另起显形任务。后者的延迟回调还调用 `TriggerEliteSecondPhase`。单纯缩短主动作不会自动让旧显形任务停止，跳转时应按原生单位和动作代次归属这些回调。

| 音乐状态 | 原生写入与当前覆盖 |
| --- | --- |
| 普通战斗进入／胜利 | `CombatManager.StartCombatInternal:584–597` 与 `EndCombatInternal:1357–1358` 刷新 `Progress`，胜利时原生选 `CombatEnd`。现有 `NativeSeek.Restore` 对普通 BGM 缺少统一回到战斗段的调用。恢复时须在战斗重新 active 后走原生控制器，保留目标的精英阶段。 |
| 精英二阶段 | `BygoneEffigy:74` 和 `InfestedPower:43` 走 `TriggerEliteSecondPhase`，写全局 `Progress=8`。现有代码捕获阶段并能写回；`InfestedPower` 延迟触发仍需单独按回调归属核对，避免已放弃的前进过程把阶段重新写回。 |
| 专曲及曲内状态 | 有 11 个 `CustomBgm` 覆盖，共 10 个事件路径，Aeonglass 与 Queen 共用曲目。当前胜利 Back 会重启大多数专曲，只有实验体的参数有专门捕获。静态查到 `queen_progress`、`kaiser_crab_progress`、`kaiser_crab_direction`、`the_kin_progress`、`knowledge_demon_progress`、`soulfysh_progress`、`beckon`、`test_subject_progress`、`insatiable_progress`、`vantom_progress`、`waterfall_giant_progress`。其中 `SurroundedPower:148` 写帝王蟹朝向，`SandpitPower:86` 写贪食者陷沙程度，`SoulFysh:106` 随手牌中的 Beckon 状态写参数。保留曲目、阶段和离散参数三者的差别；没有参数写入的 CeremonialBeast 可作专曲重启对照。 |
| 循环声与环境 | `SfxCmd.PlayLoop` 的全局路径（Ovicopter）与 `NCreature` 绑定的循环（Cubex、猫头鹰、右钳、睡眠甲虫、SnappingJaxfruit、偷窃蚱蜢、瀑布巨人）所有权不同；`UpdateAmbience` 还可按遭遇设置环境声。当前在途单次音退役不能建立目标点应持续的循环。 |

`NativeSeek.Restore` 在 `Drain` 前进入 Restoring，径向模糊与 hit-stop 的共同入口因此会阻止退役期间新开效果，旧任务由 `NativeCombatCamera` 收尾。这类已有共享处理，无需按怪物重写。普通 FMOD one-shot 已开始时通常没有回放持有的停止句柄，当前只有 Sculptor 的两个 cue 和 CeremonialBeast 的两个 cue 按清单持有句柄。未开始的声音静音与已播放尾声停止是两个能力。实机留给代理和音频资源的实际效果，包括 `Progress` 回写后音乐能否重新进入、已开始的 FMOD 事件是否停止及跨阶段听感。本轮只做静态复核。

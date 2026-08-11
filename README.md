# Myrt1eSkill_Remake

完整内容清单请查看：[技能与事件总表](docs/CONTENT_CATALOG.md)。

一个全新开发的 CS2 随机技能娱乐插件。原项目 `Myrt1eSkill` 仅作为玩法、生命周期和兼容性调研参考；新项目采用更小的插件入口和独立技能生命周期。

## 当前状态

- 已建立 CounterStrikeSharp 插件入口与配置。
- 已建立技能注册、权重与稀有度两阶段随机、近期防重复、互斥标签和统一回收机制。
- 支持技能启用、权重、稀有度和单服最大数量覆盖配置。
- 支持队伍、权限和队友条件过滤。
- 使用强类型活跃技能事件路由，不使用字符串反射调用。
- 每次技能赋予都有独立 `EffectScope`，统一清理 Timer、实体和属性恢复动作。
- 支持玩家断线、回合结束和热重载清理，以及逐技能主动冷却。
- 支持只调度活跃 Tick 技能和可选性能采样。
- 已加入 `FleetFooted`、`VampiricRounds`、`FieldMedic` 三个示例技能。
- 已加入 `Armored`（覆甲）：每次赋予独立抽取受伤倍率，并通过服务器 Pre-Damage Hook 在伤害结算前减伤。
- 已加入 `ExplosiveShot`（爆炸子弹）：每次赋予时抽取 15%–30% 触发率，在子弹落点创建并立即引爆原生 HE；支持墙面落点、队友伤害修正和爆炸击杀归属。
- 已加入 `Wallhack`（透视）：为玩家创建服务器端发光模型副本，并通过 `CheckTransmit` 只向技能持有者显示敌方穿墙轮廓。
- 已加入 `SuperpowerXray` 与 `Xray`：前者随机选择双方各一名超能力者查看敌方，后者让所有玩家查看敌我双方；两者共享 Wallhack 的 Glow 服务。
- 已加入 `Nightmare`（梦魇）：主动选择一名敌人，仅向其发送绑定在 Pawn 上的死亡镜头后处理体积，并播放定向诅咒提示音。
- 已加入 `Illiterate`（文盲）：持有者存活时，敌方看到的插件聊天、HUD 和菜单文字会被动态凯撒移位，数字变为问号。
- 已加入 `Flashlight`（手电筒）：主动开关随视角移动的 `light_barn` 光源，并致盲光束内正看向光源的敌人。
- 已加入 `Fortnite`（堡垒之夜）：主动创建拥有独立耐久、可被射击摧毁并会随技能生命周期清理的实体路障。
- 已加入 `DeadlyGrenades`（更致命的手雷）：禁用枪械购买、剥离主副武器并提供伤害 3 倍、范围 5 倍的无限 HE。
- 已加入 `Grapple`（抓钩）：向真实射线命中的墙面发射可见钩索，并持续将玩家拉向锚点；冷却 10 秒。
- 已加入 `JumpCurse`：持有者跳跃时，所有仍在地面的存活敌人会被强制同步跳跃。
- 已加入 `Pusher`（推手）：击伤敌人时按本回合随机概率，将其沿攻击者瞄准方向击飞。
- 已加入 `ThrowingKnife`（飞刀）：主动掷出自己的真实刀具，触碰敌人造成致命伤害；刀可被其他玩家捡走且不会自动补充。
- 已加入 `SmallButDeadly`（小而致命）：全员 0.5 倍体型、2 倍移速并仅有 10 点生命，重生后持续生效。
- 已建立独立娱乐事件注册、抽取、组合展开和兼容性解析。
- 事件先生成不可变 `RoundPlan`，不直接修改配置或技能管理器。
- 组合事件只会抽取互相兼容的子事件，并限制单回合最大事件数。
- 事件集合使用事务式 `EffectScope`；任一事件应用失败会整体反向回滚。
- 已实现 `NormalRound`、`NoSkill`、`MoreSkills`、`SkillsPlusPlus`、`ChooseCarnival`、`TopTierParty` 和 `TopTierPartyPlusPlus`。
- 已实现 `FastBunnyHop` 全员快速连跳事件；使用 CS2 原生 ConVar，并在事件结束时恢复服务器原值。
- 已复刻 `LowGravity` 与 `LowGravityPlusPlus`：分别使用 50% 和 20% 的回合初始重力，后者额外启用全局无扩散。
- 已复刻 `JumpOnShoot` 与 `JumpPlusPlus`：开枪分别获得 300/400 垂直速度，PlusPlus 额外提供全局无扩散和落地伤害免疫。
- 已复刻 `Blitzkrieg`、`SlowMotion` 与 `SwapOnHit`：支持可回滚时间倍率，以及带双方冷却的位置和朝向交换。
- 已复刻 `DecoyTeleport` 与 `ChickenMode`：支持诱饵弹落点传送/自动补充，以及可跨重生重建和完整回滚的小鸡外观、速度、生命效果。
- 多技能回合默认最多包含一个主动技能，避免出现无法激活的第二主动技能。
- 按当前产品方向，不加入多语言系统。
- 尚未迁移改变重力、时间、视野等世界状态的具体娱乐事件，也尚未实现菜单适配、射线适配和生产打包。

## 开发环境

`Grapple` 使用随构建输出的 `RayTraceApi.dll`。服务器还必须安装并加载 Ray-Trace CSS API 与对应的 Ray-Trace Metamod 模块，以提供 `raytrace:craytraceinterface` capability；模块缺失时抓钩会提示没有可用锚点并记录一次错误。

- .NET 10 SDK
- CounterStrikeSharp.API 1.0.371

```powershell
dotnet restore
dotnet build -c Release
```

已使用 .NET SDK 10.0.302 完成 Release 编译验证，结果为 0 个警告、0 个错误。

## 目录

```text
Myrt1eSkill_Remake/
├── Configuration/       # JSON 配置模型
├── Core/                # 技能契约、注册、选择和回合编排
├── Events/              # 娱乐事件与回合规则贡献
├── Skills/              # 具体技能，一个技能一个文件
├── docs/                # 调研结论和架构决策
├── Myrt1eSkill_Remake.csproj
└── Myrt1eSkillRemakePlugin.cs
```

## 下一阶段

1. 将技能池扩充到 5 个低风险 MVP 技能。
2. 迁移一个 ConVar 类事件和一个事件驱动类事件，验证世界状态回滚。
3. 在测试服验证组合事件、玩家断线、Bot 接管、回合清理和热重载。
4. 实现真正的 `ChooseOneOfThree` 技能后，将 `ChooseCarnivalSkillId` 从当前示例 `FieldMedic` 切换过去。
5. 增加打包脚本和部署目录校验。

## 调试命令

```text
css_rskill_status
css_forceevent <EventId>   # 仅服务器控制台
css_useskill
```

`Armored.MinimumDamageMultiplier` 与 `Armored.MaximumDamageMultiplier` 控制覆甲倍率范围，默认分别为 `0.65` 和 `0.85`。

`ExplosiveShot` 默认伤害为 `25`、半径为 `210`，概率范围为 `0.15`–`0.30`，队友伤害降低 `50%`。该技能使用 CS2 原生 HE 创建函数，部署时必须把输出目录中的 `gamedata/Myrt1eSkill_Remake.gamedata.json` 安装到 CounterStrikeSharp 的共享 `gamedata` 目录。

## 回合信息展示

技能揭示时序与 jRandomSkills 的默认行为一致：插件读取 `mp_freezetime`，默认在冻结时间结束前 `7` 秒抽取并公布技能；队伍入场阶段会额外顺延 `7` 秒，热身期间则每秒等待正式回合开始。揭示后，每位玩家会在聊天中看到自己的全部技能，默认也会看到队友的技能。

中央 HTML HUD 同时显示当前事件与自己的全部技能。事件名和技能名默认持续整回合（`SkillHudDuration = -1`），事件及技能说明显示 `7` 秒（`SkillDescriptionDuration = 7`）。这些文字统一经过插件文字管线，因此 `Illiterate` 仍会影响敌方看到的 HUD 与聊天内容。

强制下一回合启用全员快速连跳：

```text
css_forceevent FastBunnyHop
```

事件默认将 `sv_autobunnyhopping`、`sv_enablebunnyhopping` 设为开启，清除跳跃和落地体力损耗，并将 `sv_airaccelerate` 设为配置项 `FastBunnyHop.AirAccelerate`（默认 `100`）。

低重力事件可用以下命令测试：

```text
css_forceevent LowGravity
css_forceevent LowGravityPlusPlus
```

射击跳跃事件可用以下命令测试：

```text
css_forceevent JumpOnShoot
css_forceevent JumpPlusPlus
```

时间倍率与击中交换事件可用以下命令测试：

```text
css_forceevent Blitzkrieg
css_forceevent SlowMotion
css_forceevent SwapOnHit
```

TP 弹与小鸡模式可用以下命令测试：

```text
css_forceevent DecoyTeleport
css_forceevent ChickenMode
```

框架规则自检：

```powershell
dotnet run --project tests/FrameworkChecks/FrameworkChecks.csproj -c Release
```

## 许可证提醒

参考仓库及其上游 `jRandomSkills` 均为 GPL-3.0。若新项目复制、翻译或改写其受版权保护的代码，发布时应使用 GPL-3.0、保留许可证与来源说明，并提供对应源代码。最终许可证应在首次发布前明确。

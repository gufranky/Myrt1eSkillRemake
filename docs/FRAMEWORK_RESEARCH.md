# 框架调研记录

调研日期：2026-08-10

## 1. 参考项目的真实结构

`Myrt1eSkill` 是一个 CounterStrikeSharp C# 插件，目标为 .NET 8，固定依赖 `CounterStrikeSharp.API 1.0.362` 和 `CS2TraceRay 1.0.9`，并以源码项目方式携带 MenuManagerCS2 API。

本地统计结果：

- 24,058 行 C#。
- `Skills` 下 68 个 C# 文件。
- `Events` 下 50 个 C# 文件。
- 主入口 `MyrtleSkill.cs` 超过 1,600 行。

它有两套核心抽象：

- `EntertainmentEvent`：全局回合事件，生命周期是 `Register -> OnApply -> OnRevert`。
- `PlayerSkill`：玩家技能，生命周期是 `Register -> OnApply -> OnUse -> OnRevert`。

两套 Manager 都使用整数权重抽取，并用长度为 8 的历史队列减少重复。回合执行顺序是：恢复旧事件和技能状态、抽全局事件、延迟后给玩家抽技能、回合结束清理。

## 2. 参考架构的优点

- 技能和事件已有明确生命周期，具体玩法大体按文件隔离。
- 权重为 0 即禁用，服主容易理解。
- 有近期防重复与事件/技能互斥概念。
- 对 `OnApply` 的改动普遍有对应 `OnRevert`，已经意识到回合状态必须回收。
- MenuManager 通过 CounterStrikeSharp Capability 获取，可作为可选运行时依赖。

## 3. 新项目不直接继承的问题

- 插件入口承担二十多个 Hook 的集中转发，新增技能常需要同时修改主类和 Manager。
- 大量具体技能通过静态字段或 `MyrtleSkill.Instance` 访问插件，热重载和测试隔离较困难。
- 技能实例既保存定义又保存多名玩家的运行态，容易出现 Slot 复用和跨回合残留。
- 冷却以“玩家一个时间戳”管理，多主动技能时无法做到逐技能冷却。
- 互斥规则使用字符串列表，文档显示部分逻辑尚未完全启用；字符串拼写错误只能运行时发现。
- `Console.WriteLine` 较多，缺少结构化日志级别与统一异常边界。
- 每 Tick/每帧功能集中执行，后续增长容易产生不必要的全服扫描。
- 配置默认权重没有覆盖所有已注册技能，代码、文档和配置数量已经出现漂移。
- 打包脚本中的安装路径存在 `addons/counterstrikesharp/addons` 与文档中 `plugins` 概念混用的风险，应按目标 CSS 版本实服验证。

## 4. 官方框架现状

- CounterStrikeSharp 官方将 `BasePlugin.Load(bool hotReload)` 定义为插件加载入口；`Unload` 用于清理额外资源，框架会自动注销普通事件和 Listener。
- `OnAllPluginsLoaded` 适合在所有插件就绪后重新连接 Capability 等跨插件依赖。
- `RegisterAllAttributes(object)` 可以让非 `BasePlugin` 类使用属性式事件、命令和实体输出注册，适合后续模块化事件总线。
- GameEvent 对象只在回调期间有效；若进入 Timer 或 `Server.NextFrame`，必须先复制需要的值。
- `AddTimer` / `AddTickTimer` 是框架提供的计时机制，应该统一追踪并在地图/回合切换时失效。
- 2026-08-10 查询 NuGet 时，稳定包为 `CounterStrikeSharp.API 1.0.371`，目标框架为 .NET 10；参考项目的 1.0.362 已落后数个补丁版本。

官方资料：

- https://docs.cssharp.dev/
- https://docs.cssharp.dev/api/CounterStrikeSharp.API.Core.BasePlugin.html
- https://docs.cssharp.dev/docs/features/game-events.html
- https://docs.cssharp.dev/docs/guides/dependency-injection.html
- https://www.nuget.org/packages/CounterStrikeSharp.API
- https://github.com/roflmuffin/CounterStrikeSharp

## 5. 新项目的设计结论

- 插件主类只做装配和框架注册，不保存具体技能逻辑。
- 技能用稳定 `Id`、显示文本、类型、默认权重和互斥标签描述。
- Manager 统一负责抽取、赋予、撤销、历史与异常隔离。
- 回合异步任务携带 generation token；回合结束后旧 Timer 即使触发也不能再修改状态。
- 先将菜单和射线能力设计为 Adapter，可选依赖不可阻止核心插件加载。
- 后续把运行态从技能定义中分离为按玩家、按技能的 assignment state，避免共享实例污染。
- 任何改变 ConVar、玩家属性、实体或 Hook 的技能，都必须有幂等清理路径。


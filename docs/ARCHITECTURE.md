# 目标架构

```text
CounterStrikeSharp RoundStart
          |
          v
  RoundEventManager ---> EventRegistry
          |
          +--> expand composite events
          +--> CompatibilityResolver
          +--> RoundPlanBuilder
          |
          v
     immutable RoundPlan
          |
          +--> transactional event EffectScopes
          |
          v
      SkillManager ---> SkillRegistry
          |
          +--> SkillPlan constraints
          +--> RaritySelector + WeightedSelector
          +--> PlayerSession / SkillAssignment / EffectScope
```

## 边界

- `Myrt1eSkillRemakePlugin`：框架入口、对象装配、命令注册。
- `RoundCoordinator`：唯一的回合状态机，控制延迟任务失效。
- `RoundEventManager`：选择根事件、展开组合事件、生成计划并事务式应用事件集合。
- `EventRegistry`：娱乐事件目录，拒绝重复 ID。
- `CompatibilityResolver`：统一处理事件—事件和事件—技能冲突。
- `RoundPlanBuilder`：合并所有事件的声明，生成不可变 `RoundPlan`。
- `SkillRegistry`：唯一技能目录，拒绝重复 ID。
- `SkillManager`：只消费 `SkillPlan`，负责稀有度、强制技能、资格过滤、分配、历史、主动冷却、互斥和回收。
- `SkillEventRouter`：只向当前活跃且实现对应强类型接口的技能分发事件。
- `PlayerSession`：识别玩家/机器人会话，防止 Slot 复用继承旧状态。
- `EffectScope`：统一拥有 Timer、实体和其他清理动作。
- `WeightedSelector`：不依赖 CS2 API 的纯随机算法，后续可单元测试。
- `RaritySelector`：先抽取可用稀有度，再在该组内按权重选择技能。
- `ISkill`：技能生命周期契约。

## 下一步需要补齐的契约

- 更多游戏事件消费者接口：手雷、跳跃、致盲、伤害前置等。
- Bot 接管时的 assignment 转移策略。
- 主动技能次数限制与技能自定义激活失败结果。
- `IMenuAdapter` / `ITraceAdapter`：隔离可选第三方依赖。

## 回合规则优先级

1. `DisableSkills`
2. `ReplaceAllSkills`
3. `EnsureSkills`
4. 技能槽位要求（多个事件取最大值）
5. 普通稀有度与权重随机

`MoreSkills + SkillsPlusPlus` 因而稳定解析为三个槽位；`NoSkill` 会清空任何强制技能指令。事件之间还会先经过独占标签与显式 ID 冲突检查。

## 组合事件约束

- 组合事件本身不能再次作为子事件。
- `NormalRound` 不能作为子事件。
- 子事件必须两两兼容。
- 相同事件不能重复加入。
- 单回合事件总数由 `MaxEventsPerRound` 限制。
- 子事件不足时安全降级，不会强行加入冲突事件。
- 事件按应用顺序记录，按严格 LIFO 顺序清理。

## 明确不采用的 jRandomSkills 设计

- 不使用静态技能方法和全局静态玩家字典。
- 不通过 `"EnableSkill"` 等字符串执行反射调用。
- 不要求技能类名与中央枚举严格对应。
- 不加入多语言和 GeoIP 语言推断。

## MVP 验收标准

- 连续 20 回合无技能状态残留。
- 热重载后无重复 Hook、Timer 或属性叠加。
- 玩家断线、Bot 接管和 Slot 复用不会继承旧技能。
- 禁用权重、近期防重复和互斥标签均有自动化测试。
- 缺少可选依赖时只禁用相关技能，不影响插件主体加载。

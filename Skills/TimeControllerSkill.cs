using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class TimeControllerSkill : ISkill
{
    private sealed class TimeControllerState
    {
        public int SpeedIndex { get; set; } = 1;
    }

    private readonly TimeControllerSettings _settings;
    private readonly HashSet<uint> _holders = new();
    private ConVar? _hostTimescale;
    private float _originalTimescale = 1.0f;

    public TimeControllerSkill(TimeControllerSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "TimeController",
        DisplayName = "⏰ 时间控制者",
        Description = "按 [css_useSkill] 在 0.75×、1×、1.5× 游戏速度之间循环，影响所有玩家。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.1f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timescale-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new TimeControllerState());
        Acquire(context.Player.Index);
        var controllerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => Release(controllerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<TimeControllerState>(out var state))
        {
            return;
        }

        var speeds = GetSpeedLevels();
        state.SpeedIndex = (state.SpeedIndex + 1) % speeds.Length;
        var speed = speeds[state.SpeedIndex];
        GetTimescale().SetValue(speed);

        PluginText.ChatAll($"⏰ {context.Player.PlayerName} 将游戏速度切换为 {speed:0.##}×！");
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    private void Acquire(uint controllerIndex)
    {
        if (_holders.Count == 0)
        {
            _originalTimescale = GetTimescale().GetPrimitiveValue<float>();
        }

        _holders.Add(controllerIndex);
    }

    private void Release(uint controllerIndex)
    {
        if (!_holders.Remove(controllerIndex) || _holders.Count != 0)
        {
            return;
        }

        GetTimescale().SetValue(_originalTimescale);
        _hostTimescale = null;
    }

    private float[] GetSpeedLevels()
    {
        var slow = FinitePositiveOr(_settings.SlowSpeed, 0.75f);
        var normal = FinitePositiveOr(_settings.NormalSpeed, 1.0f);
        var fast = FinitePositiveOr(_settings.FastSpeed, 1.5f);
        return [slow, normal, fast];
    }

    private ConVar GetTimescale() =>
        _hostTimescale ??= ConVar.Find("host_timescale")
            ?? throw new InvalidOperationException("Required CS2 ConVar was not found: host_timescale");

    private static float FinitePositiveOr(float value, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, 0.05f, 10.0f) : fallback;
}

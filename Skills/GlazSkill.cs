using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class GlazSkill : ISkill, IGrenadeThrownSkill
{
    private sealed class GlazState
    {
        public required int GrenadesRemaining { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly GlazSettings _settings;
    private readonly GlazService _glaz;

    public GlazSkill(GlazSettings settings, GlazService glaz)
    {
        _settings = settings;
        _glaz = glaz;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Glaz",
        DisplayName = "🌫 格拉兹",
        Description = "可以透过烟雾看到敌人，并获得 3 颗烟雾弹。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "smoke-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new GlazState
        {
            GrenadesRemaining = Math.Clamp(_settings.GrenadeCount, 1, 10)
        };
        context.State.Set(state);

        var controllerIndex = context.Player.Index;
        _glaz.AddHolder(context.Player);
        context.Effects.RegisterCleanup(() =>
        {
            state.Active = false;
            _glaz.RemoveHolder(controllerIndex);
        });
        GiveSmoke(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!GrenadeReplenishment.Matches(@event.Weapon, "smokegrenade")
            || !context.State.TryGet<GlazState>(out var state)
            || !state.Active
            || state.GrenadesRemaining <= 0)
        {
            return;
        }

        state.GrenadesRemaining--;
        if (state.GrenadesRemaining <= 0)
        {
            return;
        }

        var player = context.Player;
        var effects = context.Effects;
        effects.AddTimer(GrenadeReplenishment.DelaySeconds, () =>
        {
            if (state.Active)
            {
                GiveSmoke(player);
            }
        });
    }

    private static void GiveSmoke(CCSPlayerController player)
    {
        if (player.IsValid && player.PawnIsAlive)
        {
            player.GiveNamedItem("weapon_smokegrenade");
        }
    }
}

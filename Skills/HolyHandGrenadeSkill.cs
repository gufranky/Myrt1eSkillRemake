using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class HolyHandGrenadeSkill : ISkill, IGrenadeThrownSkill
{
    private sealed class HolyGrenadeState
    {
        public required int ReplenishmentsRemaining { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly HolyHandGrenadeSettings _settings;
    private readonly HolyHandGrenadeService _service;

    public HolyHandGrenadeSkill(HolyHandGrenadeSettings settings, HolyHandGrenadeService service)
    {
        _settings = settings;
        _service = service;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "HolyHandGrenade",
        DisplayName = "✝️ 圣手榴弹",
        Description = "HE 手雷造成 2.5 倍伤害和范围；开局获得 1 颗，投掷后补充 1 次。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hegrenade-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new HolyGrenadeState
        {
            ReplenishmentsRemaining = Math.Clamp(_settings.MaximumReplenishments, 0, 10)
        };
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        _service.Acquire(context.Player, context.Effects);
        GiveGrenade(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!string.Equals(@event.Weapon, "hegrenade", StringComparison.OrdinalIgnoreCase)
            || !context.State.TryGet<HolyGrenadeState>(out var state)
            || !state.Active
            || state.ReplenishmentsRemaining <= 0)
        {
            return;
        }

        state.ReplenishmentsRemaining--;
        var player = context.Player;
        var effects = context.Effects;
        effects.AddTimer(0.01f, () =>
        {
            if (state.Active)
            {
                GiveGrenade(player);
                PluginText.Chat(player, "[圣手榴弹] HE 手雷已补充（1/1）");
            }
        });
    }

    private static void GiveGrenade(CCSPlayerController player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var alreadyHasGrenade = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_hegrenade" }) == true;
        if (!alreadyHasGrenade)
        {
            player.GiveNamedItem("weapon_hegrenade");
        }
    }
}

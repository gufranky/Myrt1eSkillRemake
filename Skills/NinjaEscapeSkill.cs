using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class NinjaEscapeSkill : ISkill, IPreDamageSkill
{
    private sealed class NinjaEscapeState
    {
        public int Uses { get; set; }
        public int ProtectedUntilTick { get; set; } = -1;
    }

    private readonly NinjaEscapeSettings _settings;
    private readonly NavMeshService _navMesh;
    private readonly SmokeProjectileService _smokes;

    public NinjaEscapeSkill(NinjaEscapeSettings settings, NavMeshService navMesh, SmokeProjectileService smokes)
    {
        _settings = settings;
        _navMesh = navMesh;
        _smokes = smokes;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "NinjaEscape",
        DisplayName = "🥷 我是忍者",
        Description = "受到致命伤害时免疫此次伤害，随机安全传送，并在原地留下会爆开的烟雾弹。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Epic,
        DefaultWeight = 10,
        MaxPerServer = 3,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "second-chance", "lethal-damage-escape"
        }
    };

    public void OnGranted(in SkillContext context) => context.State.Set(new NinjaEscapeState());
    public void OnActivated(in SkillContext context) { }
    public void OnRevoked(in SkillContext context) { }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (!context.State.TryGet<NinjaEscapeState>(out var state))
        {
            return;
        }

        if (Server.TickCount <= state.ProtectedUntilTick)
        {
            damageInfo.Damage = 0.0f;
            return;
        }

        var player = context.Player;
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true }
            || !DamageEvaluation.WouldBeLethal(pawn, damageInfo)
            || state.Uses >= Math.Max(1, _settings.MaximumUsesPerRound))
        {
            return;
        }

        var origin = pawn.AbsOrigin;
        state.Uses++;
        state.ProtectedUntilTick = Server.TickCount + 4;
        damageInfo.Damage = 0.0f;
        if (origin is not null)
        {
            _smokes.TrySpawn(new Vector(origin.X, origin.Y, origin.Z + 4.0f), player);
        }

        var teleported = _navMesh.TryTeleportRandom(player, out _);
        PluginText.Center(player, teleported ? "🥷 忍术：烟遁！" : "🥷 忍术：烟遁保命！");
        PluginText.Chat(player, teleported
            ? "[我是忍者] 你避开了致命伤害，并在烟雾中随机传送！"
            : "[我是忍者] 你免疫了致命伤害；当前地图没有可用安全传送点。");
    }
}

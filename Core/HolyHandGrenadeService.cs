using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class HolyHandGrenadeService
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly HolyHandGrenadeSettings _settings;
    private readonly HashSet<uint> _holders = new();
    private bool _loaded;

    public HolyHandGrenadeService(Myrt1eSkillRemakePlugin plugin, HolyHandGrenadeSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        _loaded = true;
    }

    public void Unload()
    {
        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
            _loaded = false;
        }

        _holders.Clear();
    }

    public void Acquire(CCSPlayerController player, EffectScope effects)
    {
        _holders.Add(player.Index);
        effects.RegisterCleanup(() => _holders.Remove(player.Index));
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (!string.Equals(entity.DesignerName, "hegrenade_projectile", StringComparison.Ordinal))
        {
            return;
        }

        var grenade = entity.As<CHEGrenadeProjectile>();
        if (grenade is { IsValid: true })
        {
            Server.NextFrame(() => Enhance(grenade));
        }
    }

    private void Enhance(CHEGrenadeProjectile grenade)
    {
        var pawn = grenade.Thrower.Value;
        var owner = pawn?.Controller.Value?.As<CCSPlayerController>();
        if (!grenade.IsValid || owner is not { IsValid: true } || !_holders.Contains(owner.Index))
        {
            return;
        }

        var damageMultiplier = float.IsFinite(_settings.DamageMultiplier)
            ? Math.Max(0.0f, _settings.DamageMultiplier)
            : 2.5f;
        var radiusMultiplier = float.IsFinite(_settings.RadiusMultiplier)
            ? Math.Max(0.0f, _settings.RadiusMultiplier)
            : 2.5f;
        grenade.Damage *= damageMultiplier;
        grenade.DmgRadius *= radiusMultiplier;
    }
}

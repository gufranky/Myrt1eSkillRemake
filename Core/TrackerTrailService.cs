using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class TrackerTrailService
{
    private sealed record Trail(uint ViewerIndex, uint TargetIndex, uint ParticleIndex);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly TrackerSettings _settings;
    private readonly Dictionary<string, Trail> _trails = new(StringComparer.Ordinal);
    private bool _loaded;

    public TrackerTrailService(Myrt1eSkillRemakePlugin plugin, TrackerSettings settings)
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

        _plugin.RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _loaded = true;
    }

    public void Unload()
    {
        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        }

        foreach (var owner in _trails.Keys.ToArray())
        {
            Release(owner);
        }

        _trails.Clear();
        _loaded = false;
    }

    public bool Apply(CCSPlayerController viewer, CCSPlayerController target, string owner)
    {
        var pawn = target.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (!viewer.IsValid
            || !target.IsValid
            || !target.PawnIsAlive
            || pawn is not { IsValid: true }
            || origin is null
            || string.IsNullOrWhiteSpace(owner))
        {
            return false;
        }

        Release(owner);
        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle is not { IsValid: true })
        {
            return false;
        }

        particle.EffectName = GetParticleName();
        particle.StartActive = true;
        particle.Teleport(origin);
        particle.DispatchSpawn();
        particle.AcceptInput("SetParent", pawn, particle, "!activator");
        particle.AcceptInput("Start");

        _trails[owner] = new Trail(viewer.Index, target.Index, particle.Index);
        return true;
    }

    public bool Release(string owner)
    {
        if (!_trails.Remove(owner, out var trail))
        {
            return false;
        }

        var particle = Utilities.GetEntityFromIndex<CParticleSystem>((int)trail.ParticleIndex);
        if (particle is { IsValid: true })
        {
            particle.AcceptInput("Stop");
            particle.Remove();
        }

        return true;
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_trails.Count == 0)
        {
            return;
        }

        var trails = _trails.Values.ToArray();
        foreach (var (info, observer) in infoList)
        {
            if (observer is not { IsValid: true })
            {
                continue;
            }

            foreach (var trail in trails)
            {
                if (!CanView(observer, trail.ViewerIndex)
                    && info.TransmitEntities.Contains(trail.ParticleIndex))
                {
                    info.TransmitEntities.Remove(trail.ParticleIndex);
                }
            }
        }
    }

    private static bool CanView(CCSPlayerController observer, uint viewerIndex)
    {
        if (observer.Index == viewerIndex)
        {
            return true;
        }

        var observedHandle = observer.PlayerPawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;
        if (observedHandle == nint.Zero)
        {
            return false;
        }

        var viewer = Utilities.GetPlayerFromIndex((int)viewerIndex);
        return viewer?.PlayerPawn.Value?.Handle == observedHandle;
    }

    private void OnServerPrecacheResources(ResourceManifest manifest) => manifest.AddResource(GetParticleName());

    private string GetParticleName() => string.IsNullOrWhiteSpace(_settings.ParticleName)
        ? "particles/ui/hud/ui_map_def_utility_trail.vpcf"
        : _settings.ParticleName;
}

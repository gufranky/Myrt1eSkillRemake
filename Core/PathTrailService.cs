using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

public sealed class PathTrailService
{
    private sealed record Trail(uint Viewer, List<uint> Particles);
    private readonly NavMeshService _nav;
    private readonly Dictionary<string, Trail> _trails = new(StringComparer.Ordinal);
    public PathTrailService(NavMeshService nav) => _nav = nav;

    public bool Apply(CCSPlayerController viewer, CCSPlayerController target, string owner)
    {
        if (!_nav.TryBuildPath(viewer, target, out var points)) return false;
        Release(owner);
        var particles = new List<uint>();
        foreach (var point in points.Where((_, i) => i % 2 == 0).Take(48))
        {
            var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle is null) continue;
            particle.EffectName = "particles/ui/hud/ui_map_def_utility_trail.vpcf";
            particle.StartActive = true;
            particle.Teleport(point);
            particle.DispatchSpawn();
            particle.AcceptInput("Start");
            particles.Add(particle.Index);
        }
        if (particles.Count == 0) return false;
        _trails[owner] = new Trail(viewer.Index, particles);
        return true;
    }

    public void Release(string owner)
    {
        if (!_trails.Remove(owner, out var trail)) return;
        foreach (var index in trail.Particles)
        {
            var particle = Utilities.GetEntityFromIndex<CParticleSystem>((int)index);
            if (particle is { IsValid: true }) { particle.AcceptInput("Stop"); particle.Remove(); }
        }
    }

    public void OnCheckTransmit(CCheckTransmitInfoList list)
    {
        foreach (var (info, observer) in list)
        foreach (var trail in _trails.Values)
        if (observer is not { IsValid: true } || observer.Index != trail.Viewer)
            foreach (var particle in trail.Particles) info.TransmitEntities.Remove(particle);
    }
}

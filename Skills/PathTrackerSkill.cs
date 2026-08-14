using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class PathTrackerSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private readonly PathTrailService _trails;
    private readonly string _owner = $"PathTracker:{Guid.NewGuid():N}";
    private uint? _target;
    private float _nextRefresh;
    public PathTrackerSkill(PathTrailService trails) => _trails = trails;
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "PathTracker", DisplayName = "循迹跟踪", Description = "你可以看到一条通往随机敌人的导航粒子线。",
        Kind = SkillKind.Passive, Rarity = SkillRarity.Common, DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "targeted-tracking-visual", "particle-trail-control" }
    };
    public void OnGranted(in SkillContext context) => context.Effects.RegisterCleanup(() => _trails.Release(_owner));
    public void OnActivated(in SkillContext context) { }
    public void OnRevoked(in SkillContext context) => _trails.Release(_owner);
    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event) { if (_target == @event.Userid?.Index) _target = null; }
    public void OnTick(in SkillContext context)
    {
        if (Server.CurrentTime < _nextRefresh) return;
        _nextRefresh = Server.CurrentTime + 1.0f;
        var owner = context.Player;
        var enemies = Utilities.GetPlayers().Where(p => p is { IsValid: true, PawnIsAlive: true } && p.Team != owner.Team && p.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist).ToArray();
        if (enemies.Length == 0) return;
        var target = _target is { } id ? Utilities.GetPlayerFromIndex((int)id) : null;
        if (target is not { IsValid: true, PawnIsAlive: true }) target = enemies[Random.Shared.Next(enemies.Length)];
        if (_trails.Apply(owner, target, _owner)) _target = target.Index;
    }
}

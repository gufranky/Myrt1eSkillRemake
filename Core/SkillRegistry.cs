using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Skills;

namespace Myrt1eSkill_Remake.Core;

public sealed class SkillRegistry
{
    private readonly Dictionary<string, ISkill> _skills = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _skills.Count;
    public IReadOnlyCollection<ISkill> All => _skills.Values;

    public static SkillRegistry CreateDefault(
        PluginConfig config,
        ExplosiveProjectileService explosions,
        WallhackService wallhack,
        NightmareService nightmare,
        IlliterateService illiterate)
    {
        var registry = new SkillRegistry();
        registry.Register(new FleetFootedSkill());
        registry.Register(new VampiricRoundsSkill());
        registry.Register(new FieldMedicSkill());
        registry.Register(new ArmoredSkill(config.Armored));
        registry.Register(new ExplosiveShotSkill(config.ExplosiveShot, explosions));
        registry.Register(new WallhackSkill(wallhack));
        registry.Register(new NightmareSkill(nightmare));
        registry.Register(new IlliterateSkill(illiterate));
        return registry;
    }

    public void Register(ISkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        var id = skill.Descriptor.Id;

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("A skill must have a non-empty id.");
        }

        if (!_skills.TryAdd(id, skill))
        {
            throw new InvalidOperationException($"Duplicate skill id: {id}");
        }
    }

    public bool TryGet(string id, out ISkill? skill) => _skills.TryGetValue(id, out skill);
}

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace Myrt1eSkill_Remake.Core;

public sealed class SilentSoundService
{
    public const int SoundEventMessageId = 208;

    private static readonly HashSet<uint> MutedSoundEvents =
    [
        3109879199, 70939233, 1342713723, 2722081556, 1909915699, 3193435079,
        2300993891, 3847761506, 4084367249, 2026488395, 2745524735, 2684452812,
        2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732,
        2240518199, 2829617974, 1194677450, 1803111098, 3749333696, 29217150,
        1692050905, 2207486967, 2633527058, 3342414459, 988265811, 540697918,
        1763490157, 3755338324, 3161194970, 3753692454, 3166948458, 3997353267,
        809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576,
        737696412, 1403457606, 1770765328, 892882552, 3023174225, 4163677892,
        3952104171, 4082928848, 1019414932, 1485322532, 1161855519, 1557420499,
        1163426340, 2708661994, 2479376962, 1404198078, 1194093029, 1253503839,
        2189706910, 1218015996, 96240187, 1116700262, 84876002, 1598540856,
        2231399653,
        2551626319, 765706800, 2860219006, 2162652424, 117596568, 740474905,
        1661204257, 3009312615, 1506215040, 115843229, 3299941720, 1016523349,
        2067683805, 4160462271, 1543118744, 585390608, 3802757032, 2302139631,
        2546391140, 144629619, 4152012084, 4113422219, 1627020521, 2899365092,
        819435812, 3218103073, 961838155, 1535891875, 1826799645, 3460445620,
        1818046345, 3666896632, 3099536373, 1440734007, 1409986305, 1939055066,
        782454593, 4074593561, 1540837791, 3257325156
    ];

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SkillManager _skills;
    private bool _loaded;

    public SilentSoundService(Myrt1eSkillRemakePlugin plugin, SkillManager skills)
    {
        _plugin = plugin;
        _skills = skills;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.HookUserMessage(SoundEventMessageId, OnSoundEvent);
        _loaded = true;
    }

    public void Unload()
    {
        if (!_loaded)
        {
            return;
        }

        _plugin.UnhookUserMessage(SoundEventMessageId, OnSoundEvent);
        _loaded = false;
    }

    public static bool IsMutedSoundEvent(uint soundEventHash) =>
        MutedSoundEvents.Contains(soundEventHash);

    private HookResult OnSoundEvent(UserMessage message)
    {
        var soundEventHash = message.ReadUInt("soundevent_hash");
        if (!IsMutedSoundEvent(soundEventHash))
        {
            return HookResult.Continue;
        }

        var sourceEntityIndex = message.ReadUInt("source_entity_index");
        if (sourceEntityIndex == 0)
        {
            return HookResult.Continue;
        }

        var source = Utilities.GetPlayers().FirstOrDefault(player =>
            player.IsValid
            && player.PlayerPawn.Value is { IsValid: true } pawn
            && pawn.Index == sourceEntityIndex);
        if (source is null
            || !_skills.GetAssignedSkills(source).Any(skill =>
                skill.Id.Equals("Silent", StringComparison.OrdinalIgnoreCase)))
        {
            return HookResult.Continue;
        }

        message.Recipients.Clear();
        return HookResult.Continue;
    }
}

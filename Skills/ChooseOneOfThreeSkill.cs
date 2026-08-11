using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ChooseOneOfThreeSkill : ISkill, IPlayerDeathSkill
{
    private const int ChoiceCount = 3;

    private sealed class ChoiceState
    {
        public string ReservationOwner { get; } = $"ChooseOneOfThree:{Guid.NewGuid():N}";
        public HashSet<string>? OfferedSkillIds { get; set; }
        public bool Revoked { get; set; }
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ChooseOneOfThree",
        DisplayName = "🎰 三选一",
        Description = "随机抽取3个技能，选择一个获得！",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-assignment-replacement"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new ChoiceState();
        var runtimeSkills = context.Plugin.RuntimeSkills;
        context.State.Set(state);
        context.Effects.RegisterCleanup(() =>
            runtimeSkills.ReleaseSkillChoiceReservations(state.ReservationOwner));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<ChoiceState>(out var state) || state.Revoked)
        {
            return;
        }

        var plugin = context.Plugin;
        var player = context.Player;
        IReadOnlyList<SkillDescriptor> choices;
        if (state.OfferedSkillIds is null)
        {
            choices = plugin.RuntimeSkills.DrawSkillChoices(
                player,
                ChoiceCount,
                state.ReservationOwner,
                Descriptor.Id);
            if (choices.Count < ChoiceCount)
            {
                PluginText.Chat(player, "[三选一] 当前可用技能不足三个。");
                return;
            }

            state.OfferedSkillIds = choices
                .Select(skill => skill.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            choices = state.OfferedSkillIds
                .Select(id => plugin.RuntimeSkills.TryGetDescriptor(id))
                .Where(descriptor => descriptor is not null)
                .Cast<SkillDescriptor>()
                .ToArray();
        }

        if (choices.Count == 0)
        {
            PluginText.Chat(player, "[三选一] 候选技能已经失效。");
            return;
        }

        var menu = new WasdMenu(
            PluginText.Transform(player, "🎰 三选一：选择一个技能"),
            plugin);
        foreach (var choice in choices)
        {
            var skillId = choice.Id;
            var label = $"{choice.DisplayName}：{choice.Description}";
            menu.AddMenuOption(
                PluginText.Transform(player, label),
                (chooser, option) => TryChoose(plugin, chooser, skillId, state));
        }

        plugin.WasdMenus.Open(player, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<ChoiceState>(out var state))
        {
            state.Revoked = true;
            context.Plugin.RuntimeSkills.ReleaseSkillChoiceReservations(state.ReservationOwner);
        }

        context.Plugin.WasdMenus.Close(context.Player);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid?.Index != context.Player.Index
            || !context.State.TryGet<ChoiceState>(out var state))
        {
            return;
        }

        context.Plugin.RuntimeSkills.ReleaseSkillChoiceReservations(state.ReservationOwner);
        state.OfferedSkillIds = null;
        context.Plugin.WasdMenus.Close(context.Player);
    }

    private static void TryChoose(
        Myrt1eSkillRemakePlugin plugin,
        CCSPlayerController player,
        string skillId,
        ChoiceState state)
    {
        if (state.Revoked
            || state.OfferedSkillIds?.Contains(skillId) != true
            || !player.IsValid
            || !player.PawnIsAlive)
        {
            return;
        }

        if (!plugin.RuntimeSkills.TryReplaceWithSkill(
                player,
                skillId,
                out var grantedSkill,
                out var error))
        {
            PluginText.Chat(player, $"[三选一] 选择失败：{error}");
            return;
        }

        PluginText.Chat(player, $"[三选一] 你选择了 {grantedSkill!.DisplayName}：{grantedSkill.Description}");
    }
}

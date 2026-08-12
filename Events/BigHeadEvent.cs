using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

/// <summary>
/// Uses a precached, parent-attached model as a visual head enlargement. CS2
/// exposes whole-player model scale, but not a stable per-bone scale through
/// CounterStrikeSharp, so this avoids changing hitboxes or the player's body.
/// </summary>
public sealed class BigHeadEvent : RoundEventBase, IRoundEventPlayerSpawn
{
    private const string HeadModel = "models/props_junk/watermelon01.vmdl";
    private readonly BigHeadEventSettings _settings;
    private readonly Dictionary<int, CDynamicProp> _heads = new();
    private bool _active;

    public BigHeadEvent(BigHeadEventSettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "BigHead",
        DisplayName = "大头模式",
        Description = "所有玩家头部模型变大。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        var effects = context.Effects;
        effects.RegisterCleanup(() =>
        {
            _active = false;
            RemoveAll();
        });

        foreach (var player in Utilities.GetPlayers())
        {
            CreateHead(effects, player);
        }

        PrintToChatAll("[娱乐事件] 大头模式：所有人的头模型都变大了！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        var effects = context.Effects;
        effects.AddTimer(0.2f, () =>
        {
            if (_active)
            {
                CreateHead(effects, player);
            }
        });
    }

    private void CreateHead(EffectScope effects, CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        RemoveHead(player.Slot);
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var head = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (head is null)
        {
            return;
        }

        head.SetModel(HeadModel);
        head.Render = System.Drawing.Color.FromArgb(255, 255, 205, 40);
        head.DispatchSpawn();
        var scale = float.IsFinite(_settings.HeadScale)
            ? Math.Clamp(_settings.HeadScale, 1.05f, 3.0f)
            : 1.5f;
        head.AcceptInput("SetScale", head, head, scale.ToString(CultureInfo.InvariantCulture));
        head.AcceptInput("SetParent", pawn, head, "!activator");
        head.AcceptInput("SetParentAttachmentMaintainOffset", pawn, head, "head");
        head.Teleport(new Vector(0, 0, 64), new QAngle(0, 0, 0), null);
        Utilities.SetStateChanged(head, "CBaseEntity", "m_CBodyComponent");
        _heads[player.Slot] = head;
        effects.TrackEntity(head);
    }

    private void RemoveAll()
    {
        foreach (var slot in _heads.Keys.ToArray())
        {
            RemoveHead(slot);
        }
    }

    private void RemoveHead(int slot)
    {
        if (_heads.Remove(slot, out var head) && head.IsValid)
        {
            head.Remove();
        }
    }
}

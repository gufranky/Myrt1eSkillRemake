using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class PilotSkill : ISkill, ITickSkill
{
    private sealed class PilotState
    {
        public required float Fuel { get; set; }
    }

    private readonly PilotSettings _settings;

    public PilotSkill(PilotSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Pilot",
        DisplayName = "飞行员",
        Description = "按住 [USE - E] 键消耗燃料飞行，停止后会逐渐恢复燃料。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flight-control",
            "gravity-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new PilotState { Fuel = GetMaximumFuel() });
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.Player.PawnIsAlive || !context.State.TryGet<PilotState>(out var state))
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        var maximumFuel = GetMaximumFuel();
        var flying = context.Player.Buttons.HasFlag(PlayerButtons.Use) && state.Fuel > 0.0f;
        var consumption = float.IsFinite(_settings.FuelConsumption)
            ? Math.Max(0.0f, _settings.FuelConsumption)
            : 0.64f;
        var refuelling = float.IsFinite(_settings.Refuelling)
            ? Math.Max(0.0f, _settings.Refuelling)
            : 0.10f;

        state.Fuel = Math.Clamp(
            state.Fuel + (flying ? -consumption : refuelling),
            0.0f,
            maximumFuel);

        if (flying)
        {
            ApplyFlight(pawn);
        }

        if ((flying || state.Fuel < maximumFuel) && Server.TickCount % 8 == 0)
        {
            var percentage = maximumFuel <= 0.0f ? 0.0f : state.Fuel / maximumFuel * 100.0f;
            PluginText.Center(context.Player, $"飞行燃料：{percentage:0}%");
        }
    }

    private void ApplyFlight(CCSPlayerPawn pawn)
    {
        var pitch = pawn.EyeAngles.X * (MathF.PI / 180.0f);
        var yaw = pawn.EyeAngles.Y * (MathF.PI / 180.0f);
        var forwardAcceleration = float.IsFinite(_settings.ForwardAcceleration)
            ? _settings.ForwardAcceleration
            : 5.0f;
        var upwardAcceleration = float.IsFinite(_settings.UpwardAcceleration)
            ? _settings.UpwardAcceleration
            : 12.0f;

        pawn.AbsVelocity.X += MathF.Cos(yaw) * MathF.Cos(pitch) * forwardAcceleration;
        pawn.AbsVelocity.Y += MathF.Sin(yaw) * MathF.Cos(pitch) * forwardAcceleration;
        pawn.AbsVelocity.Z += upwardAcceleration;
    }

    private float GetMaximumFuel()
    {
        return float.IsFinite(_settings.MaximumFuel)
            ? Math.Max(0.01f, _settings.MaximumFuel)
            : 150.0f;
    }
}

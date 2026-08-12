using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Applies Source 2 distance fog to each player's skybox fog state.
/// CS2 keeps resetting the pawn's replicated fog values, so this must be
/// refreshed from the server tick while the effect is active.
/// </summary>
public sealed class FogService
{
    private readonly Dictionary<int, CFogController> _controllers = new();
    private bool _active;
    private float _end;
    private float _maxDensity;
    private float _exponent;
    private Color _color;

    public void Enable(float end, float maxDensity, float exponent, Color color)
    {
        _active = true;
        _end = Math.Max(64.0f, end);
        _maxDensity = Math.Clamp(maxDensity, 0.0f, 1.0f);
        _exponent = Math.Max(0.01f, exponent);
        _color = color;
        ApplyVisibilityMultiplier(_maxDensity);
    }

    public void Tick()
    {
        if (!_active)
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (player is not { IsValid: true } || player.PlayerPawn.Value is not { IsValid: true } pawn)
            {
                continue;
            }

            var controller = GetOrCreateController(player);
            if (controller == null)
            {
                continue;
            }

            var fog = controller.Fog;
            fog.Enable = true;
            fog.ColorPrimary = _color;
            fog.ColorSecondary = _color;
            fog.Start = 0.0f;
            fog.End = _end;
            fog.Farz = _end;
            fog.Maxdensity = _maxDensity;
            fog.Exponent = _exponent;
            pawn.AcceptInput("SetFogController", activator: controller, value: "!activator");
            var pawnFog = pawn.Skybox3d.Fog;
            pawnFog.Enable = fog.Enable;
            pawnFog.ColorPrimary = fog.ColorPrimary;
            pawnFog.ColorSecondary = fog.ColorSecondary;
            pawnFog.Start = fog.Start;
            pawnFog.End = fog.End;
            pawnFog.Farz = fog.Farz;
            pawnFog.Maxdensity = fog.Maxdensity;
            pawnFog.Exponent = fog.Exponent;
            SetStateChangeFogparams(pawn, "CBasePlayerPawn", "m_skybox3d", Schema.GetSchemaOffset("sky3dparams_t", "fog"));
            SetStateChangeFogparams(controller, "CFogController", "m_fog");
        }
    }

    public void Disable()
    {
        _active = false;
        ApplyVisibilityMultiplier(0.0f);

        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn is not { IsValid: true })
            {
                continue;
            }

            var fog = pawn.Skybox3d.Fog;
            fog.Enable = false;
            fog.Maxdensity = 0.0f;
            SetStateChangeFogparams(pawn, "CBasePlayerPawn", "m_skybox3d", Schema.GetSchemaOffset("sky3dparams_t", "fog"));
        }

        foreach (var controller in _controllers.Values)
        {
            if (controller.IsValid)
            {
                controller.Remove();
            }
        }

        _controllers.Clear();
    }

    private CFogController? GetOrCreateController(CCSPlayerController player)
    {
        if (_controllers.TryGetValue(player.Slot, out var existing) && existing.IsValid)
        {
            return existing;
        }

        var controller = Utilities.CreateEntityByName<CFogController>("env_fog_controller");
        if (controller == null)
        {
            return null;
        }

        controller.Entity!.Name = $"Myrt1eSkillFog{player.Slot}";
        controller.DispatchSpawn();
        _controllers[player.Slot] = controller;
        return controller;
    }

    private static void ApplyVisibilityMultiplier(float value)
    {
        var visibility = Utilities.FindAllEntitiesByDesignerName<CPlayerVisibility>("env_player_visibility").FirstOrDefault();
        if (visibility == null)
        {
            return;
        }

        visibility.FogMaxDensityMultiplier = value;
        Utilities.SetStateChanged(visibility, "CPlayerVisibility", "m_flFogMaxDensityMultiplier");
    }

    private static void SetStateChangeFogparams(CBaseEntity entity, string className, string fieldName, int extraOffset = 0)
    {
        string[] fields =
        [
            "dirPrimary", "colorPrimary", "colorSecondary", "start", "end", "farz",
            "maxdensity", "exponent", "HDRColorScale", "skyboxFogFactor", "blendtobackground",
            "scattering", "locallightscale", "enable", "blend", "m_bNoReflectionFog"
        ];

        foreach (var field in fields)
        {
            Utilities.SetStateChanged(entity, className, fieldName, extraOffset + Schema.GetSchemaOffset("fogparams_t", field));
        }
    }

}

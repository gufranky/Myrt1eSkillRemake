using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class NightmareService : IDisposable
{
    private sealed record NightmareEffect(uint CasterIndex, uint TargetIndex, uint VolumeIndex);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly NightmareSettings _settings;
    private readonly Dictionary<uint, NightmareEffect> _byCaster = new();
    private readonly Dictionary<uint, NightmareEffect> _byTarget = new();
    private bool _disposed;

    public NightmareService(Myrt1eSkillRemakePlugin plugin, NightmareSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        if (!string.IsNullOrWhiteSpace(_settings.PostProcessing))
        {
            manifest.AddResource(_settings.PostProcessing);
        }
    }

    public bool TryApply(CCSPlayerController caster, CCSPlayerController target)
    {
        if (_disposed
            || !caster.IsValid
            || !caster.PawnIsAlive
            || !target.IsValid
            || !target.PawnIsAlive
            || caster.Team == target.Team)
        {
            return false;
        }

        var targetPawn = target.PlayerPawn.Value;
        if (targetPawn is not { IsValid: true } || targetPawn.AbsOrigin is null)
        {
            return false;
        }

        RemoveCaster(caster);
        RemoveTarget(target);

        var volume = Utilities.CreateEntityByName<CPostProcessingVolume>("post_processing_volume");
        if (volume is null || !volume.IsValid || volume.Entity is null)
        {
            return false;
        }

        try
        {
            using var keys = new CEntityKeyValues();
            keys.SetString("targetname", $"Myrt1eSkill_Nightmare_{target.Index}");
            keys.SetString("postprocessing", _settings.PostProcessing);
            keys.SetBool("master", true);
            keys.SetBool("enableexposure", true);
            keys.SetFloat("fadetime", FiniteOr(_settings.FadeTime, 0.25f, 0.0f, 10.0f));
            keys.SetFloat("minexposure", FiniteOr(_settings.MinimumExposure, 0.50f, 0.0f, 10.0f));
            keys.SetFloat("maxexposure", FiniteOr(_settings.MaximumExposure, 2.0f, 0.0f, 10.0f));
            keys.SetFloat("exposurespeedup", 1.0f);
            keys.SetFloat("exposurespeeddown", 1.0f);
            keys.SetBool("startdisabled", false);
            keys.SetInt("spawnflags", 4097);
            keys.SetVector("origin", targetPawn.AbsOrigin);
            volume.DispatchSpawn(keys);

            if (!volume.IsValid)
            {
                return false;
            }

            volume.AcceptInput("SetParent", targetPawn, null, "!activator");
            var effect = new NightmareEffect(caster.Index, target.Index, volume.Index);
            _byCaster[caster.Index] = effect;
            _byTarget[target.Index] = effect;

            PluginText.Chat(target, "[梦魇] 一场恐怖的幻象正在侵蚀你的视野……");
            target.EmitSound(
                "UI.ArmsRace.FinalKill_Tone",
                new RecipientFilter(target),
                FiniteOr(_settings.SoundVolume, 0.50f, 0.0f, 1.0f));
            return true;
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "Failed to apply Nightmare to target {TargetIndex}", target.Index);
            if (volume.IsValid)
            {
                volume.Remove();
            }

            return false;
        }
    }

    public void RemoveCaster(CCSPlayerController? caster)
    {
        if (caster is not null)
        {
            RemoveCaster(caster.Index);
        }
    }

    public void RemoveCaster(uint casterIndex)
    {
        if (_byCaster.TryGetValue(casterIndex, out var effect))
        {
            RemoveEffect(effect);
        }
    }

    public void RemoveTarget(CCSPlayerController? target)
    {
        if (target is not null && _byTarget.TryGetValue(target.Index, out var effect))
        {
            RemoveEffect(effect);
        }
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_byTarget.Count == 0)
        {
            return;
        }

        var effects = _byTarget.Values.ToArray();
        foreach (var (info, viewer) in infoList)
        {
            if (viewer is not { IsValid: true })
            {
                continue;
            }

            foreach (var effect in effects)
            {
                if (effect.TargetIndex != viewer.Index)
                {
                    info.TransmitEntities.Remove(effect.VolumeIndex);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var effect in _byTarget.Values.Distinct().ToArray())
        {
            RemoveEffect(effect);
        }
    }

    private void RemoveEffect(NightmareEffect effect)
    {
        if (!_byCaster.TryGetValue(effect.CasterIndex, out var casterEffect)
            || casterEffect != effect)
        {
            return;
        }

        _byCaster.Remove(effect.CasterIndex);
        if (_byTarget.TryGetValue(effect.TargetIndex, out var targetEffect) && targetEffect == effect)
        {
            _byTarget.Remove(effect.TargetIndex);
        }

        var volume = Utilities.GetEntityFromIndex<CPostProcessingVolume>((int)effect.VolumeIndex);
        if (volume is { IsValid: true })
        {
            volume.Remove();
        }
    }

    private static float FiniteOr(float value, float fallback, float minimum, float maximum) =>
        Math.Clamp(float.IsFinite(value) ? value : fallback, minimum, maximum);
}

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

public sealed class GlazService : IDisposable
{
    private readonly HashSet<uint> _holders = new();
    private readonly HashSet<int> _smokes = new();

    public void AddHolder(CCSPlayerController player)
    {
        if (player.IsValid)
        {
            _holders.Add(player.Index);
        }
    }

    public void RemoveHolder(uint controllerIndex) => _holders.Remove(controllerIndex);

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _smokes.Clear();
        return HookResult.Continue;
    }

    public void OnSmokeDetonate(EventSmokegrenadeDetonate @event)
    {
        if (@event.Entityid > 0)
        {
            _smokes.Add(@event.Entityid);
        }
    }

    public void OnSmokeExpired(EventSmokegrenadeExpired @event) => _smokes.Remove(@event.Entityid);

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_holders.Count == 0 || _smokes.Count == 0)
        {
            return;
        }

        var smokeIndexes = _smokes
            .Select(index => Utilities.GetEntityFromIndex<CBaseEntity>(index))
            .Where(entity => entity is { IsValid: true })
            .Select(entity => entity!.Index)
            .ToArray();
        if (smokeIndexes.Length == 0)
        {
            return;
        }

        foreach (var (info, viewer) in infoList)
        {
            if (viewer is not { IsValid: true } || !CanSeeThroughSmoke(viewer))
            {
                continue;
            }

            foreach (var smokeIndex in smokeIndexes)
            {
                info.TransmitEntities.Remove(smokeIndex);
            }
        }
    }

    public void Dispose()
    {
        _holders.Clear();
        _smokes.Clear();
    }

    private bool CanSeeThroughSmoke(CCSPlayerController viewer)
    {
        if (_holders.Contains(viewer.Index))
        {
            return true;
        }

        var observedHandle = viewer.Pawn.Value?.ObserverServices?.ObserverTarget?.Value?.Handle ?? nint.Zero;
        if (observedHandle == nint.Zero)
        {
            return false;
        }

        foreach (var holderIndex in _holders)
        {
            var holder = Utilities.GetPlayerFromIndex((int)holderIndex);
            if (holder?.PlayerPawn.Value is { IsValid: true } pawn && pawn.Handle == observedHandle)
            {
                return true;
            }
        }

        return false;
    }
}

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace Myrt1eSkill_Remake.Core;

public sealed class FriendlyFireService
{
    private ConVar? _autoKick;
    private bool _originalAutoKick;
    private bool _overridden;

    public void SuppressAutoKick()
    {
        if (_overridden)
        {
            return;
        }

        _autoKick ??= ConVar.Find("mp_autokick");
        if (_autoKick is null)
        {
            return;
        }

        _originalAutoKick = _autoKick.GetPrimitiveValue<bool>();
        _autoKick.SetValue(false);
        _overridden = true;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Reset();
        return HookResult.Continue;
    }

    public void Reset()
    {
        if (!_overridden || _autoKick is null)
        {
            return;
        }

        _autoKick.SetValue(_originalAutoKick);
        _overridden = false;
    }
}

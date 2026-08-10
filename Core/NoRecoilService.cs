using CounterStrikeSharp.API.Modules.Cvars;

namespace Myrt1eSkill_Remake.Core;

public sealed class NoRecoilService
{
    private ConVar? _noSpread;
    private bool _originalNoSpread;
    private int _holders;

    public void Acquire(EffectScope effects)
    {
        _noSpread ??= ConVar.Find("weapon_accuracy_nospread");
        if (_noSpread is null)
        {
            return;
        }

        if (_holders++ == 0)
        {
            _originalNoSpread = _noSpread.GetPrimitiveValue<bool>();
            _noSpread.SetValue(true);
        }

        var released = false;
        effects.RegisterCleanup(() =>
        {
            if (released)
            {
                return;
            }

            released = true;
            Release();
        });
    }

    public void Reset()
    {
        if (_holders > 0 && _noSpread is not null)
        {
            _noSpread.SetValue(_originalNoSpread);
        }

        _holders = 0;
    }

    private void Release()
    {
        if (_holders <= 0)
        {
            return;
        }

        _holders--;
        if (_holders == 0 && _noSpread is not null)
        {
            _noSpread.SetValue(_originalNoSpread);
        }
    }
}

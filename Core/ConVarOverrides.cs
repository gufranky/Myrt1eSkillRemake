using CounterStrikeSharp.API.Modules.Cvars;

namespace Myrt1eSkill_Remake.Core;

public static class ConVarOverrides
{
    public static void Set(EffectScope effects, string name, bool value)
    {
        var conVar = FindRequired(name);
        var original = conVar.GetPrimitiveValue<bool>();
        effects.RegisterCleanup(() => conVar.SetValue(original));
        conVar.SetValue(value);
    }

    public static void Set(EffectScope effects, string name, float value)
    {
        var conVar = FindRequired(name);
        var original = conVar.GetPrimitiveValue<float>();
        effects.RegisterCleanup(() => conVar.SetValue(original));
        conVar.SetValue(value);
    }

    public static float GetFloat(string name) => FindRequired(name).GetPrimitiveValue<float>();

    private static ConVar FindRequired(string name)
    {
        return ConVar.Find(name)
            ?? throw new InvalidOperationException($"Required CS2 ConVar was not found: {name}");
    }
}

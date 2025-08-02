using Verse;

namespace Rimgate;

public class CompProperties_Toggle : CompProperties
{
    public bool defaultState;

    [NoTranslate]
    public string commandTexture = "UI/Commands/DesirePower";

    [NoTranslate]
    public string commandLabelKey = "RG_ToggleLabel";

    [NoTranslate]
    public string commandDescKey = "RG_ToggleDescription";

    public CompProperties_Toggle() => compClass = typeof(Comp_Toggle);
}
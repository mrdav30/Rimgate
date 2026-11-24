using Verse;

namespace Rimgate;

public class CompProperties_Toggle : CompProperties
{
    public bool isOnByDefault;

    public string onSignal = "ToggledOn";

    public string offSignal = "ToggledOff";

    [NoTranslate]
    public string commandOnIconTexPath = "UI/Commands/DesirePower";

    [NoTranslate]
    public string commandOffIconTexPath;

    [NoTranslate]
    public string commandLabelKey = "RG_ToggleLabel";

    [NoTranslate]
    public string commandDescKey = "RG_ToggleDescription";

    public GraphicData offGraphicData;
}
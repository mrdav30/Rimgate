using Verse;

namespace Rimgate;

public class HediffCompProperties_SurgeryInspectionBiosignature : HediffCompProperties_SurgeryInspectable
{
    public ThingDef biosignatureThingDef;

    public HediffCompProperties_SurgeryInspectionBiosignature()
    {
        compClass = typeof(HediffComp_SurgeryInspectableBiosignature);
    }
}

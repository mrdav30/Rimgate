using Verse;

namespace Rimgate;

public class HediffCompProperties_SurgeryInspectionBiosignature : HediffCompProperties_SurgeryInspectable
{
    public ThingDef biosignatureThingDef;

    public bool removedOnInspection;

    public HediffCompProperties_SurgeryInspectionBiosignature()
    {
        compClass = typeof(HediffComp_SurgeryInspectableBiosignature);
    }
}

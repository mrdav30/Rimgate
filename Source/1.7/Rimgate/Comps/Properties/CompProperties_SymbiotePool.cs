using Verse;

namespace Rimgate;

public class CompProperties_SymbiotePool : CompProperties
{
    public ThingDef symbioteQueenDef;
    public ThingDef productSymbioteDef;

    public float daysPerSymbiote = 3f;
    public float fuelPerSymbiote = 5f;

    // Upkeep
    public int freeSymbiotesBeforeUpkeep = 0;
    public float upkeepFuelPerDayBase = 0.10f;
    public float upkeepFuelPerExtraSymbiote = 0.35f;

    public ThingDef feralTrapDef;
    public float queenHealthPctToGoFeral = 0.5f;
    public int starvationDamagePerEvent = 1;

    public int healPerThingPerUpkeepEvent = 1;
    public int maxThingsHealedPerUpkeepEvent = 3;

    public CompProperties_SymbiotePool() => compClass = typeof(Comp_SymbiotePool);
}

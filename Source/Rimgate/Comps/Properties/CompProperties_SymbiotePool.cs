using Verse;

namespace Rimgate;

public class CompProperties_SymbiotePool : CompProperties
{
    public ThingDef symbioteQueenDef;
    public ThingDef productSymbioteDef;

    public float daysPerSymbiote = 3f;
    public float fuelPerSymbiote = 5f;

    // Upkeep
    public int freeSymbiotesBeforeUpkeep = 1;
    public float upkeepFuelPerDayBase = 0.10f;
    public float upkeepFuelPerExtraSymbiote = 0.35f;

    // FX
    public bool spawnSymbioteMotes = true;
    public int moteIntervalTicks = 450;
    public float moteBaseScale = 0.75f;        
    public FloatRange moteScaleRange = new FloatRange(0.4f, 1.1f);
    public float moteRotationSpeed = 60f;

    public CompProperties_SymbiotePool() => compClass = typeof(Comp_SymbiotePool);
}

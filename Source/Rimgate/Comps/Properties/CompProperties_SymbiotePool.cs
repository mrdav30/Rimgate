using Verse;

namespace Rimgate;

public class CompProperties_SymbiotePool : CompProperties
{
    public ThingDef symbioteQueenDef;

    public ThingDef productSymbioteDef;

    public float daysPerSymbiote = 3f;

    public float fuelPerSymbiote = 5f;

    public bool spawnSymbioteMotes = true;

    // ~5 sec at 60 TPS
    public int moteIntervalTicks = 300;

    // multiplied by random factor
    public float moteBaseScale = 0.8f;        
    
    public FloatRange moteScaleRange = new FloatRange(0.6f, 1.2f);

    // degrees/sec-ish
    public float moteRotationSpeed = 60f;

    public CompProperties_SymbiotePool() => compClass = typeof(Comp_SymbiotePool);
}

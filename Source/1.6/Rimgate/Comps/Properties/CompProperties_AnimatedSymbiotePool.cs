using Verse;

namespace Rimgate;

public class CompProperties_AnimatedSymbiotePool : CompProperties
{
    public bool enabledByDefault;
    public int moteIntervalTicks = 450;
    public float moteBaseScale = 0.75f;
    public FloatRange moteScaleRange = new FloatRange(0.4f, 1.1f);
    public float moteRotationSpeed = 60f;

    public CompProperties_AnimatedSymbiotePool() => compClass = typeof(Comp_AnimatedSymbiotePool);
}

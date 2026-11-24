using Verse;

namespace Rimgate;

public class CompProperties_AlternateApparel : CompProperties
{
    public ThingDef alternateDef;

    public SoundDef toggleSound;

    public bool isAlternateOnly;

    public CompProperties_AlternateApparel() => compClass = typeof(Comp_AlternateApparel);
}

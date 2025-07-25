using Verse;

namespace Rimgate;

public class CompProperties_AnimatedSarcophagus : CompProperties
{
    // Define defaults if not specified in XML defs

    public GraphicData sarchophagusGlowGraphicData;

    public CompProperties_AnimatedSarcophagus() => compClass = typeof(Comp_AnimatedSarcophagus);
}

using Verse;

namespace Rimgate;

public class CompProperties_BodyGraphicSwapper : CompProperties
{
    public GraphicData tierOneGraphicData;

    public GraphicData tierOneFemaleGraphicData;

    public GraphicData tierTwoGraphicData;

    public GraphicData tierTwoFemaleGraphicData;

    public CompProperties_BodyGraphicSwapper() => compClass = typeof(Comp_BodyGraphicSwapper);
}

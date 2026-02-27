using Verse;

namespace Rimgate;

public class CompProperties_BodyGraphicSwapper : CompProperties
{
    public float tierOneHealthThreshold = 0.75f;

    public float tierTwoHealthThreshold = 0.35f;

    public GraphicData tierOneGraphicData;

    public GraphicData tierOneFemaleGraphicData;

    public GraphicData tierTwoGraphicData;

    public GraphicData tierTwoFemaleGraphicData;

    public CompProperties_BodyGraphicSwapper() => compClass = typeof(Comp_BodyGraphicSwapper);
}

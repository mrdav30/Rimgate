using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace Rimgate;

public class CompProperties_CloningPod : CompProperties
{
    public GraphicData backgroundGraphicData;

    public GraphicData fullGraphicData;

    public GraphicData emptyGraphicData;

    public CompProperties_CloningPod() => compClass = typeof(Comp_CloningPod);
}
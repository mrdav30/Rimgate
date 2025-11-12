using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_CloningPod : CompProperties
{
    public Color idleCycleColor = new Color(0.9f, 1f, 0.16f);

    public Color operatingColor = new Color(0.89f, 0.24f, 0.04f);

    public GraphicData backgroundGraphicData;

    public GraphicData fullGraphicData;

    public GraphicData emptyGraphicData;

    public CompProperties_CloningPod() => compClass = typeof(Comp_CloningPod);
}
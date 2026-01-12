using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.AI;

namespace Rimgate;

public class Comp_DHDControl : ThingComp
{
    public CompProperties_DHDControl Props => (CompProperties_DHDControl)props;

    public Graphic ActiveGraphic => _activeGraphic ??= Props.activeGraphicData?.Graphic;

    private Graphic _activeGraphic;
}

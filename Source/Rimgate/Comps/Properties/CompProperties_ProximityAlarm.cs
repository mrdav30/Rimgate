using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class CompProperties_ProximityAlarm : CompProperties
{
    public bool triggerOnPawnInRoom;

    public bool triggerOnPawnOnMap;

    public float radius;

    public int enableAfterTicks;

    public List<ThingDef> ignoreCasketDefs;

    public bool onlyHumanlike;

    public bool triggeredBySkipPsycasts;
}
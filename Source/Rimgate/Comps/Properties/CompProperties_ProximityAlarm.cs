using Verse;

namespace Rimgate;

public class CompProperties_ProximityAlarm : CompProperties
{
    public bool triggerOnPawnInRoom;

    public float radius;

    public int enableAfterTicks;

    public bool onlyHumanlike;

    public bool triggeredBySkipPsycasts;
}
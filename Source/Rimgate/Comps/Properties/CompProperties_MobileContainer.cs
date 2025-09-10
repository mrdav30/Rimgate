using UnityEngine;
using Verse;

namespace Rimgate;


public class CompProperties_MobileContainer : CompProperties
{
    public float massCapacity = 150f;

    public bool canChangeAssignedThingsAfterStarting;

    public float frontOffset = 1.0f;  // how far in front of pusher to draw/spawn

    public float moveSpeedFactorWhilePushing = 0.9f; // pusher slowdown

    // only items within this radius are eligible
    public float loadRadius = 25f;

    public bool requiresPower = false;

    public bool shouldTickContents = true;

    public bool showMassInInspectString;

    public int stallFinalizeDelayTicks = 600;  // ~10s at 60 tps

    public bool notifyOnFinalize = true;  // show one message when auto-finalizing

    public CompProperties_MobileContainer()
    {
        compClass = typeof(Comp_MobileContainer);
    }
}

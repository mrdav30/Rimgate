using UnityEngine;
using Verse;

namespace Rimgate;


public class CompProperties_MobileContainer : CompProperties
{
    public float massCapacity = 150f;

    public bool canChangeAssignedThingsAfterStarting = true;

    public float frontOffset = 1.0f;  // how far in front of pusher to draw/spawn

    public float slowdownSeverity = 0.1f; // pusher slowdown

    // only items within this radius are eligible
    public float loadRadius = 25f;

    public bool shouldTickContents = true;

    public bool showMassInInspectString = true;

    public int stallFinalizeDelayTicks = 600;  // ~10s at 60 tps

    public bool notifyOnFinalize = true;  // show one message when auto-finalizing

    public CompProperties_MobileContainer()
    {
        compClass = typeof(Comp_MobileContainer);
    }
}

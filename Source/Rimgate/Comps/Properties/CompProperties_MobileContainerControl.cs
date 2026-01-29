using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace Rimgate;


public class CompProperties_MobileContainerControl : CompProperties
{
    public float massCapacity = 150f;

    public bool canChangeAssignedThingsAfterStarting = true;

    public float frontOffset = 1.0f;  // how far in front of pusher to draw/spawn

    public float slowdownSeverity = 0.1f; // pusher slowdown

    // only items within this radius are eligible
    public float loadRadius = 10f;

    public bool shouldTickContents = true;

    public bool showMassInInspectString = true;

    public int stallFinalizeDelayTicks = 600;  // ~10s at 60 tps

    public bool notifyOnFinalize = true;  // show one message when auto-finalizing

    public CompProperties_MobileContainerControl() => compClass = typeof(Comp_MobileContainerControl);

    [DebuggerHidden]
    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Stat_Cart_LoadoutRange_Label".Translate(),
            loadRadius.ToString("F0") + " W",
            "RG_Stat_Cart_LoadoutRange_Desc".Translate(),
            4994);
    }
}

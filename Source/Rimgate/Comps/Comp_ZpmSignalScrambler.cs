using RimWorld;
using Verse;

namespace Rimgate;

public class Comp_ZpmSignalScrambler : ThingComp
{
    private CompPowerTrader Power 
        => _cachedPower ??= parent.GetComp<CompPowerTrader>();

    private CompFacility Facility 
        => _cachedFacility ??= parent.GetComp<CompFacility>();

    private MapComponent_ZpmRaidTracker Tracker
           => _cachedTracker ??= parent.Map?.GetComponent<MapComponent_ZpmRaidTracker>();

    private MapComponent_ZpmRaidTracker _cachedTracker;

    private CompFacility _cachedFacility;

    private CompPowerTrader _cachedPower;

    public CompProperties_ZpmSignalScrambler Props => (CompProperties_ZpmSignalScrambler)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        UpdateSuppression();
    }

    public override void CompTick()
    {
        if (parent.IsHashIntervalTick(60)) // 1s-ish
            UpdateSuppression();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        Tracker?.SetSuppressionActive(false);
    }

    private void UpdateSuppression()
    {
        // Power remains optional.
        bool powered = Power == null || Power.PowerOn;

        // Require at least one *linked* ZPM facility.
        bool linkedToZpm = false;
        if (Facility?.LinkedBuildings != null)
            linkedToZpm = Facility.LinkedBuildings
                .Any(b => b?.def == RimgateDefOf.Rimgate_ZPM);

        bool shouldSuppress = powered && linkedToZpm;

        // Only suppress when powered AND linked to >=1 ZPM.
        Tracker?.SetSuppressionActive(shouldSuppress);
    }

    public override string CompInspectStringExtra()
    {
        bool powered = Power == null || Power.PowerOn;
        int linkCount = Facility?.LinkedBuildings?.Count(f => f.def == RimgateDefOf.Rimgate_ZPM) ?? 0;

        if (!powered) 
            return "RG_JammerInactive".Translate() + $" ({"NoPower".Translate()})";
        return (linkCount > 0)
            ? "RG_JammerActive".Translate()
            : "RG_JammerInactive".Translate() + $" ({"RG_JammerNoLink".Translate()})";
    }
}

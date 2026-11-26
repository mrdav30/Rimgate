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

    private int LinkedZpmCount => Facility?.LinkedBuildings?.Count(b =>
        b is Building_ZPM || b is Building_ZPMHousing { HasAnyZpm: true }) ?? 0;

    public bool IsSuppressing => parent.Spawned && (Power == null || Power.PowerOn) && LinkedZpmCount > 0;

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
        map.GetComponent<MapComponent_ZpmRaidTracker>()?.NotifySuppressionChanged();
    }

    private void UpdateSuppression()
    {
        Tracker?.NotifySuppressionChanged();
    }

    public override string CompInspectStringExtra()
    {
        bool powered = Power == null || Power.PowerOn;
        int linkCount = LinkedZpmCount;

        if (!powered)
            return "RG_JammerInactive".Translate() + $" ({"NoPower".Translate()})";
        return (linkCount > 0)
            ? "RG_JammerActive".Translate()
            : "RG_JammerInactive".Translate() + $" ({"RG_JammerNoLink".Translate()})";
    }
}

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

/// <summary>
/// 2x2 containment housing for up to three ZPMs.
/// - Acts as the *powernet battery* once at least one ZPM is loaded.
/// - Scales effective capacity linearly with ZPM count (1–3).
/// - Applies the ZPM cluster bonus when holding 3 ZPMs,
///   reusing Building_ZPM.ClusterBonus.
/// - Handles dark-energy recharge similar to Building_ZPM.
/// - Notified by and notifies MapComponent_ZpmRaidTracker so that
///   raid/shroud logic still works when ZPMs are inside.
/// </summary>
[StaticConstructorOnStartup]
public class Building_ZPMHousing : Building, IThingHolder
{
    private const int MaxZpms = 3;

    public const float ClusterBonus = 0.25f;

    // Use the same base capacity as a standalone ZPM (48000)
    // so 3x ZPMs == triple capacity
    private const float BaseCapacityPerZpm = 48000f;

    // Dark energy state
    private int _darkEnergyReserve;
    private int _maxDarkEnergy;

    private bool _isBroadcasting;

    private ThingOwner<Thing> _innerContainer;

    private CompPowerBattery Battery => _battery ??= GetComp<CompPowerBattery>();

    private CompPowerBattery _battery;

    private CompAffectedByFacilities Facilities => _facilities ??= GetComp<CompAffectedByFacilities>();

    private CompAffectedByFacilities _facilities;

    private MapComponent_ZpmRaidTracker Tracker => _tracker ??= Map?.GetComponent<MapComponent_ZpmRaidTracker>();

    private MapComponent_ZpmRaidTracker _tracker;

    private static Graphic _southWestZpmOverlay;

    private static Graphic _southEastZpmOverlay;

    private static Graphic _northZpmOverlay;

    static Building_ZPMHousing()
    {
        Vector2 drawSize = RimgateDefOf.Rimgate_ZPMHousing.graphicData.drawSize;

        // south west overlay
        _southWestZpmOverlay = new Graphic_Single();
        var swData = new GraphicData()
        {
            texPath = "FX/RGZPMHolder_SouthWestZPM",
            graphicClass = typeof(Graphic_Single),
            drawSize = drawSize
        };

        _southWestZpmOverlay.Init(new GraphicRequest(
            swData.graphicClass,
            swData.texPath,
            ShaderDatabase.DefaultShader,
            drawSize,
            Color.white,
            Color.white,
            swData,
            0,
            null,
            null));


        // south east overlay
        _southEastZpmOverlay = new Graphic_Single();
        var seData = new GraphicData()
        {
            texPath = "FX/RGZPMHolder_SouthEastZPM",
            graphicClass = typeof(Graphic_Single),
            drawSize = drawSize
        };

        _southEastZpmOverlay.Init(new GraphicRequest(
            seData.graphicClass,
            seData.texPath,
            ShaderDatabase.DefaultShader,
            drawSize,
            Color.white,
            Color.white,
            seData,
            0,
            null,
            null));

        // north overlay
        _northZpmOverlay = new Graphic_Single();
        var nData = new GraphicData()
        {
            texPath = "FX/RGZPMHolder_NorthZPM",
            graphicClass = typeof(Graphic_Single),
            drawSize = drawSize
        };

        _northZpmOverlay.Init(new GraphicRequest(
            nData.graphicClass,
            nData.texPath,
            ShaderDatabase.DefaultShader,
            drawSize,
            Color.white,
            Color.white,
            nData,
            0,
            null,
            null));
    }

    public Building_ZPMHousing()
    {
        _innerContainer = new ThingOwner<Thing>(this);
    }

    #region Properties

    public int ZpmCount
    {
        get
        {
            if (_innerContainer == null) return 0;
            int count = 0;
            for (int i = 0; i < _innerContainer.Count; i++)
            {
                if (_innerContainer[i]?.def == RimgateDefOf.Rimgate_ZPM)
                    count++;
            }
            return count;
        }
    }

    public bool HasAnyZpm => ZpmCount > 0;
    public bool IsFull => ZpmCount >= MaxZpms;

    private float EffectiveMaxEnergy => BaseCapacityPerZpm * ZpmCount;

    /// <summary>
    /// Whether this housing is currently allowed to recharge dark energy
    /// </summary>
    private bool CanRecharge
    {
        get
        {
            if (!HasAnyZpm) return false;
            if (Battery == null || Facilities == null) return false;
            if (Faction.IsOfPlayerFaction() && !ResearchUtil.ParallelSubspaceCouplingComplete) return false;
            return ActiveDiverterCount() > 0;
        }
    }

    public bool CanAcceptZpm => !IsFull;

    /// <summary>
    /// Cluster bonus is only applied when we have exactly 3 ZPMs inserted.
    /// </summary>
    private float CurrentClusterMultiplier()
    {
        if (!HasAnyZpm)
            return 0f;

        float mult = 1f;
        if (ZpmCount == 3)
            mult += ClusterBonus;

        return mult;
    }

    #endregion

    #region Thing / lifecycle

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        RecomputeMaxDarkEnergy();
        ClampBatteryToEffectiveMax();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        // If we were broadcasting, tell the tracker we’re done.
        if (_isBroadcasting && Tracker != null)
        {
            _isBroadcasting = false;
            Tracker.NotifyZpmEndedBroadcast();
        }

        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        // Drop any contained ZPMs on destruction.
        if (_innerContainer != null && _innerContainer.Count > 0 && Map != null)
        {
            _innerContainer.TryDropAll(
                Position,
                Map,
                ThingPlaceMode.Near);
        }

        base.Destroy(mode);
    }

    public override void TickRare()
    {
        if (!Spawned || this.IsMinified())
            return;

        base.TickRare();

        ClampBatteryToEffectiveMax();
        HandleDarkEnergy();
        CheckBroadcast();
    }

    public override string GetInspectString()
    {
        var baseStr = base.GetInspectString();
        var sb = new System.Text.StringBuilder(baseStr);

        if (sb.Length > 0)
            sb.AppendLine();

        if (HasAnyZpm)
        {
            sb.AppendLine();
            sb.Append("RG_ZpmDarkEnergyReserve".Translate(
                _darkEnergyReserve,
                _maxDarkEnergy));
        }

        return sb.ToString().TrimEndNewlines();
    }

    #endregion

    #region Dark energy + battery

    private void RecomputeMaxDarkEnergy()
    {
        if (!HasAnyZpm)
        {
            _maxDarkEnergy = 0;
            _darkEnergyReserve = 0;
            return;
        }

        // Mirror Building_ZPM’s logic: max dark energy is some factor of physical storage.
        float maxStorage = EffectiveMaxEnergy;
        _maxDarkEnergy = Mathf.CeilToInt(maxStorage * 1.25f);

        if (_darkEnergyReserve > _maxDarkEnergy)
            _darkEnergyReserve = _maxDarkEnergy;
    }

    private void ClampBatteryToEffectiveMax()
    {
        if (Battery == null)
            return;

        if (!HasAnyZpm)
        {
            // No ZPMs loaded: housing should be inert as a battery.
            Battery.SetStoredEnergyPct(0f);
            return;
        }

        float effectiveMax = EffectiveMaxEnergy;
        if (effectiveMax <= 0f)
            return;

        float physicalMax = Battery.Props.storedEnergyMax;
        float current = Battery.StoredEnergy;

        if (current > effectiveMax)
        {
            float pct = effectiveMax / physicalMax;
            Battery.SetStoredEnergyPct(pct);
        }
    }

    private void HandleDarkEnergy()
    {
        if (!HasAnyZpm)
        {
            _darkEnergyReserve = 0;
            return;
        }

        if (Battery == null)
            return;

        if (CanRecharge)
        {
            float clusterMult = CurrentClusterMultiplier();
            if (clusterMult <= 0f)
                return;

            int incrementBase = Building_ZPM.EnergyIncrement;
            float netGain = Battery.PowerNet?.CurrentEnergyGainRate() ?? 0f;
            int increment = Mathf.RoundToInt(incrementBase * clusterMult);

            if (increment > 0)
            {
                // Positive net gain: pull more aggressively.
                if (netGain > 0.01f)
                    _darkEnergyReserve = Mathf.Min(_darkEnergyReserve + increment, _maxDarkEnergy);
                else
                {
                    // Trickle
                    int trickle = Mathf.Max(1, increment / 4);
                    _darkEnergyReserve = Mathf.Min(_darkEnergyReserve + trickle, _maxDarkEnergy);
                }
            }
        }

        // Convert dark energy into stored grid energy if:
        // - battery is not near effective max
        // - reserve has reached the overflow threshold
        float effectiveMax = EffectiveMaxEnergy;
        float current = Battery.StoredEnergy;
        int overflowLimit = Building_ZPM.OverflowLimit;

        if (current + 1f < effectiveMax
            && _darkEnergyReserve >= overflowLimit)
        {
            float allowed = effectiveMax - current;
            float toAdd = Mathf.Min(overflowLimit, allowed);

            if (toAdd > 0.01f)
            {
                Battery.AddEnergy(toAdd);
                _darkEnergyReserve -= Mathf.RoundToInt(toAdd);
            }
        }
    }

    #endregion

    #region Broadcast / raids

    private void CheckBroadcast()
    {
        if (Battery == null || Tracker == null)
            return;

        bool connected = Battery.PowerNet != null;
        bool shouldBroadcast = Faction.IsOfPlayerFaction() && HasAnyZpm && connected;

        // Start broadcasting
        if (shouldBroadcast && !_isBroadcasting)
        {
            _isBroadcasting = true;
            Tracker.NotifyZpmBeganBroadcast();
        }
        // Stop broadcasting
        else if (!shouldBroadcast && _isBroadcasting)
        {
            _isBroadcasting = false;
            Tracker.NotifyZpmEndedBroadcast();
        }
    }

    #endregion

    #region Facilities helpers

    private int ActiveDiverterCount()
    {
        if (Facilities == null)
            return 0;

        int count = 0;
        List<Thing> linked = Facilities.LinkedFacilitiesListForReading;
        if (linked == null) return 0;

        for (int i = 0; i < linked.Count; i++)
        {
            var t = linked[i];
            if (t == null || t.Destroyed) continue;
            if (t.def != RimgateDefOf.Rimgate_SubspacePhaseDiverter) continue;

            var power = t.TryGetComp<CompPowerTrader>();
            if (power?.PowerOn == true)
                count++;
        }

        return count;
    }

    #endregion

    #region ZPM insert/eject helpers

    /// <summary>
    /// Insert a ZPM into this housing, transferring its battery charge into the
    /// housing’s battery (up to the effective capacity for the new count),
    /// and merging its dark energy reserve into the housing’s dark pool.
    /// </summary>
    public bool TryInsertZpm(Building_ZPM zpm)
    {
        if (zpm == null || zpm.Destroyed || !CanAcceptZpm)
            return false;

        if (Map == null)
            return false;

        // Ensure we have a battery
        var battery = Battery;
        if (battery == null)
            return false;

        // --- BATTERY: transfer ZPM stored energy into housing ---

        // Determine the new effective max if this ZPM is inserted
        int currentCount = ZpmCount;
        int newCount = Math.Min(currentCount + 1, MaxZpms);
        float newEffectiveMax = BaseCapacityPerZpm * newCount;

        // Pull charge from the ZPM’s battery
        var zBattery = zpm.Battery;
        float zEnergy = 0f;
        float zMax = 0f;

        if (zBattery != null)
        {
            zEnergy = zBattery.StoredEnergy;
            zMax = zBattery.Props.storedEnergyMax;
        }

        float housingCurrent = battery.StoredEnergy;

        // Amount we can add without exceeding effective max for the new count
        float allowed = Mathf.Max(0f, newEffectiveMax - housingCurrent);
        float transfer = Mathf.Min(zEnergy, allowed);

        if (transfer > 0f)
            battery.AddEnergy(transfer);

        // Empty the ZPM regardless; overflow beyond housing capacity is lost
        if (zBattery != null && zMax > 0f)
            zBattery.SetStoredEnergyPct(0f);

        // --- MOVE ZPM INTO CONTAINER ---

        // Despawn ZPM and store it in the inner container.
        if (zpm.Spawned)
            zpm.DeSpawn();

        _innerContainer.TryAdd(zpm);

        // --- DARK ENERGY: merge ZPM reserve into housing reserve ---

        // Recompute max dark energy for the new ZPM count
        RecomputeMaxDarkEnergy();

        int zDark = zpm.DarkEnergyReserve;
        if (zDark > 0)
        {
            // Add as much as we can without exceeding the new max
            int capacityLeft = Math.Max(0, _maxDarkEnergy - _darkEnergyReserve);
            int addDark = Math.Min(zDark, capacityLeft);

            if (addDark > 0)
                _darkEnergyReserve += addDark;

            // ZPM dark reserve is always zeroed when inserted;
            // any overflow beyond housing capacity is lost.
            zpm.SetDarkEnergyReserve(0);
        }

        // Final clamp on battery in case of edge cases
        ClampBatteryToEffectiveMax();

        return true;
    }

    /// <summary>
    /// Ejects a single ZPM from the housing, transferring a fair share
    /// of the housing’s stored energy and dark energy into the ejected ZPM’s battery.
    /// </summary>
    public bool TryEjectOneZpm(IntVec3 dropCell)
    {
        if (!HasAnyZpm || Map == null)
            return false;

        int beforeCount = ZpmCount;
        if (beforeCount <= 0)
            return false;

        // Find one ZPM in the inner container
        Building_ZPM zpm = null;
        for (int i = 0; i < _innerContainer.Count; i++)
        {
            if (_innerContainer[i]?.def == RimgateDefOf.Rimgate_ZPM)
            {
                zpm = _innerContainer[i] as Building_ZPM;
                break;
            }
        }

        if (zpm == null)
            return false;

        // Remove from container before placing
        _innerContainer.Remove(zpm);

        var battery = Battery;
        var zBattery = zpm.Battery;

        // --- BATTERY: split stored energy ---

        if (battery != null && zBattery != null)
        {
            float housingEnergy = battery.StoredEnergy;
            float perZpmShare = beforeCount > 0 ? housingEnergy / beforeCount : 0f;

            float zMax = zBattery.Props.storedEnergyMax;
            float give = Mathf.Min(perZpmShare, zMax);

            // Energy remaining in housing after giving this ZPM its share
            float remaining = Mathf.Max(0f, housingEnergy - give);

            // Set ZPM battery
            if (zMax > 0f && give > 0f)
                zBattery.SetStoredEnergyPct(give / zMax);
            else
                zBattery.SetStoredEnergyPct(0f);

            // Clamp housing remaining energy to the new effective max (N - 1)
            int newCount = Mathf.Max(0, beforeCount - 1);
            float newEffectiveMax = BaseCapacityPerZpm * newCount;
            if (remaining > newEffectiveMax)
                remaining = newEffectiveMax;

            float physMax = battery.Props.storedEnergyMax;
            float newPct = (physMax > 0f && remaining > 0f) ? remaining / physMax : 0f;
            battery.SetStoredEnergyPct(newPct);
        }

        // --- DARK ENERGY: split reserve ---

        if (beforeCount > 0)
        {
            int totalDark = _darkEnergyReserve;
            int perZpmDark = totalDark / beforeCount;

            int zpmMaxDark = zpm.MaxDarkEnergy;
            int giveDark = Math.Min(perZpmDark, zpmMaxDark);

            // Dark energy remaining in housing
            int remainingDark = Math.Max(0, totalDark - giveDark);

            zpm.SetDarkEnergyReserve(giveDark);
            _darkEnergyReserve = remainingDark;
        }

        // Recompute max dark energy for the new ZPM count and clamp reserve
        RecomputeMaxDarkEnergy();
        ClampBatteryToEffectiveMax();

        // Finally, place the ZPM back on the map near the housing
        GenPlace.TryPlaceThing(
            zpm,
            dropCell,
            Map,
            ThingPlaceMode.Near);

        return true;
    }

    #endregion

    #region IThingHolder

    public ThingOwner GetDirectlyHeldThings() => _innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref _innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref _darkEnergyReserve, "darkEnergyReserve", 0);
        Scribe_Values.Look(ref _maxDarkEnergy, "maxDarkEnergy", 0);
        Scribe_Values.Look(ref _isBroadcasting, "isBroadcasting", false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (_innerContainer == null)
                _innerContainer = new ThingOwner<Thing>(this);

            RecomputeMaxDarkEnergy();
        }
    }

    #endregion

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        if (!Spawned) return;

        int currentCount = _innerContainer.Count;
        if (currentCount == 0) return;

        var rot = flip ? Rotation.Opposite : Rotation;
        drawLoc.y += (AltitudeLayer.Item.AltitudeFor() + 0.01f);

        if (currentCount >= 1)
            _southWestZpmOverlay.Draw(drawLoc, rot, this);
        if (currentCount >= 2)
            _southEastZpmOverlay.Draw(drawLoc, rot, this);
        if (currentCount == 3)
            _northZpmOverlay.Draw(drawLoc, rot, this);
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
    {
        foreach (var opt in base.GetFloatMenuOptions(pawn))
            yield return opt;

        if (!HasAnyZpm)
            yield break;

        if (pawn.Drafted)
            yield break;

        if (!pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            yield break;

        yield return new FloatMenuOption(
            "RG_ZpmHousing_EjectOne".Translate(),
            () =>
            {
                var job = JobMaker.MakeJob(
                    RimgateDefOf.Rimgate_EjectZpmFromHousing,
                    this);
                job.playerForced = true;
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job);
            });

        if (ZpmCount <= 1) yield break;

        yield return new FloatMenuOption(
            "RG_ZpmHousing_EjectAll".Translate(),
            () =>
            {
                var job = JobMaker.MakeJob(
                    RimgateDefOf.Rimgate_EjectZpmFromHousing,
                    this);
                job.playerForced = true;
                job.count = ZpmCount; // eject as many as currently inserted
                pawn.jobs.TryTakeOrderedJob(job);
            });
    }

    public static Building_ZPMHousing FindBestHousingFor(Pawn pawn, Building_ZPM zpm)
    {
        var map = pawn.Map;
        if (map == null) return null;

        Building_ZPMHousing best = null;
        float bestDist = float.MaxValue;

        var housings = map.listerThings.ThingsOfDef(RimgateDefOf.Rimgate_ZPMHousing);
        for (int i = 0; i < housings.Count; i++)
        {
            var h = housings[i] as Building_ZPMHousing;
            if (h == null || !h.CanAcceptZpm) continue;
            if (!pawn.CanReach(h, PathEndMode.InteractionCell, Danger.Deadly)) continue;

            float d = (h.Position - zpm.Position).LengthHorizontalSquared;
            if (d < bestDist)
            {
                bestDist = d;
                best = h;
            }
        }

        return best;
    }
}

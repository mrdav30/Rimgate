using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;
using RimWorld.QuestGen;
using System.Text;
using Verse.Noise;
using static RimWorld.FleshTypeDef;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Building_ZPM : Building
{
    // 1 tile (cardinal/diagonal)
    public const float ClusterScanRadius = 1.1f;

    // total ZPMs in cluster (including this one)
    public const int ClusterThresholdTotal = 3;

    // bonus when hitting the threshold
    public const float ClusterBonus = 0.25f;

    // bonus for each ZPM beyond threshold
    public const float PerExtraZpmBonus = 0.05f;

    private static Dictionary<string, Graphic> _chargeGraphics;

    public bool IsBroadcasting => _isBroadcasting;

    private CompPowerBattery PowerBattery
        => _powerComp ??= GetComp<CompPowerBattery>();

    private CompPowerBattery _powerComp;

    private CompAffectedByFacilities ConnectedFacilities
    => _affectedbyFacilitiesComp ??= GetComp<CompAffectedByFacilities>();

    private CompAffectedByFacilities _affectedbyFacilitiesComp;

    private MapComponent_ZpmRaidTracker Tracker
   => _tracker ??= Map?.GetComponent<MapComponent_ZpmRaidTracker>();

    private MapComponent_ZpmRaidTracker _tracker;

    private int _darkEnergyReserve;

    private const int EnergyIncrement = 100;

    private const int OverflowLimit = 1000;

    private int _maxDarkEnergy = -1;

    private bool _isBroadcasting;

    private bool _wasConnectedLastTick;

    private bool IntegrationReady
        => RimgateDefOf.Rimgate_ZPMIntegration.IsFinished == true;

    private bool CouplingReady
        => RimgateDefOf.Rimgate_ParallelSubspaceCoupling?.IsFinished == true;

    public bool CanRecharge
    {
        get
        {
            if (Faction == Faction.OfPlayer && !CouplingReady) return false;
            if (ConnectedFacilities == null) return false;

            // must have at least one powered diverter
            return ActiveDiverterCount() > 0;
        }
    }

    static Building_ZPM()
    {
        _chargeGraphics ??= new();
        _chargeGraphics.Clear();
        string[] powerStates = { "Depleted", "25%", "50%", "75%", "Full" };
        GraphicData data = RimgateDefOf.Rimgate_ZPM.graphicData;
        foreach (var powerState in powerStates)
        {
            var graphic = new Graphic_Single();

            GraphicRequest request = new GraphicRequest(
                typeof(Graphic_Single),
                $"Things/Building/Artifact/ZPM/RGZPM_{powerState}",
                ShaderDatabase.DefaultShader,
                data.drawSize,
                Color.white,
                Color.white,
                data,
                0,
                null,
                null);

            graphic.Init(request);
            _chargeGraphics.Add(powerState, graphic);
        }
    }

    public override void PostMake()
    {
        base.PostMake();

        if (PowerBattery == null) return;

        int maxStorage = (int)PowerBattery.Props.storedEnergyMax;
        _maxDarkEnergy = (int)Math.Ceiling(maxStorage * 1.25);
        IntRange startingReserve = new IntRange(0, maxStorage);
        _darkEnergyReserve = startingReserve.RandomInRange;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        bool connected = PowerBattery?.PowerNet != null;

        // If not integrated yet, ensure we are NOT broadcasting.
        if (Faction == Faction.OfPlayer)
        {
            if (!IntegrationReady)
            {
                if (_isBroadcasting)
                {
                    _isBroadcasting = false;
                    Tracker?.NotifyZpmEndedBroadcast();
                }
                _wasConnectedLastTick = connected;
                return;
            }

            if (!_isBroadcasting && connected)
            {
                _isBroadcasting = true;
                Tracker?.NotifyZpmBeganBroadcast();
            }
        }

        _wasConnectedLastTick = connected;
    }

    public override void TickRare()
    {
        if (PowerBattery == null) return;
        if (Map == null) return;

        // Inert phase: no power, no dark energy, no broadcast.
        if (Faction == Faction.OfPlayer
            && !IntegrationReady)
        {
            if (_isBroadcasting)
            {
                _isBroadcasting = false;
                Tracker?.NotifyZpmEndedBroadcast();
            }
            PowerBattery.SetStoredEnergyPct(0f);
            return;
        }

        CheckBroadcast();
        HandleDarkEnergy();
        base.TickRare();
    }

    private void CheckBroadcast()
    {
        bool connected = PowerBattery?.PowerNet != null;
        if (Faction == Faction.OfPlayer
            && connected != _wasConnectedLastTick)
        {
            if (connected && !_isBroadcasting)
            {
                _isBroadcasting = true;
                Tracker?.NotifyZpmBeganBroadcast();
            }
            else if (!connected && _isBroadcasting)
            {
                _isBroadcasting = false;
                Tracker?.NotifyZpmEndedBroadcast();
            }
            _wasConnectedLastTick = connected;
        }
    }

    private void HandleDarkEnergy()
    {
        if (CanRecharge)
        {
            bool connected = PowerBattery?.PowerNet != null;
            float increment = EnergyIncrement * CurrentClusterMultiplier();
            if (connected && PowerBattery.PowerNet.CurrentEnergyGainRate() > 0.01f)
                // increment fully using excess power
                _darkEnergyReserve += (int)increment;
            else
            {
                bool solarFlare = Map?.gameConditionManager.ConditionIsActive(IncidentDefOf.SolarFlare.gameCondition) == true;
                _darkEnergyReserve += (int)(increment * (solarFlare ? 0.45f : 0.05f));
            }

            if (_darkEnergyReserve > _maxDarkEnergy)
                _darkEnergyReserve = _maxDarkEnergy;
        }
            
        if (PowerBattery.StoredEnergyPct < 0.98f
            && _darkEnergyReserve >= OverflowLimit) 
        {
            PowerBattery.AddEnergy(OverflowLimit);
            _darkEnergyReserve -= OverflowLimit;
        }
    }

    private float CurrentClusterMultiplier()
    {
        int total = CountNearbyZpms(Position, Map);
        if (total < ClusterThresholdTotal)
            return 1f;

        int extras = total - ClusterThresholdTotal;
        return 1f + ClusterBonus + (extras * PerExtraZpmBonus);
    }

    public static int CountNearbyZpms(
        IntVec3 center,
        Map map,
        Thing ignore = null)
    {
        int count = 0;
        foreach (var c in GenRadial.RadialCellsAround(center, Building_ZPM.ClusterScanRadius, true))
        {
            if (!c.InBounds(map)) continue;
            var t = c.GetFirstThing(map, RimgateDefOf.Rimgate_ZPM);
            if (t == null || t == ignore) continue;
            count++;
        }

        return count;
    }

    private int ActiveDiverterCount()
    {
        if (ConnectedFacilities == null) return 0;
        var list = ConnectedFacilities.LinkedFacilitiesListForReading;
        if (list == null || list.Count == 0) return 0;

        int n = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t?.def != RimgateDefOf.Rimgate_SubspacePhaseDiverter) continue;

            // Must be powered to contribute
            var power = t.TryGetComp<CompPowerTrader>();
            if (power != null && power.PowerOn) n++;
        }
        return n;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Faction == Faction.OfPlayer && _isBroadcasting)
        {
            _isBroadcasting = false;
            Map.GetComponent<MapComponent_ZpmRaidTracker>()?.NotifyZpmEndedBroadcast();
        }
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Faction == Faction.OfPlayer && _isBroadcasting)
        {
            _isBroadcasting = false;
            Map.GetComponent<MapComponent_ZpmRaidTracker>()?.NotifyZpmEndedBroadcast();
        }
        base.Destroy(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _isBroadcasting, "_isBroadcasting");
        Scribe_Values.Look(ref _wasConnectedLastTick, "_wasConnectedLastTick");
        Scribe_Values.Look(ref _maxDarkEnergy, "_maxDarkEnergy");
        Scribe_Values.Look(ref _darkEnergyReserve, "_darkEnergyReserve");
    }

    public override Graphic Graphic
    {
        get
        {
            // For when it's minified or in a trade ship.
            if (PowerBattery == null)
                return base.DefaultGraphic;

            // var chargePercent = (int) ((float) currentCapacitorCharge / (float) maxCapacitorCharge) * 100;
            var chargePercent = (int)(PowerBattery.StoredEnergyPct * 100);
            if (chargePercent <= 10) return _chargeGraphics["Depleted"];
            if (chargePercent <= 25) return _chargeGraphics["25%"];
            if (chargePercent <= 50) return _chargeGraphics["50%"];
            if (chargePercent <= 75) return _chargeGraphics["75%"];
            return _chargeGraphics["Full"];
        }
    }

    public override string GetInspectString()
    {
        if (Faction == Faction.OfPlayer && !IntegrationReady)
            return "Inert";

        StringBuilder sb = new();
        if (PowerBattery?.PowerNet == null) return sb.ToString();

        sb.Append(PowerBattery.CompInspectStringExtra());

        if (sb.Length > 0) sb.AppendLine();
        sb.Append("RG_ZpmDarkEnergyReserve".Translate(_darkEnergyReserve, _maxDarkEnergy));

        if (CouplingReady)
        {
            float mult = CurrentClusterMultiplier();
            if (mult > 1f)
            {
                if (sb.Length > 0) sb.AppendLine();
                int pct = (int)Mathf.Round((mult - 1f) * 100f);
                sb.Append("RG_ZpmSynergyBonus".Translate(pct));
            }
        }

        if (Tracker != null)
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(
                Tracker.SuppressionActive
                ? "RG_ZPMsSignalBlocked".Translate()
                : "RG_ZPMsSignalUnblocked".Translate());
        }

        return sb.ToString();
    }

    public static Thing FindZpmOnMap(Map map, Thing thingToIgnore = null)
    {
        Thing zpmOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing.def == RimgateDefOf.Rimgate_ZPM)
            {
                zpmOnMap = thing;
                break;
            }
        }

        return zpmOnMap;
    }
}

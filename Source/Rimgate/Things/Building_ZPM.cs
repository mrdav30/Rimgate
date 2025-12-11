using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using static RimWorld.FleshTypeDef;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Building_ZPM : Building
{
    public const int EnergyIncrement = 100;

    public const int OverflowLimit = 1000;

    public const float SolarFlareFactor = 0.45f;

    public const float TrickleFactor = 0.05f;

    private static Dictionary<string, Graphic> _chargeGraphics;

    public bool IsBroadcasting => _isBroadcasting;

    public CompPowerBattery Battery => _battery ??= GetComp<CompPowerBattery>();

    private CompPowerBattery _battery;

    public float EffectiveMaxEnergy => Battery?.Props?.storedEnergyMax ?? 0;

    public CompAffectedByFacilities Facilities => _facilities ??= GetComp<CompAffectedByFacilities>();

    private CompAffectedByFacilities _facilities;

    public MapComponent_ZpmRaidTracker Tracker => _tracker ??= Map?.GetComponent<MapComponent_ZpmRaidTracker>();

    private MapComponent_ZpmRaidTracker _tracker;

    public int DarkEnergyReserve => _darkEnergyReserve;

    private int _darkEnergyReserve;

    public int MaxDarkEnergy => _maxDarkEnergy;

    private int _maxDarkEnergy = -1;

    private bool _isBroadcasting;

    private bool _wasConnectedLastTick;

    public bool SolarFlareActive => Map?.gameConditionManager.ConditionIsActive(IncidentDefOf.SolarFlare.gameCondition) == true;

    public bool CanRecharge
    {
        get
        {
            if (Battery == null || Facilities == null) return false;
            if (Faction.IsOfPlayerFaction() && !ResearchUtil.ParallelSubspaceCouplingComplete) return false;
            return ActiveDiverterCount() > 0;
        }
    }

    public override Graphic Graphic
    {
        get
        {
            // For when it's minified or in a trade ship.
            if (Battery == null)
                return base.DefaultGraphic;

            // var chargePercent = (int) ((float) currentCapacitorCharge / (float) maxCapacitorCharge) * 100;
            var chargePercent = (int)(Battery.StoredEnergyPct * 100);
            if (chargePercent <= 10) return _chargeGraphics["Depleted"];
            if (chargePercent <= 25) return _chargeGraphics["25%"];
            if (chargePercent <= 50) return _chargeGraphics["50%"];
            if (chargePercent <= 75) return _chargeGraphics["75%"];
            return _chargeGraphics["Full"];
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

        if (Battery == null) return;

        // Set starting energy levels
        _maxDarkEnergy = Mathf.CeilToInt(EffectiveMaxEnergy * 1.25f);
        _darkEnergyReserve = new IntRange(0, _maxDarkEnergy).RandomInRange;
        Battery.SetStoredEnergyPct(new FloatRange(0f, 0.75f).RandomInRange);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        bool connected = Battery?.PowerNet != null;

        // If not integrated yet, ensure we are NOT broadcasting.
        if (Faction.IsOfPlayerFaction())
        {
            if (!ResearchUtil.ZPMIntegrationComplete)
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
        if (Battery == null) return;
        if (Map == null) return;

        // Inert phase: no power, no dark energy, no broadcast.
        if (Faction.IsOfPlayerFaction()
            && !ResearchUtil.ZPMIntegrationComplete)
        {
            if (_isBroadcasting)
            {
                _isBroadcasting = false;
                Tracker?.NotifyZpmEndedBroadcast();
            }
            return;
        }

        CheckBroadcast();
        HandleDarkEnergy();
        base.TickRare();
    }

    private void CheckBroadcast()
    {
        bool connected = Battery?.PowerNet != null;
        if (Faction.IsOfPlayerFaction()
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
            float netGain = Battery.PowerNet?.CurrentEnergyGainRate() ?? 0f;

            // increment fully using excess power
            if (netGain > 0.01f)
                _darkEnergyReserve = Mathf.Min(_darkEnergyReserve + EnergyIncrement, _maxDarkEnergy);
            else
            {
                int trickle = (int)(EnergyIncrement 
                    * (SolarFlareActive 
                        ? SolarFlareFactor 
                        : TrickleFactor));
                _darkEnergyReserve = Mathf.Min(_darkEnergyReserve + trickle, _maxDarkEnergy);
            }
        }

        float current = Battery.StoredEnergy;

        if (current + 1f < EffectiveMaxEnergy
            && _darkEnergyReserve >= OverflowLimit)
        {
            float allowed = EffectiveMaxEnergy - current;
            float toAdd = Mathf.Min(OverflowLimit, allowed);

            if (toAdd > 0.01f)
            {
                Battery.AddEnergy(toAdd);
                _darkEnergyReserve -= Mathf.RoundToInt(toAdd);
            }
        }
    }

    /// <summary>
    /// Sets the ZPM's dark energy reserve, clamped to [0, MaxDarkEnergy].
    /// Used by ZPM housing when inserting/ejecting.
    /// </summary>
    public void SetDarkEnergyReserve(int amount)
    {
        if (_maxDarkEnergy <= 0)
        {
            _darkEnergyReserve = Math.Max(0, amount);
            return;
        }

        _darkEnergyReserve = Mathf.Clamp(amount, 0, _maxDarkEnergy);
    }

    private int ActiveDiverterCount()
    {
        if (Facilities == null) return 0;
        var list = Facilities.LinkedFacilitiesListForReading;
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
        if (Faction.IsOfPlayerFaction() && _isBroadcasting)
        {
            _isBroadcasting = false;
            Map.GetComponent<MapComponent_ZpmRaidTracker>()?.NotifyZpmEndedBroadcast();
        }
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Faction.IsOfPlayerFaction() && _isBroadcasting)
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

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
    {
        foreach (var opt in base.GetFloatMenuOptions(pawn))
            yield return opt;

        if (pawn.Drafted || pawn.WorkTagIsDisabled(WorkTags.Hauling))
            yield break;

        if (!pawn.CanReach(this, PathEndMode.ClosestTouch, Danger.Deadly))
            yield break;

        // Find a housing with capacity & reachability
        Building_ZPMHousing bestHousing = Building_ZPMHousing.FindBestHousingFor(pawn, this); 
        if (bestHousing == null)
            yield break;

        yield return new FloatMenuOption(
            "RG_InsertZpmIntoHousingFloatMenu".Translate(bestHousing.LabelCap),
            () =>
            {
                var job = JobMaker.MakeJob(
                    RimgateDefOf.Rimgate_InsertZpmIntoHousing,
                    this,
                    bestHousing);
                job.playerForced = true;
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job);
            });
    }

    public override string GetInspectString()
    {
        if (Faction.IsOfPlayerFaction() && !ResearchUtil.ZPMIntegrationComplete)
            return "Inert";

        StringBuilder sb = new();
        if (Battery?.PowerNet == null) return sb.ToString();

        sb.Append(Battery.CompInspectStringExtra());

        if (sb.Length > 0) sb.AppendLine();
        sb.Append("RG_ZpmDarkEnergyReserve".Translate(_darkEnergyReserve, _maxDarkEnergy));

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

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;
using RimWorld.QuestGen;
using System.Text;
using Verse.Noise;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Building_ZPM : Building
{
    public static Dictionary<string, Graphic> ChargeGraphics = new();

    public const int ZpmAdditionDistance = 3;

    public bool IsBroadcasting => _isBroadcasting;

    private CompPowerBattery _powerComp;

    private int _darkEnergyReserve = 7500;

    private int _maxDarkEnergy = -1;

    private bool _isBroadcasting;

    private bool _wasConnectedLastTick;

    static Building_ZPM()
    {
        if (Building_ZPM.ChargeGraphics.Any())
            return;

        string[] powerStates = { "Depleted", "25%", "50%", "75%", "Full" };
        foreach (var powerState in powerStates)
        {
            var graphic = new Graphic_Single();

            GraphicRequest request = new GraphicRequest(
                Type.GetType("Graphic_Single"),
                $"Things/Building/Power/ZPM/RGZPM_{powerState}",
                ShaderDatabase.DefaultShader,
                new Vector2(1, 2),
                Color.white,
                Color.white,
                new GraphicData(),
                0,
                null,
                null);

            graphic.Init(request);
            graphic.data.drawOffset = Rimgate_DefOf.Rimgate_ZPM.graphicData.drawOffset;
            ChargeGraphics.Add(powerState, graphic);
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _powerComp = base.GetComp<CompPowerBattery>();
        _maxDarkEnergy = (int)Math.Ceiling(_powerComp.Props.storedEnergyMax * 1.25);

        bool connected = _powerComp != null && _powerComp.PowerNet != null;
        if (Faction == Faction.OfPlayer
            && !_isBroadcasting
            && connected)
        {
            _isBroadcasting = true;
            map.GetComponent<MapComponent_ZpmRaidTracker>()?.NotifyZpmBeganBroadcast();
        }

        _wasConnectedLastTick = connected;
    }

    public override void TickRare()
    {
        bool connected = _powerComp != null && _powerComp.PowerNet != null;
        if (connected != _wasConnectedLastTick && Faction == Faction.OfPlayer)
        {
            var tracker = Map?.GetComponent<MapComponent_ZpmRaidTracker>();
            if (connected && !_isBroadcasting)
            {
                _isBroadcasting = true;
                tracker?.NotifyZpmBeganBroadcast();
            }
            else if (!connected && _isBroadcasting)
            {
                _isBroadcasting = false;
                tracker?.NotifyZpmEndedBroadcast();
            }
            _wasConnectedLastTick = connected;
        }

        if (!connected)
            return;

        // Charge using all the excess energy on the grid.
        if (_powerComp.PowerNet.CurrentEnergyGainRate() > 0.01f)
            _darkEnergyReserve += 100;

        if (_darkEnergyReserve > _maxDarkEnergy)
            _darkEnergyReserve = _maxDarkEnergy;

        if (_powerComp.StoredEnergyPct < 0.75f && _darkEnergyReserve >= 1000)
        {
            _powerComp.AddEnergy(1000f);
            _darkEnergyReserve -= 1000;
        }

        if (RimgateMod.DebugZPM)
        {
            Log.Warning($"ZPM :: Current Energy Gain Rate: {_powerComp.PowerNet.CurrentEnergyGainRate()}");
            Log.Warning($"ZPM :: Stored Energy: {_powerComp.StoredEnergy}");
        }

        base.TickRare();
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
            if (_powerComp == null)
                return base.DefaultGraphic;

            // var chargePercent = (int) ((float) currentCapacitorCharge / (float) maxCapacitorCharge) * 100;
            var chargePercent = (int)(_powerComp.StoredEnergyPct * 100);
            if (chargePercent <= 10)
                return Building_ZPM.ChargeGraphics["Depleted"];
            else if (chargePercent <= 25)
                return Building_ZPM.ChargeGraphics["25%"];
            else if (chargePercent <= 50)
                return Building_ZPM.ChargeGraphics["50%"];
            else if (chargePercent <= 75)
                return Building_ZPM.ChargeGraphics["75%"];
            else
                return Building_ZPM.ChargeGraphics["Full"];
        }
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(base.GetInspectString());
        if (_powerComp != null)
        {
            if (stringBuilder.Length > 0)
                stringBuilder.AppendLine();

            string text = $"Dark Energy Reserve: {_darkEnergyReserve}/{_maxDarkEnergy}";
            stringBuilder.Append(text);
        }

        return stringBuilder.ToString();
    }

    public static Thing FindZpmOnMap(Map map, Thing thingToIgnore = null)
    {
        Thing zpmOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing.def == Rimgate_DefOf.Rimgate_ZPM)
            {
                zpmOnMap = thing;
                break;
            }
        }

        return zpmOnMap;
    }
}

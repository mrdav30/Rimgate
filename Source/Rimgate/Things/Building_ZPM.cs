using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;

namespace Rimgate;

public class Building_ZPM : Building
{
    public const int ZpmAdditionDistance = 3;

    private static Dictionary<string, Graphic> _chargeGraphics = new();

    private CompPowerBattery _powerComp;

    private int _darkEnergyReserve = 7500;
    private int _maxDarkEnergy = -1;

    static Building_ZPM()
    {
        if (Building_ZPM._chargeGraphics.Any())
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
            _chargeGraphics.Add(powerState, graphic);
        }
    }

    #region Override

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _powerComp = base.GetComp<CompPowerBattery>();
        _maxDarkEnergy = (int)Math.Ceiling(_powerComp.Props.storedEnergyMax * 1.25);
    }

    public override void TickRare()
    {
        if (_powerComp == null || _powerComp.PowerNet == null) return;

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

    #endregion

    #region Graphics-text

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
                return Building_ZPM._chargeGraphics["Depleted"];
            else if (chargePercent <= 25)
                return Building_ZPM._chargeGraphics["25%"];
            else if (chargePercent <= 50)
                return Building_ZPM._chargeGraphics["50%"];
            else if (chargePercent <= 75)
                return Building_ZPM._chargeGraphics["75%"];
            else
                return Building_ZPM._chargeGraphics["Full"];
        }
    }

    public override string GetInspectString() => base.GetInspectString()
        + $"\nDark Energy Reserve: {_darkEnergyReserve}/{_maxDarkEnergy}";

    #endregion
}

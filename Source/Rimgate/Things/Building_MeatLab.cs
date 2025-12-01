using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF.Maps;
using Verse;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Building_MeatLab : Building_PlantGrower
{
    private static Dictionary<string, Graphic> _growGraphics = new();

    public CompPowerTrader PowerTrader
    {
        get
        {
            _cachedPowerTrader ??= GetComp<CompPowerTrader>();
            return _cachedPowerTrader;
        }
    }

    private CompPowerTrader _cachedPowerTrader;

    private Plant_MeatPlant _plant;

    static Building_MeatLab()
    {
        _growGraphics ??= new();
        _growGraphics.Clear();
        string[] growStates = { "empty", "25%", "50%", "75%", "100%" };
        GraphicData data = RimgateDefOf.Rimgate_WraithMeatLab.graphicData;
        foreach (var growState in growStates)
        {
            var graphic = new Graphic_Single();

            GraphicRequest request = new GraphicRequest(
                typeof(Graphic_Single),
                $"Things/Building/Production/MeatLab/RGMeatLab_{growState}",
                ShaderDatabase.DefaultShader,
                data.drawSize,
                Color.white,
                Color.white,
                data,
                0,
                null,
                null);

            graphic.Init(request);
            _growGraphics.Add(growState, graphic);
        }
    }

    public override void TickRare()
    {
        base.TickRare();

        // Refresh cache if it's missing, despawned, on another map, or destroyed
        if (_plant == null 
            || !_plant.Spawned 
            || _plant.Map != Map 
            || _plant.Destroyed)
        {
            _plant = null;
            foreach (Plant plant in PlantsOnMe)
            {
                if (plant is Plant_MeatPlant pmp && pmp.Spawned && pmp.Map == Map)
                {
                    _plant = pmp;
                    break;
                }
            }
        }
    }

    public override Graphic Graphic
    {
        get
        {
            // Show “empty” when no valid, spawned plant is present
            if (_plant == null || !_plant.Spawned || _plant.Map != Map)
                return _growGraphics["empty"];

            var growthPercent = Mathf.FloorToInt((_plant.Growth + 0.0001f) * 100f);
            if (growthPercent <= 10) return _growGraphics["empty"];
            if (growthPercent <= 25) return _growGraphics["25%"];
            if (growthPercent <= 50) return _growGraphics["50%"];
            if (growthPercent <= 75) return _growGraphics["75%"];
            return _growGraphics["100%"];
        }
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();

        // Only append plant inspect if it’s actually spawned on this map
        if (Spawned && _plant != null && _plant.Spawned && _plant.Map == Map)
        {
            if (!text.NullOrEmpty()) text += "\n";
            text += _plant.GetInspectString();
        }
        return text;
    }
}

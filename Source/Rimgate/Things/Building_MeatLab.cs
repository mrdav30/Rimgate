using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF.Maps;
using Verse;

namespace Rimgate;

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
        if (Building_MeatLab._growGraphics.Any())
            return;

        string[] growStates = { "empty", "25%", "50%", "75%", "100%" };
        foreach (var growState in growStates)
        {
            var graphic = new Graphic_Single();

            GraphicRequest request = new GraphicRequest(
                Type.GetType("Graphic_Single"),
                $"Things/Building/Production/MeatLab/RGMeatLab_{growState}",
                ShaderDatabase.DefaultShader,
                new Vector2(1, 1),
                Color.white,
                Color.white,
                new GraphicData(),
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

        if(_plant != null) return;
        foreach(Plant plant in PlantsOnMe)
        {
            if(plant is Plant_MeatPlant pmp)
            {
                _plant = pmp;
                break;
            }         
        }
    }

    public override Graphic Graphic
    {
        get
        {
            // For when it's minified or in a trade ship.
            if (_plant == null)
                return Building_MeatLab._growGraphics["empty"];

            var growthPercent = Mathf.FloorToInt((_plant.Growth + 0.0001f) * 100f);
            if (growthPercent <= 10)
                return Building_MeatLab._growGraphics["empty"];
            else if (growthPercent <= 25)
                return Building_MeatLab._growGraphics["25%"];
            else if (growthPercent <= 50)
                return Building_MeatLab._growGraphics["50%"];
            else if (growthPercent <= 75)
                return Building_MeatLab._growGraphics["75%"];
            else
                return Building_MeatLab._growGraphics["100%"];
        }
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();

        if (Spawned && _plant != null)
        {
            if (!text.NullOrEmpty())
                text += "\n";
            text += _plant.GetInspectString();
        }
        return text;
    }
}

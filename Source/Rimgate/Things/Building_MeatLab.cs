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
    private static Dictionary<string, Graphic> growGraphics = new Dictionary<string, Graphic>();

    CompPowerTrader power;

    Plant plant;

    static Building_MeatLab()
    {
        if (Building_MeatLab.growGraphics.Any())
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
            growGraphics.Add(growState, graphic);
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        power = base.GetComp<CompPowerTrader>();
    }

    public override void TickRare()
    {
        if (power == null 
            || power.PowerNet == null
            || !PlantsOnMe.Any()) return;

        plant = PlantsOnMe.First() ?? null;

        base.TickRare();
    }

    public override Graphic Graphic
    {
        get
        {
            // For when it's minified or in a trade ship.
            if (plant == null)
                return Building_MeatLab.growGraphics["empty"];

            var growthPercent = Mathf.FloorToInt((plant.Growth + 0.0001f) * 100f);
            if (growthPercent <= 10)
                return Building_MeatLab.growGraphics["empty"];
            else if (growthPercent <= 25)
                return Building_MeatLab.growGraphics["25%"];
            else if (growthPercent <= 50)
                return Building_MeatLab.growGraphics["50%"];
            else if (growthPercent <= 75)
                return Building_MeatLab.growGraphics["75%"];
            else
                return Building_MeatLab.growGraphics["100%"];
        }
    }

    public override string GetInspectString()
    {
        return plant != null
            ? $"{base.GetInspectString()}\n{plant.GetInspectString() ?? string.Empty}"
            : base.GetInspectString();
    }
}

using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
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

    public bool IsPlantInvalid => _plant == null || !_plant.Spawned || _plant.Destroyed || _plant.Map != Map;

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
        if (!Spawned || this.IsMinified())
            return;

        base.TickRare();

        // Refresh cache if it's missing, despawned, on another map, or destroyed
        if (!IsPlantInvalid) return;

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

    public override Graphic Graphic
    {
        get
        {
            // Show “empty” when no valid, spawned plant is present
            if (IsPlantInvalid)
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
        if (!Spawned || Destroyed || Map == null) return null;

        StringBuilder sb = new StringBuilder();

        string text = InspectStringPartsFromComps();
        if (!text.NullOrEmpty())
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(text);
        }

        if (!IsPlantInvalid)
        {
            string text2 = _plant.GetInspectString();
            if (!text2.NullOrEmpty())
                sb.AppendInNewLine(text2);
        }

        return sb.ToString().TrimEndNewlines();
    }
}

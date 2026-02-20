using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Rimgate;

public class GenStep_AncientComplex_GateSite : GenStep_AncientComplex
{
    public LayoutDef layoutDef;

    public string symbolStack;

    protected override LayoutDef LayoutDef => layoutDef ?? LayoutDefOf.AncientComplex;

    public string SymbolStack => symbolStack ?? "ancientComplex";

    private LayoutStructureSketch _structureSketch;

    private IntVec2 Size => new IntVec2(_structureSketch?.structureLayout.container.Width + 10 ?? 0, _structureSketch?.structureLayout.container.Height + 10 ?? 0);

    private static readonly IntVec2 DefaultComplexSize = new IntVec2(80, 80);

    private static readonly SimpleCurve ComplexSizeOverPointsCurve = new SimpleCurve
    {
        new CurvePoint(0f, 30f),
        new CurvePoint(10000f, 50f)
    };

    private static readonly SimpleCurve TerminalsOverRoomCountCurve = new SimpleCurve
    {
        new CurvePoint(0f, 1f),
        new CurvePoint(10f, 4f),
        new CurvePoint(20f, 6f),
        new CurvePoint(50f, 10f)
    };

    public override void Generate(Map map, GenStepParams parms)
    {
        _structureSketch = GenerateStructureSketch(parms.sitePart.parms.points);
        map.layoutStructureSketches.Add(_structureSketch);
        GenerateRuins(map, parms, DefaultMapFillPercentRange);
    }

    private LayoutStructureSketch GenerateStructureSketch(float points, bool generateTerminals = true)
    {
        int num = (int)ComplexSizeOverPointsCurve.Evaluate(points);
        StructureGenParams parms = new StructureGenParams
        {
            size = new IntVec2(num, num)
        };

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Generating lost complex structure sketch with size {parms.size} using {points}.");

        LayoutStructureSketch layoutStructureSketch = LayoutDef.Worker.GenerateStructureSketch(parms);

        if (layoutStructureSketch?.structureLayout == null)
        {
            Log.Warning("Rimgate :: Failed to find lost complex structure sketch. Generating default.");
            StructureGenParams parms2 = new StructureGenParams
            {
                size = DefaultComplexSize
            };
            layoutStructureSketch = LayoutDefOf.AncientComplex.Worker.GenerateStructureSketch(parms2);
        }

        if (generateTerminals)
        {
            int num2 = Mathf.FloorToInt(TerminalsOverRoomCountCurve.Evaluate(layoutStructureSketch.structureLayout.Rooms.Count));
            for (int i = 0; i < num2; i++)
                layoutStructureSketch.thingsToSpawn.Add(ThingMaker.MakeThing(ThingDefOf.AncientTerminal));
        }

        return layoutStructureSketch;
    }

    protected override LayoutStructureSketch GenerateAndSpawn(CellRect rect, Map map, GenStepParams parms, LayoutDef layoutDef)
    {
        CellRect container = _structureSketch.structureLayout.container;
        if (!rect.TryFindRandomInnerRect(new IntVec2(container.Width, container.Height), out var rect2))
        {
            rect2 = rect;
            Log.Error($"Attempted to generate and spawn an anicent complex, but could not find rect of valid size {Size} within provided Rect {rect}");
        }

        ResolveParams resolveParams = default(ResolveParams);
        resolveParams.ancientLayoutStructureSketch = _structureSketch;
        resolveParams.threatPoints = parms.sitePart.parms.threatPoints;
        resolveParams.rect = rect2;
        resolveParams.thingSetMakerDef = ThingSetMakerDefOf.MapGen_AncientComplexRoomLoot_Better;

        ResolveParams parms2 = resolveParams;
        FormCaravanComp component = parms.sitePart.site.GetComponent<FormCaravanComp>();
        if (component != null)
            component.foggedRoomsCheckRect = parms2.rect;

        MapGenerator.UsedRects.Add(parms2.rect);
        GenerateComplex(map, parms2);
        return _structureSketch;
    }

    protected override void GenerateComplex(Map map, ResolveParams parms)
    {
        RimWorld.BaseGen.BaseGen.globalSettings.map = map;
        RimWorld.BaseGen.BaseGen.symbolStack.Push(SymbolStack, parms);
        RimWorld.BaseGen.BaseGen.Generate();
    }
}

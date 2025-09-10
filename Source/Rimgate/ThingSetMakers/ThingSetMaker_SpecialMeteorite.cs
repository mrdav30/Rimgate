using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;
using static RimWorld.Reward_Pawn;
using static System.Collections.Specialized.BitVector32;
using Verse.AI;

namespace Rimgate;

public class ThingSetMaker_SpecialMeteorite : ThingSetMaker
{
    public const float PreciousMineableMarketValue = 5f;

    public static List<ThingDef> NonSmoothedMineables = new List<ThingDef>();

    public static readonly IntRange MineablesCountRange = new IntRange(8, 20);

    public static void Reset()
    {
        NonSmoothedMineables.Clear();
        NonSmoothedMineables.AddRange(
            from x in DefDatabase<ThingDef>.AllDefsListForReading
            where x.mineable && x != ThingDefOf.CollapsedRocks && !x.IsSmoothed
            select x);
    }

    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        IntRange? countRange = parms.countRange;
        int randomInRange = ((countRange == null)
            ? ThingSetMaker_SpecialMeteorite.MineablesCountRange
            : countRange.Value).RandomInRange;
        ThingDef def = RimgateDefOf.Rimgate_MineableNaquadah;
        for (int i = 0; i < randomInRange; i++)
        {
            Building building = (Building)ThingMaker.MakeThing(def, null);
            building.canChangeTerrainOnDestroyed = false;
            outThings.Add(building);
        }

        Map mapPlayerHome = null;
        List<Map> maps = Find.Maps;
        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].IsPlayerHome)
            {
                mapPlayerHome = maps[i];
                break;
            }
        }
    }

    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms) => NonSmoothedMineables;
}


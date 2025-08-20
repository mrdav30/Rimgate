using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public static class StargateUtility
{
    public const string Alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static Thing GetStargateOnMap(Map map, Thing thingToIgnore = null)
    {
        Thing gateOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing.def.thingClass == typeof(Building_Stargate))
            {
                gateOnMap = thing;
                break;
            }
        }

        return gateOnMap;
    }

    public static string GetStargateDesignation(PlanetTile address)
    {
        if (!address.Valid)
            return "UnknownLower".Translate();

        Rand.PushState(address);
        //pattern: P(num)(char)-(num)(num)(num)
        string designation =
            $"P{Rand.RangeInclusive(0, 9)}{Alpha[Rand.RangeInclusive(0, 25)]}"
            + $"-{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}";
        Rand.PopState();
        return designation;
    }

}

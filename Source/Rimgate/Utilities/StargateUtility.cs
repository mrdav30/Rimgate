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

    public static Building_Stargate GetStargateOnMap(
        Map map,
        Thing thingToIgnore = null)
    {
        Building_Stargate gateOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing is Building_Stargate bsg)
            {
                gateOnMap = bsg;
                break;
            }
        }

        return gateOnMap;
    }


    public static Building_DHD GetDhdOnMap(Map map)
    {
        Building_DHD dhdOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing is Building_DHD bdhd)
            {
                dhdOnMap = bdhd;
                break;
            }
        }

        return dhdOnMap;
    }

    public static bool ActiveGateOnMap(Map map)
    {
        Building_Stargate gate = StargateUtility.GetStargateOnMap(map);
        if (gate == null) return false;
        if (gate.StargateControl == null) return false;
        return gate.StargateControl.IsActive;
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

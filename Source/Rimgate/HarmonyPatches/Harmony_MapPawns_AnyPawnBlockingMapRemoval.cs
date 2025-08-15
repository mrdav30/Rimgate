using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate.HarmonyPatches;

// Keep map with Stargate open
[HarmonyPatch(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval), MethodType.Getter)]
public class Harmony_MapPawns_AnyPawnBlockingMapRemoval
{
    public static void Postfix(Map ___map, ref bool __result)
    {
        Thing sgThing = Comp_Stargate.GetStargateOnMap(___map);
        if (sgThing == null)
            return;

        Comp_Stargate sgComp = sgThing.TryGetComp<Comp_Stargate>();
        if (sgComp == null)
            return;

        if (sgComp.StargateIsActive)
            __result = true;
    }
}

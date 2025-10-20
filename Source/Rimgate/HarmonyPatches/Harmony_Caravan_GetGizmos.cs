using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;
using Verse;

namespace Rimgate.HarmonyPatches;

// Add create Stargate site to caravan gizmos
[HarmonyPatch(typeof(Caravan), "GetGizmos")]
public class Harmony_Caravan_GetGizmos
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Caravan __instance)
    {
        foreach (Gizmo gizmo in gizmos)
            yield return gizmo;

        bool containsStargate = false;
        foreach (Thing thing in __instance.AllThings)
        {
            Thing inner = thing.GetInnerIfMinified();
            if (inner != null && inner.TryGetComp<Comp_StargateControl>() != null)
            {
                containsStargate = true;
                break;
            }
        }

        Command_Action command = new Command_Action
        {
            icon = RimgateTex.PermanentSiteCommandTex,
            action = () =>
            {
                List<Thing> things = __instance.AllThings.ToList();
                for (int i = 0; i < things.Count(); i++)
                {
                    Thing inner = things[i].GetInnerIfMinified();
                    if (inner != null 
                        && inner.def.thingClass == typeof(Building_Stargate))
                    {
                        things[i].holdingOwner.Remove(things[i]);
                        break;
                    }
                }

                things = __instance.AllThings.ToList();
                for (int i = 0; i < things.Count(); i++)
                {
                    Thing inner = things[i].GetInnerIfMinified();
                    if (inner != null 
                        && inner.def.thingClass == typeof(Building_DHD))
                    {
                        things[i].holdingOwner.Remove(things[i]);
                        break;
                    }
                }

                WorldObject_PermanentStargateSite wo = WorldObjectMaker.MakeWorldObject(RimgateDefOf.Rimgate_PermanentStargateSite) as WorldObject_PermanentStargateSite;
                wo.Tile = __instance.Tile;
                Find.WorldObjects.Add(wo);
            },
            defaultLabel = "RG_CreateSGSite".Translate(),
            defaultDesc = "RG_CreateSGSiteDesc".Translate()
        };

        StringBuilder reason = new StringBuilder();
        if (!containsStargate)
            command.Disable("RG_NoGateInCaravan".Translate());
        else if (!TileFinder.IsValidTileForNewSettlement(__instance.Tile, reason))
            command.Disable(reason.ToString());

        yield return command;
    }
}

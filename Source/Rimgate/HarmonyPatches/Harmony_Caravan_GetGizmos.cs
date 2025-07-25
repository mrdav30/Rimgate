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
    static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Caravan __instance)
    {
        foreach (Gizmo gizmo in gizmos)
            yield return gizmo;

        bool containsStargate = false;
        foreach (Thing thing in __instance.AllThings)
        {
            Thing inner = thing.GetInnerIfMinified();
            if (inner != null)
            {
                if (inner.TryGetComp<Comp_Stargate>() != null)
                    containsStargate = true; break;
            }
        }

        Command_Action command = new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Icon/Map/RGStargatePermanentSiteIcon", true),
            action = () =>
            {
                ThingDef gateDef = null;
                ThingDef dhdDef = null;

                List<Thing> things = __instance.AllThings.ToList();
                for (int i = 0; i < things.Count(); i++)
                {
                    Thing inner = things[i].GetInnerIfMinified();
                    if (inner != null && inner.def.thingClass == typeof(Building_Stargate))
                    {
                        gateDef = inner.def; things[i].holdingOwner.Remove(things[i]);
                        break;
                    }
                }

                things = __instance.AllThings.ToList();
                for (int i = 0; i < things.Count(); i++)
                {
                    Thing inner = things[i].GetInnerIfMinified();
                    if (inner != null && inner.TryGetComp<Comp_DialHomeDevice>() != null
                        && inner.def.thingClass != typeof(Building_Stargate))
                    {
                        dhdDef = inner.def; things[i].holdingOwner.Remove(things[i]);
                        break;
                    }
                }

                WorldObject_PermanentStargateSite wo = (WorldObject_PermanentStargateSite)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("Rimgate_PermanentStargateSite"));
                wo.Tile = __instance.Tile;
                wo.gateDef = gateDef;
                wo.dhdDef = dhdDef;
                Find.WorldObjects.Add(wo);
            },
            defaultLabel = "Rimgate_CreateSGSite".Translate(),
            defaultDesc = "Rimgate_CreateSGSiteDesc".Translate()
        };

        StringBuilder reason = new StringBuilder();
        if (!containsStargate)
            command.Disable("Rimgate_NoGateInCaravan".Translate());
        else if (!TileFinder.IsValidTileForNewSettlement(__instance.Tile, reason))
            command.Disable(reason.ToString());

        yield return command;
    }
}

using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

// Patch to make asteroid ore generation for orbital GateQuestSite maps
[HarmonyPatch(typeof(GenStep_Asteroid), "SpawnOres")]
public static class Harmony_GenStep_Asteroid
{
    public static bool Prefix(GenStep_Asteroid __instance, Map map, GenStepParams parms)
    {
        if (map?.Parent != null && map.Parent is WorldObject_GateQuestSite wos)
        {
            if (RimgateMod.Debug)
                Log.Message("Rimgate :: Generating asteroid ores for GateQuestSite map.");

            var mineables = __instance.mineableCounts;
            ThingDef thingDef = mineables.RandomElement().mineable;
            int num = 0;
            for (int i = 0; i < mineables.Count; i++)
            {
                if (mineables[i].mineable == thingDef)
                {
                    num = mineables[i].countRange.RandomInRange;
                    break;
                }
            }

            if (num == 0)
            {
                Log.Error("Rimgate :: No count found for resource " + thingDef);
                return false;
            }

            int randomInRange = __instance.numChunks.RandomInRange;
            int forcedLumpSize = num / randomInRange;
            GenStep_ScatterLumpsMineable genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineable();
            genStep_ScatterLumpsMineable.count = randomInRange;
            genStep_ScatterLumpsMineable.forcedDefToScatter = thingDef;
            genStep_ScatterLumpsMineable.forcedLumpSize = forcedLumpSize;
            genStep_ScatterLumpsMineable.Generate(map, parms);

            return false;
        }

        return true;
    }
}

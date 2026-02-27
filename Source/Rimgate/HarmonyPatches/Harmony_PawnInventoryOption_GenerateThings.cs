using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace Rimgate.HarmonyPatches;

// This patch makes PawnInventoryOption.GenerateThings() use the default stuff for things that are made from stuff,
// instead of always making them without stuff.
[HarmonyPatch(typeof(PawnInventoryOption), nameof(PawnInventoryOption.GenerateThings), MethodType.Enumerator)]
public static class Harmony_PawnInventoryOption_GenerateThings
{
    private static Thing MakeThingWithDefaultStuffIfNeeded(ThingDef def, ThingDef stuff)
    {
        // Preserve vanilla behavior if a stuff was explicitly provided (rare here, but safe).
        if (def == null) return null;
        if (stuff != null) return ThingMaker.MakeThing(def, stuff);

        if (def.MadeFromStuff)
        {
            LogUtil.Debug($"GenerateThings: making {def} with default stuff.");
            var defaultStuff = GenStuff.DefaultStuffFor(def);
            return ThingMaker.MakeThing(def, defaultStuff);
        }

        return ThingMaker.MakeThing(def, null);
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        // RimWorld 1.6 signature: MakeThing(ThingDef def, ThingDef stuff)
        var makeThing = AccessTools.Method(
            typeof(ThingMaker),
            nameof(ThingMaker.MakeThing),
            new[] { typeof(ThingDef), typeof(ThingDef) });

        var helper = AccessTools.Method(
            typeof(Harmony_PawnInventoryOption_GenerateThings),
            nameof(MakeThingWithDefaultStuffIfNeeded));

        if (makeThing == null || helper == null)
        {
            LogUtil.Error($"GenerateThings transpiler: method lookup failed. makeThing={(makeThing != null)}, helper={(helper != null)}");
            return codes;
        }

        int replaced = 0;

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(makeThing))
            {
                // Stack at callsite is (ThingDef def, ThingDef stuff). We swap the callee only.
                codes[i] = new CodeInstruction(OpCodes.Call, helper);
                replaced++;
            }
        }

        if (replaced == 0)
            LogUtil.Warning("GenerateThings transpiler: found no ThingMaker.MakeThing(def, stuff) call sites to replace.");

        return codes;
    }
}

using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static UnityEngine.Networking.UnityWebRequest;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(EquipmentUtility), "CanEquip")]
public static class Harmony_EquipmentUtility
{
    public static void PostFix(Thing thing, Pawn pawn, ref string cantReason, ref bool checkBonded)
    {
        if (!checkBonded)
            return;

        if (thing is not Apparel apparel)
            return;

        Comp_FrameApparel comp = ThingCompUtility.TryGetComp<Comp_FrameApparel>(apparel);
        if (comp.Props == null || comp.Props.requiredFrameDefNames == null || pawn.apparel == null)
            return;

        var hasEquipped = GenCollection.Any<Apparel>(pawn.apparel.WornApparel, x =>
            comp.Props.requiredFrameDefNames.Contains(x.def.defName));
        if(!hasEquipped)
        {
            checkBonded = false;
            cantReason = comp.Props.failReason;
        }
    }
}

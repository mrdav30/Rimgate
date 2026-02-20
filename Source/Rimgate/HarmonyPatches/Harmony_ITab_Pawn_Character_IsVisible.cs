using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Rimgate.HarmonyPatches;

/*
 * This is necessary to hide "Bio" inspector tab.
 * It shows for any pawn with story, and we need story to have work working.
 */
[HarmonyPatch(typeof(ITab_Pawn_Character), "get_IsVisible", new Type[0])]
public static class Harmony_ITab_Pawn_Character_IsVisible
{
    public static void Postfix(ref bool __result)
    {
        Thing SelThing = Find.Selector.SingleSelectedThing;

        if (__result
            && SelThing != null
            && SelThing is Pawn_Unas unas
            && !unas.IsFormerHuman())
        {
            __result = false;
        }
    }
}

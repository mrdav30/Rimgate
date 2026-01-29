using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate.HarmonyPatches;

// Makes it so that drafted Malps do not show the command radius of their overseer's mechanitor tracker (it's unlimited)
[HarmonyPatch(
    typeof(Pawn_MechanitorTracker),
    nameof(Pawn_MechanitorTracker.DrawCommandRadius),
    new Type[] { })]
static class PatchMechanitorTracker_DrawCommandRadius
{
    static bool Prefix(Pawn_MechanitorTracker __instance)
    {
        List<Pawn> selectedPawns = Find.Selector.SelectedPawns;
        for (int i = 0; i < selectedPawns.Count; i++)
        {
            if (selectedPawns[i].GetOverseer() == __instance.Pawn
                && selectedPawns[i].Drafted
                && selectedPawns[i].def == RimgateDefOf.Rimgate_Malp)
            {
                return false;
            }
        }

        return true;
    }
}

using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

// Exclude Sarcophagus beds as possible prisoner beds when capturing new prisoners
[HarmonyPatch(typeof(RestUtility), nameof(RestUtility.TuckIntoBed))]
public static class Harmony_RestUtility_TuckIntoBed
{
    public static bool Prefix(
        Building_Bed bed,
        Pawn taker,
        Pawn takee,
        bool rescued)
    {
        if (bed is Building_Bed_Sarcophagus bedSarcophagus)
        {
            SarcophagusRestUtility.PutIntoSarcophagus(bedSarcophagus, taker, takee, true);

            return false;
        }

        return true;
    }
}

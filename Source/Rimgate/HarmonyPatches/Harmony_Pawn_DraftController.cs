using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate.HarmonyPatches;

// Disallow Drafting Sarcophagus Patients
[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
public static class Harmony_Pawn_DraftController
{
    public static void Postfix(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
    {
        if (__instance == null
            || __instance.pawn.IsColonistPlayerControlled
            || __instance.pawn.CurrentBed() is not Building_Bed_Sarcophagus)
            return;

        __result = PatchGetGizmos(__instance, __result);
    }

    static IEnumerable<Gizmo> PatchGetGizmos(Pawn_DraftController __instance, IEnumerable<Gizmo> result)
    {
        foreach (var gizmo in result)
        {
            if (gizmo is Command_Toggle toggleCommand
                && toggleCommand.defaultDesc == "CommandToggleDraftDesc".Translate())
            {
                toggleCommand.Disable("RG_Sarcophagus_CommandGizmoDisabled_PatientReceivingTreatment".Translate(__instance.pawn.LabelShort, __instance.pawn.CurrentBed()));

                yield return toggleCommand;
                continue;
            }

            yield return gizmo;
        }
    }
}

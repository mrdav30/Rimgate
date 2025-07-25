using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using static UnityEngine.Networking.UnityWebRequest;

namespace Rimgate.HarmonyPatches;

// Disallow Drafting Sarcophagus Patients
[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public static class Harmony_Pawn_GetGizmos
{
    static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
    {
        if (__instance == null 
            || __instance.IsColonistPlayerControlled
            || __instance.CurrentBed() is not Building_Bed_Sarcophagus)
            return;

        __result = PatchGetGizmos(__instance, __result);
    }

    static IEnumerable<Gizmo> PatchGetGizmos(Pawn pawn, IEnumerable<Gizmo> result)
    {
        foreach (var gizmo in result)
        {
            if (gizmo is Command_Toggle toggleCommand)
            {
                if (toggleCommand.defaultDesc == "CommandToggleDraftDesc".Translate())
                {
                    toggleCommand.Disable("RG_Sarcophagus_CommandGizmoDisabled_PatientReceivingTreatment".Translate(pawn.LabelShort, pawn.CurrentBed()));

                    yield return toggleCommand;
                    continue;
                }
            }

            yield return gizmo;
        }

        Pawn_EquipmentTracker equipment = pawn.equipment;

        Comp_SwitchWeapon comp = equipment != null
            ? ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(equipment.Primary)
            : null;
        if (comp == null)
            yield break;

        foreach (var gizmo in comp.SwitchWeaponOptions())
            yield return gizmo;
    }
}

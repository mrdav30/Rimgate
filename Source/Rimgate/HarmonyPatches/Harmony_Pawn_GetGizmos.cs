using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public static class Harmony_Pawn_GetGizmos
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
    {
        foreach (Gizmo gizmo in gizmos)
            yield return gizmo;

        if (!__instance.IsColonistPlayerControlled)
            yield break;

        Pawn_EquipmentTracker equipment = __instance.equipment;
        Comp_SwitchWeapon comp = equipment != null
            ? ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(equipment.Primary)
            : null;
        if (comp == null)
            yield break;

        foreach (Gizmo gizmo in comp.SwitchWeaponOptions())
            yield return gizmo;
    }
}


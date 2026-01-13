using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

// Doctors should not perform scheduled surgeries on patients using Sarcophagi
[HarmonyPatch(typeof(Pawn), nameof(Pawn.CurrentlyUsableForBills))]
public static class Harmony_Pawn
{
    public static void Postfix(ref bool __result, Pawn __instance)
    {
        if (__instance.InBed()
            && __instance.ParentHolder is Building_Sarcophagus bedSarcophagus)
        {
            JobFailReason.Is("RG_Sarcophagus_SurgeryProhibited_PatientUsingSarcophagus".Translate());
            __result = false;
        }
    }
}

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

// Allow AI to switch weapons
[HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
public static class Harmony_Pawn_TryGetAttackVerb
{
    public static void Prefix(
        Thing target,
        bool allowManualCastWeapons = false,
        bool allowTurrets = false)
    {
        if (target is not Pawn pawn || pawn.Faction.IsOfPlayerFaction())
            return;

        Pawn_EquipmentTracker equipment = pawn.equipment;
        Comp_SwitchWeapon comp = equipment != null
            ? ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(equipment.Primary)
            : null;
        if (comp == null)
            return;

        var alternate = comp.GetOrCreateAlternate();
        if (alternate == null) return;

        if (!pawn.equipment.PrimaryEq.PrimaryVerb.CanHitTarget(target)
            || Rand.Chance(0.1f))
        {
            Verb primaryVerb = ThingCompUtility.TryGetComp<CompEquippable>(alternate).PrimaryVerb;
            primaryVerb.caster = pawn;
            if (!primaryVerb.CanHitTargetFrom(pawn.Position, target))
                return;

            comp.ToggleWeapon();
        }
    }
}


[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes))]
public static class Harmony_Pawn_GetDisabledWorkTypes
{
    public static bool Prefix(ref List<WorkTypeDef> __result, Pawn __instance)
    {
        if (__instance is Pawn_Unas unas)
        {
            __result = unas.GetDisabledWorkTypes();
            return false;
        }
        return true;
    }
}
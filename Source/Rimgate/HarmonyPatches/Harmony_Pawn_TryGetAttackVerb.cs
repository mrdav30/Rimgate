using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

// Allow AI to switch weapons
[HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
public static class Harmony_Pawn_TryGetAttackVerb
{
    public static void Prefix(
        Thing target,
        bool allowManualCastWeapons = false,
        bool allowTurrets = false)
    {
        if (target is not Pawn pawn || pawn.Faction == Faction.OfPlayer)
            return;

        Pawn_EquipmentTracker equipment = pawn.equipment;
        Comp_SwitchWeapon comp = equipment != null
            ? ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(equipment.Primary)
            : null;
        if (comp == null)
            return;

        if (comp.Props.weaponToSwitch != pawn.equipment?.Primary.def && comp.CachedSwitchWeapon == null)
            comp.GetOrCreateAlternate();

        if (comp.CachedSwitchWeapon == null) return;

        if (!pawn.equipment.PrimaryEq.PrimaryVerb.CanHitTarget(target)
            || Rand.Chance(0.1f))
        {
            Verb primaryVerb = ThingCompUtility.TryGetComp<CompEquippable>(comp.CachedSwitchWeapon).PrimaryVerb;
            primaryVerb.caster = pawn;
            if (!primaryVerb.CanHitTargetFrom(pawn.Position, target))
                return;

            pawn.equipment.Remove(pawn.equipment.Primary);
            pawn.equipment.AddEquipment(comp.CachedSwitchWeapon);
        }
    }
}

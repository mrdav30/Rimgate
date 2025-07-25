using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Pawn), "TryGetAttackVerb")]
public static class Harmony_Pawn_TryGetAttackVerb
{
    public static void Prefix(Thing target, bool allowManualCastWeapons = false, bool allowTurrets = false)
    {
        if (target is not Pawn pawn || pawn.Faction == Faction.OfPlayer)
            return;

        Pawn_EquipmentTracker equipment = pawn.equipment;
        Comp_SwitchWeapon comp = equipment != null
            ? ThingCompUtility.TryGetComp<Comp_SwitchWeapon>(equipment.Primary)
            : null;
        if (comp == null)
            return;

        if (comp.generatedWeapons == null)
            comp.generatedWeapons = new Dictionary<ThingDef, Thing>();

        foreach (ThingDef key in comp.Props.weaponsToSwitch)
        {
            if (key != pawn.equipment?.Primary.def && !comp.generatedWeapons.ContainsKey(key))
            {
                Thing weaponThing = ThingMaker.MakeThing(key, null);
                comp.generatedWeapons[key] = weaponThing;
            }
        }

        if (!pawn.equipment.PrimaryEq.PrimaryVerb.CanHitTarget(target))
        {
            var verbs = comp.generatedWeapons
                .OrderBy<KeyValuePair<ThingDef, Thing>, float>(x =>
                    ThingCompUtility.TryGetComp<CompEquippable>(x.Value).PrimaryVerb.verbProps.range);
            foreach (KeyValuePair<ThingDef, Thing> keyValuePair in verbs)
            {
                Verb primaryVerb = ThingCompUtility.TryGetComp<CompEquippable>(keyValuePair.Value).PrimaryVerb;
                primaryVerb.caster = pawn;
                if (primaryVerb.CanHitTargetFrom(pawn.Position, target))
                {
                    comp.generatedWeapons[(pawn.equipment.Primary).def] = pawn.equipment.Primary;
                    pawn.equipment.Remove(pawn.equipment.Primary);
                    pawn.equipment.AddEquipment(keyValuePair.Value as ThingWithComps);
                    break;
                }
            }
        }
        else if (Rand.Chance(0.1f))
        {
            var verbs = GenCollection.InRandomOrder<KeyValuePair<ThingDef, Thing>>(comp.generatedWeapons, null);
            foreach (KeyValuePair<ThingDef, Thing> keyValuePair in verbs)
            {
                Verb primaryVerb = ThingCompUtility.TryGetComp<CompEquippable>(keyValuePair.Value).PrimaryVerb;
                primaryVerb.caster = (Thing)pawn;
                if (primaryVerb.CanHitTargetFrom(((Thing)pawn).Position, target))
                {
                    comp.generatedWeapons[((Thing)pawn.equipment.Primary).def] = (Thing)pawn.equipment.Primary;
                    pawn.equipment.Remove(pawn.equipment.Primary);
                    pawn.equipment.AddEquipment(keyValuePair.Value as ThingWithComps);
                    break;
                }
            }
        }
    }
}

using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_EquipMechanoid : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool MechanoidCanDo => true;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        var pawn = context.FirstSelectedPawn;
        return pawn.IsColonyMech && pawn.equipment != null && !pawn.Dead;
    }

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        var pawn = context.FirstSelectedPawn;
        if (!pawn.IsColonyMech || !clickedThing.HasComp<CompEquippable>()) return null;

        string labelShort = clickedThing.LabelShort;
        if (clickedThing.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn), null);
        }

        if (clickedThing.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfShootingLower".Translate(pawn), null);
        }

        if (!pawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
        }

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "Incapable".Translate().CapitalizeFirst(), null);
        }

        if (clickedThing.IsBurning())
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "BurningLower".Translate(), null);
        }

        if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(clickedThing, pawn))
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null);
        }

        if (!EquipmentUtility.CanEquip(clickedThing, pawn, out var cantReason, checkBonded: false))
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + cantReason.CapitalizeFirst(), null);
        }

        bool tagFlag = (pawn.kindDef.weaponTags == null || clickedThing.def.weaponTags == null)
            ? false
            : !(pawn.kindDef.weaponTags.Intersect(clickedThing.def.weaponTags).Any());
        if (tagFlag)
        {
            return new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "RG_IncompatibleWeaponType".Translate(), null);
        }

        string text = "Equip".Translate(labelShort);
        if (EquipmentUtility.AlreadyBondedToWeapon(clickedThing, pawn))
        {
            text += " " + "BladelinkAlreadyBonded".Translate();
            TaggedString dialogText = "BladelinkAlreadyBondedDialog".Translate(pawn.Named("PAWN"), clickedThing.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"));
            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
            {
                Find.WindowStack.Add(new Dialog_MessageBox(dialogText));
            }, MenuOptionPriority.High), pawn, clickedThing);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
        {
            string personaWeaponConfirmationText = EquipmentUtility.GetPersonaWeaponConfirmationText(clickedThing, pawn);
            if (!personaWeaponConfirmationText.NullOrEmpty())
            {
                Find.WindowStack.Add(new Dialog_MessageBox(personaWeaponConfirmationText, "Yes".Translate(), delegate
                {
                    Equip();
                }, "No".Translate()));
            }
            else
            {
                Equip();
            }
        }, MenuOptionPriority.High), pawn, clickedThing);

        void Equip()
        {
            clickedThing.SetForbidden(value: false);
            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, clickedThing), JobTag.Misc);
            FleckMaker.Static(clickedThing.DrawPos, clickedThing.MapHeld, FleckDefOf.FeedbackEquip);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
        }
    }
}

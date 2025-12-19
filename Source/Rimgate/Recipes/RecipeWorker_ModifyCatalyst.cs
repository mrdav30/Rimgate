using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class RecipeWorker_ModifyCatalyst : Recipe_Surgery
{
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
    {
        return MedicalRecipesUtility.GetFixedPartsToApplyOn(recipe, pawn);
    }

    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if (thing is not Pawn_Unas pawn)
            return false;

        // Must have Mk1
        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_CorticalCatalyst))
            return false;

        // Must NOT already have Mk2
        if (pawn.HasHediffOf(RimgateDefOf.Rimgate_CorticalCatalyst_Mk2))
            return false;

        return true;
    }

    public override void ApplyOnPawn(Pawn pawn,
        BodyPartRecord part,
        Pawn billDoer,
        List<Thing> ingredients,
        Bill bill)
    {
        if (billDoer != null)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                return;

            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        }

        pawn.health.AddHediff(recipe.addsHediff, part);
    }
}

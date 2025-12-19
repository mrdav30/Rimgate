using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public class RecipeWorker_ProcureImplantFromCorpse : RecipeWorker
{
    /// <remarks>
    /// Does not destroy the corpse
    /// </remarks>
    public override void ConsumeIngredient(
        Thing ingredient,
        RecipeDef recipeDef,
        Map map)
    {
        if (ingredient is not Corpse corpse
            || corpse == null
            || recipeDef.removesHediff == null) return;

        var hediff = corpse.InnerPawn?.GetHediffOf(recipeDef.removesHediff);
        if (hediff == null) return;


        var spawnDef = hediff.def.spawnThingOnRemoved;
        if (spawnDef == null)
        {
            Messages.Message("RG_BiomaterialRecoveryMessage_Nothing".Translate(corpse.LabelShort),
                new TargetInfo(corpse.Position, corpse.Map),
                MessageTypeDefOf.NeutralEvent);
        }
        else
        {
            if (!Rand.Chance(recipeDef.surgerySuccessChanceFactor))
            {
                Messages.Message("RG_BiomaterialRecoveryMessage_Failed".Translate(corpse.LabelShort),
                    new TargetInfo(corpse.Position, corpse.Map),
                    MessageTypeDefOf.NegativeEvent);
                return;
            }


            var thing = ThingMaker.MakeThing(spawnDef);
            Messages.Message("RG_BiomaterialRecoveryMessage_Procured".Translate(thing.LabelShort, corpse.LabelShort),
                new TargetInfo(corpse.Position, corpse.Map),
                MessageTypeDefOf.PositiveEvent);
            GenPlace.TryPlaceThing(thing, corpse.Position, corpse.Map, ThingPlaceMode.Near);
        }

        corpse.InnerPawn.RemoveHediff(hediff);
    }
}
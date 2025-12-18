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
        var spawnDef = hediff?.def.spawnThingOnRemoved;
        if (hediff == null || spawnDef == null) return;

        var successChance = recipeDef.surgerySuccessChanceFactor;
        if (successChance < 1 && new Random().NextDouble() > successChance)
        {
            Messages.Message("RG_BiomaterialRecoveryMessage_Failed".Translate(corpse.Label),
                new TargetInfo(corpse.Position, corpse.Map),
                MessageTypeDefOf.NegativeEvent);
            return;
        }

        var thing = ThingMaker.MakeThing(spawnDef);
        GenPlace.TryPlaceThing(thing, corpse.Position, corpse.Map, ThingPlaceMode.Near);
        corpse.InnerPawn.RemoveHediff(hediff);
    }
}
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class CorpseBiomaterialRecoveryUtility
{
    public static bool IsCorpseValidForRecovery(Corpse corpse, Pawn actor)
    {
        if (corpse == null || corpse.Destroyed || corpse.IsForbidden(actor))
            return false;
        if (corpse.InnerPawn == null) return false;

        return true;
    }

    public static bool CanStartRecoveryJob(
        Pawn actor,
        Corpse corpse,
        BiomaterialRecoveryDef def,
        out string reason,
        bool checkAvailability = true)
    {
        reason = null;

        if (!IsCorpseValidForRecovery(corpse, actor))
        {
            reason = "invalid corpse";
            return false;
        }

        if (!def.allowRotten && corpse.GetRotStage() == RotStage.Rotting)
        {
            reason = "corpse is rotten";
            return false;
        }

        if (def.researchPrerequisite != null && !def.researchPrerequisite.IsFinished)
        {
            reason = $"{def.researchPrerequisite.LabelCap} research required";
            return false;
        }

        if (!def.PawnSatisfiesSkillRequirements(actor, out reason))
            return false;


        if (checkAvailability)
        {
            if (CorpseBiomaterialRecoveryUtility.CountAvailableKits(actor, def.requiredKit) < 1)
            {
                reason = $"missing {def.requiredKit.LabelCap} x1";
                return false;
            }
        }

        return true;
    }

    public static bool TryRecoverFromCorpse(
        Pawn actor,
        Corpse corpse,
        BiomaterialRecoveryDef def,
        bool consumeKitOnAttempt,
        out Thing spawnedThing,
        out string failReason)
    {
        spawnedThing = null;
        failReason = null;

        // Find the hediff instance
        Pawn inner = corpse.InnerPawn;
        Hediff found = inner?.GetHediffOf(def.removesHediff);
        if (found == null)
        {
            failReason = "biomaterial missing";
            return false;
        }

        if (consumeKitOnAttempt)
            ConsumeKits(actor, def.requiredKit, 1);

        float skillChance = def.successChanceStat != null
            ? actor.GetStatValue(def.successChanceStat)
            : 1;

        // success roll
        if (!Rand.Chance(def.successChance * skillChance))
        {
            failReason = "procedure failed";
            return false;
        }

        ThingDef spawnDef = def.RecoverableThing;
        if (spawnDef != null)
        {
            spawnedThing = ThingMaker.MakeThing(spawnDef);
            GenPlace.TryPlaceThing(spawnedThing, actor.Position, actor.Map, ThingPlaceMode.Near);
        }

        inner.health.RemoveHediff(found);

        return true;
    }

    public static int CountAvailableKits(Pawn actor, ThingDef kitDef)
    {
        if (actor == null || kitDef == null) return 0;

        int count = 0;

        // Map stock only
        Map map = actor.Map;
        if (map != null)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(kitDef);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t.IsForbidden(actor)) continue;
                if (!actor.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Some)) continue;
                count += t.stackCount;
            }
        }

        return count;
    }

    public static bool TryGetClosestKit(Pawn pawn, ThingDef def, out Thing kit)
    {
        kit = null;
        if (pawn?.Map == null || def == null)
            return false;

        kit = GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForDef(def),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            999f,
            t => !t.IsForbidden(pawn)
                && pawn.CanReserve(t));

        return kit != null;
    }

    private static void ConsumeKits(Pawn actor, ThingDef kitDef, int count)
    {
        bool requiresKit = kitDef != null && count > 0;
        if (!requiresKit)
            return;

        int remaining = count;

        // Consume from carried first
        Thing carried = actor.carryTracker?.CarriedThing;
        if (carried != null && carried.def == kitDef)
        {
            int take = remaining < carried.stackCount ? remaining : carried.stackCount;
            carried.SplitOff(take).Destroy();
            remaining -= take;
            if (remaining <= 0) return;
        }

        // Then map stock (fallback)
        if (actor.Map != null)
        {
            // should be within a radius of the pawn
            var things = actor.Map.listerThings.ThingsOfDef(kitDef);
            for (int i = 0; i < things.Count && remaining > 0; i++)
            {
                Thing t = things[i];
                if (t.IsForbidden(actor)) continue;
                if (!actor.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Some)) continue;
                int take = remaining < t.stackCount ? remaining : t.stackCount;
                t.SplitOff(take).Destroy();
                remaining -= take;
            }
        }
    }
}

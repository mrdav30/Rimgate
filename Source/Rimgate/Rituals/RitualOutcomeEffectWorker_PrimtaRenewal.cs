using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class RitualOutcomeEffectWorker_PrimtaRenewal : RitualOutcomeEffectWorker_FromQuality
{
    public RitualOutcomeEffectWorker_PrimtaRenewal() { }

    public RitualOutcomeEffectWorker_PrimtaRenewal(RitualOutcomeEffectDef def) : base(def) { }

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        // Default look target is the ritual target; we may override to host.
        LookTargets letterLookTargets = jobRitual.selectedTarget;
        string extraOutcomeDesc;
        bool success = TryPerformPrimtaRenewal(jobRitual, out extraOutcomeDesc, ref letterLookTargets);

        Pawn host = jobRitual.assignments.FirstAssignedPawn("organizer");
        string label = success
            ? "RG_PrimtaRenewal_Label".Translate()
            : "RG_PrimtaRenewal_GenericFailure".Translate();

        // ---------- FAILURE PATH ----------
        if (!success)
        {
            Find.LetterStack.ReceiveLetter(
                label,
                extraOutcomeDesc,
                LetterDefOf.RitualOutcomeNegative,
                letterLookTargets);

            return;
        }

        // ---------- SUCCESS PATH ----------
        float quality = GetQuality(jobRitual, progress);
        RitualOutcomePossibility outcome = GetOutcome(quality, jobRitual);

        // Base text: success/failure summary keyed on host
        string text = outcome.description
            .Formatted(jobRitual.Ritual.Label, host.Named("PAWN"))
            .CapitalizeFirst();

        string moodDesc = def.OutcomeMoodBreakdown(outcome);
        if (!moodDesc.NullOrEmpty())
            text = text + "\n\n" + moodDesc;

        // Append our short desc / failure reason
        if (!extraOutcomeDesc.NullOrEmpty())
            text += "\n\n" + extraOutcomeDesc;

        // Append quality breakdown just like FromQuality would
        text += "\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual);

        // Single, clear ritual letter. No attachables, no extra outcome memory handling.
        Find.LetterStack.ReceiveLetter(
            label,
            text,
            success ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative,
            letterLookTargets);

        if (outcome.memory == null) return;

        foreach (KeyValuePair<Pawn, int> item in totalPresence)
        {
            if (!outcome.roleIdsNotGainingMemory.NullOrEmpty())
            {
                RitualRole ritualRole = jobRitual.assignments.RoleForPawn(item.Key);
                if (ritualRole != null && outcome.roleIdsNotGainingMemory.Contains(ritualRole.id))
                    continue;
            }

            GiveMemoryToPawn(item.Key, outcome.memory, jobRitual);
        }
    }

    /// <summary>
    /// Prim'ta swap logic.
    /// </summary>
    private bool TryPerformPrimtaRenewal(
        LordJob_Ritual jobRitual,
        out string extraOutcomeDesc,
        ref LookTargets letterLookTargets)
    {
        extraOutcomeDesc = null;

        var target = jobRitual.selectedTarget;
        var map = target.Map;
        if (map == null)
        {
            extraOutcomeDesc = "RG_PrimtaRenewal_NoMap".Translate();
            return false;
        }

        var host = jobRitual.assignments.FirstAssignedPawn("organizer");
        if (host == null || host.health == null)
        {
            extraOutcomeDesc = "RG_PrimtaRenewal_NoHost".Translate();
            return false;
        }

        letterLookTargets = host; // focus on the Jaffa

        // Must have pouch to be a valid host
        if (!host.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch))
        {
            extraOutcomeDesc = "RG_PrimtaRenewal_NoPouch".Translate(host.Named("PAWN"));
            return false;
        }

        // 1) Try to take a Prim'ta from the *ritual pool* (selected target)
        Thing primtaItem = TryTakePrimtaFromPool(target.Thing);

        // 2) Fallback: look on the ground around the ritual center
        if (primtaItem == null)
            primtaItem = FindPrimtaItemOnGroundNear(target.Cell, map);

        if (primtaItem == null)
        {
            extraOutcomeDesc = "RG_PrimtaRenewal_NoLarva".Translate();
            return false;
        }

        // ---- SUCCESS PATH ----
        var primta = host.GetHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch) as Hediff_PrimtaInPouch;
        bool hadPrimta = primta != null;
        bool primtaMature = primta != null && primta.Lifecyle != null && primta.Lifecyle.Mature;

        // If there was an existing prim'ta, remove it safely and spawn the appropriate symbiote.
        if (hadPrimta)
        {
            primta.MarkInternalRemoval();
            host.health.RemoveHediff(primta);

            if (host.Map != null)
            {
                var defToSpawn = primtaMature
                    ? RimgateDefOf.Rimgate_GoauldSymbiote
                    : RimgateDefOf.Rimgate_PrimtaSymbiote;

                var thing = ThingMaker.MakeThing(defToSpawn);
                GenPlace.TryPlaceThing(thing, host.Position, host.Map, ThingPlaceMode.Near);
            }
        }

        // Consume one larva item
        primtaItem.SplitOff(1).Destroy(DestroyMode.Vanish);

        // Add fresh prim'ta
        var newPrimta = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_PrimtaInPouch, host);
        host.health.AddHediff(newPrimta);

        // Safety: clear any residual Krin'tak
        host.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);

        extraOutcomeDesc = "RG_PrimtaRenewal_SuccessText".Translate(host.Named("PAWN"));
        return true;
    }

    private Thing TryTakePrimtaFromPool(Thing targetThing)
    {
        var pool = targetThing as Building_SymbioteSpawningPool;
        if (pool == null) return null;

        var inner = pool.InnerContainer.InnerListForReading;
        for (int i = 0; i < inner.Count; i++)
        {
            var t = inner[i];
            if (t.def == RimgateDefOf.Rimgate_PrimtaSymbiote)
            {
                // We don't need to drop it into the world; just consume one inside the pool.
                // SplitOff(1) adjusts the stack and gives us a single-thing which we destroy later.
                return t;
            }
        }

        return null;
    }

    private Thing FindPrimtaItemOnGroundNear(IntVec3 center, Map map)
    {
        foreach (var cell in GenRadial.RadialCellsAround(center, 6f, true))
        {
            if (!cell.InBounds(map))
                continue;

            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t.def == RimgateDefOf.Rimgate_PrimtaSymbiote)
                    return t;
            }
        }

        return null;
    }
}

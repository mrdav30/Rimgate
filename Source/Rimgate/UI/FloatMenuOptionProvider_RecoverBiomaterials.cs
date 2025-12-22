using RimWorld;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Verse;
using Verse.AI;
using static RimWorld.PsychicRitualRoleDef;

namespace Rimgate;

public class FloatMenuOptionProvider_RecoverBiomaterials : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not Corpse corpse)
            yield break;

        Pawn actor = context.FirstSelectedPawn;
        if (actor == null)
            yield break;

        // Quick corpse validity checks
        if (!CorpseBiomaterialRecoveryUtility.IsCorpseValidForRecovery(corpse, actor))
            yield break;

        var corpsePawn = corpse.InnerPawn;
        var defs = DefDatabase<BiomaterialRecoveryDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            BiomaterialRecoveryDef def = defs[i];
            if (def.removesHediff == null)
                continue;

            if (!corpse.InnerPawn.HasHediffOf(def.removesHediff))
                continue;

            RaceProperties race = corpsePawn?.RaceProps;
            if (race?.IsAnomalyEntity == true) continue;
            if (def.animalsOnly && !(race?.Animal == true)) continue;

            bool available = CorpseBiomaterialRecoveryUtility.CanStartRecoveryJob(
                actor,
                corpse,
                def,
                out string disableReason);

            string label = def.label.CapitalizeFirst();

            if (!available)
            {
                yield return new FloatMenuOption(label + $" ({disableReason})", null)
                {
                    Disabled = true
                };
                continue;
            }

            Action action = delegate
            {
                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_RecoverBiomaterialFromCorpse, corpse);
                job.dutyTag = def.defName;
                actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };

            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, action),
                actor,
                corpse);
        }
    }
}

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_RecoverBiomaterials : FloatMenuOptionProvider
{
    private static List<BiomaterialRecoveryDef> _cachedDefs;

    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return thing is Corpse;
    }

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

        Pawn corpsePawn = corpse.InnerPawn;
        _cachedDefs ??= DefDatabase<BiomaterialRecoveryDef>.AllDefsListForReading;
        for (int i = 0; i < _cachedDefs.Count; i++)
        {
            BiomaterialRecoveryDef def = _cachedDefs[i];
            if (def.removesHediffs.NullOrEmpty())
                continue;

            if (def.GetFirstMatchingHediff(corpse.InnerPawn) == null)
                continue;

            if (!def.TargetSatisfiesFilters(corpsePawn, out _))
                continue;

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

            void action()
            {
                Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_RecoverBiomaterialFromCorpse, corpse);
                job.dutyTag = def.defName;
                actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }

            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, action),
                actor,
                corpse);
        }
    }
}

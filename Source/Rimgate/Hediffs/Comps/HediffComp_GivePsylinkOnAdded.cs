using RimWorld;
using System.Linq;
using Verse;
using static RimWorld.Building_HoldingPlatform;

namespace Rimgate;

public class HediffComp_GivePsylinkOnAdded : HediffComp
{
    private bool _done;

    public HediffCompProperties_GivePsylinkOnAdded Props => (HediffCompProperties_GivePsylinkOnAdded)props;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        TryRunOnce();
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (!_done && parent.pawn.Spawned) TryRunOnce();
    }

    private void TryRunOnce()
    {
        if (_done) return;
        var pawn = parent.pawn;
        if (pawn == null || pawn.Dead) return;

        // Optional gene gate (useful for Wraith-only)
        if (!string.IsNullOrEmpty(Props.requiredGene))
        {
            if (!pawn.HasActiveGeneOf(Props.requiredGene))
            {
                MarkDoneAndRemove();
                return;
            }
        }

        // Ensure psylink hediff exists
        var psylink = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) as Hediff_Psylink;
        if (psylink == null)
        {
            var brain = pawn.health.hediffSet.GetBrain();
            if (brain == null)
            {
                MarkDoneAndRemove();
                return;
            }
            psylink = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn, brain) as Hediff_Psylink;
            pawn.health.AddHediff(psylink);
        }

        // Level it
        int level = Rand.RangeInclusive(Props.minLevel, Props.maxLevel);
        (psylink as Hediff_Level).ChangeLevel(level);

        // Add a few psycasts (respect level + optional tags)
        GiveRandomPsycasts(pawn, level);

        pawn.psychicEntropy?.SetInitialPsyfocusLevel();

        // Seed psyfocus (optional)
        if (Props.initialPsyfocus >= 0f)
            pawn.psychicEntropy?.TryAddEntropy(Props.initialPsyfocus);

        MarkDoneAndRemove();
    }

    private void GiveRandomPsycasts(Pawn pawn, int level)
    {
        var pool = DefDatabase<AbilityDef>.AllDefs.Where(ad =>
        {
            // not a psycast
            if (!ad.IsPsycast) return false;
            // too high level
            if (ad.level > level) return false;
            // already has
            if (pawn.abilities?.GetAbility(ad) != null) return false;

            // Optional defName filter
            if (Props.abilityTags != null && Props.abilityTags.Count > 0)
            {
                bool tagHit = Props.abilityTags.Any(t => ad.defName.IndexOf(t, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (!tagHit) 
                    return false;
            }
            return true;
        }).ToList();

        int toGive = Props.extraPsycasts.RandomInRange;
        for (int i = 0; i < toGive && pool.Count > 0; i++)
        {
            var pick = pool.RandomElement();
            pawn.abilities?.GainAbility(pick);
            pool.Remove(pick);
        }
    }

    private void MarkDoneAndRemove()
    {
        _done = true;
        // Remove the initializer hediff so nothing ticks forever
        parent.pawn.health?.RemoveHediff(parent);
    }
}

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public class HediffComp_GivePsylinkOnAdded : HediffComp
{
    public HediffCompProperties_GivePsylinkOnAdded Props => (HediffCompProperties_GivePsylinkOnAdded)props;

    private bool _done;

    private static readonly List<AbilityDef> _allCandidates = DefDatabase<AbilityDef>.AllDefs.Where(ad => ad.IsPsycast).ToList();

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
        if (pawn == null || pawn.Dead || pawn.abilities == null)
        {
            MarkDoneAndRemove();
            return;
        }

        // Optional gene gate (useful for Wraith-only)
        if (!string.IsNullOrEmpty(Props.requiredGene) && !pawn.HasActiveGeneOf(Props.requiredGene))
        {
            MarkDoneAndRemove();
            return;
        }

        List<AbilityDef> configuredCandidates = _allCandidates.Where(ad =>
        {
            if (Props.whiteListAbilityDefs != null && Props.whiteListAbilityDefs.Count > 0)
            {
                bool tagHit = Props.whiteListAbilityDefs.Any(t => ad.defName.IndexOf(t, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (tagHit)
                    return true;
            }

            // Optional defName filter
            if (Props.blackListAbilityCategories != null && Props.blackListAbilityCategories.Count > 0)
            {
                bool categoryHit = Props.blackListAbilityCategories.Any(t => ad.category != null && ad.category.defName.IndexOf(t, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (categoryHit)
                    return false;
            }

            if (Props.blackListAbilityDefs != null && Props.blackListAbilityDefs.Count > 0)
            {
                bool tagHit = Props.blackListAbilityDefs.Any(t => ad.defName.IndexOf(t, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (tagHit)
                    return false;
            }

            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Psycast pool candidate: {ad.defName}");

            return true;
        }).ToList();

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

            // Give random psycasts at level 1 first to avoid Hediff_Psylink auto-assigning psycasts we don't want
            GiveRandomPsycasts(pawn, 1, configuredCandidates, 1);

            psylink = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn, brain) as Hediff_Psylink;
            psylink.level = 1;
            psylink.suppressPostAddLetter = true;
            pawn.health.AddHediff(psylink);
        }

        // Level it, use Hediff_Level to manage levels to prevent Hediff_Psylink from giving unwanted psycasts
        int level = Rand.RangeInclusive(Props.minLevel, Props.maxLevel);
        for(int i = 0; i < level; i++)
        {
            GiveRandomPsycasts(pawn, i, configuredCandidates, Props.extraPsycasts.RandomInRange);
            psylink.level += 1;
        }

        pawn.psychicEntropy?.SetInitialPsyfocusLevel();

        // Seed psyfocus (optional)
        if (Props.initialPsyfocus >= 0f)
            pawn.psychicEntropy?.TryAddEntropy(Props.initialPsyfocus);

        MarkDoneAndRemove();
    }

    private static void GiveRandomPsycasts(Pawn pawn, int level, List<AbilityDef> candidates, int toGive = 1)
    {
        var filteredByLevel = candidates.Where(ad => ad.level <= level && pawn.abilities.GetAbility(ad) == null).ToList();

        for (int i = 0; i < toGive && filteredByLevel.Count > 0; i++)
        {
            var pick = filteredByLevel.RandomElement();
            pawn.abilities.GainAbility(pick);
            filteredByLevel.Remove(pick);
        }
    }

    private void MarkDoneAndRemove()
    {
        _done = true;

        // Remove the initializer hediff so nothing ticks forever
        parent.pawn?.health?.RemoveHediff(parent);
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref _done, "done", false);
    }
}

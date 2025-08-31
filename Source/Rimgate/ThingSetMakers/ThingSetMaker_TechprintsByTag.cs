using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class ThingSetMaker_TechprintsByTag : ThingSetMaker
{
    // XML-configurable, e.g. <techprintTags><li>Rimgate_TechprintPool</li></techprintTags>
    public List<string> techprintTags; 
    public float marketValueFactor = 1f;
    public bool requirePrereqsCompleted = true; // only offer projects you could actually start

    private static readonly List<ThingDef> tmp = new();

    // weighting: more unmet projects -> higher selection weight
    public override float ExtraSelectionWeightFactor(ThingSetMakerParams parms)
    {
        var (unfinished, needsMoreTechprints) = CountEligibleProjects();
        if (!needsMoreTechprints) return 1f;
        // mirror the simple curve (roughly): fewer *finished* projects -> more weight
        // unfinished = projects you can still research (rough proxy for vanilla curve)
        if (unfinished >= 4) return 1f;
        if (unfinished <= 0) return 5f;
        return 1f + (4 - unfinished); // 4->1x, 3->2x, 2->3x, 1->4x, 0->5x
    }

    protected override bool CanGenerateSub(ThingSetMakerParams parms)
    {
        return TryPickTechprint(parms, null, out _);
    }

    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        tmp.Clear();

        if (parms.countRange.HasValue)
        {
            int count = Mathf.Max(parms.countRange.Value.RandomInRange, 1);
            for (int i = 0; i < count; i++)
            {
                if (!TryPickTechprint(parms, tmp, out var picked)) break;
                tmp.Add(picked);
                outThings.Add(ThingMaker.MakeThing(picked));
            }
        }
        else if (parms.totalMarketValueRange.HasValue)
        {
            float cap = parms.totalMarketValueRange.Value.RandomInRange * marketValueFactor;
            float acc = 0f;
            while (TryPickTechprint(parms, tmp, out var picked))
            {
                if (tmp.Any() && acc + picked.BaseMarketValue > cap) break;
                tmp.Add(picked);
                outThings.Add(ThingMaker.MakeThing(picked));
                acc += picked.BaseMarketValue;
            }
        }
        else if (TryPickTechprint(parms, tmp, out var single))
        {
            tmp.Add(single);
            outThings.Add(ThingMaker.MakeThing(single));
        }

        tmp.Clear();
    }

    protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
    {
        foreach (var t in CandidateTechprints(parms)) yield return t;
    }

    // -------- helpers --------

    private (int unfinishedCount, bool anyNeedsTechprints) CountEligibleProjects()
    {
        int unfinished = 0; bool needs = false;
        foreach (var r in EligibleResearchProjects())
        {
            if (!r.IsFinished) unfinished++;
            if (!r.TechprintRequirementMet) needs = true;
        }
        return (unfinished, needs);
    }

    private IEnumerable<ResearchProjectDef> EligibleResearchProjects()
    {
        var all = DefDatabase<ResearchProjectDef>.AllDefsListForReading;

        IEnumerable<ResearchProjectDef> filtered = (techprintTags != null && techprintTags.Count > 0)
            ? all.Where(r => r.tags != null && r.tags.Any(t => techprintTags.Contains(t.defName)))
            : all.Where(r => r.defName.StartsWith("Rimgate_", System.StringComparison.OrdinalIgnoreCase)); // fallback

        foreach (var r in filtered)
        {
            if (r.techprintCount <= 0) continue;
            if (requirePrereqsCompleted && !r.PrerequisitesCompleted) continue;
            yield return r;
        }
    }

    private IEnumerable<ThingDef> CandidateTechprints(ThingSetMakerParams parms)
    {
        // Build the current dynamic pool (unfinished + needs techprints)
        var pool = new List<ThingDef>();
        foreach (var r in EligibleResearchProjects())
        {
            if (r.IsFinished) continue;

            // Prefer projects where player still needs techprints
            bool stillNeeds = !r.TechprintRequirementMet &&
                              !PlayerItemAccessibilityUtility.PlayerOrQuestRewardHas(
                                  r.Techprint, r.TechprintCount - r.TechprintsApplied);

            if (!stillNeeds) continue;

            var tp = r.Techprint;
            if (tp == null) continue;

            // Respect market value cap if present
            // (vanilla passes this via totalMarketValueRange; we handle it at pick time)
            pool.Add(tp);
        }

        // If pool is empty, allow *any* unfinished techprint from the tagged set as a softer fallback
        if (pool.Count == 0)
        {
            foreach (var r in EligibleResearchProjects())
            {
                if (r.IsFinished) continue;
                var tp = r.Techprint;
                if (tp != null) pool.Add(tp);
            }
        }

        return pool;
    }

    private bool TryPickTechprint(ThingSetMakerParams parms, List<ThingDef> disallow, out ThingDef picked)
    {
        picked = null;
        var pool = CandidateTechprints(parms).Where(t => disallow == null || !disallow.Contains(t)).ToList();
        if (pool.Count == 0) return false;

        if (parms.totalMarketValueRange.HasValue)
        {
            float cap = parms.totalMarketValueRange.Value.max * marketValueFactor;
            pool = pool.Where(t => t.BaseMarketValue <= cap).ToList();
            if (pool.Count == 0) return false;
        }

        picked = pool.RandomElement();
        return true;
    }
}

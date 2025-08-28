using System.Collections.Generic;
using System.Linq;
using System.Net;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace Rimgate;

public class QuestNode_RaidStargateComplex : QuestNode
{
    public SlateRef<Site> address;

    public SlateRef<IEnumerable<string>> excludeTags;

    private static readonly SimpleCurve ThreatPointsOverPointsCurve = new SimpleCurve
    {
        new CurvePoint(35f, 38.5f),
        new CurvePoint(400f, 165f),
        new CurvePoint(10000f, 4125f)
    };

    private static FloatRange _randomPointsFactorRange = new FloatRange(0.9f, 1.1f);

    protected override bool TestRunInt(Slate slate)
    {
        return TryFindEnemyFaction(out _);
    }

    protected override void RunInt()
    {
        if (!TryFindEnemyFaction(out var enemyFaction))
            return;

        Slate slate = QuestGen.slate;
        List<string> exclusions = excludeTags.GetValue(slate).ToList();
        IEnumerable<SitePartDef> sitePartDefs = slate.Get<IEnumerable<SitePartDef>>("sitePartDefs");
        if (exclusions != null && sitePartDefs != null)
        {
            if (!sitePartDefs.Where(p => p != null && CanRaid(p, exclusions)).Any())
                return;
        }

        Quest quest = QuestGen.quest;
        Site site = address.GetValue(slate);

        QuestGen.GenerateNewSignal("RaidArrives");

        float num = slate.Get("points", 1f);
        float num2 = Find.Storyteller.difficulty.allowViolentQuests
            ? ThreatPointsOverPointsCurve.Evaluate(num)
            : 1f;

        TimedDetectionRaids component = site.GetComponent<TimedDetectionRaids>();
        if (component != null)
        {
            component.alertRaidsArrivingIn = true;
            component.delayRangeHours = new FloatRange(1f, 3f);
        }

        if (Find.Storyteller.difficulty.allowViolentQuests && Rand.Chance(0.5f))
        {
            var customLetterLabel = "Raid".Translate() + ": " + enemyFaction.Name;
            var customLetterText = "RG_LetterRaidStargateComplexDesc".Translate(enemyFaction.NameColored).Resolve();

            quest.RandomRaid(
                site,
                _randomPointsFactorRange * num2,
                enemyFaction,
                null,
                PawnsArrivalModeDefOf.EdgeWalkIn,
                RaidStrategyDefOf.ImmediateAttack,
                customLetterLabel,
                customLetterText);
        }
    }

    public bool CanRaid(SitePartDef part, List<string> exclusions)
    {
        for (int i = 0; i < exclusions.Count; i++)
        {
            if (part.tags.Contains(exclusions[i]))
                return false;
        }

        return true;
    }

    private bool TryFindEnemyFaction(out Faction enemyFaction)
    {
        enemyFaction = Find.FactionManager.RandomRaidableEnemyFaction();
        return enemyFaction != null;
    }
}

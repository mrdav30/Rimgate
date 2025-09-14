using System.Collections.Generic;
using System.Linq;
using System.Net;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Noise;

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
        if(!Utils.TryFindEnemyFaction(out _))
            return false;

        List<string> exclusions = excludeTags.GetValue(slate).ToList();
        IEnumerable<SitePartDef> sitePartDefs = slate.Get<IEnumerable<SitePartDef>>("sitePartDefs");
        if (exclusions != null && exclusions.Any() && sitePartDefs != null)
        {
            if (!sitePartDefs.Where(p => p != null && CanRaid(p, exclusions)).Any())
                return false;
        }

        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        List<string> exclusions = excludeTags.GetValue(slate).ToList();
        IEnumerable<SitePartDef> sitePartDefs = slate.Get<IEnumerable<SitePartDef>>("sitePartDefs");
        if (exclusions != null && exclusions.Any() && sitePartDefs != null)
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
            component.delayRangeHours = new FloatRange(0.10f, 0.5f);
        }

        if (Find.Storyteller.difficulty.allowViolentQuests && Rand.Chance(0.5f))
        {
            QuestPart_RandomFactionRaid randomRaid = new QuestPart_RandomFactionRaid();
            randomRaid.mapParent = site;
            randomRaid.pointsRange = _randomPointsFactorRange * num2;
            randomRaid.arrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            randomRaid.raidStrategy = RimgateDefOf.ImmediateAttackSmart;
            randomRaid.UseLetterKey = "RG_LetterRaidStargateComplexDesc";
            randomRaid.generateFightersOnly = true;
            quest.AddPart(randomRaid);
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
}

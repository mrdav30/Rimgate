using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace Rimgate;

public class QuestNode_GenerateStargateSite : QuestNode
{
    public SlateRef<IEnumerable<SitePartDefWithParams>> sitePartsParams;

    public SlateRef<Faction> faction;

    public SlateRef<PlanetTile> tile;

    [NoTranslate]
    public SlateRef<string> storeAs;

    public SlateRef<RulePack> singleSitePartRules;

    public SlateRef<bool> hiddenSitePartsPossible;

    private const string RootSymbol = "root";

    protected override bool TestRunInt(Slate slate)
    {
        if (!Find.Storyteller.difficulty.allowViolentQuests 
            && sitePartsParams.GetValue(slate) != null)
        {
            foreach (SitePartDefWithParams item in sitePartsParams.GetValue(slate))
            {
                if (item.def.wantsThreatPoints)
                    return false;
            }
        }

        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Site site = QuestGen_Sites.GenerateSite(
            sitePartsParams.GetValue(slate),
            tile.GetValue(slate),
            faction.GetValue(slate),
            hiddenSitePartsPossible.GetValue(slate),
            singleSitePartRules.GetValue(slate),
            RimgateDefOf.Rimgate_StargateSite);

        if(site is not WorldObject_StargateSite wos)
        {
            Log.Error($"Rimgate :: Quest did not generate site successfully");
            QuestGen.quest.End(QuestEndOutcome.Fail);
            return;
        }

        wos.QuestId = QuestGen.quest.id;

        if (storeAs.GetValue(slate) != null)
            QuestGen.slate.Set(storeAs.GetValue(slate), wos);
    }
}
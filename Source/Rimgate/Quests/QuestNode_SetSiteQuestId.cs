using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using Verse;

namespace Rimgate;

public class QuestNode_SetSiteQuestId : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        // Need a site target (map may or may not exist yet)
        var site = slate.Get<Site>("site");
        return site != null && site is WorldObject_GateQuestSite;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        var site = slate.Get<Site>("site");
        if (site == null || site is not WorldObject_GateQuestSite wos)
        {
            if(RimgateMod.Debug)
                Log.Error("Rimgate :: could not find valid gate quest site.");
            return;
        }

        wos.SetQuest(QuestGen.quest);
    }
}
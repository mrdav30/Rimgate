using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Collections.Generic;
using Verse.Noise;

namespace Rimgate;

public class WorldObject_StargateSite : Site
{
    public int QuestId = -1;

    private bool _mapHidden;

    private Quest _questCached;

    public override void SpawnSetup()
    {
        base.SpawnSetup();
        Find.World.GetComponent<WorldComp_StargateAddresses>().AddAddress(Tile);
    }

    protected override void Tick()
    {
        base.Tick();

        if (Map == null) return;

        bool shouldHide = !_mapHidden
            && !StargateUtility.ActiveGateOnMap(Map)
            && !Map.mapPawns.AnyPawnBlockingMapRemoval;
        if (shouldHide)
        {
            Find.ColonistBar.MarkColonistsDirty();
            _mapHidden = true;
        }
    }

    public void HideSiteMap()
    {
        if (Map == null || !_mapHidden) return;
        Find.ColonistBar.MarkColonistsDirty();
        _mapHidden = false;
    }

    public override void Notify_MyMapAboutToBeRemoved()
    {
        var gate = StargateUtility.GetStargateOnMap(Map);
        if (gate != null)
            gate.StargateControl.CleanupGate();

        base.Notify_MyMapAboutToBeRemoved();
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        // only removed when quest ends and no pawns
        var quest = ResolveQuest();
        if (quest != null && quest.State == QuestState.Ongoing) return false;
        if (!Map.mapPawns.AnyPawnBlockingMapRemoval)
        {
            alsoRemoveWorldObject = true;
            return true;
        }

        return false;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos()) yield return g;

        yield return new Command_Action
        {
            defaultLabel = "RG_AbandonQuestSiteLabel".Translate(),
            defaultDesc = "RG_AbandonQuestSiteDesc".Translate(),
            icon = RimgateTex.AbandonStargateSite,
            action = () =>
            {
                var quest = ResolveQuest();
                if (quest != null && quest.State == QuestState.Ongoing)
                    quest.End(QuestEndOutcome.Fail, false, false);
            }
        };
    }

    private Quest ResolveQuest()
    {
        if (_questCached != null) return _questCached;
        if (QuestId == -1) return null;

        _questCached = Find.QuestManager.QuestsListForReading
            .FirstOrDefault(q => q.id == QuestId && q.State == QuestState.Ongoing);
        if (_questCached == null) QuestId = -1; // stale id
        return _questCached;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref QuestId, "_questId");
        Scribe_Values.Look(ref _mapHidden, "_mapHidden", false);
    }
}

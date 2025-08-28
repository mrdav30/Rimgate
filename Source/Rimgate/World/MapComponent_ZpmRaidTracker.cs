using RimWorld;
using Verse;
using System.Linq;
using RimWorld.QuestGen;

namespace Rimgate;

public class MapComponent_ZpmRaidTracker : MapComponent
{
    private int _questId = -1;      
    
    private int _activeZpmCount; 
    
    private Quest _questCached;

    public MapComponent_ZpmRaidTracker(Map map) : base(map) { }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        RebuildFromMapState();
    }

    private void RebuildFromMapState()
    {
        int countOnMap = map.listerThings
            .ThingsOfDef(Rimgate_DefOf.Rimgate_ZPM)
            .Count(t => t.Faction == Faction.OfPlayer && t.Spawned &&
                        (t is Building_ZPM b ? b.IsBroadcasting : true));

        _activeZpmCount = countOnMap;
        if (_activeZpmCount > 0) StartQuestIfNeeded();
        else EndQuestIfNoZpm();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _questId, "_questId");
        Scribe_Values.Look(ref _activeZpmCount, "_activeZpmCount");
    }

    private Quest ResolveQuest()
    {
        if (_questCached != null) return _questCached;
        if (_questId == -1) return null;

        _questCached = Find.QuestManager.QuestsListForReading
            .FirstOrDefault(q => q.id == _questId && q.State == QuestState.Ongoing);
        if (_questCached == null) _questId = -1; // stale id
        return _questCached;
    }

    private void StartQuestIfNeeded()
    {
        if (_activeZpmCount <= 0) return;
        if (ResolveQuest() != null) return;

        // Create & publish the quest for THIS map only.
        var slate = new Slate();
        slate.Set("map", map);
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));

        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(Rimgate_DefOf.Rimgate_ProtectZPM, slate);
        QuestUtility.SendLetterQuestAvailable(quest);
        _questCached = quest;
        _questId = quest.id;
    }

    private void EndQuestIfNoZpm()
    {
        if (_activeZpmCount > 0) return;

        var quest = ResolveQuest();
        if (quest != null && quest.State == QuestState.Ongoing)
            quest.End(QuestEndOutcome.Fail, false, false);

        _questCached = null;
        _questId = -1;
    }

    public void NotifyZpmBeganBroadcast()
    {
        _activeZpmCount++;
        if (_activeZpmCount == 1)
            StartQuestIfNeeded();
    }

    public void NotifyZpmEndedBroadcast()
    {
        if (_activeZpmCount > 0) _activeZpmCount--;
        if (_activeZpmCount == 0)
            EndQuestIfNoZpm();
    }
}


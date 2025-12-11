using RimWorld;
using Verse;
using System.Linq;
using RimWorld.QuestGen;

namespace Rimgate;

public class MapComponent_ZpmRaidTracker : MapComponent
{
    public bool SuppressionActive => _suppressionActive;

    private int _activeZpmCount;

    private bool _suppressionActive;

    private Quest _questCached;

    private int _questId = -1;

    public MapComponent_ZpmRaidTracker(Map map) : base(map) { }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        RebuildFromMapState();
        if (_suppressionActive)
            EndQuestIfNoZpm(); // ensure no lingering quest after load
    }

    public void SetSuppressionActive(bool active)
    {
        if (_suppressionActive == active) return;
        _suppressionActive = active;

        if(RimgateMod.Debug)
            Log.Message($"Setting ZPM raid suppression to {_suppressionActive}.");

        // Kill the quest if it’s running; ZPMs are “masked”.
        if (_suppressionActive)
            EndQuestIfNoZpm(); // no raids while masked
        else // If any broadcasting ZPMs exist, (re)start the quest loop.
            RebuildFromMapState();
    }

    private void RebuildFromMapState()
    {
        if (map == null) return;

        int physicalCount = map.listerThings
            .ThingsOfDef(RimgateDefOf.Rimgate_ZPM)
            .Count(t => t.Faction.IsOfPlayerFaction()
                     && t.Spawned
                     && (t is Building_ZPM b ? b.IsBroadcasting : true));

        // If shroud is active, treat as zero “active” ZPMs for the quest
        _activeZpmCount = _suppressionActive ? 0 : physicalCount;

        if (_activeZpmCount > 0)
            StartQuestIfNeeded();
        else
            EndQuestIfNoZpm();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _questId, "_questId");
        Scribe_Values.Look(ref _activeZpmCount, "_activeZpmCount");
        Scribe_Values.Look(ref _suppressionActive, "_suppressionActive");
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
        if (_suppressionActive) return;
        if (_activeZpmCount <= 0) return;
        if (ResolveQuest() != null) return;

        // Create & publish the quest for THIS map only.
        var slate = new Slate();
        slate.Set("map", map);
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));

        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(RimgateDefOf.Rimgate_ProtectZPM, slate);
        QuestUtility.SendLetterQuestAvailable(quest);
        _questCached = quest;
        _questId = quest.id;
    }

    private void EndQuestIfNoZpm()
    {
        if (_activeZpmCount > 0 && !_suppressionActive) return;

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


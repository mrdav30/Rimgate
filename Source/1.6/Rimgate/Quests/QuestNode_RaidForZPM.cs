using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class QuestNode_RaidForZPM : QuestNode
{
    private static IntRange _raidDelayTicksRange = new IntRange(18000, 30000);

    private static IntRange _raidIntervalTicksRange = new IntRange(90000, 120000);

    private const float MinRaidThreatPointsFactor = 0.9f;

    private const float MaxRaidThreatPointsFactor = 1.1f;

    protected override bool TestRunInt(Slate slate)
    {
        if (!Find.Storyteller.difficulty.allowViolentQuests || !Utils.TryFindEnemyFaction(out _, allowNeolithic: false))
            return false;

        Map map = QuestGen_Get.GetMap();
        return map != null && Building_ZPM.FindZpmOnMap(map) != null;
    }

    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;

        string raidDelaySignal = QuestGen.GenerateNewSignal("RaidsDelay");
        string triggerRaidSignal = QuestGen.GenerateNewSignal("TriggerRaid");
        string questEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("zpm.QuestEndFailure");

        bool allowViolentQuests = Find.Storyteller.difficulty.allowViolentQuests;
        slate.Set("allowViolence", allowViolentQuests);

        Map map = slate.Get<Map>("map");
        Thing zpm = Building_ZPM.FindZpmOnMap(map);

        // If we can't run violent raids or can’t find an enemy faction,
        // add a terminal to allow ending.
        if (!allowViolentQuests || zpm == null || !Utils.TryFindEnemyFaction(out _, allowNeolithic: false))
        {
            quest.End(QuestEndOutcome.Fail, 0, null, questEndSignal, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: false);
            return;
        }

        QuestPart_Delay questPart_Delay = new QuestPart_Delay();
        questPart_Delay.delayTicks = _raidDelayTicksRange.RandomInRange;
        questPart_Delay.alertLabel = "QuestPartRaidsDelay".Translate();
        questPart_Delay.alertExplanation = "RG_RaidZpmDelayDesc".Translate();
        questPart_Delay.ticksLeftAlertCritical = 60000;
        questPart_Delay.inSignalEnable = QuestGen.slate.Get<string>("inSignal");
        questPart_Delay.alertCulprits.Add(zpm);
        questPart_Delay.isBad = true;
        questPart_Delay.outSignalsCompleted.Add(raidDelaySignal);
        questPart_Delay.waitUntilPlayerHasHomeMap = true;
        quest.AddPart(questPart_Delay);

        quest.Signal(raidDelaySignal, () =>
        {
            QuestPart_PassOutInterval part = new QuestPart_PassOutInterval
            {
                inSignalEnable = QuestGen.slate.Get<string>("inSignal"),
                outSignals = { triggerRaidSignal },
                inSignalsDisable = { questEndSignal },
                ticksInterval = _raidIntervalTicksRange
            };
            quest.AddPart(part);
        });

        float points = slate.Get("points", 1f);

        QuestPart_RandomFactionRaid randomRaid = new QuestPart_RandomFactionRaid();
        randomRaid.inSignal = triggerRaidSignal;
        randomRaid.mapParent = map.Parent;
        randomRaid.pointsRange = new FloatRange(points * MinRaidThreatPointsFactor, points * MaxRaidThreatPointsFactor);
        randomRaid.arrivalMode = map.Tile.LayerDef.isSpace
            ? PawnsArrivalModeDefOf.CenterDrop
            : PawnsArrivalModeDefOf.EdgeWalkIn;
        randomRaid.raidStrategy = RimgateDefOf.ImmediateAttackSmart;
        randomRaid.AllowNeolithic = false;
        randomRaid.UseGateIfAvailable = true;
        randomRaid.TargetDefs = new List<ThingDef> { RimgateDefOf.Rimgate_ZPM };
        randomRaid.TargetPredicate = (t) => t is Building_ZPM zpm && zpm.IsBroadcasting;
        randomRaid.UseLetterKey = "RG_LetterRaidZpmProtectDesc";
        randomRaid.generateFightersOnly = true;
        randomRaid.fallbackToPlayerHomeMap = true;
        quest.AddPart(randomRaid);

        quest.End(QuestEndOutcome.Fail, 0, null, questEndSignal, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("map.MapRemoved"));
        slate.Set("map", map);
        slate.Set("raidIntervalAvg", _raidIntervalTicksRange.Average);
    }
}
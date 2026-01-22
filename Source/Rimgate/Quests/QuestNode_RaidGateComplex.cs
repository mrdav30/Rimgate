using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

/// Drives “timed detection” raids for a gate site, 
/// without using the vanilla TimedDetectionRaids WO comp.
/// Triggers on site.MapGenerated and then repeats on an interval until quest end.
public class QuestNode_RaidGateComplex : QuestNode
{
    // ~0.5–1 day after map gen
    private static readonly IntRange InitialDelay = new IntRange(18000, 36000);

    // ~1–1.5 days between raids
    private static readonly IntRange Interval = new IntRange(60000, 90000);

    private static readonly FloatRange RaidPointsFactor = new FloatRange(0.85f, 1.10f);

    protected override bool TestRunInt(Slate slate)
    {
        if (!Find.Storyteller.difficulty.allowViolentQuests)
            return false;

        // Need a site target (map may or may not exist yet)
        var site = slate.Get<Site>("site");
        if(site == null) 
            return false;

        bool isSpace = site.Tile.LayerDef.isSpace;

        // Must have enemy factions available
        if (!Utils.TryFindEnemyFaction(out _, allowNeolithic: !isSpace))
            return false;

        return true;
    }

    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;

        // shouldn’t happen if TestRunInt passed
        var site = slate.Get<Site>("site");
        if (site == null || !site.Tile.Valid)
        {
            Log.Error("Rimgate :: no valid site found in slate.");
            return;
        }

        // We’ll key everything off the site’s signals site.MapGenerated is the *first* kick.
        // From there we run a repeating interval signal.
        string startSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
        string triggerRaidSignal = QuestGen.GenerateNewSignal("GateComplex.TriggerRaid");
        string questEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("gate.site.QuestEnd");

        // Stop raids if the site goes away (destroyed/abandoned)
        string siteRemovedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");

        var repeater = new QuestPart_DelayedPassOutInterval
        {
            inSignalEnable = startSignal,  // still gated by site.MapGenerated
            InSignalsDisable = { questEndSignal, siteRemovedSignal },
            OutSignals = { triggerRaidSignal },
            InitialDelayTicks = InitialDelay.RandomInRange,
            TicksInterval = Interval
        };
        quest.AddPart(repeater);

        // 2) Actual raid executor (reusing your RandomFactionRaid part)
        bool isSpace = site.Tile.LayerDef.isSpace;
        var raid = new QuestPart_RandomFactionRaid
        {
            inSignal = triggerRaidSignal,
            mapParent = site,
            pointsRange = RaidPointsFactor,
            AllowNeolithic = !isSpace,
            UseGateIfAvailable = true,
            arrivalMode = isSpace
                ? PawnsArrivalModeDefOf.EdgeDrop
                : PawnsArrivalModeDefOf.EdgeWalkIn,
            raidStrategy = RimgateDefOf.ImmediateAttackSmart,
            generateFightersOnly = true,
            fallbackToPlayerHomeMap = false,
            UseLetterKey = "RG_LetterRaidGateComplexDesc"
        };

        quest.AddPart(raid);

        // 3) Clean stops: end the raid loop when site map removed or quest
        // ends by any other path
        quest.End(QuestEndOutcome.Fail, 0, null, questEndSignal, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: false);
        quest.End(QuestEndOutcome.Fail, 0, null, siteRemovedSignal, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: false);

        // Surface a couple values to slate
        slate.Set("raidIntervalAvg", Interval.Average);
    }
}
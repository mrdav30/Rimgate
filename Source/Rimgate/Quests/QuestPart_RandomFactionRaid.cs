using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace Rimgate;

public class QuestPart_RandomFactionRaid : QuestPart_RandomRaid
{
    public string useLetterKey;

    public List<ThingDef> targetDefs;

    private Faction _cachedFaction;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (fallbackToPlayerHomeMap
            && (mapParent == null || !mapParent.HasMap)
            && Find.AnyPlayerHomeMap != null)
        {
            mapParent = Find.AnyPlayerHomeMap.Parent;
        }

        if (signal.tag != inSignal || mapParent == null || !mapParent.HasMap)
            return;

        if (faction == null)
        {
            if (!Utils.TryFindEnemyFaction(out _cachedFaction, false))
                return;
            customLetterLabel = "Raid".Translate() + ": " + _cachedFaction.Name;
            customLetterText = useLetterKey.Translate(_cachedFaction.NameColored).Resolve();
        }
        else
            _cachedFaction = faction;

        Map map = mapParent.Map;
        IncidentParms incidentParms = new IncidentParms();
        incidentParms.forced = true;
        incidentParms.quest = quest;
        incidentParms.target = map;
        incidentParms.points = useCurrentThreatPoints 
            ? (StorytellerUtility.DefaultThreatPointsNow(map) * currentThreatPointsFactor) 
            : pointsRange.RandomInRange;
        incidentParms.faction = _cachedFaction;
        incidentParms.customLetterLabel = signal.args.GetFormattedText(customLetterLabel);
        incidentParms.customLetterText = signal.args.GetFormattedText(customLetterText).Resolve();

        attackTargets = map.listerThings
            .ThingsOfDef(RimgateDefOf.Rimgate_ZPM)
            .Where(p => p.Faction == Faction.OfPlayer 
                && p.Spawned 
                && (p is Building_ZPM b ? b.IsBroadcasting : true))
            .ToList();

        incidentParms.attackTargets = attackTargets;
        incidentParms.generateFightersOnly = generateFightersOnly;
        incidentParms.sendLetter = sendLetter;
        if (arrivalMode != null)
            incidentParms.raidArrivalMode = arrivalMode;

        IncidentDef incidentDef = RimgateDefOf.Rimgate_Marauders;
        if (raidStrategy != null)
            incidentParms.raidStrategy = raidStrategy;

        if (_cachedFaction != null)
        {
            incidentParms.points = Mathf.Max(
                incidentParms.points,
                _cachedFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
        }

        if (incidentDef.Worker.CanFireNow(incidentParms))
            incidentDef.Worker.TryExecute(incidentParms);
    }

    private void FindTargetsOnMap(Map map)
    {

    }
}

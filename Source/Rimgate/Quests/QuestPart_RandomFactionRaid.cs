using System;
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
    public string UseLetterKey = "RG_LetterMaraudersDesc";

    public List<ThingDef> TargetDefs;

    public Predicate<Thing> TargetPredicate;

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
            if (!Utils.TryFindEnemyFaction(out faction, false))
                return;
        }

        customLetterLabel ??= "Raid".Translate() + ": " + faction.Name;
        customLetterText ??= UseLetterKey.Translate(faction.NameColored).Resolve();

        Map map = mapParent.Map;
        IncidentParms incidentParms = new IncidentParms();
        incidentParms.forced = true;
        incidentParms.quest = quest;
        incidentParms.target = map;
        incidentParms.points = useCurrentThreatPoints
            ? (StorytellerUtility.DefaultThreatPointsNow(map) * currentThreatPointsFactor)
            : pointsRange.RandomInRange;
        incidentParms.faction = faction;
        incidentParms.customLetterLabel = signal.args.GetFormattedText(customLetterLabel);
        incidentParms.customLetterText = signal.args.GetFormattedText(customLetterText).Resolve();

        if (TargetDefs != null)
        {
            attackTargets ??= new List<Thing>();
            for (int i = 0; i <= TargetDefs.Count; i++)
            {
                var foundTargets = map.listerThings
                    .ThingsOfDef(TargetDefs[i])
                    .Where(p => p.Faction == Faction.OfPlayer
                        && p.Spawned
                        && (TargetPredicate != null ? TargetPredicate(p) : true))
                    .ToList();
                if (foundTargets.Any())
                    attackTargets.AddRange(foundTargets);
            }
        }

        incidentParms.attackTargets = attackTargets;
        incidentParms.generateFightersOnly = generateFightersOnly;
        incidentParms.sendLetter = sendLetter;
        if (arrivalMode != null)
            incidentParms.raidArrivalMode = arrivalMode;

        IncidentDef incidentDef = RimgateDefOf.Rimgate_Marauders;
        incidentParms.raidStrategy = raidStrategy;

        if (faction != null)
        {
            incidentParms.points = Mathf.Max(
                incidentParms.points,
                faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
        }

        if (incidentDef.Worker.CanFireNow(incidentParms))
            incidentDef.Worker.TryExecute(incidentParms);
    }
}

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

    public bool AllowNeolithic;

    public bool UseStargateIfAvailable;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != inSignal)
            return;

        if (fallbackToPlayerHomeMap
            && (mapParent == null || !mapParent.HasMap)
            && Find.AnyPlayerHomeMap != null)
        {
            mapParent = Find.AnyPlayerHomeMap.Parent;
        }

        if (mapParent == null || !mapParent.HasMap)
            return;

        if (!Utils.TryFindEnemyFaction(out faction, allowNeolithic: AllowNeolithic))
            return;

        customLetterLabel = "Raid".Translate() + ": " + faction.Name;
        customLetterText = UseLetterKey.Translate(faction.NameColored).Resolve();

        Map map = mapParent.Map;
        IncidentParms incidentParms = new IncidentParms();
        incidentParms.forced = true;
        incidentParms.quest = quest;
        incidentParms.target = map;
        float pts = StorytellerUtility.DefaultThreatPointsNow(map);
        incidentParms.points = useCurrentThreatPoints
            ? pts * currentThreatPointsFactor
            : pts * pointsRange.RandomInRange;
        incidentParms.faction = faction;
        incidentParms.customLetterLabel = customLetterLabel;
        incidentParms.customLetterText = customLetterText;

        if (TargetDefs != null)
        {
            attackTargets ??= new List<Thing>();
            attackTargets.Clear();
            for (int i = 0; i < TargetDefs.Count; i++)
            {
                var target = TargetDefs[i];
                if (target == null) continue;

                var foundTargets = map.listerThings
                    .ThingsOfDef(target)
                    .Where(p => p.Faction.IsOfPlayerFaction()
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

        if (UseStargateIfAvailable)
        {
            var sg = Building_Stargate.GetStargateOnMap(map);
            bool isValid = sg != null
                && !sg.IsActive
                && !sg.IsIrisActivated;
            if (isValid)
                incidentParms.raidArrivalMode = RimgateDefOf.Rimgate_StargateEnterMode;
            else
                incidentParms.raidArrivalMode = arrivalMode;
        }
        else
            incidentParms.raidArrivalMode = arrivalMode;

        incidentParms.raidStrategy = raidStrategy;

        if (faction != null)
        {
            incidentParms.points = Mathf.Max(
                incidentParms.points,
                faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
        }

        IncidentDef incidentDef = RimgateDefOf.Rimgate_Marauders;
        if (incidentDef.Worker.CanFireNow(incidentParms))
            incidentDef.Worker.TryExecute(incidentParms);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref AllowNeolithic, "AllowNeolithic");
        Scribe_Values.Look(ref UseStargateIfAvailable, "UseStargateIfAvailable");
    }
}

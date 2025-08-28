using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Rimgate;

public class IncidentWorker_Marauders : IncidentWorker_RaidEnemy
{
    protected override bool TryResolveRaidFaction(IncidentParms parms)
    {
        if (parms.faction != null)
            return true;

        if (!Utils.TryFindEnemyFaction(out Faction faction))
            return false;

        parms.faction = faction;
        return true;
    }

    protected override void ResolveRaidPoints(IncidentParms parms)
    {
        if (parms.points == -1)
            parms.points = StorytellerUtility.DefaultThreatPointsNow(parms.target) * 2f;
    }

    public override void ResolveRaidStrategy(IncidentParms parms, PawnGroupKindDef groupKind)
    {
        if (parms.raidStrategy == null)
            parms.raidStrategy = Rimgate_DefOf.ImmediateAttackSmart;
    }

    public override void ResolveRaidArriveMode(IncidentParms parms)
    {
        if (parms.raidArrivalMode == null)
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        ResolveRaidPoints(parms);
        if (!TryResolveRaidFaction(parms)
            || !FactionUtility.HostileTo(parms.faction, Faction.OfPlayer)) return false;

        PawnGroupKindDef combat = PawnGroupKindDefOf.Combat;
        ResolveRaidStrategy(parms, combat);
        ResolveRaidArriveMode(parms);
        parms.raidStrategy.Worker.TryGenerateThreats(parms);
        if (!parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
            return false;

        parms.points = IncidentWorker_Raid.AdjustedRaidPoints(
            parms.points,
            parms.raidArrivalMode,
            parms.raidStrategy,
            parms.faction,
            combat,
            parms.target);
        List<Pawn> threatPawns = parms.raidStrategy.Worker.SpawnThreats(parms);
        if (threatPawns == null)
        {
            PawnGroupMakerParms pawnMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(
                combat,
                parms,
                false);
            threatPawns = PawnGroupMakerUtility.GeneratePawns(pawnMakerParms, true).ToList<Pawn>();
            if (threatPawns.Count == 0)
            {
                Log.Error("Got no pawns spawning raid from parms " + parms?.ToString());
                return false;
            }
            parms.raidArrivalMode.Worker.Arrive(threatPawns, parms);
        }

        GenerateRaidLoot(parms, parms.points, threatPawns);
        TaggedString letterLabel = GetLetterLabel(parms);
        TaggedString letterText = GetLetterText(parms, threatPawns);
        PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(
            threatPawns,
            ref letterLabel,
            ref letterText,
            GetRelatedPawnsInfoLetterText(parms),
            true,
            true);
        List<TargetInfo> targetInfoList = new List<TargetInfo>();
        if (parms.pawnGroups != null)
        {
            List<List<Pawn>> splitPawns = IncidentParmsUtility.SplitIntoGroups(threatPawns, parms.pawnGroups);
            List<Pawn> maxPawns = GenCollection.MaxBy<List<Pawn>, int>(splitPawns, (x => x.Count));
            if (GenCollection.Any<Pawn>(maxPawns))
                targetInfoList.Add(maxPawns[0]);

            for (int index = 0; index < splitPawns.Count; ++index)
            {
                if (splitPawns[index] != maxPawns && GenCollection.Any<Pawn>(splitPawns[index]))
                    targetInfoList.Add(splitPawns[index][0]);
            }
        }
        else if (GenCollection.Any<Pawn>(threatPawns))
        {
            foreach (Pawn pawn in threatPawns)
                targetInfoList.Add(pawn);
        }

        SendStandardLetter(
            letterLabel,
            letterText,
            GetLetterDef(),
            parms,
            targetInfoList,
            Array.Empty<NamedArgument>());

        Thing target = null;
        if (parms.attackTargets != null && parms.attackTargets.Count > 0)
            target = parms.attackTargets.RandomElement();

        foreach (Pawn pawn in threatPawns)
        {
            if (pawn.RaceProps.Humanlike)
            {
                int num = !Rand.Chance(0.3f) ? 0 : 1;
                pawn.mindState.duty = num == 0
                    ? new PawnDuty(Rimgate_DefOf.Rimgate_MaraudColony, target)
                    : new PawnDuty(DutyDefOf.AssaultColony);
            }
            else
                pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
        }

        var job = new LordJob_MaraudColony(parms.faction, canKidnap: false, canTimeoutOrFlee: true,
                                           sappers: true, useAvoidGridSmart: true,
                                           canSteal: true, priorityTarget: target);
        LordMaker.MakeNewLord(parms.faction, job, (Map)parms.target, threatPawns);

        Find.StoryWatcher.statsRecord.numRaidsEnemy++;
        parms.target.StoryState.lastRaidFaction = parms.faction;

        return true;
    }

    protected override string GetLetterLabel(IncidentParms parms)
    {
        return string.IsNullOrEmpty(parms.customLetterLabel)
            ? def.letterLabel + ": " + parms.faction.Name
            : parms.customLetterLabel;
    }

    protected override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
    {
        return string.IsNullOrEmpty(parms.customLetterText)
            ? "RG_LetterMaraudersDesc".Translate(parms.faction)
            : parms.customLetterText;
    }

    protected override string GetRelatedPawnsInfoLetterText(IncidentParms parms)
    {
        return "LetterRelatedPawnsRaidEnemy".Translate(Faction.OfPlayer.def.pawnsPlural, parms.faction.def.pawnsPlural);
    }
}

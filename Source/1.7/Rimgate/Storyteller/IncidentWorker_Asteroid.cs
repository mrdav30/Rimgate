using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class IncidentWorker_Asteroid : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        return this.TryFindCell(out IntVec3 intVec, map) && RimgateMod.Settings.EnableAsteroidIncidents;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        IntVec3 intVec;
        if (!this.TryFindCell(out intVec, map))
            return false;

        List<Thing> list = RimgateDefOf.Rimgate_Meteorite.root.Generate();
        SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, list, intVec, map);
        Find.LetterStack.ReceiveLetter(
            "RG_LetterNaquadahAsteroid_Label".Translate(),
            "RG_LetterNaquadahAsteroid_Desc".Translate(),
            LetterDefOf.NeutralEvent,
            new TargetInfo(intVec, map, false),
            null,
            null);

        if (map != null && map.Parent != null)
            TriggerRaid(map.Parent, list);

        return true;
    }

    private bool TryFindCell(out IntVec3 cell, Map map)
    {
        int maxMineables = ThingSetMaker_SpecialMeteorite.MineablesCountRange.max;
        return CellFinderLoose.TryFindSkyfallerCell(
            ThingDefOf.MeteoriteIncoming,
            map,
            TerrainAffordanceDefOf.Light,
            out cell, 10,
            default(IntVec3),
            -1,
            true,
            false,
            false,
            false,
            true,
            true,
            delegate (IntVec3 x)
            {
                int num = Mathf.CeilToInt(Mathf.Sqrt((float)maxMineables)) + 2;
                CellRect cellRect = CellRect.CenteredOn(x, num, num);
                int num2 = 0;
                foreach (IntVec3 current in cellRect)
                {
                    if (current.InBounds(map) && current.Standable(map))
                        num2++;
                }
                return num2 >= maxMineables;
            });
    }

    public void TriggerRaid(MapParent mapParent, List<Thing> targets)
    {
        if (mapParent != null && mapParent.HasMap)
        {
            if (!Utils.TryFindEnemyFaction(out Faction enemyFaction))
                return;

            IncidentParms incidentParms = new IncidentParms();
            incidentParms.forced = true;
            incidentParms.target = mapParent.Map;
            incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(mapParent.Map);
            incidentParms.faction = enemyFaction;
            incidentParms.customLetterLabel = "Raid".Translate() + ": " + enemyFaction.Name;
            incidentParms.customLetterText = "RG_LetterRaidAstroid_Desc".Translate(enemyFaction.NameColored, RimgateDefOf.Rimgate_MineableNaquadah.LabelCap).Resolve();
            incidentParms.attackTargets = targets;
            incidentParms.generateFightersOnly = true;
            incidentParms.sendLetter = true;
            incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            incidentParms.raidStrategy = RimgateDefOf.StageThenAttack;
            incidentParms.points = Mathf.Max(
                incidentParms.points,
                enemyFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));

            IncidentDef incidentDef = IncidentDefOf.RaidEnemy;
            if (incidentDef.Worker.CanFireNow(incidentParms))
                incidentDef.Worker.TryExecute(incidentParms);
        }
    }
}

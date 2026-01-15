using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Rimgate;

/// <summary>
/// Like KCSG.GenStep_EnnemiesPresence, but does NOT modify the site faction.
/// - forcedFaction controls spawned pawn faction only.
/// - If forcedFaction is null, it uses (in order):
///     1) parms.sitePart.site.Faction (if present)
///     2) a random enemy faction
///     3) AncientsHostile as final fallback
/// </summary>
public class GenStep_EnemiesPresence_NoSiteFaction : GenStep
{
    public FactionDef forcedFaction;

    public bool useSiteFactionIfAny = true;

    public float pointMultiplier = 1f;

    public bool spawnOnEdge;

    public FloatRange defaultPointsRange = new FloatRange(300f, 500f);

    public override int SeedPart => 1466666193; // keep stable unless you want different RNG stream

    public override void Generate(Map map, GenStepParams parms)
    {
        IntVec3 cell = Utils.TryFindSpawnCellNear(map, RimgateDefOf.Rimgate_Dwarfgate);
        if (!cell.IsValid)
        {
            if (RimgateMod.Debug)
                Log.Warning("Rimgate :: Could not find valid spawn cell near Rimgate_Dwarfgate. Aborting enemy presence generation.");
            return;
        }

        Faction faction = ResolvePawnFaction(map, parms);

        // This genstep is intentionally pawn-only.

        Faction siteFaction = parms.sitePart?.site?.Faction;
        bool hostileToSiteFaction = siteFaction != null && faction.HostileTo(siteFaction);

        LordJob job = (siteFaction != null && faction == siteFaction)
                ? (LordJob)new LordJob_DefendBase(faction, map.Center, 25000)
                : hostileToSiteFaction
                    ? new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: false, sappers: false,
                    useAvoidGridSmart: true, canSteal: true, breachers: true, canPickUpOpportunisticWeapons: true)
                    : new LordJob_DefendPoint(map.Center, addFleeToil: false);

        Lord lord = LordMaker.MakeNewLord(faction, job, map);

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Spawning enemies presence for faction '{faction.Name}' at {cell}"
                + (siteFaction != null ? $"(site faction: '{siteFaction.Name}')" : "(no site faction)"));

        foreach (Pawn pawn in GeneratePawns(map, faction, parms))
        {
            GenSpawn.Spawn(pawn, cell, map);
            lord.AddPawn(pawn);
        }
    }

    private Faction ResolvePawnFaction(Map map, GenStepParams parms)
    {
        // 1) forced faction (preferred)
        if (forcedFaction != null)
        {
            Faction forced = Find.FactionManager.FirstFactionOfDef(forcedFaction);
            if (forced != null) return forced;
        }

        // 2) site faction (if any) — but we do NOT modify it
        if (useSiteFactionIfAny)
        {
            Faction siteFaction = parms.sitePart?.site?.Faction;
            if (siteFaction != null) return siteFaction;
        }

        // 3) enemy faction fallback
        Faction enemy = Find.FactionManager.RandomEnemyFaction(
            allowHidden: false,
            allowDefeated: false,
            allowNonHumanlike: true,
            minTechLevel: TechLevel.Neolithic
        );
        if (enemy != null) return enemy;

        // 4) hard fallback
        return Faction.OfAncientsHostile;
    }

    private IEnumerable<Pawn> GeneratePawns(Map map, Faction faction, GenStepParams parms)
    {
        float points;

        float threatPoints = parms.sitePart?.parms?.threatPoints ?? -1f;
        if (threatPoints >= defaultPointsRange.min && threatPoints <= defaultPointsRange.max)
        {
            points = threatPoints;
        }
        else
        {
            points = defaultPointsRange.RandomInRange;
        }

        points = Math.Max(points, 150f) * pointMultiplier;

        return PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Combat,
            tile = map.Tile,
            faction = faction,
            points = points
        });
    }
}

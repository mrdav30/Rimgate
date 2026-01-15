using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace Rimgate;

public class QuestNode_Root_GateSite : QuestNode
{
    public const string SiteAlias = "site";

    public const string FactionAlias = "enemyFaction";

    public class SitePartOption
    {
        [NoTranslate]
        public SitePartDef def;

        public float chance = 1f;
    }

    private SlateRef<List<SitePartOption>> sitePartDefs;
    private SlateRef<WorldObjectDef> worldObjectDef;

    private SlateRef<FactionDef> factionDef;

    // list of candidate faction defs to use if factionDef is unset
    public SlateRef<List<FactionDef>> factionDefWhitelist;

    private SlateRef<List<BiomeDef>> allowedBiomes;
    private SlateRef<Hilliness?> maxHilliness;
    private SlateRef<IntRange> distanceFromColonyRange;
    private SlateRef<List<LandmarkDef>> allowedLandmarks;
    private SlateRef<float?> selectLandmarkChance;
    private SlateRef<bool?> requiresLandmark;
    private SlateRef<bool?> desperateIgnoreBiome;
    private SlateRef<bool?> desperateIgnoreDistance;

    public SlateRef<bool> planetSurfaceOnly; // if true => reject orbit layers
    public SlateRef<bool> orbitOnly;      // if true => accept only orbit layers

    public SlateRef<bool> requireSameOrAdjacentLayer = true;
    public SlateRef<List<PlanetLayerDef>> layerWhitelist;
    public SlateRef<List<PlanetLayerDef>> layerBlacklist;

    protected override bool TestRunInt(Slate slate)
    {
        if (!TryFindSiteTile(slate, out _))
        {
            if (RimgateMod.Debug)
                Log.Error("Rimgate :: failed to find valid site tile.");
            return false;
        }
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;

        if (!TryFindSiteTile(slate, out var tile))
        {
            Log.Error("Rimgate :: Could not find valid site tile.");
            return;
        }

        Faction resolvedFaction = ResolveFaction(slate);
        slate.Set<Faction>(FactionAlias, resolvedFaction);
        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Generating gate site at tile {tile} for faction {resolvedFaction.Name}.");

        var points = slate.Get<float>("points", 0f);
        List<SitePartOption> sitePartDefsFor = sitePartDefs.GetValue(slate);
        if (sitePartDefsFor.NullOrEmpty())
        {
            Log.Error("Rimgate :: No site part defs specified for site generation.");
            return;
        }

        List<SitePartDefWithParams> partDefWithParams = new List<SitePartDefWithParams>();
        for (int i = 0; i < sitePartDefsFor.Count; i++)
        {
            var part = sitePartDefsFor[i];
            float chance = Mathf.Clamp01(part.chance);
            if (chance < 1f && !Rand.Chance(chance))
                continue;

            var def = part.def;
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Generating site part {def.defName} with {points} points.");
            partDefWithParams.Add(new SitePartDefWithParams(def, new SitePartParams
            {
                points = points,
                threatPoints = points
            }));
        }

        Site site = QuestGen_Sites.GenerateSite(
            partDefWithParams,
            tile,
            resolvedFaction,
            hiddenSitePartsPossible: false,
            singleSitePartRules: null,
            worldObjectDef: worldObjectDef.GetValue(slate)
        );

        slate.Set(SiteAlias, site);
        quest.SpawnWorldObject(site);
    }

    private Faction ResolveFaction(Slate slate)
    {
        // Try explicit factionDef first
        FactionDef resolvedDef = null;
        Faction faction = null;

        if (factionDef != null)
            resolvedDef = factionDef.GetValue(slate);

        // (2) If empty, fall back to random from whitelist
        if (resolvedDef == null)
        {
            List<FactionDef> candidates = factionDefWhitelist.GetValue(slate);
            if (!candidates.NullOrEmpty())
                resolvedDef = candidates.RandomElement();
        }

        if (resolvedDef != null)
        {
            faction = Find.FactionManager.FirstFactionOfDef(resolvedDef);
            if (faction != null)
                return faction;
        }

        // (3) Finally, try to find any enemy faction
        if (Utils.TryFindEnemyFaction(out faction))
            return faction;

        // Default fallback if still null
        return Faction.OfAncientsHostile;
    }

    protected virtual bool TryFindSiteTile(Slate slate, out PlanetTile tile)
    {
        if (!TryGetLayer(slate, out var source, out var layer))
        {
            Log.Error("Rimgate :: Could not find valid planet layer for site.");
            tile = PlanetTile.Invalid;
            return false;
        }

        Hilliness hilliness = maxHilliness.GetValue(slate) ?? Hilliness.Mountainous;

        IntRange distRange = distanceFromColonyRange.GetValue(slate);
        int trueMin = distRange.TrueMin;
        int trueMax = distRange.TrueMax;

        bool requireLm = requiresLandmark.GetValue(slate).GetValueOrDefault();
        float chance = selectLandmarkChance.GetValue(slate) ?? 0.5f;

        FastTileFinder.LandmarkMode landmarkMode =
            (requireLm || Rand.Chance(chance))
                ? FastTileFinder.LandmarkMode.Required
                : FastTileFinder.LandmarkMode.Forbidden;

        FastTileFinder.TileQueryParams query =
            new FastTileFinder.TileQueryParams(source, trueMin, trueMax, landmarkMode, reachable: true, Hilliness.Undefined, hilliness);

        int desperateMin = (!desperateIgnoreDistance.GetValue(slate).GetValueOrDefault()) ? trueMin : 0;
        float desperateMax = desperateIgnoreDistance.GetValue(slate).GetValueOrDefault()
            ? float.MaxValue
            : trueMax;

        bool checkBiome = (!desperateIgnoreBiome.GetValue(slate)) ?? true;

        FastTileFinder.TileQueryParams desperate =
            new FastTileFinder.TileQueryParams(source, desperateMin, desperateMax, FastTileFinder.LandmarkMode.Any, reachable: true, Hilliness.Undefined, hilliness, checkBiome);

        List<PlanetTile> candidates =
            layer.FastTileFinder.Query(query, allowedBiomes.GetValue(slate), allowedLandmarks.GetValue(slate), desperate);

        if (!candidates.Empty())
        {
            tile = candidates.RandomElement();
            return true;
        }

        tile = PlanetTile.Invalid;
        return false;
    }

    protected virtual bool TryGetLayer(Slate slate, out PlanetTile source, out PlanetLayer layer)
    {
        layer = null;

        // If planetSurfaceOnly is true, don't let the "source" tile finder choose space tiles.
        // If both are true, treat it as don't care.
        bool surfaceLayerOnly = planetSurfaceOnly.GetValue(slate);
        bool orbitLayerOnly = orbitOnly.GetValue(slate);
        bool allowSpaceForSource = !surfaceLayerOnly || orbitLayerOnly;

        if (RimgateMod.Debug && surfaceLayerOnly && orbitLayerOnly)
            Log.Warning("Rimgate :: planetSurfaceOnly && orbitOnly both true; treating as unrestricted.");

        Map map = QuestGen.slate.Get<Map>("map");
        if (map != null && map.Tile.Valid)
        {
            source = map.Tile;
        }
        else if (!TileFinder.TryFindRandomPlayerTile(
            out source,
            allowCaravans: false,
            validator: (PlanetTile t) =>
            {
                bool isSpace = t.LayerDef.isSpace;

                if (!orbitLayerOnly && surfaceLayerOnly && isSpace)
                    return false;

                if (!surfaceLayerOnly && orbitLayerOnly && !isSpace)
                    return false;

                return true;
            },
            canBeSpace: allowSpaceForSource))
        {
            source = surfaceLayerOnly && !orbitLayerOnly
                ? Find.WorldGrid.Surface.Tiles.RandomElement().tile
                : orbitLayerOnly && !surfaceLayerOnly
                    ? Find.WorldGrid.Orbit.Tiles.RandomElement().tile
                    : Find.WorldGrid.Tiles.RandomElement().tile;
        }

        if (Validator(source, source.Layer))
        {
            layer = source.Layer;
        }
        else
        {
            foreach (KeyValuePair<int, PlanetLayer> kvp in Find.WorldGrid.PlanetLayers.InRandomOrder())
            {
                PlanetLayer candidate = kvp.Value;
                if (candidate != source.Layer && Validator(source, candidate))
                {
                    layer = candidate;
                    break;
                }
            }
        }

        return layer != null;

        bool Validator(PlanetTile origin, PlanetLayer candidateLayer)
        {
            List<PlanetLayerDef> whitelist = layerWhitelist.GetValue(slate);
            List<PlanetLayerDef> blacklist = layerBlacklist.GetValue(slate);

            if (!whitelist.NullOrEmpty() && !whitelist.Contains(candidateLayer.Def))
                return false;

            if (!blacklist.NullOrEmpty() && blacklist.Contains(candidateLayer.Def))
                return false;

            if (requireSameOrAdjacentLayer.GetValue(slate) &&
                origin.Valid &&
                origin.Layer != candidateLayer &&
                !candidateLayer.DirectConnectionTo(origin.Layer))
            {
                return false;
            }

            return true;
        }
    }
}

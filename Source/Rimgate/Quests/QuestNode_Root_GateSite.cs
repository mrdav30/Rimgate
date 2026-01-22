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

    public const string SitePointsAlias = "sitePoints";

    public class SitePartOption
    {
        [NoTranslate]
        public SitePartDef def;

        public float chance = 1f;
    }

    private SlateRef<List<SitePartOption>> sitePartDefs;
    private SlateRef<float?> noneWeight;
    private SlateRef<WorldObjectDef> worldObjectDef;

    private SlateRef<FactionDef> factionDef;
    // list of candidate faction defs to use if factionDef is unset
    public SlateRef<List<FactionDef>> factionDefWhitelist;
    public SlateRef<PawnGroupKindDef> pawnGroupKindDef;

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

    private static readonly SimpleCurve ThreatPointsOverPointsCurve = new SimpleCurve
    {
        new CurvePoint(35f, 38.5f),
        new CurvePoint(400f, 165f),
        new CurvePoint(10000f, 4125f)
    };

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

        List<SitePartOption> sitePartDefsFor = sitePartDefs.GetValue(slate);
        if (sitePartDefsFor.NullOrEmpty())
        {
            Log.Error("Rimgate :: No site part defs specified for site generation.");
            return;
        }

        if (!TryGetSitePartDefs(sitePartDefsFor, slate, out var partDefWithParams))
            return;

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
        var kindDef = pawnGroupKindDef.GetValue(slate) ?? PawnGroupKindDefOf.Settlement;
        if (Utils.TryFindEnemyFaction(out faction, groupKindDef: kindDef))
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

    protected virtual bool TryGetSitePartDefs(
        List<SitePartOption> partOptions,
        Slate slate,
        out List<SitePartDefWithParams> partDefWithParams)
    {
        // 1) Always-include parts (chance >= 1)
        List<SitePartOption> always = new List<SitePartOption>();
        List<SitePartOption> optional = new List<SitePartOption>();

        var minThreatPoints = 0f;
        for (int i = 0; i < partOptions.Count; i++)
        {
            SitePartOption opt = partOptions[i];
            if (opt == null || opt.def == null) continue;

            minThreatPoints = Mathf.Max(minThreatPoints, opt.def.minThreatPoints);

            // Treat >= 1 as always-included
            if (opt.chance >= 1f)
                always.Add(opt);
            else if (opt.chance > 0f)
                optional.Add(opt);
            // <= 0 => never
        }

        var points = slate.Get<float>("points", 0f);
        var threatPoints = Find.Storyteller.difficulty.allowViolentQuests
            ? ThreatPointsOverPointsCurve.Evaluate(points)
            : 0f;
        threatPoints = Mathf.Max(minThreatPoints, threatPoints);
        slate.Set(SitePointsAlias, threatPoints);
        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Using {points} points and {threatPoints} threat points for site generation.");

        // Must have at least one always part (your rule)
        if (always.Count == 0)
        {
            Log.Error("Rimgate :: GateSite requires at least one always-included site part (chance >= 1).");
            partDefWithParams = null;
            return false;
        }

        var result = new List<SitePartDefWithParams>(always.Count + 1);

        // Add always parts
        for (int i = 0; i < always.Count; i++)
            AddPart(always[i].def);

        // 2) Optional pool: pick at most one, or none
        // "none" gets a baseline weight of 1.0 so that optional weights behave sensibly.
        float noneW = noneWeight.GetValue(slate) ?? 1f;
        SitePartOption picked = PickOneOptionalOrNone(optional, noneW);
        if (picked != null)
            AddPart(picked.def);

        void AddPart(SitePartDef def)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Generating site part {def.defName}");

            result.Add(new SitePartDefWithParams(def, new SitePartParams
            {
                points = points,
                threatPoints = threatPoints
            }));
        }

        partDefWithParams = result;
        return true;
    }

    private static SitePartOption PickOneOptionalOrNone(List<SitePartOption> optional, float noneWeight)
    {
        if (optional == null || optional.Count == 0)
            return null;

        // Sum weights)
        float total = noneWeight;
        for (int i = 0; i < optional.Count; i++)
        {
            float w = optional[i].chance;
            if (w > 0f) total += w;
        }

        if (total <= 0f) return null;

        // Roll in [0, total)
        float roll = Rand.Value * total;

        // If we land in the "none" segment, pick none
        if (roll < noneWeight)
            return null;

        roll -= noneWeight;

        // Otherwise pick one weighted optional
        for (int i = 0; i < optional.Count; i++)
        {
            float w = optional[i].chance;
            if (w <= 0f) continue;

            if (roll < w)
                return optional[i];

            roll -= w;
        }

        // Fallback shouldn't happen due to math, but keep it safe.
        return null;
    }
}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;

namespace Rimgate;

internal static class Utils
{
    private static Faction _ofReplicators;

    public static Faction OfReplicators
    {
        get
        {
            _ofReplicators ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_Replicator);
            return _ofReplicators;
        }
    }

    public static readonly IntVec3 SmallestMapSize = new IntVec3(75, 1, 75);

    public static Pawn ClosestTo(this IEnumerable<Pawn> pawns, IntVec3 c)
    {
        Pawn best = null; var bestDist = 999999;
        foreach (var p in pawns)
        {
            var d = p.Position.DistanceToSquared(c);
            if (d < bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    public static bool PawnIncapableOfHauling(Pawn p, out string reason)
    {
        reason = null;
        // Vanilla uses both WorkTags and WorkType
        if (p.WorkTagIsDisabled(WorkTags.ManualDumb) || p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
        {
            reason = "RG_IncapableOf".Translate(p.LabelShort, WorkTypeDefOf.Hauling.gerundLabel).CapitalizeFirst();
            return true;
        }

        // also block if Manipulation is missing
        if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            reason = "RG_IncapableOf".Translate(p.LabelShort, PawnCapacityDefOf.Manipulation.label).CapitalizeFirst();
            return true;
        }

        return false;
    }

    public static bool IsGoodSpawnCell(IntVec3 c, Map map)
    {
        if (!c.InBounds(map)) return false;
        if (!c.Standable(map)) return false;              // avoids walls, pawns, etc.
        if (c.Filled(map)) return false;                  // no buildings/solid things
        return true;
    }

    // Prefer the cell in front of pawn; then other cardinals; else nearby radius.
    public static IntVec3 BestDropCellNearThing(Thing t)
    {
        var map = t.Map;
        var from = t.def.hasInteractionCell ? t.InteractionCell : t.Position + t.Rotation.FacingCell;
        if (map == null) return from;
        if (from != t.Position && IsGoodSpawnCell(from, map)) return from;

        // try other cardinals in rotation order: right, back, left
        for (int i = 1; i < 4; i++)
        {
            var r = new Rot4((t.Rotation.AsInt + i) % 4);
            var c = t.Position + r.FacingCell;
            if (c != t.Position && IsGoodSpawnCell(c, map)) return c;
        }

        // small radial fallback
        foreach (var c in GenRadial.RadialCellsAround(t.Position, 2f, useCenter: false))
            if (c != t.Position && IsGoodSpawnCell(c, map)) return c;

        // last resort: current cell (should be rare)
        return t.Position;
    }

    // Pick a good stand cell (adjacent to dest, reachable, closest to pawn)
    public static IntVec3 FindStandCellFor(Pawn pawn, IntVec3 dest, Map map, IntVec3 from)
    {
        if (pawn == null || pawn.health.Downed) return from;

        IntVec3 best = dest; float bestDist = float.MaxValue;
        foreach (var c in GenAdj.CellsAdjacentCardinal(dest, Rot4.North, new IntVec2(1, 1)))
        {
            if (!c.InBounds(map) || c.Impassable(map)) continue;
            if (!map.reachability.CanReach(from, c, PathEndMode.Touch, TraverseParms.For(pawn))) continue;
            float d = c.DistanceTo(from);
            if (d < bestDist) { best = c; bestDist = d; }
        }
        // fallback: stand on current cell if nothing else
        return IsGoodSpawnCell(best, map) ? best : from;
    }

    public static Rot4 RotationFacingFor(IntVec3 from, IntVec3 to)
    {
        var v = to - from;
        if (Mathf.Abs(v.x) > Mathf.Abs(v.z)) return v.x >= 0 ? Rot4.East : Rot4.West;
        return v.z >= 0 ? Rot4.North : Rot4.South;
    }

    public static bool HasActiveGeneOf(this Pawn pawn, GeneDef geneDef)
    {
        if (geneDef is null) return false;
        if (pawn.genes is null) return false;
        return pawn.genes.GetGene(geneDef)?.Active ?? false;
    }

    public static bool HasActiveGene(this Pawn pawn, string geneDefName)
    {
        if (string.IsNullOrEmpty(geneDefName)) return false;
        if (pawn.genes is null) return false;
        return pawn.genes?.GetGene(DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName))?.Active ?? false;
    }

    public static bool IsXenoTypeOf(this Pawn pawn, XenotypeDef xenotypeDef)
    {
        if (xenotypeDef is null) return false;
        if (pawn.genes is null) return false;
        return pawn.genes.Xenotype == xenotypeDef;
    }

    public static bool HasHediff(this Pawn pawn, HediffDef def)
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null || def is null) return false;

        return set.HasHediff(def);
    }

    public static bool HasHediff<T>(this Pawn pawn) where T : Hediff
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null) return false;

        return set.HasHediff<T>();
    }

    public static Hediff GetHediff(this Pawn pawn, HediffDef def)
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null || def is null) return null;

        if (set.TryGetHediff(def, out Hediff result))
            return result;
        return null;
    }

    public static T GetHediff<T>(this Pawn pawn) where T : Hediff
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null) return null;

        if(set.TryGetHediff<T>(out T result))
            return result;
        return null;
    }

    public static Hediff ApplyHediff(
        this Pawn pawn,
        HediffDef hediffDef,
        BodyPartRecord bodyPart,
        int duration,
        float severity)
    {
        Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, bodyPart);

        if (severity > float.Epsilon)
            hediff.Severity = severity;

        if (hediff is HediffWithComps hediffWithComps)
        {
            foreach (HediffComp comp in hediffWithComps.comps)
            {
                if (duration > 0 && comp is HediffComp_Disappears hediffComp_Disappears)
                {
                    hediffComp_Disappears.ticksToDisappear = duration;
                }
            }
        }

        pawn.health.AddHediff(hediff);
        return pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
    }

    public static bool CanUseFaction(Faction f, bool allowNeolithic = true)
    {
        if (f == null) return false;
        if (f.temporary) return false;
        if (f.defeated) return false;
        if (f.IsPlayer) return false;

        // Only humanlike factions or mechanoids
        if (!(f.def.humanlikeFaction || f == Faction.OfMechanoids || f == OfReplicators)) return false;

        // Optional Neolithic filter
        if (!allowNeolithic && f.def.techLevel == TechLevel.Neolithic) return false;

        // Hidden factions are excluded, except mechanoids
        if (f.Hidden && f != Faction.OfMechanoids) return false;

        // Must actually be hostile to the player right now
        return f.HostileTo(Faction.OfPlayer);
    }

    public static bool TryFindEnemyFaction(out Faction faction, bool allowNeolithic = true)
    {
        var candidates = Find.FactionManager.AllFactions
            .Where(f => CanUseFaction(f, allowNeolithic));
        // Flat random:
        return candidates.TryRandomElement(out faction);

        // Or weighted (example; adjust weight source as you like):
        // return candidates.TryRandomElementByWeight(f => f.def.raidCommonalityFromPointsCurve?.Evaluate(1000f) ?? 1f, out faction);
    }

    internal static void ThrowDebugText(string text, Vector3 drawPos, Map map)
    {
        MoteMaker.ThrowText(drawPos, map, text, -1f);
    }

    internal static void ThrowDebugText(string text, IntVec3 c, Map map)
    {
        MoteMaker.ThrowText(c.ToVector3Shifted(), map, text, -1f);
    }
}

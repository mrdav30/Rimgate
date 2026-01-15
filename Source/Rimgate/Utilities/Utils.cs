using KCSG;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Analytics;
using Verse;
using Verse.AI;

namespace Rimgate;

internal static class Utils
{
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

    public static bool IncapableOfHauling(this Pawn p, out string reason)
    {
        reason = null;
        // Vanilla uses both WorkTags and WorkType
        if (p.WorkTagIsDisabled(WorkTags.ManualDumb) || p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
        {
            reason = "RG_IncapableOf".Translate(p.LabelShort, WorkTypeDefOf.Hauling.gerundLabel).CapitalizeFirst();
            return true;
        }

        // also block if Manipulation is missing
        if (p.health == null || !p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            reason = "RG_IncapableOf".Translate(p.LabelShort, PawnCapacityDefOf.Manipulation.label).CapitalizeFirst();
            return true;
        }

        return false;
    }

    public static bool IncapableOfGivingAid(this Pawn p, out string reason)
    {
        reason = null;
        // Vanilla uses both WorkTags and WorkType
        if (p.WorkTagIsDisabled(WorkTags.Caring) || p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
        {
            reason = "RG_IncapableOf".Translate(p.LabelShort, WorkTypeDefOf.Doctor.gerundLabel).CapitalizeFirst();
            return true;
        }

        // also block if Manipulation is missing
        if (p.health == null || !p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            reason = "RG_IncapableOf".Translate(p.LabelShort, PawnCapacityDefOf.Manipulation.label).CapitalizeFirst();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGoodSpawnCell(IntVec3 c, Map map) => c.InBounds(map) && c.Standable(map);

    // Prefer the cell in front of pawn;
    // then other cardinals; else nearby radius.
    public static IntVec3 BestDropCellNearThing(Thing t)
    {
        var map = t.Map;
        if (map == null)
            return t.Position;

        var from = t.def.hasInteractionCell ? t.InteractionCell : t.Position + t.Rotation.FacingCell;
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

    // Pick a good stand cell
    // (adjacent to dest, reachable, closest to pawn)
    public static bool FindStandCellFor(
        IntVec3 from,
        IntVec3 dest,
        Map map,
        out IntVec3 result)
    {
        result = default;

        float bestDist = float.MaxValue; 
        bool found = false;
        foreach (var c in GenAdj.CellsAdjacentCardinal(dest, Rot4.North, new IntVec2(1, 1)))
        {
            if (!IsGoodSpawnCell(c, map)) 
                continue;
            bool canReach = map.reachability.CanReach(
                from,
                c,
                PathEndMode.OnCell,
                TraverseParms.For(TraverseMode.ByPawn));
            if (!canReach) continue;
            float d = c.DistanceTo(from);
            if (d < bestDist) 
            {
                result = c; 
                bestDist = d;
                found = true;
            }
        }

        return found;
    }

    public static bool FindStandCellFor(
        Pawn pawn,
        IntVec3 dest,
        out IntVec3 result)
    {
        var map = pawn.Map;
        result = default;
        float bestDist = float.MaxValue;
        bool found = false;

        foreach (var c in GenAdj.CellsAdjacentCardinal(dest, Rot4.North, new IntVec2(1, 1)))
        {
            if (!IsGoodSpawnCell(c, map))
                continue;

            if (!map.reachability.CanReach(
                    pawn.Position,
                    c,
                    PathEndMode.OnCell,
                    TraverseParms.For(pawn)))
                continue;

            float d = c.DistanceTo(pawn.Position);
            if (d < bestDist)
            {
                result = c;
                bestDist = d;
                found = true;
            }
        }

        return found;
    }

    public static void TryPlaceExactOrNear(Thing thing, Map map, IntVec3? pos, Rot4? rot)
    {
        bool placed = false;
        if (pos.HasValue)
        {
            var c = pos.Value;
            var r = rot ?? Rot4.North;
            if (c.InBounds(map) && c.Standable(map) && c.GetEdifice(map) == null)
                placed = GenPlace.TryPlaceThing(thing, c, map, ThingPlaceMode.Direct, rot: r);
        }

        if (!placed)
        {
            var found = CellFinder.TryFindRandomReachableNearbyCell(
                map.Center, map, 8f, TraverseParms.For(TraverseMode.PassDoors),
                c => c.Standable(map) && c.GetEdifice(map) == null,
                null, out var drop);
            if (!found) drop = map.Center;
            GenPlace.TryPlaceThing(thing, drop, map, ThingPlaceMode.Near);
        }
    }

    public static IntVec3 TryFindSpawnCellNear(Map map, ThingDef def)
    {
        bool Valid(IntVec3 c) => c.Standable(map) && !c.Impassable(map); // optional: avoid zones

        if (def != null)
        {
            foreach (var t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (t.def == def)
                {
                    if (CellFinder.TryFindRandomCellNear(t.Position, map, 12, Valid, out IntVec3 nearVec))
                        return nearVec;
                }
            }
        }

        if (CellFinder.TryFindRandomCellNear(map.Center, map, 20, Valid, out IntVec3 alt))
            return alt;

        return CellFinder.TryFindRandomSpawnCellForPawnNear(map.Center, map, out IntVec3 result, extraValidator: Valid) ? result : IntVec3.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 AddY(Vector3 v, float dy) => new(v.x, v.y + dy, v.z);

    public static Rot4 RotationFacingFor(IntVec3 from, IntVec3 to)
    {
        var v = to - from;
        if (Mathf.Abs(v.x) > Mathf.Abs(v.z)) return v.x >= 0 ? Rot4.East : Rot4.West;
        return v.z >= 0 ? Rot4.North : Rot4.South;
    }

    // rotate a local offset into world-space for a given Rot4
    public static IntVec3 RotateOffset(IntVec3 o, Rot4 rot)
    {
        // Coordinates are (x,z) on the map grid. Y must remain 0.
        return rot.AsInt switch
        {
            0 => o,                          // North
            1 => new IntVec3(o.z, 0, -o.x),  // East  (90° CW)
            2 => new IntVec3(-o.x, 0, -o.z), // South (180°)
            3 => new IntVec3(-o.z, 0, o.x),  // West  (270°)
            _ => o
        };
    }

    public static bool HasHiveConnection(this Pawn p)
    {
        // Prefer exact xenotype def if you have it
        if (p?.genes == null) return false;
        if (p.genes.Xenotype == RimgateDefOf.Rimgate_Wraith) return true;
        if (p.HasActiveGeneOf(RimgateDefOf.Rimgate_WraithPsychic)) return true;
        return false;
    }

    public static bool HasSymbiote(this Pawn pawn)
    {
        return pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant)
            || pawn.HasHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch)
            || pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant_Kull);
    }

    public static bool CanSocialize(Pawn p1, Pawn p2)
    {
        if (p1 == null || p2 == null) return false;
        if (p1 == p2) return false;
        if (p1.Dead || p2.Dead) return false;
        if (p1.Faction == null || p2.Faction != p1.Faction) return false;
        if (p1.RaceProps == null || p2.RaceProps == null) return false;
        if (!p1.RaceProps.Humanlike || !p2.RaceProps.Humanlike) return false;

        return true;
    }

    public static void TryGiveThought(this Pawn pawn, ThoughtDef def, Pawn otherPawn = null)
    {
        if (pawn == null || def == null) return;
        var memories = pawn.needs?.mood?.thoughts?.memories;
        if(memories == null) return;

        // Refresh the short memory so it stays while in range (no stacking)
        if (memories.GetFirstMemoryOfDef(def) is Thought_Memory existing)
            existing.Renew(); // refresh duration
        else
            memories.TryGainMemory(def, otherPawn);
    }
        
    public static bool HasActiveQuestOf(QuestScriptDef def)
    {
        if (def == null) return false;

        return Find.QuestManager.QuestsListForReading
            .Any(q => q != null
                      && q.State == QuestState.Ongoing
                      && q.root == def);
    }

    public static bool CanUseFaction(Faction f, bool allowNeolithic = true)
    {
        if (f == null) return false;
        if (f.temporary) return false;
        if (f.defeated) return false;
        if (f.IsPlayer) return false;

        // Only humanlike factions or mechanoids
        if (!(f.def.humanlikeFaction || f == Faction.OfMechanoids || f == RimgateFactionOf.OfReplicators)) return false;

        // Optional Neolithic filter
        if (!allowNeolithic && f.def.techLevel == TechLevel.Neolithic) return false;

        // Hidden factions are excluded, except mechanoids
        if (f.Hidden && f != Faction.OfMechanoids) return false;

        // Must actually be hostile to the player right now
        return f.HostileTo(Faction.OfPlayer);
    }

    public static bool TryFindEnemyFaction(out Faction faction, List<FactionDef> blacklist = null, bool allowNeolithic = true)
    {
        var candidates = Find.FactionManager.AllFactions
            .Where(f => CanUseFaction(f, allowNeolithic) && (!blacklist?.Contains(f.def) ?? true));
        // Flat random:
        return candidates.TryRandomElement(out faction);

        // Or weighted (example; adjust weight source as you like):
        // return candidates.TryRandomElementByWeight(f => f.def.raidCommonalityFromPointsCurve?.Evaluate(1000f) ?? 1f, out faction);
    }

    public static Pawn GeneratePawnForContainer(Map map, Faction faction = null, List<PawnKindDef> pawnKindDefs = null)
    {
        if (pawnKindDefs != null && pawnKindDefs.Count > 0)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKindDefs.RandomElement(), faction, forceGenerateNewPawn: true));
        }

        return PawnGenerator.GeneratePawn(new PawnGenerationRequest(faction != null ? faction.RandomPawnKind() : PawnKindDefOf.Villager, faction, forceGenerateNewPawn: true));
    }

    public static string FormatTicksToPeriod(this float ticks)
    {
        return ((int)Mathf.Max(0, ticks)).ToStringTicksToPeriod();
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

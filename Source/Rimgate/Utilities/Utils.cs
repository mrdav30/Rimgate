using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

internal static class Utils
{
    internal static bool HasActiveGene(this Pawn pawn, GeneDef geneDef)
    {
        if (geneDef is null) return false;
        if (pawn.genes is null) return false;
        return pawn.genes.GetGene(geneDef)?.Active ?? false;
    }

    internal static Hediff ApplyHediff(
        Pawn targetPawn,
        HediffDef hediffDef,
        BodyPartRecord bodyPart,
        int duration,
        float severity)
    {
        Hediff hediff = HediffMaker.MakeHediff(hediffDef, targetPawn, bodyPart);

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

        targetPawn.health.AddHediff(hediff);
        return targetPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
    }

    public static bool CanUseFaction(Faction f, bool allowNeolithic = true)
    {
        if (f == null) return false;
        if (f.temporary) return false;
        if (f.defeated) return false;
        if (f.IsPlayer) return false;

        // Only humanlike factions or mechanoids
        if (!(f.def.humanlikeFaction || f == Faction.OfMechanoids)) return false;

        // Optional Neolithic filter
        if (!allowNeolithic && f.def.techLevel == TechLevel.Neolithic) return false;

        // Hidden factions are excluded, except mechanoids (keep your intent)
        if (f.Hidden && f != Faction.OfMechanoids) return false;

        // Must actually be hostile to the player right now
        return f.HostileTo(Faction.OfPlayer);
    }

    public static bool TryFindEnemyFaction(out Faction faction, bool allowNeolithic = true)
    {
        var candidates = Find.FactionManager.AllFactions.Where(f => CanUseFaction(f, allowNeolithic));
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

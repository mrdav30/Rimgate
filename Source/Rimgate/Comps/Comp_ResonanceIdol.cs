using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_ResonanceIdol : ThingComp
{
    private CompProperties_ResonanceIdol Props => (CompProperties_ResonanceIdol)props;

    private float RadiusSq => Props.radius * Props.radius;

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!parent.Spawned) return;

        var map = parent.Map;
        if (map == null) return;

        // Pull nearby pawns using AllPawnsSpawned
        var center = parent.Position.ToVector3Shifted();

        // Affect any spawned humanoid Pawn.
        List<Pawn> nearby = map.mapPawns.AllPawnsSpawned
            .Where(p => p.PositionHeld.IsValid && !p.Dead && p.Spawned && !p.RaceProps.IsMechanoid)
            .Where(p => (p.Position.ToVector3Shifted() - center).sqrMagnitude <= RadiusSq)
            .ToList();

        if(!nearby.Any()) return;

        // Apply/refresh effects
        foreach (var pawn in nearby)
        {
            TryApplyBuffs(pawn);
            TryApplyMood(pawn);
            MaybeApplySpooky(pawn);
        }
    }

    private void TryApplyBuffs(Pawn pawn)
    {
        if (Props.fieldBuffHediff != null)
            RefreshTimedHediff(pawn, Props.fieldBuffHediff, Props.refreshTicks);

        // Hosts (Goa'uld symbiote) get a small extra boost
        if (pawn.HasSymbiote())
            RefreshTimedHediff(pawn, Props.hostBoostHediff, Props.refreshTicks);
    }

    private void TryApplyMood(Pawn pawn)
    {
        bool isWraith = pawn.IsXenoTypeOf(RimgateDefOf.Rimgate_Wraith);
        var def = isWraith ? Props.thoughtNearForWraith : Props.thoughtNear;
        if (def == null) return;

        var memories = pawn.needs?.mood?.thoughts?.memories;
        if (memories == null) return;

        // Refresh the short memory so it stays while in range (no stacking)
        if (memories.GetFirstMemoryOfDef(def) is Thought_Memory existing)
            existing.Renew(); // refresh duration
        else
            memories.TryGainMemory(def);
    }

    private void MaybeApplySpooky(Pawn pawn)
    {
        if (Props.thoughtNegative == null || pawn.needs?.mood == null) return;

        if (Rand.MTBEventOccurs(Props.negativeThoughtMtbDays, GenDate.TicksPerDay, 250))
            pawn.TryGiveThought(Props.thoughtNegative);
    }

    private void RefreshTimedHediff(Pawn pawn, HediffDef def, int refreshTicks)
    {
        var hd = pawn.GetHediffOf(def);
        if (hd == null)
        {
            hd = HediffMaker.MakeHediff(def, pawn);
            pawn.health.AddHediff(hd);
        }

        // If it has a disappears comp, keep it alive by resetting the timer
        var disappears = hd.TryGetComp<HediffComp_Disappears>();
        if (disappears != null)
        {
            disappears.ticksToDisappear = Mathf.Max(disappears.ticksToDisappear, refreshTicks);
        }
    }
}

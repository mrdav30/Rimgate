using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Building_SymbioteAmbushTrap : Building_Trap
{
    private const float AmbushRadius = 2.9f;

    // extra roll after TrapSpringChance
    private const float AmbushChance = 0.35f;

    private const int CheckIntervalTicks = 60; // ~1x per second

    protected override void Tick()
    {
        base.Tick();
        if (!Spawned || !this.IsHashIntervalTick(CheckIntervalTicks))
            return;

        Map map = Map;
        if (map == null) return;

        // Scan a small radius around the pool
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, AmbushRadius, true))
        {
            if (!cell.InBounds(map)) continue;

            Pawn pawn = cell.GetFirstPawn(map);
            if (pawn == null) continue;
            if (!PawnCanTrigger(pawn)) continue;

            // per-pawn chance when a pawn comes close
            if (!Rand.Chance(AmbushChance))
                continue;

            Spring(pawn);
            break;
        }
    }

    private bool PawnCanTrigger(Pawn p)
    {
        // Reject non-humanlikes, dead, or already implanted
        return p.RaceProps.Humanlike
            && !p.Dead
            && !p.Downed
            && !p.IsPsychologicallyInvisible()
            && !p.HasHediff(RimgateDefOf.Rimgate_SymbioteImplant);
    }

    protected override void SpringSub(Pawn p)
    {
        // Apply hediff to torso
        BodyPartRecord torso = p.health.hediffSet.GetNotMissingParts()
            .FirstOrDefault(part => part.def == BodyPartDefOf.Torso) ?? p.RaceProps.body.corePart;

        var hediff = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteImplant, p, torso);
        p.health.AddHediff(hediff);

        // FX
        RimgateDefOf.Rimgate_SymbioteSpawn?.PlayOneShot(new TargetInfo(Position, Map));
        if (Position.ShouldSpawnMotesAt(Map))
        {
            FleckCreationData data = FleckMaker.GetDataStatic(
                p.DrawPos,
                Map,
                FleckDefOf.WaterSplash,
                Rand.Range(0.8f, 1.6f));
            Map.flecks.CreateFleck(data);
        }

        // Letter
        if (p.Faction == Faction.OfPlayer)
        {
            Find.LetterStack.ReceiveLetter(
                "RG_Letter_HostilePoolAmbush_Label".Translate(),
                $"RG_Letter_HostilePoolAmbush_Desc".Translate(p.LabelShortCap),
                LetterDefOf.ThreatSmall,
                new TargetInfo(p.Position, Map, false));

        }

        // Add a small stun to sell the “lunge”:
        p.stances.stunner.StunFor(60, this); // 1 second
    }

    public override void SetFaction(Faction newFaction, Pawn recruiter = null) { }
}

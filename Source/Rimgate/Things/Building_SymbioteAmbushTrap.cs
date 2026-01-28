using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using Verse;
using Verse.Sound;
using static UnityEngine.GridBrushBase;

namespace Rimgate;

public class Building_SymbioteAmbushTrap : Building_Trap
{
    private const float AmbushRadius = 2.9f;
    private const int CheckIntervalTicks = 60; // ~1x per second
    private const int PathWalkCost = 40;
    private const int IgnoreTrapDurationTicks = 600; // 10 seconds

    // tune chances (sum doesn’t need to be 1; we’ll normalize)
    private const float ChanceMatureLunge = 0.55f;
    private const float ChanceWildPrimtaTakeover = 0.45f;

    // tune damage
    private static readonly FloatRange LungeDamageFactorRange = new FloatRange(1, 3);
    private static readonly FloatRange LungeArmorPen = new FloatRange(0.10f, 0.25f);
    private static readonly IntRange StunnedDurationTicks = new IntRange(60, 90); // 1-1.5 seconds

    private Dictionary<int, int> lastCheckedTickByPawnId;

    protected override void Tick()
    {
        Map map = Map;
        if (!Spawned || map == null) return;

        // skipping parent base tick, call comp tick directly
        if (AllComps != null)
        {
            int i = 0;
            for (int count = AllComps.Count; i < count; i++)
                AllComps[i].CompTick();
        }

        if (!this.IsHashIntervalTick(CheckIntervalTicks))
            return;

        // Scan a small radius around the pool
        int maxCells = GenRadial.NumCellsInRadius(AmbushRadius);
        int now = Find.TickManager.TicksGame;
        for (int i = 0; i < maxCells; i++)
        {
            IntVec3 cell = Position + GenRadial.RadialPattern[i];
            if (!cell.InBounds(map)) continue;

            Pawn pawn = cell.GetFirstPawn(map);
            if (pawn == null) continue;
            if (!PawnCanTrigger(pawn, now)) continue;

            lastCheckedTickByPawnId ??= new();
            lastCheckedTickByPawnId[pawn.thingIDNumber] = now;

            if (!Rand.Chance(SpringChance(pawn)))
                continue;

            Spring(pawn);
            return;
        }
    }

    protected override float SpringChance(Pawn p)
    {
        if (p.kindDef.immuneToTraps || p.IsAnimal)
            return 0f;

        float chance = this.GetStatValue(StatDefOf.TrapSpringChance) * p.GetStatValue(StatDefOf.PawnTrapSpringChance);
        return Mathf.Clamp01(chance);
    }

    private bool PawnCanTrigger(Pawn p, int now)
    {
        if (lastCheckedTickByPawnId != null &&
            lastCheckedTickByPawnId.TryGetValue(p.thingIDNumber, out int lastTick))
        {
            if (now - lastTick < IgnoreTrapDurationTicks) return false;
            lastCheckedTickByPawnId.Remove(p.thingIDNumber);
        }

        return p.RaceProps.Humanlike
            && !p.Dead
            && !p.Downed
            && !p.IsPsychologicallyInvisible()
            // don’t double-dip
            && !p.HasSymbiote()
            && p.health?.hediffSet != null
            && !p.health.hediffSet.HasHediff(RimgateDefOf.Rimgate_WildPrimtaTakeover);
    }

    protected override void SpringSub(Pawn p)
    {
        // Roll outcome
        float total = ChanceMatureLunge + ChanceWildPrimtaTakeover;
        float roll = Rand.Value * total;

        if (roll < ChanceMatureLunge)
            DoMatureLunge(p);
        else
            DoWildPrimtaTakeover(p);

        // shared “ambush” FX
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
    }

    private void DoMatureLunge(Pawn p)
    {
        // Choose a sensible part: prefer torso, else core
        BodyPartRecord part = p.RaceProps.body.corePart;
        var parts = p.health.hediffSet.GetNotMissingParts();
        foreach (var bp in parts)
        {
            if (bp.def == BodyPartDefOf.Torso)
            {
                part = bp;
                break;
            }
        }

        int dmg = Mathf.CeilToInt(this.GetStatValue(StatDefOf.TrapMeleeDamage) * LungeDamageFactorRange.RandomInRange);
        float ap = LungeArmorPen.RandomInRange;

        var dinfo = new DamageInfo(DamageDefOf.Bite, dmg, ap, instigator: this, hitPart: part);
        DamageWorker.DamageResult damageResult = p.TakeDamage(dinfo);
        BattleLogEntry_DamageTaken logEntry = new(p, RimgateDefOf.Rimgate_DamageEvent_SymbioteAmbushTrap);
        Find.BattleLog.Add(logEntry);
        damageResult.AssociateWithLog(logEntry);

        p.stances?.stunner?.StunFor(StunnedDurationTicks.RandomInRange, this);

        TrySendLetter(p,
            "RG_Letter_FeralPoolAmbush_Label".Translate(),
            "RG_Letter_FeralPoolAmbush_LungeDesc".Translate(p.LabelShortCap));
    }

    private void DoWildPrimtaTakeover(Pawn p)
    {
        // Apply takeover hediff (target brain if present; otherwise core)
        BodyPartRecord part = p.health.hediffSet.GetBrain() ?? p.RaceProps.body.corePart;

        var h = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_WildPrimtaTakeover, p, part);
        p.health.AddHediff(h);

        // short stun to represent the “entry struggle”
        p.stances?.stunner?.StunFor(StunnedDurationTicks.RandomInRange, this);

        TrySendLetter(p,
            "RG_Letter_FeralPoolAmbush_Label".Translate(),
            "RG_Letter_FeralPoolAmbush_TakeoverDesc".Translate(p.LabelShortCap));
    }

    private void TrySendLetter(Pawn p, string label, string text)
    {
        // Use pawn faction instead of trap faction
        if (p.Faction != null && p.Faction.IsPlayer)
        {
            Find.LetterStack.ReceiveLetter(
                label,
                text,
                LetterDefOf.ThreatSmall,
                new TargetInfo(p.Position, Map));
        }
    }

    public override void SetFaction(Faction newFaction, Pawn recruiter = null) => base.SetFaction(null, null);

    public override ushort PathWalkCostFor(Pawn p) => PathWalkCost;

    public override bool IsDangerousFor(Pawn p) => true;

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        var map = Map;
        if (map != null)
        {
            var cells = this.OccupiedRect().Cells;
            // Change terrain to marsh
            foreach (var cell in cells)
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sand);
        }
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode)
    {
        var map = Map;
        if (!Spawned && map != null)
        {
            var cells = this.OccupiedRect().Cells;
            // Change terrain to marsh
            foreach (var cell in cells)
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sand);
        }
        base.Destroy(mode);
    }
}

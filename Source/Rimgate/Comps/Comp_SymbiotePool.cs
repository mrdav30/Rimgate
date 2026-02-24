using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_SymbiotePool : ThingComp
{
    private const float TicksPerRare = 250f;

    public CompProperties_SymbiotePool Props => (CompProperties_SymbiotePool)props;

    public CompRefuelable Refuelable => _cachedRefuelable ??= parent.GetComp<CompRefuelable>();

    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= parent.GetComp<CompPowerTrader>();

    public Comp_AnimatedSymbiotePool Animation => _cachedAnimation ??= parent.GetComp<Comp_AnimatedSymbiotePool>();

    public Texture2D QueenIcon => _cachedInsertIcon ??= ContentFinder<Texture2D>.Get(Props.symbioteQueenDef.graphicData.texPath, true);

    private float _progress; // 0-1

    private CompRefuelable _cachedRefuelable;

    private CompPowerTrader _cachedPowerTrader;

    private Comp_AnimatedSymbiotePool _cachedAnimation;

    private Texture2D _cachedInsertIcon;

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _progress, "progress", 0f);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (!Prefs.DevMode || parent is not Building_SymbioteSpawningPool)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "DEV: Spawn in ~10 ticks",
            defaultDesc = "Sets production progress to about 10 ticks before completion. It should spawn on the next pool production check.",
            action = () =>
            {
                float ticksPerProduct = GetTicksPerProduct();
                float elapsedTicks = Mathf.Clamp(ticksPerProduct - Mathf.Max(0f, 10f), 0f, ticksPerProduct);
                _progress = elapsedTicks / ticksPerProduct;
            }
        };

        yield return new Command_Action
        {
            defaultLabel = "DEV: Spawn next tick",
            defaultDesc = "Sets production to completion (0 ticks remaining) so the next production check spawns immediately.",
            action = () =>
            {
                // "_progress" stores completion ratio. 1f means 0 ticks remaining.
                _progress = 1f;
            }
        };
    }

    public override void CompTickRare()
    {
        if (!parent.Spawned) return;
        if (PowerTrader != null && !PowerTrader.PowerOn) return;

        var pool = parent as Building_SymbioteSpawningPool;
        if (pool == null) return;

        var larvaeCount = pool.HeldItemsCount;
        if (larvaeCount > 0 && (Props.upkeepFuelPerDayBase > 0f || Props.upkeepFuelPerExtraSymbiote > 0f))
        {
            float perDay = ComputeUpkeepPerDay(larvaeCount);
            if (perDay > 0f)
            {
                // Convert daily upkeep to "per rare tick"
                float upkeepPerRare = perDay * (TicksPerRare / GenDate.TicksPerDay);
                if (Refuelable == null || !Refuelable.HasFuel || Refuelable.Fuel < upkeepPerRare)
                {
                    // No feed: symbiotes begins to suffer.
                    ApplyStarvationOrGoFeral(pool);
                    Animation?.Toggle(pool.HasAnyContents);
                    return;
                }

                Refuelable.ConsumeFuel(upkeepPerRare);

                HealAllInjuredThings(
                    pool,
                    Props.healPerThingPerUpkeepEvent,
                    Props.maxThingsHealedPerUpkeepEvent);
            }
        }

        // VFX when there’s contents
        Animation?.Toggle(pool.HasAnyContents);

        // Need some fuel and a queen to progress production
        if ((Refuelable != null && !Refuelable.HasFuel) || !pool.HasQueen)
            return;

        // Production progresses every rare tick, and spawns a new symbiote when it reaches 1.0 (100%).
        _progress += TicksPerRare / GetTicksPerProduct();
        if (_progress < 1f) return;

        // Ready to complete a product;
        // Pay production cost before completing
        if (Refuelable != null && Props.fuelPerSymbiote > 0f)
        {
            if (Refuelable.Fuel < Props.fuelPerSymbiote)
            {
                // hold at “ready” until feed appears
                _progress = 1f;
                return;
            }
            Refuelable.ConsumeFuel(Props.fuelPerSymbiote);
        }

        TryProduceSymbiote();
        _progress = 0f;
    }

    private float ComputeUpkeepPerDay(int larvaeCount)
    {
        if (larvaeCount <= 0) return 0f;

        if (Props.freeSymbiotesBeforeUpkeep > 0)
        {
            int extra = Mathf.Max(0, larvaeCount - Props.freeSymbiotesBeforeUpkeep);
            return Props.upkeepFuelPerDayBase + extra * Props.upkeepFuelPerExtraSymbiote;
        }

        return Props.upkeepFuelPerDayBase + larvaeCount * Props.upkeepFuelPerExtraSymbiote;
    }

    private float GetTicksPerProduct()
    {
        float ticksPerProduct = Props.daysPerSymbiote * GenDate.TicksPerDay;
        if (ticksPerProduct <= 0f)
            ticksPerProduct = GenDate.TicksPerDay;
        return ticksPerProduct;
    }

    private void ApplyStarvationOrGoFeral(Building_SymbioteSpawningPool pool)
    {
        if (!pool.HasAnyContents) return;

        // Prefer damaging larvae first (prim'ta / symbiote items), then queen.
        Thing target = FindRandomLarva(pool);
        if (target != null)
        {
            DamageThing(target, Props.starvationDamagePerEvent);
            return;
        }

        Thing queen = FindQueen(pool);
        if (queen != null)
        {
            DamageThing(queen, Props.starvationDamagePerEvent);

            // If queen is sufficiently injured, collapse the basin into a feral trap.
            if (ShouldConvertToFeral(pool, queen))
                ConvertPoolToFeralTrap(pool);
        }
    }

    private Thing FindRandomLarva(Building_SymbioteSpawningPool pool)
    {
        var list = pool.InnerContainer?.InnerListForReading;
        if (list == null || list.Count == 0) return null;

        // Reservoir-pick one larva without allocating.
        Thing picked = null;
        int seen = 0;

        var queenDef = Props.symbioteQueenDef;
        for (int i = 0; i < list.Count; i++)
        {
            Thing t = list[i];
            if (t == null || t.Destroyed || t.def == queenDef) continue;

            seen++;
            if (Rand.RangeInclusive(1, seen) == 1)
                picked = t;
        }

        return picked;
    }

    private void DamageThing(Thing t, int amount)
    {
        if (t == null || t.Destroyed || amount <= 0) return;
        t.TakeDamage(new DamageInfo(DamageDefOf.Rotting, amount));
    }

    private Thing FindQueen(Building_SymbioteSpawningPool pool)
    {
        var queenDef = Props.symbioteQueenDef;
        if (queenDef == null) return null;

        var list = pool.InnerContainer?.InnerListForReading;
        if (list == null) return null;

        for (int i = 0; i < list.Count; i++)
        {
            Thing t = list[i];
            if (t != null && !t.Destroyed && t.def == queenDef)
                return t;
        }

        return null;
    }

    private bool ShouldConvertToFeral(Building_SymbioteSpawningPool pool, Thing queen)
    {
        if (Props.feralTrapDef == null) return false;
        if (!pool.Spawned) return false;

        // Only convert once.
        if (pool.Map == null) return false;

        // Use hitpoints ratio (Thing has HitPoints / MaxHitPoints).
        if (!queen.def.useHitPoints) return false;

        float hpPct = (float)queen.HitPoints / Mathf.Max(1, queen.MaxHitPoints);
        return hpPct <= Props.queenHealthPctToGoFeral;
    }

    private void ConvertPoolToFeralTrap(Building_SymbioteSpawningPool pool)
    {
        Map map = pool.Map;
        if (map == null) return;

        bool wasPlayerOwned = pool.Faction?.IsPlayer == true;
        IntVec3 pos = pool.Position;
        Rot4 rot = pool.Rotation;
        ThingDef stuff = pool.Stuff;
        IEnumerable<IntVec3> cells = pool.OccupiedRect().Cells;

        // Destroy the pool first (including contents)
        pool.Destroy(DestroyMode.KillFinalize);

        // Change terrain to marsh
        foreach (var cell in cells)
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Marsh);

        // Spawn feral trap basin
        Thing trap = ThingMaker.MakeThing(Props.feralTrapDef, stuff);
        GenSpawn.Spawn(trap, pos, map, rot);
        trap.SetFaction(null); // neutral

        // Optional: message/letter if player owned
        if (wasPlayerOwned)
        {
            Find.LetterStack.ReceiveLetter(
                "RG_SymbiotePool_GoneFeral_Label".Translate(),
                "RG_SymbiotePool_GoneFeral_Desc".Translate(),
                LetterDefOf.ThreatBig,
                new TargetInfo(pos, map));
        }
    }

    private void HealAllInjuredThings(Building_SymbioteSpawningPool pool, int healAmountPerThing, int maxThingsToHeal)
    {
        if (healAmountPerThing <= 0 || maxThingsToHeal <= 0) return;

        var list = pool.InnerContainer?.InnerListForReading;
        if (list == null || list.Count == 0) return;

        int healed = 0;

        for (int i = 0; i < list.Count && healed < maxThingsToHeal; i++)
        {
            Thing t = list[i];
            if (t == null || t.Destroyed) continue;
            if (!t.def.useHitPoints) continue;

            int maxHp = t.MaxHitPoints;
            if (maxHp <= 0) continue;

            if (t.HitPoints >= maxHp) continue;

            int newHp = t.HitPoints + healAmountPerThing;
            t.HitPoints = (newHp > maxHp) ? maxHp : newHp;
            healed++;
        }
    }

    private void TryProduceSymbiote()
    {
        if (Props.productSymbioteDef == null || !parent.Spawned)
            return;

        if (parent is not Building_SymbioteSpawningPool pool)
            return;

        // cap how many symbiotes can get produced
        if (pool.InnerContainer.TotalStackCount >= pool.MaxHeldItems)
            return;

        Thing queen = FindQueen(pool);
        if (queen is Thing_SymbioteQueen queenThing)
            queenThing.EnsureLineageInitialized();

        Thing product = ThingMaker.MakeThing(Props.productSymbioteDef);
        SymbioteLineageUtility.AssumeLineage(product, SymbioteLineageUtility.GetLineage(queen));

        // If storage rules reject (e.g., player changed filter),
        // just don’t create it.
        if (!pool.Accepts(product)) return;

        pool.InnerContainer.TryAddOrTransfer(product);
    }

    public override string CompInspectStringExtra()
    {
        var pool = parent as Building_SymbioteSpawningPool;
        var sb = new StringBuilder();

        if (pool.HasQueen)
        {
            sb.Append("RG_PoolQueenPresent".Translate("present"));

            if (Refuelable == null || Refuelable.HasFuel)
            {
                float ticksRemaining = Props.daysPerSymbiote * GenDate.TicksPerDay * (1f - _progress);
                sb.AppendLine();
                sb.Append("RG_NextSymbioteCount".Translate(ticksRemaining.FormatTicksToPeriod()));
            }

            int larvaeCount = pool.HeldItemsCount;
            if (larvaeCount > 0 && (Props.upkeepFuelPerDayBase > 0f || Props.upkeepFuelPerExtraSymbiote > 0f))
            {
                float perDay = ComputeUpkeepPerDay(larvaeCount);
                if (perDay > 0f)
                {
                    sb.AppendLine();
                    sb.Append("RG_PoolUpkeepPerDay".Translate(perDay.ToString("0.##"))); // add a keyed string
                }
            }

        }
        else
            sb.Append("RG_PoolQueenMissing".Translate("not present"));

        return sb.ToString().TrimEnd('\r', '\n');
    }
}

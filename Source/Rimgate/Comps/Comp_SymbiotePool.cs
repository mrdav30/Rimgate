using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static UnityEngine.GridBrushBase;

namespace Rimgate;

public class Comp_SymbiotePool : ThingComp
{
    private const float TicksPerRare = 250f;

    public CompProperties_SymbiotePool Props => (CompProperties_SymbiotePool)props;

    public CompRefuelable Refuelable => _cachedRefuelable ??= parent.GetComp<CompRefuelable>();

    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= parent.GetComp<CompPowerTrader>();

    public Texture2D QueenIcon => _cachedInsertIcon ??= ContentFinder<Texture2D>.Get(Props.symbioteQueenDef.graphicData.texPath, true);

    private float _progress; // 0-1

    private CompRefuelable _cachedRefuelable;

    private CompPowerTrader _cachedPowerTrader;

    private Texture2D _cachedInsertIcon;

    public int LarvaeCount
    {
        get
        {
            int larvaeCount = 0;
            var list = (parent as Building_SymbioteSpawningPool)?.InnerContainer?.InnerListForReading;
            if (list == null) return larvaeCount;

            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (t.def == RimgateDefOf.Rimgate_PrimtaSymbiote
                    || t.def == RimgateDefOf.Rimgate_GoauldSymbiote)
                {
                    larvaeCount++;
                }
            }
            return larvaeCount;
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _progress, "progress", 0f);
    }

    public override void CompTickRare()
    {
        if (!parent.Spawned) return;
        if (PowerTrader != null && !PowerTrader.PowerOn) return;

        var pool = parent as Building_SymbioteSpawningPool;
        if (pool == null) return;

        int larvaeCount = LarvaeCount;
        if (larvaeCount > 0
            && (Props.upkeepFuelPerDayBase > 0f || Props.upkeepFuelPerExtraSymbiote > 0f))
        {
            int extraLarvae = Mathf.Max(0, larvaeCount - Props.freeSymbiotesBeforeUpkeep);
            if (extraLarvae > 0)
            {
                float perDay = Props.upkeepFuelPerDayBase + (extraLarvae * Props.upkeepFuelPerExtraSymbiote);
                if (perDay > 0f)
                {
                    // Convert daily upkeep to "per rare tick"
                    float upkeepPerRare = perDay * (TicksPerRare / GenDate.TicksPerDay);

                    if (Refuelable == null || !Refuelable.HasFuel || Refuelable.Fuel < upkeepPerRare)
                    {
                        // No feed: brood begins to suffer.
                        ApplyStarvationDamage(pool);
                        return;
                    }

                    Refuelable.ConsumeFuel(upkeepPerRare);
                }
            }
        }

        // VFX
        bool canFx = ModsConfig.OdysseyActive
            && pool.HasAnyContents
            && Props.spawnSymbioteMotes
            && parent.IsHashIntervalTick(Props.moteIntervalTicks);
        if (canFx)
            TrySpawnSymbioteMote();

        // Need some fuel and a queen to progress production
        if ((Refuelable != null && !Refuelable.HasFuel) || !pool.HasQueen)
            return;

        // Production progress
        float ticksPerProduct = Props.daysPerSymbiote * GenDate.TicksPerDay;
        if (ticksPerProduct <= 0f) ticksPerProduct = GenDate.TicksPerDay;
        _progress += TicksPerRare / ticksPerProduct;
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

    private void ApplyStarvationDamage(Building_SymbioteSpawningPool pool)
    {
        if (pool == null || !pool.HasAnyContents)
            return;

        var list = pool.InnerContainer.InnerListForReading;
        IntRange targetCount = new IntRange(1, list.Count);
        var candidates = list
            .TakeRandom(targetCount.RandomInRange)
            .ToList();

        if (candidates.Count == 0)
            return;

        // damage all things
        foreach (var t in candidates)
        {
            if (t.Destroyed)
                continue;

            t.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 1f));
        }
    }


    private void TryProduceSymbiote()
    {
        if (Props.productSymbioteDef == null || !parent.Spawned)
            return;

        var pool = parent as Building_SymbioteSpawningPool;
        // cap how many symbiotes can get produced
        if (pool.InnerContainer.TotalStackCount >= pool.MaxHeldItems)
            return;

        Thing product = ThingMaker.MakeThing(Props.productSymbioteDef);

        // If storage rules reject (e.g., player changed filter),
        // just don’t create it.
        if (!pool.Accepts(product)) return;

        pool.InnerContainer.TryAddOrTransfer(product);
    }

    private void TrySpawnSymbioteMote()
    {
        var map = parent.Map;
        if (map == null) return;

        var def = (Rand.Bool
            ? FleckDefOf.FishShadowReverse
            : FleckDefOf.FishShadow) ?? FleckDefOf.FishShadow;
        if (def == null) return;

        // Slight jitter from center for variety
        Vector3 loc = parent.TrueCenter().SetToAltitude(AltitudeLayer.MoteLow);
        loc.x += Rand.Range(-0.10f, 0.10f);
        loc.z += Rand.Range(-0.05f, 0.05f);

        if (!loc.ShouldSpawnMotesAt(map)) return;

        float scale = Props.moteBaseScale * Props.moteScaleRange.RandomInRange;
        var data = FleckMaker.GetDataStatic(loc, map, def, scale);
        data.rotation = Rand.Range(0f, 360f);
        data.rotationRate = Rand.Sign * Props.moteRotationSpeed * Rand.Range(0.85f, 1.15f);

        map.flecks.CreateFleck(data);
    }

    public override string CompInspectStringExtra()
    {
        var pool = parent as Building_SymbioteSpawningPool;
        var sb = new StringBuilder();

        if (pool.HasQueen)
        {
            sb.Append("RG_PoolQueenPresent".Translate("present"));
            sb.AppendLine();

            float ticksRemaining = Props.daysPerSymbiote * GenDate.TicksPerDay * (1f - _progress);
            sb.Append("RG_NextSymbioteCount".Translate(ticksRemaining.FormatTicksToPeriod()));

            int larvaeCount = LarvaeCount;
            if (larvaeCount > 0
                && (Props.upkeepFuelPerDayBase > 0f || Props.upkeepFuelPerExtraSymbiote > 0f))
            {
                int extra = Mathf.Max(0, larvaeCount - Props.freeSymbiotesBeforeUpkeep);
                float perDay = Props.upkeepFuelPerDayBase + extra * Props.upkeepFuelPerExtraSymbiote;
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

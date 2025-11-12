using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_SymbiotePool : ThingComp
{
    public CompProperties_SymbiotePool Props => (CompProperties_SymbiotePool)props;

    public CompRefuelable Refuelable => _cachedRefuelable ??= parent.GetComp<CompRefuelable>();

    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= parent.GetComp<CompPowerTrader>();

    public Texture2D QueenIcon => _cachedInsertIcon ??= ContentFinder<Texture2D>.Get(Props.symbioteQueenDef.graphicData.texPath, true);

    private float _progress; // 0-1

    private CompRefuelable _cachedRefuelable;

    private CompPowerTrader _cachedPowerTrader;

    private Texture2D _cachedInsertIcon;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _progress, "progress", 0f);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();

        if (!parent.Spawned)
            return;

        if (PowerTrader != null && !PowerTrader.PowerOn)
            return;

        if (Refuelable != null && !Refuelable.HasFuel)
            return;

        if (ModsConfig.OdysseyActive)
        {
            if ((parent as Building_SymbioteSpawningPool).HasAnyContents
                && Props.spawnSymbioteMotes
                && parent.IsHashIntervalTick(Props.moteIntervalTicks))
            {
                TrySpawnSymbioteMote();
            }
        }

        if (!(parent as Building_SymbioteSpawningPool).HasQueen)
            return;

        // Progress per rare tick (250 ticks)
        float ticksPerProduct = Props.daysPerSymbiote * GenDate.TicksPerDay;
        if (ticksPerProduct <= 0f)
            ticksPerProduct = GenDate.TicksPerDay;

        _progress += 250f / ticksPerProduct;
        if (_progress < 1f) return;

        // Ready to complete a product;
        // verify payment before resetting progress
        if (Refuelable != null
            && Props.fuelPerSymbiote > 0f)
        {
            if (Refuelable.Fuel < Props.fuelPerSymbiote)
            {
                // hold at complete until there is enough fuel
                _progress = 1f;
                return;
            }

            // Pay now that we know we can
            Refuelable.ConsumeFuel(Props.fuelPerSymbiote);
        }

        TryProduceSymbiote();
        _progress = 0f;
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
        var sb = new StringBuilder();

        if ((parent as Building_SymbioteSpawningPool).HasQueen)
        {
            sb.Append("RG_PoolQueenPresent".Translate("present"));
            sb.AppendLine();
            float ticksRemaining = Props.daysPerSymbiote * GenDate.TicksPerDay * (1f - _progress);
            sb.Append("RG_NextSymbioteCount".Translate(ticksRemaining.FormatTicksToPeriod()));
        }
        else
            sb.Append("RG_PoolQueenMissing".Translate("not present"));

        return sb.ToString().TrimEnd('\r', '\n');
    }
}

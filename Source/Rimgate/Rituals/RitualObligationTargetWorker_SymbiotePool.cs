using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimgate;

public class RitualObligationTargetWorker_SymbiotePool : RitualObligationTargetFilter
{
    public RitualObligationTargetWorker_SymbiotePool()
    {
    }

    public RitualObligationTargetWorker_SymbiotePool(RitualObligationTargetFilterDef def)
        : base(def)
    {
    }

    public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
    {
        // We only care about the symbiote spawning pool def.
        var poolDef = RimgateDefOf.Rimgate_SymbioteSpawningPool;

        var pools = map.listerThings.ThingsOfDef(poolDef);
        for (int i = 0; i < pools.Count; i++)
        {
            Thing thing = pools[i];
            RitualTargetUseReport report = CanUseTarget(thing, obligation);
            if (report.canUse)
                yield return thing;
        }
    }

    protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
    {
        if (!target.HasThing)
            return false;

        var pool = target.Thing as Building_SymbioteSpawningPool;
        if (pool == null)
            return false;

        if (def.colonistThingsOnly)
        {
            if (pool.Faction == null || !pool.Faction.IsPlayer)
                return false;
        }

        // Adjust this depending on your comp API. If you have a helper (recommended),
        // you can just call comp.HasPrimtaLarva instead.
        bool hasPrimtaLarva = pool.InnerContainer.Any(
            t => t.def == RimgateDefOf.Rimgate_PrimtaSymbiote);

        if (!hasPrimtaLarva)
            return "RG_PrimtaRenewal_PoolEmpty".Translate();

        return true;
    }

    public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
    {
        yield return RimgateDefOf.Rimgate_SymbioteSpawningPool.label;
    }
}

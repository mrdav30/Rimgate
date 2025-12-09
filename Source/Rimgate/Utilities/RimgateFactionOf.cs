using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public static class RimgateFactionOf
{
    private static Faction _ofReplicators;

    public static Faction OfReplicators => _ofReplicators ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_Replicator);

    private static Faction _ofTreasureHunters;

    public static Faction OfTreasureHunters => _ofTreasureHunters ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_TreasureHunters);

    private static Faction _ofTreasureHuntersHostile;

    public static Faction OfTreasureHuntersHostile => _ofTreasureHuntersHostile ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_TreasureHuntersHostile);
}

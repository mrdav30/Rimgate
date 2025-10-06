using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public static class RimgateFactionDefOf
{
    private static Faction _ofReplicators;

    public static Faction OfReplicators
    {
        get
        {
            _ofReplicators ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_Replicator);
            return _ofReplicators;
        }
    }

    private static Faction _ofTreasureHunters;

    public static Faction OfTreasureHunters
    {
        get
        {
            _ofTreasureHunters ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_TreasureHunters);
            return _ofTreasureHunters;
        }
    }

    private static Faction _ofTreasureHuntersHostile;

    public static Faction OfTreasureHuntersHostile
    {
        get
        {
            _ofTreasureHuntersHostile ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_TreasureHuntersHostile);
            return _ofTreasureHuntersHostile;
        }
    }
}

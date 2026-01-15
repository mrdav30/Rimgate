using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public static class RimgateFactionOf
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOfPlayerFaction(this Faction f) => f != null && f == Faction.OfPlayerSilentFail;

    private static Faction _ofReplicators;

    public static Faction OfReplicators => _ofReplicators ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_Replicator);

    private static Faction _ofTreasureHunters;

    public static Faction OfTreasureHunters => _ofTreasureHunters ??= Find.FactionManager.FirstFactionOfDef(RimgateDefOf.Rimgate_TreasureHunters);
}

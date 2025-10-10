using RimWorld;
using Verse;

namespace Rimgate;

// Hosts mildly like other hosts
public class ThoughtWorker_Opinion_SymbioteKinship : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p.IsPrisoner || !p.IsColonist || p.IsSlave)
            return ThoughtState.Inactive;

        bool isHost = p.health?.hediffSet?.HasHediff(RimgateDefOf.Rimgate_SymbioteImplant) ?? false;
        if (!isHost)
            return ThoughtState.Inactive;

        return ThoughtState.ActiveDefault;
    }

    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
    {
        if (p == null || other == null) return ThoughtState.Inactive;
        if (p == other) return ThoughtState.Inactive;
        if (p.Dead || other.Dead) return ThoughtState.Inactive;
        if (p.Faction == null || other.Faction != p.Faction) return ThoughtState.Inactive;
        if (p.RaceProps == null || other.RaceProps == null) return ThoughtState.Inactive;
        if (!p.RaceProps.Humanlike || !other.RaceProps.Humanlike) return ThoughtState.Inactive;

        // Apply only if both are a host
        bool pHost = p.health?.hediffSet?.HasHediff(RimgateDefOf.Rimgate_SymbioteImplant) ?? false;
        bool otherHost = other.health?.hediffSet?.HasHediff(RimgateDefOf.Rimgate_SymbioteImplant) ?? false;

        if (pHost && otherHost)
            return ThoughtState.ActiveDefault;

        return ThoughtState.Inactive;
    }
}

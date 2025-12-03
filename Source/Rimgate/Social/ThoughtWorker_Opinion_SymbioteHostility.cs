using RimWorld;
using Verse;

namespace Rimgate;

// Non-hosts dislike hosts
public class ThoughtWorker_Opinion_SymbioteHostility : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p) {
        if (p.IsPrisoner || !p.IsColonist || p.IsSlave)
            return ThoughtState.Inactive;

        if(p.HasSymbiote())
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

        // Apply only if OTHER is the host, and p is not a host
        bool otherIsHost = other.HasSymbiote();
        bool selfIsHost = p.HasSymbiote();

        if (otherIsHost && !selfIsHost)
            return ThoughtState.ActiveDefault;

        return ThoughtState.Inactive;
    }
}

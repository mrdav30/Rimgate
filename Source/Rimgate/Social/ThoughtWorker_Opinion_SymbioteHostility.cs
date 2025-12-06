using RimWorld;
using Verse;

namespace Rimgate;

// Non-hosts dislike hosts
public class ThoughtWorker_Opinion_SymbioteHostility : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p) {
        if (p.IsPrisoner || !p.IsColonist || p.IsSlave)
            return ThoughtState.Inactive;

        // If pawn is a host or has a pouch, they're *in the culture* – no generic hostility.
        if (p.IsGoauldHost())
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

        // Only non-host pawns get the *uneasy* opinion of a host.
        bool otherIsHost = other.IsGoauldHost();
        bool selfIsHost = p.IsGoauldHost();

        if (otherIsHost && !selfIsHost)
            return ThoughtState.ActiveDefault;

        return ThoughtState.Inactive;
    }
}

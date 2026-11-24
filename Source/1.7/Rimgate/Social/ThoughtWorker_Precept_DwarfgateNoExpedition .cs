using RimWorld;
using Verse;

namespace Rimgate;

public sealed class ThoughtWorker_Precept_DwarfgateNoExpedition : ThoughtWorker_Precept
{
    // Tune freely
    private const int TicksPerDay = 60000;

    // Stages: 0 = fine (inactive), 1 = uneasy, 2 = restless, 3 = ashamed
    private const int DaysToUneasy = 7;
    private const int DaysToRestless = 14;
    private const int DaysToAshamed = 30;

    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        if (p == null || p.Faction != Faction.OfPlayer) return false;
        if (!p.IsFreeColonist) return false;

        var tracker = GameComponent_RimgateDwarfgateTracker.Get();
        int last = tracker.LastExpeditionTick;

        // Never went: treat as very old
        int now = Find.TickManager.TicksGame;
        int ticksSince = (last < 0) ? int.MaxValue : (now - last);

        int daysSince = ticksSince / TicksPerDay;

        if (daysSince < DaysToUneasy) return false;  // no thought
        if (daysSince < DaysToRestless) return ThoughtState.ActiveAtStage(0);
        if (daysSince < DaysToAshamed) return ThoughtState.ActiveAtStage(1);
        return ThoughtState.ActiveAtStage(2);
    }
}

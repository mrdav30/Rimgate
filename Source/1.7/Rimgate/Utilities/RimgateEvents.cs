using RimWorld;
using System.Runtime.CompilerServices;
using Verse;

namespace Rimgate;

public static class RimgateEvents
{
    public static void Notify_ColonistEnteredGate(Pawn pawn, Building_Gate gate)
    {
        if (!pawn.IsFreeColonist) return;

        var connected = gate.ConnectedGate;
        if (connected == null) return;

        var map = connected.Map;
        if (map == null) return;

        // We only care about gate quest sites
        if (map.Parent is not WorldObject_GateQuestSite) return;

        var tracker = GameComponent_RimgateDwarfgateTracker.Get();
        tracker.LastExpeditionTick = Find.TickManager.TicksGame;

        var ev = new HistoryEvent(RimgateDefOf.Rimgate_Dwarfgate_ExpeditionStarted, pawn.Named(HistoryEventArgsNames.Doer));
        Find.HistoryEventsManager.RecordEvent(ev);
    }

    public static void Notify_GateQuestSiteRemoving(WorldObject_GateQuestSite site)
    {
        if (site.WasEverVisited) return;

        // If nobody ever went, record "ignored" (quest expired / site removed unvisited)
        var tracker = GameComponent_RimgateDwarfgateTracker.Get();
        tracker.LastIgnoredTick = Find.TickManager.TicksGame;

        var ev = new HistoryEvent(RimgateDefOf.Rimgate_Dwarfgate_SiteExpiredUnvisited);
        Find.HistoryEventsManager.RecordEvent(ev);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Notify_SymbioteDestroyed(Thing thing)
    {
        if (Current.ProgramState != ProgramState.Playing || thing == null) return;

        var ev = new HistoryEvent(RimgateDefOf.Rimgate_SymbioteDestroyed);
        Find.HistoryEventsManager.RecordEvent(ev);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Notify_ColonyOfPawnEvent(Pawn pawn, HistoryEventDef def)
    {
        if (!pawn.IsFreeColonist) return;
        var ev = new HistoryEvent(def, pawn.Named(HistoryEventArgsNames.Doer));
        Find.HistoryEventsManager.RecordEvent(ev);
    }
}

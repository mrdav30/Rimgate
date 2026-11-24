using RimWorld;
using Verse;

namespace Rimgate;

public static class MentalStateUtil
{
    public struct State
    {
        public int nextCheckTick;
        public int retryUntilTick;
        public bool initialized;
        public bool stateAddedOnce;
    }

    public struct Config
    {
        public MentalStateDef mentalState;
        public int checkIntervalTicks;
        public IntRange retryBackoffTicks;

        public bool addStateOnce;

        public bool suppressWhileDowned;
        public bool suppressWhileStunned;
        public bool suppressWhileAsleep;

        public bool applyOverOtherMentalStates;
        public bool onlyIfNonPlayerFaction;
        public bool kickFromFaction;
    }

    public static void EnsureInitialized(ref State state)
    {
        if (state.initialized) return;

        state.initialized = true;
        int now = Find.TickManager.TicksGame;
        state.nextCheckTick = now + Rand.RangeInclusive(30, 120);
        state.retryUntilTick = 0;
        // stateAddedOnce should remain whatever it was (usually false)
    }

    public static void Tick(ref State state, Pawn pawn, in Config cfg)
    {
        // Basic gating
        if (pawn == null || pawn.Dead || !pawn.Spawned) return;
        if (state.stateAddedOnce && cfg.addStateOnce) return;
        if (cfg.mentalState == null) return;
        if (cfg.onlyIfNonPlayerFaction && pawn.Faction.IsOfPlayerFaction()) return;

        int now = Find.TickManager.TicksGame;

        if (now < state.nextCheckTick) return;
        state.nextCheckTick = now + cfg.checkIntervalTicks;

        if (now < state.retryUntilTick) return;

        // Temporary suppress conditions
        if (cfg.suppressWhileDowned && pawn.Downed) return;
        if (cfg.suppressWhileStunned && (pawn.stances?.stunner?.Stunned ?? false)) return;
        if (cfg.suppressWhileAsleep && !pawn.Awake()) return;

        var msh = pawn.mindState?.mentalStateHandler;
        if (msh == null) return;

        if (msh.CurStateDef == cfg.mentalState) return;

        if (msh.InMentalState && msh.CurStateDef != cfg.mentalState)
        {
            if (!cfg.applyOverOtherMentalStates)
                return;

            msh.CurState?.RecoverFromState();
        }

        bool started = msh.TryStartMentalState(cfg.mentalState, null, forced: true);
        if (cfg.kickFromFaction)
            pawn.SetFaction(null);

        if (!started)
            state.retryUntilTick = now + cfg.retryBackoffTicks.RandomInRange;
        else
            state.stateAddedOnce = true;
    }
}

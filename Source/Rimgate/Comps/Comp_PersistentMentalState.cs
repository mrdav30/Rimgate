using RimWorld;
using Verse;

namespace Rimgate;

public class Comp_PersistentMentalState : ThingComp
{
    private int _nextCheckTick;

    private int _retryUntilTick;

    private bool _initialized;

    private bool _stateAddedOnce;

    private CompProperties_PersistentMentalState Props => (CompProperties_PersistentMentalState)props;

    private Pawn Pawn => parent as Pawn;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _initialized, "_initialized", false);
        Scribe_Values.Look(ref _nextCheckTick, "_nextCheckTick", 0);
        Scribe_Values.Look(ref _retryUntilTick, "_retryUntilTick", 0);
        Scribe_Values.Look(ref _stateAddedOnce, "_initialized", false);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (Pawn == null || _initialized) return;

        // Schedule an early first check after spawn/load
        _initialized = true;
        _nextCheckTick = Find.TickManager.TicksGame + Rand.RangeInclusive(30, 120);
        _retryUntilTick = 0;
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        TickInternal();
    }

    private void TickInternal()
    {
        // Basic gating
        if (Pawn == null || Pawn.Dead || !Pawn.Spawned) return;
        if (_stateAddedOnce && Props.addStateOnce) return;
        if (Props.mentalState == null) return;
        if (Props.onlyIfNonPlayerFaction && Pawn.Faction == Faction.OfPlayer) return;

        int now = Find.TickManager.TicksGame;

        if (now < _nextCheckTick) return;
        _nextCheckTick = now + Props.checkIntervalTicks;

        // If we recently failed to start, wait until retry window passes
        if (now < _retryUntilTick) return;

        // Temporary suppress conditions
        if (Props.suppressWhileDowned && Pawn.Downed) return;
        if (Props.suppressWhileStunned && (Pawn.stances?.stunner?.Stunned ?? false)) return;
        if (Props.suppressWhileAsleep && Pawn.CurJob?.def.joyKind == null && !Pawn.Awake()) return;

        var msh = Pawn.mindState?.mentalStateHandler;
        if (msh == null) return;

        // Already in desired state? Done.
        if (msh.CurStateDef == Props.mentalState) return;

        // In some OTHER mental state?
        if (msh.InMentalState && msh.CurStateDef != Props.mentalState)
        {
            // Wait it out; we’ll re-apply our desired state next time it clears.
            if (!Props.applyOverOtherMentalStates)
                return;

            // If you really want to override, end the current state first.
            msh.CurState?.RecoverFromState();
        }

        // Try to (re)apply then backoff before we try again
        bool started = msh.TryStartMentalState(Props.mentalState, null, forced: true);
        if (!started)
            _retryUntilTick = now + Props.retryBackoffTicks.RandomInRange;
        else
            _stateAddedOnce = true;
    }

    public override string CompInspectStringExtra()
    {
        if (Prefs.DevMode && Props.mentalState != null)
            return $"Persistent mental state: {Props.mentalState.label}";
        return null;
    }
}
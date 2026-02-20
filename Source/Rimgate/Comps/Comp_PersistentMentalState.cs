using Verse;

namespace Rimgate;

public class Comp_PersistentMentalState : ThingComp
{
    private MentalStateUtil.State _state;

    private CompProperties_PersistentMentalState Props => (CompProperties_PersistentMentalState)props;

    private Pawn Pawn => parent as Pawn;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _state.initialized, "initialized", false);
        Scribe_Values.Look(ref _state.nextCheckTick, "nextCheckTick", 0);
        Scribe_Values.Look(ref _state.retryUntilTick, "retryUntilTick", 0);
        Scribe_Values.Look(ref _state.stateAddedOnce, "stateAddedOnce", false);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (Pawn == null) return;

        // Schedule an early first check after spawn/load
        MentalStateUtil.EnsureInitialized(ref _state);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (Pawn == null) return;

        // Ensure we have initial timing even if PostSpawnSetup didn’t run for some reason.
        MentalStateUtil.EnsureInitialized(ref _state);

        MentalStateUtil.Tick(ref _state, Pawn, Props.AsMentalStateConfig());
    }

    public override string CompInspectStringExtra()
    {
        if (Prefs.DevMode && Props.mentalState != null)
            return $"Persistent mental state: {Props.mentalState.label}";
        return null;
    }
}

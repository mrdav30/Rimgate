using Verse;

namespace Rimgate;

public class HediffComp_PersistentMentalState : HediffComp
{
    private MentalStateUtil.State _state;

    private HediffCompProperties_PersistentMentalState Props
        => (HediffCompProperties_PersistentMentalState)props;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref _state.initialized, "initialized", false);
        Scribe_Values.Look(ref _state.nextCheckTick, "nextCheckTick", 0);
        Scribe_Values.Look(ref _state.retryUntilTick, "retryUntilTick", 0);
        Scribe_Values.Look(ref _state.stateAddedOnce, "stateAddedOnce", false);
    }

    public override void CompPostMake()
    {
        MentalStateUtil.EnsureInitialized(ref _state);
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        MentalStateUtil.EnsureInitialized(ref _state);
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (!Pawn.IsHashIntervalTick(Props.checkIntervalTicks)) 
            return;

        MentalStateUtil.EnsureInitialized(ref _state);
        MentalStateUtil.Tick(ref _state, Pawn, Props.AsMentalStateConfig());
    }

    public override string CompLabelInBracketsExtra
    {
        get
        {
            if (Prefs.DevMode && Props.mentalState != null)
                return Props.mentalState.label;
            return null;
        }
    }
}

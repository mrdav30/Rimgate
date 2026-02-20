using Verse;

namespace Rimgate;

public class CompProperties_PersistentMentalState : CompProperties
{
    public MentalStateDef mentalState;

    // How often we check to (re)apply when eligible.
    public int checkIntervalTicks = 300; // ~5 sec

    // Backoff after a failed TryStart to avoid hammering the handler.
    public IntRange retryBackoffTicks = new IntRange(600, 900);

    // When true, the comp applies the state only once.
    public bool addStateOnce;

    // Gating conditions:
    public bool suppressWhileDowned = true;

    public bool suppressWhileStunned = true;

    public bool suppressWhileAsleep = true;

    // If the pawn already has some other mental state, should we try to override it?
    // Usually NO; we just wait until it ends, then re-apply ours.
    public bool applyOverOtherMentalStates;

    // Optional: only apply if pawn is not a colonist (useful for “wild” variants)
    public bool onlyIfNonPlayerFaction;

    public bool kickFromFaction;

    public CompProperties_PersistentMentalState()
    {
        compClass = typeof(Comp_PersistentMentalState);
    }

    public MentalStateUtil.Config AsMentalStateConfig()
    {
        return new MentalStateUtil.Config
        {
            mentalState = mentalState,
            checkIntervalTicks = checkIntervalTicks,
            retryBackoffTicks = retryBackoffTicks,

            addStateOnce = addStateOnce,

            suppressWhileDowned = suppressWhileDowned,
            suppressWhileStunned = suppressWhileStunned,
            suppressWhileAsleep = suppressWhileAsleep,

            applyOverOtherMentalStates = applyOverOtherMentalStates,
            onlyIfNonPlayerFaction = onlyIfNonPlayerFaction,
            kickFromFaction = kickFromFaction
        };
    }
}

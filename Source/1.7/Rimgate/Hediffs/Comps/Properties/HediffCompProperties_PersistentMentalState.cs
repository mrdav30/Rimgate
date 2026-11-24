using Verse;

namespace Rimgate;

public class HediffCompProperties_PersistentMentalState : HediffCompProperties
{
    public MentalStateDef mentalState;

    public int checkIntervalTicks = 300;
    public IntRange retryBackoffTicks = new IntRange(600, 900);

    public bool addStateOnce;

    public bool suppressWhileDowned = true;
    public bool suppressWhileStunned = true;
    public bool suppressWhileAsleep = true;

    public bool applyOverOtherMentalStates;
    public bool onlyIfNonPlayerFaction;
    public bool kickFromFaction;

    public HediffCompProperties_PersistentMentalState()
    {
        compClass = typeof(HediffComp_PersistentMentalState);
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
            kickFromFaction = kickFromFaction,
        };
    }
}

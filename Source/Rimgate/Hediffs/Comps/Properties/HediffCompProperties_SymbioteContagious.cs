using Verse;

namespace Rimgate;

public class HediffCompProperties_SymbioteContagious : HediffCompProperties
{
    // Mean time between successful infection events, in in-game days, per infected pawn
    public float infectionMtbDays = 2.5f;

    // How far the “breath zone” / close contact zone is
    public float infectionRadius = 4.9f;

    public HediffCompProperties_SymbioteContagious() => compClass = typeof(HediffComp_SymbioteContagious);
}

using RimWorld;
using Verse;

namespace Rimgate;

public class CompProperties_SetInitialGestatorState : CompProperties
{
    // Default: loaded and awakens by proximity
    public CompMechGestatorTank.TankState initialState = CompMechGestatorTank.TankState.Proximity;

    // Only apply on a percent chance (1.0 = always).
    public float chance = 1f;

    // If true and the parent has a power comp, only set state when powered.
    public bool requirePowerOn = false;

    public CompProperties_SetInitialGestatorState()
    {
        compClass = typeof(Comp_SetInitialGestatorState);
    }
}

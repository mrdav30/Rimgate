using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimgate;

public class Comp_SetInitialGestatorState : ThingComp
{
    public CompProperties_SetInitialGestatorState Props => (CompProperties_SetInitialGestatorState)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (respawningAfterLoad) return;
        if (!parent.Spawned) return;
        if (!Rand.Chance(Props.chance)) return;

        // Respect power if requested
        if (Props.requirePowerOn)
        {
            var power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) return;
        }

        var tank = parent.GetComp<CompMechGestatorTank>();
        if (tank == null) return;

        // Only flip from Empty -> desired state (don't clobber saved games).
        if (tank.State == CompMechGestatorTank.TankState.Empty)
        {
            tank.State = Props.initialState;
            // Note: If you choose Proximity, the comp’s own tick will auto-check and
            // trigger ~every 250 ticks when colonists are in radius and line of sight.
        }
    }

    // Handy dev gizmos while testing
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!DebugSettings.ShowDevGizmos) yield break;

        var tank = parent.GetComp<CompMechGestatorTank>();
        if (tank == null) yield break;

        yield return new Command_Action
        {
            defaultLabel = "DEV: Set Empty",
            action = () => tank.State = CompMechGestatorTank.TankState.Empty
        };
        yield return new Command_Action
        {
            defaultLabel = "DEV: Set Dormant",
            action = () => tank.State = CompMechGestatorTank.TankState.Dormant
        };
        yield return new Command_Action
        {
            defaultLabel = "DEV: Set Proximity",
            action = () => tank.State = CompMechGestatorTank.TankState.Proximity
        };
    }
}

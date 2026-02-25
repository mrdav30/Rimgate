using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class Command_LoadToContainer : Command
{
    public Building_MobileContainer Container;

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);

        bool unreachable = !Container.Map.reachability.CanReach(
                Container.Position,
                Container,
                PathEndMode.Touch,
                TraverseParms.For(TraverseMode.PassDoors));
        if (unreachable)
        {
            Messages.Message(
                "RG_MessageCartUnreachable".Translate(),
                Container,
                MessageTypeDefOf.RejectInput,
                historical: false);
            return;
        }

        Find.WindowStack.Add(new Dialog_LoadContainers(Container.Map, Container));
    }

    public override bool InheritInteractionsFrom(Gizmo other) => false;
}

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using Verse;

namespace Rimgate;

public class Command_LoadToContainer : Command
{
    public Comp_MobileContainerControl Container;

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);

            bool unreachable = !Container.Map.reachability.CanReach(
                    Container.parent.Position,
                    Container.parent,
                    PathEndMode.Touch,
                    TraverseParms.For(TraverseMode.PassDoors));
            if (unreachable)
            {
                Messages.Message(
                    "RG_MessageCartUnreachable".Translate(),
                    Container.parent,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

        Find.WindowStack.Add(new Dialog_LoadContainers(Container.Map, Container));
    }

    public override bool InheritInteractionsFrom(Gizmo other) => false;
}

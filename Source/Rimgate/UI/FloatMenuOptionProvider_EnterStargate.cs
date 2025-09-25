using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_EnterStargate : FloatMenuOptionProvider
{
    private static List<Pawn> tmpGateEnteringPawns = new List<Pawn>();

    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => true;

    protected override bool MechanoidCanDo => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Building_Stargate gate = clickedThing as Building_Stargate;
        if (gate == null)
            yield break;

        if (!gate.StargateControl.IsActive)
        {
            yield return new FloatMenuOption("CannotEnterPortal".Translate(gate.Label) + ": " + "RG_FailReasonGateInactive".Translate(), null);
            yield break;
        }

        if (gate.StargateControl.IsIrisActivated)
        {
            yield return new FloatMenuOption("CannotEnterPortal".Translate(gate.Label) + ": " + "RG_FailReasonGateIrisClosed".Translate(), null);
            yield break;
        }

        if (!context.IsMultiselect)
        {
            if (!CanEnterGate(context.FirstSelectedPawn, gate))
            {
                yield return new FloatMenuOption("CannotEnterPortal".Translate(gate.Label) + ": " + "NoPath".Translate(), null);
                yield break;
            }
        }

        tmpGateEnteringPawns.Clear();
        foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
        {
            if (CanEnterGate(context.FirstSelectedPawn, gate))
                tmpGateEnteringPawns.Add(validSelectedPawn);
        }

        if (!tmpGateEnteringPawns.NullOrEmpty())
        {
            var enterLabel = (gate.StargateControl.IsReceivingGate
               ? "RG_EnterReceivingStargateAction"
               : "RG_EnterStargateAction").Translate();

            yield return new FloatMenuOption(enterLabel, () =>
            {
                foreach (Pawn enteringPawn in tmpGateEnteringPawns)
                {
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_EnterStargate, gate);
                    job.playerForced = true;
                    enteringPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }, MenuOptionPriority.High);

            if (context.IsMultiselect)
                yield break;

            var bringLabel = (gate.StargateControl.IsReceivingGate
                ? "RG_BringToReceivingStargate"
                : "RG_BringToStargate").Translate();
            yield return new FloatMenuOption(bringLabel, () =>
            {
                TargetingParameters targetingParameters = new TargetingParameters()
                {
                    canTargetPawns = true,
                    canTargetCorpses = true,
                    onlyTargetIncapacitatedPawns = true,
                    canTargetBuildings = true,
                    canTargetItems = true,
                    canTargetPlants = true,
                    canTargetFires = true,
                    mapObjectTargetsMustBeAutoAttackable = false
                };

                Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo t)
                {
                    if(t.Thing != null)
                    {
                        if (!t.Thing.def.Claimable) return;

                        if (t.Thing is Building_MobileContainer container)
                        {
                            var comp = container.GetComp<Comp_MobileContainer>();
                            // send a push job targeting the *gate thing*
                            comp.AssignPushJob(
                                new LocalTargetInfo(gate),
                                dump: false,
                                context.FirstSelectedPawn);
                            return;
                        }
                    }

                    Job job = JobMaker.MakeJob(
                        RimgateDefOf.Rimgate_BringToStargate,
                        t.Thing,
                        gate);
                    context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            });
        }
    }

    private static bool CanEnterGate(Pawn pawn, Building_Stargate gate)
    {
        return pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly);
    }
}

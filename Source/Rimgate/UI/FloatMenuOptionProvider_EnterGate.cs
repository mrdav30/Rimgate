using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_EnterGate : FloatMenuOptionProvider
{
    private static List<Pawn> tmpGateEnteringPawns = new List<Pawn>();

    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => true;

    protected override bool MechanoidCanDo => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return thing is Building_Gate;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Building_Gate gate = clickedThing as Building_Gate;
        Pawn pawn = context.FirstSelectedPawn;

        if (pawn == null || gate == null)
            yield break;

        if (!gate.IsActive)
        {
            yield return new FloatMenuOption("CannotEnterPortal".Translate(gate.Label) + ": " + "RG_FailReasonGateInactive".Translate(), null);
            yield break;
        }

        if (gate.IsIrisActivated)
        {
            yield return new FloatMenuOption("CannotEnterPortal".Translate(gate.Label) + ": " + "RG_FailReasonGateIrisClosed".Translate(), null);
            yield break;
        }

        if (!context.IsMultiselect)
        {
            if (!pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterPortal".Translate(gate.Label) + ": " + "NoPath".Translate(), null);
                yield break;
            }
        }

        tmpGateEnteringPawns.Clear();
        foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
        {
            if (validSelectedPawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly))
                tmpGateEnteringPawns.Add(validSelectedPawn);
        }

        if (!tmpGateEnteringPawns.NullOrEmpty())
        {
            var enterLabel = (gate.IsReceivingGate
               ? "RG_EnterReceivingGateAction"
               : "RG_EnterGateAction").Translate(gate.LabelCap);

            yield return new FloatMenuOption(enterLabel, () =>
            {
                foreach (Pawn enteringPawn in tmpGateEnteringPawns)
                {
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_EnterGate, gate);
                    job.count = 1;
                    job.playerForced = true;
                    enteringPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }, MenuOptionPriority.High);

            if (context.IsMultiselect)
                yield break;

            var bringLabel = (gate.IsReceivingGate
                ? "RG_BringToReceivingGate"
                : "RG_BringToGate").Translate(gate.LabelCap);
            yield return new FloatMenuOption(bringLabel, () =>
            {
                TargetingParameters parms = new TargetingParameters()
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

                Find.Targeter.BeginTargeting(parms, t =>
                {
                    if (t.Thing != null)
                    {
                        if (!t.Thing.def.Claimable) return;

                        if (t.Thing is Building_MobileContainer container)
                        {
                            var comp = container.Control;
                            comp.ClearDesignations();
                            // send a push job targeting the *gate thing*
                            var pushJob = comp.GetPushJob(pawn, gate);
                            if (pushJob == null) return;
                            pushJob.playerForced = true;
                            pawn.jobs.TryTakeOrderedJob(pushJob, JobTag.MiscWork);
                            return;
                        }
                    }

                    Job job = JobMaker.MakeJob(
                        RimgateDefOf.Rimgate_BringToGate,
                        t.Thing,
                        gate);
                    job.count = 1;
                    job.playerForced = true;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            });
        }
    }


}

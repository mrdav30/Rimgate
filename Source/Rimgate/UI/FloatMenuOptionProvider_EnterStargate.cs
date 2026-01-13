using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace Rimgate;

public class FloatMenuOptionProvider_EnterStargate : FloatMenuOptionProvider
{
    private static List<Pawn> tmpGateEnteringPawns = new List<Pawn>();

    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => true;

    protected override bool MechanoidCanDo => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return thing is Building_Stargate;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Building_Stargate gate = clickedThing as Building_Stargate;
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
            if (!StargateUtil.CanEnterGate(pawn, gate))
            {
                yield return new FloatMenuOption("CannotEnterPortal".Translate(gate.Label) + ": " + "NoPath".Translate(), null);
                yield break;
            }
        }

        tmpGateEnteringPawns.Clear();
        foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
        {
            if (StargateUtil.CanEnterGate(pawn, gate))
                tmpGateEnteringPawns.Add(validSelectedPawn);
        }

        if (!tmpGateEnteringPawns.NullOrEmpty())
        {
            var enterLabel = (gate.IsReceivingGate
               ? "RG_EnterReceivingStargateAction"
               : "RG_EnterStargateAction").Translate();

            yield return new FloatMenuOption(enterLabel, () =>
            {
                foreach (Pawn enteringPawn in tmpGateEnteringPawns)
                {
                    Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_EnterStargate, gate);
                    job.count = 1;
                    job.playerForced = true;
                    enteringPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }, MenuOptionPriority.High);

            if (context.IsMultiselect)
                yield break;

            var bringLabel = (gate.IsReceivingGate
                ? "RG_BringToReceivingStargate"
                : "RG_BringToStargate").Translate();
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
                        RimgateDefOf.Rimgate_BringToStargate,
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

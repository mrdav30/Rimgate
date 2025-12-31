using System;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class JobDriver_DecodeGlyphs : JobDriver
{
    private const int UseDuration = 500;

    // returns true if we actually spawned a new quest
    private bool TryStartStargateQuest()
    {
        // Hard cap: only one SG site quest at a time
        if (Utils.HasActiveQuestOf(RimgateDefOf.Rimgate_StargateQuestScript))
        {
            Messages.Message("RG_MessageSGQuestAlreadyActive".Translate(),
                             MessageTypeDefOf.RejectInput,
                             historical: false);
            return false;
        }

        var slate = new Slate();
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(RimgateDefOf.Rimgate_StargateQuestScript, slate);
        QuestUtility.SendLetterQuestAvailable(quest);
        return true;
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        Toil wait = Toils_General.Wait(UseDuration);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;

        yield return new Toil
        {
            initAction = () =>
            {
                if (!TryStartStargateQuest()) return;

                Thing glyphThing = job.GetTarget(TargetIndex.A).Thing;
                if (glyphThing.stackCount > 1)
                {
                    Thing used = glyphThing.SplitOff(1);
                    if (!used.DestroyedOrNull()) 
                        used.Destroy();
                }
                else 
                    glyphThing.Destroy();
            }
        };
    }
}

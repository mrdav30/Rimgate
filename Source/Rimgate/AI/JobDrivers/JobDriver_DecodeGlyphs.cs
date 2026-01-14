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
    private Thing thing => job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);

        Comp_GlyphParchment comp = thing.TryGetComp<Comp_GlyphParchment>();
        if (comp == null || comp.Props == null)
        {
            EndJobWith(JobCondition.Incompletable);
            yield break;
        }

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        Toil wait = Toils_General.Wait(comp.Props.useDuration);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;

        yield return new Toil
        {
            initAction = () =>
            {
                var glyph = thing;
                var landlocked = comp.PlanetLocked;
                var spacelocked = comp.OrbitLocked;
                if (!TryStartStargateQuest(landlocked, spacelocked))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (glyph.stackCount > 1)
                {
                    Thing used = glyph.SplitOff(1);
                    if (!used.DestroyedOrNull())
                        used.Destroy();
                }
                else
                    glyph.Destroy();
            }
        };
    }

    // returns true if we actually spawned a new quest
    private static bool TryStartStargateQuest(bool landLocked, bool spaceLocked)
    {
        // Hard cap: only one SG site quest at a time
        if (Utils.HasActiveQuestOf(RimgateDefOf.Rimgate_GateQuestScript_Planet))
        {
            Messages.Message("RG_MessageSGQuestAlreadyActive".Translate(),
                             MessageTypeDefOf.RejectInput,
                             historical: false);
            return false;
        }

        var slate = new Slate();
        var def = landLocked
            ? RimgateDefOf.Rimgate_GateQuestScript_Planet
            : spaceLocked
                ? RimgateDefOf.Rimgate_GateQuestScript_Orbit
                : Rand.Element(new[]
                {
                    RimgateDefOf.Rimgate_GateQuestScript_Planet,
                    RimgateDefOf.Rimgate_GateQuestScript_Orbit
                });
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(def, slate);
        QuestUtility.SendLetterQuestAvailable(quest);
        return true;
    }
}

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
        if (StargateUtil.AddressBookFull)
        {
            Messages.Message("RG_CannotDecode".Translate("RG_Cannot_AddressBookFull".Translate()),
                             MessageTypeDefOf.RejectInput,
                             historical: false);
            EndJobWith(JobCondition.Incompletable);
            yield break;
        }

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
                if (!TryStartGateQuest(landlocked, spacelocked))
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
    private bool TryStartGateQuest(bool landLocked, bool spaceLocked)
    {
        var slate = new Slate();
        var def = landLocked && !spaceLocked
            ? RimgateDefOf.Rimgate_GateQuestScript_Planet
            : spaceLocked && !landLocked
                ? RimgateDefOf.Rimgate_GateQuestScript_Orbit
                : Rand.Element(new[]
                {
                    RimgateDefOf.Rimgate_GateQuestScript_Planet,
                    RimgateDefOf.Rimgate_GateQuestScript_Orbit
                });
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(def, slate);

        if(quest.State != QuestState.Ongoing)
        {
            Log.ErrorOnce("Failed to start gate quest from decoding glyphs.", 12345678);
            Messages.Message("RG_CannotDecode_JobFailedMessage".Translate(pawn.Named("PAWN")),
                             MessageTypeDefOf.RejectInput,
                             historical: false);
            return false;
        }

        QuestUtility.SendLetterQuestAvailable(quest);
        return true;
    }
}

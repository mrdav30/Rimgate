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
    private const float SkillLearnFactor = 0.25f;

    private Thing thing => job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        if (GateUtil.AddressBookFull)
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

        var duration = comp.Props.useDuration;
        Toil wait = Toils_General.Wait(duration);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;

        yield return new Toil
        {
            initAction = () =>
            {
                var glyph = thing;
                var landlocked = comp.PlanetLocked;
                var spacelocked = comp.OrbitLocked;

                // Random chance of failure based on pawn skill:
                // - Uses Intellectual skill (research/decoding-adjacent).
                // - Failure chance decreases with higher skill.
                // - Adds small bonus/penalty based on manipulation, with a small random factor.
                if (!RollDecodeSuccess(pawn))
                {
                    Messages.Message("RG_CannotDecode_JobFailedMessage".Translate(pawn.Named("PAWN")),
                                     MessageTypeDefOf.RejectInput,
                                     historical: false);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!TryStartGateQuest(landlocked, spacelocked))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                var accrument = duration * SkillLearnFactor;
                pawn.skills?.Learn(SkillDefOf.Intellectual, accrument);

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

        if (quest.State != QuestState.Ongoing)
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

    private static bool RollDecodeSuccess(Pawn p)
    {
        // If skill tracking is disabled or pawn is missing skills, assume success.
        if (p?.skills == null)
            return true;

        int intellectual = p.skills.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;

        // Base failure chance at skill 0, decreasing linearly to a floor at skill 20.
        // Skill 0  => 35% fail
        // Skill 10 => 17.5% fail
        // Skill 20 => 0% fail (floor)
        float failChance = Mathf.Lerp(0.35f, 0f, intellectual / 20f);

        // Small modifier from manipulation (fine motor / careful work).
        // Typical human manipulation ~1.0. Below 1 increases fail chance, above 1 decreases.
        float manipulation = p.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1f;
        failChance *= Mathf.Lerp(1.25f, 0.85f, Mathf.InverseLerp(0.5f, 1.2f, manipulation));

        // Tiny randomness so similarly-skilled pawns don't feel identical.
        failChance *= Rand.Range(0.9f, 1.1f);

        failChance = Mathf.Clamp01(failChance);
        return Rand.Chance(1f - failChance);
    }
}
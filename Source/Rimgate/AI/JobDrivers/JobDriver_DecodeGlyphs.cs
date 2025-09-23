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
    private const TargetIndex _glyphScrapItem = TargetIndex.A;

    private const int _useDuration = 500;

    private static List<QuestScriptDef> _candidates;

    private void GenerateStargateQuest()
    {
        Slate slate = new Slate();
        _candidates ??= new List<QuestScriptDef>()
        {
            RimgateDefOf.Rimgate_StargateSiteMiscScript,
            RimgateDefOf.Rimgate_StargateSiteGoauldScript,
            RimgateDefOf.Rimgate_StargateSiteTauriScript,
            RimgateDefOf.Rimgate_StargateSiteWraithScript
        };

        QuestScriptDef questDef = _candidates.RandomElement();

        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
        QuestUtility.SendLetterQuestAvailable(quest);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(_glyphScrapItem), job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(_glyphScrapItem, PathEndMode.Touch);

        Toil toil = Toils_General.Wait(_useDuration);
        toil.WithProgressBarToilDelay(_glyphScrapItem);
        yield return toil;
        yield return new Toil
        {
            initAction = () =>
            {
                GenerateStargateQuest();

                Thing glyphThing = job.GetTarget(_glyphScrapItem).Thing;
                if (glyphThing.stackCount > 1)
                {
                    Thing usedGlyphThing = glyphThing.SplitOff(1);
                    if (!usedGlyphThing.DestroyedOrNull()) usedGlyphThing.Destroy();
                }
                else glyphThing.Destroy();
            }
        };
    }
}

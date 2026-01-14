using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace Rimgate;

public class Comp_GlyphParchment : ThingComp
{
    public CompProperties_GlyphParchment Props => (CompProperties_GlyphParchment)props;

    public bool PlanetLocked => (Props?.canDecodePlanet ?? false) && !(Props?.canDecodeOrbit ?? false);

    public bool OrbitLocked => (Props?.canDecodeOrbit ?? false) && !(Props?.canDecodePlanet ?? false);

    public override string TransformLabel(string label)
    {
        if (PlanetLocked)
            return "RG_Glyph_LandLocked".Translate(label);
        if (OrbitLocked)
            return "RG_Glyph_SpaceLocked".Translate(label);

        return label;
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (selPawn == null)
            yield break;

        bool canReach = selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly);
        if (!canReach)
        {
            yield return new FloatMenuOption(
                "CannotReach".Translate(),
                null);
            yield break;
        }

        string researchReason = !ResearchUtil.GlyphDecipheringComplete
            ? "RG_CannotDecode".Translate("RG_CannotDecode_Research".Translate(RimgateDefOf.Rimgate_GlyphDeciphering.label))
            : OrbitLocked && !ResearchUtil.GateModificationComplete
                ? "RG_CannotDecode_Unknown".Translate()
                : null;

        if (!researchReason.NullOrEmpty())
        {
            yield return new FloatMenuOption("RG_CannotDecode".Translate(researchReason), null);
            yield break;
        }

        if (Utils.HasActiveQuestOf(RimgateDefOf.Rimgate_GateQuestScript_Planet))
        {
            yield return new FloatMenuOption("RG_CannotDecode".Translate("RG_CannotDecode_QuestActive".Translate()), null);
            yield break;
        }

        string postfix = PlanetLocked
            ? "RG_Glyph_LandLocked".Translate()
            : OrbitLocked
                ? "RG_Glyph_SpaceLocked".Translate()
                : null;
        var label = "RG_DecodeSGSymbols".Translate();
        if (!postfix.NullOrEmpty())
            label += $" ({postfix})";

        yield return new FloatMenuOption(label, () =>
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DecodeGlyphs, parent);
            job.count = 1;
            job.playerForced = true;
            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        });
    }
}

using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class PawnRenderNode_BodySwapper : PawnRenderNode
{
    public PawnRenderNode_BodySwapper(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
    {
    }

    public override GraphicMeshSet MeshSetFor(Pawn pawn)
    {
        Graphic graphic = GraphicFor(pawn);
        if (graphic != null)
            return MeshPool.GetMeshSetForSize(graphic.drawSize.x, graphic.drawSize.y);

        return null;
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        PawnKindLifeStage curKindLifeStage = pawn.ageTracker?.CurKindLifeStage;
        if (curKindLifeStage == null)
            return null;

        Graphic graphic = null;
        AlternateGraphic ag = null;

        // corpse graphics will always take priority
        bool shouldUseCorpseGraphic = curKindLifeStage.corpseGraphicData != null
            && (pawn.Dead || (pawn.IsMutant && pawn.mutant.Def.useCorpseGraphics));
        if (shouldUseCorpseGraphic)
            graphic = pawn.gender == Gender.Female && curKindLifeStage.femaleCorpseGraphicData != null
                ? curKindLifeStage.femaleCorpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.femaleCorpseGraphicData.Graphic.Shader, graphic.Color, graphic.ColorTwo)
                : curKindLifeStage.corpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.corpseGraphicData.Graphic.Shader, graphic.Color, graphic.ColorTwo);
        else if (pawn.TryGetComp<Comp_BodyGraphicSwapper>(out Comp_BodyGraphicSwapper comp))
            graphic = comp.GetCurrentPawnGraphic(pawn);

        // fallback to vanilla behavior
        if (graphic == null)
            graphic = pawn.TryGetAlternate(out ag, out _)
                ? ag.GetGraphic(curKindLifeStage.bodyGraphicData.Graphic)
                : pawn.gender == Gender.Female && curKindLifeStage.femaleGraphicData != null
                    ? curKindLifeStage.femaleGraphicData.Graphic
                    : curKindLifeStage.bodyGraphicData.Graphic;

        Color baseColor = graphic.Color;
        Color baseColor2 = graphic.ColorTwo;
        switch (pawn.Drawer.renderer.CurRotDrawMode)
        {
            case RotDrawMode.Fresh:
                if (pawn.IsMutant)
                {
                    baseColor = MutantUtility.GetMutantSkinColor(pawn, baseColor);
                    baseColor2 = MutantUtility.GetMutantSkinColor(pawn, baseColor2);
                }

                baseColor = pawn.health.hediffSet.GetSkinColor(baseColor);
                baseColor2 = pawn.health.hediffSet.GetSkinColor(baseColor2);
                return graphic.GetColoredVersion(graphic.Shader, baseColor, baseColor2);
            case RotDrawMode.Rotting:
                {
                    baseColor = PawnRenderUtility.GetRottenColor(pawn.health.hediffSet.GetSkinColor(baseColor));
                    baseColor2 = PawnRenderUtility.GetRottenColor(pawn.health.hediffSet.GetSkinColor(baseColor2));
                    Graphic graphic3 = ag != null
                        ? ag.GetRottingGraphic(graphic)
                        : curKindLifeStage.femaleRottingGraphicData != null && pawn.gender == Gender.Female
                            ? curKindLifeStage.femaleRottingGraphicData.Graphic
                            : curKindLifeStage.rottingGraphicData == null
                                ? graphic
                                : curKindLifeStage.rottingGraphicData.Graphic;
                    Shader newShader = graphic3.Shader;
                    if (graphic3.Shader == ShaderDatabase.CutoutComplex)
                        newShader = ShaderDatabase.Cutout;

                    return graphic3.GetColoredVersion(newShader, baseColor, baseColor2);
                }
            case RotDrawMode.Dessicated:
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                {
                    Graphic graphic2;
                    if (pawn.RaceProps.FleshType != FleshTypeDefOf.Insectoid)
                    {
                        graphic2 = pawn.gender == Gender.Female && curKindLifeStage.femaleDessicatedBodyGraphicData != null
                            ? curKindLifeStage.femaleDessicatedBodyGraphicData.GraphicColoredFor(pawn)
                            : curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(pawn);
                    }
                    else
                    {
                        Color dessicatedColorInsect = PawnRenderUtility.DessicatedColorInsect;
                        graphic2 = pawn.gender == Gender.Female && curKindLifeStage.femaleDessicatedBodyGraphicData != null
                            ? curKindLifeStage.femaleDessicatedBodyGraphicData.Graphic.GetColoredVersion(graphic.Shader, dessicatedColorInsect, dessicatedColorInsect)
                            : curKindLifeStage.dessicatedBodyGraphicData.Graphic.GetColoredVersion(ShaderDatabase.Cutout, dessicatedColorInsect, dessicatedColorInsect);
                    }

                    if (pawn.IsShambler)
                        graphic2.ShadowGraphic = graphic.ShadowGraphic;

                    if (ag != null)
                        graphic2 = ag.GetDessicatedGraphic(graphic2);

                    return graphic2;
                }

                break;
        }

        return null;
    }
}
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VEF.Apparels;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(DynamicPawnRenderNodeSetup_Apparel), "ProcessApparel")]
public static class Harmony_DynamicPawnRenderNodeSetup_Apparel_ProcessApparel
{
    public delegate IEnumerable<ValueTuple<PawnRenderNode, PawnRenderNode>> ProcessApparel(
        Pawn pawn,
        PawnRenderTree tree,
        Apparel ap,
        PawnRenderNode headApparelNode,
        PawnRenderNode bodyApparelNode,
        Dictionary<PawnRenderNode, int> layerOffsets);

    public static readonly ProcessApparel processApparel = AccessTools.MethodDelegate<ProcessApparel>(
        AccessTools.Method(
            typeof(DynamicPawnRenderNodeSetup_Apparel),
            "ProcessApparel"));

    public static IEnumerable<ValueTuple<PawnRenderNode, PawnRenderNode>> Postfix(
        IEnumerable<ValueTuple<PawnRenderNode, PawnRenderNode>> result,
        Pawn pawn,
        PawnRenderTree tree,
        Apparel ap,
        PawnRenderNode headApparelNode,
        PawnRenderNode bodyApparelNode,
        Dictionary<PawnRenderNode, int> layerOffsets)
    {
        if(!pawn.IsPlayerControlled) return result;

        Comp_ApparelWithAttachedHeadgear comp = ThingCompUtility.TryGetComp<Comp_ApparelWithAttachedHeadgear>(ap);
        if (comp == null) return result;

        Apparel item = (Apparel)ThingMaker.MakeThing(comp.Props.attachedHeadgearDef, null);
        ApparelGraphicRecordGetter.TryGetGraphicApparel(
            item,
            pawn.story.bodyType,
            false,
            out ApparelGraphicRecord apparelGraphicRecord);

        if (comp.isHatOn)
        {
            result = result.Concat(
                processApparel(
                    pawn,
                    tree,
                    item,
                    headApparelNode,
                    bodyApparelNode,
                    layerOffsets));
        }
        else if (tree.TryGetNodeByTag(item.def.apparel.parentTagDef, out PawnRenderNode node))
            result = result.Where(x => x.Item1 != node);

        return result;
    }
}

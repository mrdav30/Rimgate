using UnityEngine;
using Verse;

namespace Rimgate;

public class PawnRenderSubWorker_ShiftFrontForHeadType : PawnRenderSubWorker
{

    public override void TransformOffset(
        PawnRenderNode node,
        PawnDrawParms parms,
        ref Vector3 offset,
        ref Vector3 pivot)
    {
        if (node?.hediff?.pawn?.story.headType.narrow ?? false)
        {
            if (node.hediff.pawn.Rotation == Rot4.East)
                offset += new Vector3(-0.05f, 0, 0);
            else if (node.hediff.pawn.Rotation == Rot4.West)
                offset += new Vector3(0.05f, 0, 0);
        }
    }
}

using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class ThinkNode_ConditionalHasGeneResourceNeed : ThinkNode_Conditional
{
    public GeneDef gene;

    protected override bool Satisfied(Pawn pawn)
    {
        Gene geneEssence = pawn.GetActiveGeneOf(gene);
        return geneEssence is Gene_Resource geneResource
            && geneResource.Value < geneResource.targetValue;
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        ThinkNode_ConditionalHasGeneResourceNeed obj = (ThinkNode_ConditionalHasGeneResourceNeed)base.DeepCopy(resolve);
        obj.gene = gene;
        return obj;
    }
}
using RimWorld;
using Verse;

namespace Rimgate;

public class CompUseEffect_AddGene : CompUseEffect
{
    public CompProperties_UseEffectAddGene Props => (CompProperties_UseEffectAddGene)props;

    public override void DoEffect(Pawn user)
    {
        user.AddGene(Props.geneDef, xenogene: true);
    }

    public override AcceptanceReport CanBeUsedBy(Pawn p)
    {
        if (p.HasActiveGeneOf(Props.geneDef))
            return "RG_AlreadyHas".Translate(Props.geneDef.label);

        return true;
    }
}
using RimWorld;
using System.Linq;
using Verse;

namespace Rimgate;

public class CompUseEffect_InstallSymbiote : CompUseEffect
{
    public CompProperties_UseEffectInstallSymbiote Props => (CompProperties_UseEffectInstallSymbiote)props;

    public override void DoEffect(Pawn usedBy)
    {
        base.DoEffect(usedBy);

        var pawn = usedBy;
        if (parent is not Thing_GoualdSymbiote symbiote)
            return;

        if (usedBy.HasSymbiote())
        {
            Messages.Message(
                "RG_RejectHost_HasSymbiote".Translate(pawn.Named("PAWN")),
                pawn,
                MessageTypeDefOf.RejectInput);
            return;
        }

        // Grab Thing memory
        var symbioteHeritage = symbiote.Heritage;
        if (symbioteHeritage == null)
            return;

        var memory = symbioteHeritage.Memory;
        var lineage = symbioteHeritage.QueenLineage;
        if (memory != null)
        {
            if (memory.IsPreviousHost(pawn))
            {
                Messages.Message(
                    "RG_RejectHost_PriorHost".Translate(memory.SymbioteName, pawn.Named("PAWN")),
                    pawn,
                    MessageTypeDefOf.RejectInput);
                return;
            }
        }

        // Install hediff
        var part = pawn.RaceProps.body.GetPartsWithDef(Props.bodyPart).FirstOrDefault();
        if (HediffMaker.MakeHediff(Props.hediffDef, pawn, part) is not Hediff_SymbioteImplant hediff)
            return;

        var heritage = hediff.Heritage;
        if (heritage != null)
        {
            heritage.AssumeMemory(memory);
            heritage.AssumeQueenLineage(lineage);
        }
        pawn.health.AddHediff(hediff);

        // Destroy the item (it’s now an implant)
        symbiote.IgnoreDestroyEvent = true;
        symbiote.Destroy();
    }
}

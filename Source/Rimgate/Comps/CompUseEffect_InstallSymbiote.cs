using RimWorld;
using System.Linq;
using Verse;

namespace Rimgate;

public class CompUseEffect_InstallSymbiote : CompUseEffect
{
    public new CompProperties_UseEffectInstallSymbiote Props => (CompProperties_UseEffectInstallSymbiote)props;

    public override void DoEffect(Pawn usedBy)
    {
        base.DoEffect(usedBy);

        var pawn = usedBy;
        var thing = parent as Thing_GoualdSymbiote;

        if (usedBy.HasSymbiote())
        {
            Messages.Message(
                "RG_RejectHost_HasSymbiote".Translate(pawn.Named("PAWN")),
                pawn,
                MessageTypeDefOf.RejectInput);
            return;
        }

        // Grab Thing memory
        var thingHeritage = thing.Heritage;
        if (thingHeritage == null)
            return;

        var memory = thingHeritage.Memory;
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
        var hediff = HediffMaker.MakeHediff(Props.hediffDef, pawn, part) as Hediff_SymbioteImplant;
        var heritage = hediff.Heritage;
        if (heritage != null)
            hediff.Heritage.AssumeMemory(memory);
        pawn.health.AddHediff(hediff);

        // Destroy the item (it’s now an implant)
        thing.Destroy();
    }
}

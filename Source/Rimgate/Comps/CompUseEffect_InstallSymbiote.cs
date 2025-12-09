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

        bool hasSymbiote = usedBy.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant)
            || usedBy.HasHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch);
        if (hasSymbiote)
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
        else
        {
            memory = new SymbioteMemory();
            memory.EnsureName();
        }

        // Install hediff
        var part = pawn.RaceProps.body.GetPartsWithDef(Props.bodyPart).FirstOrDefault();
        var hediff = HediffMaker.MakeHediff(Props.hediffDef, pawn, part) as Hediff_SymbioteImplant;
        pawn.health.AddHediff(hediff);

        var hediffHeritage = hediff?.Heritage;
        if (hediffHeritage != null)
        {
            // Copy memory into hediff and apply bonuses to host
            hediffHeritage.ApplyMemoryPostEffect(memory, pawn);

            Messages.Message(
                "RG_SymbioteSkillInheritance".Translate(pawn.Named("PAWN"), hediffHeritage?.Memory?.SymbioteName),
                pawn,
                MessageTypeDefOf.PositiveEvent);
        }

        // Destroy the item (it’s now an implant)
        thing.Destroy();
    }
}

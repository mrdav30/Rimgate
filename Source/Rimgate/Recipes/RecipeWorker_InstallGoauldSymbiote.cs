using Rimgate;
using RimWorld;
using Verse;

namespace Rimgate;

public class RecipeWorker_InstallGoauldSymbiote : Recipe_InstallImplant
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if (!base.AvailableOnNow(thing, part))
            return false;

        return CanAccept(thing as Pawn);
    }

    public override bool CompletableEver(Pawn surgeryTarget)
    {
        return CanAccept(surgeryTarget);
    }

    private bool CanAccept(Pawn p)
    {

        if (p.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant))
            return false;

        if (p.HasHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch))
            return false;

        return true;
    }
}

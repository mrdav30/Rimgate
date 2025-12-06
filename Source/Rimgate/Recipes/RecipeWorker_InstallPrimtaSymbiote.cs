using Rimgate;
using RimWorld;
using Verse;

namespace Rimgate;

public class RecipeWorker_InstallPrimtaSymbiote : Recipe_InstallImplant
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
        if (p == null)
            return false;

        if (p.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant))
            return false;

        // Block non-Jaffa (no pouch) from ever receiving a Prim'ta symbiote
        if (!p.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch))
            return false;

        // Too old to safely host a new Prim'ta
        if (p.ageTracker.AgeBiologicalYears >= Hediff_PrimtaInPouch.MaxPrimtaHostAge)
            return false;

        // Only allow new primta if old one matured
        var primta = p.GetHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch) as Hediff_PrimtaInPouch;
        if (primta != null && !primta.Lifecycle.Mature)
            return false;

        return true;
    }
}

using RimWorld;
using Verse;

namespace Rimgate;

public class RitualRole_PrimtaHost : RitualRole
{
    public override bool AppliesToPawn(
        Pawn p,
        out string reason,
        TargetInfo selectedTarget,
        LordJob_Ritual ritual = null,
        RitualRoleAssignments assignments = null,
        Precept_Ritual precept = null,
        bool skipReason = false)
    {
        reason = null;

        // Children disallowed
        if (!AppliesIfChild(p, out reason, skipReason))
            return false;

        // Must be one of our colonists (or a safe player pawn)
        if (!p.Faction.IsPlayerSafe())
        {
            if (!skipReason)
                reason = "MessageRitualRoleMustBeColonist".Translate(LabelCap);
            return false;
        }

        // Must actually qualify as a Jaffa host with prim'ta
        if (!PawnValid(p, out string invalid))
        {
            if (!skipReason)
                reason = invalid;
            return false;
        }

        return true;
    }

    public bool PawnValid(Pawn pawn, out string reason)
    {
        reason = null;
        if (pawn == null || pawn.Dead || pawn.health == null)
        {
            reason = "RG_PrimtaRenewal_NoHost".Translate();
            return false;
        }

        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch))
        {
            reason = "RG_PrimtaRenewal_NoPouch".Translate();
            return false;
        }

        if (pawn.ageTracker.AgeBiologicalYears >= Hediff_PrimtaInPouch.MaxPrimtaHostAge)
        {
            reason = "RG_PrimtaRenewal_TooOld".Translate();
            return false;
        }

        return true;
    }

    public override bool AppliesToRole(
        Precept_Role role,
        out string reason,
        Precept_Ritual ritual = null,
        Pawn p = null,
        bool skipReason = false)
    {
        // Don't constrain interaction with other precept roles (like moral guide).
        reason = null;
        return true;
    }
}

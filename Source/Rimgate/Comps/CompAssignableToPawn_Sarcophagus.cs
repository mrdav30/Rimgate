using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public class CompAssignableToPawn_Sarcophagus : CompAssignableToPawn
{
    public override IEnumerable<Pawn> AssigningCandidates
    {
        get
        {
            if (!parent.Spawned)
                return Enumerable.Empty<Pawn>();

            return parent.Map.mapPawns.FreeColonistsAndPrisonersSpawned.OrderByDescending(delegate (Pawn p)
            {
                if (!CanAssignTo(p).Accepted)
                    return 0;

                return (!IdeoligionForbids(p)) ? 1 : 0;
            }).ThenBy((Pawn p) => p.LabelShort);
        }
    }

    protected override string GetAssignmentGizmoDesc() => "RG_Sarcophagus_CommandSetOwnerDesc".Translate();

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        assignedPawns.RemoveAll((Pawn x) => x == null || x.Dead);

        return base.CompGetGizmosExtra();
    }

    public override AcceptanceReport CanAssignTo(Pawn pawn)
    {
        Building_Sarcophagus sarcophagus = parent as Building_Sarcophagus;

        if (!sarcophagus.AllowSlaves && pawn.IsSlaveOfColony)
            return "RG_Sarcophagus_SlavesNotAllowed".Translate();

        if (!sarcophagus.AllowPrisoners && pawn.IsPrisonerOfColony)
            return "RG_Sarcophagus_PrisonersNotAllowed".Translate();

        if (pawn.BodySize > Building_Sarcophagus.MaxBodySize)
            return "TooLargeForBed".Translate();

        return AcceptanceReport.WasAccepted;
    }

    protected override bool CanSetUninstallAssignedPawn(Pawn pawn)
    {
        if (pawn != null && !AssignedAnything(pawn) && (bool)CanAssignTo(pawn))
        {
            if (!pawn.IsPrisonerOfColony)
                return pawn.IsColonist;

            return true;
        }

        return false;
    }

    public override bool IdeoligionForbids(Pawn pawn)
    {
        if (!ModsConfig.IdeologyActive || base.Props.maxAssignedPawnsCount == 1)
            return base.IdeoligionForbids(pawn);

        return false;
    }
}
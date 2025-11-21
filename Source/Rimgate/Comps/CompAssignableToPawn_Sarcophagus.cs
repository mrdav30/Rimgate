using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;
public class CompAssignableToPawn_Sarcophagus : CompAssignableToPawn
{
    private Building_Sarcophagus BuildingSarcophagus => parent as Building_Sarcophagus;

    public override IEnumerable<Pawn> AssigningCandidates
    {
        get
        {
            if (!parent.Spawned)
                return Enumerable.Empty<Pawn>();

            return parent.Map.mapPawns.FreeColonistsAndPrisonersSpawned
                .OrderByDescending(p =>
                {
                    if (!CanAssignTo(p).Accepted)
                        return 0;

                    return IdeoligionForbids(p) ? 0 : 1;
                })
                .ThenBy(p => p.LabelShort);
        }
    }

    protected override string GetAssignmentGizmoDesc()
        => "RG_Sarcophagus_CommandSetOwnerDesc".Translate();

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        assignedPawns.RemoveAll(x => x == null || x.Dead);
        return base.CompGetGizmosExtra();
    }

    public override AcceptanceReport CanAssignTo(Pawn pawn)
    {
        var sarcophagus = BuildingSarcophagus;

        if (!sarcophagus.AllowSlaves && pawn.IsSlaveOfColony)
            return "RG_Sarcophagus_SlavesNotAllowed".Translate();

        if (!sarcophagus.AllowPrisoners && pawn.IsPrisonerOfColony)
            return "RG_Sarcophagus_PrisonersNotAllowed".Translate();

        if (pawn.BodySize > Building_Sarcophagus.MaxBodySize)
            return "TooLargeForBed".Translate();

        return AcceptanceReport.WasAccepted;
    }

    /// <summary>
    /// True if this pawn is assigned to ANY sarcophagus in the game.
    /// </summary>
    public override bool AssignedAnything(Pawn pawn)
    {
        return AllSarcophagusComps()
            .Any(c => c.assignedPawns.Contains(pawn));
    }

    public override void TryAssignPawn(Pawn pawn)
    {
        // Remove pawn from any other sarcophagus first
        foreach (var other in AllSarcophagusComps())
        {
            if (other == this)
                continue;

            if (other.assignedPawns.Contains(pawn))
                other.ForceRemovePawn(pawn);
        }

        base.TryAssignPawn(pawn);
    }

    protected override bool CanSetUninstallAssignedPawn(Pawn pawn)
    {
        return pawn != null && !AssignedAnything(pawn) && CanAssignTo(pawn) == true && pawn.IsColonist;
    }

    public override bool IdeoligionForbids(Pawn pawn)
    {
        if (!ModsConfig.IdeologyActive || base.Props.maxAssignedPawnsCount == 1)
            return base.IdeoligionForbids(pawn);

        return false;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        OnPostExposeData();
    }

    private void OnPostExposeData()
    {
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            // Clean up any weird assignments (e.g. from removed mods or bad saves)
            int invalid = assignedPawns.RemoveAll(p =>
                p == null
                || p.Dead
                || p.Destroyed);

            if (invalid != 0)
                Log.Warning($"{parent.ToStringSafe()} had invalid assigned pawns. Removing.");
        }
    }

    private static IEnumerable<CompAssignableToPawn_Sarcophagus> AllSarcophagusComps()
    {
        foreach (var map in Find.Maps)
        {
            var buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] is Building_Sarcophagus sarc)
                {
                    var comp = sarc.Assignable;
                    if (comp != null)
                        yield return comp;
                }
            }
        }
    }
}

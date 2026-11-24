using Verse;

namespace Rimgate;

public static class SymbioteLineageUtility
{
    public static SymbioteQueenLineage GetLineage(Thing thing)
    {
        return thing switch
        {
            Thing_SymbioteQueen queen => queen.Lineage,
            Thing_PrimtaSymbiote primta => primta.Lineage,
            Thing_GoualdSymbiote symbiote => symbiote.Heritage?.QueenLineage,
            _ => null
        };
    }

    public static void AssumeLineage(Thing thing, SymbioteQueenLineage lineage)
    {
        if (thing == null || lineage == null)
            return;

        switch (thing)
        {
            case Thing_SymbioteQueen queen:
                queen.AssumeQueenLineage(lineage);
                break;
            case Thing_PrimtaSymbiote primta:
                primta.AssumeQueenLineage(lineage);
                break;
            case Thing_GoualdSymbiote symbiote:
                symbiote.Heritage?.AssumeQueenLineage(lineage);
                break;
        }
    }

    public static SymbioteQueenLineage GetLineage(Hediff hediff)
    {
        return hediff switch
        {
            Hediff_PrimtaInPouch primta => primta.QueenLineage,
            Hediff_SymbioteImplant symbiote => symbiote.Heritage?.QueenLineage,
            _ => null
        };
    }

    public static void AssumeLineage(Hediff hediff, SymbioteQueenLineage lineage)
    {
        if (hediff == null || lineage == null)
            return;

        switch (hediff)
        {
            case Hediff_PrimtaInPouch primta:
                primta.AssumeQueenLineage(lineage);
                break;
            case Hediff_SymbioteImplant symbiote:
                symbiote.Heritage?.AssumeQueenLineage(lineage);
                break;
        }
    }
}

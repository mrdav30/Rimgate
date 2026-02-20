using Verse;

namespace Rimgate;

public static class AlphaGenesCompatibility
{
    // __0 refers to ___pawn in original Alpha Genes code
    public static bool SkipIfPawnIsOnSarcophagus(Pawn __0)
    {
        // Skip if the pawn is lying in a Sarcophagus
        if (__0.ParentHolder is Building_Sarcophagus)
            return false;

        // Otherwise continue to break bones upon being downed
        return true;
    }
}
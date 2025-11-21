using RimWorld;
using Verse;

namespace Rimgate;

public static class ResearchUtil
{
    public static bool GlyphDecipheringComplete => RimgateDefOf.Rimgate_GlyphDeciphering.IsFinished;

    public static bool ParallelSubspaceCouplingComplete => RimgateDefOf.Rimgate_ParallelSubspaceCoupling.IsFinished;

    public static bool SarcophagusBioregenerationComplete => RimgateDefOf.Rimgate_SarcophagusBioregeneration.IsFinished;

    public static bool SarcophagusOptimizationComplete => RimgateDefOf.Rimgate_SarcophagusOptimization.IsFinished;

    public static bool WraithCloneGenomeComplete => RimgateDefOf.Rimgate_WraithCloneGenome.IsFinished;

    public static bool WraithCloneFullComplete => RimgateDefOf.Rimgate_WraithCloneFull.IsFinished;

    public static bool WraithCloneEnhancementComplete => RimgateDefOf.Rimgate_WraithCloneEnhancement.IsFinished;

    public static bool WraithCloneCorpseComplete => RimgateDefOf.Rimgate_WraithCloneCorpse.IsFinished;

    public static bool WraithModificationEquipmentComplete => RimgateDefOf.Rimgate_WraithModificationEquipment.IsFinished;

    public static bool ZPMIntegrationComplete => RimgateDefOf.Rimgate_ZPMIntegration.IsFinished;
}

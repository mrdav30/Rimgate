using RimWorld;
using Verse;

namespace Rimgate;

public class CompAbilityRequiresTrainable : AbilityComp
{
    public CompProperties_AbilityRequiresTrainable Props => (CompProperties_AbilityRequiresTrainable)props;

    private Pawn Pawn => parent?.pawn;

    private bool IsPlayerControlled =>
        Pawn?.Faction != null && Pawn.Faction == Faction.OfPlayer;

    private bool HasRequiredTraining()
    {
        var pawn = Pawn;
        var trainable = Props.trainableDef;
        if (pawn == null || trainable == null)
            return true; // no pawn/def => don't brick the ability

        // Only meaningful for animals
        if (pawn.training == null)
            return true;

        bool learned = pawn.training.HasLearned(trainable);

        if (!Props.requireCompleted)
            return learned || pawn.training.CanBeTrained(trainable);

        return learned;
    }

    private bool AllowAIWithoutTraining()
    {
        if (!Props.aiCanCastWithoutTrainable)
            return false;

        var pawn = Pawn;
        if (pawn == null)
            return false;

        // Not player controlled -> allow
        return pawn.Faction == null || pawn.Faction != Faction.OfPlayer;
    }

    public override bool GizmoDisabled(out string reason)
    {
        // Keep the default disable reasons (cooldown, downed, etc.)
        if (base.GizmoDisabled(out reason))
            return true;

        if (HasRequiredTraining() || AllowAIWithoutTraining())
            return false;

        var trainable = Props.trainableDef;
        reason = trainable != null
            ? $"Requires training: {trainable.LabelCap}"
            : "Requires training";
        return true;
    }

    public override bool CanCast
    {
        get
        {
            if (!base.CanCast)
                return false;

            return HasRequiredTraining() || AllowAIWithoutTraining();
        }
    }
}

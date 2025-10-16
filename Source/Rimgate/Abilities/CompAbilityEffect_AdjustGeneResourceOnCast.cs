using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompAbilityEffect_AdjustGeneResourceOnCast : CompAbilityEffect
{
    public new CompProperties_AdjustGeneResourceOnCast Props
        => (CompProperties_AdjustGeneResourceOnCast)props;

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (!base.Valid(target, throwMessages)) return false;
        // Optional: require at least one side to have the resource
        var caster = parent.pawn;
        var targetPawn = target.Pawn;
        bool casterHas = caster?.GetActiveGene(Props.geneDef) != null;
        bool targetHas = targetPawn?.GetActiveGene(Props.geneDef) != null;
        return casterHas || targetHas;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        if (Props.geneDef == null || Props.amount <= 0f) return;

        Pawn caster = parent.pawn;
        Pawn targetPawn = target.Pawn;

        // Optional: require the target to have actually been harmed very recently
        if (Props.onlyIfTargetHarmed
            && targetPawn != null
            && !targetPawn.Dead)
        {
            if (!targetPawn.health.hediffSet.hediffs.Any(h => h.Visible
                && h.ageTicks < 30)) return;
        }

        // Determine who gets the gain and who (optionally) gets drained.
        Pawn gainer = Props.affects == "Target"
            ? targetPawn
            : caster;
        Pawn drainer = Props.affects == "Target"
            ? caster
            : targetPawn;

        // Apply gain to gainer if they have the resource gene.
        var gainerGene = gainer?.GetActiveGene(Props.geneDef) as Gene_Resource;
        var drainerGene = drainer?.GetActiveGene(Props.geneDef) as Gene_Resource;

        // Nothing to do if gainer lacks the gene.
        if (gainerGene == null) return;

        // How much could the gainer actually accept?
        float want = Mathf.Clamp01(Props.amount);
        float canAccept = Mathf.Max(0f, (gainerGene.Max - gainerGene.Value - gainerGene.MaxLevelOffset));
        float gain = Mathf.Min(want, canAccept);

        // If the other pawn has the same resource,
        // drain up to the intended gain.
        if (drainerGene != null && gain > 0f)
        {
            float available = Mathf.Max(0f, drainerGene.Value);
            float drain = Mathf.Min(gain, available);

            // If there is no resource to drain,
            // return if free draw is disallowed
            if (drain > 0)
            {
                drainerGene.Value = Mathf.Max(0f, drainerGene.Value - drain);
                // match the gainer’s increase to what we actually drained
                gain = drain;
            }
            else if (!Props.allowFreeDraw) return;
        }

        if (gain > 0f)
            gainerGene.Value = Mathf.Min(gainerGene.Max - gainerGene.MaxLevelOffset, gainerGene.Value + gain);
    }
}
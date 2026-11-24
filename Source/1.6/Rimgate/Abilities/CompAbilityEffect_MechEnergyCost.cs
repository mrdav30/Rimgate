using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompAbilityEffect_MechEnergyCost : CompAbilityEffect
{
    public new CompProperties_AbilityMechEnergyCost Props => (CompProperties_AbilityMechEnergyCost)props;

    public float MechEnergyCost => Mathf.Clamp(Props.mechEnergyCostPct, 0, 1) * 100f;

    private bool HasEnoughMechEnergy
    {
        get
        {
            // Only player pawns have mech energy, so if the pawn isn't a player faction member, we can assume they have enough mech energy to cast
            if (!parent.pawn.Faction.IsOfPlayerFaction()) return true;

            Need_MechEnergy need = parent.pawn.needs?.TryGetNeed<Need_MechEnergy>();
            if (need == null || need.CurLevel < MechEnergyCost)
                return false;

            return true;
        }
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (Props.payCostAtStart)
            OffsetMechEnergy(parent.pawn, MechEnergyCost);
    }

    public override bool GizmoDisabled(out string reason)
    {
        Need_MechEnergy need = parent.pawn.needs?.TryGetNeed<Need_MechEnergy>();
        if (need == null)
        {
            reason = "RG_AbilityDisabledNoMechEnergyNeed".Translate(parent.pawn);
            return true;
        }

        float cost = MechEnergyCost;
        if (need.CurLevel < cost)
        {
            reason = "RG_AbilityDisabledNoMechEnergy".Translate(parent.pawn);
            return true;
        }

        float num = TotalMechEnergyCostOfQueuedAbilities();
        float num2 = cost + num;
        if (cost > float.Epsilon && num2 > need.CurLevel)
        {
            reason = "RG_AbilityDisabledNoMechEnergy".Translate(parent.pawn);
            return true;
        }

        reason = null;
        return false;
    }

    public override bool AICanTargetNow(LocalTargetInfo target) => HasEnoughMechEnergy;

    private float TotalMechEnergyCostOfQueuedAbilities()
    {
        float num = !(parent.pawn.jobs?.curJob?.verbToUse is Verb_CastAbility verb_CastAbility)
            ? 0f
            : GetMechEnergyCostForAbility(verb_CastAbility.ability);
        if (parent.pawn.jobs == null)
            return num;

        for (int i = 0; i < parent.pawn.jobs.jobQueue.Count; i++)
        {
            if (parent.pawn.jobs.jobQueue[i].job.verbToUse is Verb_CastAbility verb_CastAbility2)
                num += GetMechEnergyCostForAbility(verb_CastAbility2.ability);
        }

        return num;
    }

    private static float GetMechEnergyCostForAbility(Ability ability)
    {
        if (ability?.comps == null)
            return 0f;

        float num = 0f;
        for (int i = 0; i < ability.comps.Count; i++)
        {
            if (ability.comps[i] is CompAbilityEffect_MechEnergyCost comp)
                num += comp.MechEnergyCost;
        }

        return num;
    }

    private static void OffsetMechEnergy(Pawn pawn, float amount)
    {
        Need_MechEnergy need = pawn.needs?.TryGetNeed<Need_MechEnergy>();
        if (need != null)
            need.CurLevel -= amount;
    }
}
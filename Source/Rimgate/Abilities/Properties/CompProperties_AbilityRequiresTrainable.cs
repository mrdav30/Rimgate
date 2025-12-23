using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_AbilityRequiresTrainable : AbilityCompProperties
{
    public TrainableDef trainableDef;

    // If true, enemy/AI-controlled can still cast even if not trained.
    public bool aiCanCastWithoutTrainable = false;

    // If true, require the trainable to be fully completed (all steps),
    // otherwise "learned at all" is enough.
    public bool requireCompleted = true;

    public CompProperties_AbilityRequiresTrainable()
    {
        compClass = typeof(CompAbilityRequiresTrainable);
    }
}

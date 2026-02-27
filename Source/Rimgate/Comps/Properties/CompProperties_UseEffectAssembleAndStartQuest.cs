using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class CompProperties_UseEffectAssembleAndStartQuest : CompProperties
{
    public int requiredCount = 3;

    public ResearchProjectDef requiredProjectDef;

    // Legacy setting kept for XML compatibility with older patches; no longer used.
    public bool checkCaravans;

    public float nearbySearchRadius = 5.9f;

    public QuestScriptDef questScript;

    public string letterLabel;

    public string letterText;

    public CompProperties_UseEffectAssembleAndStartQuest()
    {
        compClass = typeof(CompUseEffect_AssembleAndStartQuest);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parent)
    {
        foreach (var e in base.ConfigErrors(parent)) yield return e;
        if (questScript == null) yield return "[CompProperties_AssembleAndStartQuest] questScript is null.";
        if (requiredCount <= 0) yield return "[CompProperties_AssembleAndStartQuest] requiredCount must be > 0.";
        if (nearbySearchRadius < 0f) yield return "[CompProperties_AssembleAndStartQuest] nearbySearchRadius must be >= 0.";
    }
}

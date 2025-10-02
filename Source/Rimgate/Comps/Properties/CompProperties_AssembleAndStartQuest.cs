using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class CompProperties_AssembleAndStartQuest : CompProperties
{
    public int requiredCount = 3;

    public ResearchProjectDef requiredProjectDef;

    public bool checkCaravans;

    public QuestScriptDef questScript;

    public string letterLabel;

    public string letterText;

    public CompProperties_AssembleAndStartQuest()
    {
        compClass = typeof(CompUseEffect_AssembleAndStartQuest);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parent)
    {
        foreach (var e in base.ConfigErrors(parent)) yield return e;
        if (questScript == null) yield return "[CompProperties_AssembleAndStartQuest] questScript is null.";
    }
}

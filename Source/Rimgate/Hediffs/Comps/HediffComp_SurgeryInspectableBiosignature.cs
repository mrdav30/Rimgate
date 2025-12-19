using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public class HediffComp_SurgeryInspectableBiosignature : HediffComp_SurgeryInspectable
{
    private List<ResearchProjectDef> _researchUnlocked;

    public new HediffCompProperties_SurgeryInspectionBiosignature Props => (HediffCompProperties_SurgeryInspectionBiosignature)props;

    public List<ResearchProjectDef> ResearchUnlocked
    {
        get
        {
            if (_researchUnlocked == null)
            {
                var thingDef = Props.biosignatureThingDef;
                _researchUnlocked = new List<ResearchProjectDef>();
                foreach (ResearchProjectDef allDef in DefDatabase<ResearchProjectDef>.AllDefs)
                {
                    if (!allDef.requiredAnalyzed.NullOrEmpty() 
                        && allDef.requiredAnalyzed.Contains(thingDef))
                    {
                        _researchUnlocked.Add(allDef);
                    }
                }
            }

            return _researchUnlocked;
        }
    }

    public NamedArgument? ExtraNamedArg => ResearchUnlocked.Select((ResearchProjectDef r) => r.label).ToCommaList(useAnd: true).Named("RESEARCH");

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        var def = Props.biosignatureThingDef;
        var analyzable = def?.GetCompProperties<CompProperties_CompAnalyzableUnlockResearch>();

        var biosignature = analyzable?.analysisID ?? 0;
        if (analyzable == null || biosignature == 0) return;

        if (!Find.AnalysisManager.HasAnalysisWithID(biosignature))
        {
            Find.AnalysisManager.AddAnalysisTask(biosignature, analyzable.analysisRequiredRange.RandomInRange);
        }
    }

    public override SurgicalInspectionOutcome DoSurgicalInspection(Pawn surgeon)
    {
        var def = Props.biosignatureThingDef;
        var analyzable = def?.GetCompProperties<CompProperties_CompAnalyzableUnlockResearch>();

        var biosignature = analyzable?.analysisID ?? 0;
        if (analyzable == null || biosignature == 0)
            return SurgicalInspectionOutcome.Nothing;

        if (!Find.AnalysisManager.TryIncrementAnalysisProgress(biosignature, out var details)
            && details?.Satisfied == true)
        {
            SendLetter(analyzable.repeatCompletedLetterLabel, analyzable.repeatCompletedLetter, analyzable.repeatCompletedLetterDef, Pawn);
            return SurgicalInspectionOutcome.Nothing;
        }
        else
        {
            string label = string.Empty;
            string desc = string.Empty;
            LetterDef letterDef = null;

            if (details.Satisfied)
            {
                label = analyzable.completedLetterLabel;
                desc = analyzable.completedLetter;
                letterDef = analyzable.completedLetterDef;
            }
            else
            {
                label = analyzable.progressedLetterLabel;
                if (analyzable.showProgress)
                    label += $" {details.timesDone}/{details.required}";
                desc = ((details.timesDone <= analyzable.progressedLetters.Count)
                    ? analyzable.progressedLetters[details.timesDone - 1]
                    : analyzable.progressedLetters.Last());
                letterDef = analyzable.progressedLetterDef;
            }

            SendLetter(label, desc, letterDef, Pawn);
        }

        if (analyzable.destroyedOnAnalyzed)
            Pawn.RemoveHediff(parent);

        return SurgicalInspectionOutcome.DetectedNoLetter;
    }

    private void SendLetter(
        string label,
        string letter,
        LetterDef def,
        Pawn pawn)
    {
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(letter)) return;
        string formattedLetterString = GetFormattedLetterString(label, pawn);
        string formattedLetterString2 = GetFormattedLetterString(letter, pawn);
        Find.LetterStack.ReceiveLetter(formattedLetterString, formattedLetterString2, def);
    }

    private string GetFormattedLetterString(string text, Pawn pawn)
    {
        if (ExtraNamedArg.HasValue)
            return text.Formatted(pawn.Named("PAWN"), ExtraNamedArg.Value).Resolve();

        return text.Formatted(pawn.Named("PAWN")).Resolve();
    }
}

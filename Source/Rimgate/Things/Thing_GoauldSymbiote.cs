using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Thing_GoualdSymbiote : ThingWithComps
{
    public Comp_SymbioteHeritage Heritage => _heritage ??= GetComp<Comp_SymbioteHeritage>();

    public bool IsBlankSymbiote => Heritage == null;

    private Comp_SymbioteHeritage _heritage;

    private string SymbioteLimitSuffix
    {
        get
        {
            if (IsBlankSymbiote) return null;
            // If the symbiote is already over limit, indicate it loudly.
            // Otherwise, if it is at the limit, indicate it's at max.
            var memory = Heritage.Memory;
            if (memory.IsOverLimit) return " (exhausted)";
            if (memory.IsAtLimit) return " (prime)";
            return null;
        }
    }

    public string SymbioteLabel
    {
        get
        {
            if (!_cachedSymbioteLabel.NullOrEmpty() || IsBlankSymbiote || Heritage.Memory == null)
                return _cachedSymbioteLabel;

            var name = Heritage.Memory.SymbioteName;
            var baseLabel = "RG_SymbioteMemory_Name".Translate(name);
            var suffix = SymbioteLimitSuffix;

            _cachedSymbioteLabel = suffix.NullOrEmpty()
                ? baseLabel
                : $"{baseLabel}{suffix}";

            return _cachedSymbioteLabel;
        }
    }

    public override string LabelNoCount => !IsBlankSymbiote && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelNoCount;

    public override string LabelNoParenthesis => !IsBlankSymbiote && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelNoParenthesis;

    private string _cachedSymbioteLabel;

    public override string GetCustomLabelNoCount(bool includeHp = true) => !IsBlankSymbiote && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.GetCustomLabelNoCount(includeHp);

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (var stat in base.SpecialDisplayStats())
            yield return stat;

        SymbioteQueenLineage lineage = Heritage?.QueenLineage;
        if (lineage?.HasQueenName == true)
            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsImportant,
                "RG_Symbiote_Stat_MotherQueen_Label".Translate(),
                lineage.QueenName,
                "RG_Symbiote_Stat_MotherQueen_Desc".Translate(),
                4993);

        if (lineage?.HasOffsets == true)
        {
            string offsetText = lineage.OffsetsDisplayString();
            if (!offsetText.NullOrEmpty())
                yield return new StatDrawEntry(
                    StatCategoryDefOf.BasicsImportant,
                    "RG_Symbiote_Stat_InheritedOffsets_Label".Translate(),
                    offsetText,
                    "RG_Symbiote_Stat_InheritedOffsets_Desc".Translate(),
                    4992);
        }

        if (!IsBlankSymbiote && Heritage?.Memory != null)
            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsImportant,
                "RG_Symbiote_Stat_PreviousHostCount_Label".Translate(),
                $"{Heritage.Memory.PriorHostCount} / {SymbioteMemory.MaxPreviousHosts}".ToString(),
                "RG_Symbiote_Stat_PreviousHostCount_Desc".Translate(),
                4991);
    }
}

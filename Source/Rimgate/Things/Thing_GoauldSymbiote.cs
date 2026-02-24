using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Thing_GoualdSymbiote : ThingWithComps
{
    public Comp_SymbioteHeritage Heritage => _heritage ??= GetComp<Comp_SymbioteHeritage>();

    private Comp_SymbioteHeritage _heritage;

    private string SymbioteLimitSuffix
    {
        get
        {
            if (Heritage == null) return null;
            // If the symbiote is already over limit, indicate it loudly.
            // Otherwise, if it is at the limit, indicate it's at max.
            var memory = Heritage.Memory;
            if (memory.IsOverLimit) return "RG_SymbioteMemory_OverLimit_LabelSuffix".Translate();
            if (memory.IsAtLimit) return "RG_SymbioteMemory_AtLimit_LabelSuffix".Translate();
            return null;
        }
    }

    public string SymbioteLabel
    {
        get
        {
            if (!_cachedSymbioteLabel.NullOrEmpty() || Heritage == null || Heritage.Memory == null)
                return _cachedSymbioteLabel;

            var name = Heritage.Memory.SymbioteName;
            var baseLabel = "RG_SymbioteMemory_Name".Translate(name);
            var suffix = SymbioteLimitSuffix;

            _cachedSymbioteLabel = suffix.NullOrEmpty()
                ? baseLabel
                : $"{baseLabel} {suffix}";

            return _cachedSymbioteLabel;
        }
    }

    public override string LabelNoCount => Heritage != null && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelNoCount;

    public override string LabelNoParenthesis => Heritage != null && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelNoParenthesis;

    private string _cachedSymbioteLabel;

    public override string GetCustomLabelNoCount(bool includeHp = true) => Heritage != null && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.GetCustomLabelNoCount(includeHp);
}

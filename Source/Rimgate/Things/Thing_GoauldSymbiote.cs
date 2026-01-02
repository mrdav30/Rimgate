using Verse;

namespace Rimgate;

public class Thing_GoualdSymbiote : ThingWithComps
{
    public Comp_SymbioteHeritage Heritage => _heritage ??= GetComp<Comp_SymbioteHeritage>();

    public Comp_SymbioteHeritage _heritage;

    private string SymbioteLimitSuffix
    {
        get
        {
            var memory = Heritage?.Memory;
            if (memory == null) return null;

            // If the symbiote is already over limit, indicate it loudly.
            // Otherwise, if it is at the limit, indicate it's at max.
            if (memory.IsOverLimit) return " (exhausted)";
            if (memory.IsAtLimit) return " (prime)";
            return null;
        }
    }

    public string SymbioteLabel
    {
        get
        {
            var name = Heritage?.Memory?.SymbioteName;
            if (name.NullOrEmpty()) return null;

            var baseLabel = "RG_SymbioteMemory_Name".Translate(name);
            var suffix = SymbioteLimitSuffix;

            return suffix.NullOrEmpty() ? baseLabel : $"{baseLabel}{suffix}";
        }
    }

    public override string LabelNoCount => !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelNoCount;

    public override string LabelNoParenthesis => !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelNoParenthesis;

    public override string GetCustomLabelNoCount(bool includeHp = true) => !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.GetCustomLabelNoCount(includeHp);
}

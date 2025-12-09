using Verse;

namespace Rimgate;

public class Thing_GoualdSymbiote : ThingWithComps
{
    public Comp_SymbioteHeritage Heritage => _heritage ??= GetComp<Comp_SymbioteHeritage>();

    public Comp_SymbioteHeritage _heritage;

    public string SymbioteLabel => Heritage?.Memory?.SymbioteName.NullOrEmpty() ?? true
            ? null
            : "RG_SymbioteMemory_Name".Translate(Heritage.Memory.SymbioteName);

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

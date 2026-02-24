using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Thing_SymbioteQueen : ThingWithComps
{
    public string SymbioteLabel
    {
        get
        {
            if (Lineage?.HasQueenName == false)
                return null;

            return "RG_SymbioteQueen_Label".Translate(Lineage?.QueenName);
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

    public SymbioteQueenLineage Lineage = new();

    public void EnsureLineageInitialized()
    {
        Lineage ??= new SymbioteQueenLineage();
        Lineage.EnsureInitialized();
    }

    public void AssumeQueenLineage(SymbioteQueenLineage lineage)
    {
        Lineage = SymbioteQueenLineage.DeepCopy(lineage);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref Lineage, "Lineage");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            Lineage ??= new SymbioteQueenLineage();
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (StatDrawEntry stat in base.SpecialDisplayStats())
            yield return stat;

        if (Lineage?.HasOffsets == true)
        {
            foreach (var stat in Lineage.GetStatDrawEntries(showQueenName: false))
                yield return stat;
        }
    }
}

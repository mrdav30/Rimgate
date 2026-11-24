using RimWorld;
using Verse;

namespace Rimgate;

public class SymbioteStatOffset : IExposable
{
    public string statDefName;
    public float offset;

    public SymbioteStatOffset() { }

    public SymbioteStatOffset(StatDef stat, float value)
    {
        statDefName = stat?.defName;
        offset = value;
    }

    public StatDef Stat => statDefName.NullOrEmpty()
        ? null
        : DefDatabase<StatDef>.GetNamedSilentFail(statDefName);

    public bool Matches(StatDef stat) => stat != null && stat.defName == statDefName;

    public StatDrawEntry GetStatDrawEntry()
    {
        StatDef stat = Stat;
        string label = stat?.LabelCap ?? statDefName ?? "Unknown";
        
        float pct = offset * 100f;
        string sign = pct >= 0f ? "+" : string.Empty;
        string value = $"{sign}{pct:0.#}%";

        return new StatDrawEntry(RimgateDefOf.RG_QueenLineage, label, value, stat?.description ?? string.Empty, 4670);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref statDefName, "statDefName");
        Scribe_Values.Look(ref offset, "offset", 0f);
    }

    public SymbioteStatOffset DeepCopy()
    {
        return new SymbioteStatOffset
        {
            statDefName = statDefName,
            offset = offset
        };
    }
}

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

    public string FormatForDisplay()
    {
        StatDef stat = Stat;
        string statLabel = stat?.LabelCap ?? statDefName ?? "Unknown";
        float pct = offset * 100f;
        string sign = pct >= 0f ? "+" : string.Empty;
        return $"{statLabel}: {sign}{pct:0.#}%";
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

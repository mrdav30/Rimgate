using Verse;

namespace Rimgate;

public class Hediff_Clone : HediffWithComps
{
    public int CloneGeneration = 1;

    public override bool Visible => true;

    public override string LabelBase => $"Generation {CloneGeneration} clone";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<int>(ref CloneGeneration, "CloneGeneration", 1, false);
    }
}

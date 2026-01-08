using Verse;

namespace Rimgate;

public class Hediff_ClonedTracker : HediffWithComps
{
    public int TimesCloned;

    public override bool Visible => false;
    public override string LabelBase => $"Cloned {TimesCloned} time(s)";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<int>(ref TimesCloned, "TimesCloned", 0, false);
    }
}

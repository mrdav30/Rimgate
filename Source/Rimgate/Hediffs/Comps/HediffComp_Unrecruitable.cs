using RimWorld;
using Verse;

namespace Rimgate;

/// <summary>
/// While present, this pawn cannot be recruited.
/// </summary>
public class HediffComp_Unrecruitable : HediffComp
{
    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        Enforce();
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (parent.pawn.IsHashIntervalTick(500)) // ~8.2s
            Enforce();
    }

    private void Enforce()
    {
        var pawn = parent.pawn;
        var guest = pawn?.guest;
        if (guest == null) return;
        guest.Recruitable = false;
    }
}
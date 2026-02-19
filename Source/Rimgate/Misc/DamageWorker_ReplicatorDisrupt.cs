using Verse;

namespace Rimgate;

public class DamageWorker_ReplicatorDisrupt : DamageWorker_AddInjury
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        // Only stuns and damages mechanoids
        if (victim == null || victim is not Pawn p || !p.RaceProps.IsMechanoid) return new DamageResult();


        DamageResult damageResult = base.Apply(dinfo, victim);
        damageResult.stunned = true;
        return damageResult;
    }
}
using RimWorld;
using Verse;

namespace Rimgate;

public class DamageWorker_ApplyHediffToBodyPartTag_Ext : DefModExtension
{
    public BodyPartTagDef bodypartTagTarget;

    public HediffDef hediffToApply;

    public bool shouldStun;
}

public class DamageWorker_ApplyHediffToBodyPartTag : DamageWorker
{
    private DamageWorker_ApplyHediffToBodyPartTag_Ext Ext => def.GetModExtension<DamageWorker_ApplyHediffToBodyPartTag_Ext>();

    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        DamageResult result = base.Apply(dinfo, victim);
        result.stunned = Ext.shouldStun;

        if(victim is Pawn pawn)
        {
            foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, Ext.bodypartTagTarget))
            {
                result.AddHediff(pawn.health.AddHediff(Ext.hediffToApply, notMissingPart));
                result.AddPart(pawn, notMissingPart);
                result.wounded = true;
            }
        }

        return result;
    }
}
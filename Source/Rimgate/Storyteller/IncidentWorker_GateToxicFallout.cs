using RimWorld;
using Verse;
using System.Linq;

namespace Rimgate;

public class IncidentWorker_GateToxicFallout : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;
        var map = parms.target as Map;
        if (map == null) return false;
        return map.listerThings.ThingsOfDef(RimgateDefOf.Rimgate_Dwarfgate)
                   .OfType<Building_Gate>()
                   .Any();
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        var map = (Map)parms.target;
        // ~1–3 days
        int ticks = GenDate.TicksPerDay * Rand.RangeInclusive(1, 3);
        var cond = (GameCondition_GateToxicFallout)GameConditionMaker
            .MakeCondition(RimgateDefOf.Rimgate_GateToxicFallout, ticks);
        map.gameConditionManager.RegisterCondition(cond);

        SendStandardLetter(cond.LabelCap, cond.LetterText, cond.def.letterDef, parms, LookTargets.Invalid);

        return true;
    }
}
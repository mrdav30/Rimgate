using RimWorld;
using Verse;
using System.Linq;

namespace Rimgate;

public class IncidentWorker_GateToxicFallout : IncidentWorker_Gate
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        var map = (Map)parms.target;
        // ~1–3 days
        int ticks = GenDate.TicksPerDay * Rand.RangeInclusive(1, 3);
        var cond = (GameCondition_GateToxicFallout)GameConditionMaker
            .MakeCondition(RimgateDefOf.Rimgate_GateToxicFallout, ticks);
        map.gameConditionManager.RegisterCondition(cond);

        SendIncidentLetter(cond.LabelCap, cond.LetterText, cond.def.letterDef, parms, LookTargets.Invalid, def);

        return true;
    }
}
using RimWorld;
using Verse;
using System.Linq;

namespace Rimgate;

public class IncidentWorker_GateHeatWave : IncidentWorker_Gate
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        var map = (Map)parms.target;
        // ~1–2 days
        int ticks = GenDate.TicksPerDay * Rand.RangeInclusive(1, 2);
        var cond = (GameCondition_GateHeatWave)GameConditionMaker
            .MakeCondition(RimgateDefOf.Rimgate_GateHeatWave, ticks);
        map.gameConditionManager.RegisterCondition(cond);

        SendIncidentLetter(cond.LabelCap, cond.LetterText, cond.def.letterDef, parms, LookTargets.Invalid, def);

        return true;
    }
}
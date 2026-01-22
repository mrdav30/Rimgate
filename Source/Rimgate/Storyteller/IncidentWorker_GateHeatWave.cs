using RimWorld;
using Verse;
using System.Linq;

namespace Rimgate;

public class IncidentWorker_GateHeatWave : IncidentWorker
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
        // ~1–2 days
        int ticks = GenDate.TicksPerDay * Rand.RangeInclusive(1, 2);
        var cond = (GameCondition_GateHeatWave)GameConditionMaker
            .MakeCondition(RimgateDefOf.Rimgate_GateHeatWave, ticks);
        map.gameConditionManager.RegisterCondition(cond);

        SendStandardLetter(cond.LabelCap, cond.LetterText, cond.def.letterDef, parms, LookTargets.Invalid);

        return true;
    }
}
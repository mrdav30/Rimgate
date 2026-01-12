using RimWorld;
using Verse;
using System.Linq;

namespace Rimgate;

public class IncidentWorker_StargateToxicFallout : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;
        var map = parms.target as Map;
        if (map == null) return false;
        return map.listerThings.ThingsOfDef(RimgateDefOf.Rimgate_Dwarfgate)
                   .OfType<Building_Stargate>()
                   .Any();
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        var map = (Map)parms.target;
        // ~1–3 days
        int ticks = GenDate.TicksPerDay * Rand.RangeInclusive(1, 3);
        var cond = (GameCondition_StargateToxicFallout)GameConditionMaker
            .MakeCondition(RimgateDefOf.Rimgate_StargateToxicFallout, ticks);
        map.gameConditionManager.RegisterCondition(cond);

        SendStandardLetter(cond.LabelCap, cond.LetterText, cond.def.letterDef, parms, LookTargets.Invalid);

        return true;
    }
}
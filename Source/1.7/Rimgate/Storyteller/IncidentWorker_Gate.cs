using RimWorld;
using System.Linq;
using Verse;

namespace Rimgate;

public abstract class IncidentWorker_Gate : IncidentWorker
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
}
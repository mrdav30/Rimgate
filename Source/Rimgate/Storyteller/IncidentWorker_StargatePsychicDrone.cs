using RimWorld;
using Verse;
using System.Linq;

namespace Rimgate
{
    public class IncidentWorker_StargatePsychicDrone : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            var map = parms.target as Map;
            if (map == null) return false;

            bool hasGate = map.listerThings.ThingsOfDef(RimgateDefOf.Rimgate_Dwarfgate)
                            .OfType<Building_Stargate>()
                            .Any();
            return hasGate; // only if a receiver exists
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var points = parms.points;
            if (points < 0f)
                points = StorytellerUtility.DefaultThreatPointsNow(map);
            PsychicDroneLevel level = points < 800f 
                ? PsychicDroneLevel.BadLow 
                : !(points < 2000f) 
                    ? PsychicDroneLevel.BadHigh 
                    : PsychicDroneLevel.BadMedium;
            var duration = (int)(GenDate.TicksPerDay * Rand.Range(0.75f, 1.75f));
            var cond = (GameCondition_StargatePsychicDrone)GameConditionMaker.MakeCondition(
                RimgateDefOf.Rimgate_StargatePsychicDrone,
                duration);
            cond.level = level;
            cond.gender = Rand.Element(Gender.Male, Gender.Female);
            map.gameConditionManager.RegisterCondition(cond);

            SendStandardLetter(cond.LabelCap, cond.LetterText, cond.def.letterDef, parms, LookTargets.Invalid);

            return true;
        }
    }
}

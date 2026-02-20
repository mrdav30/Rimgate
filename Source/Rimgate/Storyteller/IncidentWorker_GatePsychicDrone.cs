using RimWorld;
using Verse;

namespace Rimgate
{
    public class IncidentWorker_GatePsychicDrone : IncidentWorker_Gate
    {
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
            var cond = (GameCondition_GatePsychicDrone)GameConditionMaker.MakeCondition(
                RimgateDefOf.Rimgate_GatePsychicDrone,
                duration);
            cond.level = level;
            cond.gender = Rand.Element(Gender.Male, Gender.Female);
            map.gameConditionManager.RegisterCondition(cond);

            SendIncidentLetter(cond.LabelCap, cond.LetterText, cond.def.letterDef, parms, LookTargets.Invalid, def);

            return true;
        }
    }
}

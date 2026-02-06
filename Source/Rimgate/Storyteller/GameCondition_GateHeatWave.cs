using RimWorld;
using Verse;
using System.Linq;
using Verse.Noise;

namespace Rimgate;

public class GameCondition_GateHeatWave : GameCondition_Gate
{
    private const float MaxTempOffset = 17f;

    public override int TransitionTicks => 12000;

    public override void Init()
    {
        LessonAutoActivator.TeachOpportunity(RimgateDefOf.Rimgate_GateIrisProtection, OpportunityType.Critical);
        base.Init();
    }

    public override float TemperatureOffset()
    {
        return GameConditionUtility.LerpInOutValue(this, TransitionTicks, 17f);
    }
}
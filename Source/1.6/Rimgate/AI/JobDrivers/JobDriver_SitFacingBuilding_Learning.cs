using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_SitFacingBuilding_Learning : JobDriver_SitFacingBuilding
{
    private const float LearningRate = 0.1f;

    private SkillDef skillToTrain;

    protected override void ModifyPlayToil(Toil toil)
    {
        // Pick a random skill once per job instance
        if (skillToTrain == null)
        {
            var skills = pawn.skills?.skills;
            if (skills != null && skills.Count > 0)
                skillToTrain = skills.RandomElement().def;
        }

        if (skillToTrain == null)
            return;

        toil.AddPreTickAction(() =>
        {
            var record = pawn.skills?.GetSkill(skillToTrain);
            if (record != null)
                record.Learn(LearningRate);
        });
    }
}
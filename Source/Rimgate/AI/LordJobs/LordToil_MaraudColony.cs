using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Rimgate;

public class LordToil_MaraudColony : LordToil
{
    public override bool ForceHighStoryDanger => true;

    public override bool AllowSatisfyLongNeeds => false;

    private bool _attackDownedIfStarving;

    private bool _canPickUpOpportunisticWeapons;

    private readonly Thing _priorityTarget;

    public LordToil_MaraudColony(bool attackDownedIfStarving = false, Thing priorityTarget = null)
    {
        _attackDownedIfStarving = attackDownedIfStarving;
        _priorityTarget = priorityTarget;
    }

    public override void UpdateAllDuties()
    {
        for (int i = 0; i < lord.ownedPawns.Count; ++i)
        {
            var pawn = lord.ownedPawns[i];
            if (pawn.mindState == null) continue;

            var duty = new PawnDuty(RimgateDefOf.Rimgate_MaraudColony)
            {
                attackDownedIfStarving = _attackDownedIfStarving,
                pickupOpportunisticWeapon = _canPickUpOpportunisticWeapons
            };

            // give them a target focus if we have one
            if (_priorityTarget != null && _priorityTarget.Spawned)
                duty.focus = _priorityTarget;

            pawn.mindState.duty = duty;
            pawn.TryGetComp<CompCanBeDormant>()?.WakeUp();
        }
    }
}

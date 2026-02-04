using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_DialGate : JobDriver
{
    private const int RepairWaitTicks = 200;

    private const int OpenDelayTicks = 500;

    private const float BaseFailChance = 0.0002f;

    private const float SkillFailReductionPerLevel = 0.00001f;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var t = job.targetA.Thing;
        if(t == null)
        {
            EndJobWith(JobCondition.Incompletable);
            yield break;
        }

        Building_Gate gate = null;
        PlanetTile tile = PlanetTile.Invalid;
        IntVec3 destination = IntVec3.Invalid;
        bool delayed = t.def == RimgateDefOf.Rimgate_DialHomeDeviceDestroyed && ResearchUtil.DHDLogicComplete;
        bool fastDial = false;

        if (delayed)
        {
            Building_Gate.TryGetSpawnedGateOnMap(t.Map, out gate);
            tile = GateUtil.WorldComp.AddressList
                .Where(x => x != t.Map.Tile)
                .RandomElement();
            destination = t.Position;
        }
        else if (t is Building_DHD dhd)
        {
            dhd?.TryGetLinkedGate(out gate);
            tile = dhd?.LastDialledAddress ?? PlanetTile.Invalid;
            destination = dhd.InteractionCell;
            fastDial = dhd.Props.canFastDial;
        }
        else
        {
            EndJobWith(JobCondition.Incompletable);
            yield break;
        }

        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => gate == null || gate.IsActive || !tile.Valid);

        yield return Toils_Goto.GotoCell(destination, PathEndMode.ClosestTouch);

        if (delayed)
        {
            Toil repairToil = Toils_General.Wait(RepairWaitTicks);
            repairToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.ClosestTouch);
            repairToil.tickAction = () =>
            {
                var skillLevel = pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
                float failChance = Mathf.Clamp01(BaseFailChance - (skillLevel * SkillFailReductionPerLevel));
                if (Rand.Chance(failChance))
                {
                    Messages.Message("RG_GateDialFailed_DHDDestroyed".Translate(pawn.Named("PAWN")), gate, MessageTypeDefOf.NegativeEvent);
                    FleckMaker.ThrowExplosionCell(t.Position, t.Map, FleckDefOf.ExplosionFlash, Color.blue);
                    t.Destroy();
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                pawn.skills?.Learn(SkillDefOf.Intellectual, 0.1f);
            };
            ToilEffects.WithEffect(repairToil, EffecterDefOf.Hacking, TargetIndex.A, null);
            yield return repairToil;
        }

        yield return new Toil
        {
            initAction = () =>
            {
                gate.QueueOpen(tile, fastDial ? (int)(OpenDelayTicks * 0.5f) : OpenDelayTicks);
            }
        };

        yield break;
    }
}

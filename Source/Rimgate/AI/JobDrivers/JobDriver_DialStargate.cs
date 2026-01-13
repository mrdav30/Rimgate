using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_DialStargate : JobDriver
{
    private const int WaitTicks = 200;

    private const int OpenDelayTicks = 200;

    private const float BaseFailChance = 0.002f;

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

        Building_Stargate gate = null;
        PlanetTile tile = PlanetTile.Invalid;
        IntVec3 destination = IntVec3.Invalid;
        bool delayed = false;

        if (t.def == RimgateDefOf.Rimgate_DialHomeDeviceDestroyed
            && ResearchUtil.DHDLogicComplete)
        {
            gate = Building_Stargate.GetStargateOnMap(t.Map);
            tile = StargateUtil.WorldComp.AddressList
                .Where(x => x != t.Map.Tile)
                .RandomElement();
            destination = t.Position;
            delayed = true;
        }
        else if (t is Building_DHD dhd)
        {
            gate = dhd?.LinkedStargate;
            tile = dhd?.LastDialledAddress ?? PlanetTile.Invalid;
            destination = dhd.InteractionCell;
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

            Toil repairToil = Toils_General.Wait(WaitTicks);
            repairToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.ClosestTouch);
            repairToil.tickAction = () =>
            {
                var skillLevel = pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
                float failChance = Mathf.Clamp01(BaseFailChance - (skillLevel * 0.0001f));
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
                gate.QueueOpen(tile, OpenDelayTicks);
            }
        };

        yield break;
    }
}

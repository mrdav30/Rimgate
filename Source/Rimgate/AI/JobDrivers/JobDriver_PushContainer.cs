using System.Collections.Generic;
using Verse.AI;
using Verse;
using RimWorld;
using UnityEngine;
using System.Security.Cryptography;
using System.Threading;
using System.Net.NetworkInformation;
using RimWorld.Planet;
using Verse.Noise;

namespace Rimgate;

public class JobDriver_PushContainer : JobDriver
{
    // private Building_MobileContainer Cart => job.targetA.Thing as Building_MobileContainer;

    private Comp_MobileContainerControl _proxyComp;

    private Thing_PushedCartVisual _pushVisual;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;

        // Reserve destination (targetC will be filled in init)
        if (job.targetC.IsValid && pawn.Map != null)
            pawn.Map.reservationManager.Reserve(pawn, job, job.targetC);

        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var cart = job.targetA.Thing as Building_MobileContainer;
        var cartDef = cart?.def;
        this.FailOn(() => cartDef == null);

        this.FailOn(() => pawn.Downed);
        // Disallow if container starts loading again mid-job
        this.FailOn(() => cart.Control?.LoadingInProgress == true);

        // Go to cart
        var goToThing = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        goToThing.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        if (Map.designationManager.DesignationOn(cart, RimgateDefOf.Rimgate_DesignationPushCart) != null)
            goToThing.FailOnThingMissingDesignation(TargetIndex.A, RimgateDefOf.Rimgate_DesignationPushCart);
        yield return goToThing;

        // Init: compute stand cell, convert to proxy, apply slowdown
        var init = new Toil
        {
            initAction = () =>
            {
                var dest = job.targetC.HasThing
                    ? job.targetC.Thing.Position
                    : job.targetC.IsValid
                        ? job.targetC.Cell
                        : pawn.Position;  // final desired cell

                if (!Utils.FindStandCellFor(pawn.Position, dest, pawn.Map, out IntVec3 stand))
                {
                    Messages.Message("RG_MessagePawnCannotReachDestination".Translate(pawn.LabelShort),
                        cart,
                        MessageTypeDefOf.RejectInput);

                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Save stand in B so Goto below knows where to stand
                job.targetB = stand;
                cart.Control?.ClearDesignations();

                var colorA = cart.DrawColor;
                var colorB = cart.DrawColorTwo;
                var frontOffset = cart.Control?.Props.frontOffset ?? 1.0f;
                var slowdown = cart.Control?.SlowdownSeverity ?? 0f;

                // Convert to proxy and pick it up
                var proxy = cart.ConvertCartToProxy(pawn);
                _proxyComp = proxy?.Control;
                if (_proxyComp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!_proxyComp.ProxyFuelOk)
                {
                    Messages.Message("RG_CartNoFuel".Translate(cartDef.label),
                        proxy,
                        MessageTypeDefOf.RejectInput);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Proxy is not spawned in; give it directly to the carry tracker
                if (!pawn.carryTracker.TryStartCarry(proxy))
                {
                    Messages.Message("RG_MessagePawnCannotPush".Translate(pawn.LabelShort, cartDef.label),
                        pawn,
                        MessageTypeDefOf.RejectInput);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                ResetPushVisual();

                _pushVisual = GenSpawn.Spawn(RimgateDefOf.Rimgate_PushedCartVisual,
                pawn.Position,
                pawn.Map,
                WipeMode.Vanish) as Thing_PushedCartVisual;

                _pushVisual.Init(cartDef, frontOffset, colorA, colorB);
                _pushVisual.AttachTo(pawn);

                pawn.ApplyHediff(RimgateDefOf.Rimgate_PushingCart, severity: slowdown);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return init;

        // Walk to stand cell next to the destination
        var move = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell)
            .FailOn(() => pawn.Downed);

        // drain fuel every tick while moving
        move.tickAction = () =>
        {
            if (!_proxyComp.IsProxyRefuelable) return;

            _proxyComp.PushingFuel -= _proxyComp.FuelPerTick;
            if (_proxyComp.ProxyFuelOk) return;

            // out of fuel
            Messages.Message("RG_CartRanOutOfFuel".Translate(cart.LabelShort),
                new TargetInfo(pawn.Position, pawn.Map),
                MessageTypeDefOf.NegativeEvent);
            EndJobWith(JobCondition.Incompletable);
        };
        yield return move;

        // Place cart at exact destination (targetC)
        var place = new Toil
        {
            initAction = () =>
            {
                var carried = pawn.carryTracker.CarriedThing as Thing_MobileCartProxy;
                if (carried == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                var gate = job.targetC.Thing as Building_Stargate;
                if (gate != null)
                {
                    // sanity: gate usable?
                    var sg = gate.GateControl;
                    if (!sg.IsActive || sg.IsIrisActivated)
                    {
                        Messages.Message(
                            "CannotEnterPortal".Translate(gate.Label),
                            gate,
                            MessageTypeDefOf.RejectInput);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // 1) Convert carried proxy -> cart (unspawned), hand to gate send buffer
                    var gateCart = carried.ConvertProxyToCart(pawn,
                        cartDef,
                        spawn: false);
                    _proxyComp = null;
                    pawn.RemoveHediff(RimgateDefOf.Rimgate_PushingCart);
                    ResetPushVisual();

                    // 2) hand the unspawned cart to the gate
                    sg.AddToSendBuffer(gateCart);  // gate system will forward & spawn at receiver

                    if (job.def == RimgateDefOf.Rimgate_EnterStargateWithContainer)
                    {
                        sg.AddToSendBuffer(pawn);
                        pawn.DeSpawn();
                        EndJobWith(JobCondition.Succeeded);
                    }

                    return;
                }

                // “place on ground” path (when dest is not a gate)
                var dest = job.targetC.Cell;
                // Convert directly from carried proxy → spawned cart at destination
                var cart = carried.ConvertProxyToCart(pawn,
                    cartDef,
                    dest,
                    Utils.RotationFacingFor(job.targetB.Cell, dest));

                _proxyComp = null;
                pawn.RemoveHediff(RimgateDefOf.Rimgate_PushingCart);
                ResetPushVisual();

                // Dump if this is the push & dump job
                if (cart != null && job.haulMode == HaulMode.ToCellNonStorage)
                {
                    var cc = cart.Control;
                    cc?.InnerContainer?.TryDropAll(dest, pawn.Map, ThingPlaceMode.Near);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return place;

        // Final safety: if interrupted, try to restore a cart
        AddFinishAction((jc) =>
        {
            if (jc == JobCondition.Succeeded || _proxyComp == null) return;

            // 1) If carrying a proxy, convert it back right away
            if (pawn.carryTracker?.CarriedThing is Thing_MobileCartProxy carried)
            {
                var drop = Utils.BestDropCellNearThing(pawn);
                carried.ConvertProxyToCart(pawn,
                    cartDef,
                    drop,
                    Utils.RotationFacingFor(pawn.Position, drop));
            }
            else
            {
                // 2) Otherwise, scan small radius for a dropped proxy
                var map = pawn.Map;
                foreach (var c in GenRadial.RadialCellsAround(pawn.Position, 2.0f, useCenter: true))
                {
                    if (!c.InBounds(map)) continue;
                    var list = c.GetThingList(map);
                    for (int i = 0; i < list.Count; i++)
                        if (list[i] is Thing_MobileCartProxy pr)
                            pr.ConvertProxyToCart(pawn,
                                cartDef,
                                pr.Position,
                                Utils.RotationFacingFor(pawn.Position, pr.Position));
                }
            }

            _proxyComp = null;
            pawn.RemoveHediff(RimgateDefOf.Rimgate_PushingCart);
            ResetPushVisual();
        });
    }

    private void ResetPushVisual()
    {
        if (_pushVisual != null && !_pushVisual.Destroyed)
            _pushVisual.Destroy(DestroyMode.Vanish);
        _pushVisual = null;
    }
}
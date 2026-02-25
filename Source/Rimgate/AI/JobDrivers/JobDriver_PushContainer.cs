using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_PushContainer : JobDriver
{
    private Thing_MobileCartProxy _proxyCart;

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
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => pawn.Downed);

        // Go to cart
        var goToThing = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        goToThing.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        if (Map.designationManager.DesignationOn(job.targetA.Thing, RimgateDefOf.Rimgate_DesignationPushCart) != null)
            goToThing.FailOnThingMissingDesignation(TargetIndex.A, RimgateDefOf.Rimgate_DesignationPushCart);
        yield return goToThing;

        // Init: compute stand cell, convert to proxy, apply slowdown
        var init = new Toil
        {
            initAction = () =>
            {
                // ensure cart is still there and valid to push
                if (job.targetA.Thing is not Building_MobileContainer cart || cart.LoadingInProgress)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                var dest = job.targetC.HasThing
                    ? job.targetC.Thing.InteractionCell // destination is a gate
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
                cart.ClearDesignations();

                // Convert to proxy and pick it up
                _proxyCart = cart.GetProxyForCart();
                if (_proxyCart == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // If enough fuel to push — restore immediately and end the job
                if (!_proxyCart.ProxyFuelOk)
                {
                    Messages.Message("RG_CartNoFuel".Translate(cart.LabelShort),
                        cart,
                        MessageTypeDefOf.RejectInput);

                    _proxyCart.Destroy();
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Proxy is not spawned in; give it directly to the carry tracker
                if (!pawn.carryTracker.TryStartCarry(_proxyCart))
                {
                    // Pawn can’t carry this (capacity, reservation weirdness, etc.) — restore
                    Messages.Message("RG_MessagePawnCannotPush".Translate(pawn.LabelShort, cart.LabelShort),
                        pawn,
                        MessageTypeDefOf.RejectInput);

                    _proxyCart.Destroy();
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Move contents to proxy before despawning cart, to preserve stuff & HP
                cart.MoveContentsToProxy(_proxyCart);

                ResetPushVisual();

                _pushVisual = GenSpawn.Spawn(RimgateDefOf.Rimgate_PushedCartVisual,
                    pawn.Position,
                    pawn.Map,
                    WipeMode.Vanish) as Thing_PushedCartVisual;

                _pushVisual.Init(cart.def, cart.DrawColor, cart.DrawColorTwo, cart.Ext.frontOffset);
                _pushVisual.AttachTo(pawn);

                if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_PushingCart)) // prevent stacking
                    pawn.ApplyHediff(RimgateDefOf.Rimgate_PushingCart, severity: cart.Ext.slowdownSeverity);

                // Now that the proxy is safely in the carry tracker, we can despawn the cart
                cart.DeSpawn();
            }
        };
        yield return init;

        // Walk to stand cell next to the destination
        var move = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell)
            .FailOn(() => pawn.Downed)
            .FailOn(() => _proxyCart == null);

        // drain fuel every tick while moving
        move.tickAction = () =>
        {
            if (!_proxyCart.IsProxyRefuelable) return;

            _proxyCart.PushingFuel -= _proxyCart.FuelPerTick;
            if (_proxyCart.ProxyFuelOk) return;

            // out of fuel
            Messages.Message("RG_CartRanOutOfFuel".Translate(_proxyCart.Label),
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
                if (pawn.carryTracker.CarriedThing is not Thing_MobileCartProxy carried)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (job.targetC.Thing is Building_Gate gate)
                {
                    // sanity: gate usable?
                    if (!gate.IsActive || gate.IsIrisActivated)
                    {
                        Messages.Message(
                            "CannotEnterPortal".Translate(gate.Label),
                            gate,
                            MessageTypeDefOf.RejectInput);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // 1) Convert carried proxy -> cart (unspawned), hand to gate send buffer
                    Building_MobileContainer container = carried.ConvertProxyToCart();
                    carried.MoveContentsToContainer(container);
                    carried.Destroy();
                    _proxyCart = null; // just in case

                    pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PushingCart);
                    ResetPushVisual();

                    // 2) hand the unspawned cart to the gate
                    gate.AddToSendBuffer(container);  // gate system will forward & spawn at receiver

                    if (job.def == RimgateDefOf.Rimgate_EnterGateWithContainer)
                    {
                        gate.AddToSendBuffer(pawn);
                        pawn.DeSpawn();
                        EndJobWith(JobCondition.Succeeded);
                    }

                    return;
                }

                // “place on ground” path (when dest is not a gate)
                var map = pawn.Map;
                var dest = job.targetC.Cell;
                if (map != null && !dest.InBounds(map))
                    dest = pawn.Position; // fallback to current position if something went wrong with the destination

                // Convert directly from carried proxy → spawned cart at destination
                var cart = carried.ConvertProxyToCart();
                carried.MoveContentsToContainer(cart);
                carried.Destroy();
                _proxyCart = null; // just in case
                GenSpawn.Spawn(cart, dest, map, Utils.RotationFacingFor(job.targetB.Cell, dest));

                pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PushingCart);
                ResetPushVisual();

                // Dump if this is the push & dump job
                if (job.haulMode == HaulMode.ToCellNonStorage)
                    cart?.InnerContainer.TryDropAll(dest, pawn.Map, ThingPlaceMode.Near);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return place;

        // Final safety: if interrupted, try to restore a cart and clean up visuals/hediff
        AddFinishAction((jc) =>
        {
            if (jc == JobCondition.Succeeded || _proxyCart == null) return;

            // 1) If carrying a proxy, convert it back right away
            if (pawn.carryTracker?.CarriedThing is Thing_MobileCartProxy carried)
            {
                var drop = Utils.BestDropCellNearThing(pawn);
                var cart = carried.ConvertProxyToCart();
                carried.MoveContentsToContainer(cart);
                carried.Destroy();
                GenSpawn.Spawn(cart, drop, pawn.Map, Utils.RotationFacingFor(pawn.Position, drop));
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
                    {
                        if (list[i] is Thing_MobileCartProxy pr)
                        {
                            var cart = pr.ConvertProxyToCart();
                            pr.MoveContentsToContainer(cart);
                            pr.Destroy();
                            GenSpawn.Spawn(cart, pr.Position, pawn.Map, Utils.RotationFacingFor(pawn.Position, pr.Position));
                        }
                    }
                }
            }

            _proxyCart = null;
            pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PushingCart);
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
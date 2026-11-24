using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_PushContainer : JobDriver
{
    // whether the proxy cart has been spawned during init (used to determine if we need to try to restore it on job interruption)
    private bool _proxySpawned;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _proxySpawned, "proxySpawned");
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;

        // Reserve destination (targetC will be filled in init)
        if (job.targetB.IsValid && pawn.Map != null)
            pawn.Map.reservationManager.Reserve(pawn, job, job.targetB);

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

                var dest = Building_MobileContainer.GetPushDestinationCell(job.targetB);
                if (!dest.IsValid) dest = pawn.Position; // fallback to current position if something went wrong with the destination

                if (!Utils.FindStandCellFor(pawn.Position, dest, pawn.Map, out IntVec3 stand))
                {
                    Messages.Message("RG_MessagePawnCannotReachDestination".Translate(pawn.LabelShort),
                        cart,
                        MessageTypeDefOf.RejectInput);

                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Save stand in C so Goto below knows where to stand
                job.targetC = stand;
                cart.ClearDesignations();

                // Convert to proxy and pick it up
                Thing_MobileCartProxy proxyCart = cart.GetProxyForCart();
                if (proxyCart == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                proxyCart.PushDestination = dest;

                string failureReason = null;

                // If not enough fuel to push — restore immediately and end the job
                if (!proxyCart.ProxyFuelOk)
                    failureReason = "RG_CartNoFuel".Translate(cart.LabelShort);

                // Check if pawn can carry this
                if (!pawn.carryTracker.TryStartCarry(proxyCart))
                    failureReason = " RG_MessagePawnCannotPush".Translate(pawn.LabelShort, cart.LabelShort);

                if (!failureReason.NullOrEmpty())
                {
                    Messages.Message(failureReason, cart, MessageTypeDefOf.RejectInput);
                    proxyCart.Destroy();
                    proxyCart = null;
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                _proxySpawned = true;

                // Move contents to proxy before despawning cart, to preserve stuff & HP
                cart.MoveContentsToProxy(proxyCart);
                cart.DeSpawn();

                if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_PushingCart)) // prevent stacking
                    pawn.ApplyHediff(RimgateDefOf.Rimgate_PushingCart, severity: cart.Ext.slowdownSeverity);

                Thing_PushedCartVisual pushVisual = GenSpawn.Spawn(RimgateDefOf.Rimgate_PushedCartVisual,
                    pawn.Position,
                    pawn.Map,
                    WipeMode.Vanish) as Thing_PushedCartVisual;

                pushVisual.Setup(cart.def, cart.DrawColor, cart.DrawColorTwo, cart.Ext.frontOffset);
                pushVisual.AttachTo(pawn);
            }
        };
        yield return init;

        // Walk to stand cell next to the destination
        var move = Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.OnCell)
            .FailOn(() => pawn.Downed)
            .FailOn(() => pawn.carryTracker.CarriedThing is not Thing_MobileCartProxy carried);

        // drain fuel every tick while moving
        move.tickAction = () =>
        {
            if (pawn.carryTracker.CarriedThing is not Thing_MobileCartProxy cart || !cart.IsProxyRefuelable) return;

            cart.PushingFuel -= cart.FuelPerTick;
            if (cart.ProxyFuelOk) return;

            // out of fuel
            Messages.Message("RG_CartRanOutOfFuel".Translate(cart.Label),
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
                // Clean up visuals & hediff right away
                pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PushingCart);
                pawn.TryGetComp<CompAttachBase>()?.GetAttachment(RimgateDefOf.Rimgate_PushedCartVisual)?.Destroy();

                if (pawn.carryTracker.CarriedThing is not Thing_MobileCartProxy carried)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // 1) Convert carried proxy -> cart (unspawned)
                Building_MobileContainer cart = carried.ConvertProxyToCart(pawn.Faction);
                carried.MoveContentsToContainer(cart);
                carried.Destroy();

                if (job.targetB.HasThing && job.targetB.Thing is Building_Gate gate)
                {
                    // 2) If destination is an active gate with open iris, send the cart through the gate instead of placing on ground
                    if (gate.IsActive && !gate.IsIrisActivated)
                    {
                        // 2) hand the unspawned cart to the gate
                        gate.AddToSendBuffer(cart);  // gate system will forward & spawn at receiver

                        if (job.def == RimgateDefOf.Rimgate_EnterGateWithContainer)
                        {
                            gate.AddToSendBuffer(pawn);
                            pawn.DeSpawn();
                        }

                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }

                    // Gate is not active or iris is closed — can’t place cart into an inactive gate
                    Messages.Message("CannotEnterPortal".Translate(gate.Label),
                        gate,
                        MessageTypeDefOf.RejectInput);
                }

                // “place on ground” path (when dest is not a gate)
                var map = pawn.Map;
                var cartDestination = job.targetB.Cell;
                var pawnDestination = job.targetC.Cell;

                // Spawn the cart at destination, facing the direction of the push (from pawn to dest)
                GenSpawn.Spawn(cart, cartDestination, map, Utils.RotationFacingFor(pawnDestination, cartDestination));

                // Dump if this is the push & dump job
                if (job.haulMode == HaulMode.ToCellNonStorage)
                    cart?.InnerContainer.TryDropAll(cartDestination, pawn.Map, ThingPlaceMode.Near);

                EndJobWith(JobCondition.Succeeded);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return place;

        // Final safety: if interrupted, try to restore a cart and clean up visuals/hediff
        AddFinishAction((jc) =>
        {
            // If job completed successfully, no need to do anything — cart is already placed and visuals cleaned up
            if (jc == JobCondition.Succeeded) return;

            // Clean up visuals & hediff right away
            pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PushingCart);
            pawn.TryGetComp<CompAttachBase>()?.GetAttachment(RimgateDefOf.Rimgate_PushedCartVisual)?.Destroy();

            // If proxy was never spawned (interrupted during init), nothing to restore
            if (!_proxySpawned) return;

            // 1) If carrying a proxy, convert it back right away
            if (pawn.carryTracker?.CarriedThing is Thing_MobileCartProxy carried)
            {
                var drop = Utils.BestDropCellNearThing(pawn);
                var cart = carried.ConvertProxyToCart(pawn.Faction);
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
                            var cart = pr.ConvertProxyToCart(pawn.Faction);
                            pr.MoveContentsToContainer(cart);
                            pr.Destroy();
                            GenSpawn.Spawn(cart, pr.Position, pawn.Map, Utils.RotationFacingFor(pawn.Position, pr.Position));
                        }
                    }
                }
            }

            _proxySpawned = false;
        });
    }
}

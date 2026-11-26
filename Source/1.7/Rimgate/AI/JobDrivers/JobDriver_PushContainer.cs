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
        // On reload A is the carried proxy and the pawn may not be spawned yet.
        if (job.targetA.Thing is Building_MobileContainer { Spawned: true } designatedCart &&
            designatedCart.Map.designationManager.DesignationOn(designatedCart, RimgateDefOf.Rimgate_DesignationPushCart) != null)
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
                if (!dest.IsValid)
                {
                    // A gate can close while the pawn approaches the original cart.
                    // Leave that cart intact instead of turning an invalid gate into a cell push.
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

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
                // The original is no longer saved; retain only references to the owned proxy.
                cart.Map.reservationManager.Release(cart, pawn, job);
                job.SetTarget(TargetIndex.A, proxyCart);
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

                Building_Gate gate = null;
                if (job.targetB.HasThing)
                {
                    gate = job.targetB.Thing as Building_Gate;
                    if (gate == null || !gate.Spawned || gate.Map != pawn.Map || !gate.IsActive || gate.IsIrisActivated)
                    {
                        if (gate != null)
                            Messages.Message("CannotEnterPortal".Translate(gate.Label), gate, MessageTypeDefOf.RejectInput);
                        // Keep the carried proxy for normal failure cleanup; never spawn on a gate.
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                }

                Building_MobileContainer cart = carried.ConvertProxyToCart(pawn.Faction);
                // Secure the gate owner before releasing the proxy's cargo. Failed admission
                // leaves the original carried owner intact for interruption cleanup.
                if (gate != null && !gate.AddToSendBuffer(cart))
                {
                    cart.Destroy();
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                carried.MoveContentsToContainer(cart);
                carried.Destroy();

                if (gate != null)
                {
                    if (job.def == RimgateDefOf.Rimgate_EnterGateWithContainer && !gate.AddToSendBuffer(pawn))
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                    EndJobWith(JobCondition.Succeeded);
                    return;
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

            // Target A is the exact deeply owned proxy after conversion, including after reload.
            // Never convert another pusher's nearby proxy during this job's cleanup.
            if (job.targetA.Thing is Thing_MobileCartProxy proxy && !proxy.Destroyed
                && (pawn.carryTracker?.CarriedThing == proxy
                    || proxy.Spawned && proxy.Map == pawn.Map))
                _proxySpawned = !WorldComp_CartRecovery.TryRestore(proxy, pawn);
        });
    }
}

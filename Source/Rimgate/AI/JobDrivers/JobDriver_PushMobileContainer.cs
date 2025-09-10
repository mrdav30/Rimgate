using System.Collections.Generic;
using Verse.AI;
using Verse;
using RimWorld;
using UnityEngine;
using System.Security.Cryptography;
using System.Threading;
using System.Net.NetworkInformation;

namespace Rimgate;

public class JobDriver_PushMobileContainer : JobDriver
{
    private Building_MobileContainer Cart => job.targetA.Thing as Building_MobileContainer;

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
        this.FailOn(() => pawn.Downed);
        // Disallow if container starts loading again mid-job
        this.FailOn(() => Cart?.Mobile?.LoadingInProgress == true);

        // Go to cart
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.A);

        // Init: compute stand cell, convert to proxy, apply slowdown
        var init = new Toil
        {
            initAction = () =>
            {
                var dest = job.targetC.IsValid ? job.targetC.Cell : pawn.Position; // final desired cell
                var stand = Utils.FindStandCellFor(pawn, dest, pawn.Map, pawn.Position);

                // Save stand in B so Goto below knows where to stand
                job.targetB = stand;

                // Convert to proxy and pick it up
                var proxy = ConvertCartToProxy(Cart, pawn);
                if (proxy != null)
                {
                    var pcomp = proxy.GetComp<Comp_MobileContainer>();
                    // Proxy is unspawned; give it directly to the carry tracker
                    if (pawn.carryTracker.TryStartCarry(proxy))
                    {
                        float factor = Cart?.Mobile?.Props?.moveSpeedFactorWhilePushing ?? 0.90f;
                        ApplySlowdownHediff(pawn, 1f - Mathf.Clamp01(factor));
                        Cart?.Mobile?.StartPushVisual(pawn, Cart.def, Cart.DrawColor, Cart.DrawColorTwo);
                    }
                    else
                    {
                        // could not pick up? revert
                        ConvertProxyToCart(proxy,
                            pawn,
                            Cart.def,
                            Cart.Position,
                            Cart.Rotation);
                        EndJobWith(JobCondition.Incompletable);
                    }
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return init;

        // Walk to stand cell next to the destination
        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch)
            .FailOn(() => pawn.Downed);

        // Place cart at exact destination (targetC)
        var place = new Toil
        {
            initAction = () =>
            {
                var gate = job.targetC.Thing as Building_Stargate;
                if (gate != null)
                {
                    // sanity: gate usable?
                    var sg = gate.StargateComp;
                    if (!sg.IsActive || sg.IsIrisActivated)
                    {
                        Messages.Message(
                            "CannotEnterPortal".Translate(gate.Label),
                            gate,
                            MessageTypeDefOf.RejectInput);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // Convert carried proxy -> cart (unspawned), hand to gate send buffer, clean up visual/hediff
                    var carriedProxy = pawn.carryTracker.CarriedThing as Thing_MobileCartProxy;
                    if (carriedProxy != null)
                    {
                        // 1) Build the cart *unspawned* with saved state
                        var proxyComp = carriedProxy.GetComp<Comp_MobileContainer>();
                        var cartDef = Cart?.def ?? RimgateDefOf.Rimgate_Wheelbarrow;
                        var cart = (Building_MobileContainer)ThingMaker.MakeThing(cartDef, proxyComp.SavedStuff);
                        cart.HitPoints = Mathf.Clamp(proxyComp.SavedHitPoints > 0 ? proxyComp.SavedHitPoints : cart.MaxHitPoints, 1, cart.MaxHitPoints);

                        // transfer contents
                        var destComp = cart.GetComp<Comp_MobileContainer>();
                        proxyComp.InnerContainer.TryTransferAllToContainer(destComp.InnerContainer);

                        // save paint
                        var paint = cart.TryGetComp<CompColorable>();
                        if (paint != null && proxyComp.SavedHasPaint)
                            paint.SetColor(proxyComp.SavedDrawColor);

                        // 2) remove slowdown + visual, destroy proxy
                        RemoveSlowdownHediff(pawn);
                        Cart?.Mobile?.StopPushVisual();
                        carriedProxy.Destroy(DestroyMode.Vanish);

                        // 3) hand the unspawned cart to the gate
                        sg.AddToSendBuffer(cart);  // gate system will forward & spawn at receiver
                        return;
                    }
                }

                // “place on ground” path (when dest is not a gate)
                var carried = pawn.carryTracker.CarriedThing as Thing_MobileCartProxy;
                var dest = job.targetC.Cell;
                if (carried != null)
                {
                    // Convert directly from carried proxy → spawned cart at destination
                    var cart = ConvertProxyToCart(carried,
                        pawn,
                        Cart?.def ?? RimgateDefOf.Rimgate_Wheelbarrow,
                        dest,
                        Utils.RotationFacingFor(job.targetB.Cell, dest));

                    Cart?.Mobile?.StopPushVisual();

                    // Dump if this is the “push & dump” job
                    if (job.def == RimgateDefOf.Rimgate_PushAndDumpMobileContainer && cart != null)
                    {
                        var cc = cart.GetComp<Comp_MobileContainer>();
                        cc?.InnerContainer?.TryDropAll(dest, pawn.Map, ThingPlaceMode.Near);
                    }
                }

                RemoveSlowdownHediff(pawn);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return place;

        // Final safety: if interrupted, try to restore a cart
        AddFinishAction((jc) =>
        {
            if (jc == JobCondition.Succeeded) return;

            RemoveSlowdownHediff(pawn);
            // 1) If carrying a proxy, convert it back right away
            if (pawn.carryTracker?.CarriedThing is Thing_MobileCartProxy carried)
            {
                var drop = Utils.BestDropCellNearPawn(pawn);
                ConvertProxyToCart(carried,
                    pawn,
                    Cart?.def ?? RimgateDefOf.Rimgate_Wheelbarrow,
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
                            ConvertProxyToCart(pr,
                                pawn,
                                Cart?.def ?? RimgateDefOf.Rimgate_Wheelbarrow,
                                pr.Position,
                                Utils.RotationFacingFor(pawn.Position, pr.Position));
                }
            }

            Cart?.Mobile?.StopPushVisual();
        });
    }

    // Docked -> proxy (same as your logic, but returns an unspawned proxy Thing ready to spawn)
    Thing_MobileCartProxy ConvertCartToProxy(Building_MobileContainer cart, Pawn p)
    {
        var ccomp = cart.GetComp<Comp_MobileContainer>();
        if (ccomp == null) return null;

        // Create proxy + move contents
        var proxy = (Thing_MobileCartProxy)ThingMaker.MakeThing(RimgateDefOf.Rimgate_MobileCartProxy);
        var pcomp = proxy.GetComp<Comp_MobileContainer>();
        if (pcomp == null)
        {
            Log.Error("[Rimgate] Proxy missing Comp_MobileContainer.");
            return null;
        }

        // cache stuff, hp, paint & draw colors before we despawn
        pcomp.SaveVisualFrom(cart);
        var cartContainer = cart.TryGetInnerContainer();
        cartContainer?.TryTransferAllToContainer(pcomp.InnerContainer);

        // Despawn the cart (contents already moved)
        cart.DeSpawn();

        pcomp.Attach(p);
        return proxy;
    }

    // proxy -> Docked at a specific cell, using the original cart def when possible
    Building_MobileContainer ConvertProxyToCart(Thing_MobileCartProxy proxy, Pawn p, ThingDef cartDef, IntVec3 at, Rot4 rot)
    {
        var map = p.Map;
        var pcomp = proxy.GetComp<Comp_MobileContainer>();
        if (pcomp == null) return null;

        if (!at.InBounds(map)) at = p.Position;

        // Make cart with original stuff (if any), then spawn that instance
        var made = (Building_MobileContainer)ThingMaker.MakeThing(cartDef, pcomp.SavedStuff);
        made.HitPoints = Mathf.Clamp(pcomp.SavedHitPoints, 1, made.MaxHitPoints);
        var cart = (Building_MobileContainer)GenSpawn.Spawn(made, at, map, rot);

        var ccomp = cart.GetComp<Comp_MobileContainer>();

        // Move back contents
        pcomp.InnerContainer.TryTransferAllToContainer(ccomp.InnerContainer);

        pcomp.Detach();
        proxy.Destroy(DestroyMode.Vanish);

        var paint = cart.TryGetComp<CompColorable>();
        if (paint != null && pcomp.SavedHasPaint)
            paint.SetColor(pcomp.SavedDrawColor);

        return cart;
    }
    void ApplySlowdownHediff(Pawn p, float severity)
    {
        // Uses your HediffDef; set severity so one HediffDef can scale per cart type
        var def = RimgateDefOf.Rimgate_PushingCart;
        if (!p.health.hediffSet.HasHediff(def))
        {
            var h = HediffMaker.MakeHediff(def, p);
            h.Severity = severity;
            p.health.AddHediff(h);
        }
        else
        {
            var h = p.health.hediffSet.GetFirstHediffOfDef(def);
            h.Severity = severity;
        }
    }

    void RemoveSlowdownHediff(Pawn p)
    {
        var h = p.health.hediffSet.GetFirstHediffOfDef(RimgateDefOf.Rimgate_PushingCart);
        if (h != null) p.health.RemoveHediff(h);
    }
}
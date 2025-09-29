using System.Collections.Generic;
using Verse.AI;
using Verse;
using RimWorld;
using UnityEngine;
using System.Security.Cryptography;
using System.Threading;
using System.Net.NetworkInformation;
using RimWorld.Planet;
using static HarmonyLib.Code;
using Verse.Noise;

namespace Rimgate;

public class JobDriver_PushMobileContainer : JobDriver
{
    private Building_MobileContainer Cart => job.targetA.Thing as Building_MobileContainer;

    private Comp_MobileContainer _proxyComp;

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
                var dest = job.targetC.HasThing
                    ? job.targetC.Thing.Position
                    : job.targetC.IsValid
                        ? job.targetC.Cell
                        : pawn.Position;  // final desired cell
                var stand = Utils.FindStandCellFor(pawn, dest, pawn.Map, pawn.Position);

                // Save stand in B so Goto below knows where to stand
                job.targetB = stand;

                // Convert to proxy and pick it up
                var proxy = ConvertCartToProxy(Cart, pawn);
                _proxyComp = proxy.GetComp<Comp_MobileContainer>();
                if (proxy == null || _proxyComp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!_proxyComp.ProxyFuelOk)
                {
                    Messages.Message("RG_CartNoFuel".Translate(Cart.LabelShort),
                        Cart,
                        MessageTypeDefOf.RejectInput);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Proxy is not spawned in; give it directly to the carry tracker
                if (!pawn.carryTracker.TryStartCarry(proxy))
                {
                    Messages.Message("RG_MessagePawnCannotPush".Translate(pawn.LabelShort, Cart.LabelShort),
                    Cart,
                    MessageTypeDefOf.RejectInput);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                StartPushVisual(pawn,
                    Cart.def,
                    Cart.DrawColor,
                    Cart.DrawColorTwo,
                    Cart.Mobile.Props.frontOffset);

                float severity = Cart.Mobile.SlowdownSeverity;
                ApplySlowdownHediff(pawn, severity);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return init;

        // Walk to stand cell next to the destination
        var move = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch)
            .FailOn(() => pawn.Downed);

        // drain fuel every tick while moving
        move.tickAction = () =>
        {
            if (!_proxyComp.IsProxyRefuelable) return;

            _proxyComp.PushingFuel -= _proxyComp.FuelPerTick;
            if (_proxyComp.ProxyFuelOk) return;

            // out of fuel
            Messages.Message("RG_CartRanOutOfFuel".Translate(Cart.LabelShort),
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
                    var sg = gate.StargateControl;
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
                    var gateCart = ConvertProxyToCart(carried,
                        pawn,
                        Cart?.def ?? RimgateDefOf.Rimgate_Wheelbarrow,
                        spawn: false);
                    _proxyComp = null;
                    RemoveSlowdownHediff(pawn);
                    StopPushVisual();

                    // 2) hand the unspawned cart to the gate
                    sg.AddToSendBuffer(gateCart);  // gate system will forward & spawn at receiver

                    if(job.def == RimgateDefOf.Rimgate_EnterStargateWithContainer)
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
                var cart = ConvertProxyToCart(carried,
                    pawn,
                    Cart?.def ?? RimgateDefOf.Rimgate_Wheelbarrow,
                    dest,
                    Utils.RotationFacingFor(job.targetB.Cell, dest));

                _proxyComp = null;
                RemoveSlowdownHediff(pawn);
                StopPushVisual();

                // Dump if this is the “push & dump” job
                if (job.def == RimgateDefOf.Rimgate_PushAndDumpMobileContainer && cart != null)
                {
                    var cc = cart.GetComp<Comp_MobileContainer>();
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

            _proxyComp = null;
            RemoveSlowdownHediff(pawn);
            StopPushVisual();
        });
    }

    // Docked -> proxy returns an unspawned proxy Thing ready to spawn
    static Thing_MobileCartProxy ConvertCartToProxy(Building_MobileContainer cart, Pawn p)
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

        var refuelable = ccomp.Refuelable;
        if (refuelable != null)
        {
            // copy current fuel
            pcomp.PushingFuel = refuelable.Fuel;
            pcomp.FuelPerTick = ccomp.ConsumptionRatePerTick;
        }
        else
        {
            pcomp.PushingFuel = 0f;
            pcomp.FuelPerTick = 0f;
        }

        // Despawn the cart (contents already moved)
        cart.DeSpawn();
        pcomp.Attach(p);

        return proxy;
    }

    // proxy -> Docked at a specific cell, using the original cart def when possible
    static Building_MobileContainer ConvertProxyToCart(
        Thing_MobileCartProxy proxy,
        Pawn p,
        ThingDef cartDef,
        IntVec3? at = null,
        Rot4? rot = null,
        bool spawn = true)
    {
        var pcomp = proxy.GetComp<Comp_MobileContainer>();
        if (pcomp == null) return null;

        // Make cart with original stuff (if any), then spawn that instance
        var made = (Building_MobileContainer)ThingMaker.MakeThing(cartDef, pcomp.SavedStuff);
        made.HitPoints = Mathf.Clamp(pcomp.SavedHitPoints, 1, made.MaxHitPoints);

        if (spawn)
        {
            var map = p.Map;
            if (at == null || !at.Value.InBounds(map)) at = p.Position;
            if (rot == null) rot = p.Rotation;
            GenSpawn.Spawn(made, at.Value, map, rot.Value);
        }

        var ccomp = made.GetComp<Comp_MobileContainer>();

        // Move back contents
        pcomp.InnerContainer.TryTransferAllToContainer(ccomp.InnerContainer);

        // restore fuel (if any)
        if (ccomp.Refuelable != null)
        {
            // CompRefuelable starts at 0 on fresh Thing; add whatever is left from pushing
            if (pcomp != null && pcomp.PushingFuel > 0f)
                ccomp.Refuelable.Refuel(pcomp.PushingFuel);
        }

        var paint = made.TryGetComp<CompColorable>();
        if (paint != null && pcomp.SavedHasPaint)
            paint.SetColor(pcomp.SavedDrawColor);

        pcomp.Detach();
        pcomp = null;
        proxy.Destroy(DestroyMode.Vanish);

        return made;
    }
    static void ApplySlowdownHediff(Pawn p, float severity)
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

    static void RemoveSlowdownHediff(Pawn p)
    {
        var h = p.health.hediffSet.GetFirstHediffOfDef(RimgateDefOf.Rimgate_PushingCart);
        if (h != null) p.health.RemoveHediff(h);
    }

    public void StartPushVisual(
        Pawn pawn,
        ThingDef cartDef,
        Color drawA,
        Color drawB,
        float offset = 1.0f)
    {
        if (_pushVisual == null || _pushVisual.Destroyed)
            StopPushVisual();

        _pushVisual = GenSpawn.Spawn(RimgateDefOf.Rimgate_PushedCartVisual,
        pawn.Position,
        pawn.Map,
        WipeMode.Vanish) as Thing_PushedCartVisual;

        _pushVisual.Init(cartDef, offset, drawA, drawB);
        _pushVisual.AttachTo(pawn);
    }

    public void StopPushVisual()
    {
        if (_pushVisual != null && !_pushVisual.Destroyed)
            _pushVisual.Destroy(DestroyMode.Vanish);
        _pushVisual = null;
    }
}
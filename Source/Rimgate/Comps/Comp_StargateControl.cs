using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_StargateControl : ThingComp
{
    private const int _glowRadius = 10;

    private const int _idleTimeout = 2500;

    public int TicksSinceBufferUnloaded;

    public int TicksSinceOpened;

    public PlanetTile GateAddress;

    public bool IsActive = false;

    public bool IsReceivingGate;

    public bool HasIris = false;

    public bool HasPower = false;

    public int TicksUntilOpen => _ticksUntilOpen;

    public int ExternalHoldCount => _externalHoldCount;

    public CompProperties_StargateControl Props => (CompProperties_StargateControl)props;

    public Graphic StargatePuddle => _stargatePuddle ??= Props.puddleGraphicData.Graphic;

    public Graphic StargateIris => _stargateIris ??= Props.irisGraphicData.Graphic;

    public Graphic ChevronHighlight => _chevronHighlight ??= Props.chevronHighlight.Graphic;

    public bool GateIsLoading
    {
        get
        {
            return Parent?.Transporter?.LoadingInProgressOrReadyToLaunch == true
                && Parent?.Transporter?.AnyInGroupHasAnythingLeftToLoad == true;
        }
    }

    public IEnumerable<IntVec3> VortexCells
    {
        get
        {
            var rot = parent.Rotation;
            if (rot == Rot4.North) // default is for north facing
            {
                foreach (IntVec3 offset in Props.vortexPattern)
                    yield return offset + parent.Position;
                yield break;
            }

            foreach (var off in Props.vortexPattern)
                yield return parent.Position + Utils.RotateOffset(off, rot);
        }
    }

    public bool IsIrisActivated => _isIrisActivated;

    public bool WantsIrisToggled => _wantsIrisToggled;

    public PlanetTile ConnectedAddress = PlanetTile.Invalid;

    public Building_Stargate Parent => parent as Building_Stargate;

    public Building_Stargate ConnectedGate;

    public Sustainer PuddleSustainer;

    public Texture2D ToggleIrisIcon => _cachedIrisToggleIcon ??= ContentFinder<Texture2D>.Get(Props.irisGraphicData.texPath, true);

    private int _externalHoldCount;

    private int _ticksUntilOpen = -1;

    private List<Thing> _sendBuffer;

    private HashSet<int> _redraftOnArrival;

    private Queue<Thing> _recvBuffer;

    private bool _isIrisActivated = false;

    private bool _wantsIrisToggled;

    private PlanetTile _queuedAddress;

    private Graphic _stargatePuddle;

    private Graphic _stargateIris;

    private Graphic _chevronHighlight;

    private Texture2D _cachedIrisToggleIcon;

    #region DHD Controls

    public void QueueOpen(PlanetTile address, int delay)
    {
        _queuedAddress = address;
        _ticksUntilOpen = delay;
    }

    private void Open(PlanetTile address)
    {
        if (!address.Valid) return;

        MapParent site = Find.WorldObjects.MapParentAt(address);
        if (site == null)
        {
            Log.Error($"Rimgate :: stargate address at {address} doesn't have an associated MapParent.");
            return;
        }

        if (!site.HasMap)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: generating map for {site} using {site.def.defName}");

            LongEventHandler.QueueLongEvent(delegate
            {
                GetOrGenerateMapUtility.GetOrGenerateMap(
                    site.Tile,
                    site is WorldObject_StargateTransitSite
                            ? RimgateMod.MinMapSize
                            : Find.World.info.initialMapSize,
                    null);

            },
            "RG_GeneratingStargateSite",
            false,
            GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap,
            callback: delegate
            {
                if (RimgateMod.Debug)
                    Log.Message($"Rimgate :: finished generating map");

                FinalizeOpen(address, site.Map);
            });
        }
        else
        {
            if (site is WorldObject_StargateQuestSite wos)
                wos.ToggleSiteMap();
            FinalizeOpen(address, site.Map);
        }
    }

    private void FinalizeOpen(PlanetTile address, Map map)
    {
        Building_Stargate gate = GetConnectedGate(map);
        bool invalid = gate == null
            || gate.TryGetComp<Comp_StargateControl>().IsActive;
        if (invalid)
        {
            Messages.Message(
                "RG_GateDialFailed".Translate(),
                MessageTypeDefOf.NegativeEvent);
            RimgateDefOf.Rimgate_StargateFailDial.PlayOneShot(SoundInfo.InMap(parent));
            return;
        }

        IsActive = true;
        ConnectedAddress = address;

        if (ConnectedAddress.Valid)
        {
            ConnectedGate = gate;

            Comp_StargateControl otherComp = ConnectedGate.GateControl;
            otherComp.IsActive = true;
            otherComp.IsReceivingGate = true;
            otherComp.ConnectedAddress = GateAddress;
            otherComp.ConnectedGate = Parent;

            otherComp.EvacuateVortexPath();

            otherComp.PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(otherComp.parent));
            RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(otherComp.parent));

            if (otherComp.Parent.Glower != null)
            {
                otherComp.Parent.Glower.Props.glowRadius = _glowRadius;
                otherComp.Parent.Glower.PostSpawnSetup(false);
            }
        }

        PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(parent));

        if (Parent.Glower != null)
        {
            Parent.Glower.Props.glowRadius = _glowRadius;
            Parent.Glower.PostSpawnSetup(false);
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: finished opening gate {parent}");
    }

    public void PushExternalHold() => _externalHoldCount++;

    public void PopExternalHold()
    {
        if (_externalHoldCount > 0)
            _externalHoldCount--;
    }

    public void ForceLocalOpenAsReceiver()
    {
        if (IsActive && IsReceivingGate) return;

        IsActive = true;
        IsReceivingGate = true;
        ConnectedAddress = PlanetTile.Invalid;
        ConnectedGate = null;  // local-only, no remote

        EvacuateVortexPath();

        PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(parent));
        if (Parent?.Glower != null)
        {
            Parent.Glower.Props.glowRadius = _glowRadius;
            Parent.Glower.PostSpawnSetup(false);
        }
    }

    public void CloseStargate(bool closeOtherGate = false)
    {
        if (Parent == null || (!IsActive && _externalHoldCount == 0))
            return;

        Parent.Transporter?.CancelLoad();

        // clear buffers just in case
        var drop = Utils.BestDropCellNearThing(parent);

        if (_sendBuffer?.Any() == true)
        {
            foreach (Thing thing in _sendBuffer)
                GenSpawn.Spawn(thing, drop, parent.Map);
            _sendBuffer.Clear();
        }

        if (_recvBuffer?.Any() == true)
        {
            foreach (Thing thing in _recvBuffer)
                GenSpawn.Spawn(thing, drop, parent.Map);
            _recvBuffer.Clear();
        }

        _redraftOnArrival?.Clear();

        Comp_StargateControl otherSgComp = null;
        if (closeOtherGate)
        {
            otherSgComp = ConnectedGate.GateControl;
            if (ConnectedGate == null || otherSgComp == null)
            {
                Log.Warning($"Rimgate :: Recieving stargate connected to stargate {parent.ThingID} didn't have CompStargate, but this stargate wanted it closed.");
            }
            else
                otherSgComp.CloseStargate();
        }

        SoundDef puddleCloseDef = RimgateDefOf.Rimgate_StargateClose;
        puddleCloseDef.PlayOneShot(SoundInfo.InMap(parent));
        if (otherSgComp != null)
            puddleCloseDef.PlayOneShot(SoundInfo.InMap(otherSgComp.parent));

        PuddleSustainer?.End();

        if (Parent.Glower != null)
        {
            Parent.Glower.Props.glowRadius = 0;
            Parent.Glower.PostSpawnSetup(false);
        }

        if (Props.explodeOnUse)
        {
            if (Parent.Explosive == null)
                Log.Warning($"Rimgate :: Stargate {parent.ThingID} has the explodeOnUse tag set to true but doesn't have CompExplosive.");
            else
                Parent.Explosive.StartWick();
        }

        IsActive = false;
        TicksSinceBufferUnloaded = 0;
        TicksSinceOpened = 0;
        ConnectedAddress = PlanetTile.Invalid;
        ConnectedGate = null;
        IsReceivingGate = false;
    }

    #endregion

    private Building_Stargate GetConnectedGate(Map map)
    {
        var gate = Building_Stargate.GetStargateOnMap(map);
        // ensure a valid gate and a DHD exist(and link)
        if (gate == null)
        {
            gate = StargateUtil.PlaceRandomGate(map);
            StargateUtil.EnsureDhdNearGate(map, gate);
        }

        return gate;
    }

    private void PlayTeleportSound()
    {
        DefDatabase<SoundDef>.GetNamed($"Rimgate_StargateTeleport0{Rand.RangeInclusive(1, 4)}").PlayOneShot(SoundInfo.InMap(parent));
    }

    private void DoUnstableVortex()
    {
        var map = parent.Map;
        if (map is null) return;

        var dam = RimgateDefOf.Rimgate_KawooshExplosion;
        // Ignore only the gate itself so we don't nuke the building
        List<Thing> ignored = new(1) { parent };

        // Small radius: exactly the cell the vortex passes through
        const float radius = 0.51f;

        foreach (var cell in VortexCells)
        {
            if (!cell.InBounds(map)) continue;

            GenExplosion.DoExplosion(
                center: cell,
                map: map,
                radius: radius,
                damType: dam,
                instigator: parent,
                damAmount: dam.defaultDamage,
                armorPenetration: dam.defaultArmorPenetration,
                explosionSound: dam.soundExplosion,
                ignoredThings: ignored,
                damageFalloff: false
            );
        }
    }

    public void AddToSendBuffer(Thing thing)
    {
        _sendBuffer ??= new();
        _sendBuffer.Add(thing);

        // redraft flag travels as an ID on the destination comp
        Comp_StargateControl dest = ConnectedGate?.GateControl;
        if (dest != null && ShouldRedraftAfterSpawn(thing))
            dest.MarkRedraftOnArrival(thing);

        PlayTeleportSound();
    }

    private void BeamSendBufferTo()
    {
        Comp_StargateControl dest = ConnectedGate.GateControl;

        for (int i = 0; i < _sendBuffer.Count; i++)
        {
            Thing t = _sendBuffer[i];

            if (IsReceivingGate)
            {
                if (!t.DestroyedOrNull())
                    t.Kill();
                continue;
            }

            dest.AddToReceiveBuffer(t);
        }

        _sendBuffer.Clear();
    }

    private static bool ShouldRedraftAfterSpawn(Thing t)
    {
        if (t is not Pawn p) return false;
        if (!p.Faction.IsOfPlayerFaction()) return false;
        if (p.Downed) return false;
        if (p.drafter == null) return false;
        if (!p.Drafted) return false;
        return true;
    }

    public void MarkRedraftOnArrival(Thing t)
    {
        _redraftOnArrival ??= new();
        _redraftOnArrival.Add(t.thingIDNumber);
    }

    public void AddToReceiveBuffer(Thing thing)
    {
        _recvBuffer ??= new();
        _recvBuffer.Enqueue(thing);
    }

    private void SpawnFromReceiveBuffer()
    {
        TicksSinceBufferUnloaded = 0;

        // check without removing
        Thing t = _recvBuffer.Peek();
        if (t == null || t.Destroyed)
        {
            _recvBuffer.Dequeue();
            return;
        }

        if (!_isIrisActivated)
        {
            // Buildings (e.g., carts) must NOT spawn exactly on the gate/edifices.
            IntVec3 drop = Utils.BestDropCellNearThing(parent);
            var spawned = GenSpawn.Spawn(t, drop, parent.Map);
            PlayTeleportSound();

            // Cleanly re-draft on arrival
            if (spawned is Pawn p && _redraftOnArrival?.Remove(p.thingIDNumber) == true)
            {
                // avoid lingering pre-teleport jobs
                p.jobs?.StopAll();
                if (p.drafter != null)
                    p.drafter.Drafted = true;
                // hold position for a tick; avoids wander
                p.pather?.StopDead();
            }
        }
        else
        {
            if (!t.DestroyedOrNull())
                t.Kill();
            RimgateDefOf.Rimgate_IrisHit.PlayOneShot(SoundInfo.InMap(parent));
        }

        // remove after handling
        _recvBuffer.Dequeue();
    }

    #region Comp Overrides

    public override void PostDraw()
    {
        base.PostDraw();

        var rot = parent.Rotation;
        var drawOffset = parent.def.graphicData.DrawOffsetForRot(rot);

        var posBelow = parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingBelowTop) + drawOffset;
        var posAbove = parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Item) + drawOffset;

        // Puddle is slightly below the iris.
        if (IsActive)
            StargatePuddle.Draw(Utils.AddY(posBelow, -0.02f), rot, parent);

        // Iris sits a bit above the puddle.
        if (_isIrisActivated)
            StargateIris.Draw(Utils.AddY(posBelow, -0.01f), rot, parent);

        // Chevron highlight floats above the gate/puddle/iris.
        if (IsActive)
            ChevronHighlight.Draw(Utils.AddY(posAbove, +0.01f), rot, parent);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (_ticksUntilOpen > 0)
        {
            _ticksUntilOpen--;
            if (_ticksUntilOpen == 0)
            {
                _ticksUntilOpen = -1;
                Open(_queuedAddress);
                _queuedAddress = PlanetTile.Invalid;
            }
        }

        if (!IsActive) return;

        if (!_isIrisActivated && TicksSinceOpened < 150 && TicksSinceOpened % 10 == 0)
            DoUnstableVortex();

        if (parent.Fogged())
            FloodFillerFog.FloodUnfog(parent.Position, parent.Map);

        if (Parent.Transporter != null)
        {
            Thing thing = Parent.Transporter.innerContainer.FirstOrFallback();
            if (thing != null)
            {
                if (thing.Spawned)
                    thing.DeSpawn();

                AddToSendBuffer(thing);
                Parent.Transporter.innerContainer.Remove(thing);
            }
            else if (Parent.Transporter.LoadingInProgressOrReadyToLaunch
                && !Parent.Transporter.AnyInGroupHasAnythingLeftToLoad)
            {
                Parent.Transporter.CancelLoad();
            }
        }

        if (_sendBuffer?.Any() == true)
            BeamSendBufferTo();

        if (_recvBuffer?.Any() == true)
        {
            if (TicksSinceBufferUnloaded > Rand.Range(10, 80))
                SpawnFromReceiveBuffer();
        }
        else
        {
            // close out early
            // (i.e., raid arrivals, game conditions)
            if (!ConnectedAddress.Valid
                && _externalHoldCount == 0)
            {
                CloseStargate();
                return;
            }
        }

        TicksSinceBufferUnloaded++;
        TicksSinceOpened++;

        bool otherLoading = ConnectedGate?.GateControl.GateIsLoading == true;

        bool shouldClose = IsReceivingGate
            && TicksSinceBufferUnloaded > _idleTimeout
            && !otherLoading;

        if (shouldClose)
            CloseStargate(true);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        GateAddress = parent.Map.Tile;
        Find.World.GetComponent<WorldComp_StargateAddresses>().AddAddress(GateAddress);

        if (IsActive)
        {
            if (ConnectedGate == null && ConnectedAddress.Valid)
            {
                MapParent site = Find.WorldObjects.MapParentAt(ConnectedAddress);
                if (site.HasMap)
                    ConnectedGate = GetConnectedGate(site.Map);
                else
                    CloseStargate();
            }

            if (ConnectedGate != null || _externalHoldCount > 0)
                PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle
                    .TrySpawnSustainer(SoundInfo.InMap(parent));
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: compsg postspawnssetup:"
                + $" sgactive={IsActive},"
                + $" connectgate={ConnectedGate},"
                + $" connectaddress={ConnectedAddress},"
                + $" mapparent={parent.Map.Parent}");
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (Props.canHaveIris && HasIris)
        {
            var action = (_isIrisActivated
                ? "RG_OpenIris"
                : "RG_CloseIris").Translate();
            Command_Toggle command = new Command_Toggle
            {
                defaultLabel = "RG_ToggleIris".Translate(action),
                defaultDesc = "RG_ToggleIrisDesc".Translate(action),
                icon = ToggleIrisIcon,
                isActive = () => _wantsIrisToggled,
                toggleAction = delegate
                {
                    _wantsIrisToggled = !_wantsIrisToggled;

                    var dm = parent?.Map?.designationManager;
                    if (dm == null) return;

                    Designation designation = dm.DesignationOn(parent, RimgateDefOf.Rimgate_DesignationToggleIris);
                    if (designation == null)
                        dm.AddDesignation(new Designation(parent, RimgateDefOf.Rimgate_DesignationToggleIris));
                    else
                        designation?.Delete();
                }
            };

            if (!HasPower)
                command.Disable("PowerNotConnected".Translate());

            yield return command;
        }

        if (!Prefs.DevMode)
            yield break;

        Command_Action commandDevMode = new Command_Action
        {
            defaultLabel = "Add/remove iris",
            action = delegate ()
            {
                HasIris = !HasIris;
            }
        };

        yield return commandDevMode;

        commandDevMode = new Command_Action
        {
            defaultLabel = "Force close",
            defaultDesc = "Force close this gate to hopefully remove strange behaviours (this will not close gate at the other end).",
            action = delegate ()
            {
                CloseStargate();
                Log.Message($"Rimgate :: Stargate {parent.ThingID} was force-closed.");
            }
        };

        yield return commandDevMode;
    }

    public override void PostDeSpawn(Map previousMap, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(previousMap);
        CleanupGate();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        CleanupGate();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref IsActive, "StargateIsActive");
        Scribe_Values.Look(ref _externalHoldCount, "ExternalHoldCount");
        Scribe_Values.Look(ref IsReceivingGate, "IsRecievingGate");
        Scribe_Values.Look(ref HasIris, "HasIris");
        Scribe_Values.Look(ref _isIrisActivated, "_irisIsActivated");
        Scribe_Values.Look(ref _wantsIrisToggled, "_wantsIrisToggled");
        Scribe_Values.Look(ref TicksSinceOpened, "TicksSinceOpened");
        Scribe_Values.Look(ref ConnectedAddress, "ConnectedAddress");
        Scribe_Values.Look(ref ConnectedAddress, "ConnectedAddress");
        Scribe_References.Look(ref ConnectedGate, "ConnectedStargate");

        // --- SEND buffer (List<Thing>) ---
        Scribe_Collections.Look(ref _sendBuffer, "_sendBuffer", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            _sendBuffer = _sendBuffer?.Where(t => t != null).ToList() ?? new List<Thing>();

        // --- RECV buffer (Queue<Thing>) via temp list ---
        List<Thing> recvList = null;
        if (Scribe.mode == LoadSaveMode.Saving)
            recvList = _recvBuffer?.Where(t => t != null).ToList();

        Scribe_Collections.Look(ref recvList, "_recvBuffer_list", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            _recvBuffer = new Queue<Thing>(recvList?.Where(t => t != null) ?? Enumerable.Empty<Thing>());

        // --- Redraft set (HashSet<int>) via temp list ---
        List<int> redraftList = null;
        if (Scribe.mode == LoadSaveMode.Saving && _redraftOnArrival != null)
            redraftList = _redraftOnArrival.ToList();

        Scribe_Collections.Look(ref redraftList, "redraftOnArrival_list", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            _redraftOnArrival = redraftList != null ? new HashSet<int>(redraftList) : null;
    }

    public override string CompInspectStringExtra()
    {
        return base.CompInspectStringExtra() + "RG_RespawnGateString".Translate();
    }

    #endregion

    public string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        string address = StargateUtil.GetStargateDesignation(GateAddress);
        sb.AppendLine("RG_GateAddress".Translate(address));
        if (!IsActive)
            sb.AppendLine("InactiveFacility".Translate().CapitalizeFirst());
        else
        {
            string connectAddress = StargateUtil.GetStargateDesignation(ConnectedAddress);
            var connectLabel = (IsReceivingGate
                ? "RG_IncomingConnection"
                : "RG_OutgoingConnection").Translate();
            sb.AppendLine("RG_ConnectedToGate".Translate(connectAddress, connectLabel));
        }

        if (HasIris)
        {
            var irisLabel = (_isIrisActivated
                ? "RG_IrisClosedStatus"
                : "RG_IrisOpenStatus").Translate();
            sb.AppendLine("RG_IrisStatus".Translate(irisLabel));
        }

        if (_ticksUntilOpen > 0)
            sb.AppendLine("RG_TimeUntilGateLock".Translate(_ticksUntilOpen.ToStringTicksToPeriod()));

        return sb.ToString().TrimEndNewlines();
    }

    public void ToggleIris()
    {
        _isIrisActivated = !_isIrisActivated;
    }

    public void DoToggleIris()
    {
        _isIrisActivated = !_isIrisActivated;
        _wantsIrisToggled = false;
        var snd = _isIrisActivated ? RimgateDefOf.Rimgate_IrisClose
                                   : RimgateDefOf.Rimgate_IrisOpen;
        snd.PlayOneShot(SoundInfo.InMap(parent));
    }

    public void CleanupGate()
    {
        CloseStargate(ConnectedGate != null);
        Find.World.GetComponent<WorldComp_StargateAddresses>().RemoveAddress(GateAddress);
    }

    private void EvacuateVortexPath()
    {
        var map = parent.Map;
        if (map == null) return;

        // Hash the path and mark reserved
        // so we never place pawns back into it.
        var vortexSet = new HashSet<IntVec3>();
        foreach (var c in VortexCells)
            if (c.InBounds(map)) vortexSet.Add(c);

        if (vortexSet.Count == 0) return;

        // Collect pawns currently standing on any vortex cell
        var pawnsToMove = new List<Pawn>();
        foreach (var cell in vortexSet)
        {
            var things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
                if (things[i] is Pawn p && p.Spawned && !p.Dead) pawnsToMove.Add(p);
        }

        if (pawnsToMove.Count == 0) return;

        // Keep track of targets we’ve picked to avoid stacking multiple pawns
        var reserved = new HashSet<IntVec3>(vortexSet);

        foreach (var p in pawnsToMove)
        {
            // Prefer same-room tiles if the pawn is in a room;
            // otherwise any standable tile works.
            Room room = p.Position.GetRoom(map);

            bool IsGood(IntVec3 c) =>
                c.InBounds(map)
                && c.Walkable(map)
                && !reserved.Contains(c)
                && !vortexSet.Contains(c)
                && (room == null || c.GetRoom(map) == room);

            bool IsOkay(IntVec3 c) =>
                c.InBounds(map)
                && c.Walkable(map)
                && !reserved.Contains(c)
                && !vortexSet.Contains(c);

            IntVec3 best = IntVec3.Invalid;

            // Try to find a nearby safe cell
            foreach (var c in GenRadial.RadialCellsAround(p.Position, 9, true))
            {
                if (IsGood(c))
                {
                    best = c;
                    break;
                }
            }

            if (!best.IsValid)
            {
                foreach (var c in GenRadial.RadialCellsAround(p.Position, 9, true))
                {
                    if (IsOkay(c)) 
                    { 
                        best = c;
                        break;
                    }
                }
            }

            if (!best.IsValid)
            {
                // Worst case: drop somewhere near the gate
                // but off the vortex
                best = Utils.BestDropCellNearThing(parent);
                if (vortexSet.Contains(best)) best = parent.Position;
            }

            // Move the pawn and clear any current jobs/path
            // to avoid rubber-banding back
            p.Position = best;
            p.pather?.StopDead();
            p.jobs?.StopAll();

            reserved.Add(best);
        }
    }
}

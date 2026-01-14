using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.Code;

namespace Rimgate;

public class Building_Stargate_Ext : DefModExtension
{
    public bool canHaveIris = true;

    public float irisPowerConsumption = 0;

    public bool explodeOnUse = false;

    public GraphicData puddleGraphicData;

    public GraphicData irisGraphicData;

    public GraphicData chevronHighlight;

    public List<IntVec3> vortexPattern = new List<IntVec3>
    {
        new IntVec3(0,0,1),
        new IntVec3(1,0,1),
        new IntVec3(-1,0,1),
        new IntVec3(0,0,0),
        new IntVec3(1,0,0),
        new IntVec3(-1,0,0),
        new IntVec3(0,0,-1),
        new IntVec3(1,0,-1),
        new IntVec3(-1,0,-1),
        new IntVec3(0,0,-2),
        new IntVec3(1,0,-2),
        new IntVec3(-1,0,-2),
        new IntVec3(0,0,-3)
    };

    public List<SoundDef> teleportSounds;
}

public class Building_Stargate : Building
{
    public Building_Stargate_Ext Props => _cachedProps ??= def.GetModExtension<Building_Stargate_Ext>();

    private const int GlowRadius = 10;

    private const int IdleTimeout = 2500;

    private const int UnstableVortexInterval = 150;

    #region Fields and properties

    public int TicksSinceBufferUnloaded;

    public int TicksSinceOpened;

    public PlanetTile GateAddress;

    public bool IsActive = false;

    public bool IsReceivingGate;

    public bool HasIris = false;

    public PlanetTile ConnectedAddress = PlanetTile.Invalid;

    public Building_Stargate ConnectedGate;

    public Sustainer PuddleSustainer;

    public Graphic StargatePuddle => Props.puddleGraphicData?.Graphic;

    public Graphic StargateIris => Props.irisGraphicData?.Graphic;

    public Graphic ChevronHighlight => Props.chevronHighlight?.Graphic;

    public IEnumerable<IntVec3> VortexCells
    {
        get
        {
            var rot = Rotation;
            if (rot == Rot4.North) // default is for north facing
            {
                foreach (IntVec3 offset in Props.vortexPattern)
                    yield return offset + Position;
                yield break;
            }

            foreach (var off in Props.vortexPattern)
                yield return Position + Utils.RotateOffset(off, rot);
        }
    }

    public Texture2D ToggleIrisIcon => _cachedIrisToggleIcon ??= ContentFinder<Texture2D>.Get(Props.irisGraphicData.texPath, true);

    public bool Powered => PowerTrader == null || PowerTrader.PowerOn;

    public int TicksUntilOpen => _ticksUntilOpen;

    public int ExternalHoldCount => _externalHoldCount;

    public bool GateIsLoading
    {
        get
        {
            return Transporter != null
                && Transporter.LoadingInProgressOrReadyToLaunch
                && Transporter.AnyInGroupHasAnythingLeftToLoad;
        }
    }

    public bool IsIrisActivated => _isIrisActivated;

    public bool WantsIrisToggled => _wantsIrisToggled;

    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= GetComp<CompPowerTrader>();

    public CompTransporter Transporter => _cachedTransporter ??= GetComp<CompTransporter>();

    public CompGlower Glower => _cachedGlowComp ??= GetComp<CompGlower>();

    public CompExplosive Explosive => _cachedexplosiveComp ??= GetComp<CompExplosive>();

    private Building_Stargate_Ext _cachedProps;

    private int _externalHoldCount;

    private int _ticksUntilOpen = -1;

    private List<Thing> _sendBuffer;

    private HashSet<int> _redraftOnArrival;

    private Queue<Thing> _recvBuffer;

    private bool _isIrisActivated = false;

    private bool _wantsIrisToggled;

    private PlanetTile _queuedAddress;

    private CompPowerTrader _cachedPowerTrader;

    private CompTransporter _cachedTransporter;

    private CompGlower _cachedGlowComp;

    private CompExplosive _cachedexplosiveComp;

    private Texture2D _cachedIrisToggleIcon;

    #endregion

    #region Building overrides and lifecycle

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        if(GetStargateOnMap(map) != null)
        {
            Log.Warning($"Rimgate :: Attempted to spawn a second active stargate {this} on map {map}. Destroying the new instance.");
            Destroy(DestroyMode.Vanish);
            return;
        }

        base.SpawnSetup(map, respawningAfterLoad);

        // prevent nullreferenceexception in-case innercontainer disappears
        if (Transporter != null && Transporter.innerContainer == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: attempting to fix null container for {this.ThingID}");
            _cachedTransporter.innerContainer = new ThingOwner<Thing>(_cachedTransporter);
        }

        GateAddress = Map?.Tile ?? PlanetTile.Invalid;
        if(GateAddress.Valid)
            StargateUtil.AddGateAddress(GateAddress);

        if (IsActive)
        {
            if (ConnectedGate == null && ConnectedAddress.Valid)
            {
                MapParent site = Find.WorldObjects.MapParentAt(ConnectedAddress);
                if (!respawningAfterLoad && site.HasMap)
                    ConnectedGate = GetConnectedGate(site.Map);
                else
                    CloseStargate();
            }

            if (ConnectedGate != null || _externalHoldCount > 0)
                PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle
                    .TrySpawnSustainer(SoundInfo.InMap(this));
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: spawnssetup:"
                + $" sgactive={IsActive},"
                + $" connectgate={ConnectedGate},"
                + $" connectaddress={ConnectedAddress},"
                + $" mapparent={Map.Parent}");
    }

    protected override void Tick()
    {
        base.Tick();

        if (PowerTrader != null)
        {
            if (HasIris)
            {
                float powerConsumption = -(Props.irisPowerConsumption + PowerTrader.Props.PowerConsumption);
                PowerTrader.PowerOutput = powerConsumption;

                if (!Powered && IsIrisActivated)
                    _isIrisActivated = !_isIrisActivated;
            }
            else
                PowerTrader.PowerOutput = -PowerTrader.Props.PowerConsumption;
        }

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

        if (!_isIrisActivated && TicksSinceOpened < UnstableVortexInterval && TicksSinceOpened % 10 == 0)
            DoUnstableVortex();

        if (this.Fogged())
            FloodFillerFog.FloodUnfog(Position, Map);

        if (Transporter != null)
        {
            Thing thing = Transporter.innerContainer.FirstOrFallback();
            if (thing != null)
            {
                if (thing.Spawned)
                    thing.DeSpawn();

                AddToSendBuffer(thing);
                Transporter.innerContainer.Remove(thing);
            }
            else if (Transporter.LoadingInProgressOrReadyToLaunch && !Transporter.AnyInGroupHasAnythingLeftToLoad)
                Transporter.CancelLoad();
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
            if (!ConnectedAddress.Valid && _externalHoldCount == 0)
            {
                CloseStargate(ConnectedGate != null);
                return;
            }
        }

        TicksSinceBufferUnloaded++;
        TicksSinceOpened++;

        bool otherLoading = ConnectedGate?.GateIsLoading == true;

        bool shouldClose = IsReceivingGate
            && TicksSinceBufferUnloaded > IdleTimeout
            && !otherLoading;

        if (shouldClose)
            CloseStargate(true);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        bool blockInteractions = IsActive || ExternalHoldCount > 0;
        string why = "RG_StargateHeldCannotReinstall".Translate();
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            if (gizmo is Command_LoadToTransporter && !IsActive)
                continue;

            if (gizmo is Designator_Install dInstall)
            {
                if (blockInteractions)
                    dInstall.Disable(why);
                else if (dInstall.Disabled)
                    dInstall.Disabled = false;
                yield return dInstall;
                continue;
            }

            yield return gizmo;
        }

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

                    var dm = Map?.designationManager;
                    if (dm == null) return;

                    Designation designation = dm.DesignationOn(this, RimgateDefOf.Rimgate_DesignationToggleIris);
                    if (designation == null)
                        dm.AddDesignation(new Designation(this, RimgateDefOf.Rimgate_DesignationToggleIris));
                    else
                        designation?.Delete();
                }
            };

            if (!Powered)
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
                CloseStargate(ConnectedGate != null);
                Log.Message($"Rimgate :: Stargate {this} was force-closed.");
            }
        };

        yield return commandDevMode;
    }

    // override to hide interaction cell
    public override void DrawExtraSelectionOverlays()
    {
        Blueprint_Install blueprint_Install = InstallBlueprintUtility.ExistingBlueprintFor(this);
        if (blueprint_Install != null)
            GenDraw.DrawLineBetween(this.TrueCenter(), blueprint_Install.TrueCenter());

        if (def.specialDisplayRadius > 0.1f)
            GenDraw.DrawRadiusRing(Position, def.specialDisplayRadius);

        if (!def.drawPlaceWorkersWhileSelected || def.PlaceWorkers == null) return;

        for (int i = 0; i < def.PlaceWorkers.Count; i++)
            def.PlaceWorkers[i].DrawGhost(def, Position, Rotation, Color.white, this);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        var rot = Rotation;
        var drawOffset = def.graphicData.DrawOffsetForRot(rot);

        var posBelow = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingBelowTop) + drawOffset;
        var posAbove = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Item) + drawOffset;

        // Puddle is slightly below the iris.
        if (IsActive)
            StargatePuddle?.Draw(Utils.AddY(posBelow, -0.02f), rot, this);

        // Iris sits a bit above the puddle.
        if (_isIrisActivated)
            StargateIris?.Draw(Utils.AddY(posBelow, -0.01f), rot, this);

        // Chevron highlight floats above the gate/puddle/iris.
        if (IsActive)
            ChevronHighlight?.Draw(Utils.AddY(posAbove, +0.01f), rot, this);
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();

        if (!GateAddress.Valid)
            return sb.Append("RG_RespawnGateString".Translate()).ToString();

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

        if (HasIris && PowerTrader != null)
            sb.AppendLine(PowerTrader.CompInspectStringExtra());

        return sb.ToString().TrimEndNewlines();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        CleanupGate();
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        CleanupGate();
        base.Destroy(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref IsActive, "IsActive");
        Scribe_Values.Look(ref _externalHoldCount, "_externalHoldCount");
        Scribe_Values.Look(ref IsReceivingGate, "IsRecievingGate");
        Scribe_Values.Look(ref HasIris, "HasIris");
        Scribe_Values.Look(ref _isIrisActivated, "_irisIsActivated");
        Scribe_Values.Look(ref _wantsIrisToggled, "_wantsIrisToggled");
        Scribe_Values.Look(ref TicksSinceOpened, "TicksSinceOpened");
        Scribe_Values.Look(ref ConnectedAddress, "ConnectedAddress");
        Scribe_Values.Look(ref ConnectedAddress, "ConnectedAddress");
        Scribe_References.Look(ref ConnectedGate, "ConnectedGate");

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

    #endregion

    #region Iris handling

    public void DoToggleIris()
    {
        _isIrisActivated = !_isIrisActivated;
        _wantsIrisToggled = false;
        var snd = _isIrisActivated ? RimgateDefOf.Rimgate_IrisClose
                                   : RimgateDefOf.Rimgate_IrisOpen;
        snd.PlayOneShot(SoundInfo.InMap(this));
    }

    #endregion

    #region Gate control

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushExternalHold() => _externalHoldCount++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(this));
        RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(this));
        if (Glower != null)
        {
            Glower.Props.glowRadius = GlowRadius;
            Glower.PostSpawnSetup(false);
        }
    }

    public void OpenAsReceivingGate(Building_Stargate connectedGate, PlanetTile connectedAddress)
    {
        if (!connectedAddress.Valid) return;

        IsActive = true;
        IsReceivingGate = true;
        ConnectedAddress = connectedAddress;
        ConnectedGate = connectedGate;

        if (Map == null || !Spawned) return;

        EvacuateVortexPath();

        PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(this));
        RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(this));

        if (Glower != null)
        {
            Glower.Props.glowRadius = GlowRadius;
            Glower.PostSpawnSetup(false);
        }
    }

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
                    site is WorldObject_GateTransitSite
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
            FinalizeOpen(address, site.Map);
    }

    private void FinalizeOpen(PlanetTile address, Map map)
    {
        Building_Stargate gate = GetConnectedGate(map);
        bool invalid = gate == null || gate.IsActive;
        if (invalid)
        {
            Messages.Message(
                "RG_GateDialFailed".Translate(),
                MessageTypeDefOf.NegativeEvent);
            RimgateDefOf.Rimgate_StargateFailDial.PlayOneShot(SoundInfo.InMap(this));
            return;
        }

        IsActive = true;
        ConnectedAddress = address;

        if (ConnectedAddress.Valid)
        {
            ConnectedGate = gate;
            ConnectedGate.OpenAsReceivingGate(this, GateAddress);
        }

        PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(this));
        RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(this));

        if (Glower != null)
        {
            Glower.Props.glowRadius = GlowRadius;
            Glower.PostSpawnSetup(false);
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: finished opening gate {this}");
    }

    public void CloseStargate(bool closeOtherGate = false)
    {
        if (!IsActive && _externalHoldCount == 0)
            return;

        Transporter?.CancelLoad();

        // clear buffers just in case
        var drop = Utils.BestDropCellNearThing(this);

        var dropMap = Map;
        if (dropMap == null && ConnectedGate?.Map != null)
            dropMap = ConnectedGate.Map;

        if (dropMap != null)
        {
            if (_sendBuffer?.Any() == true)
            {
                foreach (Thing thing in _sendBuffer)
                    GenSpawn.Spawn(thing, drop, dropMap);
            }

            if (_recvBuffer?.Any() == true)
            {
                foreach (Thing thing in _recvBuffer)
                    GenSpawn.Spawn(thing, drop, dropMap);
            }
        }

        _sendBuffer?.Clear();
        _recvBuffer?.Clear();
        _redraftOnArrival?.Clear();

        if (closeOtherGate)
        {
            if (ConnectedGate == null)
                Log.Warning($"Rimgate :: Recieving stargate connected to stargate {this} doesn't exist, but this stargate wanted it closed.");
            else
                ConnectedGate.CloseStargate();
        }

        SoundDef puddleCloseDef = RimgateDefOf.Rimgate_StargateClose;
        puddleCloseDef.PlayOneShot(SoundInfo.InMap(this));
        if (ConnectedGate != null)
            puddleCloseDef.PlayOneShot(SoundInfo.InMap(ConnectedGate));

        PuddleSustainer?.End();

        if (Spawned)
        {
            if (Glower != null)
            {
                Glower.Props.glowRadius = 0;
                Glower.PostSpawnSetup(false);
            }

            if (Props.explodeOnUse)
            {
                if (Explosive == null)
                    Log.Warning($"Rimgate :: Stargate {this} has the explodeOnUse tag set to true but doesn't have CompExplosive.");
                else
                    Explosive.StartWick();
            }
        }

        IsActive = false;
        TicksSinceBufferUnloaded = 0;
        TicksSinceOpened = 0;
        ConnectedAddress = PlanetTile.Invalid;
        ConnectedGate = null;
        IsReceivingGate = false;
    }

    public void AddToSendBuffer(Thing thing)
    {
        _sendBuffer ??= new();
        _sendBuffer.Add(thing);

        // redraft flag travels as an ID on the destination comp
        if (ConnectedGate != null && ShouldRedraftAfterSpawn(thing))
            ConnectedGate.MarkRedraftOnArrival(thing);

        PlayTeleportSound();
    }

    private void BeamSendBufferTo()
    {
        for (int i = 0; i < _sendBuffer.Count; i++)
        {
            Thing t = _sendBuffer[i];

            if (IsReceivingGate)
            {
                if (!t.DestroyedOrNull())
                    t.Kill();
                continue;
            }

            ConnectedGate.AddToReceiveBuffer(t);
        }

        _sendBuffer.Clear();
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
            IntVec3 drop = Utils.BestDropCellNearThing(this);
            var spawned = GenSpawn.Spawn(t, drop, Map);
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
            RimgateDefOf.Rimgate_IrisHit.PlayOneShot(SoundInfo.InMap(this));
        }

        // toggle site map if applicable
        if (Map.Parent is WorldObject_GateQuestSite wos)
            wos.ToggleSiteMap();

        // remove after handling
        _recvBuffer.Dequeue();
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

    public void CleanupGate()
    {
        CloseStargate(ConnectedGate != null);
        StargateUtil.RemoveGateAddress(GateAddress);
    }

    private void PlayTeleportSound()
    {
        if (TryGetTeleportSound(out SoundDef def))
            def.PlayOneShot(SoundInfo.InMap(this));
    }

    private void DoUnstableVortex()
    {
        var map = Map;
        if (map is null) return;

        var dam = RimgateDefOf.Rimgate_KawooshExplosion;
        // Ignore only the gate itself so we don't nuke the building
        List<Thing> ignored = new(1) { this };

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
                instigator: this,
                damAmount: dam.defaultDamage,
                armorPenetration: dam.defaultArmorPenetration,
                explosionSound: dam.soundExplosion,
                ignoredThings: ignored,
                damageFalloff: false
            );
        }
    }

    #endregion

    #region Helpers

    private void EvacuateVortexPath()
    {
        var map = Map;
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
                best = Utils.BestDropCellNearThing(this);
                if (vortexSet.Contains(best)) best = Position;
            }

            // Move the pawn and clear any current jobs/path
            // to avoid rubber-banding back 
            p.pather?.StopDead();
            p.jobs?.StopAll();
            p.pather.StartPath(best, PathEndMode.OnCell);

            reserved.Add(best);
        }
    }

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

    private bool TryGetTeleportSound(out SoundDef def)
    {
        if (Props.teleportSounds == null || Props.teleportSounds.Count == 0)
        {
            def = null;
            return false;
        }

        def = Props.teleportSounds.RandomElement();
        return true;
    }

    #endregion

    #region Static utility

    public static Building_Stargate GetStargateOnMap(
        Map map,
        Thing thingToIgnore = null)
    {
        if (map == null) return null;

        Building_Stargate gateOnMap = null;
        foreach (Thing thing in map?.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing is Building_Stargate bsg)
            {
                gateOnMap = bsg;
                break;
            }
        }

        return gateOnMap;
    }

    #endregion
}

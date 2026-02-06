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

public class Building_Gate_Ext : DefModExtension
{
    public bool canHaveIris = true;

    public float irisPowerConsumption = 0;

    public bool explodeOnUse = false;

    public Color modActivatedColor = new Color(0.38f, 0.53f, 0.49f);

    public GraphicData puddleGraphicData;

    public GraphicData irisGraphicData;

    public GraphicData irisGlowGraphicData;

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

public class Building_Gate : Building
{
    public static readonly Dictionary<int, int> GlobalVortexEntryCellCache = new();

    public Building_Gate_Ext Props => _cachedProps ??= def.GetModExtension<Building_Gate_Ext>();

    private const int GlowRadius = 10;

    private static readonly IntRange IdleTimeoutRange = new IntRange(1900, 2500);

    private const int UnstableVortexInterval = 150;

    #region Fields and properties

    public int TicksSinceBufferUnloaded;

    public int TicksSinceOpened;

    public PlanetTile GateAddress;

    public bool IsActive = false;

    public bool IsReceivingGate;

    public bool HasIris = false;

    public PlanetTile ConnectedAddress = PlanetTile.Invalid;

    public Building_Gate ConnectedGate;

    public Sustainer PuddleSustainer;

    public Graphic GatePuddle => Props.puddleGraphicData?.Graphic;

    public Graphic GateIris => Props.irisGraphicData?.Graphic;

    private Mesh IrisGlowMesh => _cachedIrisGlowMesh ??= Props.irisGlowGraphicData?.Graphic.MeshAt(Rotation);

    private Material IrisGlowMat => _cachedIrisGlowMat ??= Props.irisGlowGraphicData?.Graphic.MatAt(Rotation, null);

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

    public bool Powered => PowerTrader == null || PowerTrader.PowerOn;

    public int TicksUntilOpen => _ticksUntilOpen;

    public bool IsOpeningQueued => _ticksUntilOpen > -1;

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

    public bool IsHomeGate
    {
        get
        {
            return Map?.Parent is not WorldObject_GateQuestSite
                && Map?.Parent is not WorldObject_GateTransitSite;
        }
    }

    public Building_DHD ConnectedDHD
    {
        get
        {
            var facilities = Facilities?.LinkedFacilitiesListForReading;
            if (facilities == null || facilities.Count <= 0) return null;

            return facilities.FirstOrDefault(f => f is Building_DHD && f.Spawned) as Building_DHD;
        }
    }

    public CompAffectedByFacilities Facilities => _cachedFacilities ??= GetComp<CompAffectedByFacilities>();

    public CompPowerTrader PowerTrader => _cachedPowerTrader ??= GetComp<CompPowerTrader>();

    public CompTransporter Transporter => _cachedTransporter ??= GetComp<CompTransporter>();

    public CompGlower Glower => _cachedGlowComp ??= GetComp<CompGlower>();

    public CompExplosive Explosive => _cachedexplosiveComp ??= GetComp<CompExplosive>();

    private Building_Gate_Ext _cachedProps;

    private int _externalHoldCount;

    private int _ticksUntilOpen = -1;

    private List<Thing> _sendBuffer;

    private HashSet<int> _redraftOnArrival;

    private Queue<Thing> _recvBuffer;

    private bool _isIrisActivated = false;

    private PlanetTile _queuedAddress;

    private CompAffectedByFacilities _cachedFacilities;

    private CompPowerTrader _cachedPowerTrader;

    private CompTransporter _cachedTransporter;

    private CompGlower _cachedGlowComp;

    private CompExplosive _cachedexplosiveComp;

    private Texture2D _cachedIrisToggleIcon;

    private bool _inert;

    private bool _gateConditionActive;

    private Material _cachedIrisGlowMat;

    private Mesh _cachedIrisGlowMesh;

    #endregion

    #region Building overrides and lifecycle

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        PlanetTile tile = map?.Tile ?? PlanetTile.Invalid;
        if (Destroyed || Spawned || tile == PlanetTile.Invalid)
        {
            Log.Error($"Rimgate :: Invalid gate address for gate {this} on map {map}. Destroying.");
            Destroy(DestroyMode.KillFinalize);
            return;
        }

        // only one active gate allowed per map
        // this should never happen normally, but just in-case let it spawn inert, then uninstall
        if (TryGetSpawnedGateOnMap(map, out _))
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: Attempted to spawn a second active gate {this} on map {map} - marking inert to prevent conflicts.");
            _inert = true;
            base.SpawnSetup(map, respawningAfterLoad);
            return;
        }

        GateAddress = tile;
        GateUtil.AddGateAddress(GateAddress);

        int key = map.uniqueID;
        // cache interaction cell, there should only be one gate per map
        GlobalVortexEntryCellCache[key] = map.cellIndices.CellToIndex(InteractionCell);

        base.SpawnSetup(map, respawningAfterLoad);

        // prevent nullreferenceexception in-case innercontainer disappears
        if (Transporter != null && Transporter.innerContainer == null)
        {
            if (RimgateMod.Debug)
                Log.Warning($"Rimgate :: attempting to fix null container for {this.ThingID}");
            _cachedTransporter.innerContainer = new ThingOwner<Thing>(_cachedTransporter);
        }

        if (IsActive)
        {
            if (ConnectedGate == null && ConnectedAddress.Valid)
            {
                MapParent site = Find.WorldObjects.MapParentAt(ConnectedAddress);
                if (!respawningAfterLoad && site.HasMap)
                {
                    if (!TryGetReceivingGate(site.Map, out ConnectedGate))
                    {
                        Log.Error($"Rimgate :: could not re-establish connection for gate {this} to address {ConnectedAddress} on map {site.Map}.");
                        CloseGate();
                        return;
                    }
                }
                else
                {
                    CloseGate();
                    return;
                }
            }

            if (ConnectedGate != null || _externalHoldCount > 0)
                PuddleSustainer = RimgateDefOf.Rimgate_GateIdle.TrySpawnSustainer(SoundInfo.InMap(this));
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: spawnssetup:"
                + $" sgactive={IsActive},"
                + $" connectgate={ConnectedGate},"
                + $" connectaddress={ConnectedAddress},"
                + $" mapparent={Map.Parent},"
                + $" isHomeGate={IsHomeGate}");
    }

    protected override void Tick()
    {
        if (!Spawned || this.IsMinified())
            return;

        if (_inert)
        {
            _inert = false;
            this.Uninstall(); // should be safe to uninstall now
            return;
        }

        if (this.IsHashIntervalTick(GenTicks.TickRareInterval))
        {
            var colorComp = GetComp<CompColorable>();
            if (GateUtil.ModificationEquipmentActive && !colorComp.Active)
                colorComp.SetColor(Props.modActivatedColor);
            else if (!GateUtil.ModificationEquipmentActive && colorComp.Active)
                colorComp.Disable();

            _gateConditionActive = GateUtil.IsGateConditionActive(Map);
        }

        base.Tick();

        if (PowerTrader != null)
        {
            if (HasIris)
            {
                float powerConsumption = -(Props.irisPowerConsumption + PowerTrader.Props.PowerConsumption);
                PowerTrader.PowerOutput = powerConsumption;
            }
            else
                PowerTrader.PowerOutput = -PowerTrader.Props.PowerConsumption;
        }

        int ticket = Map?.uniqueID ?? -1;
        bool ticking = IsOpeningQueued;
        if (ticking)
        {
            _ticksUntilOpen--;
            if (_ticksUntilOpen < 0)
            {
                _ticksUntilOpen = -1;
                Open(_queuedAddress);
                _queuedAddress = PlanetTile.Invalid;
            }
        }

        if (!IsActive)
            return;

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
                CloseGate(ConnectedGate != null);
                return;
            }
        }

        TicksSinceBufferUnloaded++;
        TicksSinceOpened++;

        bool shouldClose = IsReceivingGate
            && (ConnectedGate == null || ConnectedGate.GateIsLoading == false)
            && TicksSinceBufferUnloaded > IdleTimeoutRange.RandomInRange;

        if (shouldClose)
            CloseGate(true);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        bool blockInteractions = IsActive || ExternalHoldCount > 0;
        string why = "RG_GateHeldCannotReinstall".Translate();
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
                CloseGate(ConnectedGate != null);
                Log.Message($"Rimgate :: {this} was force-closed.");
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

        var drawPos = Utils.AddY(drawLoc + drawOffset, AltitudeLayer.Blueprint.AltitudeFor());

        // Puddle is slightly below the iris.
        if (IsActive && !_isIrisActivated && GatePuddle != null)
            GatePuddle.Draw(Utils.AddY(drawPos, 0.01f), rot, this);

        // Iris sits a bit above the puddle.
        if (_isIrisActivated && GateIris != null)
        {
            var irisDrawPos = Utils.AddY(drawPos, 0.02f);
            GateIris.Draw(irisDrawPos, rot, this);

            if (_gateConditionActive)
                Graphics.DrawMesh(
                    IrisGlowMesh,
                    irisDrawPos,
                    Rotation.AsQuat,
                    FadedMaterialPool.FadedVersionOf(
                        IrisGlowMat,
                        0.5f),
                    0);
        }

        // Chevron highlight floats above the gate/puddle/iris.
        if ((IsOpeningQueued || IsActive) && ChevronHighlight != null)
            ChevronHighlight.Draw(Utils.AddY(drawPos, 0.01f), rot, this);
    }

    public override string GetInspectString()
    {
        if (this.IsMinified())
            return null;

        StringBuilder sb = new StringBuilder();

        if (!GateAddress.Valid)
            return sb.Append("RG_RespawnGateString".Translate()).ToString();

        string address = GateUtil.GetGateDesignation(GateAddress);
        sb.AppendLine("RG_GateAddress".Translate(address));
        if (GateUtil.ModificationEquipmentActive)
            sb.AppendLine("RG_ModificationEquipmentActive".Translate());

        if (!IsActive)
            sb.AppendLine("InactiveFacility".Translate().CapitalizeFirst());
        else
        {
            string connectAddress = GateUtil.GetGateDesignation(ConnectedAddress);
            var connectLabel = (IsReceivingGate
                ? "RG_IncomingConnection"
                : "RG_OutgoingConnection").Translate();
            sb.AppendLine("RG_ConnectedToGate".Translate(LabelCap, connectAddress, connectLabel));
        }

        if (HasIris)
        {
            var irisLabel = _isIrisActivated
                ? "RG_IrisClosedStatus".Translate()
                : "RG_IrisOpenStatus".Translate();
            sb.AppendLine("RG_IrisStatus".Translate(irisLabel));
        }

        if (IsOpeningQueued)
            sb.AppendLine("RG_TimeUntilGateLock".Translate(_ticksUntilOpen.ToStringTicksToPeriod()));

        if (HasIris && PowerTrader != null && Props.irisPowerConsumption > 0)
        {
            sb.AppendLine("RG_IrisPowerConsumption".Translate(Props.irisPowerConsumption.ToString("F0")));
            if (!Powered)
                sb.AppendLine("RG_IrisNonFunctional".Translate());
        }

        return sb.ToString().TrimEndNewlines();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        _cachedIrisGlowMat = null;
        _cachedIrisGlowMesh = null;

        CleanupGate();
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!Spawned)
            CleanupGate();
        base.Destroy(mode);
    }

    private void CleanupGate()
    {
        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Cleaning up gate {this}.");

        // Remove designations on connected DHD that would target this gate
        var dm = Map?.designationManager;
        if (dm != null)
        {
            var connectedDHD = ConnectedDHD;
            if (connectedDHD != null)
                dm.RemoveAllDesignationsOn(connectedDHD);
        }

        GlobalVortexEntryCellCache.Remove(Map?.uniqueID ?? -1);

        CloseGate(ConnectedGate != null);
        GateUtil.RemoveGateAddress(GateAddress);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref IsActive, "IsActive");
        Scribe_Values.Look(ref _externalHoldCount, "_externalHoldCount");
        Scribe_Values.Look(ref IsReceivingGate, "IsReceivingGate");
        Scribe_Values.Look(ref HasIris, "HasIris");
        Scribe_Values.Look(ref _isIrisActivated, "_isIrisActivated");
        Scribe_Values.Look(ref TicksSinceOpened, "TicksSinceOpened");
        Scribe_Values.Look(ref ConnectedAddress, "ConnectedAddress");
        Scribe_Values.Look(ref GateAddress, "GateAddress");
        Scribe_References.Look(ref ConnectedGate, "ConnectedGate");
        Scribe_Values.Look(ref _ticksUntilOpen, "_ticksUntilOpen", -1);
        Scribe_Values.Look(ref TicksSinceBufferUnloaded, "TicksSinceBufferUnloaded");
        Scribe_Values.Look(ref _queuedAddress, "_queuedAddress");
        Scribe_Values.Look(ref _inert, "_inert", false);
        Scribe_Values.Look(ref _gateConditionActive, "_gateConditionActive", false);

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

    public void SetIrisActive(bool status)
    {
        bool previous = _isIrisActivated;
        _isIrisActivated = status;
        if (previous == _isIrisActivated)
            return;

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

        PuddleSustainer = RimgateDefOf.Rimgate_GateIdle.TrySpawnSustainer(SoundInfo.InMap(this));
        RimgateDefOf.Rimgate_GateOpen.PlayOneShot(SoundInfo.InMap(this));
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

        // only evacuate if we know when it's opening
        EvacuateVortexPath();
    }

    private void Open(PlanetTile address)
    {
        if (!address.Valid) return;

        MapParent mp = Find.WorldObjects.MapParentAt(address);
        if (mp == null)
        {
            Log.Error($"Rimgate :: gate address at {address} doesn't have an associated MapParent.");
            return;
        }

        if (!mp.HasMap)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: generating map for {mp} using {mp.def.defName}");

            IntVec3 mapSize = mp.def.overrideMapSize.HasValue
                ? mp.def.overrideMapSize.Value
                : (mp as Site).PreferredMapSize;
            LongEventHandler.QueueLongEvent(delegate
            {
                // note: world object is created via quest initialization;
                // if we don't have a wo yet, something went wrong
                GetOrGenerateMapUtility.GetOrGenerateMap(
                    mp.Tile,
                    mapSize,
                    null);
            },
            (mp is WorldObject_GateTransitSite) ? "RG_GeneratingGateSite_Transit" : "RG_GeneratingGateSite",
            false,
            GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap,
            callback: delegate
            {
                if (RimgateMod.Debug)
                    Log.Message($"Rimgate :: finished generating map");

                FinalizeOpen(address, mp.Map);
            });
        }
        else
            FinalizeOpen(address, mp.Map);
    }

    private void FinalizeOpen(PlanetTile address, Map map)
    {
        if (!TryGetReceivingGate(map, out Building_Gate gate) || gate.IsActive)
        {
            Messages.Message(
                "RG_GateDialFailed".Translate(),
                MessageTypeDefOf.NegativeEvent);
            RimgateDefOf.Rimgate_GateFailDial.PlayOneShot(SoundInfo.InMap(this));
            CloseGate();
            return;
        }

        IsActive = true;
        ConnectedAddress = address;

        if (ConnectedAddress.Valid)
        {
            ConnectedGate = gate;
            ConnectedGate.FinalizeOpenReceivingGate(this, GateAddress);
        }

        PuddleSustainer = RimgateDefOf.Rimgate_GateIdle.TrySpawnSustainer(SoundInfo.InMap(this));
        RimgateDefOf.Rimgate_GateOpen.PlayOneShot(SoundInfo.InMap(this));

        if (Glower != null)
        {
            Glower.Props.glowRadius = GlowRadius;
            Glower.PostSpawnSetup(false);
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: finished opening gate {this}");
    }

    // called on the receiving gate by the dialing gate
    private void FinalizeOpenReceivingGate(Building_Gate connectedGate, PlanetTile connectedAddress)
    {
        if (!connectedAddress.Valid) return;

        IsActive = true;
        IsReceivingGate = true;
        ConnectedAddress = connectedAddress;
        ConnectedGate = connectedGate;

        if (Map == null || !Spawned) return;

        PuddleSustainer = RimgateDefOf.Rimgate_GateIdle.TrySpawnSustainer(SoundInfo.InMap(this));
        RimgateDefOf.Rimgate_GateOpen.PlayOneShot(SoundInfo.InMap(this));

        if (Glower != null)
        {
            Glower.Props.glowRadius = GlowRadius;
            Glower.PostSpawnSetup(false);
        }
    }

    public void CloseGate(bool closeOtherGate = false)
    {
        if (!IsActive && _externalHoldCount == 0)
            return;

        Transporter?.CancelLoad();

        var connectedDHD = ConnectedDHD;
        if (connectedDHD != null)
            Map.designationManager.RemoveAllDesignationsOn(connectedDHD);

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
                Log.Warning($"Rimgate :: Recieving gate connected to gate {this} doesn't exist, but this gate wanted it closed.");
            else
                ConnectedGate.CloseGate();
        }

        SoundDef puddleCloseDef = RimgateDefOf.Rimgate_GateClose;
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
                    Log.Warning($"Rimgate :: {this} has the explodeOnUse tag set to true but doesn't have CompExplosive.");
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

    private void MarkRedraftOnArrival(Thing t)
    {
        _redraftOnArrival ??= new();
        _redraftOnArrival.Add(t.thingIDNumber);
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

    public void EvacuateVortexPath()
    {
        Map map = Map;
        if (map == null) return;

        // Build vortex list + reserved set (reserved starts with vortex cells)
        var vortex = new List<IntVec3>(16);
        var reserved = new HashSet<IntVec3>();

        foreach (var c in VortexCells)
        {
            if (!c.InBounds(map)) continue;
            vortex.Add(c);
            reserved.Add(c);
        }

        int vortexCount = vortex.Count;
        if (vortexCount == 0) return;

        // Expand pickup radius by 2
        const int dangerRadius = 2;
        const int dangerRadiusSq = dangerRadius * dangerRadius;

        // Compute AABB around vortex cells for cheap early reject
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        for (int i = 0; i < vortexCount; i++)
        {
            IntVec3 v = vortex[i];
            if (v.x < minX) minX = v.x;
            if (v.x > maxX) maxX = v.x;
            if (v.z < minZ) minZ = v.z;
            if (v.z > maxZ) maxZ = v.z;
        }
        minX -= dangerRadius; maxX += dangerRadius;
        minZ -= dangerRadius; maxZ += dangerRadius;

        // Collect humans near vortex cells (within radius)
        var pawnsToMove = new List<Pawn>();
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllHumanlike;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn p = pawns[i];
            if (p == null || !p.Spawned || p.Dead) continue;

            IntVec3 pos = p.Position;

            // AABB early reject
            if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
            {
                if (RimgateMod.Debug)
                    Log.Message($"Rimgate :: Skipping pawn {p.LabelShort} at {pos} outside AABB [{minX},{minZ}] - [{maxX},{maxZ}]");
                continue;
            }

            // Precise check: within 2 cells of any vortex cell
            bool near = false;
            for (int j = 0; j < vortexCount; j++)
            {
                IntVec3 v = vortex[j];
                int dx = pos.x - v.x;
                int dz = pos.z - v.z;
                if (dx * dx + dz * dz <= dangerRadiusSq)
                {
                    near = true;
                    break;
                }
            }

            var curPath = p.pather?.curPath;
            if (curPath != null && curPath.NodesLeftCount > 0)
            {
                IntVec3 next = curPath.Peek(0);
                if (reserved.Contains(next)) near = true;
            }

            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Pawn {p.LabelShort} at {pos} is {(near ? "" : "not ")}near vortex.");

            if (near)
                pawnsToMove.Add(p);
        }

        if (pawnsToMove.Count == 0) return;

        int holdTicks = Math.Max(620, IsOpeningQueued ? _ticksUntilOpen + 120 : 620);

        for (int pi = 0; pi < pawnsToMove.Count; pi++)
        {
            Pawn p = pawnsToMove[pi];
            IntVec3 origin = p.Position;

            IntVec3 best = IntVec3.Invalid;
            if (!CellFinder.TryFindRandomCellNear(origin, map, 12, c =>
                c.InBounds(map) && c.Walkable(map) && !reserved.Contains(c), out best))
            {
                best = Position + Rotation.FacingCell;
                if (!best.InBounds(map) || !best.Walkable(map) || reserved.Contains(best))
                {
                    if (RimgateMod.Debug)
                        Log.Message($"Rimgate :: Could not evacuate pawn {p.LabelShort} from {origin}; no valid cells.");
                    continue;
                }
            }

            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Evacuating pawn {p.LabelShort} from {origin} to {best}");

            var gotoJob = JobMaker.MakeJob(JobDefOf.Goto, best);
            gotoJob.playerForced = true;
            gotoJob.expiryInterval = holdTicks;
            gotoJob.locomotionUrgency = LocomotionUrgency.Sprint;
            gotoJob.checkOverrideOnExpire = true;

            var waitJob = JobMaker.MakeJob(JobDefOf.Wait, best);
            waitJob.playerForced = true;
            waitJob.expiryInterval = holdTicks;
            waitJob.checkOverrideOnExpire = true;

            p.jobs?.StopAll();
            p.jobs.StartJob(gotoJob, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            p.jobs.jobQueue.EnqueueLast(waitJob);

            reserved.Add(best);
        }
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

    public static bool TryGetReceivingGate(Map map, out Building_Gate gate)
    {
        gate = null;
        if (Building_Gate.TryGetSpawnedGateOnMap(map, out gate))
            return true;

        gate = GateUtil.PlaceRandomGate(map);
        if (gate == null)
            return false;

        // Ensure DHD is placed near the gate
        GateUtil.EnsureDhdNearGate(map, gate, map.ParentFaction);
        return true;
    }

    public static bool TryGetSpawnedGateOnMap(
        Map map,
        out Building_Gate gate,
        Thing thingToIgnore = null)
    {
        if (map == null)
        {
            gate = null;
            return false;
        }

        gate = null;
        foreach (Thing thing in map?.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing is Building_Gate bsg
                && bsg.Spawned
                && !bsg.IsMinified())
            {
                gate = bsg;
                break;
            }
        }

        return gate != null;
    }

    #endregion
}

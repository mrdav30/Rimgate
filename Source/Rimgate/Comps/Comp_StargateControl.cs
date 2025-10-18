using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

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

    public int TicksUntilOpen = -1;

    public int ExternalHoldCount;

    public CompProperties_StargateControl Props => (CompProperties_StargateControl)props;

    public Graphic StargatePuddle => _stargatePuddle ??= Props.puddleGraphicData.Graphic;

    public Graphic StargateIris => _stargateIris ??= Props.irisGraphicData.Graphic;

    public bool GateIsLoadingTransporter
    {
        get
        {
            return Parent.Transporter != null
                && Parent.Transporter.LoadingInProgressOrReadyToLaunch
                && Parent.Transporter.AnyInGroupHasAnythingLeftToLoad;
        }
    }
    public IEnumerable<IntVec3> VortexCells
    {
        get
        {
            foreach (IntVec3 offset in Props.vortexPattern)
                yield return offset + parent.Position;
        }
    }

    private List<Thing> _sendBuffer;

    private HashSet<int> _redraftOnArrival;

    private Queue<Thing> _recvBuffer;

    private bool _isIrisActivated = false;

    public bool IsIrisActivated => _isIrisActivated;

    public bool _wantsIrisToggled;

    public bool WantsIrisClosed => _wantsIrisToggled;

    public PlanetTile ConnectedAddress = PlanetTile.Invalid;

    public Building_Stargate Parent => parent as Building_Stargate;

    public Building_Stargate ConnectedStargate;

    public Sustainer PuddleSustainer;

    private PlanetTile _queuedAddress;

    private Graphic _stargatePuddle;

    private Graphic _stargateIris;

    #region DHD Controls

    public void OpenStargateDelayed(PlanetTile address, int delay)
    {
        _queuedAddress = address;
        TicksUntilOpen = delay;
    }

    public void OpenStargate(PlanetTile address)
    {
        Building_Stargate gate = GetorCreateReceivingStargate(address);
        bool invalid = address.Valid
            && (gate == null || gate.TryGetComp<Comp_StargateControl>().IsActive);
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
            ConnectedStargate = gate;

            Comp_StargateControl otherComp = ConnectedStargate.StargateControl;
            otherComp.IsActive = true;
            otherComp.IsReceivingGate = true;
            otherComp.ConnectedAddress = GateAddress;
            otherComp.ConnectedStargate = Parent;

            otherComp.PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(otherComp.parent));
            RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(otherComp.parent));

            otherComp.Parent.Glower.Props.glowRadius = _glowRadius;
            otherComp.Parent.Glower.PostSpawnSetup(false);
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

    public void PushExternalHold() => ExternalHoldCount++;
    public void PopExternalHold() { 
        if (ExternalHoldCount > 0) 
            ExternalHoldCount--; 
    }

    public void ForceLocalOpenAsReceiver()
    {
        if (IsActive && IsReceivingGate) return;

        IsActive = true;
        IsReceivingGate = true;
        ConnectedAddress = PlanetTile.Invalid;
        ConnectedStargate = null;  // local-only, no remote
        PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        RimgateDefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(parent));
        if (Parent?.Glower != null)
        {
            Parent.Glower.Props.glowRadius = 10;
            Parent.Glower.PostSpawnSetup(false);
        }
    }

    public void CloseStargate(bool closeOtherGate = false)
    {
        if (Parent == null || !IsActive) return;

        Parent.Transporter?.CancelLoad();

        //clear buffers just in case
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
            otherSgComp = ConnectedStargate.StargateControl;
            if (ConnectedStargate == null || otherSgComp == null)
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
        ConnectedStargate = null;
        IsReceivingGate = false;
    }

    #endregion

    private Building_Stargate GetorCreateReceivingStargate(PlanetTile address)
    {
        if (!address.Valid) return null;

        MapParent connectedSite = Find.WorldObjects.MapParentAt(address);
        if (connectedSite == null)
        {
            Log.Error($"Rimgate :: Tried to get a paired stargate at address {address} but the map parent does not exist!");
            return null;
        }

        if (!connectedSite.HasMap)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: generating map for {connectedSite} using {connectedSite.def.defName}");

            GetOrGenerateMapUtility.GetOrGenerateMap(
                connectedSite.Tile,
                connectedSite is WorldObject_PermanentStargateSite
                        ? RimgateMod.MinMapSize
                        : Find.World.info.initialMapSize,
                null);

            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: finished generating map");
        }
        else if (connectedSite is WorldObject_QuestStargateSite wos)
            wos.ToggleSiteMap();

        Map map = connectedSite.Map;

        // ensure a valid gate and a DHD exist(and link)
        var gate = StargateUtility.EnsureGateAndDhd(map);

        return gate;
    }

    private void PlayTeleportSound()
    {
        DefDatabase<SoundDef>.GetNamed($"Rimgate_StargateTeleport0{Rand.RangeInclusive(1, 4)}").PlayOneShot(SoundInfo.InMap(parent));
    }

    private void DoUnstableVortex()
    {
        List<Thing> excludedThings = new List<Thing>() { parent };
        foreach (IntVec3 pos in Props.vortexPattern)
        {
            foreach (Thing thing in parent.Map.thingGrid.ThingsAt(parent.Position + pos))
            {
                if (thing.def.passability == Traversability.Standable)
                    excludedThings.Add(thing);
            }
        }

        foreach (IntVec3 pos in Props.vortexPattern)
        {
            DamageDef damType = DefDatabase<DamageDef>.GetNamed("Rimgate_KawooshExplosion");

            Explosion explosion = (Explosion)GenSpawn.Spawn(
                ThingDefOf.Explosion,
                parent.Position,
                parent.Map,
                WipeMode.Vanish);
            explosion.damageFalloff = false;
            explosion.damAmount = damType.defaultDamage;
            explosion.Position = parent.Position + pos;
            explosion.radius = 0.5f;
            explosion.damType = damType;
            explosion.StartExplosion(null, excludedThings);
        }
    }

    public void AddToSendBuffer(Thing thing)
    {
        _sendBuffer ??= new();
        _sendBuffer.Add(thing);

        // redraft flag travels as an ID on the destination comp
        Comp_StargateControl dest = ConnectedStargate?.StargateControl;
        if (dest != null && ShouldRedraftAfterSpawn(thing))
            dest.MarkRedraftOnArrival(thing);

        PlayTeleportSound();
    }

    private void BeamSendBufferTo()
    {
        Comp_StargateControl dest = ConnectedStargate.StargateControl;

        for (int i = 0; i < _sendBuffer.Count; i++)
        {
            Thing t = _sendBuffer[i];

            if (IsReceivingGate)
            {
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
        if (p.Faction != Faction.OfPlayer) return false;
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
        if(t == null || t.Destroyed)
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
                // hold position for a tick; avoids “wander”
                p.pather?.StopDead();
            }
        }
        else
        {
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
        if (_isIrisActivated)
        {
            Vector3 offset = RimgateDefOf.Rimgate_Stargate.graphicData.drawOffset;
            offset.y -= 0.01f;

            StargateIris.Draw(
                parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingBelowTop) + offset,
                Rot4.North,
                parent);
        }

        if (IsActive)
        {
            Vector3 offset = parent.def.graphicData.drawOffset;
            offset.y -= 0.02f;

            StargatePuddle.Draw(
                parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingBelowTop) + offset,
                Rot4.North,
                parent);
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (TicksUntilOpen > 0)
        {
            TicksUntilOpen--;
            if (TicksUntilOpen == 0)
            {
                TicksUntilOpen = -1;
                OpenStargate(_queuedAddress);
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

        if (!ConnectedAddress.Valid && _recvBuffer?.Any() == false)
        {
            CloseStargate();
            return;
        }

        if (_recvBuffer?.Any() == true 
            && TicksSinceBufferUnloaded > Rand.Range(10, 80))
        {
            SpawnFromReceiveBuffer();
        }

        TicksSinceBufferUnloaded++;
        TicksSinceOpened++;

        bool otherLoading = ConnectedStargate != null
            && ConnectedStargate.StargateControl.GateIsLoadingTransporter;

        bool shouldClose = IsReceivingGate
            && ExternalHoldCount == 0
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
            if (ConnectedStargate == null && ConnectedAddress.Valid)
                ConnectedStargate = GetorCreateReceivingStargate(ConnectedAddress);
            PuddleSustainer = RimgateDefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: compsg postspawnssetup: sgactive={IsActive} connectgate={ConnectedStargate} connectaddress={ConnectedAddress}, mapparent={parent.Map.Parent}");
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
                icon = ContentFinder<Texture2D>.Get(Props.irisGraphicData.texPath, true),
                isActive = () => _wantsIrisToggled,
                toggleAction = delegate
                {
                    _wantsIrisToggled = !_wantsIrisToggled;

                    Designation designation = parent.Map.designationManager.DesignationOn(parent, RimgateDefOf.Rimgate_DesignationToggleIris);

                    if (designation == null)
                        parent.Map.designationManager.AddDesignation(new Designation(parent, RimgateDefOf.Rimgate_DesignationToggleIris));
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
        Scribe_Values.Look(ref IsReceivingGate, "IsRecievingGate");
        Scribe_Values.Look(ref HasIris, "HasIris");
        Scribe_Values.Look(ref _isIrisActivated, "_irisIsActivated");
        Scribe_Values.Look(ref _wantsIrisToggled, "_wantsIrisToggled");
        Scribe_Values.Look(ref TicksSinceOpened, "TicksSinceOpened");
        Scribe_Values.Look(ref ConnectedAddress, "_connectedAddress");
        Scribe_References.Look(ref ConnectedStargate, "_connectedStargate");

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
        string address = StargateUtility.GetStargateDesignation(GateAddress);
        sb.AppendLine("RG_GateAddress".Translate(address));
        if (!IsActive)
            sb.AppendLine("InactiveFacility".Translate().CapitalizeFirst());
        else
        {
            string connectAddress = StargateUtility.GetStargateDesignation(ConnectedAddress);
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

        if (TicksUntilOpen > 0)
            sb.AppendLine("RG_TimeUntilGateLock".Translate(TicksUntilOpen.ToStringTicksToPeriod()));

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
        CloseStargate(ConnectedStargate != null);
        Find.World.GetComponent<WorldComp_StargateAddresses>().RemoveAddress(GateAddress);
    }
}

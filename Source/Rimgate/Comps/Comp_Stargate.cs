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

namespace Rimgate;

public class Comp_Stargate : ThingComp
{
    public const int GlowRadius = 10;

    public const string Alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public int TicksSinceBufferUnloaded;

    public int TicksSinceOpened;

    public PlanetTile GateAddress;

    public bool StargateIsActive = false;

    public bool IsReceivingGate;

    public bool HasIris = false;

    public bool HasPower = false;

    public int TicksUntilOpen = -1;

    public CompProperties_Stargate Props => (CompProperties_Stargate)props;

    public Graphic StargatePuddle
    {
        get
        {
            _stargatePuddle ??= GraphicDatabase.Get<Graphic_Single>(
                Props.puddleTexture,
                ShaderDatabase.Mote,
                Props.puddleDrawSize,
                Color.white);

            return _stargatePuddle;
        }
    }
    public Graphic StargateIris
    {
        get
        {
            _stargateIris ??= GraphicDatabase.Get<Graphic_Single>(
                Props.irisTexture,
                ShaderDatabase.Mote,
                Props.puddleDrawSize,
                Color.white);

            return _stargateIris;
        }
    }

    public bool GateIsLoadingTransporter
    {
        get
        {
            CompTransporter transComp = parent.GetComp<CompTransporter>();
            return transComp != null
                && (transComp.LoadingInProgressOrReadyToLaunch
                && transComp.AnyInGroupHasAnythingLeftToLoad);
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

    public CompTransporter Transporter
    {
        get
        {
            _cachedtransComp ??= parent.GetComp<CompTransporter>();
            return _cachedtransComp;
        }
    }

    public CompGlower Glower
    {
        get
        {
            _cachedglowComp ??= parent.GetComp<CompGlower>();
            return _cachedglowComp;
        }
    }

    public CompExplosive Explosive
    {
        get
        {
            _cachedexplosiveComp ??= parent.GetComp<CompExplosive>();
            return _cachedexplosiveComp;
        }
    }

    private List<Thing> _sendBuffer = new List<Thing>();

    private Queue<Thing> _recvBuffer = new Queue<Thing>();

    private bool _irisIsActivated = false;

    public bool IsIrisActivated => _irisIsActivated;

    public bool _wantsIrisToggled;

    public bool WantsIrisClosed => _wantsIrisToggled;

    public PlanetTile ConnectedAddress = PlanetTile.Invalid;

    public Thing ConnectedStargate;

    public Sustainer PuddleSustainer;

    private PlanetTile _queuedAddress;

    private Graphic _stargatePuddle;

    private Graphic _stargateIris;

    private CompTransporter _cachedtransComp;

    private CompGlower _cachedglowComp;

    private CompExplosive _cachedexplosiveComp;

    #region DHD Controls

    public void OpenStargateDelayed(PlanetTile address, int delay)
    {
        _queuedAddress = address;
        TicksUntilOpen = delay;
    }

    public void OpenStargate(PlanetTile address)
    {
        Thing gate = GetorCreateReceivingStargate(address);
        bool invalid = address.Valid
            && (gate == null || gate.TryGetComp<Comp_Stargate>().StargateIsActive);
        if (invalid)
        {
            Messages.Message(
                "RG_GateDialFailed".Translate(),
                MessageTypeDefOf.NegativeEvent);
            Rimgate_DefOf.Rimgate_StargateFailDial.PlayOneShot(SoundInfo.InMap(parent));
            return;
        }

        StargateIsActive = true;
        ConnectedAddress = address;

        if (ConnectedAddress.Valid)
        {
            ConnectedStargate = gate;

            Comp_Stargate sgComp = ConnectedStargate.TryGetComp<Comp_Stargate>();
            sgComp.StargateIsActive = true;
            sgComp.IsReceivingGate = true;
            sgComp.ConnectedAddress = GateAddress;
            sgComp.ConnectedStargate = parent;

            sgComp.PuddleSustainer = Rimgate_DefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(sgComp.parent));
            Rimgate_DefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(sgComp.parent));

            CompGlower otherGlowComp = sgComp.parent.GetComp<CompGlower>();
            otherGlowComp.Props.glowRadius = GlowRadius;
            otherGlowComp.PostSpawnSetup(false);
        }

        PuddleSustainer = Rimgate_DefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        Rimgate_DefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(parent));

        if(Glower != null)
        {
            Glower.Props.glowRadius = GlowRadius;
            Glower.PostSpawnSetup(false);
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: finished opening gate {parent}");
    }

    public void CloseStargate(bool closeOtherGate)
    {
        Transporter?.CancelLoad();

        //clear buffers just in case
        foreach (Thing thing in _sendBuffer)
            GenSpawn.Spawn(thing, parent.InteractionCell, parent.Map);

        foreach (Thing thing in _recvBuffer)
            GenSpawn.Spawn(thing, parent.InteractionCell, parent.Map);

        Comp_Stargate sgComp = null;
        if (closeOtherGate)
        {
            sgComp = ConnectedStargate.TryGetComp<Comp_Stargate>();
            if (ConnectedStargate == null || sgComp == null)
            {
                Log.Warning($"Rimgate :: Recieving stargate connected to stargate {parent.ThingID} didn't have CompStargate, but this stargate wanted it closed.");
            }
            else
                sgComp.CloseStargate(false);
        }

        SoundDef puddleCloseDef = Rimgate_DefOf.Rimgate_StargateClose;
        puddleCloseDef.PlayOneShot(SoundInfo.InMap(parent));
        if (sgComp != null)
            puddleCloseDef.PlayOneShot(SoundInfo.InMap(sgComp.parent));

        if (PuddleSustainer != null)
            PuddleSustainer.End();

        if(Glower != null)
        {
            Glower.Props.glowRadius = 0;
            Glower.PostSpawnSetup(false);
        }

        if (Props.explodeOnUse)
        {

            if (Explosive == null)
            {
                Log.Warning($"Rimgate :: Stargate {parent.ThingID} has the explodeOnUse tag set to true but doesn't have CompExplosive.");
            }
            else
                Explosive.StartWick();
        }

        StargateIsActive = false;
        TicksSinceBufferUnloaded = 0;
        TicksSinceOpened = 0;
        ConnectedAddress = PlanetTile.Invalid;
        ConnectedStargate = null;
        IsReceivingGate = false;
    }

    #endregion

    public static Thing GetStargateOnMap(Map map, Thing thingToIgnore = null)
    {
        Thing gateOnMap = null;
        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing != thingToIgnore
                && thing.def.thingClass == typeof(Building_Stargate))
            {
                gateOnMap = thing;
                break;
            }
        }

        return gateOnMap;
    }

    public static string GetStargateDesignation(PlanetTile address)
    {
        if (!address.Valid)
            return "UnknownLower".Translate();

        Rand.PushState(address);
        //pattern: P(num)(char)-(num)(num)(num)
        string designation = 
            $"P{Rand.RangeInclusive(0, 9)}{Alpha[Rand.RangeInclusive(0, 25)]}"
            + $"-{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}";
        Rand.PopState();
        return designation;
    }

    private Thing GetorCreateReceivingStargate(PlanetTile address)
    {
        if (!address.Valid)
            return null;

        MapParent connectedMap = Find.WorldObjects.MapParentAt(address);
        if (connectedMap == null)
        {
            Log.Error($"Rimgate :: Tried to get a paired stargate at address {address} but the map parent does not exist!");
            return null;
        }

        if (!connectedMap.HasMap)
        {
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: generating map for {connectedMap}");

            GetOrGenerateMapUtility.GetOrGenerateMap(
                    connectedMap.Tile,
                    Find.World.info.initialMapSize,
                    null);

            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: finished generating map");
        }

        Map map = connectedMap.Map;
        Thing gate = GetStargateOnMap(map);

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
        _sendBuffer.Add(thing);
        PlayTeleportSound();
    }

    public void AddToRecieveBuffer(Thing thing)
    {
        _recvBuffer.Enqueue(thing);
    }

    #region Comp Overrides

    public override void PostDraw()
    {
        base.PostDraw();
        if (_irisIsActivated)
        {
            StargateIris.Draw(
                parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingOnTop) - (Vector3.one * 0.01f),
                Rot4.North,
                parent);
        }

        if (StargateIsActive)
        {
            StargatePuddle.Draw(
                parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingOnTop) - (Vector3.one * 0.02f),
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

        if (!StargateIsActive) return;

        if (!_irisIsActivated && TicksSinceOpened < 150 && TicksSinceOpened % 10 == 0)
            DoUnstableVortex();

        if (parent.Fogged())
            FloodFillerFog.FloodUnfog(parent.Position, parent.Map);

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
            else if (Transporter.LoadingInProgressOrReadyToLaunch
                && !Transporter.AnyInGroupHasAnythingLeftToLoad)
            {
                Transporter.CancelLoad();
            }
        }

        if (_sendBuffer.Any())
        {
            Comp_Stargate sgComp = ConnectedStargate.TryGetComp<Comp_Stargate>();
            if (!IsReceivingGate)
            {
                foreach (var item in _sendBuffer)
                    sgComp.AddToRecieveBuffer(item);
            }
            else
            {
                foreach (var item in _sendBuffer)
                    item.Kill();
            }
            _sendBuffer.Clear();
        }

        if (_recvBuffer.Any() && TicksSinceBufferUnloaded > Rand.Range(10, 80))
        {
            TicksSinceBufferUnloaded = 0;
            var thing = _recvBuffer.Peek(); // check without removing

            if (!_irisIsActivated)
            {
                GenSpawn.Spawn(thing, parent.InteractionCell, parent.Map);
                PlayTeleportSound();
            }
            else
            {
                thing.Kill();
                Rimgate_DefOf.Rimgate_IrisHit.PlayOneShot(SoundInfo.InMap(parent));
            }

            _recvBuffer.Dequeue(); // remove after handling
        }

        if (!ConnectedAddress.Valid && !_recvBuffer.Any())
            CloseStargate(false);

        TicksSinceBufferUnloaded++;
        TicksSinceOpened++;

        if (IsReceivingGate
            && TicksSinceBufferUnloaded > 2500
            && !ConnectedStargate.TryGetComp<Comp_Stargate>().GateIsLoadingTransporter)
        {
            CloseStargate(true);
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        GateAddress = parent.Map.Tile;
        Find.World.GetComponent<WorldComp_StargateAddresses>().AddAddress(GateAddress);

        if (StargateIsActive)
        {
            if (ConnectedStargate == null && ConnectedAddress.Valid)
                ConnectedStargate = GetorCreateReceivingStargate(ConnectedAddress);
            PuddleSustainer = Rimgate_DefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: compsg postspawnssetup: sgactive={StargateIsActive} connectgate={ConnectedStargate} connectaddress={ConnectedAddress}, mapparent={parent.Map.Parent}");
    }

    public string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        string address = GetStargateDesignation(GateAddress);
        sb.AppendLine("RG_GateAddress".Translate(address));
        if (!StargateIsActive)
            sb.AppendLine("InactiveFacility".Translate().CapitalizeFirst());
        else
        {
            string connectAddress = GetStargateDesignation(ConnectedAddress);
            var connectLabel = (IsReceivingGate 
                ? "RG_IncomingConnection" 
                : "RG_OutgoingConnection").Translate();
            sb.AppendLine("RG_ConnectedToGate".Translate(connectAddress, connectLabel));
        }

        if (HasIris)
        {
            var irisLabel = (_irisIsActivated 
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
        _irisIsActivated = !_irisIsActivated;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (Props.canHaveIris && HasIris)
        {
            var action = (_irisIsActivated 
                ? "RG_OpenIris" 
                : "RG_CloseIris").Translate();
            Command_Toggle command = new Command_Toggle
            {
                defaultLabel = "RG_ToggleIris".Translate(action),
                defaultDesc = "RG_ToggleIrisDesc".Translate(action),
                icon = ContentFinder<Texture2D>.Get(Props.irisTexture, true),
                isActive = () => _wantsIrisToggled,
                toggleAction = delegate
                {
                    _wantsIrisToggled = !_wantsIrisToggled;

                    Designation designation = parent.Map.designationManager.DesignationOn(parent, Rimgate_DefOf.Rimgate_DesignationToggleIris);

                    if (designation == null)
                        parent.Map.designationManager.AddDesignation(new Designation(parent, Rimgate_DefOf.Rimgate_DesignationToggleIris));
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
                CloseStargate(false);
                Log.Message($"Rimgate :: Stargate {parent.ThingID} was force-closed.");
            }
        };

        yield return commandDevMode;
    }

    public void DoToggleIris()
    {
        _irisIsActivated = !_irisIsActivated;
        _wantsIrisToggled = false;
        if (_irisIsActivated)
            Rimgate_DefOf.Rimgate_IrisOpen.PlayOneShot(SoundInfo.InMap(parent));
        else
            Rimgate_DefOf.Rimgate_IrisClose.PlayOneShot(SoundInfo.InMap(parent));
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (!StargateIsActive || _irisIsActivated)
            yield break;

        bool canReach = selPawn.CanReach(
            parent,
            PathEndMode.Touch,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);

        if (!canReach)
            yield break;


        var enterLabel = (IsReceivingGate
            ? "RG_EnterReceivingStargateAction"
            : "RG_EnterStargateAction").Translate();
        yield return new FloatMenuOption(enterLabel, () =>
        {
            Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_EnterStargate, parent);
            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        });

        var bringLabel = (IsReceivingGate
            ? "RG_BringPawnToReceivingGateAction"
            : "RG_BringPawnToGateAction").Translate();
        yield return new FloatMenuOption(bringLabel, () =>
        {
            TargetingParameters targetingParameters = new TargetingParameters()
            {
                onlyTargetIncapacitatedPawns = true,
                canTargetBuildings = false,
                canTargetItems = true,
            };

            Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo t)
            {
                Job job = JobMaker.MakeJob(
                    Rimgate_DefOf.Rimgate_BringToStargate,
                    t.Thing,
                    parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        });

        yield break;
    }

    public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
    {
        if (!StargateIsActive)
            yield break;

        List<Pawn> allowedPawns = new List<Pawn>();
        foreach (Pawn selPawn in selPawns)
        {
            bool canReach = selPawn.CanReach(
                parent,
                PathEndMode.Touch,
                Danger.Deadly,
                false,
                false,
                TraverseMode.ByPawn);
            if (canReach)
                allowedPawns.Add(selPawn);
        }

        var label = (IsReceivingGate
            ? "RG_EnterReceivingStargateWithSelectedAction"
            : "RG_EnterStargateWithSelectedAction").Translate();
        yield return new FloatMenuOption(label, () =>
        {
            foreach (Pawn selPawn in allowedPawns)
            {
                Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_EnterStargate, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        });

        yield break;
    }

    private void CleanupGate()
    {
        if (ConnectedStargate != null)
            CloseStargate(true);

        Find.World.GetComponent<WorldComp_StargateAddresses>().RemoveAddress(GateAddress);
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
        Scribe_Values.Look(ref StargateIsActive, "StargateIsActive");
        Scribe_Values.Look(ref IsReceivingGate, "IsRecievingGate");
        Scribe_Values.Look(ref HasIris, "HasIris");
        Scribe_Values.Look(ref _irisIsActivated, "_irisIsActivated");
        Scribe_Values.Look(ref _wantsIrisToggled, "_wantsIrisToggled");
        Scribe_Values.Look(ref TicksSinceOpened, "TicksSinceOpened");
        Scribe_Values.Look(ref ConnectedAddress, "_connectedAddress");
        Scribe_References.Look(ref ConnectedStargate, "_connectedStargate");
        Scribe_Collections.Look(ref _recvBuffer, "_recvBuffer", LookMode.GlobalTargetInfo);
        Scribe_Collections.Look(ref _sendBuffer, "_sendBuffer", LookMode.GlobalTargetInfo);
    }

    public override string CompInspectStringExtra()
    {
        return base.CompInspectStringExtra() + "RG_RespawnGateString".Translate();
    }
    #endregion
}


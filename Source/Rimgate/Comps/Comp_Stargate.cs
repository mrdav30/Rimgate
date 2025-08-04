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

    public bool IsRecievingGate;

    public bool HasIris = false;

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

    private List<Thing> _sendBuffer = new List<Thing>();

    private List<Thing> _recvBuffer = new List<Thing>();

    private bool _irisIsActivated = false;

    private PlanetTile _queuedAddress;

    private PlanetTile _connectedAddress = -1;

    private Thing _connectedStargate;

    private Sustainer _puddleSustainer;

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
        Thing gate = GetDialledStargate(address);
        if (address > -1
            && (gate == null || gate.TryGetComp<Comp_Stargate>().StargateIsActive))
        {
            Messages.Message(
                "Rimgate_GateDialFailed".Translate(),
                MessageTypeDefOf.NegativeEvent);
            Rimgate_DefOf.Rimgate_StargateFailDial.PlayOneShot(SoundInfo.InMap(parent));
            return;
        }

        StargateIsActive = true;
        _connectedAddress = address;

        if (_connectedAddress != -1)
        {
            _connectedStargate = GetDialledStargate(_connectedAddress);
            Comp_Stargate sgComp = _connectedStargate.TryGetComp<Comp_Stargate>();
            sgComp.StargateIsActive = true;
            sgComp.IsRecievingGate = true;
            sgComp._connectedAddress = GateAddress;
            sgComp._connectedStargate = parent;

            sgComp._puddleSustainer = Rimgate_DefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(sgComp.parent));
            Rimgate_DefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(sgComp.parent));

            CompGlower otherGlowComp = sgComp.parent.GetComp<CompGlower>();
            otherGlowComp.Props.glowRadius = GlowRadius;
            otherGlowComp.PostSpawnSetup(false);
        }

        _puddleSustainer = Rimgate_DefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        Rimgate_DefOf.Rimgate_StargateOpen.PlayOneShot(SoundInfo.InMap(parent));

        CompGlower glowComp = parent.GetComp<CompGlower>();
        glowComp.Props.glowRadius = GlowRadius;
        glowComp.PostSpawnSetup(false);

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: finished opening gate {parent}");
    }

    public void CloseStargate(bool closeOtherGate)
    {
        CompTransporter transComp = parent.GetComp<CompTransporter>();
        transComp?.CancelLoad();

        //clear buffers just in case
        foreach (Thing thing in _sendBuffer)
            GenSpawn.Spawn(thing, parent.InteractionCell, parent.Map);

        foreach (Thing thing in _recvBuffer)
            GenSpawn.Spawn(thing, parent.InteractionCell, parent.Map);

        Comp_Stargate sgComp = null;
        if (closeOtherGate)
        {
            sgComp = _connectedStargate.TryGetComp<Comp_Stargate>();
            if (_connectedStargate == null || sgComp == null)
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

        if (_puddleSustainer != null)
            _puddleSustainer.End();

        CompGlower glowComp = parent.GetComp<CompGlower>();
        glowComp.Props.glowRadius = 0;
        glowComp.PostSpawnSetup(false);

        if (Props.explodeOnUse)
        {
            CompExplosive explosive = parent.TryGetComp<CompExplosive>();
            if (explosive == null)
            {
                Log.Warning($"Rimgate :: Stargate {parent.ThingID} has the explodeOnUse tag set to true but doesn't have CompExplosive.");
            }
            else
                explosive.StartWick();
        }

        StargateIsActive = false;
        TicksSinceBufferUnloaded = 0;
        TicksSinceOpened = 0;
        _connectedAddress = -1;
        _connectedStargate = null;
        IsRecievingGate = false;
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
        if (address < 0)
            return "UnknownLower".Translate();

        Rand.PushState(address);
        //pattern: P(num)(char)-(num)(num)(num)
        string designation = $"P{Rand.RangeInclusive(0, 9)}{Alpha[Rand.RangeInclusive(0, 25)]}-{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}";
        Rand.PopState();
        return designation;
    }

    private Thing GetDialledStargate(PlanetTile address)
    {
        if (address < 0)
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
                connectedMap as WorldObject_PermanentStargateSite != null
                    ? new IntVec3(75, 1, 75)
                    : Find.World.info.initialMapSize, null);

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
        _recvBuffer.Add(thing);
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
                _queuedAddress = -1;
            }
        }

        if (!StargateIsActive) return;

        if (!_irisIsActivated && TicksSinceOpened < 150 && TicksSinceOpened % 10 == 0)
            DoUnstableVortex();

        if (parent.Fogged())
            FloodFillerFog.FloodUnfog(parent.Position, parent.Map);

        Comp_Stargate sgComp = _connectedStargate.TryGetComp<Comp_Stargate>();
        CompTransporter transComp = parent.GetComp<CompTransporter>();

        if (transComp != null)
        {
            Thing thing = transComp.innerContainer.FirstOrFallback();
            if (thing != null)
            {
                if (thing.Spawned)
                    thing.DeSpawn();

                AddToSendBuffer(thing);
                transComp.innerContainer.Remove(thing);
            }
            else if (transComp.LoadingInProgressOrReadyToLaunch
                && !transComp.AnyInGroupHasAnythingLeftToLoad)
            {
                transComp.CancelLoad();
            }
        }

        if (_sendBuffer.Any())
        {
            if (!IsRecievingGate)
            {
                for (int i = 0; i <= _sendBuffer.Count; i++)
                {
                    sgComp.AddToRecieveBuffer(_sendBuffer[i]);
                    _sendBuffer.Remove(_sendBuffer[i]);
                }
            }
            else
            {
                for (int i = 0; i <= _sendBuffer.Count; i++)
                {
                    _sendBuffer[i].Kill();
                    _sendBuffer.Remove(_sendBuffer[i]);
                }
            }
        }

        if (_recvBuffer.Any() && TicksSinceBufferUnloaded > Rand.Range(10, 80))
        {
            TicksSinceBufferUnloaded = 0;
            if (!_irisIsActivated)
            {
                GenSpawn.Spawn(_recvBuffer[0], parent.InteractionCell, parent.Map);
                _recvBuffer.Remove(_recvBuffer[0]);
                PlayTeleportSound();
            }
            else
            {
                _recvBuffer[0].Kill();
                _recvBuffer.Remove(_recvBuffer[0]);
                Rimgate_DefOf.Rimgate_IrisHit.PlayOneShot(SoundInfo.InMap(parent));
            }
        }

        if (_connectedAddress == -1 && !_recvBuffer.Any())
            CloseStargate(false);

        TicksSinceBufferUnloaded++;
        TicksSinceOpened++;

        if (IsRecievingGate
            && TicksSinceBufferUnloaded > 2500
            && !_connectedStargate.TryGetComp<Comp_Stargate>().GateIsLoadingTransporter)
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
            if (_connectedStargate == null && _connectedAddress != -1)
                _connectedStargate = GetDialledStargate(_connectedAddress);
            _puddleSustainer = Rimgate_DefOf.Rimgate_StargateIdle.TrySpawnSustainer(SoundInfo.InMap(parent));
        }

        //fix nullreferenceexception that happens when the innercontainer disappears for some reason,
        //hopefully this doesn't end up causing a bug that will take hours to track down ;)
        CompTransporter transComp = parent.GetComp<CompTransporter>();
        if (transComp != null && transComp.innerContainer == null)
            transComp.innerContainer = new ThingOwner<Thing>(transComp);

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: compsg postspawnssetup: sgactive={StargateIsActive} connectgate={_connectedStargate} connectaddress={_connectedAddress}, mapparent={parent.Map.Parent}");
    }

    public string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Rimgate_GateAddress".Translate(GetStargateDesignation(GateAddress)));
        if (!StargateIsActive)
            sb.AppendLine("InactiveFacility".Translate().CapitalizeFirst());
        else
        {
            sb.AppendLine("Rimgate_ConnectedToGate".Translate(
                GetStargateDesignation(_connectedAddress),
                (IsRecievingGate ? "Rimgate_Incoming" : "Rimgate_Outgoing").Translate()));
        }

        if (HasIris)
            sb.AppendLine("Rimgate_IrisStatus".Translate(
                (_irisIsActivated ? "Rimgate_IrisClosed" : "Rimgate_IrisOpen").Translate()));

        if (TicksUntilOpen > 0)
            sb.AppendLine("Rimgate_TimeUntilGateLock".Translate(TicksUntilOpen.ToStringTicksToPeriod()));

        return sb.ToString().TrimEndNewlines();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (Props.canHaveIris && HasIris)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "Rimgate_OpenCloseIris".Translate(),
                defaultDesc = "Rimgate_OpenCloseIrisDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get(Props.irisTexture, true),
                action = delegate ()
                {
                    _irisIsActivated = !_irisIsActivated;
                    if (_irisIsActivated)
                        Rimgate_DefOf.Rimgate_IrisOpen.PlayOneShot(SoundInfo.InMap(parent));
                    else
                        Rimgate_DefOf.Rimgate_IrisClose.PlayOneShot(SoundInfo.InMap(parent));
                }
            };

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

        yield return new FloatMenuOption("Rimgate_EnterStargateAction".Translate(), () =>
        {
            Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_EnterStargate, parent);
            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        });
        yield return new FloatMenuOption("Rimgate_BringPawnToGateAction".Translate(), () =>
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

        yield return new FloatMenuOption("Rimgate_EnterStargateWithSelectedAction".Translate(), () =>
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
        if (_connectedStargate != null)
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
        Scribe_Values.Look(ref IsRecievingGate, "IsRecievingGate");
        Scribe_Values.Look(ref HasIris, "HasIris");
        Scribe_Values.Look(ref _irisIsActivated, "_irisIsActivated");
        Scribe_Values.Look(ref TicksSinceOpened, "TicksSinceOpened");
        Scribe_Values.Look(ref _connectedAddress, "_connectedAddress");
        Scribe_References.Look(ref _connectedStargate, "_connectedStargate");
        Scribe_Collections.Look(ref _recvBuffer, "_recvBuffer", LookMode.GlobalTargetInfo);
        Scribe_Collections.Look(ref _sendBuffer, "_sendBuffer", LookMode.GlobalTargetInfo);
    }

    public override string CompInspectStringExtra()
    {
        return base.CompInspectStringExtra() + "Rimgate_RespawnGateString".Translate();
    }
    #endregion
}


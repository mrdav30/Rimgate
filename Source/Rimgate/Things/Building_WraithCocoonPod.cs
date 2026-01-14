using HarmonyLib;
using Rimgate;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate;

public class Building_WraithCocoonPod_Ext : DefModExtension
{
    public List<PawnKindDef> containPawnKindAnyOf;

    public bool spawnVictim = true;
}

public class Building_WraithCocoonPod : Building, IThingHolder
{
    public const float DeteriorationRate = 2.0f; // base deterioration rate when empty

    public Building_WraithCocoonPod_Ext Props => _cachedProps ??= def.GetModExtension<Building_WraithCocoonPod_Ext>();

    public bool IsAbilitySpawn { get; set; }

    public Thing ContainedThing
    {
        get
        {
            if (innerContainer.Count != 0)
                return innerContainer[0];

            return null;
        }
    }

    public bool HasAnyContents => innerContainer.Count > 0;

    public virtual bool CanOpen => HasAnyContents;

    private Building_WraithCocoonPod_Ext _cachedProps;

    protected ThingOwner innerContainer;

    protected bool contentsKnown;

    public Building_WraithCocoonPod()
    {
        innerContainer = new ThingOwner<Thing>(this, true);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (Faction.IsOfPlayerFaction())
            contentsKnown = true;
        else if (!respawningAfterLoad
            && !IsAbilitySpawn
            && Props?.spawnVictim == true)
        {
            if(RimgateMod.Debug)
                Log.Message($"Rimgate :: Spawning victim in cocoon pod at {Position} on map {Map}.");
            Pawn victim = Utils.GeneratePawnForContainer(Map, null, Props.containPawnKindAnyOf);
            TryAcceptThing(victim);
            contentsKnown = false;
        }
    }

    public override void TickRare()
    {
        base.TickRare();

        // slowly deteriorate once open
        if (HasAnyContents) return;

        if (!(DeteriorationRate < float.Epsilon) && Rand.Chance(DeteriorationRate / 36f))
            TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1f));
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn) => Enumerable.Empty<FloatMenuOption>();

    public virtual bool Accepts(Thing thing) => innerContainer.CanAcceptAnyOf(thing);

    public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        if (!Accepts(thing))
            return false;

        bool flag;
        if (thing.holdingOwner != null)
        {
            thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
            flag = true;
        }
        else
            flag = innerContainer.TryAdd(thing);

        if (flag)
        {
            if (thing.Faction != null && thing.Faction.IsPlayer)
                contentsKnown = true;

            return true;
        }

        return false;
    }

    public virtual void EjectContents()
    {
        if (!HasAnyContents)
            return;

        ThingDef filth_Slime = ThingDefOf.Filth_Slime;
        foreach (Thing item in (IEnumerable<Thing>)innerContainer)
        {
            if (item is Pawn pawn)
            {
                PawnComponentsUtility.AddComponentsForSpawn(pawn);
                pawn.filth.GainFilth(filth_Slime);
                if (pawn.RaceProps.IsFlesh)
                    pawn.health.AddHediff(RimgateDefOf.Rimgate_WraithCocoonPodSickness);

                pawn.TryGiveThought(RimgateDefOf.Rimgate_WraithCocoonPod_ReleasedVictim);
            }
        }

        if (!Destroyed)
            SoundDefOf.CocoonDestroyed.PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map)));

        innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        contentsKnown = true;

        DirtyMapMesh(base.Map);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        base.Destroy(mode);
        if (innerContainer.Count > 0
            && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
        {
            if (mode != DestroyMode.Deconstruct)
            {
                List<Pawn> list = new List<Pawn>();
                foreach (Thing thing in (IEnumerable<Thing>)innerContainer)
                {
                    if (thing is Pawn victim)
                        list.Add(victim);
                }

                foreach (Pawn victim in list)
                    HealthUtility.DamageUntilDowned(victim);
            }

            innerContainer.TryDropAll(base.Position, Map, ThingPlaceMode.Near);
        }

        innerContainer.ClearAndDestroyContents();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (Faction == Faction.OfPlayer && innerContainer.Count > 0 && def.building.isPlayerEjectable)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.action = EjectContents;
            command_Action.defaultLabel = "RG_CommandCocoonPodEject".Translate();
            command_Action.defaultDesc = "RG_CommandCocoonPodEjectDesc".Translate();
            if (innerContainer.Count == 0)
            {
                command_Action.Disable("CommandPodEjectFailEmpty".Translate());
            }

            command_Action.hotKey = KeyBindingDefOf.Misc8;
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Button/RGWraithCocoonPodEject");
            yield return command_Action;
        }

        if (DebugSettings.ShowDevGizmos && CanOpen)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Open",
                action = () => EjectContents()
            };
        }
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(base.GetInspectString());

        if (HasAnyContents)
        {
            if (sb.Length > 0)
                sb.AppendLine();

            string str = contentsKnown
                ? innerContainer.ContentsString
                : "UnknownLower".Translate();

            sb.Append("CasketContains".Translate() + ": " + str.CapitalizeFirst());
        }
        else if (DeteriorationRate > float.Epsilon)
        {
            if (sb.Length > 0)
                sb.AppendLine();

            string rate = DeteriorationRate.ToStringByStyle(ToStringStyle.FloatMaxTwo);
            string text2 = string.Format("{0}: {1}",
                "DeterioratingBecauseOf".Translate(),
                "PerDay".Translate(rate));
            sb.Append(text2);
        }

        return sb.ToString().Trim();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref contentsKnown, "contentsKnown", defaultValue: false);
    }

    public static Building_WraithCocoonPod FindFilledPodFor(Pawn p, bool ignoreOtherReservations = false)
    {
        bool queuing = KeyBindingDefOf.QueueOrder.IsDownEvent;
        Building_WraithCocoonPod pod = GenClosest.ClosestThingReachable(p.PositionHeld,
            p.MapHeld,
            ThingRequest.ForDef(RimgateDefOf.Rimgate_WraithCocoonPod),
            PathEndMode.InteractionCell,
            TraverseParms.For(p),
            9999f,
            Validator) as Building_WraithCocoonPod;

        if (pod != null)
            return pod;

        bool Validator(Thing x)
        {
            if (((Building_WraithCocoonPod)x).HasAnyContents && (!queuing || !p.HasReserved(x)))
                return p.CanReserve(x, 1, -1, null, ignoreOtherReservations);

            return false;
        }

        return null;
    }
}
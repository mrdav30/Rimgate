using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace Rimgate;

public static class MedicalUtil
{
    public static bool HasAllowedMedicalCareCategory(Pawn pawn)
    {
        return pawn != null
            && WorkGiver_DoBill.GetMedicalCareCategory(pawn) >= MedicalCareCategory.NormalOrWorse;
    }

    public static bool HasUsageBlockingHediffs(
        Pawn pawn,
        List<HediffDef> usageBlockingHediffs,
        out List<Hediff> blockingHediffs)
    {
        blockingHediffs = pawn?.health?.hediffSet?.hediffs
            .Where(x => usageBlockingHediffs.Contains(x.def))
            .ToList();

        return blockingHediffs?.Count > 0;
    }

    public static bool HasUsageBlockingTraits(
        Pawn pawn,
        List<TraitDef> usageBlockingTraits,
        out List<Trait> blockingTraits)
    {
        blockingTraits = pawn.story?.traits?.allTraits
            .Where(x => usageBlockingTraits.Contains(x.def))
            .ToList();
        return blockingTraits?.Count > 0;
    }

    public static bool HasImmunizableHediffs(
        Pawn pawn,
        List<HediffDef> inclusions = null,
        List<HediffDef> exclusions = null)
    {
        var result = FindImmunizableHediffs(pawn, inclusions, exclusions);
        return result != null && result.Count > 0;
    }

    public static List<Hediff> FindImmunizableHediffs(
        Pawn pawn,
        List<HediffDef> inclusions = null,
        List<HediffDef> exclusions = null)
    {
        List<Hediff> hediffs = new();
        List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < allHediffs.Count; i++)
        {
            Hediff current = allHediffs[i];

            if (inclusions != null && inclusions.Contains(current.def))
            {
                hediffs.Add(current);
                continue;
            }

            bool isViable = current.Visible
                && current.def.everCurableByItem
                && current.TryGetComp<HediffComp_Immunizable>() != null
                && !current.FullyImmune();
            if (isViable)
            {
                if (exclusions != null
                    && exclusions.Contains(current.def)) continue;

                hediffs.Add(allHediffs[i]);
            }
        }

        return hediffs;
    }

    public static void FixImmunizableHealthConditions(
        Pawn pawn,
        List<HediffDef> inclusions = null,
        List<HediffDef> exclusions = null)
    {
        List<Hediff> hediffs = FindImmunizableHediffs(pawn, inclusions, exclusions);

        if (hediffs == null || hediffs.Count == 0)
            return;

        foreach (var hediff in hediffs)
            HealthUtility.Cure(hediff);
    }


    public static bool HasHediffOf(this Pawn pawn, HediffDef def)
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null || def is null) return false;

        return set.HasHediff(def);
    }

    public static bool HasHediff<T>(this Pawn pawn) where T : Hediff
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null) return false;

        return set.HasHediff<T>();
    }

    public static Hediff GetHediffOf(this Pawn pawn, HediffDef def)
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null || def is null) return null;

        if (set.TryGetHediff(def, out Hediff result))
            return result;
        return null;
    }

    public static T GetHediff<T>(this Pawn pawn) where T : Hediff
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null) return null;

        if (set.TryGetHediff<T>(out T result))
            return result;
        return null;
    }

    public static void ApplyHediff(
    this Pawn pawn,
    HediffDef def,
    BodyPartRecord part = null,
    float severity = -1,
    int duration = -1)
    {
        if (pawn == null || def == null)
            return;

        bool hasHediff = pawn.HasHediffOf(def);
        Hediff hediff = hasHediff
            ? pawn.GetHediffOf(def)
            : HediffMaker.MakeHediff(def, pawn, part);

        if (hediff == null)
            return;

        if (severity > -1)  // -1 means "leave as-is"/use default
        {
            if (hasHediff)
                hediff.Severity += severity;
            else
                hediff.Severity = severity;
        }

        if (duration > -1
            && hediff is HediffWithComps hediffWithComps
            && hediff.TryGetComp(out HediffComp_Disappears disappears))
        {
            disappears.ticksToDisappear = duration;
        }

        if (!hasHediff)
            pawn.health.AddHediff(hediff);
    }

    public static void RemoveHediff(this Pawn p, Hediff hediff)
    {
        var h = p?.health?.hediffSet?.GetFirstHediffOfDef(hediff.def);
        if (h != null) p.health.RemoveHediff(h);
    }

    public static void RemoveHediffOf(this Pawn p, HediffDef def)
    {
        var h = p?.health?.hediffSet?.GetFirstHediffOfDef(def);
        if (h != null) p.health.RemoveHediff(h);
    }

    public static bool TryResurrectPawn(Pawn pawn, ResurrectionParams parms = null)
    {
        if (!pawn.Dead)
        {
            Log.Error("Tried to resurrect a pawn who is not dead: " + pawn.ToStringSafe());
            return false;
        }

        if (pawn.Discarded)
        {
            Log.Error("Tried to resurrect a discarded pawn: " + pawn.ToStringSafe());
            return false;
        }

        Corpse corpse = pawn.Corpse;
        bool flag = false;
        IntVec3 loc = IntVec3.Invalid;
        Map map = null;
        if (ModsConfig.AnomalyActive && corpse is UnnaturalCorpse)
        {
            Messages.Message("MessageUnnaturalCorpseResurrect".Translate(corpse.InnerPawn.Named("PAWN")), corpse, MessageTypeDefOf.NeutralEvent);
            return false;
        }

        bool flag2 = Find.Selector.IsSelected(corpse);
        if (corpse != null)
        {
            flag = corpse.SpawnedOrAnyParentSpawned;
            loc = corpse.PositionHeld;
            map = corpse.MapHeld;
            corpse.InnerPawn = null;
            corpse.Destroy();
        }

        if (flag && pawn.IsWorldPawn())
            Find.WorldPawns.RemovePawn(pawn);

        pawn.ForceSetStateToUnspawned();
        PawnComponentsUtility.CreateInitialComponents(pawn);
        pawn.health.Notify_Resurrected(parms?.restoreMissingParts ?? true, parms?.gettingScarsChance ?? 0f);
        if (pawn.Faction != null && pawn.Faction.IsPlayer)
        {
            pawn.workSettings?.EnableAndInitialize();
            Find.StoryWatcher.watcherPopAdaptation.Notify_PawnEvent(pawn, PopAdaptationEvent.GainedColonist);
        }

        if (flag && (parms == null || !parms.dontSpawn))
        {
            GenSpawn.Spawn(pawn, loc, map);
            Lord lord = pawn.GetLord();
            if (lord != null)
                lord?.Notify_PawnUndowned(pawn);
            else if (pawn.Faction != null 
                && pawn.Faction != Faction.OfPlayer 
                && pawn.HostileTo(Faction.OfPlayer) 
                && (parms == null || !parms.noLord))
            {
                LordMaker.MakeNewLord(lordJob: parms == null 
                    ? new LordJob_MaraudColony(pawn.Faction) 
                    : new LordJob_MaraudColony(pawn.Faction, parms.canKidnap, parms.canTimeoutOrFlee, parms.sappers, parms.useAvoidGridSmart, parms.canSteal, parms.breachers, parms.canPickUpOpportunisticWeapons),
                    faction: pawn.Faction,
                    map: pawn.Map,
                    startingPawns: Gen.YieldSingle(pawn));
            }

            if (pawn.apparel != null)
            {
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    wornApparel[i].Notify_PawnResurrected(pawn);
                }
            }
        }

        if (parms != null && parms.removeDiedThoughts)
            PawnDiedOrDownedThoughtsUtility.RemoveDiedThoughts(pawn);

        pawn.royalty?.Notify_Resurrected();
        if (pawn.relations != null)
            pawn.relations.hidePawnRelations = false;

        if (pawn.guest != null && pawn.guest.IsInteractionEnabled(PrisonerInteractionModeDefOf.Execution))
            pawn.guest.SetNoInteraction();

        if (flag2 && pawn != null)
            Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);

        pawn.Drawer.renderer.SetAllGraphicsDirty();
        if (parms != null && parms.invisibleStun)
            pawn.stances.stunner.StunFor(5f.SecondsToTicks(), pawn, addBattleLog: false, showMote: false);

        pawn.needs.AddOrRemoveNeedsAsAppropriate();
        return true;
    }
}

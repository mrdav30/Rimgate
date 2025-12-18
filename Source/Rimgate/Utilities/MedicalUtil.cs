using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public static class MedicalUtil
{
    public const int MaxCorpseAgeForHarvest = 3;

    public const float MaxFrozenDecay = 0f;

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
        bool hasHediff = !pawn.HasHediffOf(def);
        Hediff hediff = !hasHediff
            ? HediffMaker.MakeHediff(def, pawn, part)
            : pawn.GetHediffOf(def);

        if (hediff == null) return;

        if (severity > -1)
            hediff.Severity = severity;

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

    public static IEnumerable<Thing> TraverseCorpseForBiomaterial(
        Corpse corpse,
        float skillChance,
        int validCorpseAge = MaxCorpseAgeForHarvest,
        float frozenDecay = MaxFrozenDecay,
        float organChance = 1f,
        float implantChance = 1f,
        int maxResults = 99)
    {
        Pawn subject = corpse?.InnerPawn;
        if (subject == null) return null;

        BodyPartRecord core = subject.RaceProps?.body?.corePart;
        List<BodyPartRecord> queue = new() { core };
        HediffSet hediffSet = subject.health?.hediffSet;
        List<Thing> results = new();
        List<BodyPartRecord> damagedParts = new();
        while (queue.Count > 0)
        {
            BodyPartRecord part = queue.First();
            queue.Remove(part);

            if (part == null) continue;

            bool gotParts = TryGetBiomaterialFromCorpse(corpse,
                part,
                skillChance,
                ref results,
                ref damagedParts,
                validCorpseAge: validCorpseAge,
                frozenDecay: frozenDecay,
                organChance: organChance,
                implantChance: implantChance);
            if (gotParts && core != part)
                continue;
            queue.AddRange(part.parts?
                .Where(x => hediffSet == null || !hediffSet.PartIsMissing(x)));
        }

        foreach (BodyPartRecord part in damagedParts)
            DamageProcuredParts(subject, part);

        if (results.Count > maxResults)
            return results.OrderBy(i => new Random().Next()).Take(maxResults);

        return results;
    }

    public static bool TryGetBiomaterialFromCorpse(
        Corpse corpse,
        BodyPartRecord part,
        float skillChance,
        ref List<Thing> result,
        ref List<BodyPartRecord> damagedParts,
        int validCorpseAge = MaxCorpseAgeForHarvest,
        float frozenDecay = MaxFrozenDecay,
        float organChance = 1f,
        float implantChance = 1f)
    {
        Pawn subject = corpse?.InnerPawn;
        if (subject == null || part == null) return false;

        if (organChance > 0 && IsCleanAndDroppable(subject, part))
        {
            damagedParts.Add(part);
            CompRottable rot = corpse.TryGetComp<CompRottable>();
            if (rot == null
                ? corpse.Age <= validCorpseAge
                : rot.RotProgress + (corpse.Age - rot.RotProgress) * frozenDecay <=
                  validCorpseAge)
            {
                if (Rand.Chance(Math.Min(skillChance, organChance)))
                    result.Add(ThingMaker.MakeThing(part.def.spawnThingOnRemoved));
                return true;
            }
        }

        if (implantChance <= 0)
            return false;

        List<Hediff> allImplants = subject.health.hediffSet.hediffs
            .Where(x =>
                part.Equals(x.Part)
                && x.def.spawnThingOnRemoved != null
                && (x is Hediff_Implant || x is Hediff_AddedPart))
            .ToList();

        if (allImplants.Count <= 0)
            return false;

        var foundImplants = allImplants
            .Where(x => Rand.Chance(Math.Min(skillChance, implantChance)))
            .Select(x => ThingMaker.MakeThing(x.def.spawnThingOnRemoved))
            .ToList();

        if (foundImplants.Count <= 0)
            return false;

        result.AddRange(foundImplants);

        // Destroy anything left
        if (!part.def.destroyableByDamage)
            allImplants.ForEach(x => subject.RemoveHediff(x));

        damagedParts.Add(part);
        return true;
    }

    public static bool IsCleanAndDroppable(Pawn pawn, BodyPartRecord part)
    {
        return pawn != null
            && pawn.RaceProps != null
            && !pawn.RaceProps.Animal
            && !pawn.RaceProps.IsAnomalyEntity
            && !pawn.RaceProps.IsMechanoid
            && part.def.spawnThingOnRemoved != null
            && !pawn.health.hediffSet.hediffs.Any(x => x.Part == part);
    }

    public static void DamageProcuredParts(Pawn p, BodyPartRecord part)
    {
        if (p == null || part == null) return;

        HediffSet set = p.health?.hediffSet;
        if (set == null) return;

        IEnumerable<BodyPartRecord> targets = set.GetNotMissingParts().Where(pa => part.parent == pa);
        List<BodyPartRecord> bodyPartRecords = targets?.ToList();

        if (bodyPartRecords?.Count == 0) return;

        float partHealth = set.GetPartHealth(part);
        if (partHealth >= float.Epsilon)
            DamagePart(p, part, (int)Math.Ceiling(partHealth));

        DateTime start = DateTime.Now;
        int totalSharedDamage = Rand.Range(5, 10);
        while (totalSharedDamage > 0 && bodyPartRecords.Count != 0)
        {
            if ((DateTime.Now - start).TotalSeconds > 1)
                return;

            if (!bodyPartRecords.TryRandomElementByWeight(x => x.coverageAbs, out BodyPartRecord bodyPartRecord))
                return;

            partHealth = set.GetPartHealth(bodyPartRecord);
            if (partHealth < float.Epsilon)
            {
                bodyPartRecords.Remove(bodyPartRecord);
                continue;
            }

            int num = Rand.Range(1, 3);

            DamagePart(p, bodyPartRecord, num);

            totalSharedDamage -= num;
        }
    }

    public static void DamagePart(Pawn p, BodyPartRecord part, int damage)
    {
        if (p == null || p.health == null || part == null || damage <= 0)
            return;

        HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(DamageDefOf.SurgicalCut, p, part);

        Hediff_Injury injury = HediffMaker.MakeHediff(hediffDefFromDamage, p, part) as Hediff_Injury;
        injury.Severity = damage;

        p.health.AddHediff(
            injury,
            part,
            new DamageInfo(DamageDefOf.SurgicalCut, damage, 999f, -1f, null, part));
        GenLeaving.DropFilthDueToDamage(p, damage);
    }
}

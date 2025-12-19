using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Verse;

namespace Rimgate;

public class HediffComp_Regeneration : HediffComp
{
    public HediffCompProperties_Regeneration Props => (HediffCompProperties_Regeneration)props;

    private static readonly List<BodyPartRecord> _tmpCandidates = new();

    public override void CompPostTick(ref float severityAdjustment)
    {
        int interval = Props.checkIntervalTicks;
        if (interval <= 0) interval = 2500;

        if (!parent.pawn.IsHashIntervalTick(interval))
            return;

        var pawn = parent.pawn;
        if (pawn.Dead || pawn.health?.hediffSet == null)
            return;

        bool didSomething = TryRegrowOneMissingPart(pawn)
            || (Props.healScars && TryHealOneScar(pawn))
            || (Props.healChronics && TryHealOneChronic(pawn));

        if (didSomething && Props.showHealingFleck && pawn.Spawned)
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
    }

    private bool TryRegrowOneMissingPart(Pawn pawn)
    {
        if (Props.regeneratingHediff == null) return false;

        _tmpCandidates.Clear();
        var hediffSet = pawn.health.hediffSet;
        var body = pawn.def.race.body;
        if (body == null) return false;

        // Find regrowable missing parts (parent exists, not replaced by added parts)
        foreach (var part in body.AllParts)
        {
            if (!hediffSet.PartIsMissing(part))
                continue;

            var parentPart = part.parent;
            if (parentPart == null)
                continue;

            if (hediffSet.PartIsMissing(parentPart))
                continue; // can’t regrow child if parent missing

            if (hediffSet.AncestorHasDirectlyAddedParts(part))
                continue; // respect bionics/added parts chain

            _tmpCandidates.Add(part);
        }

        if (_tmpCandidates.Count == 0)
            return false;

        // Snapshot missing-part hediffs before restore so we know which hediff(s) were removed.
        // We only need the ones matching the chosen branch, but keeping it simple:
        // collect refs to existing missing part hediffs (no LINQ).
        var beforeMissing = ListPool<Hediff_MissingPart>.Get();
        try
        {
            var hediffs = hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_MissingPart mp)
                    beforeMissing.Add(mp);
            }

            var partToRestore = _tmpCandidates.RandomElement();
            pawn.health.RestorePart(partToRestore);

            // After restore, add “regenerating tissue” for missing-part hediffs that disappeared.
            // Compare references: if a beforeMissing item is no longer present, it was restored.
            for (int i = 0; i < beforeMissing.Count; i++)
            {
                var oldMissing = beforeMissing[i];
                if (hediffSet.hediffs.Contains(oldMissing))
                    continue;

                var h = HediffMaker.MakeHediff(Props.regeneratingHediff, pawn, oldMissing.Part);
                h.Severity = oldMissing.Part.def.GetMaxHealth(pawn) - 1f;
                pawn.health.AddHediff(h);
            }

            return true;
        }
        finally
        {
            beforeMissing.Clear();
            ListPool<Hediff_MissingPart>.Release(beforeMissing);
            _tmpCandidates.Clear();
        }
    }

    private bool TryHealOneScar(Pawn pawn)
    {
        if (Props.healScarChance <= 0f) return false;
        if (Props.healScarChance < 1f 
            && !Rand.Chance(Props.healScarChance)) return false;

        var hediffs = pawn.health.hediffSet.hediffs;

        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is Hediff_Injury inj && inj.IsPermanent())
            {
                var h = HediffMaker.MakeHediff(Props.regeneratingHediff, pawn, inj.Part);
                // Permanent injury healing reduced since we aren't restoring whole limbs
                h.Severity = (inj.Part.def.GetMaxHealth(pawn) - 1f) * Props.healScarTimeFactor;

                pawn.health.RemoveHediff(inj);
                pawn.health.AddHediff(h);

                return true;
            }
        }

        return false;
    }

    private bool TryHealOneChronic(Pawn pawn)
    {
        if (Props.healChronicChance <= 0f) return false;
        if (Props.healChronicChance < 1f 
            && !Rand.Chance(Props.healChronicChance)) return false;

        var hediffSet = pawn.health.hediffSet;
        var hediffs = hediffSet.hediffs;

        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff h = hediffs[i];

            // Skip stuff we already handle or never want to remove
            if (h is Hediff_MissingPart) continue;
            // Chronic filter
            // Skip permanent injuries here (scars handled elsewhere)
            if (!h.def.isBad 
                || !h.def.chronic
                || h is Hediff_Injury) continue;

            // If the hediff is on a part,
            // respect "added parts" ancestry (avoids weirdness with prosthetics)
            if (h.Part != null && hediffSet.AncestorHasDirectlyAddedParts(h.Part))
                continue;

            // Heal action:
            if (h.def.lethalSeverity > 0f || h.def.maxSeverity > 0f || h.Severity > 0f)
            {
                // avoids *instant cures*
                // Example: subtract a small amount per check; scale with severity.
                float delta = 0.15f; // per interval; tune
                h.Severity = Mathf.Max(0f, h.Severity - delta);

                // Optionally add regen tissue marker on that body part (if any)
                if (Props.regeneratingHediff != null && h.Part != null)
                {
                    var regen = HediffMaker.MakeHediff(Props.regeneratingHediff, pawn, h.Part);
                    regen.Severity = (h.Part.def.GetMaxHealth(pawn) - 1f) * Props.healChronicTimeFactor;
                    pawn.health.AddHediff(regen);
                }

                // If it reaches 0, remove it
                if (h.Severity <= 0.001f)
                    pawn.health.RemoveHediff(h);

                return true;
            }

            // Binary chronic: just remove (rare)
            pawn.health.RemoveHediff(h);
            return true;
        }

        return false;
    }
}

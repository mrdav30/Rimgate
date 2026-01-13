using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Verse;
using Verse.Sound;
using static RimWorld.PsychicRitualRoleDef;

namespace Rimgate;

public class HediffComp_Regeneration : HediffComp
{
    private static readonly FleckDef RegeneratingTissueFleck = DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect");

    public HediffCompProperties_Regeneration Props => (HediffCompProperties_Regeneration)props;

    private static List<BodyPartRecord> _tmpCandidates;

    private int _resurrectionsLeft = 1;

    private ThingWithComps _cachedMechWeapon;

    public override void CompPostMake()
    {
        base.CompPostMake();
        _resurrectionsLeft = Props.resurrectionAttempts;
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (!parent.pawn.IsHashIntervalTick(Props.checkIntervalTicks, delta))
            return;

        var pawn = parent.pawn;
        if (pawn.Dead || pawn.health?.hediffSet == null || pawn.health.hediffSet.hediffs.Count == 0)
            return;

        if (pawn.IsColonyMech)
        {
            var energyNeed = pawn.needs.TryGetNeed<Need_MechEnergy>();
            if (energyNeed != null && energyNeed.CurLevel <= 0f)
                return; // out of energy, can’t heal
        }

        bool didSomething = TryRegrowOneMissingPart(pawn)
            || (Props.healInjuries && TryHealOneInjury(pawn))
            || (Props.healChronics && TryHealOneChronic(pawn));

        if (!Props.showHealingFleck || !parent.pawn.IsHashIntervalTick(Props.healingFleckInterval, delta))
            return;

        if (didSomething && pawn.Spawned)
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
    }

    private bool TryRegrowOneMissingPart(Pawn pawn)
    {
        _tmpCandidates ??= new List<BodyPartRecord>();
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

            if (pawn.IsColonyMech)
                AdjustMechEnergyUsage(pawn, partToRestore.def.GetMaxHealth(pawn));

            // After restore, add “regenerating tissue” for missing-part hediffs that disappeared.
            // Compare references: if a beforeMissing item is no longer present, it was restored.
            if (Props.regeneratingHediff != null)
            {
                for (int i = 0; i < beforeMissing.Count; i++)
                {
                    var oldMissing = beforeMissing[i];
                    if (hediffSet.hediffs.Contains(oldMissing))
                        continue;

                    var h = HediffMaker.MakeHediff(Props.regeneratingHediff, pawn, oldMissing.Part);
                    h.Severity = (oldMissing.Part.def.GetMaxHealth(pawn) - 1f) * Props.regenerateMissingTimeFactor;
                    pawn.health.AddHediff(h);
                }
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

    private bool TryHealOneInjury(Pawn pawn)
    {
        if (!Rand.Chance(Props.healInjuryChance)) return false;

        var hediffs = pawn.health.hediffSet.hediffs;

        float regenRate = Props.injuryRegeneration;
        regenRate *= 0.00025f; // per interval scaling

        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is not Hediff_Injury inj) continue;
            if (inj.IsPermanent())
            {
                if (!Props.healPermanentInjuries) continue;

                if (pawn.IsColonyMech)
                    AdjustMechEnergyUsage(pawn, inj.Severity);

                pawn.health.RemoveHediff(inj);

                if (Props.regeneratingHediff != null)
                {
                    var h = HediffMaker.MakeHediff(Props.regeneratingHediff, pawn, inj.Part);
                    // Permanent injury healing reduced since we aren't restoring whole limbs
                    h.Severity = inj.Severity * Props.regeneratePermanentTimeFactor;
                    pawn.health.AddHediff(h);
                }

                return true;
            }

            float regenCap = Mathf.Min(regenRate, inj.Severity);
            inj.Heal(regenCap);
            if (pawn.IsColonyMech)
                AdjustMechEnergyUsage(pawn, regenCap);
            pawn.health.hediffSet.Notify_Regenerated(regenCap);

            return true;
        }

        return false;
    }

    private bool TryHealOneChronic(Pawn pawn)
    {
        if (!Rand.Chance(Props.healChronicChance)) return false;

        var hediffSet = pawn.health.hediffSet;
        var hediffs = hediffSet?.hediffs;

        if(hediffs == null) return false;

        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff h = hediffs[i];
            if(!MedicalUtil.IsEligibleChronic(hediffSet, h)) continue;

            var maxHealth = h.Part?.def.GetMaxHealth(pawn) ?? 1;
            if (pawn.IsColonyMech)
                AdjustMechEnergyUsage(pawn, maxHealth);

            // avoids *instant cures*
            // Example: subtract a small amount per check; scale with severity.
            float delta = 0.15f; // per interval; tune
            h.Severity = Mathf.Max(0f, h.Severity - delta);

            // Optionally add regen tissue marker on that body part
            if (Props.regeneratingHediff != null && h.Part != null)
            {
                var regen = HediffMaker.MakeHediff(Props.regeneratingHediff, pawn, h.Part);
                regen.Severity = (h.Part.def.GetMaxHealth(pawn) - 1f) * Props.regenerateChronicTimeFactor;
                pawn.health.AddHediff(regen);
            }

            // If it reaches 0, remove it
            if (h.Severity <= 0.001f)
                pawn.health.RemoveHediff(h);

            return true;
        }

        return false;
    }

    private void AdjustMechEnergyUsage(Pawn pawn, float amount)
    {
        var energyNeed = pawn.needs.TryGetNeed<Need_MechEnergy>();
        if (energyNeed == null) return;
        float energyLoss = Mathf.Clamp(amount * pawn.GetStatValue(StatDefOf.MechEnergyLossPerHP), 0.05f, 1f);
        energyNeed.CurLevel -= energyLoss;
    }

    public override void Notify_PawnKilled()
    {
        var pawn = parent.pawn;
        if (pawn.RaceProps.IsMechanoid && Props.restoreMechWeaponOnDeath)
        {
            var mechWeapon = pawn.equipment.Primary;
            if (mechWeapon != null)
            {
                _cachedMechWeapon = mechWeapon;
                pawn.equipment.Remove(mechWeapon);
            }
        }
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        if (!Props.canResurrect) return;

        var corpse = parent.pawn?.Corpse;
        var inner = corpse?.InnerPawn;
        Map map = corpse?.Map;
        if (map == null || _resurrectionsLeft < 1) return;
        // Only resurrect if brain is not missing or destroyed
        BodyPartRecord brain = inner.health.hediffSet.GetBrain();
        if (brain == null
            || inner.health.hediffSet.PartIsMissing(brain)
            || inner.health.hediffSet.GetPartHealth(brain) <= 0f)
            return;

        Delay.AfterNTicks(Props.resurrectionDelayRange.RandomInRange, () =>
        {
            SoundDefOf.PsychicPulseGlobal.PlayOneShot(new TargetInfo(corpse.Position, corpse.Map));
            FleckMaker.AttachedOverlay(corpse, RegeneratingTissueFleck, Vector3.zero);
            ResurrectionParams resurrectionParams = new ResurrectionParams
            {
                restoreMissingParts = false,
                breachers = true,
                canPickUpOpportunisticWeapons = true
            };
            MedicalUtil.TryResurrectPawn(inner, resurrectionParams);
            _resurrectionsLeft--;

            if (_cachedMechWeapon != null)
            {
                inner.equipment.AddEquipment(_cachedMechWeapon);
                _cachedMechWeapon = null;
            }

            if (!Props.resurrectionMessageKey.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(inner))
            {
                Messages.Message(Props.resurrectionMessageKey.Translate(inner.Named("PAWN")),
                    new TargetInfo(inner.Position, inner.Map),
                    MessageTypeDefOf.PositiveEvent);
            }
        });
    }

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref _resurrectionsLeft, "_resurrectionsLeft", 1);
        Scribe_Values.Look(ref _cachedMechWeapon, "_cachedMechWeapon");
    }
}

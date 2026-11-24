using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Hediff_SymbioteImplant_Ext : DefModExtension
{
    public int chronicHealCheckTicks = 30000; // once every in-game day

    public float chronicHealChance = 0.15f;
}

public class Hediff_SymbioteImplant : Hediff_Implant
{
    public HediffComp_SymbioteHeritage Heritage => _heritage ??= GetComp<HediffComp_SymbioteHeritage>();

    public Hediff_SymbioteImplant_Ext Props => _cachedProps ??= def.GetModExtension<Hediff_SymbioteImplant_Ext>();

    public override bool Visible => true;

    public string SymbioteLabel
    {
        get
        {
            if (!_cachedSymbioteLabel.NullOrEmpty() || Heritage == null)
                return _cachedSymbioteLabel;

            var name = Heritage?.Memory.SymbioteName;
            if (name.NullOrEmpty()) return null;

            var baseLabel = "RG_SymbioteMemory_Name".Translate(name);
            var suffix = SymbioteLimitSuffix;

            _cachedSymbioteLabel = suffix.NullOrEmpty()
                ? baseLabel
                : $"{baseLabel} {suffix}";

            return _cachedSymbioteLabel;
        }
    }

    public override string LabelBase => Heritage != null && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelBase;

    public override string Label => Heritage != null && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.Label;

    public override string LabelInBrackets => Heritage != null && !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelInBrackets;

    public string SymbioteLimitSuffix
    {
        get
        {
            if (Heritage == null) return null;
            // If the symbiote is already over limit, indicate it loudly.
            // Otherwise, if it is at the limit, indicate it's at max.
            var memory = Heritage.Memory;
            if (memory.IsOverLimit) return "RG_SymbioteMemory_OverLimit_LabelSuffix".Translate();
            if (memory.IsAtLimit) return "RG_SymbioteMemory_AtLimit_LabelSuffix".Translate();
            return null;
        }
    }

    private string _cachedSymbioteLabel;

    private bool _immediateRejection;

    private Hediff_SymbioteImplant_Ext _cachedProps;

    private HediffComp_SymbioteHeritage _heritage;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        bool isConfigStage = Current.ProgramState != ProgramState.Playing;

        // Safety: don't allow pawns that already have a symbiote
        if (!IsValidHost(out string reason))
        {
            // Spawn mature symbiote item at pawn's position
            if (Heritage != null && pawn.Map != null)
            {
                if (ThingMaker.MakeThing(RimgateDefOf.Rimgate_GoauldSymbiote) is Thing_GoualdSymbiote thing)
                {
                    if (thing.Heritage != null)
                    {
                        thing.Heritage.AssumeMemory(Heritage?.Memory);
                        thing.Heritage.AssumeQueenLineage(Heritage?.QueenLineage);
                    }

                    GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }
            }

            if (!isConfigStage && pawn.Faction.IsOfPlayerFaction())
                Messages.Message(
                    reason,
                    pawn,
                    MessageTypeDefOf.ThreatSmall);

            _immediateRejection = true;

            pawn.health.RemoveHediff(this);

            return;
        }

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal);

        // None of the below applies to anything non-human (i.e. Unas)
        if (!pawn.RaceProps.Humanlike) return;

        // Mature symbiote will remove the pouch
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbiotePouch);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PouchDegeneration);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_TretoninAddiction);

        if (!isConfigStage)
            RimgateEvents.Notify_ColonyOfPawnEvent(pawn, RimgateDefOf.Rimgate_InstalledSymbiote);

        if (Heritage == null) return;

        Heritage.Memory ??= new SymbioteMemory();
        Heritage.Memory.EnsureName();

        // Copy memory into hediff and apply bonuses to host
        Heritage.ApplyMemoryPostEffect(pawn);

        if (!isConfigStage && pawn.IsFreeColonist)
        {
            var msg = "RG_SymbioteSkillInheritance".Translate(
                pawn.Named("PAWN"),
                Heritage.Memory.SymbioteName);
            Messages.Message(
                msg,
                pawn,
                MessageTypeDefOf.PositiveEvent);
        }
    }

    public override void PostTick()
    {
        base.PostTick();

        if (pawn == null || pawn.Dead) return;
        var hs = pawn.health?.hediffSet;
        if (hs == null || hs.hediffs.Count == 0) return;

        if (!pawn.IsHashIntervalTick(Props.chronicHealCheckTicks)) return;

        if (Heritage != null)
        {
            var memory = Heritage.Memory;
            if (memory.IsOverLimit == true) return; // symbiote is exhausted; no free cures
        }

        if (!Rand.Chance(Props.chronicHealChance)) return;

        if (MedicalUtil.TryHealOneChronic(pawn) && pawn.Spawned)
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
    }

    public bool IsValidHost(out string reason)
    {
        reason = null;

        if (pawn.HasHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch))
        {
            reason = "RG_RejectHost_HasSymbiote".Translate(pawn.Named("PAWN"));
            return false;
        }

        return true;
    }

    public override void PostRemoved()
    {
        base.PostRemoved();

        if (_immediateRejection || pawn == null || pawn.health == null || Heritage == null)
            return;

        if (!pawn.Dead
            && pawn.needs.TryGetNeed(RimgateDefOf.Rimgate_TretoninChemicalNeed) == null
            && !pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal))
        {
            var wd = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteWithdrawal, pawn);
            pawn.health.AddHediff(wd);
        }

        TrySpawnSymbiote();
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (var stat in base.SpecialDisplayStats(req))
            yield return stat;

        if (Heritage != null)
        {
            foreach (var stat in Heritage.GetSpecialDisplayStats())
                yield return stat;
        }
    }

    public override void Notify_PawnKilled()
    {
        _immediateRejection = true;
        base.Notify_PawnKilled();
        TrySpawnSymbiote();
        pawn.RemoveHediff(this);
    }

    private void TrySpawnSymbiote()
    {
        if (Heritage == null || !pawn.RaceProps.Humanlike)
            return;

        var hediffMemory = Heritage.Memory;
        var hediffLineage = Heritage.QueenLineage;
        // Undo this symbiote's bonuses on the current host
        hediffMemory?.RemoveSessionBonuses(pawn);

        if (pawn.Map == null) return;

        // Preserve its accumulated memory and inherit skills from previous host,
        // regardless of how the symbiote is removed
        if (ThingMaker.MakeThing(RimgateDefOf.Rimgate_GoauldSymbiote) is not Thing_GoualdSymbiote thing)
            return;

        var heritageComp = thing.Heritage;
        if (heritageComp != null)
        {
            heritageComp.AssumeMemory(hediffMemory);
            heritageComp.AssumeQueenLineage(hediffLineage);
            heritageComp.ApplyMemoryPostRemoval(pawn);
            var memory = heritageComp.Memory;

            if (memory?.IsOverLimit == true)
            {
                Find.LetterStack.ReceiveLetter(
                    "RG_SymbioteMemory_MaxHostsPerishedLabel".Translate(),
                    "RG_SymbioteMemory_MaxHostsPerishedText".Translate(memory.SymbioteName),
                    LetterDefOf.NeutralEvent,
                    pawn);
                thing.Destroy();

                return;
            }

            if (memory?.IsAtLimit == true)
                Find.LetterStack.ReceiveLetter(
                    "RG_SymbioteMemory_MaxHostsReachedLabel".Translate(),
                    "RG_SymbioteMemory_MaxHostsReachedText".Translate(memory.SymbioteName),
                    LetterDefOf.NeutralEvent,
                    pawn);
        }

        GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
    }
}

using RimWorld;
using System.Text;
using Verse;

namespace Rimgate;

public class Hediff_SymbioteImplant : Hediff_Implant
{
    public HediffComp_SymbioteHeritage Heritage => _heritage ?? GetComp<HediffComp_SymbioteHeritage>();

    public override bool Visible => true;

    public string SymbioteLabel => Heritage?.Memory?.SymbioteName.NullOrEmpty() ?? true
        ? null
        : "RG_SymbioteMemory_Name".Translate(Heritage.Memory.SymbioteName);

    public override string LabelBase => !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.LabelBase;

    public override string Label => !SymbioteLabel.NullOrEmpty()
        ? SymbioteLabel
        : base.Label;

    public override string LabelInBrackets => !SymbioteLabel.NullOrEmpty()
    ? SymbioteLabel
    : base.LabelInBrackets;

    private bool _skipWithdrawl;

    private HediffComp_SymbioteHeritage _heritage;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        bool isConfigStage = Find.GameInitData != null;

        // Safety: don't allow pawns that already have a symbiote
        if (!IsValidHost(out string reason))
        {
            // Spawn mature symbiote item at pawn's position
            if (pawn.Map != null)
            {
                var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_GoauldSymbiote);
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }

            if (!isConfigStage && pawn.Faction.IsOfPlayerFaction())
                Messages.Message(
                    reason,
                    pawn,
                    MessageTypeDefOf.ThreatSmall);

            _skipWithdrawl = true;

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

        if (!isConfigStage && pawn.Faction.IsOfPlayerFaction())
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(RimgateDefOf.Rimgate_InstalledSymbiote, pawn.Named(HistoryEventArgsNames.Doer)));

        if (Heritage == null) return;

        Heritage.Memory ??= new SymbioteMemory();
        Heritage.Memory.EnsureName();

        // Copy memory into hediff and apply bonuses to host
        Heritage.ApplyMemoryPostEffect(pawn);

        if (!isConfigStage && pawn.Faction.IsOfPlayerFaction())
        {
            var msg = "RG_SymbioteSkillInheritance".Translate(
                pawn.Named("PAWN"),
                Heritage.Memory?.SymbioteName);
            Messages.Message(
                msg,
                pawn,
                MessageTypeDefOf.PositiveEvent);
        }
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

        if (pawn == null || pawn.health == null)
            return;

        // If this was a rejection or internal event we flagged, skip spawn + withdrawal
        if (_skipWithdrawl)
            return;

        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal))
        {
            var wd = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteWithdrawal, pawn);
            pawn.health.AddHediff(wd);
        }

        if (Heritage == null || !pawn.RaceProps.Humanlike)
            return;

        var hediffMemory = Heritage.Memory;
        // Undo this symbiote's bonuses on the current host
        hediffMemory?.RemoveSessionBonuses(pawn);

        if (pawn.Map == null) return;
        var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_GoauldSymbiote) as Thing_GoualdSymbiote;

        // Preserve its accumulated memory and inherit skills from previous host,
        // regardless of how the symbiote is removed
        var heritageComp = thing.Heritage;
        if (heritageComp != null)
        {
            heritageComp.AssumeMemory(hediffMemory);
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
using RimWorld;
using RimWorld.QuestGen;
using System;
using Verse;

namespace Rimgate;

public class CompUseEffect_AssembleAndStartQuest : CompUseEffect
{
    public CompProperties_UseEffectAssembleAndStartQuest Props => (CompProperties_UseEffectAssembleAndStartQuest)props;

    public override AcceptanceReport CanBeUsedBy(Pawn p)
    {
        if (Props.requiredProjectDef != null && !Props.requiredProjectDef.IsFinished)
        {
            var message = "RG_CannotDecode".Translate("RG_CannotDecode_Research".Translate(Props.requiredProjectDef.label));
            return new AcceptanceReport(message);
        }

        if (Utils.HasActiveQuestOf(Props.questScript))
        {
            return new AcceptanceReport("RG_CannotDecode".Translate("RG_CannotDecode_QuestActive".Translate()));
        }

        int have = TreasureCipherUtility.CountColonyReachableFragments(p, parent);
        if (have < Props.requiredCount)
        {
            var message = "RG_CannotDecode".Translate("RG_CannotDecode_Count".Translate(Props.requiredCount, parent.LabelShort, have));
            return new AcceptanceReport(message);
        }

        return true;
    }

    public override void DoEffect(Pawn usedBy)
    {
        var availableFragments = TreasureCipherUtility.CollectLocalFragments(usedBy, parent, Props.nearbySearchRadius);
        int availableCount = 0;
        for (int i = 0; i < availableFragments.Count; i++)
            availableCount += availableFragments[i].stackCount;

        if (availableCount < Props.requiredCount)
        {
            Messages.Message(
                "RG_CannotDecode".Translate("RG_CannotDecode_Count".Translate(Props.requiredCount, parent.LabelShort, availableCount)),
                MessageTypeDefOf.RejectInput,
                historical: false);
            return;
        }

        // 1) Start quest
        var slate = new Slate();
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(Props.questScript, slate);

        if (quest.State != QuestState.Ongoing)
        {
            Log.ErrorOnce("Failed to start assemble quest.", 12345679);
            Messages.Message("RG_CannotDecode_JobFailedMessage".Translate(usedBy.Named("PAWN")),
                             MessageTypeDefOf.RejectInput,
                             historical: false);
            return;
        }

        QuestUtility.SendLetterQuestAvailable(quest);

        // 2) Optional extra letter
        if (!Props.letterLabel.NullOrEmpty())
        {
            Find.LetterStack.ReceiveLetter(
                Props.letterLabel.Translate(),
                Props.letterText.Translate(),
                LetterDefOf.PositiveEvent);
        }

        // 3) Consume required fragments from local sources (used stack, carried, inventory, nearby ground).
        int needed = Props.requiredCount;
        for (int i = 0; i < availableFragments.Count && needed > 0; i++)
        {
            Thing thing = availableFragments[i];
            if (thing == null || thing.Destroyed)
                continue;

            int take = Math.Min(needed, thing.stackCount);
            if (take <= 0)
                continue;

            Thing consumed = thing.SplitOff(take);
            consumed?.Destroy(DestroyMode.Vanish);
            needed -= take;
        }

        if (needed > 0)
            LogUtil.Warning($"Assembled cipher quest started but {needed} required fragment(s) were not consumed.");
    }

    public override void PrepareTick() { }
}

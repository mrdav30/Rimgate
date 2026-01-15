using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Rimgate;

// TODO: this should really be a job driver instead of a CompUseEffect, similiar to JobDriver_DecodeGlyphs
// however, the new job driver will need to account for the pawn retrieving all required fragments from storage etc.
// or even simpler, make sure there are enough fragments in the specific pawns inventory, being held, or on the ground nearby.
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

        // TODO: configure how many sites can be active at once ?
        if (Utils.HasActiveQuestOf(Props.questScript))
        {
            return new AcceptanceReport("RG_CannotDecode".Translate("RG_CannotDecode_QuestActive".Translate()));
        }

        int have = TotalFragmentsInPlayer();
        if (have < Props.requiredCount)
        {
            var message = "RG_CannotDecode".Translate("RG_CannotDecode_Count".Translate(Props.requiredCount, parent.LabelShort, have));
            return new AcceptanceReport(message);
        }

        return true;
    }

    public override void DoEffect(Pawn usedBy)
    {
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

        // 3) Consume the item that was actually used (in the pawn’s hands)
        int needed = Props.requiredCount;
        if (parent != null && !parent.Destroyed)
        {
            parent.SplitOff(1).Destroy();
            needed--;
        }

        if (needed > 0)
        {
            // 4) Consume the remaining fragments from player holdings
            var candidates = EnumeratePlayerFragments().ToList();
            foreach (var thing in candidates)
            {
                if (needed <= 0) break;
                int take = Math.Min(needed, thing.stackCount);
                thing.SplitOff(take).Destroy(DestroyMode.Vanish);
                needed -= take;
            }
        }
    }

    public override void PrepareTick() { }

    private int TotalFragmentsInPlayer()
    {
        int total = 0;
        foreach (var t in EnumeratePlayerFragments()) total++;
        return total;
    }

    private IEnumerable<Thing> EnumeratePlayerFragments()
    {
        var def = parent.def;

        // 1) Player maps (spawned items)
        foreach (var map in Find.Maps)
        {
            // Count only player-controlled maps
            if (!(map.IsPlayerHome || (map.Parent?.Faction.IsOfPlayerFaction() ?? true))) 
                continue;

            // a) Spawned items (on ground / in storage) not forbidden.
            var spawnedList = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < spawnedList.Count; i++)
            {
                var thing = spawnedList[i];
                if (thing.Destroyed || thing.IsForbidden(Faction.OfPlayer))
                    continue;
                yield return thing;
            }

            // b) Player pawns’ inventories on this map
            var pawns = map.mapPawns?.PawnsInFaction(Faction.OfPlayer);
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    var inv = pawns[i].inventory?.innerContainer;
                    if (inv == null) continue;
                    for (int j = 0; j < inv.Count; j++)
                        if (inv[j].def == def) yield return inv[j];
                }
            }
        }

        if (!Props.checkCaravans) yield break;

        // 2) Player caravans (AllThings already includes inventories)
        foreach (var caravan in Find.WorldObjects.Caravans)
        {
            if (!caravan.Faction.IsOfPlayerFaction()) continue;
            var things = caravan.AllThings;
            foreach (var thing in things)
                if (thing.def == def) yield return thing;
        }
    }
}
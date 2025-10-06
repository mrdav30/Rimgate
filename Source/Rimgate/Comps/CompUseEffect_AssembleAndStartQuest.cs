using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Rimgate;

public class CompUseEffect_AssembleAndStartQuest : CompUseEffect
{
    public CompProperties_AssembleAndStartQuest Props => (CompProperties_AssembleAndStartQuest)props;

    public override AcceptanceReport CanBeUsedBy(Pawn p)
    {
        if (Props.requiredProjectDef != null 
            && !Props.requiredProjectDef.IsFinished)
        {
            return new AcceptanceReport("RG_CannotDecodeCipherResearch".Translate(Props.requiredProjectDef.label));
        }

        int have = TotalFragmentsInPlayer();
        if (have < Props.requiredCount)
        {
            return new AcceptanceReport("RG_CannotDecodeCipherCount".Translate(Props.requiredCount, parent.LabelShort, have));
        }

        return true;
    }

    public override void DoEffect(Pawn usedBy)
    {
        // 1) Consume the item that was actually used (in the pawn’s hands)
        int needed = Props.requiredCount;
        if (parent != null && !parent.Destroyed)
        {
            parent.SplitOff(1).Destroy();
            needed--;
        }

        if (needed > 0)
        {
            // 2) Consume the remaining fragments from player holdings
            var candidates = EnumeratePlayerFragments().ToList();
            foreach (var thing in candidates)
            {
                if (needed <= 0) break;
                thing.SplitOff(1).Destroy();
                needed -= 1;
            }
        }

        // 3) Start quest
        var slate = new Slate();
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(Props.questScript, slate);
        QuestUtility.SendLetterQuestAvailable(quest);

        // 4) Optional extra letter
        if (!Props.letterLabel.NullOrEmpty())
        {
            Find.LetterStack.ReceiveLetter(
                Props.letterLabel.Translate(),
                Props.letterText.Translate(),
                LetterDefOf.PositiveEvent);
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
            if (!(map.IsPlayerHome || map.Parent?.Faction == Faction.OfPlayer)) continue;

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
            if (caravan.Faction != Faction.OfPlayer) continue;
            var things = caravan.AllThings;
            foreach (var thing in things)
                if (thing.def == def) yield return thing;
        }
    }
}
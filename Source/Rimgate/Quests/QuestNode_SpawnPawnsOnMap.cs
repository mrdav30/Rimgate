using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class QuestNode_SpawnPawnsOnMap : QuestNode
{
    public SlateRef<string> inSignal = "site.MapGenerated";

    public SlateRef<FactionDef> factionDef;

    public SlateRef<PawnKindDef> pawnKind;

    public SlateRef<List<PawnInventoryOption>> inventoryOptions;

    public SlateRef<float> skipChance;

    public SlateRef<IntRange> countRange = new IntRange(0, 2);

    public SlateRef<bool> alwaysAtLeastOne = false;

    public SlateRef<bool> spawnNearGate = true;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;

        var part = new QuestPart_SpawnPawnsOnSignal
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)),
            factionDef = factionDef.GetValue(slate),
            pawnKind = pawnKind.GetValue(slate),
            skipChance = skipChance.GetValue(slate),
            countRange = countRange.GetValue(slate),
            alwaysAtLeastOne = alwaysAtLeastOne.GetValue(slate),
            spawnNearGate = spawnNearGate.GetValue(slate)
        };

        var site = slate.Get<Site>("site");
        if (site != null) part.mapParent = site;

        QuestGen.quest.AddPart(part);
    }

    protected override bool TestRunInt(Slate slate) => true;
}

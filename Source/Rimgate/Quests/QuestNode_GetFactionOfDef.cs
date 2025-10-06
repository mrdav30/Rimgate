using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Rimgate;

public class QuestNode_GetFactionOfDef : QuestNode
{
    public SlateRef<FactionDef> factionDef;
    public SlateRef<string> storeAs;

    protected override void RunInt() => Resolve(QuestGen.slate);

    protected override bool TestRunInt(Slate slate) => Resolve(slate);

    private bool Resolve(Slate slate)
    {
        Faction var = Find.FactionManager.FirstFactionOfDef(factionDef.GetValue(slate));
        if (var == null)
            return false;
        slate.Set<Faction>(storeAs.GetValue(slate), var);
        return true;
    }
}
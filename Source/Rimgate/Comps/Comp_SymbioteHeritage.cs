using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimgate;

public class Comp_SymbioteHeritage : ThingComp
{
    public SymbioteMemory Memory;

    public SymbioteQueenLineage QueenLineage;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref Memory, "Memory");
        Scribe_Deep.Look(ref QueenLineage, "QueenLineage");
    }

    public void AssumeMemory(SymbioteMemory memory)
    {
        Memory = SymbioteMemory.DeepCopy(memory);
    }

    public void AssumeQueenLineage(SymbioteQueenLineage lineage)
    {
        QueenLineage = SymbioteQueenLineage.DeepCopy(lineage);
    }

    public void ApplyMemoryPostRemoval(Pawn host)
    {
        if (Memory == null) return;

        Memory.MarkPreviousHost(host);
        if (Memory.IsOverLimit) // cap how many times we inherit skills on removal
            return;

        var skills = host?.skills?.skills;
        if (skills == null) return;

        foreach (var sk in skills)
        {
            if (sk.TotallyDisabled) continue;
            // must be at least "adept" post symbiote removal
            if (sk.Level < SymbioteMemory.PawnMinSkillLevel) continue;
            Memory.AddRandomBonus(sk.def);
        }
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (var stat in Memory.GetStatDrawEntries())
            yield return stat;

        if (QueenLineage == null) yield break;

        foreach (var stat in QueenLineage.GetStatDrawEntries())
            yield return stat;
    }
}

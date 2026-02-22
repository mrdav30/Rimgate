using System.Text;
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

    public override string GetDescriptionPart()
    {
        if (Memory == null) return null;
        StringBuilder sb = new StringBuilder();
        var skills = Memory.SkillDescription;
        if (skills != null) sb.Append(skills);
        return sb.Length > 0
            ? sb.ToString()
            : null;
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
            if (sk.Level < 6) continue;
            Memory.AddRandomBonus(sk.def);
        }
    }
}

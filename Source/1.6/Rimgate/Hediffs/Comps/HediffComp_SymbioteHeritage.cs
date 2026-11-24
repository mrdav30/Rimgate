using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimgate;

public class HediffComp_SymbioteHeritage : HediffComp
{
    public SymbioteMemory Memory = new();

    public SymbioteQueenLineage QueenLineage;

    public override void CompExposeData()
    {
        base.CompExposeData();
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

    public void ApplyMemoryPostEffect(Pawn host)
    {
        if (Memory == null) return;

        var skills = host?.skills?.skills;
        if (skills == null) return;

        foreach (var sk in skills)
        {
            if (sk.TotallyDisabled) continue;

            // must be at least *adept* and the symbiote not over limit to pass on bonuses
            if (sk.Level >= SymbioteMemory.PawnMinSkillLevel && !Memory.IsOverLimit)
                Memory.AddRandomBonus(sk.def);

            // pawn still recieves bonus from symbiote,
            // even if they aren't *adept*
            int totalBonusLevels = Memory.GetBonus(sk.def);
            if (totalBonusLevels <= 0) continue;
            int newLevel = Math.Min(sk.Level + totalBonusLevels, SymbioteMemory.MaxSkillLevel);
            sk.Level = newLevel;
        }
    }

    public IEnumerable<StatDrawEntry> GetSpecialDisplayStats()
    {
        foreach (var stat in Memory.GetStatDrawEntries())
            yield return stat;

        if (QueenLineage == null) yield break;

        foreach (var stat in QueenLineage.GetStatDrawEntries())
            yield return stat;
    }
}

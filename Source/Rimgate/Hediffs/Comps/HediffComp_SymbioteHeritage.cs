using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffComp_SymbioteHeritage : HediffComp
{
    public SymbioteMemory Memory = new SymbioteMemory();

    public override string CompDescriptionExtra
    {
        get
        {
            if (Memory == null) return null;
            StringBuilder sb = new StringBuilder();
            var skillDesc = Memory.SkillDescription;
            if (!skillDesc.NullOrEmpty())
            {
                sb.AppendLine();
                sb.Append(skillDesc);
            }

            return sb.Length > 0
                ? sb.ToString()
                : null;
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Deep.Look(ref Memory, "Memory");
    }

    public void AssumeMemory(SymbioteMemory memory)
    {
        Memory = SymbioteMemory.DeepCopy(memory);
    }

    public void ApplyMemoryPostEffect(Pawn host)
    {
        if (Memory == null) return;

        var skills = host?.skills?.skills;
        if (skills == null) return;

        foreach (var sk in skills)
        {
            if (sk.TotallyDisabled) continue;

            // must be at least *adept* and the symbiote able to accept
            if (sk.Level >= 6 && !Memory.IsOverLimit)
                Memory.AddRandomBonus(sk.def);

            // pawn still recieves bonus from symbiote,
            // even if they aren't *adept*
            int totalBonusLevels = Memory.GetBonus(sk.def);
            if (totalBonusLevels <= 0) continue;
            int newLevel = Math.Min(sk.Level + totalBonusLevels, 20);
            sk.Level = newLevel;
        }
    }
}

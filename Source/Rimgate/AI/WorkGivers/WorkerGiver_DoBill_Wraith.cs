using HarmonyLib;
using RimWorld;
using Verse.Grammar;
using Verse;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_DoBillWraith : WorkGiver_DoBill
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (!pawn.HasActiveGeneOf(RimgateDefOf.Rimgate_WraithCocoonTrap))
            return true;

        return base.ShouldSkip(pawn, forced);
    }
}

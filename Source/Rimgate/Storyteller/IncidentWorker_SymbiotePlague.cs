using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimgate;

public class IncidentWorker_SymbiotePlague : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms) => AnyEligibleHost(parms.target);

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        List<Pawn> candidates = GetEligibleHosts(parms.target).ToList();
        int num = candidates.Count;
        if (num == 0)
            return false;

        // Infect a random amount of hosts depending on how many we have
        int randomInRange = new IntRange(
                Mathf.RoundToInt((float)num * def.diseaseVictimFractionRange.min),
                Mathf.RoundToInt((float)num * def.diseaseVictimFractionRange.max)
            ).RandomInRange;
        int countToInfect = Mathf.Clamp(randomInRange, 1, def.diseaseMaxVictims);
        candidates.Shuffle();

        var infected = ApplyToPawns(candidates.Take(countToInfect).ToList(), out string blockedInfo);

        // Letter
        TaggedString baseLetterText;
        if (infected.Count > 0)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < infected.Count; i++)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.AppendLine();

                stringBuilder.AppendTagged("  - " + infected[i].LabelNoCountColored.Resolve());
            }

            baseLetterText = def.letterText.Formatted(
                    infected.Count.ToString(),
                    Faction.OfPlayer.def.pawnsPlural,
                    RimgateDefOf.Rimgate_SymbiotePlague.label)
                .Resolve() + ":\n\n" + stringBuilder;
        }
        else
            baseLetterText = string.Empty;

        if (!blockedInfo.NullOrEmpty())
        {
            if (!baseLetterText.NullOrEmpty())
                baseLetterText += "\n\n";

            baseLetterText += blockedInfo;
        }

        SendStandardLetter(def.letterLabel, baseLetterText, def.letterDef, parms, infected);
        return true;
    }

    private bool AnyEligibleHost(IIncidentTarget target) => GetEligibleHosts(target).Count() != 0;

    private IEnumerable<Pawn> GetEligibleHosts(IIncidentTarget target)
    {
        if (target is Map map)
        {
            return map.mapPawns.FreeColonistsAndPrisonersSpawned
                .Where(p =>
                    !p.Dead
                    && (p.ParentHolder == null
                        || !(p.ParentHolder is Building_CryptosleepCasket
                            || p.ParentHolder is Building_Sarcophagus
                            || p.ParentHolder is Building_WraithCloningPod
                            || p.ParentHolder is CompBiosculpterPod))
                    && p.HasSymbiote()
                    && !p.HasHediff(RimgateDefOf.Rimgate_SymbiotePlague));
        }

        if (target is Caravan caravan)
        {
            return caravan.PawnsListForReading
                .Where(x =>
                    !x.Dead
                    && (x.IsFreeColonist || x.IsPrisonerOfColony)
                    && x.HasSymbiote()
                    && !x.HasHediff(RimgateDefOf.Rimgate_SymbiotePlague));
        }

        return new List<Pawn>();
    }

    public List<Pawn> ApplyToPawns(List<Pawn> pawns, out string blockedInfo)
    {
        List<Pawn> applied = new List<Pawn>();
        List<Pawn> blocked = new List<Pawn>();
        foreach (Pawn p in pawns)
        {
            float chance = 1f;
            bool hasImmunity = false;
            var immunityRecord = p.health.immunity.GetImmunityRecord(RimgateDefOf.Rimgate_SymbiotePlague);
            if (immunityRecord != null)
            {
                hasImmunity = true;
                chance = Mathf.Lerp(1f, 0f, immunityRecord.immunity / 0.6f);
            }

            if (Rand.Chance(chance))
            {
                p.health.AddHediff(RimgateDefOf.Rimgate_SymbiotePlague);
                TaleRecorder.RecordTale(
                    TaleDefOf.IllnessRevealed,
                    p,
                    RimgateDefOf.Rimgate_SymbiotePlague);
                applied.Add(p);
            }
            else if (hasImmunity)
                blocked.Add(p);
        }

        blockedInfo = "";
        if (blocked.Count != 0)
        {
            var label = RimgateDefOf.Rimgate_SymbiotePlague.label;
            blockedInfo = blockedInfo + "RG_Letter_DiseaseBlocked".Translate(label).Resolve()
                + ":\n"
                + blocked.Select(victim => victim.LabelNoCountColored.Resolve()).ToLineList("  - ");
        }

        return applied;
    }
}

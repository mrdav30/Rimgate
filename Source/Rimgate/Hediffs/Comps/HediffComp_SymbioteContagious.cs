using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffComp_SymbioteContagious : HediffComp
{
    private const int IntervalCheck = 250; // roughly 4 times per in-game hour

    public HediffCompProperties_SymbioteContagious Props => (HediffCompProperties_SymbioteContagious)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (!parent.pawn.IsHashIntervalTick(IntervalCheck))
            return;

        if (!Rand.MTBEventOccurs(Props.infectionMtbDays, 60000f, IntervalCheck))
            return;

        TryInfectNearbyHosts(parent.pawn);
    }

    private void TryInfectNearbyHosts(Pawn source)
    {
        Map map = source.Map;
        if (map == null)
            return;

        float radius = Props.infectionRadius;

        var candidates = map.mapPawns.FreeColonistsAndPrisonersSpawned
                        .Where(p =>
                            p != source
                            && !p.Dead
                            && (p.ParentHolder == null
                                || !(p.ParentHolder is Building_CryptosleepCasket
                                    || p.ParentHolder is Building_Sarcophagus
                                    || p.ParentHolder is Building_CloningPod
                                    || p.ParentHolder is CompBiosculpterPod))
                            && p.HasSymbiote()
                            && !p.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePlague)
                            && p.Position.InHorDistOf(source.Position, radius))
                        .InRandomOrder()
                        .ToList();

        if (candidates.Count == 0)
            return;

        // Infect between 0 and up to 2 (or up to candidates.Count if it’s smaller)
        int maxToInfect = Mathf.Min(2, candidates.Count);
        int amount = Rand.RangeInclusive(0, maxToInfect);

        for (int i = 0; i < amount; i++)
        {
            var candidate = candidates[i];

            float chance = 1f;
            bool hasImmunity = false;
            var immunityRecord = candidate.health.immunity.GetImmunityRecord(RimgateDefOf.Rimgate_SymbiotePlague);
            if (immunityRecord != null)
            {
                hasImmunity = true;
                chance = Mathf.Lerp(1f, 0f, immunityRecord.immunity / 0.6f);
            }

            if (Rand.Chance(chance))
            {
                candidate.health.AddHediff(RimgateDefOf.Rimgate_SymbiotePlague);
                TaleRecorder.RecordTale(
                    TaleDefOf.IllnessRevealed,
                    candidate,
                    RimgateDefOf.Rimgate_SymbiotePlague);
            }
        }
    }
}

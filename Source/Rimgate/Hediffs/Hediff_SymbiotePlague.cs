using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Hediff_SymbiotePlague : HediffWithComps
{
    private const float SeverityPostSymbiote = 0.633f / GenDate.TicksPerHour;

    public override void PostTick()
    {
        // Standard safety
        if (pawn == null || pawn.Dead || !pawn.Spawned)
            return;

        if (!pawn.HasSymbiote())
        {
            // once the symbiote dies, grant immuninty and let the pawn recover
            var record = pawn.health.immunity.GetImmunityRecord(def);
            if (record != null && record.immunity < 1)
                record.immunity = 1;
        }

        // Even if pawn doesn't have a symbiote, they can still spread the disease
        base.PostTick();
    }
}

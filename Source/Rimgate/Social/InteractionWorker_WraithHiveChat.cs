using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class InteractionWorker_WraithHiveChat : InteractionWorker
{
    // lower for rarer, higher for more common
    const float BaseWeight = 2f;

    // Fires occasionally, more often when pawns are close.
    public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
    {
        if (initiator?.Map == null 
            || recipient?.Map == null 
            || initiator.Map != recipient.Map) return 0f;

        if (!ModsConfig.BiotechActive 
            || !initiator.HasHiveConnection() 
            || !recipient.HasHiveConnection()) return 0f;

        if (initiator.Downed || recipient.Downed) return 0f;
        if (initiator.HostileTo(recipient)) return 0f;
        if (initiator.Drafted || recipient.Drafted) return 0f;

        // Bias toward nearby pairs: closer -> higher weight
        float dist = (initiator.Position - recipient.Position).LengthHorizontal;
        // 0 at 15+ cells, ~1 at 2 cells
        float proximity = Mathf.InverseLerp(35f, 2f, dist);

        // Slight mood bias: happier Wraith hum more
        float moodBias = 1f;
        if (initiator.needs?.mood != null)
            moodBias *= Mathf.Lerp(0.75f, 1.25f, initiator.needs.mood.CurLevel);

        return BaseWeight * proximity * moodBias;
    }
}
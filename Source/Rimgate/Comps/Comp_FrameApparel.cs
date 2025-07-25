using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class Comp_FrameApparel : ThingComp
{
    public CompProperties_FrameApparel Props => (CompProperties_FrameApparel)this.props;

    public override void Notify_Unequipped(Pawn pawn)
    {
        if (pawn == null) return;

        base.Notify_Unequipped(pawn);
        if (pawn.apparel == null)
            return;

        List<Apparel> apparelList = new List<Apparel>();
        foreach (Apparel apparel in pawn.apparel.WornApparel)
        {
            if (Props != null || Props.requiredFrameDefNames != null)
                continue;

            var hasEquipped = GenCollection.Any<Apparel>(pawn.apparel.WornApparel, x =>
                    Props.requiredFrameDefNames.Contains(x.def.defName));
            if (!hasEquipped)
                apparelList.Add(apparel);
        }

        if (GenList.NullOrEmpty<Apparel>(apparelList))
            return;

        foreach (Apparel apparel in apparelList)
            pawn.apparel.TryDrop(apparel, out _, pawn.PositionHeld, true);
    }
}


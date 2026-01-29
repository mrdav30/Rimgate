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
    public CompProperties_FrameApparel Props => (CompProperties_FrameApparel)props;

    public bool HasFrameRequirement => !Props.isFrameRoot && Props.frameRootDefs != null && Props.frameRootDefs.Count > 0;

    public Apparel FrameApparel => parent as Apparel;

    private bool forceRemove = false;

    // When frame component apparel is equipped, check if the wearer has the required frame root apparel.
    public override void Notify_Equipped(Pawn pawn)
    {
        if (pawn == null || !pawn.Spawned || Props.isFrameRoot) return;

        if (!ShouldUnequipFrameComponent(pawn)) return;

        forceRemove = true;

        pawn.apparel.TryDrop(FrameApparel, out _, pawn.PositionHeld, true);

        if (pawn.IsFreeColonist)
            Messages.Message(
                $"{"CannotEquipApparel".Translate(FrameApparel.LabelCap, FrameApparel)} : {"RG_FrameApparel_CannotWear_NoFrame".Translate()}",
                pawn,
                MessageTypeDefOf.NegativeEvent);
    }

    // When frame root removed, check if any worn frame components need to be unequipped.
    public override void Notify_Unequipped(Pawn pawn)
    {
        if (pawn == null || !pawn.Spawned || !Props.isFrameRoot) return;

        var wornItems = pawn.apparel?.WornApparel;
        if (wornItems == null || wornItems.Count == 0) return;

        // Collect items to remove first to avoid modifying collection during iteration
        var toRemove = new List<Apparel>();
        for (int i = 0; i < wornItems.Count; i++)
        {
            var item = wornItems[i];
            var comp = item?.GetComp<Comp_FrameApparel>();
            if (comp == null) continue;
            if (!comp.ShouldUnequipFrameComponent(pawn)) continue;
            toRemove.Add(item);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            var remove = toRemove[i];
            pawn.apparel.TryDrop(remove, out _, pawn.PositionHeld, true);

            if (pawn.IsFreeColonist)
                Messages.Message(
                    $"{"CannotEquipApparel".Translate(remove.LabelCap, remove)} : {"RG_FrameApparel_CannotWear_NoFrame".Translate()}",
                    pawn,
                    MessageTypeDefOf.NegativeEvent);
        }
    }

    public bool ShouldUnequipFrameComponent(Pawn pawn)
    {
        if (!HasFrameRequirement) return false;

        var worn = pawn.apparel?.WornApparel;
        if (worn == null || worn.Count == 0) return true;

        var required = Props.frameRootDefs;

        var hasFrameEquipped = false;
        for (int i = 0; i < required.Count; i++)
        {
            if (worn.Any(apparel => apparel.def == required[i]))
                hasFrameEquipped = true;
        }

        return !hasFrameEquipped;
    }
}


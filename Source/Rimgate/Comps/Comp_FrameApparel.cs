using RimWorld;
using RimWorld.Planet;
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

    public Apparel Apparel => (Apparel)parent;

    public bool IsFrameRoot => Props.isFrameRoot;

    public bool IsFrameComponent => !Props.isFrameRoot && Props.frameRootDefs != null && Props.frameRootDefs.Count > 0;

    // If frame component apparel is equipped, check if the wearer has the required frame root.
    // If not, force unequip this frame component.
    public override void Notify_Equipped(Pawn pawn)
    {
        if (pawn == null) return;
        if (!IsFrameComponent) return;
        if (HasAnyRequiredRootEquipped(pawn))
            return;

        ForceRemoveFromPawn(pawn, Apparel);

        if (pawn.IsFreeColonist)
            Messages.Message(
                $"{"CannotEquipApparel".Translate(Apparel.LabelCap, Apparel)} : {"RG_FrameApparel_CannotWear_NoFrame".Translate()}",
                pawn,
                MessageTypeDefOf.NegativeEvent);
    }

    // If a frame root is unequipped, check if this frame component required it.
    // If so, force unequip this frame component.
    public override void Notify_Unequipped(Pawn pawn)
    {
        if (pawn == null) return;
        if (!IsFrameRoot) return;

        // Root was removed: remove any dependent components still worn.
        var worn = pawn.apparel?.WornApparel;
        if (worn == null || worn.Count == 0) return;

        // Iterate backwards because we'll remove items.
        for (int i = worn.Count - 1; i >= 0; i--)
        {
            Apparel a = worn[i];
            if (a == null) continue;

            var comp = a.TryGetComp<Comp_FrameApparel>();
            if (comp == null || !comp.IsFrameComponent) continue;

            // If this component lists *this* root def as a requirement, it must come off.
            var req = comp.Props.frameRootDefs;
            if (req == null) continue;

            bool requiresThisRoot = false;
            for (int r = 0; r < req.Count; r++)
            {
                if (req[r] == this.parent.def)
                {
                    requiresThisRoot = true;
                    break;
                }
            }

            if (!requiresThisRoot) continue;

            ForceRemoveFromPawn(pawn, a);

            if (pawn.IsFreeColonist)
                Messages.Message(
                    $"{"CannotEquipApparel".Translate(a.LabelCap, a)} : {"RG_FrameApparel_CannotWear_NoFrame".Translate()}",
                    pawn,
                    MessageTypeDefOf.NegativeEvent);
        }
    }

    public bool HasAnyRequiredRootEquipped(Pawn pawn)
    {
        var worn = pawn.apparel?.WornApparel;
        if (worn == null || worn.Count == 0) return false;

        var req = Props.frameRootDefs;
        if (req == null || req.Count == 0) return true; // no requirements

        // O(W*R) but tiny lists; still early-out.
        for (int i = 0; i < worn.Count; i++)
        {
            var def = worn[i]?.def;
            if (def == null) continue;

            for (int r = 0; r < req.Count; r++)
            {
                if (def == req[r])
                    return true;
            }
        }

        return false;
    }

    private static void ForceRemoveFromPawn(Pawn pawn, Apparel apparel)
    {
        if (pawn == null || apparel == null) return;

        // Ensure it's actually removed from worn first.
        pawn.apparel?.Remove(apparel);

        // 1) Prefer inventory
        if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(apparel))
            return;

        // 2) No map (caravan/world): destroy to avoid null refs / dupes
        if (pawn.Map == null)
        {
            apparel.Destroy();
            return;
        }

        // 3) Drop near using engine placement to avoid stacking visuals
        GenDrop.TryDropSpawn(apparel, pawn.Position, pawn.Map, ThingPlaceMode.Near, out _);
    }
}


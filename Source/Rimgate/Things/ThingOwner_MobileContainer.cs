using UnityEngine;
using Verse;

namespace Rimgate;

public sealed class ThingOwner_MobileContainer : ThingOwner<Thing>
{
    private readonly Building_MobileContainer _container;

    public ThingOwner_MobileContainer(Building_MobileContainer container) : base(container)
    {
        _container = container;
    }

    public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
    {
        if (item == null)
        {
            Log.Warning("Tried to add null item to ThingOwner.");
            return false;
        }

        if (Contains(item))
        {
            Log.Warning($"Tried to add {item.ToStringSafe()} but it's already in this ThingOwner.");
            return false;
        }

        if (item.holdingOwner != null)
        {
            Log.Warning($"Tried to add {item.ToStringSafe()} but it's already in another container (current owner={item.holdingOwner.Owner.ToStringSafe()}). Use TryTransfer APIs.");
            return false;
        }

        if (!CanAcceptAnyOf(item, canMergeWithExistingStacks))
            return false;

        // Explicitly handle MERGE so we can notify the comp about the merged delta.
        if (canMergeWithExistingStacks)
        {
            var list = InnerListForReading; // safe read/write access to the underlying list
            for (int i = 0; i < list.Count; i++)
            {
                var stack = list[i];
                if (!stack.CanStackWith(item)) continue;

                int toMerge = Mathf.Min(item.stackCount, stack.def.stackLimit - stack.stackCount);
                if (toMerge > 0)
                {
                    var split = item.SplitOff(toMerge);
                    int before = stack.stackCount;
                    stack.TryAbsorbStack(split, respectStackLimit: true);
                    int merged = stack.stackCount - before;
                    if (merged > 0)
                    {
                        // Our comp-specific MERGE notification
                        // Note: this is what vanilla only does for CompTransporter
                        _container?.Notify_ThingAddedAndMergedWith(stack, merged);
                    }

                    if (item.Destroyed || item.stackCount == 0)
                        return true; // fully merged
                }
            }
        }

        // For a NEW STACK (or if nothing left to merge), defer to base.
        // This ensures owner/holdingOwner bookkeeping + capacity rules + NotifyAdded all happen correctly.
        return base.TryAdd(item, canMergeWithExistingStacks: false);
    }
}

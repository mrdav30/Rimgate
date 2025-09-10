using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Thing_MobileCartProxy : ThingWithComps, IThingHolder
{
    public IThingHolder ParentHolder => holdingOwner?.Owner;

    public override Graphic Graphic => RimgateTex.EmptyGraphic;

    public ThingOwner GetDirectlyHeldThings() => this.TryGetComp<Comp_MobileContainer>()?.InnerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
        => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
}


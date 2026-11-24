using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class ITab_MobileContainer : ITab_ContentsBase
{
    public Building_MobileContainer Parent => SelThing as Building_MobileContainer;

    public override IList<Thing> container => Parent.InnerContainer;

    public override bool UseDiscardMessage => false;

    public override bool IsVisible
    {
        get
        {
            var parent = Parent;
            if (parent != null && parent.Faction.IsOfPlayerFaction())
            {
                if (!parent.LoadingInProgress)
                    return parent.InnerContainer.Any;

                return true;
            }

            return false;
        }
    }

    public override IntVec3 DropOffset => DropOffset;

    public ITab_MobileContainer()
    {
        labelKey = "TabTransporterContents";
        containedItemsKey = "ContainedItems";
    }

    protected override void DoItemsLists(Rect inRect, ref float curY)
    {
        Building_MobileContainer parent = Parent;
        Rect rect = new(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
        Text.Font = GameFont.Small;
        bool flag = false;
        float curY2 = 0f;
        Widgets.BeginGroup(rect);
        Widgets.ListSeparator(ref curY2, rect.width, "ItemsToLoad".Translate());
        if (parent.LeftToLoad != null)
        {
            for (int i = 0; i < parent.LeftToLoad.Count; i++)
            {
                TransferableOneWay t = parent.LeftToLoad[i];
                if (t.CountToTransfer > 0 && t.HasAnyThing)
                {
                    flag = true;
                    DoThingRow(
                        t.ThingDef,
                        t.CountToTransfer,
                        t.things,
                        rect.width,
                        ref curY2,
                        delegate (int x)
                        {
                            OnDropToLoadThing(t, x);
                        });
                }
            }
        }

        if (!flag)
            Widgets.NoneLabel(ref curY2, rect.width);

        Widgets.EndGroup();
        Rect inRect2 = new((inRect.width + 10f) / 2f, curY, (inRect.width - 10f) / 2f, inRect.height);
        float curY3 = 0f;
        DoItemsLists(inRect2, ref curY3);
        curY += Mathf.Max(curY2, curY3);
    }

    protected override void OnDropThing(Thing t, int count)
    {
        OnDropThing(t, count);
        Parent.Notify_ItemRemoved(t);
    }

    private void OnDropToLoadThing(TransferableOneWay t, int count)
    {
        int newCount = t.CountToTransfer - count;
        if (newCount > 0)
            t.ForceTo(newCount);
        else
            Parent.RemoveFromLoadList(t);
        EndJobForEveryoneHauling(t);
    }

    private void EndJobForEveryoneHauling(TransferableOneWay t)
    {
        IReadOnlyList<Pawn> allPawnsSpawned = SelThing.Map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < allPawnsSpawned.Count; i++)
        {
            Pawn pawn = allPawnsSpawned[i];
            if (pawn.CurJobDef == JobDefOf.HaulToTransporter)
            {
                bool isValid = pawn.jobs.curDriver is JobDriver_HaulToMobileContainer jobDriver
                    && jobDriver.MobileContainer.ThingID == Parent.ThingID
                    && jobDriver.ThingToCarry != null
                    && jobDriver.ThingToCarry.def == t.ThingDef;
                if (isValid)
                    allPawnsSpawned[i].jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
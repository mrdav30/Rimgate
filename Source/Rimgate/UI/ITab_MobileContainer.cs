using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Rimgate;

public class ITab_MobileContainer : ITab_ContentsBase
{
    public override IList<Thing> container => Mobile.InnerContainer;

    public override bool UseDiscardMessage => false;

    public Comp_MobileContainerControl Mobile => base.SelThing.TryGetComp<Comp_MobileContainerControl>();

    public override bool IsVisible
    {
        get
        {
            if ((base.SelThing.Faction == null
                || base.SelThing.Faction.IsOfPlayerFaction())
                && Mobile != null)
            {
                if (!Mobile.LoadingInProgress)
                    return Mobile.InnerContainer.Any;

                return true;
            }

            return false;
        }
    }

    public override IntVec3 DropOffset => base.DropOffset;

    public ITab_MobileContainer()
    {
        labelKey = "TabTransporterContents";
        containedItemsKey = "ContainedItems";
    }

    protected override void DoItemsLists(Rect inRect, ref float curY)
    {
        Comp_MobileContainerControl container = Mobile;
        Rect rect = new Rect(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
        Text.Font = GameFont.Small;
        bool flag = false;
        float curY2 = 0f;
        Widgets.BeginGroup(rect);
        Widgets.ListSeparator(ref curY2, rect.width, "ItemsToLoad".Translate());
        if (container.LeftToLoad != null)
        {
            for (int i = 0; i < container.LeftToLoad.Count; i++)
            {
                TransferableOneWay t = container.LeftToLoad[i];
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
        Rect inRect2 = new Rect((inRect.width + 10f) / 2f, curY, (inRect.width - 10f) / 2f, inRect.height);
        float curY3 = 0f;
        base.DoItemsLists(inRect2, ref curY3);
        curY += Mathf.Max(curY2, curY3);
    }

    protected override void OnDropThing(Thing t, int count)
    {
        base.OnDropThing(t, count);
        Mobile.Notify_ThingRemoved();
    }

    private void OnDropToLoadThing(TransferableOneWay t, int count)
    {
        int newCount = t.CountToTransfer - count;
        if (newCount > 0)
            t.ForceTo(newCount);
        else
            Mobile.RemoveFromLoadList(t);
        EndJobForEveryoneHauling(t);
    }

    private void EndJobForEveryoneHauling(TransferableOneWay t)
    {
        IReadOnlyList<Pawn> allPawnsSpawned = base.SelThing.Map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < allPawnsSpawned.Count; i++)
        {
            Pawn pawn = allPawnsSpawned[i];
            if (pawn.CurJobDef == JobDefOf.HaulToTransporter)
            {
                bool isValid = pawn.jobs.curDriver is JobDriver_HaulToMobileContainer jobDriver
                    && jobDriver.Mobile.parent.ThingID == Mobile.parent.ThingID
                    && jobDriver.ThingToCarry != null
                    && jobDriver.ThingToCarry.def == t.ThingDef;
                if (isValid)
                    allPawnsSpawned[i].jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
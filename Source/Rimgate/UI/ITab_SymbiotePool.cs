using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

public class ITab_SymbiotePool : ITab_ContentsBase
{
    private static readonly CachedTexture DropTex = new CachedTexture("UI/Buttons/Drop");

    public override IList<Thing> container => Pool.HeldItems.ToList();

    public override bool IsVisible
    {
        get
        {
            if (base.SelThing != null)
            {
                return base.IsVisible;
            }

            return false;
        }
    }

    public Building_SymbioteSpawningPool Pool => base.SelThing as Building_SymbioteSpawningPool;

    public override bool VisibleInBlueprintMode => false;

    public ITab_SymbiotePool()
    {
        labelKey = "TabCasketContents";
        containedItemsKey = "TabCasketContents";
    }

    protected override void DoItemsLists(Rect inRect, ref float curY)
    {
        ListContainedSymbiotes(inRect, container, ref curY);
    }

    private void ListContainedSymbiotes(Rect inRect, IList<Thing> symbiote, ref float curY)
    {
        GUI.BeginGroup(inRect);
        Widgets.ListSeparator(ref curY, inRect.width, containedItemsKey.Translate());
        bool flag = false;
        for (int i = 0; i < symbiote.Count; i++)
        {
            Thing thing = symbiote[i];
            if (thing != null)
            {
                flag = true;
                DoRow(thing, inRect.width, i, ref curY);
            }
        }

        if (!flag)
            Widgets.NoneLabel(ref curY, inRect.width);

        GUI.EndGroup();
    }

    private void DoRow(Thing thing, float width, int i, ref float curY)
    {
        Rect rect = new Rect(0f, curY, width, 28f);
        if (Mouse.IsOver(rect))
            Widgets.DrawHighlightSelected(rect);
        else if (i % 2 == 1)
            Widgets.DrawLightHighlight(rect);

        Rect rect2 = new Rect(rect.width - 24f, curY, 24f, 24f);
        if (Widgets.ButtonImage(rect2, DropTex.Texture))
        {
            if (!Pool.OccupiedRect().AdjacentCells.Where((IntVec3 x) => x.Walkable(Pool.Map)).TryRandomElement(out var result))
            {
                result = Pool.Position;
            }

            Pool.TryDrop(thing, result, ThingPlaceMode.Near, 1, out var dropped);
            if (dropped.TryGetComp(out CompForbiddable comp))
                comp.Forbidden = true;
        }
        else if (Widgets.ButtonInvisible(rect))
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(thing);
        }

        TooltipHandler.TipRegionByKey(rect2, "RG_EjectSymbioteTooltip");
        Widgets.ThingIcon(new Rect(24f, curY, 28f, 28f), thing);
        Rect rect3 = new Rect(60f, curY, rect.width - 36f, rect.height);
        rect3.xMax = rect2.xMin;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rect3, thing.LabelCap.Truncate(rect3.width));
        Text.Anchor = TextAnchor.UpperLeft;
        if (Mouse.IsOver(rect))
        {
            TargetHighlighter.Highlight(thing, arrow: true, colonistBar: false);
            TooltipHandler.TipRegion(rect, thing.DescriptionDetailed);
        }

        curY += 28f;
    }
}
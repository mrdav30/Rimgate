using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

public class ITab_SymbiotePool : ITab_ContentsBase
{
    private static readonly CachedTexture DropTex = new("UI/Buttons/Drop");

    public override IList<Thing> container => [.. Pool.HeldItems];

    public override bool IsVisible
    {
        get
        {
            if (SelThing != null)
                return base.IsVisible;

            return false;
        }
    }

    public Building_SymbioteSpawningPool Pool => SelThing as Building_SymbioteSpawningPool;

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
        Widgets.ListSeparator(ref curY, inRect.width, HeaderWithCounts());

        bool any = false;
        for (int i = 0; i < symbiote.Count; i++)
        {
            Thing thing = symbiote[i];
            if (thing != null)
            {
                any = true;
                DoRow(thing, inRect.width, i, ref curY);
            }
        }

        if (!any)
            Widgets.NoneLabel(ref curY, inRect.width);

        GUI.EndGroup();
    }

    private string HeaderWithCounts()
    {
        var baseLabel = containedItemsKey.Translate();
        int cur = Pool?.HeldItems?.Count ?? 0;
        int max = Pool?.MaxHeldItems ?? 0;
        // If max is not defined for some reason, just show current.
        return max > 0
            ? $"{baseLabel} ({cur} / {max})"
            : $"{baseLabel} ({cur})";
    }

    private void DoRow(Thing thing, float width, int i, ref float curY)
    {
        Rect rect = new(0f, curY, width, 28f);
        if (Mouse.IsOver(rect))
            Widgets.DrawHighlightSelected(rect);
        else if (i % 2 == 1)
            Widgets.DrawLightHighlight(rect);

        Rect dropRect = new(rect.width - 24f, curY, 24f, 24f);
        Rect infoRect = new(dropRect.xMin - 24f, curY, 24f, 24f);

        Widgets.InfoCardButton(infoRect.x, infoRect.y, thing);

        if (Widgets.ButtonImage(dropRect, DropTex.Texture))
        {
            if (!Pool.OccupiedRect().AdjacentCells.Where(x => x.Walkable(Pool.Map)).TryRandomElement(out var result))
            {
                result = Pool.Position;
            }

            Pool.TryDrop(thing, result, ThingPlaceMode.Near, 1, out var dropped);
            if (dropped.TryGetComp(out CompForbiddable comp))
                comp.Forbidden = true;
        }
        else
        {
            Rect selectRect = rect;
            selectRect.xMax = infoRect.xMin;
            if (Widgets.ButtonInvisible(selectRect))
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(thing);
            }
        }

        TooltipHandler.TipRegionByKey(dropRect, "RG_EjectSymbioteTooltip");
        Widgets.ThingIcon(new Rect(24f, curY, 28f, 28f), thing);
        Rect rect3 = new(60f, curY, rect.width - 36f, rect.height)
        {
            xMax = infoRect.xMin
        };
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

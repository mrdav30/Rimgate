using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class GeneGizmo_ResourceEssence(Gene_Resource gene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barHighlightColor)
    : GeneGizmo_Resource(gene, drainGenes, barColor, barHighlightColor)
{
    private static bool _draggingBar;
    private const float ToggleSize = 24f;
    private const float ToggleSpacing = 2f;

    protected override bool DraggingBar { get => _draggingBar; set => _draggingBar = value; }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
        float num = Mathf.Repeat(Time.time, 0.85f);
        float num2 = 1f;
        if (num < 0.1f)
            num2 = num / 0.1f;
        else if (num >= 0.25f)
            num2 = 1f - (num - 0.25f) / 0.6f;

        _ = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
        if (MapGizmoUtility.LastMouseOverGizmo is Command_Ability command_Ability && gene.Max != 0f)
        {
            foreach (CompAbilityEffect effectComp in command_Ability.Ability.EffectComps)
            {
                if (effectComp is not CompAbilityEffect_EssenceCost cost || cost.Props.essenceCost < float.Epsilon)
                    continue;

                Rect rect = barRect.ContractedBy(3f);
                float width = rect.width;
                float num3 = gene.Value / gene.Max;
                rect.xMax = rect.xMin + width * num3;
                float num4 = Mathf.Min(cost.Props.essenceCost / gene.Max, 1f);
                rect.xMin = Mathf.Max(rect.xMin, rect.xMax - width * num4);
                GUI.color = new Color(1f, 1f, 1f, num2 * 0.7f);
                GenUI.DrawTextureWithMaterial(rect, RimgateTex.EssenceCostTex, null);
                GUI.color = Color.white;
                break;
            }
        }

        return result;
    }

    protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
    {
        if (IsDraggable)
            OnDrawHeader(headerRect, ref mouseOverElement);

        base.DrawHeader(headerRect, ref mouseOverElement);
    }

    private void OnDrawHeader(Rect headerRect, ref bool mouseOverElement)
    {
        if (gene is not Gene_WraithEssenceMetabolism essenceGene)
            return;

        float minValue = essenceGene.PostProcessValue(essenceGene.targetValue);

        headerRect.xMax -= ToggleSize;
        Rect podRect = new(headerRect.xMax, headerRect.y, ToggleSize, ToggleSize);

        headerRect.xMax -= ToggleSize + ToggleSpacing;
        Rect prisonerRect = new(headerRect.xMax, headerRect.y, ToggleSize, ToggleSize);

        if (DrawAutoUseToggle(prisonerRect, XenotypeDefOf.Baseliner, ref essenceGene.PrisonersAllowed, "RG_AutoUsePrisonersDesc", minValue, 282973714)
            || DrawAutoUseToggle(podRect, RimgateDefOf.Rimgate_WraithCocoonPod, ref essenceGene.FilledPodsAllowed, "RG_AutoUsePodsDesc", minValue, 282973713))
            mouseOverElement = true;
    }

    private bool DrawAutoUseToggle(Rect rect, Def iconDef, ref bool autoUseEnabled, string tooltipKey, float minValue, int tooltipId)
    {
        Widgets.DefIcon(rect, iconDef);
        GUI.DrawTexture(new Rect(rect.center.x, rect.y, rect.width / 2f, rect.height / 2f),
            autoUseEnabled
                ? Widgets.CheckboxOnTex
                : Widgets.CheckboxOffTex);

        if (Widgets.ButtonInvisible(rect))
        {
            autoUseEnabled = !autoUseEnabled;
            if (autoUseEnabled)
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            else
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }

        if (!Mouse.IsOver(rect))
            return false;

        Widgets.DrawHighlight(rect);
        string onOff = (autoUseEnabled
            ? "On"
            : "Off").Translate().ToString().UncapitalizeFirst();
        TooltipHandler.TipRegion(rect,
            () => tooltipKey.Translate(gene.pawn.Named("PAWN"), minValue.Named("MIN"), onOff.Named("ONOFF")).Resolve(),
            tooltipId);

        return true;
    }

    protected override string GetTooltip()
    {
        var sb = new StringBuilder();

        sb.Append($"{gene.ResourceLabel.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor)}: {gene.ValueForDisplay} / {gene.MaxForDisplay}");

        if (!gene.def.resourceDescription.NullOrEmpty())
        {
            if (sb.Length > 0) sb.AppendLine();

            sb.Append(gene.def.resourceDescription.Formatted(gene.pawn.Named("PAWN")).Resolve());
        }

        return sb.ToString();
    }
}

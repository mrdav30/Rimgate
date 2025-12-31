using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class GeneGizmo_ResourceEssence : GeneGizmo_Resource
{
    private static bool _draggingBar;

    protected override bool DraggingBar { get => _draggingBar; set => _draggingBar = value; }

    public GeneGizmo_ResourceEssence(Gene_Resource gene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barHighlightColor)
        : base(gene, drainGenes, barColor, barHighlightColor) { }

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
        Gene_WraithEssenceMetabolism essenceGene = gene as Gene_WraithEssenceMetabolism;
        if (essenceGene == null) return;

        headerRect.xMax -= 24f;
        Rect rect = new Rect(headerRect.xMax, headerRect.y, 24f, 24f);
        Widgets.DefIcon(rect, RimgateDefOf.Rimgate_WraithCocoonPod);
        GUI.DrawTexture(new Rect(rect.center.x, rect.y, rect.width / 2f, rect.height / 2f),
            essenceGene.FilledPodsAllowed
                ? Widgets.CheckboxOnTex
                : Widgets.CheckboxOffTex);
        if (Widgets.ButtonInvisible(rect))
        {
            essenceGene.FilledPodsAllowed = !essenceGene.FilledPodsAllowed;
            if (essenceGene.FilledPodsAllowed)
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            else
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }

        if (!Mouse.IsOver(rect)) return;

        Widgets.DrawHighlight(rect);
        string onOff = (essenceGene.FilledPodsAllowed
            ? "On"
            : "Off").Translate().ToString().UncapitalizeFirst();
        TooltipHandler.TipRegion(rect,
            () => "RG_AutoUsePodsDesc".Translate(gene.pawn.Named("PAWN"),
            essenceGene.PostProcessValue(essenceGene.targetValue).Named("MIN"),
            onOff.Named("ONOFF")).Resolve(),
            282973713);
        mouseOverElement = true;
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
using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimgate;

public class GeneGizmo_ResourceEssence : GeneGizmo_Resource
{
    public GeneGizmo_ResourceEssence(Gene_Resource gene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barHighlightColor)
        : base(gene, drainGenes, barColor, barHighlightColor) { }

    protected override bool IsDraggable => false;

    protected override bool DraggingBar { get; set; } = false;

    protected override string GetTooltip() {

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
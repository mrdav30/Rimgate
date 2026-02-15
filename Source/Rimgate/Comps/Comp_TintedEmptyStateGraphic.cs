using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

/// <summary>
/// Shows a tinted overlay (respecting stuff/paint colors) 
/// when the parent is "empty".
/// </summary>
public class Comp_TintedEmptyStateGraphic : ThingComp
{
    private Graphic _tinted;                  

    private Color _lastPrimary = Color.white; 

    private Color _lastSecondary = Color.white;

    public CompProperties_TintedEmptyStateGraphic Props => (CompProperties_TintedEmptyStateGraphic)props;

    private bool ParentIsEmpty
    {
        get
        {
            if (parent is IThingHolder th && th.GetDirectlyHeldThings().NullOrEmpty()) 
                return true;

            var wake = parent.TryGetComp<CompPawnSpawnOnWakeup>();
            if (wake != null && !wake.CanSpawn) 
                return true;

            return false;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        RebuildTintedIfNeeded(force: true);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        // Rebuild lazily next draw if colors changed while despawned.
        _tinted = null;
    }

    public override bool DontDrawParent()
    {
        if (ParentIsEmpty) return !Props.alwaysDrawParent;
        return false;
    }

    public override void PostDraw()
    {
        if (!ParentIsEmpty) return;
        if (parent.def.drawerType == DrawerType.MapMeshOnly) return;

        var g = RebuildTintedIfNeeded();
        if (g == null) return;

        var drawPos = parent.DrawPos;
        if (!Props.useParentAltitude)
            drawPos.y = parent.def.altitudeLayer.AltitudeFor();

        // Respect per-rotation drawOffset
        Vector3 offset = Props.graphicData?.drawOffset ?? Vector3.zero;
        g.Draw(drawPos + offset.RotatedBy(parent.Rotation), parent.Rotation, parent);
    }

    public override void PostPrintOnto(SectionLayer layer)
    {
        if (!ParentIsEmpty) return;

        var g = RebuildTintedIfNeeded();
        if (g == null) return;

        // Print uses MapMesh (baked mesh);
        // Graphic handles stuff tint via our colored instance.
        g.Print(layer, parent, 0f);
    }

    /// <summary>
    /// Builds (or rebuilds) a tinted overlay graphic 
    /// that honors the parent’s stuff/paint colors.
    /// Refreshes if colors changed since last draw.
    /// </summary>
    private Graphic RebuildTintedIfNeeded(bool force = false)
    {
        if (Props.graphicData == null) return null;

        // stuff/paint primary
        var primary = parent.DrawColor;
        // dual-tint shaders (CutoutComplex, etc.)
        var secondary = parent.DrawColorTwo; 

        if (!force && _tinted != null && primary == _lastPrimary && secondary == _lastSecondary)
            return _tinted;

        // Base graphic from XML (resolves shader, data, etc.)
        var baseGraphic = Props.graphicData.Graphic;
        if (baseGraphic == null) return null;

        // Build a properly tinted variant
        // (handles both single & dual-tint shaders)
        _tinted = baseGraphic.GetColoredVersion(baseGraphic.Shader, primary, secondary);

        _lastPrimary = primary;
        _lastSecondary = secondary;
        return _tinted;
    }
}

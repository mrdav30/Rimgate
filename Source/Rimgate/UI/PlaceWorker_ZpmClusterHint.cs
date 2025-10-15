using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class PlaceWorker_ZpmClusterHint : PlaceWorker
{
    private static IntVec3? _lastReinstallOrigin;

    private static Map _lastReinstallMap;

    private static int _lastReinstallStamp;

    private readonly static Color _textColor = new Color(0.65f, 1f, 0.65f, 1f);

    public override void DrawGhost(
        ThingDef def,
        IntVec3 center,
        Rot4 rot,
        Color ghostCol,
        Thing thing = null)
    {
        if (Find.CurrentMap == null) return;

        // track origin only if this is a reinstall of a ZPM
        if (thing != null 
            && thing.def == RimgateDefOf.Rimgate_ZPM)
        {
            _lastReinstallOrigin = thing.Position;
            _lastReinstallMap = thing.Map;
            _lastReinstallStamp = Find.TickManager.TicksGame;
        }

        // Show the adjacency ring so players know how tight to pack
        GenDraw.DrawRadiusRing(center, Building_ZPM.ClusterScanRadius);
    }

    public override void DrawMouseAttachments(BuildableDef def)
    {
        var map = Find.CurrentMap;
        if (map == null) return;

        IntVec3 cell = UI.MouseCell();

        // Count *existing* ZPMs in radius (no self at center)
        int existing = Building_ZPM.CountNearbyZpms(cell, map);

        // If this is a reinstall preview and the origin ZPM is within the radius,
        // subtract it because it won’t exist there after the move.
        if (_lastReinstallOrigin.HasValue
            && _lastReinstallMap == map
            && Find.TickManager.TicksGame == _lastReinstallStamp
            && _lastReinstallOrigin.Value.InHorDistOf(cell, Building_ZPM.ClusterScanRadius))
        {
            existing = Mathf.Max(0, existing - 1);
        }

        // Prospective total = existing neighbors + the ZPM we’re placing
        int prospectiveTotal = existing + 1;
        if (prospectiveTotal < Building_ZPM.ClusterThresholdTotal)
            return;

        int extras = prospectiveTotal - Building_ZPM.ClusterThresholdTotal;
        int bonusPct = Mathf.RoundToInt(
            (Building_ZPM.ClusterBonus + extras 
            * Building_ZPM.PerExtraZpmBonus) * 100f);

        Widgets.MouseAttachedLabel(
            "RG_ZpmSynergyBonus".Translate(bonusPct),
            8f,
            35f,
            _textColor);

        // Clear the cached origin after we’ve used it this frame
        _lastReinstallOrigin = null;
        _lastReinstallMap = null;
    }   
}

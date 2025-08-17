using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate.HarmonyPatches;

// Generate custom interaction cell locations for Sarcophaguss and VetPods
[HarmonyPatch(typeof(BedCellSearchPattern), nameof(BedCellSearchPattern.AddCellsToList))]
public static class Harmony_BedCellSearchPattern
{
    public static bool Prefix(
        List<IntVec3> orderedCells,
        Thing thing,
        CellRect rect,
        IntVec3 focus,
        Rot4 focusRotation)
    {
        if (thing is Building_Bed_Sarcophagus bedSarcophagus)
        {
            if (!rect.Contains(focus))
                throw new ArgumentException();

            if (bedSarcophagus.def.building.bed_humanlike)
                BedCellOffsetsSarcophagus(orderedCells);

            RotationDirection relativeRotation = Rot4.GetRelativeRotation(Rot4.South, focusRotation);
            for (int i = 0; i < orderedCells.Count; i++)
                orderedCells[i] = focus + orderedCells[i].RotatedBy(relativeRotation);

            return false;
        }
        return true;
    }

    // Sarcophagus only have interaction cells near the bed 
    public static void BedCellOffsetsSarcophagus(List<IntVec3> offsets)
    {
        offsets.Add(IntVec3.West);
        offsets.Add(IntVec3.East);
        offsets.Add(IntVec3.West + IntVec3.South);
        offsets.Add(IntVec3.East + IntVec3.South);
        offsets.Add(2 * IntVec3.South);
        offsets.Add(IntVec3.West + 2 * IntVec3.South);
        offsets.Add(IntVec3.East + 2 * IntVec3.South);
        offsets.Add(IntVec3.South);
        offsets.Add(IntVec3.Zero);
    }
}

using Rimgate;
using VEF.Utils;
using Verse;

namespace Rimgate;

public class JobDriver_CloneOccupantSoldier : JobDriver_CloneBase
{
    protected override CloneType CloneJob => CloneType.Enhanced;

    protected override void Clone()
    {
        CloneUtility.Clone(ClonePod, CloneJob);
    }
}

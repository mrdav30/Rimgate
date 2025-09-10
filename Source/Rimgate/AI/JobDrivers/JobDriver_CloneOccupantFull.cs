using Rimgate;
using VEF.Utils;
using Verse;

namespace Rimgate;

public class JobDriver_CloneOccupantFull : JobDriver_CloneBase
{
    protected override CloneType CloneJob => CloneType.Full;

    protected override void Clone()
    {
        CloneUtility.Clone(ClonePod, CloneJob);
    }
}

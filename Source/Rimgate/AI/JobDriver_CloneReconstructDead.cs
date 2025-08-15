using Rimgate;
using VEF.Utils;
using Verse;

namespace Rimgate;

public class JobDriver_CloneReconstructDead : JobDriver_CloneBase
{
    protected override CloneType CloneJob => CloneType.Reconstruct;

    protected override void Clone()
    {
        CloneUtility.Clone(ClonePod, CloneJob);
    }
}
using VEF.Utils;
using Verse;

namespace Rimgate;

public class JobDriver_CloneOccupantGenes : JobDriver_CloneBase
{
    protected override CloneType CloneJob => CloneType.Genome;

    protected override void Clone()
    {
        CloneUtility.Clone(ClonePod, CloneJob);
    }
}

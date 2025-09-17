using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_ZpmSignalScrambler : CompProperties
{
    public bool mapWide;

    public CompProperties_ZpmSignalScrambler() => compClass = typeof(Comp_ZpmSignalScrambler);
}

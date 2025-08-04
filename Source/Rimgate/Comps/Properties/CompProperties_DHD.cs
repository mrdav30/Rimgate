using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_DialHomeDevice : CompProperties
{
    public bool selfDialler = false;

    public bool requiresPower = false;

    public CompProperties_DialHomeDevice() => compClass = typeof(Comp_DialHomeDevice);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_DHDControl : CompProperties
{
    public bool selfDialler;

    public bool requiresPower;

    public bool canToggleIris;

    public GraphicData activeGraphicData;

    public CompProperties_DHDControl() => compClass = typeof(Comp_DHDControl);
}

using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_UseEffectInstallSymbiote : CompProperties_UseEffect
{
    public HediffDef hediffDef;
    public BodyPartDef bodyPart;

    public CompProperties_UseEffectInstallSymbiote() => compClass = typeof(CompUseEffect_InstallSymbiote);
}

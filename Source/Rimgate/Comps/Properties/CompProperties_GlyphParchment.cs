using Rimgate;
using System.Collections.Generic;
using UnityEngine;
using Verse;

public class CompProperties_GlyphParchment : CompProperties
{
    public bool canDecodePlanet;

    public bool canDecodeOrbit;

    public int useDuration = 500;

    public CompProperties_GlyphParchment() => compClass = typeof(Comp_GlyphParchment);
}
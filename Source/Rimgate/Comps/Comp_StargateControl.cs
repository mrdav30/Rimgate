using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_StargateControl : ThingComp
{
    public CompProperties_StargateControl Props => (CompProperties_StargateControl)props;

    public Graphic StargatePuddle => _stargatePuddle ??= Props.puddleGraphicData.Graphic;

    public Graphic StargateIris => _stargateIris ??= Props.irisGraphicData.Graphic;

    public Graphic ChevronHighlight => _chevronHighlight ??= Props.chevronHighlight.Graphic;

    public IEnumerable<IntVec3> VortexCells
    {
        get
        {
            var rot = parent.Rotation;
            if (rot == Rot4.North) // default is for north facing
            {
                foreach (IntVec3 offset in Props.vortexPattern)
                    yield return offset + parent.Position;
                yield break;
            }

            foreach (var off in Props.vortexPattern)
                yield return parent.Position + Utils.RotateOffset(off, rot);
        }
    }

    public Texture2D ToggleIrisIcon => _cachedIrisToggleIcon ??= ContentFinder<Texture2D>.Get(Props.irisGraphicData.texPath, true);

    private Graphic _stargatePuddle;

    private Graphic _stargateIris;

    private Graphic _chevronHighlight;

    private Texture2D _cachedIrisToggleIcon;

    public bool TryGetTeleportSound(out SoundDef def)
    {
        if(Props.teleportSounds == null || Props.teleportSounds.Count == 0)
        {
            def = null;
            return false;
        }

        def = Props.teleportSounds.RandomElement();
        return true;
    }
}

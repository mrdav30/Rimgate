using UnityEngine;
using Verse;

namespace Rimgate;

// Attaches to a Pawn and draws the cart graphic in front of them.
public class Thing_PushedCartVisual : AttachableThing
{
    public ThingDef ParentDef;

    public override Graphic Graphic => RimgateTex.EmptyGraphic;

    public override string InspectStringAddon => string.Empty;

    public float DrawPosY => Altitudes.AltitudeFor(def.altitudeLayer);

    public Graphic ColoredGraphic
    {
        get
        {
            if (_coloredGraphic != null)
                return _coloredGraphic;

            // fallback to parent cart's graphic if we lost the cached one for some reason
            if (ParentDef != null)
            {
                // cache the multi-rot graphic from the *cart* (not proxy)
                Graphic graphic = ParentDef.graphicData.Graphic;
                if (graphic != null)
                    _coloredGraphic = graphic.GetColoredVersion(graphic.Shader, _colA, _colB);
            }

            return _coloredGraphic ?? RimgateTex.EmptyGraphic;
        }
    }

    private Graphic _coloredGraphic;

    private float _frontOffset = 1.0f;  // offset ahead of pawn (from Comp props)

    private Color _colA = Color.white;

    private Color _colB = Color.white;

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Defs.Look(ref ParentDef, "ParentDef");
        Scribe_Values.Look(ref _frontOffset, "frontOffset", 1f);
        Scribe_Values.Look(ref _colA, "colA", Color.white);
        Scribe_Values.Look(ref _colB, "colB", Color.white);
    }

    public void Init(ThingDef def, Color drawA, Color drawB, float frontOffset)
    {
        ParentDef = def;
        _frontOffset = frontOffset;
        _colA = drawA;
        _colB = drawB;
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        // if we lost our parent or pusher, bail
        if (parent is not Pawn pawn || ColoredGraphic == null) return;

        // push direction/pos
        Rot4 rot = pawn.Rotation;
        Vector3 dir = new(rot.FacingCell.x, 0f, rot.FacingCell.z);
        Vector3 pos = pawn.DrawPos + dir * _frontOffset;
        pos.y = DrawPosY; // slightly above ground

        // ensure our own Rotation matches pawn so Graphic_Multi picks the right subtex
        Rotation = rot;
        ColoredGraphic.Draw(pos, rot, this);
    }
}

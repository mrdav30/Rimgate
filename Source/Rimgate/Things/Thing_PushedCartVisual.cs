using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.BaseGen;

namespace Rimgate;

// Attaches to a Pawn and draws the cart graphic in front of them.
public class Thing_PushedCartVisual : AttachableThing
{
    public Pawn pusher;

    public ThingDef cartDef;  // which cart visual to use

    public float frontOffset = 1.0f;  // offset ahead of pawn (from Comp props)

    private Color _colA = Color.white, _colB = Color.white;

    private Graphic _coloredGraphic;

    public override Graphic Graphic => RimgateTex.EmptyGraphic;
    
    public override string InspectStringAddon => string.Empty;

    public void Init(Pawn p, ThingDef cartDef, float frontOffset, Color drawA, Color drawB)
    {
        this.pusher = p;
        this.cartDef = cartDef;
        this.frontOffset = frontOffset;
        // cache the multi-rot graphic from the *cart* (not proxy)
        _colA = drawA; _colB = drawB;
        var gd = cartDef.graphicData;
        var baseG = gd?.Graphic;
        if (baseG != null)
            _coloredGraphic = baseG.GetColoredVersion(baseG.Shader, _colA, _colB);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        // if we lost our parent or pusher, bail
        var pawn = parent as Pawn ?? pusher;
        if (pawn == null || _coloredGraphic == null) return;

        // push direction/pos
        Rot4 rot = pawn.Rotation;
        Vector3 dir = new Vector3(rot.FacingCell.x, 0f, rot.FacingCell.z);
        Vector3 pos = pawn.DrawPos + dir * frontOffset;
        pos.y = Altitudes.AltitudeFor(AltitudeLayer.Item); // slightly above ground

        // ensure our own Rotation matches pawn so Graphic_Multi picks the right subtex
        Rotation = rot;
        _coloredGraphic.Draw(pos, rot, this);
    }
}

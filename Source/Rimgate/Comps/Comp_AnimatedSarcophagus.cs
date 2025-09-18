using Rimgate;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_AnimatedSarcophagus : ThingComp
{
    public CompProperties_AnimatedSarcophagus Props => (CompProperties_AnimatedSarcophagus)props;

    public override void PostDraw()
    {
        base.PostDraw();

        Building_Bed_Sarcophagus building_sarcophagus = parent as Building_Bed_Sarcophagus;
        if (!building_sarcophagus.IsSarcophagusInUse()) return;

        Mesh sarchophagusGlowMesh = Props.sarchophagusGlowGraphicData.Graphic.MeshAt(parent.Rotation);

        Vector3 sarchophagusGlowDrawPos = parent.DrawPos;

        float drawAltitude = AltitudeLayer.Pawn.AltitudeFor();

        sarchophagusGlowDrawPos.y = drawAltitude + 0.06f;

        Graphics.DrawMesh(
            sarchophagusGlowMesh,
            sarchophagusGlowDrawPos,
            Quaternion.identity,
            FadedMaterialPool.FadedVersionOf(
                Props.sarchophagusGlowGraphicData.Graphic.MatAt(parent.Rotation, null),
                1f),
            0);
    }
}
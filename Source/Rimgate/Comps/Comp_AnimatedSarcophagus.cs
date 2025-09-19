using Rimgate;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_AnimatedSarcophagus : ThingComp
{
    public CompProperties_AnimatedSarcophagus Props => (CompProperties_AnimatedSarcophagus)props;

    private Mesh GlowMesh => _cachedMesh ??= Props.sarchophagusGlowGraphicData?.Graphic.MeshAt(parent.Rotation);

    private Mesh _cachedMesh;

    private Material GlowMaterial => _cachedMaterial ??= Props.sarchophagusGlowGraphicData?.Graphic.MatAt(parent.Rotation, null);

    private Material _cachedMaterial;

    public override void PostDraw()
    {
        base.PostDraw();

        Building_Bed_Sarcophagus building_sarcophagus = parent as Building_Bed_Sarcophagus;
        if (!building_sarcophagus.IsSarcophagusInUse()) return;

        Vector3 sarchophagusGlowDrawPos = parent.DrawPos;

        float drawAltitude = AltitudeLayer.Pawn.AltitudeFor();

        sarchophagusGlowDrawPos.y = drawAltitude + 0.06f;

        Graphics.DrawMesh(
            GlowMesh,
            sarchophagusGlowDrawPos,
            Quaternion.identity,
            FadedMaterialPool.FadedVersionOf(
                GlowMaterial,
                1f),
            0);
    }
}
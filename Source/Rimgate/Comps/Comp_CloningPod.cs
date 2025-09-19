using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Comp_CloningPod : ThingComp
{
    public static Color IdleCycleColor = new Color(0.9f, 1f, 0.16f);// new Color(0.321f, 1f, 1f);

    public static Color OperatingColor = new Color(0.89f, 0.24f, 0.04f);// new Color(0.267f, 0.792f, 0.969f);

    public bool PowerOn => _clonePod != null && _clonePod.Power.PowerOn;

    public bool Fueled => _clonePod != null && _clonePod.Refuelable.IsFull;

    public CompProperties_CloningPod Props => (CompProperties_CloningPod)props;

    private Building_WraithCloningPod _clonePod;

    public Graphic FullGraphic => _cachedFullGraphic ??= Props.fullGraphicData.Graphic;

    public Graphic _cachedFullGraphic;

    public Graphic EmptyGraphic => _cachedEmptyGraphic ??= Props.emptyGraphicData.Graphic;

    public Graphic _cachedEmptyGraphic;

    private Mesh BackgroundMesh => _cachedMesh ??= Props.backgroundGraphicData?.Graphic.MeshAt(parent.Rotation);

    private Mesh _cachedMesh;

    private Material BackgroundMat => _cachedMaterial ??= Props.backgroundGraphicData?.Graphic.MatAt(parent.Rotation, null);

    private Material _cachedMaterial;

    private Effecter _idleEffecter;

    private Effecter _operatingEffecter;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (parent is Building_WraithCloningPod clonePod)
            _clonePod = clonePod;
    }

    public override void CompTick()
    {
        base.CompTick();

        if (_clonePod.Status != CloningStatus.Idle || !PowerOn || !Fueled)
        {
            _idleEffecter?.Cleanup();
            _idleEffecter = null;
        }
        else
        {
            if (_idleEffecter == null)
            {
                _idleEffecter = RimgateDefOf.Rimgate_ClonePod_Idle.Spawn();
                ColorizeEffecter(_idleEffecter, IdleCycleColor);
                _idleEffecter.Trigger(
                    new TargetInfo(parent),
                    new TargetInfo(parent.InteractionCell, parent.Map));
            }

            _idleEffecter.EffectTick(
                new TargetInfo(parent),
                new TargetInfo(parent.InteractionCell, parent.Map));
        }

        if (!_clonePod.IsWorking)
        {
            _operatingEffecter?.Cleanup();
            _operatingEffecter = null;
        }
        else if (_clonePod.RemainingWork > 0f)
        {
            if (!PowerOn)
            {
                _operatingEffecter?.Cleanup();
                _operatingEffecter = null;
                return;
            }

            IntVec3 operatingPos = (parent.DrawPos + parent.def.graphicData.drawOffset).ToIntVec3();
            if (_operatingEffecter == null)
            {
                _operatingEffecter = RimgateDefOf.Rimgate_ClonePod_Operating.Spawn();
                ColorizeEffecter(_operatingEffecter, OperatingColor);
                _operatingEffecter.Trigger(
                    new TargetInfo(parent),
                    new TargetInfo(operatingPos, parent.Map));
            }

            _operatingEffecter.EffectTick(
                new TargetInfo(parent),
                new TargetInfo(operatingPos, parent.Map));
        }
    }

    private void ColorizeEffecter(Effecter effecter, Color color)
    {
        foreach (SubEffecter child in effecter.children)
        {
            if (child is SubEffecter_Sprayer subEffecter_Sprayer)
                subEffecter_Sprayer.colorOverride = color * child.def.color;
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();

        Vector3 drawPos = parent.DrawPos + parent.def.graphicData.drawOffset;

        Vector3 panePos = drawPos;
        panePos.y = parent.def.altitudeLayer.AltitudeFor() + 0.01f;

        if (Fueled)
        {
            FullGraphic.Draw(
                panePos,
                parent.Rotation,
                parent);
        }
        else
        {
            EmptyGraphic.Draw(
            panePos,
            parent.Rotation,
            parent);
        }

        if (Props.backgroundGraphicData != null)
        {
            Vector3 backgroundPos = drawPos;
            backgroundPos.y -= 2.0f;

            Graphics.DrawMesh(
                BackgroundMesh,
                backgroundPos,
                parent.Rotation.AsQuat,
                BackgroundMat,
                0);
        }

        if (!_clonePod.HasAnyContents) return;

        Pawn occupant = _clonePod.InnerPawn;
        drawPos += new Vector3(0.15f, 0.0f, -0.15f);

        float pawnOffset = 0;
        if (_clonePod.IsWorking)
            pawnOffset = FloatingOffset(_clonePod.RemainingWork);
        drawPos.z += pawnOffset;

        occupant.Drawer.renderer.RenderPawnAt(
            drawPos,
            Rot4.East,
            neverAimWeapon: true);
    }

    public static float FloatingOffset(float tickOffset)
    {
        float num = tickOffset % 500f / 500f;
        float num2 = Mathf.Sin((float)Math.PI * num);
        float z = num2 * 0.135f;
        return z;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        _operatingEffecter?.Cleanup();
        _operatingEffecter = null;
        _idleEffecter?.Cleanup();
        _idleEffecter = null;
    }
}

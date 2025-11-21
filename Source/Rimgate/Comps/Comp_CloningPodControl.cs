using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Rimgate;

public class Comp_CloningPodControl : ThingComp
{
    public bool PowerOn => _clonePod != null && _clonePod.Power.PowerOn;

    public bool Fueled => _clonePod != null && _clonePod.Refuelable.IsFull;

    public CompProperties_CloningPodControl Props => (CompProperties_CloningPodControl)props;

    private Building_CloningPod _clonePod;

    public Graphic FullGraphic => Props.fullGraphicData.Graphic;

    public Graphic EmptyGraphic => Props.emptyGraphicData.Graphic;

    private Mesh BackgroundMesh => _cachedBackgroundMesh ??= Props.backgroundGraphicData?.Graphic.MeshAt(parent.Rotation);

    private Mesh _cachedBackgroundMesh;

    private Material BackgroundMat => _cachedBackgroundMat ??= Props.backgroundGraphicData?.Graphic.MatAt(parent.Rotation, null);

    private Material _cachedBackgroundMat;

    private Effecter _idleEffecter;

    private Effecter _operatingEffecter;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (parent is Building_CloningPod clonePod)
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
                ColorizeEffecter(_idleEffecter, Props.idleCycleColor);
                _idleEffecter.Trigger(parent, new TargetInfo(parent.InteractionCell, parent.Map));
            }


            _idleEffecter.EffectTick(parent, new TargetInfo(parent.InteractionCell, parent.Map));
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
                ColorizeEffecter(_operatingEffecter, Props.operatingColor);
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
        panePos.y = parent.def.altitudeLayer.AltitudeFor() - 0.01f;

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
                Quaternion.identity,
                BackgroundMat,
                0);
        }

        if (!_clonePod.HasAnyContents) return;

        Pawn occupant = _clonePod.InnerPawn;
        var rotation = parent.Rotation;
        drawPos += GetPawnDrawOffset(rotation);

        float floatOffset = 0;
        if (_clonePod.IsWorking)
            floatOffset = FloatingOffset(_clonePod.RemainingWork);

        if (rotation == Rot4.North || rotation == Rot4.South)
            drawPos.z += floatOffset;
        else
            drawPos.x += floatOffset;

        occupant.Drawer.renderer.RenderPawnAt(
            drawPos,
            null,
            neverAimWeapon: true);
    }

    private static Vector3 GetPawnDrawOffset(Rot4 rot)
    {
        if (rot == Rot4.North)
            return new Vector3(0f, 0f, 0.5f);

        if (rot == Rot4.East)
            return new Vector3(0.5f, 0f, 0.25f);

        if (rot == Rot4.South)
            return new Vector3(0f, 0.0f, -0.35f);

        if (rot == Rot4.West)
            return new Vector3(-0.5f, 0f, 0.25f);

        return Vector3.zero;
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

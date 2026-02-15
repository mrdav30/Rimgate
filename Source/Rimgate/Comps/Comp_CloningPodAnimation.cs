using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Noise;
using static UnityEngine.Networking.UnityWebRequest;

namespace Rimgate;

public class Comp_CloningPodAnimation : ThingComp
{
    public CompProperties_CloningPodAnimation Props => (CompProperties_CloningPodAnimation)props;

    private Building_CloningPod _clonePod;

    public Graphic FullGraphic => Props.fullGraphicData.Graphic;

    public Graphic EmptyGraphic => Props.emptyGraphicData.Graphic;

    private Mesh BackgroundMesh => _cachedBackgroundMesh ??= Props.backgroundGraphicData?.Graphic.MeshAt(parent.Rotation);

    private Material BackgroundMat => _cachedBackgroundMat ??= Props.backgroundGraphicData?.Graphic.MatAt(parent.Rotation, null);

    private Material _cachedBackgroundMat;

    private Mesh _cachedBackgroundMesh;

    private Effecter _idleEffecter;

    private Effecter _operatingEffecter;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (parent is Building_CloningPod clonePod)
            _clonePod = clonePod;
    }

    public override void CompTick()
    {
        if (_clonePod == null)
            return;

        if (_clonePod.Status != CloningStatus.Idle || !_clonePod.Powered)
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
        else
        {
            if (!_clonePod.Powered)
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
        if (_clonePod == null)
            return;

        Vector3 drawPos = parent.DrawPos + parent.def.graphicData.drawOffset;
        Rot4 rotation = parent.Rotation;
        Vector3 panePos = drawPos;
        panePos.y = parent.def.altitudeLayer.AltitudeFor() - 0.01f;

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

        if (_clonePod.FuelForCurrentCycleConsumed)
        {
            FullGraphic.Draw(
                panePos,
                rotation,
                parent);
        }
        else
        {
            EmptyGraphic.Draw(
                panePos,
                rotation,
                parent);
        }

        if (_clonePod.Status == CloningStatus.Paused && !_clonePod.TryGetCostForCurrentCycle(out _))
        {
            parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.OutOfFuel);
            return;
        }

        float ticks = _clonePod.Status == CloningStatus.CloningStarted
            ? _clonePod.RemainingCalibrationWork
            : _clonePod.Status == CloningStatus.Incubating
                ? _clonePod.RemainingIncubationTicks
                : 0f;

        if (_clonePod.Status == CloningStatus.Incubating)
        {
            var stageCount = Props.incubatingStagesGraphicData?.Count ?? 0;
            if (stageCount <= 0) return;

            var percent = _clonePod.GetIncubationProgress();
            if (percent <= 0) return;

            // once close enough, show full stage
            if (percent < 0.85f)
            {
                int stageIndex = Mathf.FloorToInt(percent * stageCount);
                stageIndex = Mathf.Clamp(stageIndex, 0, stageCount - 1);

                var stageGraphic = Props.incubatingStagesGraphicData[stageIndex].Graphic;
                var oppositeRot = rotation.Opposite;
                drawPos += GetDrawOffset(rotation, ticks);
                drawPos.y = _clonePod.HeldPawnDrawPos_Y;
                stageGraphic.Draw(
                    drawPos,
                    oppositeRot,
                    parent);

                return;
            }
        }

        Pawn occupant = _clonePod.Status != CloningStatus.Incubating && _clonePod.HasHostPawn
            ? _clonePod.HostPawn
            : _clonePod.Status == CloningStatus.Incubating && _clonePod.HasClonePawn
                ? _clonePod.ClonePawn
                : null;

        if (occupant == null)
            return;

        drawPos += GetDrawOffset(rotation, ticks);

        occupant.Drawer.renderer.RenderPawnAt(
            drawPos,
            null,
            neverAimWeapon: true);

        return;
    }

    private Vector3 GetDrawOffset(Rot4 rot, float ticks)
    {
        Vector3 result = Vector3.zero;
        if (rot == Rot4.North)
            result = new Vector3(0f, 0f, 0.5f);
        else if (rot == Rot4.East)
            result = new Vector3(0.5f, 0f, 0.25f);
        else if (rot == Rot4.South)
            result = new Vector3(0f, 0.0f, -0.35f);
        else if (rot == Rot4.West)
            result = new Vector3(-0.5f, 0f, 0.25f);

        if (_clonePod.IsWorking)
            return SetFloatingOffset(result, rot, ticks);
        else
            return result;
    }

    private Vector3 SetFloatingOffset(Vector3 drawPos, Rot4 rot, float tickOffset)
    {
        float floatOffset = FloatingOffset(tickOffset);
        if (rot == Rot4.North || rot == Rot4.South)
            drawPos.z += floatOffset;
        else
            drawPos.x += floatOffset;
        return drawPos;
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
        _cachedBackgroundMat = null;
        _cachedBackgroundMesh = null;

        _operatingEffecter?.Cleanup();
        _operatingEffecter = null;
        _idleEffecter?.Cleanup();
        _idleEffecter = null;
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        _operatingEffecter?.Cleanup();
        _operatingEffecter = null;
        _idleEffecter?.Cleanup();
        _idleEffecter = null;
    }
}

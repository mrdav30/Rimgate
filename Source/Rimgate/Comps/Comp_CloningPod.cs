using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Comp_CloningPod : ThingComp
{
    public static readonly Material BackgroundMat = SolidColorMaterials.NewSolidColorMaterial(
        new Color(0.082f, 0.078f, 0.063f),
        ShaderDatabase.SolidColorBehind);

    public static Color IdleCycleColor = new Color(0.321f, 1f, 1f);

    public static Color OperatingColor = new Color(0.267f, 0.792f, 0.969f);

    public bool PowerOn => _clonePod != null && _clonePod.Power.PowerOn;

    public bool Fueled => _clonePod != null && _clonePod.Refuelable.IsFull;

    public CompProperties_CloningPod Props => (CompProperties_CloningPod)props;

    private Building_WraithCloningPod _clonePod;

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
            if (_clonePod.RemainingWork > 0f)
            {
                if (!PowerOn)
                {
                    _operatingEffecter?.Cleanup();
                    _operatingEffecter = null;
                }
                else
                {
                    if (_operatingEffecter == null)
                    {
                        _operatingEffecter = RimgateDefOf.Rimgate_ClonePod_Operating.Spawn();
                        ColorizeEffecter(_operatingEffecter, OperatingColor);
                        _operatingEffecter.Trigger(parent, new TargetInfo(parent.InteractionCell, parent.Map));
                    }

                    _operatingEffecter.EffectTick(parent, new TargetInfo(parent.InteractionCell, parent.Map));
                }
            }
        }
    }

    private void ColorizeEffecter(Effecter effecter, Color color)
    {
        foreach (SubEffecter child in effecter.children)
        {
            if (child is SubEffecter_Sprayer subEffecter_Sprayer)
            {
                subEffecter_Sprayer.colorOverride = color * child.def.color;
            }
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();
        Rot4 rotation = parent.Rotation;
        Vector3 s = new Vector3(parent.def.graphicData.drawSize.x * 0.8f, 1f, parent.def.graphicData.drawSize.y * 0.8f);
        Vector3 drawPos = parent.DrawPos;

        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, rotation.AsQuat, s), BackgroundMat, 0);

        if (_clonePod.HasAnyContents)
        {
            Pawn occupant = _clonePod.InnerPawn;
            Vector3 drawLoc = parent.InteractionCell.ToVector3();

            if (rotation == Rot4.South)
            {
                drawLoc += new Vector3(0.5f, 0.0f, 2f);
            }
            else if (rotation == Rot4.West)
            {
                drawLoc += new Vector3(2f, 0.0f, 0.35f);
            }
            else if (rotation == Rot4.North)
            {
                drawLoc += new Vector3(0.5f, 0.0f, -0.9f);
            }
            else if (rotation == Rot4.East)
            {
                drawLoc += new Vector3(-1f, 0.0f, 0.35f);
            }
            else drawLoc = parent.DrawPos;

            float offset = 0;
            if (_clonePod.IsWorking)
                offset = FloatingOffset(_clonePod.RemainingWork);

            if (rotation == Rot4.East || rotation == Rot4.West)
            {
                drawLoc.x += offset;
                drawLoc.z += 0.2f;
            }
            else
                drawLoc.z += offset;

            occupant.Drawer.renderer.RenderPawnAt(drawLoc, null, neverAimWeapon: true);
        }
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

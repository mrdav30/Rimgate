using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_Toggle : ThingComp
{
    public const string OffGraphicSuffix = "_Off";

    public const string FlickedOnSignal = "FlickedOn";

    public const string FlickedOffSignal = "FlickedOff";

    private CompProperties_Toggle Props => (CompProperties_Toggle)props;

    private bool _switchOnInt = false;

    private bool _wantSwitchOn = false;

    private Graphic _offGraphic;

    private Texture2D _cachedCommandTex;

    public Texture2D CommandTex
    {
        get
        {
            _cachedCommandTex ??= ContentFinder<Texture2D>.Get(Props.commandTexture);
            return _cachedCommandTex;
        }
    }

    public bool SwitchIsOn
    {
        get => _switchOnInt;
        set
        {
            if (_switchOnInt != value)
            {
                _switchOnInt = value;
                if (_switchOnInt)
                    parent.BroadcastCompSignal("FlickedOn");
                else
                    parent.BroadcastCompSignal("FlickedOff");

                if (!parent.Spawned)
                    return;

                parent.Map.mapDrawer.MapMeshDirty(parent.Position, (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.Things);
            }
        }
    }

    public Graphic CurrentGraphic
    {
        get
        {
            if (SwitchIsOn)
                return parent.DefaultGraphic;

            if (_offGraphic == null)
            {
                _offGraphic = GraphicDatabase.Get(
                    parent.def.graphicData.graphicClass,
                    parent.def.graphicData.texPath + "_Off",
                    parent.def.graphicData.shaderType.Shader,
                    parent.def.graphicData.drawSize,
                    parent.DrawColor,
                    parent.DrawColorTwo);
            }

            return _offGraphic;
        }
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        _switchOnInt = Props.defaultState;
        _wantSwitchOn = Props.defaultState;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _switchOnInt, "_switchOn", defaultValue: false);
        Scribe_Values.Look(ref _wantSwitchOn, "_wantSwitchOn", defaultValue: false);
    }

    public bool WantsFlick() => _wantSwitchOn != _switchOnInt;

    public void DoFlick()
    {
        SwitchIsOn = !SwitchIsOn;
        SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
    }

    public void ResetToOn()
    {
        _switchOnInt = true;
        _wantSwitchOn = true;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction == Faction.OfPlayer)
        {
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.hotKey = KeyBindingDefOf.Command_TogglePower;
            command_Toggle.icon = CommandTex;
            command_Toggle.defaultLabel = Props.commandLabelKey.Translate(parent.Label);
            command_Toggle.defaultDesc = Props.commandDescKey.Translate(parent.Label);
            command_Toggle.isActive = () => _wantSwitchOn;
            command_Toggle.toggleAction = delegate
            {
                _wantSwitchOn = !_wantSwitchOn;
                Designation designation = parent.Map.designationManager.DesignationOn(parent, RimgateDefOf.Rimgate_DesignationToggle);

                if (designation == null)
                    parent.Map.designationManager.AddDesignation(new Designation(parent, RimgateDefOf.Rimgate_DesignationToggle));
                else
                    designation?.Delete();
            };
            yield return command_Toggle;
        }
    }
}

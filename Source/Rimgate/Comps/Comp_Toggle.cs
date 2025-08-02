using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

public class Comp_Toggle : ThingComp
{
    private bool switchOnInt = false;

    private bool wantSwitchOn = false;

    private Graphic offGraphic;

    private Texture2D cachedCommandTex;

    private const string OffGraphicSuffix = "_Off";

    public const string FlickedOnSignal = "FlickedOn";

    public const string FlickedOffSignal = "FlickedOff";

    private CompProperties_Toggle Props => (CompProperties_Toggle)props;

    private Texture2D CommandTex
    {
        get
        {
            if (cachedCommandTex == null)
                cachedCommandTex = ContentFinder<Texture2D>.Get(Props.commandTexture);

            return cachedCommandTex;
        }
    }

    public bool SwitchIsOn
    {
        get => switchOnInt;
        set
        {
            if (switchOnInt != value)
            {
                switchOnInt = value;
                if (switchOnInt)
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

            if (offGraphic == null)
            {
                offGraphic = GraphicDatabase.Get(
                    parent.def.graphicData.graphicClass,
                    parent.def.graphicData.texPath + "_Off",
                    parent.def.graphicData.shaderType.Shader,
                    parent.def.graphicData.drawSize,
                    parent.DrawColor,
                    parent.DrawColorTwo);
            }

            return offGraphic;
        }
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        switchOnInt = Props.defaultState;
        wantSwitchOn = Props.defaultState;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref switchOnInt, "switchOn", defaultValue: false);
        Scribe_Values.Look(ref wantSwitchOn, "wantSwitchOn", defaultValue: false);
    }

    public bool WantsFlick() => wantSwitchOn != switchOnInt;

    public void DoFlick()
    {
        SwitchIsOn = !SwitchIsOn;
        SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
    }

    public void ResetToOn()
    {
        switchOnInt = true;
        wantSwitchOn = true;
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
            command_Toggle.isActive = () => wantSwitchOn;
            command_Toggle.toggleAction = delegate
            {
                wantSwitchOn = !wantSwitchOn;
                UpdateFlickDesignation(parent);
            };
            yield return command_Toggle;
        }
    }

    public static void UpdateFlickDesignation(Thing t)
    {
        Designation designation = t.Map.designationManager.DesignationOn(t, DesignationDefOf.Flick);
        if (designation == null)
            t.Map.designationManager.AddDesignation(new Designation(t, DesignationDefOf.Flick));
    }
}

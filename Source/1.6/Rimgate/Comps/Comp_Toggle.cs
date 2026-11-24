using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimgate;

// Like a flickable, but for toggling things on and off independent of power
public class Comp_Toggle : ThingComp
{
    protected CompProperties_Toggle Props => (CompProperties_Toggle)props;

    public Graphic OffGraphic => Props.offGraphicData?.Graphic;

    public Texture2D CommandOnTex => _cachedCommandOnTex ??= ContentFinder<Texture2D>.Get(Props.commandOnIconTexPath);

    public Texture2D CommandOffTex => _cachedCommandOffTex ??= !Props.commandOffIconTexPath.NullOrEmpty()
        ? ContentFinder<Texture2D>.Get(Props.commandOffIconTexPath, true)
        : null;

    public bool SwitchIsOn
    {
        get => _switchOnInt;
        set
        {
            if (_switchOnInt != value)
            {
                _switchOnInt = value;
                if (_switchOnInt)
                    parent.BroadcastCompSignal(Props.onSignal);
                else
                    parent.BroadcastCompSignal(Props.offSignal);

                if (!parent.Spawned)
                    return;

                parent.Map.mapDrawer.MapMeshDirty(parent.Position, (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.Things);
            }
        }
    }

    public bool WantsToggle => _wantToggle;

    protected bool _switchOnInt = false;

    protected bool _wantToggle = false;

    private Texture2D _cachedCommandOnTex;

    private Texture2D _cachedCommandOffTex;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        _switchOnInt = Props.isOnByDefault;
        _wantToggle = false;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _switchOnInt, "_switchOnInt", defaultValue: false);
        Scribe_Values.Look(ref _wantToggle, "_wantToggle", defaultValue: false);
    }

    public virtual void DoToggle()
    {
        SwitchIsOn = !SwitchIsOn;
        _wantToggle = false;
        SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
    }

    public virtual void SetToggleStatus(bool status)
    {
        _switchOnInt = status;
        _wantToggle = false;
    }

    public override bool DontDrawParent() => OffGraphic != null && !SwitchIsOn;

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (OffGraphic == null || SwitchIsOn)
            return;

        OffGraphic.Draw(drawLoc, flip ? parent.Rotation.Opposite : parent.Rotation, parent);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!parent.Faction.IsOfPlayerFaction()) yield break;

        Command_Toggle command_Toggle = new Command_Toggle();
        command_Toggle.icon = _switchOnInt && CommandOffTex != null ? CommandOffTex : CommandOnTex;
        command_Toggle.defaultLabel = Props.commandLabelKey.Translate(parent.Label);
        command_Toggle.defaultDesc = Props.commandDescKey.Translate(parent.Label);
        command_Toggle.isActive = () => _wantToggle;
        command_Toggle.toggleAction = delegate
        {
            _wantToggle = !_wantToggle;

            var dm = parent.Map.designationManager;
            Designation des = dm.DesignationOn(parent, RimgateDefOf.Rimgate_DesignationToggle);

            if (_wantToggle && des == null)
                dm.AddDesignation(new Designation(parent, RimgateDefOf.Rimgate_DesignationToggle));
            else
                des?.Delete();
        };

        yield return command_Toggle;
    }
}

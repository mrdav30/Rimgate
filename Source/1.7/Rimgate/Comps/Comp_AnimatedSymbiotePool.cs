using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_AnimatedSymbiotePool : ThingComp
{
    private const float TicksPerRare = 250f;

    public CompProperties_AnimatedSymbiotePool Props => (CompProperties_AnimatedSymbiotePool)props;

    private bool _enabled;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _enabled, "_enabled", false);
    }

    public void Toggle(bool status) => _enabled = status;

    public override void PostSpawnSetup(bool respawningAfterLoad)
        => _enabled = Props.enabledByDefault;

    public override void CompTick()
    {
        if (!parent.Spawned) return;

        // VFX
        bool canFx = ModsConfig.OdysseyActive
            && _enabled
            && parent.IsHashIntervalTick(Props.moteIntervalTicks);
        if (canFx)
            TrySpawnSymbioteMote();
    }

    public override void CompTickRare()
    {
        if (!parent.Spawned) return;

        // VFX
        bool canFx = ModsConfig.OdysseyActive
            && _enabled
            && parent.IsHashIntervalTick(Props.moteIntervalTicks);
        if (canFx)
            TrySpawnSymbioteMote();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        _enabled = false;
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        _enabled = false;
    }

    private void TrySpawnSymbioteMote()
    {
        var map = parent.Map;
        if (map == null) return;

        var def = (Rand.Bool
            ? FleckDefOf.FishShadowReverse
            : FleckDefOf.FishShadow) ?? FleckDefOf.FishShadow;
        if (def == null) return;

        // Slight jitter from center for variety
        Vector3 loc = parent.TrueCenter().SetToAltitude(AltitudeLayer.MoteLow);
        loc.x += Rand.Range(-0.10f, 0.10f);
        loc.z += Rand.Range(-0.05f, 0.05f);

        if (!loc.ShouldSpawnMotesAt(map)) return;

        float scale = Props.moteBaseScale * Props.moteScaleRange.RandomInRange;
        var data = FleckMaker.GetDataStatic(loc, map, def, scale);
        data.rotation = Rand.Range(0f, 360f);
        data.rotationRate = Rand.Sign * Props.moteRotationSpeed * Rand.Range(0.85f, 1.15f);

        map.flecks.CreateFleck(data);
    }
}

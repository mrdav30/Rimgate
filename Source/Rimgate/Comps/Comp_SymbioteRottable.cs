using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Rimgate;

public class Comp_SymbioteRottable : CompRottable
{
    private const float FreezeThreshold = 0f; // Celsius

    public CompProperties_SymbioteRottable SymbProps => (CompProperties_SymbioteRottable)props;

    public override void CompTickRare()
    {
        if (!parent.SpawnedOrAnyParentSpawned || !Active)
            return;

        base.CompTickRare();
        CheckFrozenAndDestroyIfNeeded();
    }

    public override void CompTickInterval(int delta)
    {
        if (!parent.SpawnedOrAnyParentSpawned || !Active)
            return;

        base.CompTickInterval(delta);

        if (Find.TickManager.TicksGame % GenTicks.TickRareInterval == 0)
            CheckFrozenAndDestroyIfNeeded();
    }

    private void CheckFrozenAndDestroyIfNeeded()
    {
        if (IsInSymbiotePool())
            return;

        float temp = parent.AmbientTemperature;
        if (temp > FreezeThreshold)
            return;

        Map map = parent.MapHeld;
        IntVec3 pos = parent.PositionHeld;

        Messages.Message(
            "RG_SymbioteFrozen".Translate(parent.LabelCap),
            map != null ? new TargetInfo(pos, map) : GlobalTargetInfo.Invalid,
            MessageTypeDefOf.NegativeEvent
        );

        parent.Destroy();
    }

    private bool IsInSymbiotePool()
    {
        // Stored directly in the spawning pool's inner container
        return parent?.ParentHolder is Building_SymbioteSpawningPool;
    }
}

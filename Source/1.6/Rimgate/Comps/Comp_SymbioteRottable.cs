using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Rimgate;

public class Comp_SymbioteRottable : CompRottable
{
    private const float FreezeThreshold = 0f; // Celsius

    public bool ShouldRot
    {
        get
        {
            if (disabled)
                return false;

            // If the item is in a spawning pool or canopic jar, it should not rot
            // Note: This is a safety check in case the disabled flag is not properly set by the container buildings
            if (parent.ParentHolder is Thing container && (container is Building_SymbioteSpawningPool || container is Building_CanopicJar))
                return false;

            // Otherwise, it should rot as normal
            return true;
        }
    }

    public override void CompTickRare()
    {
        if (!ShouldRot)
            return;

        base.CompTickRare();
        CheckFrozenAndDestroyIfNeeded();
    }

    public override void CompTickInterval(int delta)
    {
        if (!ShouldRot)
            return;

        base.CompTickInterval(delta);

        if (Find.TickManager.TicksGame % GenTicks.TickRareInterval == 0)
            CheckFrozenAndDestroyIfNeeded();
    }

    private void CheckFrozenAndDestroyIfNeeded()
    {
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
}

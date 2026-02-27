using System.Runtime.CompilerServices;
using Verse;

namespace Rimgate;

public enum SwapTier
{
    None,
    TierOne,
    TierTwo
}

public class Comp_BodyGraphicSwapper : ThingComp
{
    public CompProperties_BodyGraphicSwapper Props => (CompProperties_BodyGraphicSwapper)props;

    private SwapTier _currentTier;

    public override void CompTickRare()
    {
        var pawn = parent as Pawn;
        if (pawn == null || pawn.Dead || !pawn.Spawned) return;
        var frameTier = GetCurrentSwapTier(pawn);
        if (frameTier != _currentTier)
        {
            _currentTier = frameTier;
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Graphic GetGraphicForTierOne(Pawn pawn)
    {
        if (pawn.gender == Gender.None || pawn.gender == Gender.Male)
            return Props.tierOneGraphicData.Graphic;
        return Props.tierOneFemaleGraphicData.Graphic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Graphic GetGraphicForTierTwo(Pawn pawn)
    {
        if (pawn.gender == Gender.None || pawn.gender == Gender.Male)
            return Props.tierTwoGraphicData.Graphic;
        return Props.tierTwoFemaleGraphicData.Graphic;
    }

    // TODO: Make the thresholds configurable via CompProperties
    public virtual SwapTier GetCurrentSwapTier(Pawn pawn)
    {
        var healthPercentage = pawn.health.summaryHealth.SummaryHealthPercent;
        if (healthPercentage < 0.75f && healthPercentage >= 0.35f)
            return SwapTier.TierOne;
        else if (healthPercentage < 0.35f)
            return SwapTier.TierTwo;
        else
            return SwapTier.None;
    }

    public Graphic GetCurrentPawnGraphic(Pawn pawn)
    {
        if (pawn == null) return null;

        // Update current tier only if pawn is alive to avoid unnecessary changes after death
        if (!pawn.Dead)
            _currentTier = GetCurrentSwapTier(pawn);
        switch (_currentTier)
        {
            case SwapTier.TierOne:
                return GetGraphicForTierOne(pawn);
            case SwapTier.TierTwo:
                return GetGraphicForTierTwo(pawn);
            default:
                return null;
        }
    }

    public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
    {
        var pawn = parent as Pawn;
        if (pawn == null) return;
        pawn.Drawer?.renderer?.SetAllGraphicsDirty();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref _currentTier, "currentTier", SwapTier.None);
    }
}

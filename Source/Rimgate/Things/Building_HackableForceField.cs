using UnityEngine;
using Verse;
using RimWorld;

namespace Rimgate;

/// <summary>
/// A door-like energy barrier:
/// - Blocks passage while "Locked" (not hacked).
/// - Allows free passage when hacked.
/// - If it has CompPowerTrader, respects PowerOn; otherwise assumes powered.
/// - Uses CompProperties_Hackable.unhackedGraphicData to show the red (locked) look.
///   The building's ThingDef.graphicData should be the blue (hacked) look.
/// </summary>
public class Building_HackableForceField : Building_SupportedDoor, IHackable
{
    private CompHackable _hackable;
    private CompPowerTrader _power;

    private CompHackable Hackable => _hackable ??= GetComp<CompHackable>();
    private CompPowerTrader Power => _power ??= GetComp<CompPowerTrader>();

    private bool RequiresPower => Power != null;
    private bool IsPowered => !RequiresPower || Power.PowerOn;

    public bool Locked => !(Hackable?.IsHacked ?? false);

    // Let anyone attempt to hack (IHackable/CompHackable gates success).
    protected override bool CheckFaction => false;

    // We want normal "closed door" pathing unless hacked+powered.
    // No animation: open instantly when allowed.
    public override bool FreePassage => IsPowered && !Locked;

    protected override bool AlwaysOpen => FreePassage ? true : false;
    protected override float OpenPct => FreePassage ? 1f : 0f;
    protected override bool CanDrawMovers => false;

    public override bool PawnCanOpen(Pawn p)
    {
        // If it’s effectively unpowered, treat as hard barrier
        if (!IsPowered) return false;
        // Only open when hacked; then defer to base for the rest
        return !Locked && base.PawnCanOpen(p);
    }

    // The most authoritative check doors use when a pawn actually reaches them
    public override bool BlocksPawn(Pawn p)
    {
        // Still honor base checks (downed pawns etc.), but *only* if the field is “down”
        if (IsPowered && Locked) return true;

        return base.BlocksPawn(p);
    }

    public void OnLockedOut(Pawn pawn = null)
    {
        // optional: feedback (mote/sound) when a hack lockout occurs
    }

    public void OnHacked(Pawn pawn = null)
    {
        // Optional: claim to the hacker's faction to avoid weird “forbidden” visuals
        if (pawn?.Faction != null)
            SetFaction(pawn.Faction);
        // The CompHackable will handle switching visuals (unhackedGraphic overlay disappears).
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        // Draw the shimmering field only when the barrier is “up”: locked + powered.
        if (RequiresPower && ((Power?.PowerOn ?? false)) || Locked) return;

        Graphic?.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this);
    }
}
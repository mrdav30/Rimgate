using RimWorld;
using Verse;

namespace Rimgate;

public class Building_HackableSingleDoor : Building_Door, IHackable
{
    public CompHackable Hackable => _cachedHackable ??= GetComp<CompHackable>();

    public bool Locked => !Hackable.IsHacked;

    protected override bool CheckFaction => false;

    protected CompHackable _cachedHackable;

    public override bool PawnCanOpen(Pawn p) => !Locked && base.PawnCanOpen(p);

    public override bool BlocksPawn(Pawn p) => Locked || base.BlocksPawn(p);

    public virtual void OnLockedOut(Pawn pawn = null) { }

    public virtual void OnHacked(Pawn pawn = null)
    {
        if (pawn?.Faction != null)
            SetFaction(pawn.Faction);
    }
}

using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class Building_WraithCocoonPod : Building_CryptosleepCasket
{
    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn) => Enumerable.Empty<FloatMenuOption>();

    public override IEnumerable<Gizmo> GetGizmos()
    {
        yield break;
    }
}
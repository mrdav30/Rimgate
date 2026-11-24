using RimWorld;
using Verse;

namespace Rimgate;

public class TokraTunnelCrystalDefModExt : DefModExtension
{
    public int radius = 8;
    public int maxTiles = 18;
    public bool destroySpawnsYield = true;
}

public class CompTargetEffect_TokraTunnelCrystal : CompTargetEffect
{
    private TokraTunnelCrystalDefModExt Ext =>
        parent.def.GetModExtension<TokraTunnelCrystalDefModExt>() ?? new TokraTunnelCrystalDefModExt();

    public override void DoEffectOn(Pawn user, Thing target)
    {
        Map map = user?.Map ?? parent.Map;
        if (map == null) return;

        IntVec3 pos = target.Position;

        var ext = Ext;

        // Collect candidate cells in radius that have a Mine designation
        int max = ext.maxTiles;
        int radius = ext.radius;

        int num = GenRadial.NumCellsInRadius(radius);
        int cleared = 0;

        for (int i = 0; i < num && cleared < max; i++)
        {
            IntVec3 c = pos + GenRadial.RadialPattern[i];
            if (!c.InBounds(map)) continue;

            // Only consider cells with mineable edifices
            Building ed = c.GetEdifice(map);
            if (ed == null || ed is not Mineable mineable || !ed.def.mineable) continue;

            // Only act on Mine designations
            Designation des = map.designationManager.DesignationAt(c, DesignationDefOf.Mine);
            if (des == null) continue;

            // Remove designation
            map.designationManager.RemoveDesignation(des);

            if (ext.destroySpawnsYield)
                mineable.DestroyMined(user); // spawns yield as well as removes the edifice
            else
                mineable.Destroy(DestroyMode.Vanish); // just removes the edifice, no yield

            cleared++;
        }

        user.ApplyHediff(RimgateDefOf.Rimgate_CrystalResonanceEffect);

        if (user.Faction.IsOfPlayerFaction())
        {
            Messages.Message(
                "RG_TunnelCrystalClearedDesignation".Translate(cleared),
                new TargetInfo(pos, map),
                MessageTypeDefOf.PositiveEvent);
        }
    }
}

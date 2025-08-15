using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public class IncidentWorker_KillAllClonesNow : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms) => RimgateModSettings.EnableCloneIncidents;
 
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        List<Pawn> list = PawnsFinder.AllMaps_FreeColonistsSpawned
            .Concat<Pawn>(PawnsFinder.AllMaps_SpawnedPawnsInFaction(Faction.OfPlayer))
            .Concat<Pawn>(Find.WorldPawns.AllPawnsAliveOrDead)
            .ToList<Pawn>();
        int num = 0;
        foreach (Pawn pawn in list)
        {
            if (CloneUtility.HasCloneHediff(pawn))
            {
                KillPawn(pawn);
                ++num;
            }
        }

        if (num > 0)
            Find.LetterStack.ReceiveLetter(
                Translator.Translate("RG_LetterClonesDiedLabel"),
                Translator.Translate("RG_LetterClonesDiedLabelDesc"),
                LetterDefOf.ThreatBig,
                null,
                null,
                null,
                null,
                null,
                0,
                true);
        return true;
    }

    private void KillPawn(Pawn pawn)
    {
        if (pawn.Dead)
            return;
        pawn.Kill(null, null);
    }
}

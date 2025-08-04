using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Rimgate;

public class CaravanArrivalAction_PermanentStargateSite : CaravanArrivalAction
{
    private MapParent _arrivalSite;

    public override string Label => "ApproachSite".Translate(_arrivalSite.Label);

    public override string ReportString => "ApproachingSite".Translate(_arrivalSite.Label);

    public CaravanArrivalAction_PermanentStargateSite(MapParent site) => _arrivalSite = site;

    public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
    {
        if (_arrivalSite != null
            && _arrivalSite.Tile != destinationTile) return false;

        return true;
    }

    public override void Arrived(Caravan caravan)
    {
        Find.LetterStack.ReceiveLetter(
            "LetterLabelCaravanEnteredMap".Translate(_arrivalSite),
            "LetterCaravanEnteredMap".Translate(caravan.Label, _arrivalSite).CapitalizeFirst(),
            LetterDefOf.NeutralEvent,
            caravan.PawnsListForReading);

        LongEventHandler.QueueLongEvent(() =>
            {
                Map map = null;
                map = GetOrGenerateMapUtility.GetOrGenerateMap(
                    _arrivalSite.Tile,
                    new IntVec3(75, 1, 75),
                    _arrivalSite.def);
                CaravanEnterMapUtility.Enter(caravan, _arrivalSite.Map, CaravanEnterMode.Center);
            },
            "GeneratingMapForNewEncounter",
            false,
            null);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref _arrivalSite, "_arrivalSite");
    }
}

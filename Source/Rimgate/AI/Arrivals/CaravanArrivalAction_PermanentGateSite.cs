using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Rimgate;

public class CaravanArrivalAction_PermanentGateSite : CaravanArrivalAction
{
    private WorldObject_GateTransitSite _arrivalSite;

    public override string Label => "ApproachSite".Translate(_arrivalSite.Label);

    public override string ReportString => "ApproachingSite".Translate(_arrivalSite.Label);

    public CaravanArrivalAction_PermanentGateSite(MapParent site) => _arrivalSite = site as WorldObject_GateTransitSite;

    public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
    {
        return _arrivalSite == null || _arrivalSite.Tile == destinationTile;
    }

    public override void Arrived(Caravan caravan)
    {
        Find.LetterStack.ReceiveLetter(
            "LetterLabelCaravanEnteredMap".Translate(_arrivalSite),
            "LetterCaravanEnteredMap".Translate(caravan.Label, _arrivalSite).CapitalizeFirst(),
            LetterDefOf.NeutralEvent,
            caravan.PawnsListForReading);

        IntVec3 mapSize = _arrivalSite.def.overrideMapSize.HasValue
            ? _arrivalSite.def.overrideMapSize.Value
            : new IntVec3(75, 1, 75); // default size if not specified
        LongEventHandler.QueueLongEvent(() =>
            {
                Map map = GetOrGenerateMapUtility.GetOrGenerateMap(
                    _arrivalSite.Tile,
                    mapSize,
                    _arrivalSite.def);
                CaravanEnterMapUtility.Enter(caravan, _arrivalSite.Map, CaravanEnterMode.Center);
            },
            "RG_EnteringGateSite_Transit",
            false,
            null);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref _arrivalSite, "_arrivalSite");
    }
}

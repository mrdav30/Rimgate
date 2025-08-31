using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class Item_ScrapNote : ThingWithComps
{
    public static string[] NoteKeys = new string[15]
    {
        "RG_ScribbledNote1",
        "RG_ScribbledNote2",
        "RG_ScribbledNote3",
        "RG_ScribbledNote4",
        "RG_ScribbledNote5",
        "RG_ScribbledNote6",
        "RG_ScribbledNote7",
        "RG_ScribbledNote8",
        "RG_ScribbledNote9",
        "RG_ScribbledNote10",
        "RG_ScribbledNote11",
        "RG_ScribbledNote12",
        "RG_ScribbledNote13",
        "RG_ScribbledNote14",
        "RG_ScribbledNote15",
    };

    private int _keyIndex = -1;

    public override string DescriptionFlavor => _keyIndex > -1 
        ? NoteKeys[_keyIndex].Translate()
        : string.Empty;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        if (NoteKeys.Length > 0)
        {
            IntRange intRange = new IntRange(0, 14);
            _keyIndex = intRange.RandomInRange;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _keyIndex, "_keyIndex", -1);
    }
}

using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Rimgate
{
    /// Spawns a small group of specific pawns when a signal fires, then assigns a raid lord.
    public class QuestPart_SpawnPawnsOnSignal : QuestPart
    {
        public string inSignal;

        public MapParent mapParent;

        public FactionDef factionDef;

        public PawnKindDef pawnKind;

        public List<PawnInventoryOption> inventoryOptions;

        public float skipChance;

        public IntRange countRange = new IntRange(0, 2);

        public bool alwaysAtLeastOne = false;

        public bool spawnNearGate = true;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignal) return;

            if (mapParent == null || !mapParent.HasMap)
                return;

            if (Rand.Value < skipChance) return;

            Map map = mapParent.Map;

            var fac = Find.FactionManager.FirstFactionOfDef(factionDef) ?? Faction.OfAncientsHostile;
            int count = countRange.RandomInRange;
            if (alwaysAtLeastOne && count == 0) count = 1;
            if (count <= 0) return;

            // Make a lord so they immediately behave like a small raid
            var lord = LordMaker.MakeNewLord(
                fac,
                new LordJob_MaraudColony(
                    fac,
                    canTimeoutOrFlee: false,
                    useAvoidGridSmart: true,
                    breachers: true,
                    canPickUpOpportunisticWeapons: true),
                map,
                null);

            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = Utils.TryFindSpawnCellNear(map, RimgateDefOf.Rimgate_Dwarfgate);
                if (!cell.IsValid) cell = CellFinder.RandomSpawnCellForPawnNear(map.Center, map);

                Pawn p = PawnGenerator.GeneratePawn(pawnKind, fac);
                GenSpawn.Spawn(p, cell, map, Rot4.Random);
                if (p.Faction != fac)
                    p.SetFaction(fac); // make sure faction is set

                lord.AddPawn(p);

                if (inventoryOptions == null) continue;

                foreach (PawnInventoryOption option in inventoryOptions)
                {
                    foreach (Thing item in option.GenerateThings())
                        p.inventory?.innerContainer?.TryAdd(item, canMergeWithExistingStacks: true);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_Defs.Look(ref factionDef, "factionDef");
            Scribe_Defs.Look(ref pawnKind, "pawnKind");
            Scribe_Collections.Look(ref inventoryOptions, "inventoryOptions", LookMode.Deep);
            Scribe_Values.Look(ref skipChance, "skipChance", 0f);
            Scribe_Values.Look(ref countRange, "countRange");
            Scribe_Values.Look(ref alwaysAtLeastOne, "alwaysAtLeastOne");
            Scribe_Values.Look(ref spawnNearGate, "spawnNearGate", true);
        }

    }
}
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Collections.Generic;
using Verse.Noise;
using System.Runtime.CompilerServices;

namespace Rimgate;

public class WorldObject_GateQuestSite : Site
{
    public int QuestId = -1;

    private bool _mapHidden;

    private Quest _cachedQuest;

    public override void SpawnSetup()
    {
        // register gate address for this site since gate is placed during mapgen
        GateUtil.AddGateAddress(Tile);
        base.SpawnSetup();
    }

    protected override void Tick()
    {
        base.Tick();

        var map = Map;
        if (map == null) return;

        bool allowPeek = GateUtil.ModificationEquipmentActive;
        bool hasBlockingPawns = map?.mapPawns?.AnyPawnBlockingMapRemoval ?? false;
        bool shouldBeHidden = !allowPeek && !hasBlockingPawns;

        // If no pawns, no active gate, and we’re still showing this map: hide + pop to world
        if (!_mapHidden && shouldBeHidden)
        {
            // Hide from colonist bar
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Hiding gate quest site map at tile {Tile} due to: "
                    + $"map hidden - {_mapHidden}, allow peek - {allowPeek}, nobody visible - {!hasBlockingPawns}");
            Find.ColonistBar.MarkColonistsDirty();
            _mapHidden = true;
        }

        if (_mapHidden && allowPeek)
        {
            // Reveal to colonist bar
            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Revealing gate quest site map at tile {Tile} due to: "
                    + $"map hidden - {_mapHidden}, allow peek - {allowPeek}");
            Find.ColonistBar.MarkColonistsDirty();
            _mapHidden = false;
        }

        // If the player is currently looking at this map, kick them out to the world
        if (_mapHidden && Find.CurrentMap == map)
            PopToWorldAndSelect();
    }

    private void PopToWorldAndSelect()
    {
        // Try to focus the world camera on this site for a smooth UX
        CameraJumper.TryHideWorld(); // no-op if already on world
        CameraJumper.TryJump(this);
        Find.World.renderer.wantedMode = WorldRenderMode.Planet;

        // Clear any map-specific selection that might linger
        Find.Selector.ClearSelection();
        // Make sure colonist bar updates immediately
        Find.ColonistBar.MarkColonistsDirty();
    }

    public void ToggleSiteMap()
    {
        if (Map == null || !_mapHidden) return;

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Revealing gate quest site map at tile {Tile} due to gate use");

        Find.ColonistBar.MarkColonistsDirty();
        _mapHidden = false;
    }

    public override void PostMapGenerate()
    {
        if(!Building_Gate.TryGetReceivingGate(Map, out _) && RimgateMod.Debug)
            Log.Warning($"Rimgate :: No receiving gate found on quest site map at tile {Tile} during PostMapGenerate");
        GateUtil.IncrementQuestSiteCount();
        base.PostMapGenerate();
    }

    public override void Notify_MyMapAboutToBeRemoved()
    {
        if (Building_Gate.TryGetSpawnedGateOnMap(Map, out Building_Gate gate))
            gate.CloseGate(gate.ConnectedGate != null);
        GateUtil.DecrementQuestSiteCount();
        base.Notify_MyMapAboutToBeRemoved();
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;

        // remove when there are blocking pawns, an active gate, or an ongoing quest
        var map = Map;
        if (map.mapPawns.AnyPawnBlockingMapRemoval)
            return false;

        foreach (PocketMapParent item in Find.World.pocketMaps.ToList())
        {
            if (item.sourceMap == map && item.Map.mapPawns.AnyPawnBlockingMapRemoval)
                return false;
        }

        if (ModsConfig.OdysseyActive && map.listerThings.AnyThingWithDef(ThingDefOf.GravAnchor))
            return false;

        if (!Building_Gate.TryGetSpawnedGateOnMap(map, out _))
        {
            Find.LetterStack.ReceiveLetter(
                "RG_QuestGateRemoved_Label".Translate(),
                "RG_QuestGateRemoved_Desc".Translate(),
                LetterDefOf.NegativeEvent);
            alsoRemoveWorldObject = true;
            return true;
        }

        if (TryResolveQuest(out Quest quest) && quest.State == QuestState.Ongoing)
            return false;

        alsoRemoveWorldObject = true;
        return true;
    }

    public override void Destroy()
    {
        GateUtil.RemoveGateAddress(Tile);
        base.Destroy();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        Map map = Map;
        bool allowPeak = GateUtil.ModificationEquipmentActive;
        bool hasBlockingPawns = map?.mapPawns?.AnyPawnBlockingMapRemoval ?? false;
        bool shouldBeHidden = !allowPeak && !hasBlockingPawns;
        foreach (var g in base.GetGizmos())
        {
            // lock "CommandShowMap" behind gate mod equipment or active gate
            if (g is Command_Action ca && HasMap)
            {
                var lbl = ca.defaultLabel ?? string.Empty;
                if (lbl == "CommandShowMap".Translate() && shouldBeHidden)
                    ca.Disable("RG_GateSiteHidden".Translate());
            }

            yield return g;
        }

        var abandon = new Command_Action
        {
            defaultLabel = "RG_AbandonQuestSite_Label".Translate(),
            defaultDesc = "RG_AbandonQuestSite_Desc".Translate(),
            icon = RimgateTex.AbandonGateSite,
            action = () =>
            {
                if (TryResolveQuest(out Quest quest) && quest.State == QuestState.Ongoing)
                {
                    quest.End(QuestEndOutcome.Fail, false, false);
                    if (HasMap)
                        Current.Game.DeinitAndRemoveMap(map, notifyPlayer: true);
                    Destroy();
                }
            }
        };

        if (hasBlockingPawns)
        {
            abandon.Disabled = true;
            abandon.disabledReason = "RG_AbandonQuestSite_Disabled".Translate();
        }

        yield return abandon;
    }

    private bool TryResolveQuest(out Quest quest)
    {
        quest = null;
        if (_cachedQuest != null)
        {
            quest = _cachedQuest;
            return true;
        }

        // stale id
        if (QuestId == -1)
        {
            _cachedQuest = null;
            return false;
        }

        _cachedQuest = Find.QuestManager.QuestsListForReading
            .FirstOrDefault(q =>
                q.id == QuestId
                && q.State == QuestState.Ongoing);

        if (_cachedQuest == null)
        {
            QuestId = -1;
            return false;
        }

        quest = _cachedQuest;
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref QuestId, "_questId");
        Scribe_Values.Look(ref _mapHidden, "_mapHidden", false);
    }
}

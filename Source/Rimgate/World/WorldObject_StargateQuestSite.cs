using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Collections.Generic;
using Verse.Noise;

namespace Rimgate;

public class WorldObject_StargateQuestSite : Site
{
    public int QuestId = -1;

    private bool _mapHidden;

    private Quest _questCached;

    public override void SpawnSetup()
    {
        base.SpawnSetup();
        Find.World.GetComponent<WorldComp_StargateAddresses>().AddAddress(Tile);
    }

    protected override void Tick()
    {
        base.Tick();

        if (Map == null) return;

        bool allowPeek = StargateUtility.ActiveGateOnMap(Map)
            || RimgateDefOf.Rimgate_WraithModificationEquipment.IsFinished;

        bool nobodyVisible = !Map.mapPawns.AnyPawnBlockingMapRemoval;

        // If no pawns, no active gate, and we’re still showing this map: hide + pop to world
        if (!_mapHidden && !allowPeek && nobodyVisible)
        {
            // Hide from colonist bar (your existing behavior)
            Find.ColonistBar.MarkColonistsDirty();
            _mapHidden = true;

            // If the player is currently looking at this map, kick them out to the world
            if (Find.CurrentMap == Map)
                PopToWorldAndSelect();
        }
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
        Find.ColonistBar.MarkColonistsDirty();
        _mapHidden = false;
    }

    public override void Notify_MyMapAboutToBeRemoved()
    {
        var gate = Building_Stargate.GetStargateOnMap(Map);
        if (gate != null)
            gate.GateControl.CleanupGate();

        base.Notify_MyMapAboutToBeRemoved();
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        // only removed when quest ends and no pawns
        var quest = ResolveQuest();
        if (quest != null && quest.State == QuestState.Ongoing) 
            return false;

        if (Map.mapPawns.AnyPawnBlockingMapRemoval)
            return false;

        foreach (PocketMapParent item in Find.World.pocketMaps.ToList())
        {
            if (item.sourceMap == base.Map && item.Map.mapPawns.AnyPawnBlockingMapRemoval)
            {
                return false;
            }
        }

        if (ModsConfig.OdysseyActive && base.Map.listerThings.AnyThingWithDef(ThingDefOf.GravAnchor))
        {
            return false;
        }

        alsoRemoveWorldObject = true;
        return true;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos())
        {
            // lock "CommandShowMap" behind research
            if (base.HasMap
                && !RimgateDefOf.Rimgate_WraithModificationEquipment.IsFinished)
            {
                if (g is Command_Action ca)
                {
                    var lbl = ca.defaultLabel ?? string.Empty;
                    if (lbl == "CommandShowMap".Translate())
                        continue;
                }
            }

            yield return g;
        }

        var abandon = new Command_Action
        {
            defaultLabel = "RG_AbandonQuestSiteLabel".Translate(),
            defaultDesc = "RG_AbandonQuestSiteDesc".Translate(),
            icon = RimgateTex.AbandonStargateSite,
            action = () =>
            {
                var quest = ResolveQuest();
                if (quest != null && quest.State == QuestState.Ongoing)
                {
                    quest.End(QuestEndOutcome.Fail, false, false);
                    if (HasMap)
                        Current.Game.DeinitAndRemoveMap(Map, notifyPlayer: true);
                    Destroy();
                }
            }
        };

        if (Map?.mapPawns?.AnyPawnBlockingMapRemoval == true)
        {
            abandon.Disabled = true;
            abandon.disabledReason = "Disabled: There are colonists on the map.";
        }

        yield return abandon;
    }

    private Quest ResolveQuest()
    {
        if (_questCached != null) return _questCached;
        if (QuestId == -1) return null;

        _questCached = Find.QuestManager.QuestsListForReading
            .FirstOrDefault(q =>
                q.id == QuestId
                && q.State == QuestState.Ongoing);
        if (_questCached == null) QuestId = -1; // stale id
        return _questCached;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref QuestId, "_questId");
        Scribe_Values.Look(ref _mapHidden, "_mapHidden", false);
    }
}

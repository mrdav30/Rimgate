using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Thing_MobileCartProxy : ThingWithComps, IThingHolder
{
    public ThingOwner InnerContainer;

    public ThingDef SavedDef; // original cart def

    public float FuelPerTick; // cached per-tick rate during push

    public float PushingFuel;  // current fuel while proxy is carried

    public ThingDef SavedStuff; // original stuff

    public int SavedHitPoints;

    public Color SavedDrawColor, SavedDrawColorTwo;

    public bool SavedHasPaint;

    public bool SavedUseContentsSetting;

    public IntVec3 PushDestination = IntVec3.Invalid;

    public override string Label => SavedDef?.LabelCap ?? base.Label;

    public override string LabelShort => SavedDef?.LabelCap ?? base.LabelShort;

    public override string LabelNoCount => SavedDef?.LabelCap ?? base.LabelNoCount;

    public override Graphic Graphic => RimgateTex.EmptyGraphic;

    public bool IsProxyRefuelable => FuelPerTick > 0f;

    public bool ProxyFuelOk => !IsProxyRefuelable || PushingFuel > 0f;

    public bool HasPushDestination => PushDestination.IsValid;

    private Graphic _cachedPushGhostGraphic;

    public Thing_MobileCartProxy()
    {
        InnerContainer = new ThingOwner<Thing>(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref InnerContainer, "InnerContainer", this);
        Scribe_Values.Look(ref FuelPerTick, "FuelPerTick", 0f);
        Scribe_Values.Look(ref PushingFuel, "PushingFuel", 0f);
        Scribe_Defs.Look(ref SavedDef, "SavedDef");
        Scribe_Defs.Look(ref SavedStuff, "SavedStuff");
        Scribe_Values.Look(ref SavedHitPoints, "SavedHitPoints", 0);
        Scribe_Values.Look(ref SavedDrawColor, "SavedDrawColor", default);
        Scribe_Values.Look(ref SavedDrawColorTwo, "SavedDrawColorTwo", default);
        Scribe_Values.Look(ref SavedHasPaint, "SavedHasPaint", false);
        Scribe_Values.Look(ref SavedUseContentsSetting, "SavedUseContentsSetting", false);
        Scribe_Values.Look(ref PushDestination, "PushDestination", IntVec3.Invalid);
    }

    public ThingOwner GetDirectlyHeldThings() => InnerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    // proxy -> Docked at a specific cell, using the original cart def when possible
    public Building_MobileContainer ConvertProxyToCart(Faction faction = null)
    {
        // Make cart with original stuff (if any), then spawn that instance
        var made = ThingMaker.MakeThing(SavedDef, SavedStuff) as Building_MobileContainer;

        if(faction != null)
            made.SetFaction(faction);

        made.HitPoints = Mathf.Clamp(SavedHitPoints, 1, made.MaxHitPoints);
        made.AllowColonistsUseContents = SavedUseContentsSetting;

        // restore fuel (if any)
        if (made.Refuelable != null)
        {
            // CompRefuelable starts at 0 on fresh Thing; add whatever is left from pushing
            if (PushingFuel > 0f)
                made.Refuelable.Refuel(PushingFuel);
        }

        var paint = made.TryGetComp<CompColorable>();
        if (paint != null && SavedHasPaint)
            paint.SetColor(SavedDrawColor);

        return made;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveContentsToContainer(Building_MobileContainer container)
    {
        if (container == null) return;

        InnerContainer.TryTransferAllToContainer(container.InnerContainer);
    }

    public Graphic GetPushGhostGraphic()
    {
        if (SavedDef?.graphicData == null)
            return null;

        if (_cachedPushGhostGraphic != null)
            return _cachedPushGhostGraphic;

        Graphic baseGraphic = SavedDef.graphicData.Graphic;
        if (baseGraphic == null)
            return null;

        _cachedPushGhostGraphic = baseGraphic.GetColoredVersion(baseGraphic.Shader, SavedDrawColor, SavedDrawColorTwo);
        return _cachedPushGhostGraphic;
    }
}

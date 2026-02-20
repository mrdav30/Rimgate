using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimgate;

public class Dialog_LoadContainers : Window
{
    private enum Tab
    {
        Items
    }

    private Map _map;

    private Comp_MobileContainerControl _container;

    private List<TransferableOneWay> _transferables;

    private TransferableOneWayWidget _itemsTransfer;

    private Tab _tab;

    private float _lastMassFlashTime = -9999f;

    private bool _massUsageDirty = true;

    private float _cachedMassUsage;

    private bool _visibilityDirty = true;

    private float _cachedVisibility;

    private string cachedVisibilityExplanation;

    private const float TitleRectHeight = 35f;

    private const float BottomAreaHeight = 55f;

    private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

    private static List<TabRecord> tabsList = new();

    public bool CanChangeAssignedThingsAfterStarting => _container.Props.canChangeAssignedThingsAfterStarting;

    public bool LoadingInProgress => _container.LoadingInProgress;

    public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

    protected override float Margin => 0f;

    private float MassCapacity => _container.MassCapacity;

    private string ContainersLabel => _container.parent.Label;

    private string ContainersLabelCap => ContainersLabel.CapitalizeFirst();

    private BiomeDef Biome => _map.Biome;

    private float MassUsage
    {
        get
        {
            if (_massUsageDirty)
            {
                _massUsageDirty = false;
                // pending-to-add…
                float pending = CollectionsMassCalculator.MassUsageTransferables(
                _transferables,
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                includePawnsMass: true);
                // …plus what’s already in the cart
                _cachedMassUsage = _container.MassUsage + pending;
            }

            return _cachedMassUsage;
        }
    }

    private float Visibility
    {
        get
        {
            if (_visibilityDirty)
            {
                _visibilityDirty = false;
                StringBuilder stringBuilder = new StringBuilder();
                _cachedVisibility = CaravanVisibilityCalculator.Visibility(_transferables, stringBuilder);
                cachedVisibilityExplanation = stringBuilder.ToString();
            }

            return _cachedVisibility;
        }
    }

    public Dialog_LoadContainers(Map map, Comp_MobileContainerControl container)
    {
        _map = map;
        _container = container;
        forcePause = true;
        absorbInputAroundWindow = true;
    }

    public override void PostOpen()
    {
        base.PostOpen();
        CalculateAndRecacheTransferables();
        SetLoadedItemsToLoad();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect rect = new Rect(0f, 0f, inRect.width, 35f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, "LoadTransporters".Translate(ContainersLabel).CapitalizeFirst());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        if (_container.Map.Tile.Valid)
        {
            TaggedString taggedString = $"{MassUsage.ToStringEnsureThreshold(MassCapacity, 0)} / {MassCapacity:F0}" + "kg".Translate();

            Rect infoRect = new Rect(12f, 35f, inRect.width - 24f, 40f);
            if (infoRect.width > 230f)
            {
                infoRect.x += Mathf.Floor((infoRect.width - 230f) / 2f);
                infoRect.width = 230f;
            }

            Widgets.BeginGroup(infoRect);

            Rect infoRect2 = new Rect(0f, 0f, infoRect.width, infoRect.height);
            Rect infoRect3 = new Rect(0f, 0f, infoRect.width, infoRect.height / 2f);
            Rect infoRect4 = new Rect(0, infoRect.height / 2f, infoRect.width, infoRect.height / 2f);

            if (Time.time - _lastMassFlashTime < 1f)
                GUI.DrawTexture(infoRect2, TransferableUIUtility.FlashTex);

            Text.Anchor = TextAnchor.LowerCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(infoRect3.x, infoRect3.y - 2f, infoRect3.width, infoRect3.height - -3f), "Mass".Translate());
            Rect infoRect5 = new Rect(infoRect4.x, infoRect4.y + -3f + 2f, infoRect4.width, infoRect4.height - -3f);
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = GetMassColor(MassUsage, MassCapacity);
            Widgets.Label(infoRect5, taggedString);

            GUI.color = Color.white;
            Widgets.DrawHighlightIfMouseover(infoRect2);
            TooltipHandler.TipRegion(infoRect2, GetMassTip(MassUsage, MassCapacity));

            Widgets.EndGroup();
            Text.Anchor = TextAnchor.UpperLeft;
            inRect.yMin += 52f;
        }

        tabsList.Clear();
        tabsList.Add(new TabRecord(
        "ItemsTab".Translate(),
        delegate
        {
            _tab = Tab.Items;
        },
        _tab == Tab.Items));
        inRect.yMin += 67f;
        Widgets.DrawMenuSection(inRect);
        TabDrawer.DrawTabs(inRect, tabsList);
        inRect = inRect.ContractedBy(17f);
        inRect.height += 17f;
        Widgets.BeginGroup(inRect);
        Rect rect2 = inRect.AtZero();
        DoBottomButtons(rect2);
        Rect inRect2 = rect2;
        inRect2.yMax -= 76f;
        bool anythingChanged = false;
        switch (_tab)
        {
            case Tab.Items:
                _itemsTransfer.OnGUI(inRect2, out anythingChanged);
                break;
        }
        if (anythingChanged)
            CountToTransferChanged();
        Widgets.EndGroup();
    }

    private static Color GetMassColor(float massUsage, float massCapacity)
    {
        if (massCapacity == 0f)
            return Color.white;

        if (massUsage > massCapacity)
            return Color.red;

        return Color.white;
    }

    private static string GetMassTip(float massUsage, float massCapacity)
    {
        return "MassCarriedSimple".Translate()
            + ": " + massUsage.ToStringEnsureThreshold(massCapacity, 2)
            + " " + "kg".Translate()
            + "\n" + "MassCapacity".Translate()
            + ": " + massCapacity.ToString("F2")
            + " " + "kg".Translate();
    }

    public override bool CausesMessageBackground() => true;

    private void AddToTransferables(Thing t)
    {
        TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, _transferables, TransferAsOneMode.PodsOrCaravanPacking);
        if (transferableOneWay == null)
        {
            transferableOneWay = new TransferableOneWay();
            _transferables.Add(transferableOneWay);
        }

        if (transferableOneWay.things.Contains(t))
            Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
        else
            transferableOneWay.things.Add(t);
    }

    private void DoBottomButtons(Rect rect)
    {
        if (Widgets.ButtonText(new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate()))
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            CalculateAndRecacheTransferables();
        }

        if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate()))
        {
            Close();
        }

        Rect rect2 = new Rect(rect.width - BottomButtonSize.x, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y);
        if (Widgets.ButtonText(rect2, "AcceptButton".Translate()))
            OnAcceptButton();

        if (Prefs.DevMode)
        {
            float width = 200f;
            float height = BottomButtonSize.y / 2f;
            if (!LoadingInProgress && Widgets.ButtonText(new Rect(0f, rect2.yMax + 4f, width, height), "DEV: Load instantly") && DebugTryLoadInstantly())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(doCloseSound: false);
            }

            if (Widgets.ButtonText(new Rect(0f, rect2.yMax + 4f, width, height), "DEV: Select everything"))
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                SetToLoadEverything();
            }
        }
    }

    private void OnAcceptButton()
    {
        if (TryAccept())
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            Close(doCloseSound: false);
        }
    }

    private void CalculateAndRecacheTransferables()
    {
        _transferables ??= new List<TransferableOneWay>();
        _transferables.Clear();
        foreach (Thing item in MobileContainerUtility.AllSendableItems(_container, _map))
            AddToTransferables(item);

        // Always show what's already in the cart (read-only presence in the list)
        for (int j = 0; j < _container.InnerContainer.Count; j++)
            AddToTransferables(_container.InnerContainer[j]);

        // Also show stacks already assigned to be hauled in (if you like seeing them)
        if (CanChangeAssignedThingsAfterStarting && LoadingInProgress)
            foreach (Thing item in MobileContainerUtility.ThingsBeingHauledTo(_container, _map))
                AddToTransferables(item);

        _itemsTransfer = new TransferableOneWayWidget(
            _transferables.Where((TransferableOneWay x) =>
                x.ThingDef.category != ThingCategory.Pawn),
            null,
            null,
            "TransporterColonyThingCountTip".Translate(),
            drawMass: true,
            IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
            includePawnsMassInMassUsage: true, () => MassCapacity - MassUsage,
            0f,
            ignoreSpawnedCorpseGearAndInventoryMass: false,
            _map.Tile,
            drawMarketValue: true,
            drawEquippedWeapon: false,
            drawNutritionEatenPerDay: false,
            drawMechEnergy: false,
            drawItemNutrition: true,
            drawForagedFoodPerDay: false,
            drawDaysUntilRot: true);
        CountToTransferChanged();
    }

    private bool DebugTryLoadInstantly()
    {
        int i;
        for (i = 0; i < _transferables.Count; i++)
        {
            TransferableUtility.Transfer(
                _transferables[i].things,
                _transferables[i].CountToTransfer,
                delegate (Thing splitPiece, IThingHolder originalThing)
                {
                    _container.GetDirectlyHeldThings().TryAdd(splitPiece);
                });
        }

        return true;
    }

    private bool TryAccept()
    {
        List<Pawn> pawnsFromContainers = TransferableUtility.GetPawnsFromTransferables(_transferables);
        if (!HasErrors(pawnsFromContainers))
            return false;

        if (LoadingInProgress)
        {
            AssignTransferablesToContainer();
            IReadOnlyList<Pawn> allPawnsSpawned = _map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                Pawn current = allPawnsSpawned[i];
                bool isMyJob = current.CurJobDef == RimgateDefOf.Rimgate_HaulToContainer
                    && current.jobs.curDriver is JobDriver_HaulToMobileContainer jd
                    && jd.Mobile.parent.ThingID == _container.parent.ThingID;
                if (isMyJob)
                    allPawnsSpawned[i].jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
        else
        {
            AssignTransferablesToContainer();
            if (_container.LeftToLoad.Count > 0)
                Messages.Message(
                "RG_MessageContainerLoadingProcessStarted".Translate(),
                _container.parent,
                MessageTypeDefOf.TaskCompletion,
                historical: false);
        }

        return true;
    }

    private void SetLoadedItemsToLoad()
    {
        // 1) Seed counts with what's already in the cart
        for (int j = 0; j < _container.InnerContainer.Count; j++)
        {
            Thing inCart = _container.InnerContainer[j];
            var tow = TransferableUtility.TransferableMatchingDesperate(
            inCart, _transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (tow != null)
            {
                int add = inCart.stackCount;
                if (tow.CanAdjustBy(add).Accepted) tow.AdjustBy(add);
            }
        }

        // 2) If there is a pending manifest from earlier (not finished), add that too
        if (_container.LeftToLoad == null || _container.LeftToLoad.Count == 0) return;

        for (int k = 0; k < _container.LeftToLoad.Count; k++)
        {
            var left = _container.LeftToLoad[k];
            if (left.CountToTransfer <= 0 || !left.HasAnyThing) continue;
            var tow = TransferableUtility.TransferableMatchingDesperate(
            left.AnyThing, _transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (tow != null && tow.CanAdjustBy(left.CountToTransfer).Accepted)
                tow.AdjustBy(left.CountToTransfer);
        }
    }

    private void AssignTransferablesToContainer()
    {
        // 1) Rebuild LeftToLoad from the UI selections
        _container.LeftToLoad ??= new List<TransferableOneWay>();
        _container.LeftToLoad.Clear();

        for (int i = 0; i < _transferables.Count; i++)
        {
            var tr = _transferables[i];
            if (tr.CountToTransfer <= 0 || !tr.HasAnyThing) continue;

            // Make a shallow clone TransferableOneWay: same thing refs, new desired count
            var clone = new TransferableOneWay();
            clone.things.AddRange(tr.things);
            clone.AdjustTo(tr.CountToTransfer);
            _container.LeftToLoad.Add(clone);
        }

        for (int i = _container.InnerContainer.Count - 1; i >= 0; i--)
        {
            // 2) Subtract what's already in the container
            Thing inCart = _container.InnerContainer[i];
            int used = _container.SubtractFromToLoadList(inCart, inCart.stackCount, sendMessageOnFinished: false);

            // 3) If the UI reduced requested counts below what's already in-cart, optionally drop the excess
            if (used < inCart.stackCount)
            {
                int overflow = inCart.stackCount - used;
                _container.Notify_ItemRemoved(inCart);
                if (!_container.InnerContainer.TryDrop(
                    inCart,
                    _container.parent.Position,
                    _container.parent.Map,
                    ThingPlaceMode.Near,
                    overflow,
                    out _)) Log.Error($"Container failed to drop {overflow} of {inCart}");
            }
        }
    }

    private bool HasErrors(List<Pawn> pawns)
    {
        if (MassUsage > MassCapacity)
        {
            FlashMass();
            Messages.Message("TooBigTransporterSingleMassUsage".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        Pawn pawn = pawns.Find((Pawn x) =>
            !x.MapHeld.reachability.CanReach(
                x.PositionHeld,
                _container.parent,
                PathEndMode.Touch,
                TraverseParms.For(TraverseMode.PassDoors))
            && !_container.InnerContainer.Contains(x));
        if (pawn != null)
        {
            Messages.Message(
                "PawnCantReachTransporterSingle".Translate(pawn.LabelShort, pawn).CapitalizeFirst(),
                MessageTypeDefOf.RejectInput,
                historical: false);
            return false;
        }

        float r2 = _container.Props.loadRadius * _container.Props.loadRadius;
        IntVec3 center = _container.parent.Position;

        for (int i = 0; i < _transferables.Count; i++)
        {
            if (_transferables[i].ThingDef.category != ThingCategory.Item) continue;
            int countToTransfer = _transferables[i].CountToTransfer;
            if (countToTransfer <= 0) continue;

            int num = 0;
            for (int j = 0; j < _transferables[i].things.Count; j++)
            {
                Thing t = _transferables[i].things[j];
                var carry = t.ParentHolder as Pawn_CarryTracker;

                bool withinRadius = t.PositionHeld.DistanceToSquared(center) <= r2;
                bool reachable = _map.reachability.CanReach(
                        t.PositionHeld,
                        _container.parent,
                        PathEndMode.Touch,
                        TraverseParms.For(TraverseMode.PassDoors))
                    || _container.InnerContainer.Contains(t)
                    || (carry != null
                        && carry.pawn.MapHeld.reachability.CanReach(
                            carry.pawn.PositionHeld,
                            _container.parent,
                            PathEndMode.Touch,
                            TraverseParms.For(TraverseMode.PassDoors)));

                if (withinRadius && reachable)
                {
                    num += t.stackCount;
                    if (num >= countToTransfer) break;
                }
            }

            if (num < countToTransfer)
            {
                Messages.Message(
                    countToTransfer == 1
                        ? "RG_CartItemIsUnreachableSingle".Translate(_transferables[i].ThingDef.label)
                        : "RG_CartItemIsUnreachableMulti".Translate(countToTransfer, _transferables[i].ThingDef.label),
                    MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
        }

        return true;
    }

    private void FlashMass() => _lastMassFlashTime = Time.time;

    private void SetToLoadEverything()
    {
        for (int i = 0; i < _transferables.Count; i++)
            _transferables[i].AdjustTo(_transferables[i].GetMaximumToTransfer());

        CountToTransferChanged();
    }

    private void CountToTransferChanged()
    {
        _massUsageDirty = true;
        _visibilityDirty = true;
    }

    public override void OnAcceptKeyPressed() => OnAcceptButton();
}
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Verse;
using static HarmonyLib.Code;

namespace Rimgate;

public class Building_MobileContainer : Building
{
    public Comp_MobileContainerControl Control => _cachedMobile ??= GetComp<Comp_MobileContainerControl>();

    private Comp_MobileContainerControl _cachedMobile;

    public override void DrawExtraSelectionOverlays()
    {
        if (!Spawned || Control == null) return;
        GenDraw.DrawRadiusRing(Position, Control.Props.loadRadius);
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(base.GetInspectString());

        if (stringBuilder.Length > 0)
            stringBuilder.AppendLine();

        stringBuilder.Append("RG_LoadOutRange".Translate(Control.Props.loadRadius));

        return stringBuilder.ToString();
    }

    // Docked -> proxy returns an unspawned proxy Thing ready to spawn
    public Thing_MobileCartProxy ConvertCartToProxy(Pawn p)
    {
        if (Control == null) return null;

        // Create proxy + move contents
        var proxy = ThingMaker.MakeThing(RimgateDefOf.Rimgate_MobileCartProxy) as Thing_MobileCartProxy;
        var pcomp = proxy.Control;
        if (pcomp == null)
        {
            Log.Error("Rimgate :: Proxy missing Comp_MobileContainer.");
            return null;
        }

        // cache stuff, hp, paint & draw colors before we despawn
        pcomp.SaveVisualFrom(this);
        var cartContainer = Control?.InnerContainer;
        cartContainer?.TryTransferAllToContainer(pcomp.InnerContainer);

        var refuelable = Control?.Refuelable;
        if (refuelable != null)
        {
            // copy current fuel
            pcomp.PushingFuel = refuelable.Fuel;
            // TODO: lerp with more weight
            pcomp.FuelPerTick = Control?.ConsumptionRatePerTick ?? 0;
        }
        else
        {
            pcomp.PushingFuel = 0f;
            pcomp.FuelPerTick = 0f;
        }

        // Despawn the cart (contents already moved)
        DeSpawn();
        pcomp.Attach(p);

        return proxy;
    }
}

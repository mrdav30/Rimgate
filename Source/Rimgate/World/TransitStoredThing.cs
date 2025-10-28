using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Verse;

public class TransitStoredThing : IExposable
{
    public ThingDef Def;

    public int Count = 1;

    public ThingDef Stuff;

    public QualityCategory? Quality;

    public int? HP;

    public IntVec3? Pos;

    public Rot4? Rot;

    public bool WasMinified;

    public List<TransitStoredThing> Contents;

    public float? Fuel;

    public bool? AutoRefuel;

    public float? BatteryEnergy;

    public TransitStoredThing() { }
    public TransitStoredThing(Thing t)
    {
        if (t is MinifiedThing mt)
        {
            var inner = mt.InnerThing;
            WasMinified = true;
            Def = inner.def;
            Stuff = inner.Stuff;
            Count = 1;
            if (inner.TryGetQuality(out var q)) Quality = q;
            // position is the minified shell’s cell
            if (t.Spawned) Pos = t.Position;
            Rot = inner.Rotation;
            if (inner.def.useHitPoints) HP = inner.HitPoints;

            // If the inner building is itself a container, capture its contents
            TryCaptureContents(inner);

            var ci = inner.TryGetComp<CompRefuelable>();
            if (ci != null)
            {
                Fuel = ci.Fuel;
                AutoRefuel = ci.allowAutoRefuel;
            }

            var bi = inner.TryGetComp<CompPowerBattery>();
            if (bi != null)
                BatteryEnergy = bi.StoredEnergy;

            return;
        }

        // Normal item or non-minified building
        // (may still be a container)
        Def = t.def;
        Count = t.stackCount;
        Stuff = t.Stuff;
        if (t.TryGetQuality(out var q2)) Quality = q2;
        if (t.Spawned) Pos = t.Position;
        if (t.Rotation != default) Rot = t.Rotation;
        if (t.def.useHitPoints) HP = t.HitPoints;

        TryCaptureContents(t);

        var c = t.TryGetComp<CompRefuelable>();
        if (c != null)
        {
            Fuel = c.Fuel;
            AutoRefuel = c.allowAutoRefuel;
        }

        var b = t.TryGetComp<CompPowerBattery>();
        if (b != null)
            BatteryEnergy = b.StoredEnergy;
    }

    private void TryCaptureContents(Thing t)
    {
        if (t is not IThingHolder holder) return;
        Contents = new List<TransitStoredThing>();
        CaptureHolderRecursive(holder, Contents);
    }

    private static void CaptureHolderRecursive(IThingHolder holder, List<TransitStoredThing> into)
    {
        // Directly held things
        ThingOwner directlyHeld = holder.GetDirectlyHeldThings();
        if (directlyHeld != null)
        {
            for (int i = 0; i < directlyHeld.Count; i++)
            {
                var child = directlyHeld[i];
                if (child is Pawn) continue;
                into.Add(new TransitStoredThing(child));
            }
        }
        // Child holders
        var tmp = ListPool<IThingHolder>.Get();
        holder.GetChildHolders(tmp);
        for (int i = 0; i < tmp.Count; i++)
            CaptureHolderRecursive(tmp[i], into);
        ListPool<IThingHolder>.Release(tmp);
    }

    public static Thing MakeThingFromRecord(TransitStoredThing rec)
    {
        Thing thing;

        if (rec.WasMinified) // wrap inner into a minified shell
        {
            var min = ThingMaker.MakeThing(ThingDefOf.MinifiedThing);
            var inner = ThingMaker.MakeThing(rec.Def, rec.Stuff);

            if (inner == null || min == null) return null;

            if (rec.Quality.HasValue)
                inner.TryGetComp<CompQuality>()?.SetQuality(rec.Quality.Value, ArtGenerationContext.Outsider);

            inner.HitPoints = Mathf.Clamp(rec.HP ?? inner.MaxHitPoints, 1, inner.MaxHitPoints);
            inner.stackCount = 1;
            if (rec.Rot.HasValue) inner.Rotation = rec.Rot.Value;

            ((MinifiedThing)min).InnerThing = inner;
            thing = min;

            // If the inner is a container, rebuild its contents now
            if (rec.Contents != null && rec.Contents.Count > 0 && inner is IThingHolder ihInner)
                RestoreContentsIntoHolder(ihInner, rec.Contents);

            RestoreFuel(rec, inner);
            RestoreEnergy(rec, inner);
        }
        else
        {
            thing = ThingMaker.MakeThing(rec.Def, rec.Stuff);
            if (thing == null) return null;

            if (rec.Quality.HasValue)
                thing.TryGetComp<CompQuality>()?.SetQuality(rec.Quality.Value, ArtGenerationContext.Outsider);

            if (rec.Def.useHitPoints)
                thing.HitPoints = Mathf.Clamp(rec.HP ?? thing.MaxHitPoints, 1, thing.MaxHitPoints);

            thing.stackCount = Mathf.Clamp(rec.Count, 1, thing.def.stackLimit);

            // If this non-minified thing is a container, rebuild its contents
            if (rec.Contents != null && rec.Contents.Count > 0 && thing is IThingHolder ih)
                RestoreContentsIntoHolder(ih, rec.Contents);

            RestoreFuel(rec, thing);
            RestoreEnergy(rec, thing);
        }

        return thing;
    }

    public static void RestoreContentsIntoHolder(IThingHolder holder, List<TransitStoredThing> records)
    {
        if (records == null || records.Count == 0) return;

        // Prefer the primary owner if present
        // (e.g., CompTransporter.innerContainer)
        var owner = holder.GetDirectlyHeldThings();
        for (int i = 0; i < records.Count; i++)
        {
            var child = MakeThingFromRecord(records[i]);
            if (child == null) continue;

            if (owner != null) owner.TryAdd(child);
            else
            {
                // As a fallback, try child holders (rare)
                var tmp = ListPool<IThingHolder>.Get();
                holder.GetChildHolders(tmp);
                if (tmp.Count > 0)
                {
                    var o2 = tmp[0].GetDirectlyHeldThings();
                    o2?.TryAdd(child);
                }
                ListPool<IThingHolder>.Release(tmp);
            }
        }
    }

    public static void RestoreFuel(TransitStoredThing rec, Thing t)
    {
        if (!rec.Fuel.HasValue) return;

        var cr = t.TryGetComp<CompRefuelable>();
        if (cr == null) return;

        float target = Mathf.Clamp(rec.Fuel.Value, 0f, cr.Props.fuelCapacity);
        // Ensure we end at the exact value (Refuel adds to current)
        if (cr.Fuel > 0f) cr.ConsumeFuel(cr.Fuel); // -> 0
        if (target > 0f) cr.Refuel(target); // -> exact target

        if (rec.AutoRefuel.HasValue)
            cr.allowAutoRefuel = rec.AutoRefuel.Value;
    }


    public static void RestoreEnergy(TransitStoredThing rec, Thing t)
    {
        if (!rec.BatteryEnergy.HasValue) return;

        var bat = t.TryGetComp<CompPowerBattery>();
        if (bat == null) return;

        float target = Mathf.Clamp(rec.BatteryEnergy.Value, 0f, bat.Props.storedEnergyMax);

        if (bat.StoredEnergy > 0f) bat.DrawPower(bat.StoredEnergy);
        if (target > 0f) bat.AddEnergy(target);
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref Def, "def");
        Scribe_Values.Look(ref Count, "cnt", 1);
        Scribe_Defs.Look(ref Stuff, "stuff");
        Scribe_Values.Look(ref Quality, "qlt");
        Scribe_Values.Look(ref Pos, "pos");
        Scribe_Values.Look(ref Rot, "rot");
        Scribe_Values.Look(ref HP, "hp");
        Scribe_Values.Look(ref WasMinified, "wasMin");
        Scribe_Collections.Look(ref Contents, "contents", LookMode.Deep);
        Scribe_Values.Look(ref Fuel, "fuel");
        Scribe_Values.Look(ref AutoRefuel, "autoRefuel");
        Scribe_Values.Look(ref BatteryEnergy, "batteryEnergy");
    }
}

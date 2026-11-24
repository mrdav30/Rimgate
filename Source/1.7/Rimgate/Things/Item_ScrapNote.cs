using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate
{
    // Mod extension: defines which keyed notes this item can pull from
    public class Item_ScrapNote_Ext : DefModExtension
    {
        public List<string> keys;
    }

    public class Item_ScrapNote : ThingWithComps
    {
        public Item_ScrapNote_Ext Props => _cachedProps ??= def.GetModExtension<Item_ScrapNote_Ext>();

        private Item_ScrapNote_Ext _cachedProps;

        private int _keyIndex = -1;

        private List<string> KeyPool
        {
            get
            {
                var keys = Props?.keys;
                return (keys != null && keys.Count > 0)
                    ? keys
                    : new List<string>(); // Empty list means no usable notes
            }
        }

        private string CurrentKeyOrNull
        {
            get
            {
                var pool = KeyPool;
                if (pool.Count == 0) return null;

                // Clamp index in case defs changed since save
                if (_keyIndex < 0 || _keyIndex >= pool.Count)
                    _keyIndex = 0;

                return pool[_keyIndex];
            }
        }

        public override string DescriptionFlavor
        {
            get
            {
                var key = CurrentKeyOrNull;
                if (key.NullOrEmpty())
                    return "RG_ScribbledNote_Default".Translate();
                return key.Translate();
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            EnsureKeySelected();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            EnsureKeySelected();
        }

        private void EnsureKeySelected()
        {
            if (_keyIndex >= 0) return;

            var pool = KeyPool;
            if (pool.Count > 0)
            {
                _keyIndex = Rand.Range(0, pool.Count);
            }
            else
            {
                _keyIndex = -1; // No valid key
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _keyIndex, "_keyIndex", -1);

            // Clamp after load
            var pool = KeyPool;
            if (pool.Count == 0)
            {
                _keyIndex = -1;
            }
            else if (_keyIndex < 0 || _keyIndex >= pool.Count)
            {
                _keyIndex = Mathf.Clamp(_keyIndex, 0, pool.Count - 1);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace OnTheTrainDemoMod
{
    internal static class Items
    {
        private static object _localInv;
        private static Type _collectableType;
        private static Type _playerInvType;
        private static Type _gameSettingsType;
        private static readonly Dictionary<string, object> ItemCache = new Dictionary<string, object>();

        public struct ItemEntry
        {
            public string ItemName;
            public string DisplayName;
            public int StackLimit;
        }

        private const BindingFlags InstanceFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static object LocalInventory
        {
            get
            {
                if (_localInv is UnityEngine.Object u && u == null) _localInv = null;
                _localInv = _localInv ?? FindLocalInventory();
                return _localInv;
            }
        }

        public static void Give(string nameKey, int amount)
        {
            if (string.IsNullOrEmpty(nameKey) || amount <= 0) return;
            _playerInvType = _playerInvType ?? ReflectionUtil.FindType("PlayerInventory");
            var inv = LocalInventory;
            if (inv == null || _playerInvType == null)
            {
                MelonLogger.Warning("[Give] Local PlayerInventory not found yet (enter a game/scene first).");
                return;
            }
            var data = FindItemData(nameKey);
            if (data == null)
            {
                MelonLogger.Warning("[Give] Item not found: '" + nameKey + "'. Use 'List Item Names' to see valid names.");
                return;
            }
            var m = _playerInvType.GetMethod("AddItemInventory",
                new[] { _collectableType, typeof(int), typeof(float), typeof(int) });
            if (m == null)
            {
                MelonLogger.Warning("[Give] AddItemInventory signature not found.");
                return;
            }
            try
            {
                m.Invoke(inv, new object[] { data, amount, -1f, -1 });
                MelonLogger.Msg("[Give] +" + amount + " " + nameKey);
            }
            catch (System.Exception e)
            {
                MelonLogger.Warning("[Give] invoke failed: " + e.InnerException?.Message ?? e.Message);
            }
        }

        public static void GiveStack(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return;
            var data = FindItemData(nameKey);
            if (data == null)
            {
                MelonLogger.Warning("[GiveStack] Item not found: '" + nameKey + "'.");
                return;
            }
            int stack = GetItemStackLimit(data);
            Give(nameKey, stack);
        }

        public static int GetItemStackLimit(object itemData)
        {
            if (itemData == null) return 1;
            try
            {
                int multiplier = GetItemSizeMultiplier(itemData);
                if (multiplier <= 0) multiplier = 1;
                int slotSize = GetInventorySlotSize();
                if (slotSize <= 0) slotSize = 32;
                int limit = slotSize / multiplier;
                return Math.Max(1, limit);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Items] GetItemStackLimit failed: " + e.Message);
                return 32;
            }
        }

        private static bool _itemsLoggedOnce;

        public static List<ItemEntry> GetAllItems(bool forceRefresh = false)
        {
            _collectableType = _collectableType ?? ReflectionUtil.FindType("CollectableItemData");
            if (_collectableType == null) return new List<ItemEntry>();

            var all = Resources.FindObjectsOfTypeAll(_collectableType);
            var list = new List<ItemEntry>(all.Length);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var debugNames = new List<string>();

            foreach (var o in all)
            {
                if (o == null) continue;
                var itemName = GetMemberValue(o, "itemName") as string;
                if (string.IsNullOrEmpty(itemName))
                    itemName = (o as UnityEngine.Object)?.name;
                if (string.IsNullOrEmpty(itemName)) continue;
                if (seen.Add(itemName) == false) continue;

                var display = I18n.GetIgnoreCase("item." + itemName);
                if (string.IsNullOrEmpty(display))
                    display = GetMemberValue(o, "itemDisplayName") as string;
                if (string.IsNullOrEmpty(display)) display = itemName;

                list.Add(new ItemEntry
                {
                    ItemName = itemName,
                    DisplayName = display,
                    StackLimit = GetItemStackLimit(o)
                });
                debugNames.Add(itemName);
            }

            if (!_itemsLoggedOnce && debugNames.Count > 0)
            {
                _itemsLoggedOnce = true;
                debugNames.Sort();
                MelonLogger.Msg("[Items] GetAllItems found " + debugNames.Count + " items:");
                foreach (var n in debugNames)
                    MelonLogger.Msg("   " + n);
            }

            list.Sort((a, b) =>
            {
                int ta = GetItemTier(a.ItemName);
                int tb = GetItemTier(b.ItemName);
                if (ta != tb) return ta.CompareTo(tb);
                return string.CompareOrdinal(a.ItemName, b.ItemName);
            });
            return list;
        }

        private static readonly Dictionary<string, int> ItemTierTable =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Wood", 10 }, { "Stone", 10 }, { "Coal", 10 },
                { "Iron Ore", 10 }, { "Copper", 10 }, { "Gold", 10 }, { "Sulfur Ore", 10 },
                { "Clay", 10 }, { "Dirt", 10 }, { "Sand", 10 }, { "Gravel", 10 },
                { "Flint", 10 }, { "Charcoal", 10 }, { "Resin", 10 }, { "Salt", 10 },
                { "Plant Fiber", 10 }, { "Hemp", 10 }, { "Hemp Seed", 10 },
                { "Mushroom", 10 },
                { "Beet", 10 }, { "Carrot", 10 }, { "Corn", 10 }, { "Corn Seed", 10 },
                { "Onion", 10 }, { "Potato", 10 },
                { "Blackberry", 10 }, { "Blackberry Seed", 10 },
                { "Blueberry", 10 }, { "Blueberry Seed", 10 },
                { "Raspberry", 10 }, { "Raspberry Seed", 10 },
                { "Strawberry", 10 }, { "Strawberry Seed", 10 },
                { "Animal Fat", 10 }, { "Animal Horn", 10 }, { "Horn Powder", 10 },
                { "Meat", 10 }, { "Duck Meat", 10 }, { "Zombie Flesh", 10 },
                { "Rotten Organs", 10 }, { "Water", 10 },
                { "Iron Dust", 20 }, { "Iron Ingot", 20 }, { "Iron Plate", 20 }, { "Iron Rod", 20 },
                { "Copper Dust", 20 }, { "Copper Ingot", 20 }, { "Copper Wire", 20 },
                { "Gold Dust", 20 }, { "Sulfur Dust", 20 }, { "Refined Sulfur", 20 },
                { "Steel Ingot", 20 },
                { "Brick", 20 }, { "Glass", 20 }, { "Colored Glass", 20 }, { "Glass Bottle", 20 },
                { "Cloth", 20 }, { "Leather", 20 }, { "Plastic", 20 },
                { "Nail", 20 }, { "Bolt", 20 }, { "Screw", 20 },
                { "Rope", 20 }, { "Chain", 20 }, { "Hinge", 20 }, { "Hook", 20 },
                { "Gear", 20 }, { "Spring", 20 }, { "Fuse", 20 },
                { "Mechanical Parts", 20 }, { "Chemical Extract", 20 }, { "Gun Powder", 20 },
                { "Paper", 20 }, { "Metal Scrap", 20 }, { "Metal Pipe", 20 },
                { "Plastic Pipe", 20 }, { "Bowl", 20 }, { "Bucket", 20 }, { "Pot", 20 },
                { "Clean Water Bottle", 30 }, { "Dirty Water Bottle", 30 }, { "Water Bottle", 30 },
                { "Canned Food", 30 }, { "Nutrition Syrup", 30 },
                { "Cooked Meat", 30 }, { "Cooked Mushroom", 30 },
                { "Bandage", 40 }, { "Health Syringe", 40 }, { "Stamina Syringe", 40 },
                { "Not Hungry Pill", 40 }, { "Recovery Pills", 40 }, { "Regenaration Pill", 40 },
                { "Pills Blue", 40 }, { "Pills Green", 40 }, { "Pills Orange", 40 },
                { "Syringe Blue", 40 }, { "Syringe Green", 40 }, { "Syringe Red", 40 },
                { "Tonic Blue", 40 }, { "Tonic Green", 40 }, { "Tonic Red", 40 },
                { "Stone Axe", 50 }, { "Metal Axe", 50 },
                { "Stone Pickaxe", 50 }, { "Metal Pickaxe", 50 },
                { "Stone Shovel", 50 }, { "Iron Shovel", 50 }, { "Coal Shovel", 50 },
                { "Building Hammer", 50 }, { "Knife", 50 }, { "Crowbar", 50 },
                { "Fishing Rod", 50 }, { "Scissors", 50 }, { "Scythe", 50 },
                { "Wrench", 50 }, { "Paint Brush", 50 }, { "Lighter", 50 },
                { "Compass", 50 },
                { "Crossbow", 60 }, { "Crossbow Arrow", 60 },
                { "AK47", 60 }, { "M1911", 60 }, { "M1A", 60 }, { "MP5", 60 },
                { "Revolver", 60 }, { "Hunting Rifle", 60 },
                { "Double Barrel Shotgun", 60 }, { "R870", 60 }, { "Flamethrower", 60 },
                { "Pistol Ammo", 60 }, { "Rifle Ammo", 60 }, { "Shotgun Bullet", 60 },
                { "5.56", 60 },
                { "Fire Arrow", 60 }, { "Poison Arrow", 60 }, { "Explosive Arrow", 60 },
                { "Dynamite", 60 }, { "Grenade", 60 }, { "F1", 60 },
                { "Scrap Armor", 60 }, { "Plastic Helmet", 60 },
                { "Fence Wood", 70 }, { "Pillar", 70 }, { "Pillar Half", 70 },
                { "Ladder", 70 }, { "Small Ladder", 70 },
                { "Bed", 80 }, { "Tree Bed", 80 }, { "Wooden Chair", 80 },
                { "Table", 80 }, { "Shelf", 80 }, { "Multi Shelf", 80 },
                { "Cabinet", 80 }, { "Decorative Cabinet", 80 },
                { "Sofa", 80 }, { "Single Armchair", 80 }, { "Double Armchair", 80 },
                { "Carpet", 80 }, { "Curtain", 80 }, { "Clock", 80 },
                { "Candlestick", 80 }, { "Oil Lamp", 80 }, { "Standing Torch", 80 },
                { "Torch", 80 }, { "Vase", 80 }, { "Basic Vase", 80 },
                { "Paintings", 80 }, { "Gramophone", 80 },
                { "Sign", 80 }, { "Wall Sign", 80 }, { "Colored Flag", 80 },
                { "Figurine", 80 }, { "Statuette", 80 }, { "Live Plants", 80 },
                { "Workbench", 85 }, { "Weapon Workbench", 85 }, { "Repair Bench", 85 },
                { "Research Table", 85 }, { "Medical Desk", 85 },
                { "Grill", 85 }, { "Stone Grill", 85 }, { "Oven", 85 },
                { "Smelter", 85 }, { "Metal Smelter", 85 },
                { "Chest", 85 }, { "Large Chest", 85 }, { "Refrigerator", 85 },
                { "Generator", 85 },
                { "Water Barrel", 85 }, { "Water Filter", 85 },
                { "Water Purifier", 85 }, { "Advanced Water Purifier", 85 },
                { "Coal Drill", 85 }, { "Stone Quarry", 85 },
                { "Single Planter", 85 }, { "Big Planter", 85 },
                { "Wagon", 90 },
                { "Ground", 95 },
                { "Folded Pipe", 95 }, { "Standard Pipe", 95 },
                { "Triple Pipe", 95 }, { "Quadruple Pipe", 95 },
            };

        public static int GetItemTier(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return 99;
            int t;
            if (ItemTierTable.TryGetValue(itemName, out t)) return t;
            if (itemName.StartsWith("Story Paper", StringComparison.OrdinalIgnoreCase))
                return 95;
            if (itemName.StartsWith("Cooked ", StringComparison.OrdinalIgnoreCase))
                return 30;
            if (itemName.StartsWith("Iron ", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("Metal ", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("Wood ", StringComparison.OrdinalIgnoreCase))
                return 70;
            return 99;
        }

        private static int GetItemSizeMultiplier(object itemData)
        {
            try
            {
                var itemType = itemData.GetType();
                var m = itemType.GetMethod("GetItemSizeMultiplier",
                    BindingFlags.Public | BindingFlags.Instance);
                if (m != null)
                {
                    var result = m.Invoke(itemData, null);
                    if (result is int i) return i;
                    if (result is long l) return (int)l;
                }
                var field = itemType.GetField("itemSizeType",
                    BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(itemData);
                    if (val != null)
                    {
                        if (val is int) return (int)val;
                        return Convert.ToInt32(val);
                    }
                }
            }
            catch { }
            return 1;
        }

        private static int GetInventorySlotSize()
        {
            try
            {
                _gameSettingsType = _gameSettingsType ?? ReflectionUtil.FindType("GameSettings");
                if (_gameSettingsType == null) return 32;
                var instProp = _gameSettingsType.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static);
                var inst = instProp?.GetValue(null, null);
                if (inst == null) return 32;
                var slotField = _gameSettingsType.GetField("inventorySlotSize",
                    BindingFlags.Public | BindingFlags.Instance);
                if (slotField != null)
                {
                    var v = slotField.GetValue(inst);
                    if (v is int i) return i;
                    if (v is long l) return (int)l;
                }
            }
            catch { }
            return 32;
        }

        public static void GatherNearby(float radius = 40f, int max = 40)
        {
            var inv = LocalInventory;
            if (inv == null) { MelonLogger.Warning("[Gather] Local PlayerInventory not found."); return; }
            _playerInvType = _playerInvType ?? ReflectionUtil.FindType("PlayerInventory");
            MelonCoroutines.Start(GatherRoutine(inv, radius, max));
        }

        public static void ListItemNames()
        {
            _collectableType = _collectableType ?? ReflectionUtil.FindType("CollectableItemData");
            if (_collectableType == null) { MelonLogger.Warning("[Items] CollectableItemData type not found."); return; }
            var all = Resources.FindObjectsOfTypeAll(_collectableType);
            var names = new List<string>();
            foreach (var o in all)
            {
                var nm = GetMemberValue(o, "itemName") as string;
                if (!string.IsNullOrEmpty(nm)) names.Add(nm);
            }
            names.Sort();
            MelonLogger.Msg("[Items] " + names.Count + " item names available:");
            foreach (var n in names) MelonLogger.Msg("   " + n);
        }

        private static IEnumerator GatherRoutine(object inv, float radius, int max)
        {
            var pinv = inv as Component;
            Vector3 center = pinv != null ? pinv.transform.position : Vector3.zero;
            var invType = _playerInvType ?? ReflectionUtil.FindType("PlayerInventory");
            int done = 0;

            foreach (var typeName in new[] { "TreeCollectable", "OreCollectable" })
            {
                var t = ReflectionUtil.FindType(typeName);
                if (t == null) continue;
                var arr = UnityEngine.Object.FindObjectsOfType(t);
                var list = new List<UnityEngine.Object>(arr);
                list.Sort((a, b) =>
                {
                    var pa = (a as Component)?.transform.position ?? Vector3.zero;
                    var pb = (b as Component)?.transform.position ?? Vector3.zero;
                    return Vector3.Distance(center, pa).CompareTo(Vector3.Distance(center, pb));
                });

                var getDamage = t.GetMethod("GetDamage",
                    new[] { invType, typeof(float), typeof(Vector3) });

                foreach (var o in list)
                {
                    if (done >= max) { MelonLogger.Msg("[Gather] reached cap (" + max + ")."); yield break; }
                    var c = o as Component;
                    if (c == null) continue;
                    float d = Vector3.Distance(center, c.transform.position);
                    if (d > radius) break;
                    try
                    {
                        getDamage?.Invoke(o, new object[] { inv, 99999f, c.transform.position });
                        done++;
                    }
                    catch (System.Exception e)
                    {
                        MelonLogger.Warning("[Gather] " + (e.InnerException?.Message ?? e.Message));
                    }
                    if ((done % 5) == 0) yield return null;
                }
            }
            MelonLogger.Msg("[Gather] processed " + done + " nodes within " + radius + "m.");
        }

        private static object FindItemData(string nameKey)
        {
            if (ItemCache.TryGetValue(nameKey, out var cached)) return cached;
            _collectableType = _collectableType ?? ReflectionUtil.FindType("CollectableItemData");
            if (_collectableType == null) return null;

            var all = Resources.FindObjectsOfTypeAll(_collectableType);
            object exact = null, partial = null;
            foreach (var o in all)
            {
                var nm = GetMemberValue(o, "itemName") as string;
                if (string.IsNullOrEmpty(nm)) continue;
                if (nm.Equals(nameKey, System.StringComparison.OrdinalIgnoreCase)) { exact = o; break; }
                if (partial == null && nm.IndexOf(nameKey, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    partial = o;
            }
            var result = exact ?? partial;
            if (result != null) ItemCache[nameKey] = result;
            return result;
        }

        private static object FindLocalInventory()
        {
            _playerInvType = _playerInvType ?? ReflectionUtil.FindType("PlayerInventory");
            if (_playerInvType == null) return null;

            var netClient = ReflectionUtil.FindType("Mirror.NetworkClient") ?? ReflectionUtil.FindType("NetworkClient");
            if (netClient != null)
            {
                var lp = netClient.GetProperty("localPlayer", StaticFlags)?.GetValue(null, null);
                var comp = lp as Component;
                if (comp != null)
                {
                    var inv = comp.GetComponent(_playerInvType);
                    if (inv != null) return inv;
                    var root = comp.transform.root;
                    inv = root?.GetComponent(_playerInvType)
                       ?? root?.GetComponentInChildren(_playerInvType, true);
                    if (inv != null) return inv;
                }
            }

            var all = UnityEngine.Object.FindObjectsOfType(_playerInvType);
            var nbType = ReflectionUtil.FindType("Mirror.NetworkBehaviour") ?? ReflectionUtil.FindType("NetworkBehaviour");
            foreach (var o in all)
            {
                var c = o as Component;
                if (c == null) continue;
                if (nbType != null)
                {
                    foreach (var nb in c.GetComponents(nbType))
                    {
                        var ilp = nb.GetType().GetProperty("isLocalPlayer")?.GetValue(nb, null);
                        if (ilp is bool b && b) return o;
                    }
                }
            }
            if (all.Length > 0) return all[0];
            return null;
        }

        private static object GetMemberValue(object obj, params string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, InstanceFlags);
                if (p != null && p.CanRead) { try { return p.GetValue(obj, null); } catch { } }
                var f = t.GetField(n, InstanceFlags);
                if (f != null) { try { return f.GetValue(obj); } catch { } }
            }
            return null;
        }
    }
}

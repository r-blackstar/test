using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace OnTheTrainDemoCheat
{
    /// <summary>
    /// One-click resource/craft giving + auto-gather, built on real game APIs found by decompiling
    /// Assembly-CSharp (ilspycmd):
    ///   - PlayerInventory.AddItemInventory(CollectableItemData, int count, float durability, int preferredSlot)
    ///   - CollectableItemData : ScriptableObject  (field: string itemName)
    ///   - TreeCollectable / OreCollectable : BreakableObject  -> GetDamage(PlayerInventory, float, Vector3)
    ///   - Mirror.NetworkClient.localPlayer  to locate the local player's inventory
    ///
    /// Give-Items is the reliable "一键" path (instantly adds resources / crafted materials),
    /// covering 砍树/挖矿/制作 outputs. Chop/Mine Nearby is best-effort (calls the real hit method).
    /// </summary>
    internal static class Items
    {
        private static object _localInv;
        private static Type _collectableType;
        private static Type _playerInvType;
        private static Type _gameSettingsType;
        private static readonly Dictionary<string, object> ItemCache = new Dictionary<string, object>();

        /// <summary>物品浏览器条目：itemName + 显示名 + 堆叠上限。</summary>
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

        // v1.5.8：FindLocalInventory 失败冷却，避免每帧全场景扫描
        private static float _lastInvFindTime;
        private const float InvFindCooldown = 2f;

        public static object LocalInventory
        {
            get
            {
                if (_localInv is UnityEngine.Object u && u == null) _localInv = null;
                if (_localInv == null && Time.unscaledTime - _lastInvFindTime > InvFindCooldown)
                {
                    _lastInvFindTime = Time.unscaledTime;
                    _localInv = FindLocalInventory();
                }
                return _localInv;
            }
        }

        /// <summary>Give `amount` of the item whose itemName matches `nameKey` (exact, then partial).</summary>
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

        /// <summary>
        /// 给予一个格子堆满的物品数量。
        /// 堆叠上限 = inventorySlotSize / item.GetItemSizeMultiplier()，
        /// 开启 InfiniteInventoryCapacity 时为 int.MaxValue。
        /// </summary>
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

        /// <summary>
        /// 计算指定物品在一格内的堆叠上限。
        /// 公式：slotSize / itemSizeMultiplier
        ///   - slotSize = GameSettings.Instance.inventorySlotSize（默认32，模组可改 int.MaxValue）
        ///   - itemSizeMultiplier = item.GetItemSizeMultiplier()
        ///     Single=1, x2=2, x4=4, x8=8, MaxSize=slotSize
        /// </summary>
        public static int GetItemStackLimit(object itemData)
        {
            if (itemData == null) return 1;
            try
            {
                // 1. 取 itemSizeMultiplier（调用 item.GetItemSizeMultiplier()）
                int multiplier = GetItemSizeMultiplier(itemData);
                if (multiplier <= 0) multiplier = 1;

                // 2. 取 inventorySlotSize
                int slotSize = GetInventorySlotSize();
                if (slotSize <= 0) slotSize = 32;

                // 3. 堆叠上限 = slotSize / multiplier（至少 1）
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

        /// <summary>获取所有已加载的 CollectableItemData，整理为浏览器条目列表。</summary>
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

                // 优先使用模组自带翻译 JSON（键名格式：item.<itemName>，大小写不敏感）
                var display = I18n.GetIgnoreCase("item." + itemName);
                // 兜底：尝试读取 itemDisplayName 字段
                if (string.IsNullOrEmpty(display))
                {
                    display = GetMemberValue(o, "itemDisplayName") as string;
                }
                if (string.IsNullOrEmpty(display)) display = itemName;

                list.Add(new ItemEntry
                {
                    ItemName = itemName,
                    DisplayName = display,
                    StackLimit = GetItemStackLimit(o)
                });
                debugNames.Add(itemName);
            }

            // 首次构建列表时把所有物品名写入日志（方便补充翻译表）
            if (!_itemsLoggedOnce && debugNames.Count > 0)
            {
                _itemsLoggedOnce = true;
                debugNames.Sort();
                MelonLogger.Msg("[Items] GetAllItems found " + debugNames.Count + " items:");
                foreach (var n in debugNames)
                    MelonLogger.Msg("   " + n);
            }

            // 按"游戏进度阶段"排序：基础材料 → 加工材料 → 食物 → 工具 → 武器弹药
            // → 医疗 → 建筑 → 火车 → 特殊 → 未分类
            list.Sort((a, b) =>
            {
                int ta = GetItemTier(a.ItemName);
                int tb = GetItemTier(b.ItemName);
                if (ta != tb) return ta.CompareTo(tb);
                return string.CompareOrdinal(a.ItemName, b.ItemName);
            });
            return list;
        }

        // ---- 内部：堆叠计算 ----

        /// <summary>
        /// 物品在游戏进度中的阶段（越小越靠前）。
        /// 10=基础原材料 / 20=加工材料 / 30=食物饮品 / 40=医疗
        /// 50=工具 / 60=武器弹药 / 70=建筑部件 / 80=家具装饰
        /// 85=工作台设备 / 90=火车 / 95=特殊/剧情 / 99=未分类
        /// 先精确匹配 ItemTierTable，再按前缀回退（处理 Iron*/Metal*/Wood*/Cooked*/Story Paper* 等系列）。
        /// </summary>
        private static readonly Dictionary<string, int> ItemTierTable =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                // 10 - 基础原材料（采集/打怪掉落）
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

                // 20 - 加工材料（冶炼/制造获得）
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

                // 30 - 食物饮品
                { "Clean Water Bottle", 30 }, { "Dirty Water Bottle", 30 }, { "Water Bottle", 30 },
                { "Canned Food", 30 }, { "Nutrition Syrup", 30 },
                { "Cooked Meat", 30 }, { "Cooked Mushroom", 30 },

                // 40 - 医疗
                { "Bandage", 40 }, { "Health Syringe", 40 }, { "Stamina Syringe", 40 },
                { "Not Hungry Pill", 40 }, { "Recovery Pills", 40 }, { "Regenaration Pill", 40 },
                { "Pills Blue", 40 }, { "Pills Green", 40 }, { "Pills Orange", 40 },
                { "Syringe Blue", 40 }, { "Syringe Green", 40 }, { "Syringe Red", 40 },
                { "Tonic Blue", 40 }, { "Tonic Green", 40 }, { "Tonic Red", 40 },

                // 50 - 工具
                { "Stone Axe", 50 }, { "Metal Axe", 50 },
                { "Stone Pickaxe", 50 }, { "Metal Pickaxe", 50 },
                { "Stone Shovel", 50 }, { "Iron Shovel", 50 }, { "Coal Shovel", 50 },
                { "Building Hammer", 50 }, { "Knife", 50 }, { "Crowbar", 50 },
                { "Fishing Rod", 50 }, { "Scissors", 50 }, { "Scythe", 50 },
                { "Wrench", 50 }, { "Paint Brush", 50 }, { "Lighter", 50 },
                { "Compass", 50 },

                // 60 - 武器弹药
                { "Crossbow", 60 }, { "Crossbow Arrow", 60 },
                { "AK47", 60 }, { "M1911", 60 }, { "M1A", 60 }, { "MP5", 60 },
                { "Revolver", 60 }, { "Hunting Rifle", 60 },
                { "Double Barrel Shotgun", 60 }, { "R870", 60 }, { "Flamethrower", 60 },
                { "Pistol Ammo", 60 }, { "Rifle Ammo", 60 }, { "Shotgun Bullet", 60 },
                { "5.56", 60 },
                { "Fire Arrow", 60 }, { "Poison Arrow", 60 }, { "Explosive Arrow", 60 },
                { "Dynamite", 60 }, { "Grenade", 60 }, { "F1", 60 },
                { "Scrap Armor", 60 }, { "Plastic Helmet", 60 },

                // 70 - 建筑部件（Iron*/Metal*/Wood* 前缀匹配兜底）
                { "Fence Wood", 70 }, { "Pillar", 70 }, { "Pillar Half", 70 },
                { "Ladder", 70 }, { "Small Ladder", 70 },

                // 80 - 家具装饰
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

                // 85 - 工作台/设备
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

                // 90 - 火车相关
                { "Wagon", 90 },

                // 95 - 特殊/剧情/管道
                { "Ground", 95 },
                { "Folded Pipe", 95 }, { "Standard Pipe", 95 },
                { "Triple Pipe", 95 }, { "Quadruple Pipe", 95 },
            };

        /// <summary>
        /// 根据物品名返回游戏进度阶段。先查精确表，再用前缀回退：
        /// Story Paper* / Cooked* → 食物或剧情；Iron*/Metal*/Wood* → 建筑部件（70）。
        /// </summary>
        public static int GetItemTier(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return 99;

            // 精确匹配
            int t;
            if (ItemTierTable.TryGetValue(itemName, out t)) return t;

            // 前缀回退（不区分大小写）
            if (itemName.StartsWith("Story Paper", StringComparison.OrdinalIgnoreCase))
                return 95; // 剧情纸
            if (itemName.StartsWith("Cooked ", StringComparison.OrdinalIgnoreCase))
                return 30; // 烹饪食物
            if (itemName.StartsWith("Iron ", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("Metal ", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("Wood ", StringComparison.OrdinalIgnoreCase))
                return 70; // 建筑部件系列

            return 99; // 未分类
        }


        private static int GetItemSizeMultiplier(object itemData)
        {
            try
            {
                var itemType = itemData.GetType();

                // 优先调用 GetItemSizeMultiplier()（游戏内置方法）
                var m = itemType.GetMethod("GetItemSizeMultiplier",
                    BindingFlags.Public | BindingFlags.Instance);
                if (m != null)
                {
                    var result = m.Invoke(itemData, null);
                    if (result is int i) return i;
                    if (result is long l) return (int)l;
                }

                // 兜底：读取 itemSizeType 枚举字段
                var field = itemType.GetField("itemSizeType",
                    BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(itemData);
                    if (val != null)
                    {
                        // 枚举底层是 int
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

                // Singleton<GameSettings>.Instance.inventorySlotSize
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

        /// <summary>
        /// Best-effort: 一键采集附近的树木/矿石/地表拾取物/渐进式采集物（金属废料等）。
        /// v1.5.7：radius 从 40 缩小到 20，max 从 40 调整为 30，新增 LootableTerrainItem 和 LootableTerrainItemProgressive 支持。
        /// v1.5.8：协程单例控制（避免并发），每次调用前检测 inv 有效性。
        /// </summary>
        // v1.5.8：MelonCoroutines.Start 返回 object，用 object 保存协程引用
        private static object _activeGather;

        public static void GatherNearby(float radius = 20f, int max = 30)
        {
            var inv = LocalInventory;
            if (inv == null) { MelonLogger.Msg("[Gather] 请先进入游戏场景再使用采集功能。"); return; }
            _playerInvType = _playerInvType ?? ReflectionUtil.FindType("PlayerInventory");
            // v1.5.8：取消上一个未完成的协程，避免并发干扰
            if (_activeGather != null)
            {
                try { MelonCoroutines.Stop(_activeGather); } catch { }
                _activeGather = null;
            }
            _activeGather = MelonCoroutines.Start(GatherRoutine(inv, radius, max));
        }

        /// <summary>Print every loaded CollectableItemData.itemName to the MelonLoader console.</summary>
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
            bool reachedCap = false;

            // v1.5.7：三类采集对象
            //   TreeCollectable / OreCollectable        -> GetDamage(inv, 99999f, point) 瞬采
            //   LootableTerrainItem                      -> Take(player)  单次拾取（蘑菇/草药等）
            //   LootableTerrainItemProgressive           -> 反射调用 FinishLooting() 完成渐进采集（金属废料等）
            var breakableTypes = new[]
            {
                "TreeCollectable",                 // 砍树
                "OreCollectable",                  // 挖矿
                "LootableTerrainItem",             // 地表拾取物（单次采集）
                "LootableTerrainItemProgressive"   // 渐进式采集（金属废料等，长按 F）
            };

            foreach (var typeName in breakableTypes)
            {
                if (reachedCap) break;
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

                // 按类型选择采集方法
                MethodInfo gatherMethod = null;
                bool isTakeStyle = false;       // Take(player)
                bool isFinishStyle = false;     // FinishLooting()
                if (typeName == "TreeCollectable" || typeName == "OreCollectable")
                {
                    gatherMethod = t.GetMethod("GetDamage",
                        new[] { invType, typeof(float), typeof(Vector3) });
                }
                else if (typeName == "LootableTerrainItem")
                {
                    gatherMethod = t.GetMethod("Take", new[] { invType });
                    isTakeStyle = true;
                }
                else if (typeName == "LootableTerrainItemProgressive")
                {
                    // LootableTerrainItemProgressive 的 Take 方法不存在，Interact 只是开锁住按 F
                    // 直接反射调用 private FinishLooting() 跳过渐进流程，立即完成并给物品
                    // 注意有重载：FinishLooting() 和 FinishLooting(PlayerInventory)，取无参版本
                    var methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var m in methods)
                    {
                        if (m.Name == "FinishLooting" && m.GetParameters().Length == 0)
                        {
                            gatherMethod = m;
                            break;
                        }
                    }
                    isFinishStyle = true;
                }

                if (gatherMethod == null)
                {
                    MelonLogger.Warning("[Gather] " + typeName + ": no gather method found, skip.");
                    continue;
                }

                foreach (var o in list)
                {
                    // v1.5.8：每次迭代前检测 inv 是否仍有效（玩家可能已离开场景/死亡）
                    if (pinv is UnityEngine.Object u && u == null)
                    {
                        MelonLogger.Warning("[Gather] Player destroyed mid-routine, abort.");
                        yield break;
                    }
                    if (done >= max) { reachedCap = true; break; }
                    var c = o as Component;
                    if (c == null) continue;
                    float d = Vector3.Distance(center, c.transform.position);
                    if (d > radius) break; // sorted ascending, rest are farther
                    try
                    {
                        if (isTakeStyle)
                        {
                            // Take(player)
                            gatherMethod.Invoke(o, new object[] { inv });
                        }
                        else if (isFinishStyle)
                        {
                            // LootableTerrainItemProgressive.FinishLooting() 是 private
                            // v1.5.8：player 字段 null 检查，若字段不存在跳过该对象
                            var playerField = t.GetField("player",
                                BindingFlags.NonPublic | BindingFlags.Instance);
                            if (playerField == null)
                            {
                                MelonLogger.Warning("[Gather] " + typeName + ": 'player' field not found, skip object.");
                                continue;
                            }
                            playerField.SetValue(o, inv);
                            gatherMethod.Invoke(o, null);
                        }
                        else
                        {
                            // GetDamage(inv, 99999f, point)
                            gatherMethod.Invoke(o, new object[] { inv, 99999f, c.transform.position });
                        }
                        done++;
                    }
                    catch (System.Exception e)
                    {
                        MelonLogger.Warning("[Gather] " + typeName + ": " + (e.InnerException?.Message ?? e.Message));
                    }
                    if ((done % 5) == 0) yield return null; // pace to avoid flooding one frame
                }
                MelonLogger.Msg("[Gather] " + typeName + " processed, total done=" + done);
            }
            MelonLogger.Msg("[Gather] processed " + done + " nodes within " + radius + "m" + (reachedCap ? " (reached cap " + max + ")" : "") + ".");
            _activeGather = null;
        }

        private static object FindItemData(string nameKey)
        {
            // v1.5.8：取出缓存时验证 Unity 生命周期，避免持有已销毁 ScriptableObject
            if (ItemCache.TryGetValue(nameKey, out var cached))
            {
                if (cached is UnityEngine.Object uo && uo == null)
                {
                    ItemCache.Remove(nameKey);
                }
                else
                {
                    return cached;
                }
            }
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

            // 1) Mirror.NetworkClient.localPlayer -> GetComponent<PlayerInventory> (on self or root)
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

            // 2) Fallback: 找所有 PlayerInventory，检查其 GameObject 上的 TSPlayerController.isLocalPlayer
            // v1.5.8 修正：PlayerInventory 继承 MonoBehaviour 不是 NetworkBehaviour，原代码 GetComponents(nbType) 永远空
            //   正确做法：PlayerInventory 所在 GameObject 上有 TSPlayerController（NetworkBehaviour），检查它的 isLocalPlayer
            var all = UnityEngine.Object.FindObjectsOfType(_playerInvType);
            var playerCtrlType = ReflectionUtil.FindType("TSPlayerController");
            foreach (var o in all)
            {
                var c = o as Component;
                if (c == null) continue;
                // 检查同 GameObject 上的 TSPlayerController
                if (playerCtrlType != null)
                {
                    var ctrl = c.GetComponent(playerCtrlType);
                    if (ctrl != null)
                    {
                        var ilp = ctrl.GetType().GetProperty("isLocalPlayer")?.GetValue(ctrl, null);
                        if (ilp is bool b && b) return o;
                    }
                }
            }
            // v1.5.8：单人模式或 TSPlayerController 尚未生成时，若场景中只有一个 PlayerInventory，返回它
            if (all.Length == 1)
            {
                MelonLogger.Msg("[Items] Only one PlayerInventory in scene, using it (single-player fallback).");
                return all[0];
            }
            // v1.5.10：all.Length == 0 表示还在主菜单/未进入游戏场景，属于预期情况，静默返回不刷日志
            // 只有 all.Length > 1 且都找不到 isLocalPlayer 才是真正异常
            if (all.Length > 1)
            {
                MelonLogger.Warning("[Items] No local PlayerInventory found (isLocalPlayer check failed, count=" + all.Length + ").");
            }
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

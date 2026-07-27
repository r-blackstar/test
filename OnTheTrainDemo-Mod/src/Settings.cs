using MelonLoader;

namespace OnTheTrainDemoMod
{
    /// <summary>
    /// Persisted mod settings. MelonLoader writes these to a MelonPreferences config file
    /// next to the game (MelonLoader/UserData/...) so toggles survive restarts.
    /// </summary>
    internal static class Settings
    {
        public static MelonPreferences_Category Category;

        public static MelonPreferences_Entry<bool> GodMode;
        public static MelonPreferences_Entry<bool> InfiniteVitals;
        public static MelonPreferences_Entry<bool> InfiniteStamina;
        public static MelonPreferences_Entry<bool> InfiniteAmmo;
        public static MelonPreferences_Entry<bool> InfiniteFuel;
        public static MelonPreferences_Entry<bool> InfiniteInventoryCapacity;
        public static MelonPreferences_Entry<bool> FreeCraft;
        public static MelonPreferences_Entry<bool> ShowOverlay;
        public static MelonPreferences_Entry<string> Language;

        public static void Register()
        {
            Category = MelonPreferences.CreateCategory("OnTheTrainDemo", "On The Train Demo");

            GodMode         = Category.CreateEntry(nameof(GodMode),         false, "God Mode (no damage)");
            // 满血/满饥渴/满水：每帧把本地玩家的 Hp/Food/Water 拉满。static postfix 到 TSPlayerStatusHolder.Update。
            InfiniteVitals  = Category.CreateEntry(nameof(InfiniteVitals),  false, "Full Hp/Food/Water (vitals always max)");
            InfiniteStamina = Category.CreateEntry(nameof(InfiniteStamina), false, "Infinite Stamina");
            InfiniteAmmo    = Category.CreateEntry(nameof(InfiniteAmmo),    false, "Infinite Ammo");
            InfiniteFuel    = Category.CreateEntry(nameof(InfiniteFuel),    false, "Infinite Train Fuel");
            // 背包格子堆叠容量无限（每格可堆叠至 ~21 亿）。注：修改后需重启游戏生效。
            InfiniteInventoryCapacity = Category.CreateEntry(nameof(InfiniteInventoryCapacity), false, "Infinite inventory slot capacity (requires restart)");
            // 免费制造：Craft 时跳过材料消耗逻辑（与游戏 Creative 模式相同效果）。
            FreeCraft       = Category.CreateEntry(nameof(FreeCraft),       false, "Free Crafting (no material cost)");
            ShowOverlay     = Category.CreateEntry(nameof(ShowOverlay),     true,  "Show Info Overlay");
            // 语言代码（对应 Mods/OnTheTrainDemoMod/lang/<code>.json）。默认 zh-CN（简体中文）。
            Language        = Category.CreateEntry(nameof(Language),        "zh-CN", "Language code (e.g. zh-CN, en-US)");
        }
    }
}

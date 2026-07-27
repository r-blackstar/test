using MelonLoader;

namespace OnTheTrainDemoMod
{
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
            InfiniteVitals  = Category.CreateEntry(nameof(InfiniteVitals),  false, "Full Hp/Food/Water (vitals always max)");
            InfiniteStamina = Category.CreateEntry(nameof(InfiniteStamina), false, "Infinite Stamina");
            InfiniteAmmo    = Category.CreateEntry(nameof(InfiniteAmmo),    false, "Infinite Ammo");
            InfiniteFuel    = Category.CreateEntry(nameof(InfiniteFuel),    false, "Infinite Train Fuel");
            InfiniteInventoryCapacity = Category.CreateEntry(nameof(InfiniteInventoryCapacity), false, "Infinite inventory slot capacity (requires restart)");
            FreeCraft       = Category.CreateEntry(nameof(FreeCraft),       false, "Free Crafting (no material cost)");
            ShowOverlay     = Category.CreateEntry(nameof(ShowOverlay),     true,  "Show Info Overlay");
            Language        = Category.CreateEntry(nameof(Language),        "zh-CN", "Language code (e.g. zh-CN, en-US)");
        }
    }
}

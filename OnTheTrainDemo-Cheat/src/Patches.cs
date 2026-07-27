using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace OnTheTrainDemoCheat
{
    /// <summary>
    /// STATIC Harmony patches. Registered once in OnInitializeMelon. No per-frame reflection,
    /// no FindObjectsOfType in hot loops — this is what eliminates the previous FPS drop.
    ///
    /// Each prefix reads a Settings toggle, so every feature is a manual on/off switch with
    /// near-zero cost when off (a single bool check) and a short-circuit when on.
    ///
    /// Targets (confirmed by decompiling Assembly-CSharp with ilspycmd):
    ///   God Mode    : TSPlayerStatusHolder.ApplyHealthChange(float) — healing/damage via items
    ///                 TSPlayerStatusHolder.GetDamage(float, bool)   — zombie/starvation damage
    ///   Infinite Vitals : TSPlayerStatusHolder.Update() postfix — clamp Hp/Food/Water to 100
    ///   Infinite Ammo : JUTPS.WeaponSystem.Weapon.Shot()  — has `if(!InfiniteAmmo) BulletsAmounts--;`
    ///   Infinite Stamina : HQFPSTemplate.PlayerVitals.UpdateStats() (private) — stamina depletion loop
    ///   Infinite Fuel : TrainController.set_NetworknetworkFuelLevel(float value) — fuel SyncVar setter
    ///   Free Craft  : CraftItemUI.Craft() prefix — skip the "subtract materials" loop
    /// </summary>
    internal static class Patches
    {
        public const string InstanceId = "com.workbuddy.onthetraindemo";
        private static HarmonyLib.Harmony _harmony;

        public static void Install()
        {
            _harmony = new HarmonyLib.Harmony(InstanceId);

            // God Mode: skip incoming damage from BOTH paths.
            // 1) ApplyHealthChange — used by bandages (+) and any direct negative health change.
            Patch("TSPlayerStatusHolder", "ApplyHealthChange",
                  Prefix(nameof(GodHealthPrefix)), new[] { typeof(float) });

            // 2) GetDamage(float, bool) — zombie hits & starvation/thirst damage.
            Patch("TSPlayerStatusHolder", "GetDamage",
                  Prefix(nameof(GodDamagePrefix)), new[] { typeof(float), typeof(bool) });

            // Infinite Vitals: clamp Hp/Food/Water to 100 every frame on the LOCAL player.
            // Update() is private and early-outs for non-local players, so the postfix only runs
            // on the local one. nonPublic is required to find the method.
            PatchPostfix("TSPlayerStatusHolder", "Update",
                         Postfix(nameof(VitalsPostfix)), nonPublic: true);

            // Infinite Ammo: force the weapon's InfiniteAmmo flag true before Shot() decrements.
            Patch("Weapon", "Shot", Prefix(nameof(AmmoPrefix)));

            // Infinite Stamina: skip the stamina depletion/regen update entirely (freezes stamina).
            Patch("PlayerVitals", "UpdateStats",
                  Prefix(nameof(StaminaPrefix)), nonPublic: true);

            // Infinite Fuel: block decreases to the train fuel SyncVar.
            Patch("TrainController", "set_NetworknetworkFuelLevel",
                  Prefix(nameof(FuelPrefix)), new[] { typeof(float) });

            // Infinite Inventory Capacity: PostFix on PlayerInventory.Initialize — once the game
            // builds its slot list, overwrite every maxCapacity with int.MaxValue. Static, no
            // per-frame cost. Toggle requires restart (Initialize only runs once per inventory).
            PatchPostfix("PlayerInventory", "Initialize",
                         Postfix(nameof(InventoryCapacityPostfix)),
                         nonPublic: true);

            // Free Craft: prefix+postfix on CraftItemUI.Craft — when on, we swap neededItemsData
            // for an empty list before the consume loop runs, then restore it after so the UI keeps
            // rendering the recipe's cost correctly. Static; only fires on user click.
            PatchPrePost("CraftItemUI", "Craft",
                         Prefix(nameof(FreeCraftPrefix)),
                         Postfix(nameof(FreeCraftPostfix)));
        }

        // ---- prefixes (static; toggled via Settings) ----

        // Return false to skip ApplyHealthChange when God Mode is on and the change is damage.
        private static bool GodHealthPrefix(float healthAmount)
            => !(Settings.GodMode.Value && healthAmount < 0f);

        // Return false to skip GetDamage entirely when God Mode is on (no Hp loss, no faint).
        private static bool GodDamagePrefix(float damage, bool isZombieHit)
            => !Settings.GodMode.Value;

        // Force InfiniteAmmo=true on the weapon instance before Shot() checks it.
        private static void AmmoPrefix(ref bool ___InfiniteAmmo)
        {
            if (Settings.InfiniteAmmo.Value) ___InfiniteAmmo = true;
        }

        // Return false to skip UpdateStats when Infinite Stamina is on.
        private static bool StaminaPrefix()
            => !Settings.InfiniteStamina.Value;

        // Return false to block fuel decreases when Infinite Fuel is on.
        private static bool FuelPrefix(float value, ref float ___networkFuelLevel)
            => !(Settings.InfiniteFuel.Value && value < ___networkFuelLevel);

        // Free Craft: clear neededItemsData for this call so the consume loop does nothing.
        // We can't use __state (CraftItemUI is one instance per recipe), so we back up to a
        // thread-static field and restore in the postfix. Cheap, only runs on user click.
        private static void FreeCraftPrefix(object __instance, ref IList __state)
        {
            if (!Settings.FreeCraft.Value) return;
            try
            {
                // neededItemsData is List<CostData>; back it up and replace with empty list so
                // the foreach (CostData in neededItemsData) inventory.AddItemInventory(-cost) loop
                // subtracts nothing.
                var list = ReflectionUtil.GetMemberValue(__instance, "neededItemsData") as IList;
                __state = list;
                if (list != null)
                {
                    var empty = (IList)Activator.CreateInstance(list.GetType());
                    ReflectionUtil.SetMemberValue(__instance, empty, "neededItemsData");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Patch] FreeCraft prefix failed: " + e.Message);
            }
        }

        // Restore the original neededItemsData after Craft() ran. Without this the UI would
        // permanently lose its cost list and the recipe panel would render without costs.
        private static void FreeCraftPostfix(object __instance, IList __state)
        {
            if (!Settings.FreeCraft.Value || __state == null) return;
            try
            {
                ReflectionUtil.SetMemberValue(__instance, __state, "neededItemsData");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Patch] FreeCraft postfix failed: " + e.Message);
            }
        }

        // ---- postfixes ----

        // TSPlayerStatusHolder.Update() — runs every frame on the local player only (the method
        // early-returns for non-local players via `isLocalPlayer` check). When InfiniteVitals is
        // on, clamp Hp/Food/Water to 100. The fields are public on TSPlayerStatusHolder; we use
        // the cached reflection lookup (first call caches, subsequent calls are dict-get).
        private static void VitalsPostfix(object __instance)
        {
            if (!Settings.InfiniteVitals.Value) return;
            try
            {
                ReflectionUtil.SetMemberValue(__instance, 100f, "playerHpFuel");
                ReflectionUtil.SetMemberValue(__instance, 100f, "playerFoodFuel");
                ReflectionUtil.SetMemberValue(__instance, 100f, "playerWaterFuel");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Patch] InfiniteVitals failed: " + e.Message);
            }
        }

        // PlayerInventory.Initialize(TSPlayerController) — runs once when the local inventory is
        // built. We bump every slot's maxCapacity to int.MaxValue and the global fallback fields
        // (PlayerInventory.inventorySlotMaxCapacity and GameSettings.inventorySlotSize) too, so
        // both code paths in the game's drag/add logic agree on "infinite".
        //
        // 游戏无负重系统（反编译确认：玩家相关类中没有 carryWeight/encumbrance/maxWeight 字段），
        // 所以这里只处理"格子容量"这一种限制。
        private static void InventoryCapacityPostfix(object __instance)
        {
            if (!Settings.InfiniteInventoryCapacity.Value) return;
            try
            {
                // 1) PlayerInventory.inventorySlotMaxCapacity = int.MaxValue
                ReflectionUtil.SetMemberValue(__instance, int.MaxValue,
                    "inventorySlotMaxCapacity");

                // 2) inventorySlotsData[*].maxCapacity = int.MaxValue
                var slots = ReflectionUtil.GetMemberValue(__instance, "inventorySlotsData")
                            as System.Collections.IList;
                if (slots != null)
                {
                    foreach (var slot in slots)
                        ReflectionUtil.SetMemberValue(slot, int.MaxValue, "maxCapacity");
                }

                // 3) GameSettings.inventorySlotSize = int.MaxValue (if Instance exists)
                var gsType = ReflectionUtil.FindType("GameSettings");
                if (gsType != null)
                {
                    var instProp = gsType.GetProperty("Instance",
                        BindingFlags.Public | BindingFlags.Static);
                    var gs = instProp?.GetValue(null, null);
                    if (gs != null)
                        ReflectionUtil.SetMemberValue(gs, int.MaxValue, "inventorySlotSize");
                }

                MelonLogger.Msg("[Patch] InfiniteInventoryCapacity applied (" +
                    (slots?.Count ?? 0) + " slots).");
            }
            catch (System.Exception e)
            {
                MelonLogger.Warning("[Patch] InfiniteInventoryCapacity failed: " + e.Message);
            }
        }

        // ---- helpers ----

        private static HarmonyMethod Prefix(string methodName)
            => new HarmonyMethod(typeof(Patches).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Static));

        private static HarmonyMethod Postfix(string methodName)
            => new HarmonyMethod(typeof(Patches).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Static));

        private static void Patch(string typeName, string methodName, HarmonyMethod prefix,
            Type[] paramTypes = null, bool nonPublic = false)
        {
            try
            {
                var t = ReflectionUtil.FindType(typeName);
                if (t == null)
                {
                    MelonLogger.Warning("[Patch] type not found: " + typeName + " — " + methodName + " disabled.");
                    return;
                }

                MethodInfo m;
                if (paramTypes != null)
                    m = t.GetMethod(methodName, paramTypes);
                else
                    m = t.GetMethod(methodName,
                        BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0) | BindingFlags.Instance);

                if (m == null)
                {
                    MelonLogger.Warning("[Patch] method not found: " + typeName + "." + methodName + " — disabled.");
                    return;
                }

                _harmony.Patch(m, prefix: prefix);
                MelonLogger.Msg("[Patch] installed " + typeName + "." + methodName);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Patch] " + typeName + "." + methodName + " failed: " + e.Message);
            }
        }

        private static void PatchPostfix(string typeName, string methodName, HarmonyMethod postfix,
            Type[] paramTypes = null, bool nonPublic = false)
        {
            try
            {
                var t = ReflectionUtil.FindType(typeName);
                if (t == null)
                {
                    MelonLogger.Warning("[Patch] type not found: " + typeName + " — " + methodName + " disabled.");
                    return;
                }

                MethodInfo m;
                if (paramTypes != null)
                    m = t.GetMethod(methodName, paramTypes);
                else
                    m = t.GetMethod(methodName,
                        BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0) | BindingFlags.Instance);

                if (m == null)
                {
                    MelonLogger.Warning("[Patch] method not found: " + typeName + "." + methodName + " — disabled.");
                    return;
                }

                _harmony.Patch(m, postfix: postfix);
                MelonLogger.Msg("[Patch] installed " + typeName + "." + methodName);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Patch] " + typeName + "." + methodName + " failed: " + e.Message);
            }
        }

        // Patch both prefix and postfix on the same method (used by Free Craft: backup → run → restore).
        private static void PatchPrePost(string typeName, string methodName,
            HarmonyMethod prefix, HarmonyMethod postfix,
            Type[] paramTypes = null, bool nonPublic = false)
        {
            try
            {
                var t = ReflectionUtil.FindType(typeName);
                if (t == null)
                {
                    MelonLogger.Warning("[Patch] type not found: " + typeName + " — " + methodName + " disabled.");
                    return;
                }

                MethodInfo m;
                if (paramTypes != null)
                    m = t.GetMethod(methodName, paramTypes);
                else
                    m = t.GetMethod(methodName,
                        BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0) | BindingFlags.Instance);

                if (m == null)
                {
                    MelonLogger.Warning("[Patch] method not found: " + typeName + "." + methodName + " — disabled.");
                    return;
                }

                _harmony.Patch(m, prefix: prefix, postfix: postfix);
                MelonLogger.Msg("[Patch] installed " + typeName + "." + methodName + " (pre+post)");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[Patch] " + typeName + "." + methodName + " failed: " + e.Message);
            }
        }
    }
}

using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace OnTheTrainDemoMod
{
    internal static class Patches
    {
        public const string InstanceId = "com.workbuddy.onthetraindemo";
        private static HarmonyLib.Harmony _harmony;

        public static void Install()
        {
            _harmony = new HarmonyLib.Harmony(InstanceId);

            Patch("TSPlayerStatusHolder", "ApplyHealthChange",
                  Prefix(nameof(GodHealthPrefix)), new[] { typeof(float) });

            Patch("TSPlayerStatusHolder", "GetDamage",
                  Prefix(nameof(GodDamagePrefix)), new[] { typeof(float), typeof(bool) });

            PatchPostfix("TSPlayerStatusHolder", "Update",
                         Postfix(nameof(VitalsPostfix)), nonPublic: true);

            Patch("Weapon", "Shot", Prefix(nameof(AmmoPrefix)));

            Patch("PlayerVitals", "UpdateStats",
                  Prefix(nameof(StaminaPrefix)), nonPublic: true);

            Patch("TrainController", "set_NetworknetworkFuelLevel",
                  Prefix(nameof(FuelPrefix)), new[] { typeof(float) });

            PatchPostfix("PlayerInventory", "Initialize",
                         Postfix(nameof(InventoryCapacityPostfix)),
                         nonPublic: true);

            PatchPrePost("CraftItemUI", "Craft",
                         Prefix(nameof(FreeCraftPrefix)),
                         Postfix(nameof(FreeCraftPostfix)));
        }

        private static bool GodHealthPrefix(float healthAmount)
            => !(Settings.GodMode.Value && healthAmount < 0f);

        private static bool GodDamagePrefix(float damage, bool isZombieHit)
            => !Settings.GodMode.Value;

        private static void AmmoPrefix(ref bool ___InfiniteAmmo)
        {
            if (Settings.InfiniteAmmo.Value) ___InfiniteAmmo = true;
        }

        private static bool StaminaPrefix()
            => !Settings.InfiniteStamina.Value;

        private static bool FuelPrefix(float value, ref float ___networkFuelLevel)
            => !(Settings.InfiniteFuel.Value && value < ___networkFuelLevel);

        private static void FreeCraftPrefix(object __instance, ref IList __state)
        {
            if (!Settings.FreeCraft.Value) return;
            try
            {
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

        private static void InventoryCapacityPostfix(object __instance)
        {
            if (!Settings.InfiniteInventoryCapacity.Value) return;
            try
            {
                ReflectionUtil.SetMemberValue(__instance, int.MaxValue,
                    "inventorySlotMaxCapacity");

                var slots = ReflectionUtil.GetMemberValue(__instance, "inventorySlotsData")
                            as System.Collections.IList;
                if (slots != null)
                {
                    foreach (var slot in slots)
                        ReflectionUtil.SetMemberValue(slot, int.MaxValue, "maxCapacity");
                }

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
                    MelonLogger.Warning("[Patch] type not found: " + typeName + " - " + methodName + " disabled.");
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
                    MelonLogger.Warning("[Patch] method not found: " + typeName + "." + methodName + " - disabled.");
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
                    MelonLogger.Warning("[Patch] type not found: " + typeName + " - " + methodName + " disabled.");
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
                    MelonLogger.Warning("[Patch] method not found: " + typeName + "." + methodName + " - disabled.");
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

        private static void PatchPrePost(string typeName, string methodName,
            HarmonyMethod prefix, HarmonyMethod postfix,
            Type[] paramTypes = null, bool nonPublic = false)
        {
            try
            {
                var t = ReflectionUtil.FindType(typeName);
                if (t == null)
                {
                    MelonLogger.Warning("[Patch] type not found: " + typeName + " - " + methodName + " disabled.");
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
                    MelonLogger.Warning("[Patch] method not found: " + typeName + "." + methodName + " - disabled.");
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

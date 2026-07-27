using System.Collections;
using MelonLoader;
using UnityEngine;

namespace OnTheTrainDemoCheat
{
    /// <summary>
    /// Button-triggered actions only. The continuous cheats (God/Stamina/Ammo/Fuel) are now
    /// static Harmony patches (see Patches.cs), so there is NO per-frame polling here — that
    /// was the cause of the previous FPS drop.
    /// </summary>
    internal static class Cheats
    {
        /// <summary>Best-effort: advance the game's day cycle via TrainGameManager.</summary>
        public static void SkipToMorning()
        {
            var mgr = ReflectionUtil.FindComponent<object>("TrainGameManager")
                   ?? ReflectionUtil.FindComponent<object>("TrainManager");

            if (mgr == null)
            {
                MelonLogger.Warning("[SkipToMorning] TrainGameManager not found.");
                return;
            }

            if (ReflectionUtil.InvokeMethod(mgr, "SkipToMorning", "SkipToDay", "ForceMorning") != null)
                return;

            var coroutine = ReflectionUtil.InvokeMethod(mgr, "SkipToMorningCoroutine", "SkipToMorningRoutine") as IEnumerator;
            if (coroutine != null)
            {
                var start = mgr.GetType().GetMethod("StartCoroutine", new[] { typeof(IEnumerator) });
                if (start != null)
                {
                    start.Invoke(mgr, new object[] { coroutine });
                    return;
                }
            }

            MelonLogger.Warning("[SkipToMorning] No suitable method found on TrainGameManager.");
        }

        /// <summary>Local player position from the cached inventory reference (no per-frame scanning).</summary>
        public static Vector3? GetPlayerPosition()
        {
            var inv = Items.LocalInventory as Component;
            return inv != null ? (Vector3?)inv.transform.position : null;
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Steamworks;
using UnityEngine;

namespace OnTheTrainDemoPublicLobby
{
    /// <summary>
    /// 公开大厅补丁：
    /// 1. Patch SteamLobby.HostLobby - 开启 PublicLobby 模式时改用 k_ELobbyTypePublic 创建大厅
    /// 2. Patch SteamLobby.GetLobbiesList - 额外调用 SteamMatchmaking.RequestLobbyList() 拉取所有公开大厅
    /// 3. 注册 LobbyMatchList_t 回调 - 把匹配到的公开大厅加入 SteamLobby.lobbyIDs，触发游戏原生 UI 显示
    ///
    /// 这样陌生人通过游戏主菜单"加入游戏"按钮就能搜到并加入公开大厅。
    /// </summary>
    internal static class PublicLobbyPatches
    {
        private static bool _initialized;
        private static Callback<LobbyMatchList_t> _lobbyMatchListCallback;

        /// <summary>初始化补丁和 Steam 回调。在 Main.OnInitializeMelon 中调用一次。</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 注册 LobbyMatchList_t 回调：RequestLobbyList 完成后触发
                _lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
                MelonLogger.Msg("[PublicLobby] LobbyMatchList callback registered.");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[PublicLobby] Init failed: " + e.Message);
            }
        }

        /// <summary>HostLobby Prefix：开启公开模式时强制用 Public 类型，跳过原方法。</summary>
        [HarmonyPatch(typeof(SteamLobby), "HostLobby")]
        private static class HostLobbyPatch
        {
            private static bool Prefix(SteamLobby __instance)
            {
                // 读取当前 lobbyMode 和 maxConnections 用于日志
                int lobbyMode = __instance.lobbyMode;
                int maxConnections = 16;
                try
                {
                    var lobbyType = typeof(SteamLobby);
                    var managerField = lobbyType.GetField("manager",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    object manager = managerField?.GetValue(__instance);
                    if (manager != null)
                    {
                        var maxField = manager.GetType().GetField("maxConnections",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (maxField != null && maxField.GetValue(manager) is int mc)
                            maxConnections = mc;
                    }
                }
                catch { }

                // v1.0.2：默认公开模式（除非用户在 cfg 中显式关闭）
                if (Settings.PublicLobby != null && !Settings.PublicLobby.Value)
                {
                    MelonLogger.Msg("[PublicLobby] HostLobby (original, public disabled by user) lobbyMode=" + lobbyMode +
                        " -> ELobbyType=" + (lobbyMode == 0 ? "k_ELobbyTypePrivate" : "k_ELobbyTypeFriendsOnly") +
                        " maxConnections=" + maxConnections);
                    return true;
                }

                try
                {
                    MelonLogger.Msg("[PublicLobby] HostLobby (PATCHED) forcing k_ELobbyTypePublic" +
                        " maxConnections=" + maxConnections +
                        " originalLobbyMode=" + lobbyMode);

                    // 调用 ConnectToServer（设置 isConnecting 标志，public 方法）
                    var connectMethod = typeof(SteamLobby).GetMethod("ConnectToServer",
                        BindingFlags.Public | BindingFlags.Instance);
                    connectMethod?.Invoke(__instance, null);

                    // 强制使用 k_ELobbyTypePublic
                    SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxConnections);
                    MelonLogger.Msg("[PublicLobby] CreateLobby(k_ELobbyTypePublic, " + maxConnections + ") called.");
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("[PublicLobby] HostLobby prefix failed, fallback to original: " + e.Message);
                    return true; // 出错时走原方法
                }

                return false; // 跳过原方法
            }
        }

        /// <summary>GetLobbiesList Prefix：在原方法执行前记录调用。</summary>
        [HarmonyPatch(typeof(SteamLobby), "GetLobbiesList")]
        private static class GetLobbiesListPatch
        {
            private static void Prefix(SteamLobby __instance)
            {
                MelonLogger.Msg("[PublicLobby] GetLobbiesList called (pre) - friend lobby scan begins.");
            }

            private static void Postfix(SteamLobby __instance)
            {
                int friendLobbies = __instance.lobbyIDs?.Count ?? 0;
                MelonLogger.Msg("[PublicLobby] GetLobbiesList done (post) - friend lobbies found: " + friendLobbies);

                // v1.0.2：默认公开模式（除非用户在 cfg 中显式关闭）
                if (Settings.PublicLobby != null && !Settings.PublicLobby.Value)
                {
                    MelonLogger.Msg("[PublicLobby] PublicLobby mode OFF (user disabled), skipping RequestLobbyList.");
                    return;
                }

                try
                {
                    // 添加过滤器：只搜 "game" == "OnTheTrain" 的大厅（游戏在 OnLobbyCreated 中设置了这个字段）
                    SteamMatchmaking.AddRequestLobbyListStringFilter("game", "OnTheTrain", ELobbyComparison.k_ELobbyComparisonEqual);
                    // 限制最多 50 个结果
                    SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);
                    // 请求公开大厅列表，结果通过 LobbyMatchList_t 回调返回
                    SteamMatchmaking.RequestLobbyList();
                    MelonLogger.Msg("[PublicLobby] RequestLobbyList sent (filter: game=OnTheTrain, max 50).");
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("[PublicLobby] RequestLobbyList failed: " + e.Message);
                }
            }
        }

        /// <summary>LobbyMatchList_t 回调：RequestLobbyList 返回时触发，把公开大厅加入 lobbyIDs。</summary>
        private static void OnLobbyMatchList(LobbyMatchList_t result)
        {
            try
            {
                var steamLobby = Singleton<SteamLobby>.Instance;
                if (steamLobby == null)
                {
                    MelonLogger.Warning("[PublicLobby] OnLobbyMatchList: SteamLobby instance is null.");
                    return;
                }

                int count = (int)result.m_nLobbiesMatching;
                MelonLogger.Msg("[PublicLobby] ===== OnLobbyMatchList: matched " + count + " public lobbies =====");

                // 更新 UI 调试信息
                PublicLobbyUI.UpdateSearchResult(count, steamLobby.lobbyIDs.Count);

                for (int i = 0; i < count; i++)
                {
                    CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
                    if (!lobbyID.IsValid())
                    {
                        MelonLogger.Msg("[PublicLobby]   [" + i + "] invalid lobby ID, skipped.");
                        continue;
                    }

                    // 读取大厅详细信息
                    string name = SteamMatchmaking.GetLobbyData(lobbyID, "name");
                    string game = SteamMatchmaking.GetLobbyData(lobbyID, "game");
                    string host = SteamMatchmaking.GetLobbyData(lobbyID, "HostAddress");
                    int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
                    int memberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);

                    MelonLogger.Msg("[PublicLobby]   [" + i + "] ID=" + lobbyID.m_SteamID +
                        " name='" + name + "'" +
                        " game='" + game + "'" +
                        " members=" + memberCount + "/" + memberLimit +
                        " host='" + host + "'");

                    // 检查是否已在列表中（避免与好友列表重复）
                    if (!steamLobby.lobbyIDs.Contains(lobbyID))
                    {
                        steamLobby.lobbyIDs.Add(lobbyID);
                        MelonLogger.Msg("[PublicLobby]   [" + i + "] added to lobbyIDs (total=" + steamLobby.lobbyIDs.Count + ")");
                    }
                    else
                    {
                        MelonLogger.Msg("[PublicLobby]   [" + i + "] already in lobbyIDs, skip add.");
                    }
                    // 请求大厅详情（name 字段），触发 OnGetLobbyData -> LobbiesListManager.DisplayLobbies
                    SteamMatchmaking.RequestLobbyData(lobbyID);
                }
                MelonLogger.Msg("[PublicLobby] ===== OnLobbyMatchList done, total lobbyIDs=" + steamLobby.lobbyIDs.Count + " =====");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[PublicLobby] OnLobbyMatchList failed: " + e.Message);
            }
        }
    }
}

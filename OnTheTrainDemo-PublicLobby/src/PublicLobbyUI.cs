using Steamworks;
using UnityEngine;

namespace OnTheTrainDemoPublicLobby
{
    /// <summary>
    /// 公开大厅控制面板 - 按 F8 打开/关闭。
    /// </summary>
    internal static class PublicLobbyUI
    {
        private const int WindowId = 0x7A88;
        private static Rect _window = new Rect(560, 60, 520, 460);
        private static bool _open;

        // 调试状态：用于在面板上显示最近一次搜索结果
        internal static int LastMatchedCount = -1;   // -1 = 未搜索过
        internal static int TotalLobbyIDs = 0;
        internal static string LastSearchTime = "";

        public static bool IsOpen => _open;

        public static void Toggle()
        {
            _open = !_open;
        }

        // 由 PublicLobbyPatches.OnLobbyMatchList 调用，更新调试信息
        internal static void UpdateSearchResult(int matched, int totalIDs)
        {
            LastMatchedCount = matched;
            TotalLobbyIDs = totalIDs;
            LastSearchTime = System.DateTime.Now.ToString("HH:mm:ss");
        }

        public static void Draw()
        {
            if (!_open) return;

            _window = GUILayout.Window(WindowId, _window, (id) =>
            {
                DrawContent();
                GUI.DragWindow(new Rect(0, 0, 10000, 24));
            }, "公开大厅面板 v1.0.1 - F8 关闭");
        }

        private static void DrawContent()
        {
            // 模式开关
            bool current = Settings.PublicLobby != null && Settings.PublicLobby.Value;
            GUILayout.Label("公开大厅模式：" + (current ? "已开启" : "已关闭"), GUI.skin.box);
            if (GUILayout.Button(current ? "关闭公开大厅" : "开启公开大厅"))
            {
                Settings.Toggle();
            }

            GUILayout.Space(8);

            // Steam 状态
            GUILayout.Label("Steam 状态：" + (SteamManager.Initialized ? "已连接" : "未连接"), GUI.skin.box);

            if (SteamManager.Initialized)
            {
                try
                {
                    var sid = SteamUser.GetSteamID();
                    GUILayout.Label("我的 Steam ID：" + sid.m_SteamID, GUI.skin.label);
                    GUILayout.Label("我的昵称：" + SteamFriends.GetPersonaName(), GUI.skin.label);
                    try
                    {
                        GUILayout.Label("AppID：" + SteamUtils.GetAppID().m_AppId, GUI.skin.label);
                    }
                    catch { }
                }
                catch { }
            }

            // 当前大厅状态
            try
            {
                var steamLobby = Singleton<SteamLobby>.Instance;
                if (steamLobby != null)
                {
                    GUILayout.Space(4);
                    GUILayout.Label("当前大厅 ID：" +
                        (steamLobby.CurrentLobbyID != 0 ? steamLobby.CurrentLobbyID.ToString() : "（未加入大厅）"),
                        GUI.skin.box);
                    GUILayout.Label("lobbyIDs 列表数量：" + (steamLobby.lobbyIDs?.Count ?? 0), GUI.skin.label);

                    GUILayout.Space(4);

                    // 操作按钮
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("邀请好友（Steam 覆盖层）"))
                    {
                        steamLobby.OpenInviteDialog();
                    }
                    if (GUILayout.Button("离开当前大厅"))
                    {
                        steamLobby.LeaveLobby();
                    }
                    GUILayout.EndHorizontal();

                    // 手动触发搜索（调用游戏的 GetLobbiesList，会触发我们的 Postfix）
                    if (GUILayout.Button("手动搜索大厅（触发 GetLobbiesList）"))
                    {
                        steamLobby.GetLobbiesList();
                    }
                }
                else
                {
                    GUILayout.Label("SteamLobby 尚未初始化（请先进入主菜单）", GUI.skin.box);
                }
            }
            catch { }

            GUILayout.Space(8);

            // 调试：搜索状态
            GUILayout.Label("── 搜索状态 ──", GUI.skin.label);
            if (LastMatchedCount < 0)
            {
                GUILayout.Label("尚未执行过公开大厅搜索", GUI.skin.label);
                GUILayout.Label("（点游戏「加入游戏」或上面「手动搜索」按钮）", GUI.skin.label);
            }
            else
            {
                GUILayout.Label("最近搜索时间：" + LastSearchTime, GUI.skin.label);
                GUILayout.Label("公开大厅匹配数：" + LastMatchedCount, GUI.skin.label);
                GUILayout.Label("lobbyIDs 总数：" + TotalLobbyIDs, GUI.skin.label);
            }

            GUILayout.Space(8);
            GUILayout.Label("说明：开启公开模式后，建主用 Public 类型；加入游戏时会额外调用 RequestLobbyList 拉取公开大厅。", GUI.skin.label);
        }
    }
}

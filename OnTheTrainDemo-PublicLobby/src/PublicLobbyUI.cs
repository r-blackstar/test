using Steamworks;
using UnityEngine;

namespace OnTheTrainDemoPublicLobby
{
    /// <summary>
    /// 公开大厅 UI v1.0.3：
    /// - 屏幕右侧显示一个侧边按钮，点击弹出大厅信息面板
    /// - 面板显示当前大厅 ID、成员列表、搜索状态等
    /// - 面板内提供「关闭公开大厅」按钮：运行时切换 PublicLobby 模式并保存配置
    ///   关闭后建主与搜索走游戏原方法，恢复好友大厅搜索行为
    /// </summary>
    internal static class PublicLobbyUI
    {
        private const int WindowId = 0x7A88;

        // 侧边按钮（始终显示在屏幕右侧）
        private static Rect _sideButton = new Rect(Screen.width - 40, Screen.height / 2 - 40, 32, 80);
        // 弹窗（点击按钮后显示）
        private static Rect _window = new Rect(80, 80, 480, 560);
        private static bool _open;
        private static Vector2 _memberScroll;

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
            // 始终绘制屏幕右侧侧边按钮
            DrawSideButton();

            // 弹窗（仅当 _open 时）
            if (!_open) return;

            _window = GUILayout.Window(WindowId, _window, (id) =>
            {
                DrawContent();
                GUI.DragWindow(new Rect(0, 0, 10000, 24));
            }, "公开大厅信息 v1.0.3");
        }

        private static void DrawSideButton()
        {
            // 自适应屏幕尺寸（右侧贴边）
            _sideButton.x = Screen.width - 40;
            if (_sideButton.y > Screen.height - 100)
                _sideButton.y = Screen.height / 2 - 40;

            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = _open ? new Color(0.4f, 0.7f, 1f, 1f) : new Color(0.2f, 0.5f, 0.85f, 0.85f);
            // 右侧按钮：开启时显示 ◀（点击向左收起），关闭时显示 ▶（点击向左展开）
            var label = _open ? "▶" : "◀";
            if (GUI.Button(_sideButton, label, GUI.skin.box))
            {
                _open = !_open;
            }
            GUI.backgroundColor = oldBg;
        }

        private static void DrawContent()
        {
            // 公开模式状态与开关
            DrawPublicModeToggle();

            GUILayout.Space(6);

            // Steam 状态
            GUILayout.Label("Steam 状态：" + (SteamManager.Initialized ? "已连接" : "未连接"), GUI.skin.box);

            if (SteamManager.Initialized)
            {
                try
                {
                    GUILayout.Label("我的昵称：" + SteamFriends.GetPersonaName(), GUI.skin.label);
                    GUILayout.Label("我的 Steam ID：" + SteamUser.GetSteamID().m_SteamID, GUI.skin.label);
                }
                catch { }
            }

            GUILayout.Space(6);

            // 当前大厅状态
            try
            {
                var steamLobby = Singleton<SteamLobby>.Instance;
                if (steamLobby != null)
                {
                    GUILayout.Label("当前大厅 ID：" +
                        (steamLobby.CurrentLobbyID != 0 ? steamLobby.CurrentLobbyID.ToString() : "（未加入大厅）"),
                        GUI.skin.box);

                    if (steamLobby.CurrentLobbyID != 0)
                    {
                        DrawCurrentLobbyInfo(new CSteamID(steamLobby.CurrentLobbyID));
                    }

                    GUILayout.Space(4);
                    GUILayout.Label("可加入大厅列表数量：" + (steamLobby.lobbyIDs?.Count ?? 0), GUI.skin.label);

                    GUILayout.Space(4);

                    // 操作按钮
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("邀请好友"))
                    {
                        steamLobby.OpenInviteDialog();
                    }
                    if (GUILayout.Button("离开当前大厅"))
                    {
                        steamLobby.LeaveLobby();
                    }
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("手动搜索大厅"))
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

            // 搜索状态
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
            if (Settings.PublicLobby != null && Settings.PublicLobby.Value)
            {
                GUILayout.Label("说明：公开模式已开启，建主用 Public 类型，陌生人可搜到。", GUI.skin.label);
                GUILayout.Label("点击上方「关闭公开大厅」可恢复游戏原生行为。", GUI.skin.label);
            }
            else
            {
                GUILayout.Label("说明：公开模式已关闭，建主与搜索走游戏原方法（仅好友）。", GUI.skin.label);
                GUILayout.Label("点击上方「开启公开大厅」可重新启用模组。", GUI.skin.label);
            }
        }

        /// <summary>绘制公开模式开关按钮，运行时切换并保存配置。</summary>
        private static void DrawPublicModeToggle()
        {
            bool isOn = Settings.PublicLobby != null && Settings.PublicLobby.Value;
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = isOn ? new Color(0.85f, 0.3f, 0.3f, 1f) : new Color(0.3f, 0.75f, 0.4f, 1f);
            string btnText = isOn ? "关闭公开大厅" : "开启公开大厅";
            if (GUILayout.Button(btnText, GUILayout.Height(32)))
            {
                Settings.Toggle();
            }
            GUI.backgroundColor = oldBg;

            GUILayout.Label("当前状态：" + (isOn ? "● 公开模式（陌生人可搜到）" : "○ 好友模式（游戏原生）"), GUI.skin.label);
        }

        /// <summary>显示当前大厅详细信息：成员列表、大厅数据等。</summary>
        private static void DrawCurrentLobbyInfo(CSteamID lobbyID)
        {
            try
            {
                int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
                int memberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
                string lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");
                string hostAddr = SteamMatchmaking.GetLobbyData(lobbyID, "HostAddress");

                GUILayout.Space(4);
                GUILayout.Label("大厅名称：" + (string.IsNullOrEmpty(lobbyName) ? "(未设置)" : lobbyName), GUI.skin.label);
                GUILayout.Label("房主地址：" + (string.IsNullOrEmpty(hostAddr) ? "(未知)" : hostAddr), GUI.skin.label);
                GUILayout.Label("成员数：" + memberCount + " / " + memberLimit, GUI.skin.label);

                GUILayout.Space(4);
                GUILayout.Label("── 成员列表 ──", GUI.skin.label);

                _memberScroll = GUILayout.BeginScrollView(_memberScroll, GUILayout.Height(140));
                for (int i = 0; i < memberCount; i++)
                {
                    CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
                    if (!member.IsValid()) continue;

                    string name = SteamFriends.GetFriendPersonaName(member);
                    bool isOwner = SteamMatchmaking.GetLobbyOwner(lobbyID) == member;
                    string tag = isOwner ? " [房主]" : "";
                    GUILayout.Label((i + 1) + ". " + name + tag + "  (" + member.m_SteamID + ")", GUI.skin.label);
                }
                if (memberCount == 0)
                {
                    GUILayout.Label("（暂无成员）", GUI.skin.label);
                }
                GUILayout.EndScrollView();
            }
            catch (System.Exception e)
            {
                GUILayout.Label("读取大厅信息失败：" + e.Message, GUI.skin.label);
            }
        }
    }
}

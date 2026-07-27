using MelonLoader;
using Steamworks;
using UnityEngine;

[assembly: MelonInfo(typeof(OnTheTrainDemoPublicLobby.Main), "On The Train Demo Public Lobby", "1.0.1", "DestinyWind")]
[assembly: MelonGame("EastUpInteractive", "On The Train Demo")]

namespace OnTheTrainDemoPublicLobby
{
    /// <summary>
    /// On The Train Demo 公开大厅模组。
    ///
    /// 游戏原生 HostLobby 用 (ELobbyType)(lobbyMode==0) 创建大厅，只能得到 Private/FriendsOnly，
    /// 陌生人无法搜到。本模组：
    ///   1. Patch SteamLobby.HostLobby - 开启公开模式时改用 k_ELobbyTypePublic
    ///   2. Patch SteamLobby.GetLobbiesList - 额外调用 RequestLobbyList 拉取所有公开大厅
    ///   3. 注册 LobbyMatchList_t 回调 - 把公开大厅加入 lobbyIDs 触发游戏原生 UI 显示
    ///
    /// 快捷键：F8 打开/关闭控制面板。
    /// </summary>
    public class Main : MelonMod
    {
        private readonly KeyCode _panelKey = KeyCode.F8;

        public override void OnInitializeMelon()
        {
            Settings.Register();
            PublicLobbyPatches.Initialize();

            MelonLogger.Msg("[PublicLobby] ===== On The Train Demo Public Lobby v1.0.1 loaded =====");
            MelonLogger.Msg("[PublicLobby] Press F8 to toggle the public lobby panel.");
            MelonLogger.Msg("[PublicLobby] Public mode is " + (Settings.PublicLobby.Value ? "ON" : "OFF") + " by default.");
            MelonLogger.Msg("[PublicLobby] SteamManager.Initialized = " + SteamManager.Initialized);
            try
            {
                if (SteamManager.Initialized)
                {
                    MelonLogger.Msg("[PublicLobby] AppID = " + SteamUtils.GetAppID().m_AppId);
                    MelonLogger.Msg("[PublicLobby] My SteamID = " + SteamUser.GetSteamID().m_SteamID);
                    MelonLogger.Msg("[PublicLobby] PersonaName = " + SteamFriends.GetPersonaName());
                }
            }
            catch { }
            MelonLogger.Msg("[PublicLobby] ============================================");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(_panelKey))
                PublicLobbyUI.Toggle();
        }

        public override void OnGUI()
        {
            PublicLobbyUI.Draw();
        }
    }
}

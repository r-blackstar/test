using MelonLoader;

namespace OnTheTrainDemoPublicLobby
{
    /// <summary>模组设置：公开大厅模式开关。</summary>
    internal static class Settings
    {
        private static MelonPreferences_Category _category;

        /// <summary>公开大厅模式：开启后 HostLobby 改用 k_ELobbyTypePublic，陌生人能通过 RequestLobbyList 搜到。</summary>
        public static MelonPreferences_Entry<bool> PublicLobby;

        public static void Register()
        {
            _category = MelonPreferences.CreateCategory("OnTheTrainDemoPublicLobby", "On The Train Demo Public Lobby");
            PublicLobby = _category.CreateEntry(nameof(PublicLobby), false, "Public Lobby Mode (strangers can find & join)");
        }

        /// <summary>切换公开大厅模式并保存配置。</summary>
        public static void Toggle()
        {
            if (PublicLobby == null) return;
            PublicLobby.Value = !PublicLobby.Value;
            MelonPreferences.Save();
            MelonLogger.Msg("[PublicLobby] Mode toggled to: " + PublicLobby.Value);
        }
    }
}

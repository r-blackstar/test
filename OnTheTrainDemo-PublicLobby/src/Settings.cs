using MelonLoader;

namespace OnTheTrainDemoPublicLobby
{
    /// <summary>模组设置：v1.0.2 起默认开启公开大厅，无需手动启用。</summary>
    internal static class Settings
    {
        private static MelonPreferences_Category _category;

        /// <summary>
        /// 公开大厅模式：v1.0.2 起默认 true，安装即生效。
        /// 仍保留配置项供高级用户在 cfg 中关闭。
        /// </summary>
        public static MelonPreferences_Entry<bool> PublicLobby;

        public static void Register()
        {
            _category = MelonPreferences.CreateCategory("OnTheTrainDemoPublicLobby", "On The Train Demo Public Lobby");
            // 默认 true：安装即公开
            PublicLobby = _category.CreateEntry(nameof(PublicLobby), true, "Public Lobby Mode (strangers can find & join)");
        }

        /// <summary>切换公开大厅模式并保存配置（高级用户用）。</summary>
        public static void Toggle()
        {
            if (PublicLobby == null) return;
            PublicLobby.Value = !PublicLobby.Value;
            MelonPreferences.Save();
            MelonLogger.Msg("[PublicLobby] Mode toggled to: " + PublicLobby.Value);
        }
    }
}

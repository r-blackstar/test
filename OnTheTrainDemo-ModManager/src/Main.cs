using System.Collections.ObjectModel;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(OnTheTrainDemoModManager.Main), "On The Train Demo Mod Manager", "1.0.0", "DestinyWind")]
[assembly: MelonGame("EastUpInteractive", "On The Train Demo")]

namespace OnTheTrainDemoModManager
{
    /// <summary>
    /// 游戏内模组管理器 v1.0.0：
    /// - 按 F1 显示/关闭模组管理器面板
    /// - 列出所有已加载成功的 MelonLoader 模组（名称、版本、作者、所在 DLL）
    /// </summary>
    public class Main : MelonMod
    {
        private readonly KeyCode _toggleKey = KeyCode.F1;
        private bool _open;
        private Rect _window = new Rect(40, 40, 640, 520);
        private Vector2 _scroll;
        private string _searchText = "";

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[ModManager] ===== On The Train Demo Mod Manager v1.0.0 loaded =====");
            MelonLogger.Msg("[ModManager] Press F1 to toggle the mod manager panel.");
            int count = MelonMod.RegisteredMelons != null ? MelonMod.RegisteredMelons.Count : 0;
            MelonLogger.Msg("[ModManager] Current loaded mods: " + count);
            for (int i = 0; i < count; i++)
            {
                var m = MelonMod.RegisteredMelons[i];
                MelonLogger.Msg("[ModManager]   [" + i + "] " + m.Info.Name + " v" + m.Info.Version + " by " + m.Info.Author);
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(_toggleKey))
                _open = !_open;
        }

        public override void OnGUI()
        {
            if (!_open) return;

            _window = GUILayout.Window(0x7B11, _window, (id) =>
            {
                DrawContent();
                GUI.DragWindow(new Rect(0, 0, 10000, 24));
            }, "模组管理器 v1.0.0 - F1 关闭");
        }

        private void DrawContent()
        {
            ReadOnlyCollection<MelonMod> mods = MelonMod.RegisteredMelons;
            int total = mods != null ? mods.Count : 0;

            // 顶部信息
            GUILayout.Label("已加载模组数：" + total, GUI.skin.box);
            try
            {
                GUILayout.Label("MelonLoader 版本：" + MelonLoader.Properties.BuildInfo.Version, GUI.skin.label);
            }
            catch { }
            GUILayout.Label("游戏：On The Train Demo (EastUpInteractive)", GUI.skin.label);

            GUILayout.Space(4);

            // 搜索框
            GUILayout.BeginHorizontal();
            GUILayout.Label("过滤：", GUILayout.Width(40));
            _searchText = GUILayout.TextField(_searchText, GUILayout.Width(200));
            if (GUILayout.Button("清空", GUILayout.Width(50)))
                _searchText = "";
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // 模组列表
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(330));

            if (total == 0)
            {
                GUILayout.Label("（未加载任何模组）", GUI.skin.label);
            }
            else
            {
                int shown = 0;
                for (int i = 0; i < total; i++)
                {
                    var m = mods[i];
                    string name = m.Info?.Name ?? "(unknown)";
                    string version = m.Info?.Version ?? "?";
                    string author = m.Info?.Author ?? "?";
                    string location = "";
                    try { location = m.MelonAssembly?.Location ?? ""; } catch { }

                    // 过滤
                    if (!string.IsNullOrEmpty(_searchText) &&
                        !name.ToLowerInvariant().Contains(_searchText.ToLowerInvariant()) &&
                        !author.ToLowerInvariant().Contains(_searchText.ToLowerInvariant()))
                        continue;

                    shown++;
                    string shortPath = string.IsNullOrEmpty(location) ? "(内置)" :
                        System.IO.Path.GetFileName(location);

                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.BeginVertical();
                    GUILayout.Label(shown + ". " + name + "  v" + version, GUI.skin.label);
                    GUILayout.Label("   作者：" + author, GUI.skin.label);
                    GUILayout.Label("   文件：" + shortPath, GUI.skin.label);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

                if (shown == 0 && !string.IsNullOrEmpty(_searchText))
                {
                    GUILayout.Label("（无匹配结果）", GUI.skin.label);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.Label("提示：F1 显示/关闭 | 此面板由 OnTheTrainDemoModManager 提供", GUI.skin.label);
        }
    }
}

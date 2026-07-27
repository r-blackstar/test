using System.Collections.Generic;
using UnityEngine;

namespace OnTheTrainDemoCheat
{
    /// <summary>
    /// IMGUI overlay + trainer window. Drawn every frame in OnGUI.
    /// 所有可见文案都从 I18n 取，方便多语言切换。按 F6 切换菜单。
    /// </summary>
    internal static class MenuUI
    {
        private const int WindowId = 0x1337;
        private static Rect _window = new Rect(20, 20, 360, 700);

        private static string _itemName = "Wood";
        private static string _amount = "50";

        // 语言列表缓存：仅在菜单打开时刷新一次，避免每帧扫描目录。
        private static List<KeyValuePair<string, string>> _langList;
        private static int _langIndex;

        public static void Draw(ref bool menuOpen)
        {
            if (Settings.ShowOverlay.Value)
                DrawOverlay();

            if (!menuOpen)
                return;

            _window = GUILayout.Window(WindowId, _window, (id) =>
            {
                GUILayout.Label(I18n.Get("menu.header"), GUI.skin.box);

                GUILayout.Label(I18n.Get("section.cheats"), GUI.skin.box);
                Settings.GodMode.Value         = GUILayout.Toggle(Settings.GodMode.Value,         I18n.Get("cheat.godmode"));
                Settings.InfiniteVitals.Value  = GUILayout.Toggle(Settings.InfiniteVitals.Value,  I18n.Get("cheat.vitals"));
                Settings.InfiniteStamina.Value = GUILayout.Toggle(Settings.InfiniteStamina.Value, I18n.Get("cheat.stamina"));
                Settings.InfiniteAmmo.Value    = GUILayout.Toggle(Settings.InfiniteAmmo.Value,    I18n.Get("cheat.ammo"));
                Settings.InfiniteFuel.Value    = GUILayout.Toggle(Settings.InfiniteFuel.Value,    I18n.Get("cheat.fuel"));
                Settings.InfiniteInventoryCapacity.Value = GUILayout.Toggle(Settings.InfiniteInventoryCapacity.Value, I18n.Get("cheat.inventory"));
                Settings.FreeCraft.Value       = GUILayout.Toggle(Settings.FreeCraft.Value,       I18n.Get("cheat.freecraft"));
                Settings.ShowOverlay.Value     = GUILayout.Toggle(Settings.ShowOverlay.Value,     I18n.Get("cheat.overlay"));

                GUILayout.Space(4);
                if (GUILayout.Button(I18n.Get("action.skip_morning")))
                    Cheats.SkipToMorning();

                GUILayout.Space(8);
                GUILayout.Label(I18n.Get("section.items"), GUI.skin.box);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(I18n.Get("quick.wood")))   Items.Give("Wood", 50);
                if (GUILayout.Button(I18n.Get("quick.stone")))  Items.Give("Stone", 50);
                if (GUILayout.Button(I18n.Get("quick.coal")))   Items.Give("Coal", 50);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(I18n.Get("quick.iron")))   Items.Give("Iron", 50);
                if (GUILayout.Button(I18n.Get("quick.copper"))) Items.Give("Copper", 50);
                if (GUILayout.Button(I18n.Get("quick.stick")))  Items.Give("Stick", 50);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _itemName = GUILayout.TextField(_itemName, 16, GUILayout.Width(150));
                GUILayout.Label("x", GUILayout.Width(12));
                _amount = GUILayout.TextField(_amount, 4, GUILayout.Width(40));
                if (GUILayout.Button(I18n.Get("action.give")))
                {
                    if (int.TryParse(_amount, out int a) && a > 0)
                        Items.Give(_itemName.Trim(), a);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(I18n.Get("action.list_items"))) Items.ListItemNames();
                if (GUILayout.Button(I18n.Get("action.gather")))     Items.GatherNearby();
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                DrawLanguageSelector();

                GUILayout.Space(6);
                GUILayout.Label(I18n.Get("hint.toggle"), GUI.skin.box);

                GUI.DragWindow(new Rect(0, 0, 10000, 24));
            }, I18n.Get("window.title"));
        }

        /// <summary>
        /// 语言选择器：列出 Mods/lang/*.json 中所有可用语言（按代码排序），
        /// 点击按钮即时切换并写回配置。每次菜单打开时刷新一次列表。
        /// </summary>
        private static void DrawLanguageSelector()
        {
            // 首次或语言列表为空时刷新
            if (_langList == null || _langList.Count == 0)
                RefreshLangList();

            GUILayout.Label(I18n.Get("section.language"), GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Get("language.current") + I18n.CurrentDisplayName, GUILayout.Width(220));
            if (GUILayout.Button(I18n.Get("action.reload_lang"), GUILayout.Width(120)))
                I18n.Reload();
            GUILayout.EndHorizontal();

            if (_langList == null || _langList.Count <= 1)
                return;

            // 多语言按钮网格：每个按钮显示该语言的显示名，点击切换。
            var labels = new string[_langList.Count];
            for (int i = 0; i < _langList.Count; i++)
            {
                labels[i] = _langList[i].Value;
                if (_langList[i].Key == I18n.CurrentLanguage)
                {
                    labels[i] = "▶ " + labels[i];   // 标记当前选中
                    _langIndex = i;
                }
            }

            int newIdx = GUILayout.SelectionGrid(_langIndex, labels, 3);
            if (newIdx != _langIndex && newIdx >= 0 && newIdx < _langList.Count)
            {
                var target = _langList[newIdx].Key;
                I18n.SwitchTo(target);
                _langIndex = newIdx;
                // 切换后刷新按钮文案（当前语言标记会移动）
                RefreshLangList();
            }
        }

        private static void RefreshLangList()
        {
            _langList = new List<KeyValuePair<string, string>>();
            foreach (var kv in I18n.AvailableLanguages)
                _langList.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
            _langList.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            _langIndex = -1;
            for (int i = 0; i < _langList.Count; i++)
            {
                if (_langList[i].Key == I18n.CurrentLanguage)
                {
                    _langIndex = i;
                    break;
                }
            }
        }

        private static void DrawOverlay()
        {
            var fps = Time.deltaTime > 0f ? 1f / Time.deltaTime : 0f;
            var rect = new Rect(Screen.width - 230, 10, 220, 70);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(I18n.Get("overlay.fps") + fps.ToString("0"));
            var pos = Cheats.GetPlayerPosition();
            GUILayout.Label(pos.HasValue
                ? I18n.Get("overlay.pos") + string.Format("{0:0.0} , {1:0.0} , {2:0.0}", pos.Value.x, pos.Value.y, pos.Value.z)
                : I18n.Get("overlay.pos") + I18n.Get("overlay.no_player"));
            GUILayout.EndArea();
        }
    }
}

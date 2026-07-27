using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;

namespace OnTheTrainDemoCheat
{
    /// <summary>
    /// 物品浏览器 UI v1.0 - 按 F5 打开/关闭。
    /// 显示所有已加载的 CollectableItemData，支持搜索过滤。
    /// 点击物品按钮 = 给予一个格子堆满的数量（按 itemSizeType 自动计算）。
    /// Shift+点击 = 给予10倍堆叠上限（多格）；右键 = 给予1个。
    /// </summary>
    internal static class ItemBrowserUI
    {
        private const int WindowId = 0x7A11;

        private static Rect _window = new Rect(60, 60, 540, 560);
        private static string _search = "";
        private static Vector2 _scroll;
        private static List<Items.ItemEntry> _items;
        private static List<Items.ItemEntry> _filtered;
        private static float _lastRefresh;
        private static string _lastSearch = "\0";
        private static GUIStyle _smallStyle;

        public static void Draw(ref bool browserOpen)
        {
            if (!browserOpen) return;

            // 每帧刷新物品列表太昂贵，每 2 秒刷新一次
            if (_items == null || Time.unscaledTime - _lastRefresh > 2f)
            {
                _items = Items.GetAllItems();
                _filtered = null;
                _lastRefresh = Time.unscaledTime;
            }

            // 搜索框变化时重新过滤
            if (_filtered == null || _search != _lastSearch)
            {
                _filtered = FilterItems(_items, _search);
                _lastSearch = _search;
            }

            _window = GUILayout.Window(WindowId, _window, (id) =>
            {
                // v1.5.8：异常保护，避免 IMGUI layout stack 损坏导致窗口永久不可用
                try
                {
                    DrawHeader();
                    DrawSearchBar();
                    DrawItemList();
                    GUILayout.Label(I18n.Get("browser.hint"), GUI.skin.box);
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("[ItemBrowserUI] draw failed: " + e.Message);
                    // 尽力恢复 IMGUI 平衡
                    try { GUILayout.EndHorizontal(); } catch { }
                    try { GUILayout.EndScrollView(); } catch { }
                }
                GUI.DragWindow(new Rect(0, 0, 10000, 24));
            }, I18n.Get("browser.title"));
        }

        private static void DrawHeader()
        {
            GUILayout.Label(I18n.Get("browser.header"), GUI.skin.box);
            GUILayout.Label(string.Format(I18n.Get("browser.count"),
                _filtered?.Count ?? 0, _items?.Count ?? 0));
        }

        private static void DrawSearchBar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Get("browser.search"), GUILayout.Width(50));
            string newSearch = GUILayout.TextField(_search, 32);
            if (newSearch != _search)
            {
                _search = newSearch;
                _filtered = FilterItems(_items, _search);
                _lastSearch = _search;
            }
            if (GUILayout.Button(I18n.Get("browser.clear"), GUILayout.Width(60)))
            {
                _search = "";
                _filtered = FilterItems(_items, _search);
                _lastSearch = _search;
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawItemList()
        {
            if (_filtered == null || _filtered.Count == 0)
            {
                GUILayout.Label(I18n.Get("browser.empty"), GUI.skin.box);
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            // 按分类 tier 分组渲染：每组先显示分类标题，再以多列布局显示该组物品
            // 分组顺序 = tier 升序（基础材料 → 后期特殊）
            const int columns = 2;
            int currentTier = -1;
            int itemsInRow = 0;
            bool inHorizontal = false;

            for (int i = 0; i < _filtered.Count; i++)
            {
                var entry = _filtered[i];
                int tier = Items.GetItemTier(entry.ItemName);

                // 进入新分类：先结束上一行（补齐空位），再显示分类标题
                if (tier != currentTier)
                {
                    if (inHorizontal)
                    {
                        while (itemsInRow < columns)
                        {
                            GUILayout.Button("", GUI.skin.label, GUILayout.Width(240), GUILayout.Height(24));
                            itemsInRow++;
                        }
                        GUILayout.EndHorizontal();
                        inHorizontal = false;
                        itemsInRow = 0;
                    }
                    currentTier = tier;
                    GUILayout.Label(I18n.Get("tier." + tier), GUI.skin.box);
                }

                // 行首：开始新行
                if (itemsInRow == 0)
                {
                    GUILayout.BeginHorizontal();
                    inHorizontal = true;
                }

                DrawItemButton(entry);
                itemsInRow++;

                // 行满：结束当前行
                if (itemsInRow >= columns)
                {
                    GUILayout.EndHorizontal();
                    inHorizontal = false;
                    itemsInRow = 0;
                }
            }

            // 最后收尾：补齐最后一行空位
            if (inHorizontal)
            {
                while (itemsInRow < columns)
                {
                    GUILayout.Button("", GUI.skin.label, GUILayout.Width(240), GUILayout.Height(24));
                    itemsInRow++;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private static void DrawItemButton(Items.ItemEntry entry)
        {
            if (_smallStyle == null)
            {
                _smallStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };
            }

            string label = entry.DisplayName + " [" + entry.StackLimit + "]";
            if (GUILayout.Button(label, _smallStyle, GUILayout.Width(240), GUILayout.Height(24)))
            {
                Event e = Event.current;
                if (e != null && e.button == 1)
                {
                    // 右键 = 给 1 个
                    Items.Give(entry.ItemName, 1);
                }
                else if (e != null && e.shift)
                {
                    // Shift+点击 = 给10倍堆叠
                    // v1.5.8：防止 int.MaxValue * 10 溢出为负数
                    int giveAmount = entry.StackLimit;
                    if (giveAmount > 0 && giveAmount <= int.MaxValue / 10)
                        giveAmount *= 10;
                    // 否则保持原值（已接近 int.MaxValue）
                    Items.Give(entry.ItemName, giveAmount);
                }
                else
                {
                    // 普通点击 = 给一格堆满
                    Items.GiveStack(entry.ItemName);
                }
            }
        }

        private static List<Items.ItemEntry> FilterItems(List<Items.ItemEntry> source, string keyword)
        {
            if (source == null) return new List<Items.ItemEntry>();
            if (string.IsNullOrEmpty(keyword))
                return new List<Items.ItemEntry>(source);

            var kw = keyword.Trim().ToLowerInvariant();
            return source.Where(e =>
                (e.ItemName != null && e.ItemName.ToLowerInvariant().Contains(kw)) ||
                (e.DisplayName != null && e.DisplayName.ToLowerInvariant().Contains(kw))
            ).ToList();
        }
    }
}

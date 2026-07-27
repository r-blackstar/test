using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace OnTheTrainDemoMod
{
    /// <summary>
    /// 模组菜单的国际化支持。
    ///
    /// 语言文件位于模组 DLL 旁边的 lang/ 目录，每个文件是一个扁平 JSON 对象
    /// （string -&gt; string）。模组同时把 zh-CN.json 和 en-US.json 作为内嵌资源打包，
    /// 首次启动时释放到磁盘，方便用户直接编辑或新增语言。
    ///
    /// 加载优先级：外部文件 &gt; 内嵌资源 &gt; 内置英文兜底。
    /// </summary>
    internal static class I18n
    {
        private const string ResPrefix = "OnTheTrainDemoMod.lang.";
        private const string FallbackLang = "en-US";

        private static readonly Dictionary<string, string> _strings =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static string _currentLang = "zh-CN";

        /// <summary>所有已发现语言的代码 -&gt; 显示名（从 _lang_name 读取，找不到则用代码本身）。</summary>
        private static readonly Dictionary<string, string> _availableLangs =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>模组 DLL 所在目录（用于定位 lang/ 文件夹）。</summary>
        private static string ModDir
        {
            get
            {
                var p = Assembly.GetExecutingAssembly().Location;
                return string.IsNullOrEmpty(p) ? "" : Path.GetDirectoryName(p) ?? "";
            }
        }

        private static string LangDir  => Path.Combine(ModDir, "lang");
        private static string LangPath(string code) => Path.Combine(LangDir, code + ".json");

        public static string CurrentLanguage => _currentLang;

        /// <summary>当前语言的显示名（如"简体中文"）。未加载时返回代码本身。</summary>
        public static string CurrentDisplayName =>
            _availableLangs.ContainsKey(_currentLang) ? _availableLangs[_currentLang] : _currentLang;

        /// <summary>所有已发现语言（代码 -&gt; 显示名），按代码排序。</summary>
        public static IReadOnlyDictionary<string, string> AvailableLanguages => _availableLangs;

        /// <summary>按 Settings.Language 加载语言；找不到则回退到内嵌英文。</summary>
        public static void Load(string langCode)
        {
            ScanAvailableLanguages();
            _currentLang = string.IsNullOrEmpty(langCode) ? "zh-CN" : langCode;
            _strings.Clear();

            if (TryLoadFromFile(LangPath(_currentLang), out var dict, out var displayName) && dict.Count > 0)
            {
                Merge(dict);
                if (!string.IsNullOrEmpty(displayName))
                    _availableLangs[_currentLang] = displayName;
                MelonLogger.Msg(Get("log.lang.loaded"), _currentLang, _strings.Count);
                return;
            }

            if (TryLoadFromResource(ResPrefix + _currentLang + ".json", out dict, out displayName) && dict.Count > 0)
            {
                Merge(dict);
                if (!string.IsNullOrEmpty(displayName))
                    _availableLangs[_currentLang] = displayName;
                MelonLogger.Msg(Get("log.lang.loaded"), _currentLang, _strings.Count);
                return;
            }

            if (TryLoadFromResource(ResPrefix + FallbackLang + ".json", out dict, out displayName) && dict.Count > 0)
            {
                Merge(dict);
                if (!string.IsNullOrEmpty(displayName))
                    _availableLangs[FallbackLang] = displayName;
                MelonLogger.Warning(Get("log.lang.fallback"), _currentLang);
                _currentLang = FallbackLang;
                return;
            }

            // 终极兜底（理论上不会到这）：硬编码几条最关键的英文
            _strings["window.title"] = "On The Train Demo Mod";
        }

        /// <summary>重新从磁盘读取当前语言文件。菜单按钮调用。</summary>
        public static void Reload()
        {
            ScanAvailableLanguages();
            var code = _currentLang;
            _strings.Clear();

            if (TryLoadFromFile(LangPath(code), out var dict, out var displayName) && dict.Count > 0)
            {
                Merge(dict);
                if (!string.IsNullOrEmpty(displayName))
                    _availableLangs[code] = displayName;
                MelonLogger.Msg(Get("log.lang.reloaded"), code, _strings.Count);
                return;
            }
            if (TryLoadFromResource(ResPrefix + code + ".json", out dict, out displayName) && dict.Count > 0)
            {
                Merge(dict);
                if (!string.IsNullOrEmpty(displayName))
                    _availableLangs[code] = displayName;
                MelonLogger.Msg(Get("log.lang.reloaded"), code, _strings.Count);
                return;
            }
            if (TryLoadFromResource(ResPrefix + FallbackLang + ".json", out dict, out displayName) && dict.Count > 0)
            {
                Merge(dict);
                if (!string.IsNullOrEmpty(displayName))
                    _availableLangs[FallbackLang] = displayName;
                MelonLogger.Msg(Get("log.lang.reloaded"), FallbackLang, _strings.Count);
            }
        }

        /// <summary>
        /// 切换到指定语言代码并即时生效。同时把选择写回 Settings.Language，下次启动自动加载。
        /// </summary>
        public static void SwitchTo(string langCode)
        {
            if (string.IsNullOrEmpty(langCode) || langCode == _currentLang) return;
            Load(langCode);
            try
            {
                if (Settings.Language != null)
                {
                    Settings.Language.Value = langCode;
                    MelonPreferences.Save();
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[I18n] save Settings.Language failed: " + e.Message);
            }
        }

        /// <summary>扫描 Mods/lang/*.json 目录，更新 _availableLangs（代码 -&gt; 显示名）。</summary>
        private static void ScanAvailableLanguages()
        {
            try
            {
                // 内嵌的两个语言总是可用
                EnsureLanguageEntry("zh-CN", null);
                EnsureLanguageEntry("en-US", null);

                if (!Directory.Exists(LangDir)) return;
                foreach (var path in Directory.GetFiles(LangDir, "*.json"))
                {
                    string code = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrEmpty(code)) continue;
                    string displayName = null;
                    try
                    {
                        if (TryLoadFromFile(path, out var dict, out displayName) && dict != null
                            && !string.IsNullOrEmpty(displayName))
                        {
                            _availableLangs[code] = displayName;
                        }
                    }
                    catch { }
                    EnsureLanguageEntry(code, displayName);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[I18n] scan lang dir failed: " + e.Message);
            }
        }

        private static void EnsureLanguageEntry(string code, string displayName)
        {
            if (string.IsNullOrEmpty(code)) return;
            if (!_availableLangs.ContainsKey(code))
                _availableLangs[code] = string.IsNullOrEmpty(displayName) ? code : displayName;
        }

        /// <summary>首次启动时把内嵌语言文件释放到磁盘，便于用户编辑。</summary>
        public static void ExtractEmbeddedFiles()
        {
            try
            {
                Directory.CreateDirectory(LangDir);
                foreach (var name in new[] { "zh-CN", "en-US" })
                {
                    var resName = ResPrefix + name + ".json";
                    var path    = LangPath(name);
                    if (File.Exists(path)) continue;

                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
                    {
                        if (stream == null) continue;
                        using (var sr = new StreamReader(stream, Encoding.UTF8))
                        using (var fs = new StreamWriter(path, false, new UTF8Encoding(false)))
                        {
                            fs.Write(sr.ReadToEnd());
                        }
                    }
                    MelonLogger.Msg(Get("log.lang.extracted"), path);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[I18n] extract failed: " + e.Message);
            }
        }

        /// <summary>取文案。{0} {1} ... 用 args 填充。未找到时返回 key 本身。</summary>
        public static string Get(string key, params object[] args)
        {
            string s;
            if (!_strings.TryGetValue(key, out s) || string.IsNullOrEmpty(s))
                s = key;
            return (args != null && args.Length > 0) ? string.Format(s, args) : s;
        }

        /// <summary>
        /// 大小写不敏感的查找：依次尝试 key 原样、key.ToLower()、首字母大写形式。
        /// 找不到任一变体时返回 null（调用方可用原始 key 自行兜底）。
        /// 用于物品名等大小写不固定的场景（游戏 itemName 可能是 "Wood"/"wood"/"WOOD"）。
        /// </summary>
        public static string GetIgnoreCase(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // 1. 原样
            string s;
            if (_strings.TryGetValue(key, out s) && !string.IsNullOrEmpty(s))
                return s;

            // 2. 全小写
            string lower = key.ToLowerInvariant();
            if (_strings.TryGetValue(lower, out s) && !string.IsNullOrEmpty(s))
                return s;

            // 3. 全大写
            string upper = key.ToUpperInvariant();
            if (_strings.TryGetValue(upper, out s) && !string.IsNullOrEmpty(s))
                return s;

            // 4. 首字母大写
            if (key.Length > 0)
            {
                string title = char.ToUpperInvariant(key[0]) + key.Substring(1);
                if (_strings.TryGetValue(title, out s) && !string.IsNullOrEmpty(s))
                    return s;
            }

            return null;
        }

        // ---- 内部 ----

        private static void Merge(Dictionary<string, string> dict)
        {
            foreach (var kv in dict) _strings[kv.Key] = kv.Value;
        }

        private static bool TryLoadFromFile(string path, out Dictionary<string, string> dict, out string displayName)
        {
            dict = null;
            displayName = null;
            try
            {
                if (!File.Exists(path)) return false;
                dict = ParseJson(File.ReadAllText(path, Encoding.UTF8));
                if (dict != null)
                    dict.TryGetValue("_lang_name", out displayName);
                return dict != null;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[I18n] read file failed: " + path + " — " + e.Message);
                return false;
            }
        }

        private static bool TryLoadFromResource(string resName, out Dictionary<string, string> dict, out string displayName)
        {
            dict = null;
            displayName = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream(resName))
                {
                    if (s == null) return false;
                    using (var sr = new StreamReader(s, Encoding.UTF8))
                        dict = ParseJson(sr.ReadToEnd());
                }
                if (dict != null)
                    dict.TryGetValue("_lang_name", out displayName);
                return dict != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 极简 JSON 解析器，仅支持扁平的 string -&gt; string 对象（这正是我们语言文件的格式）。
        /// 不依赖任何外部库，net472 开箱即用。支持标准转义：\" \\ \/ \b \f \n \r \t \uXXXX。
        /// </summary>
        private static Dictionary<string, string> ParseJson(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text)) return result;

            int i = 0, len = text.Length;
            SkipWhitespace();
            if (i >= len || text[i] != '{') return null;
            i++;
            SkipWhitespace();

            while (i < len)
            {
                if (text[i] == '}') { i++; break; }
                if (text[i] == ',') { i++; SkipWhitespace(); continue; }

                string key = ReadString();
                if (key == null) return null;
                SkipWhitespace();
                if (i >= len || text[i] != ':') return null;
                i++;
                SkipWhitespace();
                string value = ReadString();
                if (value == null) return null;

                result[key] = value;
                SkipWhitespace();
            }
            return result;

            void SkipWhitespace()
            {
                while (i < len && (text[i] == ' ' || text[i] == '\t' || text[i] == '\r' || text[i] == '\n'))
                    i++;
            }

            string ReadString()
            {
                if (i >= len || text[i] != '"') return null;
                i++;
                var sb = new StringBuilder();
                while (i < len && text[i] != '"')
                {
                    char c = text[i];
                    if (c == '\\' && i + 1 < len)
                    {
                        char next = text[i + 1];
                        switch (next)
                        {
                            case '"':  sb.Append('"');  i += 2; break;
                            case '\\': sb.Append('\\'); i += 2; break;
                            case '/':  sb.Append('/');  i += 2; break;
                            case 'b':  sb.Append('\b'); i += 2; break;
                            case 'f':  sb.Append('\f'); i += 2; break;
                            case 'n':  sb.Append('\n'); i += 2; break;
                            case 'r':  sb.Append('\r'); i += 2; break;
                            case 't':  sb.Append('\t'); i += 2; break;
                            case 'u':
                                if (i + 5 < len)
                                {
                                    string hex = text.Substring(i + 2, 4);
                                    int code;
                                    if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                                        sb.Append((char)code);
                                    i += 6;
                                }
                                else { sb.Append('u'); i += 2; }
                                break;
                            default: sb.Append(next); i += 2; break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                        i++;
                    }
                }
                if (i < len && text[i] == '"') i++;
                return sb.ToString();
            }
        }
    }
}

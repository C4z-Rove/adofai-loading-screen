using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 评级图标（复用 LevelLibrary 的 assets/marks 资源）与评级读写。
    /// 写入优先走 LevelLibrary.MarkStore，保证和 LevelLibrary 自己的列表一致；
    /// 找不到 LevelLibrary 时退化到本 Mod 的 ratings.txt。
    /// </summary>
    public static class RatingMarks
    {
        public const string Unranked = "Unranked";

        public static readonly string[] Tabs = new string[] { "P", "G", "U", "UJQ", "特殊" };
        public static readonly string[] TabLabels = new string[] { "Planetary", "Galactic", "Universal", "UJ＆Q", "特殊" };

        private static bool _initialized;
        private static string _marksDir;
        private static readonly Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, List<string>> _tabCache = new Dictionary<string, List<string>>();

        private static bool _levelLibChecked;
        private static MethodInfo _getMethod;
        private static MethodInfo _setMethod;

        private static Dictionary<string, string> _local;
        private static readonly Dictionary<string, string> _localWrites = new Dictionary<string, string>();

        // ------------------------------------------------------------------
        public static string MarksDirectory
        {
            get
            {
                EnsureInit();
                return _marksDir;
            }
        }

        public static bool HasMarks
        {
            get
            {
                EnsureInit();
                return !string.IsNullOrEmpty(_marksDir) && Directory.Exists(_marksDir)
                    && Directory.GetFiles(_marksDir, "*.png").Length > 0;
            }
        }

        /// <summary>启动时调用一次，提前把图标目录解析好（顺便打日志）。</summary>
        public static void Init()
        {
            EnsureInit();
        }

        private static void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                _marksDir = ResolveMarksDir();
                Main.Info("marks dir: " + _marksDir);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private static bool HasAnyPng(string dir)
        {
            try
            {
                return !string.IsNullOrEmpty(dir) && Directory.Exists(dir)
                    && Directory.GetFiles(dir, "*.png").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveMarksDir()
        {
            // 1) 配置里指定的路径（填到文件夹即可）
            try
            {
                string custom = Main.Config != null ? Main.Config.MarksPath : null;
                if (HasAnyPng(custom)) return custom;
            }
            catch { }

            // 2) 本 Mod 根目录下的 Marks（推荐放这里）
            try
            {
                foreach (string name in new string[] { "Marks", "marks" })
                {
                    string dir = Path.Combine(Main.ModPath, name);
                    if (HasAnyPng(dir)) return dir;
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            // 3) LevelLibrary 的 assets/marks（或任意 mod 的 assets/marks）
            try
            {
                string mods = Path.GetDirectoryName(Main.ModPath);
                if (!string.IsNullOrEmpty(mods) && Directory.Exists(mods))
                {
                    string direct = Path.Combine(mods, "LevelLibrary", "assets", "marks");
                    if (HasAnyPng(direct)) return direct;

                    foreach (string dir in Directory.GetDirectories(mods))
                    {
                        string candidate = Path.Combine(dir, "assets", "marks");
                        if (HasAnyPng(candidate)) return candidate;
                    }
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            // 4) 老兜底
            return Path.Combine(Main.ModPath, "Difficulties");
        }

        // ------------------------------------------------------------------
        // 图标
        // ------------------------------------------------------------------
        /// <summary>Marks 目录里是否存在这张图标。</summary>
        public static bool HasIcon(string mark)
        {
            try
            {
                EnsureInit();
                if (string.IsNullOrEmpty(mark) || string.IsNullOrEmpty(_marksDir)) return false;
                return File.Exists(Path.Combine(_marksDir, mark + ".png"));
            }
            catch
            {
                return false;
            }
        }

        public static Sprite Icon(string mark)
        {
            if (string.IsNullOrEmpty(mark)) mark = Unranked;

            Sprite cached;
            if (_iconCache.TryGetValue(mark, out cached) && cached != null) return cached;

            EnsureInit();
            try
            {
                if (!string.IsNullOrEmpty(_marksDir))
                {
                    string path = Path.Combine(_marksDir, mark + ".png");
                    if (!File.Exists(path)) path = Path.Combine(_marksDir, Unranked + ".png");
                    if (!File.Exists(path))
                    {
                        string[] files = Directory.GetFiles(_marksDir, "*.png");
                        if (files.Length > 0) path = files[0];
                    }
                    Sprite sprite = UiKit.LoadSpriteFromFile(path);
                    if (sprite != null)
                    {
                        _iconCache[mark] = sprite;
                        return sprite;
                    }
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
            return null;
        }

        // ------------------------------------------------------------------
        // 分页列表
        // ------------------------------------------------------------------
        public static List<string> ListTab(string tab)
        {
            EnsureInit();

            List<string> cached;
            if (_tabCache.TryGetValue(tab, out cached)) return cached;

            List<string> list = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(_marksDir) && Directory.Exists(_marksDir))
                {
                    foreach (string file in Directory.GetFiles(_marksDir, "*.png"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (IsJudge(name)) continue;
                        if (TabOf(name) == tab) list.Add(name);
                    }
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            SortTab(tab, list);
            _tabCache[tab] = list;
            return list;
        }

        public static bool IsJudge(string mark)
        {
            return string.Equals(mark, "normal", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mark, "wide", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mark, "strict", StringComparison.OrdinalIgnoreCase);
        }

        public static string TabOf(string mark)
        {
            if (string.IsNullOrEmpty(mark)) return "特殊";
            char c = char.ToUpperInvariant(mark[0]);
            if (c == 'P') return "P";
            if (c == 'G') return "G";
            if (c == 'U') return mark.IndexOf('J') >= 0 || mark.IndexOf('j') >= 0 ? "UJQ" : "U";
            if (c == 'Q') return "UJQ";
            return "特殊";
        }

        private static readonly string[] SpecialOrder = new string[]
        {
            "Unranked", "Censored", "Impossible", "Omamental", "T2", "T2H", "T3", "T3H", "T4", "T4H"
        };

        private static void SortTab(string tab, List<string> list)
        {
            if (tab == "特殊")
            {
                list.Sort(delegate(string a, string b)
                {
                    int ia = Array.IndexOf(SpecialOrder, a);
                    int ib = Array.IndexOf(SpecialOrder, b);
                    if (ia >= 0 || ib >= 0)
                    {
                        int ka = ia < 0 ? 999 : ia;
                        int kb = ib < 0 ? 999 : ib;
                        if (ka != kb) return ka.CompareTo(kb);
                    }
                    return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                });
                return;
            }

            list.Sort(delegate(string a, string b)
            {
                int qa = tab == "UJQ" ? (char.ToUpperInvariant(a[0]) == 'Q' ? 0 : 1) : 0;
                int qb = tab == "UJQ" ? (char.ToUpperInvariant(b[0]) == 'Q' ? 0 : 1) : 0;
                if (qa != qb) return qa.CompareTo(qb);

                int na = Number(a);
                int nb = Number(b);
                if (na != nb) return na.CompareTo(nb);
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static int Number(string mark)
        {
            int value = 0;
            bool any = false;
            for (int i = 0; i < mark.Length; i++)
            {
                if (char.IsDigit(mark[i]))
                {
                    value = value * 10 + (mark[i] - '0');
                    any = true;
                }
                else if (any)
                {
                    break;
                }
            }
            return any ? value : 999;
        }

        // ------------------------------------------------------------------
        // 读取 / 写入评级
        // ------------------------------------------------------------------
        public static string GetMark(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return null;

            EnsureInit();
            BindLevelLibrary();


            if (_getMethod != null)
            {
                try
                {
                    string value = _getMethod.Invoke(null, new object[] { levelPath }) as string;
                    if (!string.IsNullOrEmpty(value)) return value;
                }
                catch (Exception e)
                {
                    Main.Error(e);
                }
            }

            string local = LocalGet(levelPath);
            if (!string.IsNullOrEmpty(local)) return local;

            return MarksFileGet(levelPath);
        }

        /// <summary>取这张谱面「判定」：wide / normal / strict，没有就 null。</summary>
        public static string GetJudge(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return null;

            EnsureInit();
            BindLevelLibrary();

            // 0) 本 Mod 自己记的判定（在弹窗里选的，优先级最高）
            string own = GetLocalJudge(levelPath);
            if (own != null) return own;

            if (_getJudgeMethod != null)
            {
                try
                {
                    string value = _getJudgeMethod.Invoke(null, new object[] { levelPath }) as string;
                    string normalized = NormalizeJudge(value);
                    if (normalized != null) return normalized;
                    if (!string.IsNullOrEmpty(value)) Main.Info("judge: unmapped '" + value + "' for " + levelPath);
                }
                catch (Exception e)
                {
                    Main.Error(e);
                }
            }

            EnsureMarksFile();
            if (_judgesFile != null)
            {
                try
                {
                    string judge;
                    if (_judgesFile.TryGetValue(Path.GetFullPath(levelPath), out judge)) return judge;
                }
                catch (Exception e)
                {
                    Main.Error(e);
                }
            }

            // 3) 通关记录（records.txt）里「最佳通关」的判定槽
            string fromRecords = GetJudgeFromRecords(levelPath);
            if (fromRecords != null) return fromRecords;

            return null;
        }

        /// <summary>records.txt 里每张谱面最佳通关的判定（0=宽判 1=标判 2=严判）。</summary>
        private static string GetJudgeFromRecords(string levelPath)
        {
            EnsureRecords();
            if (_recordsJudge == null || string.IsNullOrEmpty(levelPath)) return null;

            try
            {
                string folder = Path.GetDirectoryName(levelPath);
                if (string.IsNullOrEmpty(folder)) return null;

                string judge;
                if (_recordsJudge.TryGetValue(NormalizeFolder(folder), out judge)) return judge;
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            return null;
        }

        private static string NormalizeFolder(string folder)
        {
            try
            {
                return Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return folder;
            }
        }

        private static void EnsureRecords()
        {
            if (_recordsLoaded) return;
            _recordsLoaded = true;

            try
            {
                string root = FindLevelLibraryRoot();
                if (string.IsNullOrEmpty(root)) return;

                string file = Path.Combine(root, "records.txt");
                if (!File.Exists(file)) return;

                Dictionary<string, double> bestXacc = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                _recordsJudge = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (string line in File.ReadAllLines(file))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 10) continue;

                    string folder = parts[0].Trim();
                    if (folder.Length == 0) continue;

                    double xacc;
                    if (!double.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out xacc)) continue;

                    int slot;
                    if (!int.TryParse(parts[9], out slot)) continue;

                    string judge = NormalizeJudge(slot.ToString());
                    if (judge == null) continue;

                    string abs = Path.IsPathRooted(folder) ? folder : Path.Combine(root, folder);
                    string key = NormalizeFolder(abs);

                    double prev;
                    if (bestXacc.TryGetValue(key, out prev) && prev >= xacc) continue;

                    bestXacc[key] = xacc;
                    _recordsJudge[key] = judge;
                }

                Main.Info("judge records: " + _recordsJudge.Count + " entries");
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        // ------------------------------------------------------------------
        // 本 Mod 自己保存的判定（judges.txt：绝对路径 \t wide/normal/strict）
        // ------------------------------------------------------------------
        private static Dictionary<string, string> LocalJudges
        {
            get
            {
                if (_localJudges != null) return _localJudges;

                _localJudges = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    string path = Path.Combine(Main.ModPath, "judges.txt");
                    if (!File.Exists(path)) return _localJudges;

                    foreach (string line in File.ReadAllLines(path))
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length < 2) continue;
                        string key = parts[0].Trim();
                        string judge = NormalizeJudge(parts[1].Trim());
                        if (key.Length > 0 && judge != null) _localJudges[key] = judge;
                    }
                }
                catch (Exception e)
                {
                    Main.Error(e);
                }
                return _localJudges;
            }
        }

        private static string LocalJudgeKey(string levelPath)
        {
            try
            {
                return Path.GetFullPath(levelPath);
            }
            catch
            {
                return levelPath;
            }
        }

        public static string GetLocalJudge(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return null;
            string judge;
            return LocalJudges.TryGetValue(LocalJudgeKey(levelPath), out judge) ? judge : null;
        }

        /// <summary>设置判定：judge = wide / normal / strict / null（清除）。</summary>
        public static void SetJudge(string levelPath, string judge)
        {
            if (string.IsNullOrEmpty(levelPath)) return;

            string key = LocalJudgeKey(levelPath);
            string normalized = NormalizeJudge(judge);

            try
            {
                if (normalized == null) LocalJudges.Remove(key);
                else LocalJudges[key] = normalized;

                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> pair in LocalJudges) lines.Add(pair.Key + "\t" + pair.Value);
                File.WriteAllLines(Path.Combine(Main.ModPath, "judges.txt"), lines.ToArray());

                Main.Info("judge saved: " + (normalized ?? "none") + " for " + levelPath);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            // 同步给 LevelLibrary（它加载着的话）
            try
            {
                BindLevelLibrary();
                if (_setJudgeMethod != null)
                {
                    string chinese = normalized == "wide" ? "宽判" : (normalized == "strict" ? "严判" : (normalized == "normal" ? "标判" : null));
                    _setJudgeMethod.Invoke(null, new object[] { levelPath, chinese });
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        /// <summary>宽判/标判/严判 或 wide/normal/strict → wide/normal/strict。</summary>
        private static string NormalizeJudge(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            string v = value.Trim();
            if (v.Length == 0) return null;

            if (v.IndexOf("宽", StringComparison.Ordinal) >= 0 || v.IndexOf("wide", StringComparison.OrdinalIgnoreCase) >= 0) return "wide";
            if (v.IndexOf("严", StringComparison.Ordinal) >= 0 || v.IndexOf("strict", StringComparison.OrdinalIgnoreCase) >= 0) return "strict";
            if (v.IndexOf("标", StringComparison.Ordinal) >= 0 || v.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0) return "normal";

            // LevelLibrary 的判定槽：0=宽判 1=标判 2=严判
            if (v == "0") return "wide";
            if (v == "1") return "normal";
            if (v == "2") return "strict";

            return null;
        }

        public static void SetMark(string levelPath, string mark)
        {
            if (string.IsNullOrEmpty(levelPath)) return;

            EnsureInit();
            BindLevelLibrary();

            if (mark == Unranked) mark = "";

            if (_setMethod != null)
            {
                try
                {
                    _setMethod.Invoke(null, new object[] { levelPath, mark ?? "" });
                    Main.Info("rating saved to LevelLibrary: " + levelPath + " => " + (string.IsNullOrEmpty(mark) ? "Unranked" : mark));
                    return;
                }
                catch (Exception e)
                {
                    Main.Error(e);
                }
            }

            LocalSet(levelPath, mark);
            Main.Info("rating saved locally: " + levelPath + " => " + (string.IsNullOrEmpty(mark) ? "Unranked" : mark));
        }

        // ------------------------------------------------------------------
        // LevelLibrary 没加载（被关掉）时，直接读它的 marks.txt
        // ------------------------------------------------------------------
        private static bool _marksFileLoaded;
        private static Dictionary<string, string> _marksFile;
        private static Dictionary<string, string> _judgesFile;
        private static Dictionary<string, string> _recordsJudge;
        private static bool _recordsLoaded;
        private static MethodInfo _getJudgeMethod;
        private static MethodInfo _setJudgeMethod;
        private static Dictionary<string, string> _localJudges;
        private static string _marksFileRoot;

        private static string MarksFileGet(string levelPath)
        {
            EnsureMarksFile();
            if (_marksFile == null) return null;
            try
            {
                string key = Path.GetFullPath(levelPath);
                string mark;
                if (_marksFile.TryGetValue(key, out mark)) return mark;
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
            return null;
        }

        /// <summary>找 LevelLibrary 的根目录（marks.txt 所在处），和图标目录无关。</summary>
        private static string FindLevelLibraryRoot()
        {
            try
            {
                string mods = Path.GetDirectoryName(Main.ModPath);
                if (string.IsNullOrEmpty(mods) || !Directory.Exists(mods)) return null;

                string direct = Path.Combine(mods, "LevelLibrary");
                if (File.Exists(Path.Combine(direct, "marks.txt"))) return direct;

                foreach (string dir in Directory.GetDirectories(mods))
                {
                    if (File.Exists(Path.Combine(dir, "marks.txt"))) return dir;
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
            return null;
        }

        private static void EnsureMarksFile()
        {
            if (_marksFileLoaded) return;
            _marksFileLoaded = true;

            try
            {
                string libRoot = FindLevelLibraryRoot();
                if (string.IsNullOrEmpty(libRoot)) return;

                string file = Path.Combine(libRoot, "marks.txt");
                if (!File.Exists(file)) return;

                _marksFileRoot = libRoot;
                _marksFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (string line in File.ReadAllLines(file))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 2) continue;

                    string rel = parts[0].Trim();
                    string mark = parts[1].Trim();
                    if (rel.Length == 0 || mark.Length == 0) continue;

                    string abs = Path.IsPathRooted(rel) ? rel : Path.Combine(_marksFileRoot, rel);
                    string key = Path.GetFullPath(abs);
                    _marksFile[key] = mark;

                    if (parts.Length >= 3)
                    {
                        string judge = NormalizeJudge(parts[2]);
                        if (judge != null)
                        {
                            if (_judgesFile == null) _judgesFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            _judgesFile[key] = judge;
                        }
                    }
                }

                Main.Info("marks.txt fallback loaded: " + _marksFile.Count + " entries");
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private static void BindLevelLibrary()
        {
            if (_levelLibChecked) return;
            _levelLibChecked = true;

            try
            {
                Type type = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType("LevelLibrary.MarkStore", false);
                    if (type != null) break;
                }
                if (type == null) return;

                _getMethod = type.GetMethod("Get", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                _setMethod = type.GetMethod("Set", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);
                _getJudgeMethod = type.GetMethod("GetJudge", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                _setJudgeMethod = type.GetMethod("SetJudge", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);
                Main.Info("LevelLibrary MarkStore bound: get=" + (_getMethod != null) + " set=" + (_setMethod != null));
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private static Dictionary<string, string> Local
        {
            get
            {
                if (_local != null) return _local;
                _local = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    string path = Path.Combine(Main.ModPath, "ratings.txt");
                    if (File.Exists(path))
                    {
                        foreach (string line in File.ReadAllLines(path))
                        {
                            string[] parts = line.Split('\t');
                            if (parts.Length >= 2 && parts[0].Trim().Length > 0)
                            {
                                _local[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Error(e);
                }
                return _local;
            }
        }

        private static string LocalGet(string levelPath)
        {
            string mark;
            return Local.TryGetValue(levelPath, out mark) ? mark : null;
        }

        private static void LocalSet(string levelPath, string mark)
        {
            if (string.IsNullOrEmpty(mark)) Local.Remove(levelPath);
            else Local[levelPath] = mark;

            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> pair in Local)
                {
                    lines.Add(pair.Key + "\t" + pair.Value);
                }
                File.WriteAllLines(Path.Combine(Main.ModPath, "ratings.txt"), lines);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }
    }
}

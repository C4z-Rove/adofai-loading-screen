using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace PhigrosLoading
{
    /// <summary>
    /// 跟 AutoRating 联动：读取它算好的 PGU 评级（P18 / G3 / U12 这种，和评级图标同一套命名）。
    /// 只读它的静态字段，不自己跑计算，避免打断它自己的面板显示。
    /// 另外把结果按谱面路径缓存到本地，这样下次进同一关时加载画面也能直接显示。
    /// </summary>
    public static class AutoRatingBridge
    {
        public class Entry
        {
            public string Mark;
            public string GG;
            public double Star;
            public long Stamp;
        }

        private static bool _init;
        private static bool _bound;
        private static FieldInfo _pguField;
        private static FieldInfo _ggField;
        private static FieldInfo _starField;
        private static Dictionary<string, Entry> _cache;

        /// <summary>AutoRating 是否已加载并可用。</summary>
        public static bool Available
        {
            get
            {
                EnsureInit();
                return _bound;
            }
        }

        private static string CachePath
        {
            get { return Path.Combine(Main.ModPath, "autorating_cache.json"); }
        }

        // ------------------------------------------------------------------
        private static void EnsureInit()
        {
            if (_init) return;
            _init = true;

            try
            {
                Type type = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType("AdofaiRater.RaterMain", false);
                    if (type != null) break;
                }

                if (type != null)
                {
                    _pguField = type.GetField("PGU", BindingFlags.Public | BindingFlags.Static);
                    _ggField = type.GetField("GGRating", BindingFlags.Public | BindingFlags.Static);
                    _starField = type.GetField("globalRating", BindingFlags.Public | BindingFlags.Static);
                    _bound = _pguField != null;
                    Main.Info("AutoRating bound: " + _bound + " (PGU=" + (_pguField != null) + ")");
                }
                else
                {
                    Main.Info("AutoRating not found");
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            LoadCache();
        }

        private static void LoadCache()
        {
            try
            {
                if (!File.Exists(CachePath)) return;
                _cache = JsonConvert.DeserializeObject<Dictionary<string, Entry>>(File.ReadAllText(CachePath));
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private static void SaveCache()
        {
            if (_cache == null) return;
            try
            {
                File.WriteAllText(CachePath, JsonConvert.SerializeObject(_cache, Formatting.Indented));
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private static Dictionary<string, Entry> Cache
        {
            get
            {
                if (_cache == null) _cache = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
                return _cache;
            }
        }

        // ------------------------------------------------------------------
        /// <summary>读 AutoRating 当前的 PGU（对应现在正在玩的这张谱面）。</summary>
        public static bool TryGetLive(out string mark, out double star, out string gg)
        {
            mark = null;
            star = 0.0;
            gg = null;

            EnsureInit();
            if (!_bound) return false;

            try
            {
                string pgu = _pguField.GetValue(null) as string;
                if (!IsValidMark(pgu)) return false;

                mark = pgu;
                star = _starField != null ? Convert.ToDouble(_starField.GetValue(null)) : 0.0;
                gg = _ggField != null ? _ggField.GetValue(null) as string : null;
                return true;
            }
            catch (Exception e)
            {
                Main.Error(e);
                return false;
            }
        }

        /// <summary>把当前谱面的自动评级记进缓存（按文件修改时间失效）。</summary>
        public static void CacheFor(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return;

            string mark;
            double star;
            string gg;
            if (!TryGetLive(out mark, out star, out gg)) return;

            try
            {
                long stamp = 0;
                if (File.Exists(levelPath)) stamp = File.GetLastWriteTimeUtc(levelPath).Ticks;

                Entry entry;
                if (Cache.TryGetValue(levelPath, out entry) && entry != null
                    && entry.Mark == mark && entry.Stamp == stamp)
                {
                    return;
                }

                Cache[levelPath] = new Entry { Mark = mark, GG = gg, Star = star, Stamp = stamp };
                SaveCache();
                Main.Info("auto rating cached: " + mark + " (star " + star.ToString("0.00") + ") for " + levelPath);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        /// <summary>取缓存里的 GG 评级（例如 "18" / "20.3" / "-21+"）。</summary>
        public static string GetCachedGG(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return null;

            EnsureInit();
            try
            {
                Entry entry;
                if (!Cache.TryGetValue(levelPath, out entry) || entry == null) return null;
                if (string.IsNullOrEmpty(entry.GG)) return null;

                if (File.Exists(levelPath))
                {
                    long stamp = File.GetLastWriteTimeUtc(levelPath).Ticks;
                    if (entry.Stamp != 0 && entry.Stamp != stamp) return null;
                }

                return entry.GG;
            }
            catch (Exception e)
            {
                Main.Error(e);
                return null;
            }
        }

        /// <summary>取缓存里的自动评级（谱面没改动过才有效）。</summary>
        public static string GetCachedMark(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return null;

            EnsureInit();
            try
            {
                Entry entry;
                if (!Cache.TryGetValue(levelPath, out entry) || entry == null) return null;
                if (!IsValidMark(entry.Mark)) return null;

                if (File.Exists(levelPath))
                {
                    long stamp = File.GetLastWriteTimeUtc(levelPath).Ticks;
                    if (entry.Stamp != 0 && entry.Stamp != stamp) return null;
                }
                return entry.Mark;
            }
            catch (Exception e)
            {
                Main.Error(e);
                return null;
            }
        }

        /// <summary>拼成 "P18 / Lv.18+" 这种显示串（缺哪个就省略哪个）。</summary>
        public static string Format(string pgu, string gg)
        {
            string left = string.IsNullOrEmpty(pgu) || pgu == "Default" ? null : pgu;
            string right = string.IsNullOrEmpty(gg) || gg == "Default" ? null : "Lv." + gg;

            if (left != null && right != null) return left + " / " + right;
            if (left != null) return left;
            return right;
        }

        public static double GetCachedStar(string levelPath)
        {
            Entry entry;
            if (string.IsNullOrEmpty(levelPath) || !Cache.TryGetValue(levelPath, out entry) || entry == null) return 0.0;
            return entry.Star;
        }

        private static bool IsValidMark(string mark)
        {
            if (string.IsNullOrEmpty(mark)) return false;
            if (mark == "Default" || mark == "PGU100") return false;

            char c = char.ToUpperInvariant(mark[0]);
            if (c != 'P' && c != 'G' && c != 'U') return false;
            return mark.Length >= 2 && char.IsDigit(mark[1]);
        }
    }
}

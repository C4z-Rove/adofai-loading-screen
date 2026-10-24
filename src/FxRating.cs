using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace PhigrosLoading
{
    /// <summary>
    /// 谱面特效评级（T 系列）：读取 .adofai，统计各类"视觉"事件，
    /// 加权出一个分数，映射到 Marks\T2 / T3 / T4（以及 *H 变体，若图标存在）。
    /// </summary>
    public static class FxRating
    {
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        private static readonly Dictionary<string, DateTime> Stamps = new Dictionary<string, DateTime>();

        // 阈值：T4 对标 Megamix2026 - Full VFX 这种满特效谱
        private const double ThresholdT2 = 120.0;
        private const double ThresholdT3 = 600.0;
        private const double ThresholdT4 = 3000.0;

        /// <summary>返回 "T2" / "T3H" 这样的等级；无特效返回 null。</summary>
        public static string Get(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return null;

            try
            {
                string chart = ResolveChart(levelPath);
                if (chart == null) return null;

                DateTime stamp = File.GetLastWriteTimeUtc(chart);
                if (Cache.ContainsKey(chart) && Stamps.ContainsKey(chart) && Stamps[chart] == stamp)
                {
                    return Cache[chart];
                }

                string result = Compute(chart);
                Cache[chart] = result;
                Stamps[chart] = stamp;
                Main.Info("fx rating: " + (result ?? "none") + " for " + chart);
                return result;
            }
            catch (Exception e)
            {
                Main.Error(e);
                return null;
            }
        }

        private static string ResolveChart(string levelPath)
        {
            if (File.Exists(levelPath)) return levelPath;
            if (!Directory.Exists(levelPath)) return null;

            string main = Path.Combine(levelPath, "main.adofai");
            if (File.Exists(main)) return main;

            string[] files = Directory.GetFiles(levelPath, "*.adofai");
            return files.Length > 0 ? files[0] : null;
        }

        private static string Compute(string chartPath)
        {
            JObject root = JObject.Parse(File.ReadAllText(chartPath));

            int decorations = 0, moves = 0, particles = 0, filters = 0;
            int anims = 0, moveTracks = 0, flashes = 0, blooms = 0, cameras = 0;

            JArray actions = root["actions"] as JArray;
            if (actions != null)
            {
                foreach (JToken action in actions)
                {
                    string type = (string)action["eventType"] ?? "";
                    switch (type)
                    {
                        case "AddDecoration": decorations++; break;
                        case "MoveDecorations": moves++; break;
                        case "SetParticle":
                        case "EmitParticle": particles++; break;
                        case "SetFilter":
                        case "SetFilterAdvanced": filters++; break;
                        case "AnimateTrack":
                        case "RecolorTrack": anims++; break;
                        case "MoveTrack": moveTracks++; break;
                        case "Flash": flashes++; break;
                        case "Bloom": blooms++; break;
                        case "MoveCamera": cameras++; break;
                    }
                }
            }

            int tiles = 0;
            JArray angleData = root["angleData"] as JArray;
            JArray pathData = root["pathData"] as JArray;
            if (angleData != null) tiles = angleData.Count;
            else if (pathData != null) tiles = pathData.Count;
            if (tiles <= 0) tiles = 1;

            // 加权：装饰物 / 滤镜 / 闪光最重，纯轨道移动最轻
            double score = decorations * 1.00
                         + filters * 1.20
                         + flashes * 1.50
                         + blooms * 1.50
                         + cameras * 0.80
                         + particles * 0.80
                         + moves * 0.40
                         + moveTracks * 0.30
                         + anims * 0.20;

            // 每格视觉事件密度
            double density = (decorations + filters + flashes + blooms + cameras + particles + moves) / (double)tiles;

            if (score < ThresholdT2) return null;

            string tier = score >= ThresholdT4 ? "T4" : (score >= ThresholdT3 ? "T3" : "T2");

            // H 变体：密度高时使用，但图标不存在就退回普通档
            if (density >= 1.5)
            {
                string heavy = tier + "H";
                if (RatingMarks.HasIcon(heavy)) return heavy;
            }

            if (RatingMarks.HasIcon(tier)) return tier;

            // 兜底：依次退回更低档，避免显示"未评级"
            if (RatingMarks.HasIcon("T3")) return "T3";
            if (RatingMarks.HasIcon("T2")) return "T2";
            return null;
        }
    }
}

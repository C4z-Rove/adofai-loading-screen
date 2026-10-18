using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 一次进入关卡时，加载画面需要展示的全部信息。
    /// </summary>
    public class LoadingInfo
    {
        public string Key = "";
        public string Title = "";
        public string Artist = "";
        public string Author = "";
        public int Difficulty = -1;
        public string Mark = "";

        public string LevelPath = "";
        public bool IsInternal;
        public string InternalName = "";

        public string PreviewImage = "";
        public string BgImage = "";

        public Sprite Cover;
        public bool CoverIsFallback;
        public Sprite DifficultySprite;

        /// <summary>这是「离开关卡」时的过渡动画，不是进关。</summary>
        public bool IsExit;

        private static readonly Dictionary<string, Sprite> CoverCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<int, Sprite> DifficultyCache = new Dictionary<int, Sprite>();
        private static readonly string[] ImageExtensions = new string[] { ".png", ".jpg", ".jpeg", ".webp" };
        private static Sprite _coverFallback;

        // ------------------------------------------------------------------
        // 入口
        // ------------------------------------------------------------------
        public void PreparePreview()
        {
            ResolveCover();
            ResolveDifficulty();
        }

        public static LoadingInfo Create()
        {
            string scene = GCS.sceneToLoad;
            if (string.IsNullOrEmpty(scene)) return null;

            LoadingInfo info = new LoadingInfo();

            if (scene == "scnGame")
            {
                if (!string.IsNullOrEmpty(GCS.internalLevelName))
                {
                    info.IsInternal = true;
                    info.InternalName = GCS.internalLevelName;
                    info.Key = info.InternalName;
                    info.ReadInternalLevel();
                }
                else if (GCS.customLevelPaths != null && GCS.customLevelPaths.Length > 0)
                {
                    int index = Mathf.Clamp(GCS.customLevelIndex, 0, GCS.customLevelPaths.Length - 1);
                    info.LevelPath = GCS.customLevelPaths[index];
                    info.Key = SafeName(Path.GetFileNameWithoutExtension(info.LevelPath));
                    info.ReadFileLevel(info.LevelPath);
                }
                else
                {
                    return null;
                }
            }
            else if (scene.EndsWith(".adofai", StringComparison.OrdinalIgnoreCase) && File.Exists(scene))
            {
                info.LevelPath = scene;
                info.Key = SafeName(Path.GetFileNameWithoutExtension(scene));
                info.ReadFileLevel(scene);
            }
            else if (ADOBase.isScnGame && scnGame.instance != null && Main.Config != null && Main.Config.ExitAnimation)
            {
                // 离开关卡：用当前这关的信息做退出动画（不再走原版黑屏）
                info.IsExit = true;
                info.LevelPath = scnGame.instance.levelPath;
                if (!string.IsNullOrEmpty(info.LevelPath))
                {
                    info.Key = SafeName(Path.GetFileNameWithoutExtension(info.LevelPath));
                    info.ReadFileLevel(info.LevelPath);
                }
                else
                {
                    info.Key = "";
                }

                scnGame game = scnGame.instance;
                if (game != null && game.levelData != null)
                {
                    info.Title = Clean(game.levelData.song);
                    info.Artist = Clean(game.levelData.artist);
                    info.Author = Clean(game.levelData.author);
                }
            }
            else
            {
                return null;
            }

            info.ResolveCover();
            info.ResolveDifficulty();
            return info;
        }

        /// <summary>
        /// 关卡 JSON 真正读出来之后（scnGame.LoadLevel 之后）再一次校正文本信息。
        /// 主要覆盖 bundle/DLC 关卡、以及读取失败的场合。
        /// </summary>
        public static void ApplyLevelData(LoadingInfo info, ADOFAI.LevelData data, string levelPath)
        {
            if (info == null || data == null) return;
            try
            {
                if (!string.IsNullOrEmpty(data.song)) info.Title = Clean(data.song);
                if (!string.IsNullOrEmpty(data.artist)) info.Artist = Clean(data.artist);
                if (!string.IsNullOrEmpty(data.author)) info.Author = Clean(data.author);
                try { info.Difficulty = data.difficulty; } catch { }

                string preview = "";
                try { preview = data.previewImage; } catch { }
                if (!string.IsNullOrEmpty(preview) && (info.Cover == null || info.CoverIsFallback))
                {
                    info.PreviewImage = preview;
                    if (!string.IsNullOrEmpty(levelPath)) info.LevelPath = levelPath;
                    info.ResolveCover();
                }

                info.ResolveDifficulty();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        // ------------------------------------------------------------------
        // 读取关卡
        // ------------------------------------------------------------------
        private void ReadInternalLevel()
        {
            try
            {
                string[] parts = InternalName.Split('-');
                if (parts.Length < 2) return;
                string world = parts[0];
                string level = parts[parts.Length - 1];
                string sub = level == "X" ? "main" : "sub" + level;
                string resourcePath = "InternalLevels/" + world + "/" + sub;

                TextAsset asset = Resources.Load<TextAsset>(resourcePath);
                if (asset == null)
                {
                    Main.Info("internal level asset not found: " + resourcePath + " (DLC? using fallback)");
                    return;
                }
                ParseJson(asset.text);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private void ReadFileLevel(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
                ParseJson(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private void ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            JObject root = JObject.Parse(json);
            JToken settings = root["settings"];
            if (settings == null || settings.Type != JTokenType.Object) settings = root;

            Title = Clean(Str(settings["song"]));
            Artist = Clean(Str(settings["artist"]));
            Author = Clean(Str(settings["author"]));
            PreviewImage = Str(settings["previewImage"]);
            BgImage = Str(settings["bgImage"]);

            int diff = -1;
            try
            {
                JToken d = settings["difficulty"];
                if (d != null) diff = Convert.ToInt32(d.ToString());
            }
            catch { diff = -1; }
            Difficulty = diff;

            if (string.IsNullOrEmpty(Title)) Title = Key;
        }

        // ------------------------------------------------------------------
        // 曲图
        // ------------------------------------------------------------------
        private void ResolveCover()
        {
            if (Cover != null && !CoverIsFallback) return;

            // 1. 用户放在 Mods/PhigrosLoading/Covers/ 里的自定义曲图
            foreach (string name in CoverNames())
            {
                foreach (string ext in ImageExtensions)
                {
                    Sprite sprite = LoadCachedSprite(Path.Combine(Main.ModPath, "Covers", name + ext), "cover:" + name);
                    if (sprite != null)
                    {
                        Cover = sprite;
                        CoverIsFallback = false;
                        return;
                    }
                }
            }

            // 2. 关卡自带的 previewImage
            Sprite preview = LoadLevelImage(PreviewImage);
            if (preview != null)
            {
                Cover = preview;
                CoverIsFallback = false;
                return;
            }

            // 3. 关卡背景图
            Sprite bg = LoadLevelImage(BgImage);
            if (bg != null)
            {
                Cover = bg;
                CoverIsFallback = false;
                return;
            }

            // 4. 官方关卡：用世界的 portal 图
            if (IsInternal)
            {
                string world = InternalName.Split('-')[0];
                Sprite portal = LoadResourceSprite("InternalLevels/" + world + "/portal");
                if (portal != null)
                {
                    Cover = portal;
                    CoverIsFallback = false;
                    return;
                }
            }

            // 5. 兜底渐变色
            if (_coverFallback == null) _coverFallback = UiKit.MakeVerticalGradient(new Color(0.13f, 0.09f, 0.26f), new Color(0.02f, 0.03f, 0.07f));
            Cover = _coverFallback;
            CoverIsFallback = true;
        }

        public static Sprite LoadResourceSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            try
            {
                Texture2D tex = Resources.Load<Texture2D>(resourcePath);
                if (tex != null)
                {
                    return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }

                Sprite sprite = Resources.Load<Sprite>(resourcePath);
                if (sprite != null && sprite.texture != null)
                {
                    Texture2D spriteTex = sprite.texture;
                    return Sprite.Create(spriteTex, new Rect(0f, 0f, spriteTex.width, spriteTex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
            return null;
        }

        private Sprite LoadLevelImage(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            try
            {
                if (IsInternal)
                {
                    string world = InternalName.Split('-')[0];
                    string baseName = Path.GetFileNameWithoutExtension(name);
                    return LoadResourceSprite("InternalLevels/" + world + "/" + baseName);
                }

                if (string.IsNullOrEmpty(LevelPath)) return null;
                string dir = Path.GetDirectoryName(LevelPath);
                if (string.IsNullOrEmpty(dir)) return null;
                return LoadCachedSprite(Path.Combine(dir, name), "levelimg:" + dir + "|" + name);
            }
            catch (Exception e)
            {
                Main.Error(e);
                return null;
            }
        }

        private IEnumerable<string> CoverNames()
        {
            if (!string.IsNullOrEmpty(Key)) yield return Key;

            if (!string.IsNullOrEmpty(LevelPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(LevelPath);
                if (!string.IsNullOrEmpty(fileName)) yield return fileName;

                string dir = Path.GetDirectoryName(LevelPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    string folder = Path.GetFileName(dir);
                    if (!string.IsNullOrEmpty(folder)) yield return folder;
                }
            }

            if (!string.IsNullOrEmpty(Author)) yield return Author;
        }

        private static Sprite LoadCachedSprite(string path, string cacheKey)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            Sprite cached;
            if (CoverCache.TryGetValue(cacheKey, out cached) && cached != null) return cached;

            Sprite sprite = UiKit.LoadSpriteFromFile(path);
            if (sprite != null) CoverCache[cacheKey] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------
        // 难度图 = LevelLibrary 的评级图标（Unranked 兜底）
        // ------------------------------------------------------------------
        private void ResolveDifficulty()
        {
            string mark = null;
            if (!IsInternal && !string.IsNullOrEmpty(LevelPath))
            {
                mark = RatingMarks.GetMark(LevelPath);

                if (string.IsNullOrEmpty(mark) && Main.Config != null && Main.Config.AutoRatingDisplay)
                {
                    mark = AutoRatingBridge.GetCachedMark(LevelPath);
                }
            }
            if (string.IsNullOrEmpty(mark)) mark = RatingMarks.Unranked;

            if (Mark == mark && DifficultySprite != null) return;
            Mark = mark;

            Sprite sprite = RatingMarks.HasMarks ? RatingMarks.Icon(mark) : null;
            if (sprite == null) sprite = LoadDifficultySprite(Difficulty);

            DifficultySprite = sprite;
        }

        private static Sprite LoadDifficultySprite(int difficulty)
        {
            string dir = Path.Combine(Main.ModPath, "Difficulties");
            List<string> names = new List<string>();
            if (difficulty >= 0) names.Add(difficulty.ToString());
            names.Add("default");

            foreach (string name in names)
            {
                foreach (string ext in ImageExtensions)
                {
                    string path = Path.Combine(dir, name + ext);
                    if (!File.Exists(path)) continue;
                    Sprite sprite = UiKit.LoadSpriteFromFile(path);
                    if (sprite != null) return sprite;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // 工具
        // ------------------------------------------------------------------


        private static string Str(JToken token)
        {
            if (token == null) return "";
            try { return token.ToString(); }
            catch { return ""; }
        }

        private static string Clean(string value)
        {
            return UiKit.StripRichText(value);
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid) value = value.Replace(c, '_');
            return value.Trim();
        }
    }
}

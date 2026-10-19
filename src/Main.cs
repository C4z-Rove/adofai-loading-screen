using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

namespace PhigrosLoading
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Log;
        public static string ModPath = "";
        public static Settings Config = new Settings();

        /// <summary>UMM 里这个模组是否启用（关掉时所有元素立即隐藏/停止工作）。</summary>
        public static bool Active;
        private static Harmony _harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                Log = modEntry.Logger;
                ModPath = (modEntry.Path ?? "").TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                Config = Settings.Load(Path.Combine(ModPath, "config.json"));

                _harmony = new Harmony(modEntry.Info.Id);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                InGameBadge.Init();
                ClsBadge.Init();
                RatingMarks.Init();
                JudgmentRecorder.Init();
                HotkeyWatcher.Init();
                LanguageFixer.Init();

                GameObject watcher = new GameObject("PhigrosLoadingWatcher");
                UnityEngine.Object.DontDestroyOnLoad(watcher);
                watcher.AddComponent<GameWatcher>();

                if (Config.PreviewOnStart) StartPreview();

                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                modEntry.OnToggle = OnToggle;
                Active = true;

                Log.Log("Adofai Loading Screen v2.3.3 loaded. ModPath = " + ModPath);
                return true;
            }
            catch (Exception e)
            {
                if (Log != null) Log.LogException(e);
                return false;
            }
        }

        /// <summary>UMM 开关回调：关掉时立刻隐藏所有元素。</summary>
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            try
            {
                Active = value;
                Info("mod " + (value ? "enabled" : "disabled") + " via UMM");
                if (!value) HideAll();
            }
            catch (Exception e)
            {
                Error(e);
            }
            return true;
        }

        /// <summary>把本模组画的所有东西收起来。</summary>
        public static void HideAll()
        {
            try { PhigrosOverlay.HideImmediate(); } catch (Exception e) { Error(e); }
            try { InGameBadge.Hide(); } catch (Exception e) { Error(e); }
            try { ClsBadge.HideAll(); } catch (Exception e) { Error(e); }
            try { RatingPrompt.ForceClose(); } catch (Exception e) { Error(e); }
        }

        public static void Info(string message)
        {
            if (Log != null) Log.Log(message);
        }

        public static void Error(Exception e)
        {
            if (Log != null) Log.LogException(e);
        }

        public static void StartPreview()
        {
            try
            {
                GameObject go = new GameObject("PhigrosLoadingPreview");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<PreviewRunner>();
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        // ------------------------------------------------------------------
        // UMM 设置界面
        // ------------------------------------------------------------------
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save();
        }

        private static bool _waitingHotkey;
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + Lang.T("通用", "General") + "</b>");

                GUILayout.BeginHorizontal();
                GUILayout.Label(Lang.T("界面语言", "UI language"), GUILayout.Width(232f));
                if (GUILayout.Button(Lang.T("自动", "Auto"), GUILayout.Width(70f))) Config.Language = "auto";
                if (GUILayout.Button("中文", GUILayout.Width(70f))) Config.Language = "zh";
                if (GUILayout.Button("English", GUILayout.Width(90f))) Config.Language = "en";
                GUILayout.Label(Lang.Chinese ? "（当前：中文）" : "(current: English)", GUILayout.Width(160f));
                GUILayout.EndHorizontal();

                Config.Enabled = RowToggle(Lang.T("启用加载画面", "Enable loading screen"), Config.Enabled);
                Config.RatingPrompt = RowToggle(Lang.T("离开关卡时弹出评级窗", "Show rating window on level exit"), Config.RatingPrompt);
                Config.InGameDifficulty = RowToggle(Lang.T("游戏内显示角落文字", "Show corner labels in game"), Config.InGameDifficulty);
                Config.ShowPercent = RowToggle(Lang.T("加载条显示百分比", "Show percentage on bar"), Config.ShowPercent);
                Config.KenBurns = RowToggle(Lang.T("曲图缓慢放大", "Slow zoom on cover"), Config.KenBurns);
                Config.AutoRatingSuggest = RowToggle(Lang.T("评级弹窗采用 AutoRating 建议", "Use AutoRating as suggestion"), Config.AutoRatingSuggest);
                Config.AutoRatingDisplay = RowToggle(Lang.T("右下角显示 AutoRating（TUF / Lv.GG）", "Show AutoRating in corner (TUF / Lv.GG)"), Config.AutoRatingDisplay);
                Config.ClsBadge = RowToggle(Lang.T("选关界面显示评级图标", "Show rating badge in level select"), Config.ClsBadge);
                Config.SkipRatedPrompt = RowToggle(Lang.T("已评级时跳过评级窗", "Skip rating window when already rated"), Config.SkipRatedPrompt);
                Config.ExitAnimation = RowToggle(Lang.T("退出关卡也用这段动画", "Play this animation when leaving"), Config.ExitAnimation);
                Config.FixLanguage = RowToggle(Lang.T("修复语言被锁成中文", "Fix language locked to Chinese"), Config.FixLanguage);
                Config.Dim = RowSlider(Lang.T("曲图压暗强度", "Cover dim"), Config.Dim, 0f, 1f, "0.00");
                Config.MinLoadingSeconds = RowSlider(Lang.T("加载画面最短时长(秒)", "Min loading time (s)"), Config.MinLoadingSeconds, 0f, 5f, "0.00");
                Config.MaxLoadingSeconds = RowSlider(Lang.T("加载画面最长时长(秒)", "Max loading time (s)"), Config.MaxLoadingSeconds, 10f, 300f, "0");

                GUILayout.BeginHorizontal();
                GUILayout.Label(Lang.T("局内改评级快捷键", "In-game rating hotkey"), GUILayout.Width(232f));
                if (GUILayout.Button(string.IsNullOrEmpty(Config.Hotkey) ? Lang.T("未设置（点我绑定）", "Not set (click)") : Config.Hotkey, GUILayout.Width(140f)))
                {
                    _waitingHotkey = true;
                }
                if (_waitingHotkey)
                {
                    GUILayout.Label(Lang.T("请按一个键…（Esc 取消）", "Press a key… (Esc cancels)"), GUILayout.Width(220f));
                    if (Event.current != null && Event.current.type == EventType.KeyDown)
                    {
                        KeyCode key = Event.current.keyCode;
                        if (key == KeyCode.Escape) _waitingHotkey = false;
                        else if (key != KeyCode.None)
                        {
                            Config.Hotkey = key.ToString();
                            _waitingHotkey = false;
                        }
                        Event.current.Use();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Lang.T("AutoRating 连接状态", "AutoRating status"), GUILayout.Width(232f));
                GUILayout.Label(AutoRatingBridge.Available ? Lang.T("已连接", "connected") : Lang.T("未找到（未装/未启用）", "not found"), GUILayout.Width(260f));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(4f);
                if (GUILayout.Button(Lang.T("预览一次", "Preview"), GUILayout.Width(120f)))
                {
                    StartPreview();
                }
                GUILayout.Label(Lang.T("依次预览：角落文字 → 加载画面 → 评级窗", "corner labels → loading screen → rating window"));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + Lang.T("游戏内 · 左下角曲名", "In game · bottom-left song title") + "</b>");
                Config.TitleX = RowSlider(Lang.T("离左边距", "Left margin"), Config.TitleX, 0f, 400f, "0");
                Config.TitleY = RowSlider(Lang.T("离底边距", "Bottom margin"), Config.TitleY, 0f, 400f, "0");
                Config.TitleFont = RowSlider(Lang.T("字号", "Font size"), Config.TitleFont, 8f, 140f, "0");
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + Lang.T("游戏内 · 右下角评级", "In game · bottom-right rating") + "</b>");
                Config.RatingX = RowSlider(Lang.T("离右边距", "Right margin"), Config.RatingX, 0f, 400f, "0");
                Config.RatingY = RowSlider(Lang.T("离底边距", "Bottom margin"), Config.RatingY, 0f, 400f, "0");
                Config.RatingFont = RowSlider(Lang.T("字号", "Font size"), Config.RatingFont, 8f, 140f, "0");
                Config.ClsBadgeOffsetX = RowSlider(Lang.T("选关图标 水平偏移", "Level-select badge X"), Config.ClsBadgeOffsetX, -2f, 2f, "0.00");
                Config.ClsBadgeOffsetY = RowSlider(Lang.T("选关图标 垂直偏移", "Level-select badge Y"), Config.ClsBadgeOffsetY, -2f, 2f, "0.00");
                Config.ClsJudgeScale = RowSlider(Lang.T("判定图标 大小", "Judge icon size"), Config.ClsJudgeScale, 0.3f, 3f, "0.00");
                Config.ClsJudgeOffsetX = RowSlider(Lang.T("判定图标 水平偏移", "Judge icon X"), Config.ClsJudgeOffsetX, -2f, 2f, "0.00");
                Config.ClsJudgeOffsetY = RowSlider(Lang.T("判定图标 垂直偏移", "Judge icon Y"), Config.ClsJudgeOffsetY, -2f, 2f, "0.00");
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + Lang.T("加载画面 · 右侧曲名/曲师/谱师", "Loading screen · right info block") + "</b>");
                Config.InfoRight = RowSlider(Lang.T("离右边距", "Right margin"), Config.InfoRight, 0f, 700f, "0");
                Config.InfoY = RowSlider(Lang.T("垂直偏移(向上为正)", "Vertical offset (+ up)"), Config.InfoY, -500f, 500f, "0");
                Config.LoadTitleFont = RowSlider(Lang.T("曲名字号", "Title size"), Config.LoadTitleFont, 10f, 160f, "0");
                Config.LoadArtistFont = RowSlider(Lang.T("曲师字号", "Artist size"), Config.LoadArtistFont, 8f, 120f, "0");
                Config.LoadChartFont = RowSlider(Lang.T("谱师字号", "Charter size"), Config.LoadChartFont, 8f, 120f, "0");
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + Lang.T("加载画面 · 右下角评级图标", "Loading screen · bottom-right rating icon") + "</b>");
                Config.DiffHeight = RowSlider(Lang.T("图标高度", "Icon height"), Config.DiffHeight, 40f, 800f, "0");
                Config.DiffRight = RowSlider(Lang.T("离右边距", "Right margin"), Config.DiffRight, 0f, 800f, "0");
                Config.DiffBottom = RowSlider(Lang.T("离底边距", "Bottom margin"), Config.DiffBottom, 0f, 800f, "0");
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + Lang.T("加载画面 · 顶部加载条", "Loading screen · top bar") + "</b>");
                Config.BarThickness = RowSlider(Lang.T("加载条高度", "Bar height"), Config.BarThickness, 1f, 60f, "0");
                Config.BarSweep = RowToggle(Lang.T("Phigros 加载条（红蓝交替扫过）", "Phigros bar (red/blue sweep)"), Config.BarSweep);
                Config.BarSweepSeconds = RowSlider(Lang.T("扫过一次时长(秒)", "Sweep duration (s)"), Config.BarSweepSeconds, 0.3f, 5f, "0.00");
                Config.BarSweepWidth = RowSlider(Lang.T("色块宽度(占比)", "Segment width (ratio)"), Config.BarSweepWidth, 0.1f, 0.6f, "0.00");
                Config.PercentFont = RowSlider(Lang.T("百分比字号", "Percentage size"), Config.PercentFont, 8f, 120f, "0");
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Lang.T("恢复默认", "Reset defaults"), GUILayout.Width(120f)))
                {
                    Config.Reset();
                    Settings.Save();
                }
                GUILayout.Label(Lang.T("改完点 UMM 右下角 Save 写入 config.json（改动实时生效）", "Press Save in UMM to write config.json (changes apply live)"));
                GUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                Error(e);
            }
        }
        private static bool RowToggle(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(232f));
            bool result = GUILayout.Toggle(value, "", GUILayout.Width(30f));
            GUILayout.EndHorizontal();
            return result;
        }

        private static float RowSlider(string label, float value, float min, float max, string format)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(232f));
            float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(260f));
            GUILayout.Label(result.ToString(format), GUILayout.Width(60f));
            GUILayout.EndHorizontal();
            return result;
        }
    }
}

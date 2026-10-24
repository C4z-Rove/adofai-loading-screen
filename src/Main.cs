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

        /// <summary>模组版本（显示在面板上）。</summary>
        public const string VersionText = "3.1.2";

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
                PhigrosOverlay.PurgeStaleOverlays();
                // 让加载动画在窗口失焦/后台时也继续跑
                Application.runInBackground = true;

                Log.Log("Adofai Loading Screen v3.5.4 loaded. ModPath = " + ModPath);
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
                // 快捷键捕获
                if (_waitingHotkey && Event.current != null && Event.current.type == EventType.KeyDown)
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

                SettingsUI.Draw();
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        public static void BeginHotkeyCapture()
        {
            _waitingHotkey = true;
        }

        public static bool IsCapturingHotkey
        {
            get { return _waitingHotkey; }
        }

        public static void OpenModFolder()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + ModPath + "\"");
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        public static string[] ListAnimFolders()
        {
            try
            {
                string root = System.IO.Path.Combine(ModPath, "Animations");
                if (!System.IO.Directory.Exists(root)) return new string[0];

                string[] dirs = System.IO.Directory.GetDirectories(root);
                string[] names = new string[dirs.Length];
                for (int i = 0; i < dirs.Length; i++) names[i] = System.IO.Path.GetFileName(dirs[i]);
                Array.Sort(names, StringComparer.OrdinalIgnoreCase);
                return names;
            }
            catch (Exception e)
            {
                Error(e);
                return new string[0];
            }
        }

        public static string NextAnimFolder(string[] folders, string current)
        {
            if (folders == null || folders.Length == 0) return "";
            int index = Array.FindIndex(folders, f => string.Equals(f, current, StringComparison.OrdinalIgnoreCase));
            index = (index + 1) % folders.Length;
            return folders[index];
        }

        public static void OpenAnimFolder()
        {
            try
            {
                string root = System.IO.Path.Combine(ModPath, "Animations");
                if (!System.IO.Directory.Exists(root)) System.IO.Directory.CreateDirectory(root);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + root + "\"");
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

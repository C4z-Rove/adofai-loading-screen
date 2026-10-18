using System;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 局内快捷键：暂停 / 未开始 / 结算时按下，直接改这张谱面的评级。
    /// </summary>
    public class HotkeyWatcher : MonoBehaviour
    {
        private static HotkeyWatcher _instance;
        private KeyCode _key = KeyCode.F7;
        private string _keySource;

        public static void Init()
        {
            if (_instance != null) return;
            try
            {
                GameObject go = new GameObject("PhigrosLoadingHotkey");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<HotkeyWatcher>();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        // ------------------------------------------------------------------
        private static KeyCode ParseHotkey(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    return (KeyCode)Enum.Parse(typeof(KeyCode), text.Trim(), true);
                }
                catch
                {
                    // 忽略，用默认值
                }
            }
            return KeyCode.F7;
        }

        private void Update()
        {
            try
            {
                if (!Main.Active) return;
                Settings cfg = Main.Config;
                if (cfg == null) return;

                string source = cfg.Hotkey ?? "";
                if (source != _keySource)
                {
                    _keySource = source;
                    _key = ParseHotkey(source);
                }

                if (!Input.GetKeyDown(_key)) return;

                TryOpenRating();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        /// <summary>暂停 / 未开始（press to start、倒计时）/ 结算（Won、Fail）时才允许改。</summary>
        private static bool CanOpenNow()
        {
            if (!ADOBase.isScnGame || ADOBase.isLevelEditor) return false;

            scnGame game = scnGame.instance;
            if (game == null || game.isLoading) return false;

            scrController controller = scrController.instance;
            if (controller == null) return false;

            if (controller.paused) return true;

            States state = controller.currentState;
            return state == States.Start
                || state == States.Countdown
                || state == States.Checkpoint
                || state == States.Won
                || state == States.Fail
                || state == States.Fail2;
        }

        private static void TryOpenRating()
        {
            if (RatingPrompt.IsOpen) return;
            if (!CanOpenNow()) return;

            string path = Patch_LoadSceneWithTransition.CurrentLevelPath();
            if (string.IsNullOrEmpty(path))
            {
                Main.Info("hotkey: this level has no chart file, cannot rate");
                return;
            }

            Main.Info("hotkey: opening rating prompt for " + path);
            RatingPrompt.Show(path, Patch_LoadSceneWithTransition.CurrentSong(),
                Patch_LoadSceneWithTransition.CurrentArtist(), null);
        }
    }
}

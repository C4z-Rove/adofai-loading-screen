using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 进入关卡时接管黑屏；离开关卡前弹出评级。
    /// </summary>
    [HarmonyPatch(typeof(scrLoader), "LoadSceneWithTransition")]
    internal static class Patch_LoadSceneWithTransition
    {
        private static bool _intercepting;
        private static bool _pendingExit;
        private static MethodInfo _original;

        private static bool Prefix(scrLoader __instance, WipeDirection direction, string scene)
        {
            try
            {
                // 复刻原方法开头的行为，后面真正调用时也会再执行一次
                // scene == null 且当前在关卡里 = 退出；进关/重开会显式传 "scnGame"
                _pendingExit = scene == null && ADOBase.isScnGame;

                if (scene != null) GCS.sceneToLoad = scene;

                // 弹窗打开期间的任何再次触发都吞掉
                if (_intercepting) return false;

                if (!Main.Active) return true;
                if (Main.Config == null || !Main.Config.RatingPrompt) return true;
                if (!ADOBase.isScnGame) return true;
                if (GameWatcher.PromptShown) return true;

                // 重试 / 明确指定目标场景（重开本关会传 "scnGame"）不打断
                if (scene != null) return true;

                string path = CurrentLevelPath();
                if (string.IsNullOrEmpty(path)) return true; // 官方关卡没有谱面文件，跳过

                // 已经评过级：直接跳过评级窗，走退出动画
                if (Main.Config.SkipRatedPrompt && !string.IsNullOrEmpty(RatingMarks.GetMark(path))) return true;

                GameWatcher.PromptShown = true;
                _intercepting = true;

                string song = CurrentSong();
                string artist = CurrentArtist();

                RatingPrompt.Show(path, song, artist, delegate
                {
                    _intercepting = false;
                    try
                    {
                        if (_original == null)
                        {
                            _original = AccessTools.Method(typeof(scrLoader), "LoadSceneWithTransition");
                        }
                        _original.Invoke(__instance, new object[] { direction, scene });
                    }
                    catch (Exception e)
                    {
                        Main.Error(e);
                        try { __instance.LoadSceneWithTransition(direction, scene); }
                        catch (Exception e2) { Main.Error(e2); }
                    }
                });

                return false;
            }
            catch (Exception e)
            {
                Main.Error(e);
                _intercepting = false;
                return true;
            }
        }

        private static void Postfix()
        {
            try
            {
                if (!Main.Active || (Main.Config != null && !Main.Config.Enabled))
                {
                    PhigrosOverlay.HideImmediate();
                    return;
                }

                if (_pendingExit)
                {
                    _pendingExit = false;

                    BossStage.Disconnect();
                    PhigrosOverlay.HideImmediate();

                    if (PhigrosOverlay.ShouldFadeExit()) ExitFade.Play();
                    return;
                }

                LoadingInfo info = LoadingInfo.Create();
                if (info == null)
                {
                    // 不是进入关卡（回主菜单 / 选关界面等），保持原版黑屏
                    PhigrosOverlay.HideImmediate();
                    return;
                }

                Main.Info("loading screen for: " + info.Key + " | " + info.Title + " - " + info.Artist + " | mark " + info.Mark);
                if (info.IsExit)
                {
                    // 退出：先掐掉进关演出，再走纯淡出
                    BossStage.Disconnect();
                    PhigrosOverlay.HideImmediate();

                    if (PhigrosOverlay.ShouldFadeExit())
                    {
                        ExitFade.Play();
                        return;
                    }
                }

                // 进关：boss 样式用独立的演出 canvas，播完自毁
                if (!info.IsExit && Main.Config != null
                    && string.Equals(Main.Config.AnimStyle, LoadingAnim.StyleBoss, StringComparison.OrdinalIgnoreCase))
                {
                    string ggText = info.GGText;
                    if (string.IsNullOrEmpty(ggText)
                        && string.Equals(Main.Config.BossRatingSource, "gg", StringComparison.OrdinalIgnoreCase))
                    {
                        ggText = AutoRatingBridge.GetCachedGG(info.LevelPath);
                    }

                    BossStage.Play(info.Mark, ggText);
                    return;
                }

                PhigrosOverlay.Show(info);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        // ------------------------------------------------------------------
        internal static string CurrentLevelPath()
        {
            try
            {
                if (ADOBase.isScnGame)
                {
                    scnGame game = scnGame.instance;
                    if (game != null && !string.IsNullOrEmpty(game.levelPath)) return game.levelPath;
                }
            }
            catch { }

            try
            {
                if (GCS.customLevelPaths != null && GCS.customLevelPaths.Length > 0)
                {
                    int index = Mathf.Clamp(GCS.customLevelIndex, 0, GCS.customLevelPaths.Length - 1);
                    return GCS.customLevelPaths[index];
                }
            }
            catch { }

            return null;
        }

        internal static string CurrentSong()
        {
            try
            {
                scnGame game = scnGame.instance;
                if (game != null && game.levelData != null) return UiKit.StripRichText(game.levelData.song);
            }
            catch { }
            return null;
        }

        internal static string CurrentArtist()
        {
            try
            {
                scnGame game = scnGame.instance;
                if (game != null && game.levelData != null) return UiKit.StripRichText(game.levelData.artist);
            }
            catch { }
            return null;
        }
    }

    /// <summary>
    /// 新关卡开始 -> 重置「本关是否通关 / 是否已弹过窗」。
    /// </summary>
    [HarmonyPatch(typeof(scnGame), "Awake")]
    internal static class Patch_ScnGameAwake
    {
        private static void Postfix()
        {
            GameWatcher.OnLevelStart();
        }
    }

    /// <summary>
    /// 新场景加载完毕、黑屏 wipe 退场 -> 加载画面淡出。
    /// </summary>
    [HarmonyPatch(typeof(scrLoader), "WipeFromBlack")]
    internal static class Patch_WipeFromBlack
    {
        private static void Prefix()
        {
            try
            {
                PhigrosOverlay.Complete();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }
    }

    /// <summary>
    /// 关卡 JSON 真正解析完成后，用权威数据补齐/修正文字与曲图。
    /// </summary>
    [HarmonyPatch(typeof(scnGame), "LoadLevel")]
    internal static class Patch_LoadLevel
    {
        private static void Postfix(scnGame __instance, string levelPath, bool __result)
        {
            try
            {
                if (!__result || __instance == null || __instance.levelData == null) return;
                if (!PhigrosOverlay.IsShowing) return;

                LoadingInfo info = LoadingInfo.Create();
                if (info == null) return;

                LoadingInfo.ApplyLevelData(info, __instance.levelData, levelPath);
                PhigrosOverlay.Refresh(info);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace PhigrosLoading
{
    /// <summary>
    /// 自定义关卡选关界面（CLS）里的徽章：
    ///   · 完成度/挑战次数 右上角：评级图标
    ///   · Best XACC 那一行右边：通关判定图标（wide/normal/strict）
    /// 位置稳定一小会儿后才淡入；切换谱面/面板刷新时先淡出，避免瞬移。
    /// </summary>
    public class ClsBadge : MonoBehaviour
    {
        private static ClsBadge _instance;

        private static FieldInfo _fLevelToSelect;
        private static FieldInfo _fLoadedLevelDirs;
        private static bool _fieldsBound;

        /// <summary>徽章通用状态：延迟稳定 + 淡入淡出。</summary>
        private class Badge
        {
            public RectTransform Rect;
            public Image Image;
            public CanvasGroup Group;
            public Vector2 LastPos;
            public bool HasLast;
            public int Stable;
            public float Target;
            public string ShownValue;

            public void Ensure(Transform parent, string name)
            {
                if (Rect != null && Image != null) return;

                GameObject go = new GameObject(name, typeof(RectTransform));
                Rect = (RectTransform)go.transform;
                Rect.SetParent(parent, false);
                Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
                Rect.pivot = new Vector2(0.5f, 0.5f);

                Image = go.AddComponent<Image>();
                Image.raycastTarget = false;
                Image.preserveAspect = true;
                Image.enabled = false;

                Group = go.AddComponent<CanvasGroup>();
                Group.alpha = 0f;
                Group.blocksRaycasts = false;
                Group.interactable = false;

                HasLast = false;
                Stable = 0;
                Target = 0f;
                ShownValue = null;
            }

            public void Reset()
            {
                HasLast = false;
                Stable = 0;
                Target = 0f;
                ShownValue = null;
                if (Group != null) Group.alpha = 0f;
                if (Image != null) Image.enabled = false;
            }

            public void SetSprite(Sprite sprite)
            {
                if (Image == null || sprite == null) return;
                if (Image.sprite != sprite) Image.sprite = sprite;
                Image.enabled = true;
            }
        }

        private readonly Badge _markBadge = new Badge();
        private readonly Badge _judgeBadge = new Badge();

        private scnCLS _owner;
        private string _levelKey;
        private float _nextRefresh;

        // ------------------------------------------------------------------
        public static void Init()
        {
            if (_instance != null) return;
            try
            {
                GameObject go = new GameObject("PhigrosClsBadge");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<ClsBadge>();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        public static void HideAll()
        {
            if (_instance == null) return;
            _instance._markBadge.Reset();
            _instance._judgeBadge.Reset();
            _instance._levelKey = null;
        }

        // ------------------------------------------------------------------
        private void Update()
        {
            // 每帧只做淡入淡出，位置计算放到 0.3s 的 Refresh 里
            float dt = Time.unscaledDeltaTime;
            FadeStep(_markBadge, dt);
            FadeStep(_judgeBadge, dt);

            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.3f;

            try
            {
                Refresh();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private static void FadeStep(Badge badge, float dt)
        {
            if (badge.Group == null) return;

            badge.Group.alpha = Mathf.MoveTowards(badge.Group.alpha, badge.Target, dt * 5f);

            if (badge.Image != null)
            {
                if (badge.Target > 0f && !badge.Image.enabled && badge.Image.sprite != null) badge.Image.enabled = true;
                if (badge.Target <= 0f && badge.Group.alpha <= 0.01f) badge.Image.enabled = false;
            }
        }

        private static void BindFields()
        {
            if (_fieldsBound) return;
            _fieldsBound = true;
            try
            {
                _fLevelToSelect = AccessTools.Field(typeof(scnCLS), "levelToSelect");
                _fLoadedLevelDirs = AccessTools.Field(typeof(scnCLS), "loadedLevelDirs");
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private void Refresh()
        {
            if (!Main.Active || (Main.Config != null && !Main.Config.ClsBadge))
            {
                HideAll();
                return;
            }

            scnCLS cls = scnCLS.instance;
            if (cls == null || cls.portalStats == null || cls.levelInfoCanvas == null)
            {
                HideAll();
                return;
            }

            if (!cls.portalStats.gameObject.activeInHierarchy || !cls.portalStats.enabled)
            {
                HideAll();
                return;
            }

            BindFields();

            string key = _fLevelToSelect != null ? _fLevelToSelect.GetValue(cls) as string : null;
            if (string.IsNullOrEmpty(key) || key.StartsWith("Folder:", StringComparison.OrdinalIgnoreCase))
            {
                HideAll();
                return;
            }

            if (_owner != cls)
            {
                _owner = cls;
                _markBadge.Reset();
                _judgeBadge.Reset();
                _levelKey = null;
            }

            if (_levelKey != key)
            {
                _levelKey = key;
                _markBadge.Reset();
                _judgeBadge.Reset();
            }

            Transform parent = cls.levelInfoCanvas;
            if (parent == null) parent = cls.portalStats.transform.parent;

            string levelPath = ResolveLevelPath(cls, key);

            // ---------- 评级 ----------
            string mark = ResolveMark(levelPath);
            if (string.IsNullOrEmpty(mark)) mark = RatingMarks.Unranked;
            Sprite markSprite = RatingMarks.Icon(mark);
            if (markSprite != null)
            {
                _markBadge.Ensure(parent, "PhigrosClsMark");
                _markBadge.SetSprite(markSprite);
                if (_markBadge.ShownValue != mark)
                {
                    _markBadge.ShownValue = mark;
                    Main.Info("cls badge: " + mark + " for " + (levelPath ?? key));
                }

                Vector2 pos;
                float size;
                ComputeMarkPos(cls, out pos, out size);
                ApplyPosition(_markBadge, pos, size);
            }
            else
            {
                _markBadge.Target = 0f;
            }

            // ---------- 判定 ----------
            string judge = RatingMarks.GetJudge(levelPath);
            Sprite judgeSprite = string.IsNullOrEmpty(judge) ? null : RatingMarks.Icon(judge);
            if (judgeSprite != null)
            {
                _judgeBadge.Ensure(parent, "PhigrosClsJudge");
                _judgeBadge.SetSprite(judgeSprite);
                if (_judgeBadge.ShownValue != judge)
                {
                    _judgeBadge.ShownValue = judge;
                    Main.Info("cls judge badge: " + judge);
                }

                Vector2 pos;
                float size;
                ComputeJudgePos(cls, out pos, out size);
                ApplyPosition(_judgeBadge, pos, size);
            }
            else
            {
                _judgeBadge.Target = 0f;
            }
        }

        /// <summary>摆好位置；位置连续 0.6 秒没变才允许淡入。</summary>
        private static void ApplyPosition(Badge badge, Vector2 pos, float size)
        {
            if (badge.Rect == null || badge.Image == null) return;

            badge.Rect.sizeDelta = new Vector2(size, size);
            badge.Rect.anchoredPosition = pos;

            if (!badge.HasLast || (pos - badge.LastPos).sqrMagnitude > 4f)
            {
                badge.HasLast = true;
                badge.LastPos = pos;
                badge.Stable = 0;
                badge.Target = 0f;   // 还在动：先藏着
                return;
            }

            badge.Stable++;
            if (badge.Stable >= 2) badge.Target = 1f;   // 稳定约 0.6s -> 淡入
        }

        private static string ResolveMark(string levelPath)
        {
            if (string.IsNullOrEmpty(levelPath)) return RatingMarks.Unranked;

            string mark = RatingMarks.GetMark(levelPath);
            if (!string.IsNullOrEmpty(mark)) return mark;

            if (Main.Config == null || Main.Config.AutoRatingDisplay)
            {
                string auto = AutoRatingBridge.GetCachedMark(levelPath);
                if (!string.IsNullOrEmpty(auto)) return auto;
            }

            return RatingMarks.Unranked;
        }

        /// <summary>CLS 里只存目录，这里推回具体的 .adofai 路径。</summary>
        private static string ResolveLevelPath(scnCLS cls, string key)
        {
            string dir = null;
            try
            {
                Dictionary<string, string> dirs = _fLoadedLevelDirs != null
                    ? _fLoadedLevelDirs.GetValue(cls) as Dictionary<string, string>
                    : null;
                if (dirs != null && dirs.ContainsKey(key)) dir = dirs[key];
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;

            try
            {
                string main = Path.Combine(dir, "main.adofai");
                if (File.Exists(main)) return main;

                string backup = Path.Combine(dir, "backup.adofai");
                if (File.Exists(backup)) return backup;

                string[] files = Directory.GetFiles(dir, "*.adofai");
                if (files.Length > 0) return files[0];
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            return null;
        }

        private static void ComputeMarkPos(scnCLS cls, out Vector2 anchored, out float size)
        {
            RectTransform stats = cls.portalStats.rectTransform;
            Vector3[] corners = new Vector3[4];
            stats.GetWorldCorners(corners);

            size = Mathf.Clamp(stats.rect.height > 1f ? stats.rect.height * 0.55f : 90f, 48f, 170f);
            float margin = size * 0.22f;

            Settings cfg = Main.Config;
            float offsetX = cfg != null ? cfg.ClsBadgeOffsetX : -0.45f;
            float offsetY = cfg != null ? cfg.ClsBadgeOffsetY : 0f;

            RectTransform parent = (RectTransform)stats.parent;
            Vector2 topRight = parent.InverseTransformPoint(corners[2]);

            anchored = topRight + new Vector2(
                margin + size * (0.5f + offsetX),
                margin + size * (0.5f + offsetY));
        }

        private static void ComputeJudgePos(scnCLS cls, out Vector2 anchored, out float size)
        {
            RectTransform stats = cls.portalStats.rectTransform;
            Vector3[] corners = new Vector3[4];
            stats.GetWorldCorners(corners);

            string text = cls.portalStats.text ?? "";
            string[] lines = text.Split('\n');
            int index = FindLine(lines, "XACC");
            if (index < 0) index = FindLine(lines, "准度");
            if (index < 0) index = FindLine(lines, "Accuracy");
            if (index < 0) index = 0;

            float height = corners[1].y - corners[0].y;
            float lineHeight = height / Mathf.Max(1, lines.Length);
            float centerY = corners[1].y - (index + 0.5f) * lineHeight;
            float rightX = corners[2].x;

            Settings cfg = Main.Config;
            float scale = cfg != null ? Mathf.Clamp(cfg.ClsJudgeScale, 0.3f, 3f) : 1.15f;
            float offsetX = cfg != null ? cfg.ClsJudgeOffsetX : 0.25f;
            float offsetY = cfg != null ? cfg.ClsJudgeOffsetY : 0f;

            size = Mathf.Clamp(lineHeight * scale, 20f, 160f);

            RectTransform parent = (RectTransform)stats.parent;
            Vector2 local = parent.InverseTransformPoint(new Vector3(rightX, centerY, 0f));
            anchored = local + new Vector2(size * offsetX, size * offsetY);
        }

        private static int FindLine(string[] lines, string keyword)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != null && lines[i].IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return i;
            }
            return -1;
        }
    }
}

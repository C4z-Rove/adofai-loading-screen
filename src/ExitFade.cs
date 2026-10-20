using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhigrosLoading
{
    /// <summary>
    /// 退出关卡时的专属动画：
    ///   1) 屏幕被 8 条黑色横带从上下交错「合拢」（带一点横向抖动，像画面撕裂）
    ///   2) 一条红/蓝细线扫过
    ///   3) 全黑后整体淡出，接选关界面
    /// 自己建最小 canvas，播完自毁，不碰加载画面的任何对象。
    /// </summary>
    public class ExitFade : MonoBehaviour
    {
        private const int Bands = 10;
        private const float CloseSeconds = 0.22f;
        private const float HoldSeconds = 0.16f;
        private const float OpenSeconds = 0.28f;

        private readonly List<RectTransform> _bars = new List<RectTransform>();
        private readonly List<float> _delays = new List<float>();
        private CanvasGroup _group;
        private Image _accent;
        private Image _accent2;
        private Image _flash;
        private float _start;
        private bool _opening;
        private float _openStart;

        public static void Play()
        {
            try
            {
                GameObject go = new GameObject("PhigrosExitFade");
                UnityEngine.Object.DontDestroyOnLoad(go);

                Canvas canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32766;

                CanvasScaler scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;

                RectTransform root = (RectTransform)go.transform;

                ExitFade stage = go.AddComponent<ExitFade>();
                stage._group = go.AddComponent<CanvasGroup>();
                stage._group.alpha = 1f;
                stage._group.blocksRaycasts = true;
                stage._group.interactable = false;

                // 8 条横带：奇数从下往上、偶数从上往下合拢
                for (int i = 0; i < Bands; i++)
                {
                    GameObject barGo = new GameObject("Bar" + i, typeof(RectTransform));
                    RectTransform bar = (RectTransform)barGo.transform;
                    bar.SetParent(root, false);

                    float top = 1f - (i + 1) / (float)Bands;
                    float bottom = top + 1f / Bands;

                    bar.anchorMin = new Vector2(0f, top);
                    bar.anchorMax = new Vector2(1f, bottom);
                    bar.pivot = new Vector2(0.5f, (i % 2 == 0) ? 1f : 0f);
                    bar.offsetMin = Vector2.zero;
                    bar.offsetMax = Vector2.zero;
                    bar.localScale = new Vector3(1f, 0f, 1f);

                    Image image = barGo.AddComponent<Image>();
                    image.color = Color.black;
                    image.raycastTarget = false;

                    stage._bars.Add(bar);
                    stage._delays.Add(UnityEngine.Random.Range(0f, 0.07f));
                }

                // 扫过的红线
                GameObject accentGo = new GameObject("Accent", typeof(RectTransform));
                RectTransform accent = (RectTransform)accentGo.transform;
                accent.SetParent(root, false);
                accent.anchorMin = new Vector2(0f, 0.5f);
                accent.anchorMax = new Vector2(1f, 0.5f);
                accent.pivot = new Vector2(0.5f, 0.5f);
                accent.sizeDelta = new Vector2(0f, 4f);
                accent.anchoredPosition = new Vector2(0f, 540f);
                stage._accent = accentGo.AddComponent<Image>();
                stage._accent.color = new Color(1f, 0.22f, 0.29f, 0.95f);
                stage._accent.raycastTarget = false;

                GameObject blueGo = new GameObject("Accent2", typeof(RectTransform));
                RectTransform blue = (RectTransform)blueGo.transform;
                blue.SetParent(root, false);
                blue.anchorMin = new Vector2(0f, 0.5f);
                blue.anchorMax = new Vector2(1f, 0.5f);
                blue.pivot = new Vector2(0.5f, 0.5f);
                blue.sizeDelta = new Vector2(0f, 4f);
                blue.anchoredPosition = new Vector2(0f, -540f);
                stage._accent2 = blueGo.AddComponent<Image>();
                stage._accent2.color = new Color(0.25f, 0.52f, 1f, 0.95f);
                stage._accent2.raycastTarget = false;

                // 白闪
                GameObject flashGo = new GameObject("Flash", typeof(RectTransform));
                RectTransform flashRt = (RectTransform)flashGo.transform;
                flashRt.SetParent(root, false);
                flashRt.anchorMin = Vector2.zero;
                flashRt.anchorMax = Vector2.one;
                flashRt.offsetMin = Vector2.zero;
                flashRt.offsetMax = Vector2.zero;
                stage._flash = flashGo.AddComponent<Image>();
                stage._flash.color = new Color(1f, 1f, 1f, 0f);
                stage._flash.raycastTarget = false;

                stage._start = Time.realtimeSinceStartup;
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private void Update()
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                float t = now - _start;

                float closeT = Mathf.Clamp01(t / (CloseSeconds + 0.1f));

                for (int i = 0; i < _bars.Count; i++)
                {
                    float local = Mathf.Clamp01((t - _delays[i]) / CloseSeconds);
                    local = 1f - Mathf.Pow(1f - local, 3f);   // ease out

                    RectTransform bar = _bars[i];
                    if (bar == null) continue;

                    bar.localScale = new Vector3(1f + (1f - local) * 0.02f, local, 1f);

                    // 合拢过程中的横向抖动（数字撕裂感）
                    if (local > 0.05f && local < 0.95f)
                    {
                        bar.anchoredPosition = new Vector2(UnityEngine.Random.Range(-26f, 26f), 0f);
                    }
                    else
                    {
                        bar.anchoredPosition = Vector2.zero;
                    }
                }

                float at = Mathf.Clamp01(t / 0.26f);
                if (_accent != null)
                {
                    _accent.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(540f, -540f, at));
                    Color c = _accent.color;
                    c.a = 0.95f * (1f - at);
                    _accent.color = c;
                }
                if (_accent2 != null)
                {
                    _accent2.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-540f, 540f, at));
                    Color c2 = _accent2.color;
                    c2.a = 0.95f * (1f - at);
                    _accent2.color = c2;
                }
                if (_flash != null)
                {
                    float ft = Mathf.Clamp01((t - CloseSeconds * 0.75f) / 0.10f);
                    Color fc = _flash.color;
                    fc.a = (1f - ft) * 0.55f;
                    _flash.color = fc;
                }

                // 收拢时的轻微缩放冲击
                float punch = 1f + 0.03f * Mathf.Clamp01(t / CloseSeconds);
                transform.localScale = new Vector3(punch, punch, 1f);

                if (!_opening && t > CloseSeconds + HoldSeconds)
                {
                    _opening = true;
                    _openStart = now;
                }

                if (_opening)
                {
                    float ot = Mathf.Clamp01((now - _openStart) / OpenSeconds);
                    _group.alpha = 1f - ot;

                    if (ot >= 1f)
                    {
                        UnityEngine.Object.Destroy(gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}

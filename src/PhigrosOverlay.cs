using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhigrosLoading
{
    /// <summary>
    /// Phigros 风格的黑屏替代加载画面。
    /// 布局：曲图背景 / 顶部加载条 / 右侧曲名·曲师·谱师 / 右下角难度图
    /// </summary>
    public class PhigrosOverlay : MonoBehaviour
    {
        private static PhigrosOverlay _instance;

        private class Anim
        {
            public Graphic Graphic;
            public Color BaseColor;
            public Vector2 BasePosition;
            public float Delay;
            public float Slide;
            public float ScaleFrom;
            public float Duration;
        }

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _root;

        private RectTransform _bgRect;
        private RectTransform _infoGroup;
        private LoadingAnim _anim;
        private Anim _difficultyAnim;
        private Image _bgImage;
        private bool _stretchCover;
        private Sprite _fallbackCover;


        private Text _percent;

        private Text _title;
        private Text _artist;
        private Text _chart;
        private Image _difficulty;
        private Image _exitBlack;
        private RectTransform _difficultyRect;

        private readonly List<Anim> _anims = new List<Anim>();

        private float _time;
        private float _progress;
        private bool _showing;
        private bool _completing;
        private bool _completeRequested;
        private float _completeTime;
        private float _showRealtime;
        private float _minDuration = 1.2f;
        private int _screenW;
        private int _screenH;
        private bool _built;
        private string _lastKey = "";

        // ------------------------------------------------------------------
        // 静态入口
        // ------------------------------------------------------------------
        public static void Show(LoadingInfo info)
        {
            Ensure();
            if (_instance != null) _instance.ShowInternal(info);
        }

        public static void Complete()
        {
            if (_instance != null) _instance.BeginComplete();
        }

        public static void HideImmediate()
        {
            if (_instance != null) _instance.HideInternal();
        }

        public static bool IsShowing
        {
            get { return _instance != null && _instance._showing; }
        }

        public static void Refresh(LoadingInfo info)
        {
            if (_instance == null || !_instance._showing || info == null) return;
            _instance.ApplyInfo(info);
        }

        /// <summary>销毁场景里所有残留的 overlay 对象（含孤儿）。</summary>
        public static void PurgeStaleOverlays()
        {
            try
            {
                PhigrosOverlay[] all = Resources.FindObjectsOfTypeAll<PhigrosOverlay>();
                int killed = 0;
                foreach (PhigrosOverlay o in all)
                {
                    if (o == null) continue;
                    if (!o.gameObject.scene.IsValid()) continue;
                    killed++;
                    UnityEngine.Object.DestroyImmediate(o.gameObject);
                }
                _instance = null;
                if (killed > 0) Main.Info("purged " + killed + " stale overlay(s)");
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            // UMM 重载/热重载会留下 DontDestroyOnLoad 的旧 overlay（静态引用丢了、对象还在），
            // 那些孤儿 canvas 会一直把动画留在屏幕上。这里统一清掉。
            PurgeStaleOverlays();
            try
            {
                GameObject go = new GameObject("PhigrosLoadingOverlay", typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<PhigrosOverlay>();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        // ------------------------------------------------------------------
        // 生命周期
        // ------------------------------------------------------------------
        private void Awake()
        {
            try
            {
                Build();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
            gameObject.SetActive(false);
        }

        /// <summary>退出时用纯淡出（黑幕淡入 → 新场景淡出）还是原来的曲图过渡。</summary>
        /// <summary>退出时是否走纯淡出（给 Hooks 判断）。</summary>
        public static bool ShouldFadeExit()
        {
            return UseFadeExit();
        }

        private static bool UseFadeExit()
        {
            Settings cfg = Main.Config;
            if (cfg == null) return false;

            string mode = cfg.ExitStyle ?? "auto";
            if (string.Equals(mode, "fade", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(mode, "overlay", StringComparison.OrdinalIgnoreCase)) return false;

            // auto：进关用的是 boss 演出，就用纯淡出
            return string.Equals(cfg.AnimStyle, LoadingAnim.StyleBoss, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyExitFade(bool on)
        {
            if (_exitBlack != null) _exitBlack.enabled = on;

            bool show = !on;
            if (_bgImage != null) _bgImage.enabled = show;
            if (_bgRect != null) _bgRect.gameObject.SetActive(show);
            if (_infoGroup != null) _infoGroup.gameObject.SetActive(show);
            if (_difficultyRect != null) _difficultyRect.gameObject.SetActive(show);
        }

        private void ShowInternal(LoadingInfo info)
        {
            if (!_built) return;

            if (!Main.Active || (Main.Config != null && !Main.Config.Enabled))
            {
                HideInternal();
                return;
            }

            ApplyInfo(info);

            _time = 0f;
            _progress = 0.06f;
            _completing = false;
            _completeRequested = false;
            _completeTime = 0f;
            _showing = true;
            _group.alpha = 0f;
            _showRealtime = Time.realtimeSinceStartup;
            _minDuration = info.IsExit
                ? 0.45f
                : (Main.Config != null ? Mathf.Max(0f, Main.Config.MinLoadingSeconds) : 1.2f);

            // 退出动画：boss 进关时默认用纯淡出，其它情况保留原来的曲图过渡
            bool exitFade = info.IsExit && UseFadeExit();
            ApplyExitFade(exitFade);
            if (exitFade) _minDuration = 0.18f;

            if (info.IsExit && _anim != null)
            {
                _anim.ForceHide();
            }

            // 先按这张谱面设置好动画素材，才能算准演出时长
            if (_anim != null)
            {
                // 样式可能刚在 UMM 里改过，这里同步一次
                _anim.SetStyle(Main.Config != null ? Main.Config.AnimStyle : LoadingAnim.StyleSweep);

                string ggText = info.GGText;
                if (string.IsNullOrEmpty(ggText) && Main.Config != null
                    && string.Equals(Main.Config.BossRatingSource, "gg", StringComparison.OrdinalIgnoreCase))
                {
                    ggText = AutoRatingBridge.GetCachedGG(info.LevelPath);
                }
                _anim.SetRating(info.Mark, ggText);
                _anim.Restart();
                _anim.SetVisible(!info.IsExit);
            }

            // boss 动画是一段完整演出，至少要放完
            bool bossStyle = !info.IsExit && Main.Config != null
                && string.Equals(Main.Config.AnimStyle, LoadingAnim.StyleBoss, StringComparison.OrdinalIgnoreCase);
            if (bossStyle) _minDuration = Mathf.Max(_anim != null ? _anim.BossDuration(Main.Config) : 4.6f, _minDuration);
            if (_percent != null)
            {
                _percent.gameObject.SetActive(!info.IsExit && (Main.Config == null || Main.Config.ShowPercent));
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Main.Info("overlay SHOWN: exit=" + info.IsExit + " style=" + (Main.Config != null ? Main.Config.AnimStyle : "?") + " min=" + _minDuration.ToString("0.00"));

            _screenW = Screen.width;
            _screenH = Screen.height;
            ApplyLayout();
            FitCover();
            ApplyBar(0f);
            Main.Info("loading screen shown (min " + _minDuration.ToString("0.00") + "s, exit=" + info.IsExit + ")");
        }

        /// <summary>调试：返回当前 overlay / 动画状态。</summary>
        public static string DebugState()
        {
            try
            {
                if (_instance == null) return "overlay=null";
                PhigrosOverlay o = _instance;
                string s = "active=" + o.gameObject.activeSelf + " alpha=" + (o._group != null ? o._group.alpha.ToString("0.00") : "-");
                s += " anim=" + (o._anim != null ? o._anim.DebugState() : "-");
                return s;
            }
            catch (Exception e)
            {
                return "err " + e.Message;
            }
        }

        private void ApplyInfo(LoadingInfo info)
        {
            bool sameLevel = info.Key == _lastKey;
            _lastKey = info.Key;

            SetText(_title, string.IsNullOrEmpty(info.Title) ? info.Key : info.Title);
            SetText(_artist, info.Artist);
            SetText(_chart, string.IsNullOrEmpty(info.Author) ? "" : Lang.T("谱师  ", "Chart  ") + info.Author);

            if (info.Cover != null && (!sameLevel || _bgImage.sprite == null || _stretchCover))
            {
                _stretchCover = info.CoverIsFallback && info.Cover == _fallbackCover;
                _bgImage.sprite = info.Cover;
                _bgImage.color = _stretchCover ? new Color(1f, 1f, 1f, 1f) : Color.white;
                FitCover();
            }

            if (info.DifficultySprite != null)
            {
                _difficulty.sprite = info.DifficultySprite;
                _difficulty.color = Color.white;
                _difficulty.gameObject.SetActive(true);
                FitDifficulty(info.DifficultySprite);
            }
            else
            {
                _difficulty.sprite = null;
                _difficulty.gameObject.SetActive(false);
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text == null) return;
            text.text = value == null ? "" : value;
            text.gameObject.SetActive(!string.IsNullOrEmpty(text.text));
        }

        private void BeginComplete()
        {
            if (!_showing || _completeRequested || _completing) return;
            _completeRequested = true;
        }

        private void HideInternal()
        {
            _showing = false;
            _completing = false;
            _completeRequested = false;
            _lastKey = "";
            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_showing) return;

            if (_screenW != Screen.width || _screenH != Screen.height)
            {
                _screenW = Screen.width;
                _screenH = Screen.height;
                FitCover();
            }

            float dt = Time.unscaledDeltaTime;
            _time += dt;
            float elapsed = Time.realtimeSinceStartup - _showRealtime;

            // 最短显示时间：加载很快时把画面多留一会儿；加载本身就慢就不再延长
            if (_completeRequested && !_completing && elapsed >= _minDuration)
            {
                _completing = true;
                _completeTime = 0f;
                Main.Info("loading screen fade out at " + elapsed.ToString("0.00") + "s");
            }

            float alpha;
            if (_completing)
            {
                _completeTime += dt;
                _progress = Mathf.MoveTowards(_progress, 1f, dt * 5f);
                float fade = Mathf.Clamp01((_completeTime - 0.08f) / 0.36f);
                alpha = 1f - fade;
                if (_completeTime > 0.52f)
                {
                    HideInternal();
                    return;
                }
            }
            else
            {
                if (_time > 0.3f && LevelSceneReady()) BeginComplete();
                if (elapsed > (Main.Config != null ? Mathf.Max(5f, Main.Config.MaxLoadingSeconds) : 90f)) BeginComplete();

                if (_completeRequested)
                {
                    _progress = Mathf.MoveTowards(_progress, 1f, dt * 3f);
                }
                else
                {
                    _progress = Mathf.Min(0.94f, _progress + dt * (0.22f + (0.94f - _progress) * 1.7f));
                }
                alpha = Mathf.Clamp01(_time / 0.18f);
            }

            _group.alpha = alpha;
            if (_anim != null) _anim.Tick(_progress, dt, Main.Config);
            ApplyBar(_progress);
            ApplyAnims();

            if (_bgRect != null && !_stretchCover && (Main.Config == null || Main.Config.KenBurns))
            {
                float zoom = 1.04f + 0.05f * Mathf.Clamp01(_time / 7f);
                _bgRect.localScale = new Vector3(zoom, zoom, 1f);
            }
        }

        private static bool LevelSceneReady()
        {
            try
            {
                if (!ADOBase.isScnGame) return false;
                scnGame game = scnGame.instance;
                return game != null && !game.isLoading;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyAnims()
        {
            for (int i = 0; i < _anims.Count; i++)
            {
                Anim a = _anims[i];
                if (a.Graphic == null) continue;
                float t = Mathf.Clamp01((_time - a.Delay) / a.Duration);
                float e = 1f - Mathf.Pow(1f - t, 3f);

                Color c = a.BaseColor;
                c.a *= e;
                a.Graphic.color = c;

                RectTransform rt = a.Graphic.rectTransform;
                rt.anchoredPosition = a.BasePosition + new Vector2((1f - e) * a.Slide, 0f);
                float scale = Mathf.Lerp(a.ScaleFrom, 1f, e);
                rt.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void ApplyBar(float p)
        {
            p = Mathf.Clamp01(p);

            if (_percent != null && Main.Config != null && Main.Config.ShowPercent)
            {
                _percent.text = Mathf.RoundToInt(p * 100f) + "%";
            }
        }

        // ------------------------------------------------------------------
        // 构建 UI
        // ------------------------------------------------------------------
        private void Build()
        {
            _root = (RectTransform)transform;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32767;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _fallbackCover = UiKit.MakeVerticalGradient(new Color(0.13f, 0.09f, 0.26f), new Color(0.02f, 0.03f, 0.07f));

            // ---------- 背景 ----------
            RectTransform bgWrap = UiKit.Rect("Background", transform);
            UiKit.Stretch(bgWrap);
            bgWrap.gameObject.AddComponent<RectMask2D>();

            _bgRect = UiKit.Rect("Cover", bgWrap);
            _bgImage = UiKit.Image(_bgRect, Color.white, _fallbackCover);
            _stretchCover = true;
            _bgImage.preserveAspect = false;

            // 压暗
            RectTransform dimRect = UiKit.Rect("Dim", transform);
            UiKit.Stretch(dimRect);
            float dim = Main.Config != null ? Mathf.Clamp01(Main.Config.Dim) : 0.42f;
            UiKit.Image(dimRect, new Color(0f, 0f, 0f, dim));

            // 底部渐变，保证文字可读
            RectTransform bottomRect = UiKit.Rect("BottomShade", transform);
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0.58f);
            bottomRect.offsetMin = Vector2.zero;
            bottomRect.offsetMax = Vector2.zero;
            UiKit.Image(bottomRect, Color.white, UiKit.MakeVerticalGradient(new Color(0f, 0f, 0f, 0.82f), new Color(0f, 0f, 0f, 0f)));

            // 顶部渐变，保证加载条可读
            RectTransform topRect = UiKit.Rect("TopShade", transform);
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.offsetMin = new Vector2(0f, -150f);
            topRect.offsetMax = Vector2.zero;
            UiKit.Image(topRect, Color.white, UiKit.MakeVerticalGradient(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.72f)));

            // ---------- 退出用纯淡出黑幕 ----------
        RectTransform exitRect = UiKit.Rect("ExitFade", transform);
        UiKit.Stretch(exitRect);
        _exitBlack = UiKit.Image(exitRect, Color.black);
        _exitBlack.enabled = false;

        // ---------- 动画指示器（样式可切换） ----------
        _anim = new LoadingAnim();
        _anim.Build((RectTransform)transform, Main.Config);
        // 百分比
            RectTransform percentRect = UiKit.Rect("Percent", transform);
            percentRect.anchorMin = new Vector2(1f, 1f);
            percentRect.anchorMax = new Vector2(1f, 1f);
            percentRect.pivot = new Vector2(1f, 1f);
            percentRect.anchoredPosition = new Vector2(-42f, -22f);
            percentRect.sizeDelta = new Vector2(240f, 40f);
            bool showPercent = Main.Config == null || Main.Config.ShowPercent;
            _percent = UiKit.Text(percentRect, "0%", 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperRight);
            _percent.gameObject.SetActive(showPercent);

            // ---------- 右侧信息 ----------
            RectTransform info = UiKit.Rect("Info", transform);
            info.anchorMin = new Vector2(1f, 0.5f);
            info.anchorMax = new Vector2(1f, 0.5f);
            info.pivot = new Vector2(1f, 0.5f);
            info.anchoredPosition = new Vector2(-148f, 66f);
            info.sizeDelta = new Vector2(1180f, 460f);
            _infoGroup = info;

            RectTransform titleRect = UiKit.Rect("Title", info);
            UiKit.Anchor(titleRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 118f), new Vector2(1180f, 160f));
            _title = UiKit.Text(titleRect, "", 62, Color.white, TextAnchor.MiddleRight);
            _title.fontStyle = FontStyle.Bold;
            UiKit.AddShadow(_title, 3f);

            RectTransform artistRect = UiKit.Rect("Artist", info);
            UiKit.Anchor(artistRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 22f), new Vector2(1180f, 52f));
            _artist = UiKit.Text(artistRect, "", 32, new Color(1f, 1f, 1f, 0.88f), TextAnchor.MiddleRight);
            UiKit.AddShadow(_artist, 2f);

            RectTransform chartRect = UiKit.Rect("Chart", info);
            UiKit.Anchor(chartRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -28f), new Vector2(1180f, 44f));
            _chart = UiKit.Text(chartRect, "", 26, new Color(1f, 1f, 1f, 0.62f), TextAnchor.MiddleRight);

            // 右侧竖线装饰
            RectTransform accentRect = UiKit.Rect("Accent", info);
            UiKit.Anchor(accentRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(46f, 66f), new Vector2(4f, 250f));
            UiKit.Image(accentRect, new Color(1f, 1f, 1f, 0.55f));

            // ---------- 右下角难度 ----------
            _difficultyRect = UiKit.Rect("Difficulty", transform);
            _difficultyRect.anchorMin = new Vector2(1f, 0f);
            _difficultyRect.anchorMax = new Vector2(1f, 0f);
            _difficultyRect.pivot = new Vector2(1f, 0f);
            _difficultyRect.anchoredPosition = new Vector2(-120f, 84f);
            _difficultyRect.sizeDelta = new Vector2(460f, 260f);
            _difficulty = UiKit.Image(_difficultyRect, Color.white, null);
            _difficulty.preserveAspect = true;
            _difficulty.gameObject.SetActive(false);

            // ---------- 动画 ----------
            _anims.Add(MakeAnim(_title, new Vector2(0f, 118f), 0f, 90f, 0.18f));
            _anims.Add(MakeAnim(_artist, new Vector2(0f, 22f), 0.04f, 70f, 0.2f));
            _anims.Add(MakeAnim(_chart, new Vector2(0f, -28f), 0.08f, 50f, 0.22f));
            _difficultyAnim = MakeAnim(_difficulty, new Vector2(-120f, 84f), 0.1f, 0f, 0.2f);
            _anims.Add(_difficultyAnim);

            _built = true;
        }

        private Anim MakeAnim(Graphic graphic, Vector2 basePosition, float delay, float slide, float duration)
        {
            Anim anim = new Anim();
            anim.Graphic = graphic;
            anim.BaseColor = graphic.color;
            anim.BasePosition = basePosition;
            anim.Delay = delay;
            anim.Slide = slide;
            anim.ScaleFrom = slide > 0f ? 1f : 0.86f;
            anim.Duration = duration;
            return anim;
        }

        private void FitDifficulty(Sprite sprite)
        {
            if (sprite == null || _difficultyRect == null) return;
            float height = Main.Config != null ? Mathf.Max(20f, Main.Config.DiffHeight) : 240f;
            float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
            _difficultyRect.sizeDelta = new Vector2(Mathf.Clamp(height * aspect, 120f, 700f), height);
        }

        /// <summary>按 config.json / UMM 面板里的数值摆放各元素。</summary>
        private void ApplyLayout()
        {
            Settings cfg = Main.Config;
            if (cfg == null) return;

            if (_infoGroup != null) _infoGroup.anchoredPosition = new Vector2(-cfg.InfoRight, cfg.InfoY);
            if (_title != null) _title.fontSize = Mathf.Max(8, Mathf.RoundToInt(cfg.LoadTitleFont));
            if (_artist != null) _artist.fontSize = Mathf.Max(6, Mathf.RoundToInt(cfg.LoadArtistFont));
            if (_chart != null) _chart.fontSize = Mathf.Max(6, Mathf.RoundToInt(cfg.LoadChartFont));
            if (_percent != null) _percent.fontSize = Mathf.Max(6, Mathf.RoundToInt(cfg.PercentFont));
            if (_anim != null) _anim.ApplyLayout(cfg);

            Vector2 diffPos = new Vector2(-cfg.DiffRight, cfg.DiffBottom);
            if (_difficultyRect != null) _difficultyRect.anchoredPosition = diffPos;
            if (_difficultyAnim != null) _difficultyAnim.BasePosition = diffPos;
        }

        private void FitCover()
        {
            if (_bgRect == null || _root == null) return;

            if (_stretchCover || _bgImage == null || _bgImage.sprite == null)
            {
                _bgRect.anchorMin = Vector2.zero;
                _bgRect.anchorMax = Vector2.one;
                _bgRect.pivot = new Vector2(0.5f, 0.5f);
                _bgRect.offsetMin = Vector2.zero;
                _bgRect.offsetMax = Vector2.zero;
                return;
            }

            Rect area = _root.rect;
            if (area.width <= 1f || area.height <= 1f) return;

            Sprite sprite = _bgImage.sprite;
            float sw = sprite.rect.width;
            float sh = sprite.rect.height;
            if (sw <= 0f || sh <= 0f) return;

            float scale = Mathf.Max(area.width / sw, area.height / sh);
            _bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            _bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            _bgRect.pivot = new Vector2(0.5f, 0.5f);
            _bgRect.anchoredPosition = Vector2.zero;
            _bgRect.sizeDelta = new Vector2(sw * scale, sh * scale);

            bool kenBurns = Main.Config == null || Main.Config.KenBurns;
            if (!kenBurns)
            {
                _bgRect.localScale = new Vector3(1.02f, 1.02f, 1f);
            }
        }
    }
}

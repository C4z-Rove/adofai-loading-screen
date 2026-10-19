using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PhigrosLoading
{
    /// <summary>
    /// 加载画面的动画指示器：内置几种样式 + 自定义序列帧。
    /// 样式：sweep / pulse / wave / flow / spinner / dots / custom
    /// </summary>
    public class LoadingAnim
    {
        public const string StyleSweep = "sweep";
        public const string StylePulse = "pulse";
        public const string StyleWave = "wave";
        public const string StyleFlow = "flow";
        public const string StyleSpinner = "spinner";
        public const string StyleDots = "dots";
        public const string StyleCustom = "custom";
        public const string StyleBoss = "boss";

        public static readonly string[] Styles = new string[]
        {
            StyleSweep, StylePulse, StyleWave, StyleFlow, StyleSpinner, StyleDots, StyleCustom, StyleBoss
        };

        private static readonly Color AccentRed = new Color(1f, 0.22f, 0.29f, 1f);
        private static readonly Color AccentBlue = new Color(0.25f, 0.52f, 1f, 1f);

        // 顶栏
        private RectTransform _track;
        private Image _trackBg;
        private Image _fill;
        private Image _head;
        private RawImage _flow;
        private RectTransform _waveRoot;
        private readonly List<Image> _slices = new List<Image>();

        // 中央
        private RectTransform _center;
        private Image _ring;
        private readonly List<Image> _dots = new List<Image>();
        private Image _custom;

        // boss 动画
        private RectTransform _bossRoot;
        private CanvasGroup _bossGroup;
        private Image _bossBlack;
        private Image _bossLetter;
        private Image _bossGlitchA;
        private Image _bossGlitchB;
        private readonly Text[] _bossDigits = new Text[2];
        private Text _bossGlitchTextA;
        private Text _bossGlitchTextB;
        private string _bossMark;
        private string _bossDigitsValue = "";
        private Sprite _bossLetterSprite;
        private float _bossTime;

        private string _style = StyleSweep;
        private float _time;

        // 自定义序列
        private string _framesKey;
        private Sprite[] _frames;
        private float _frameFps = 24f;
        private int _frameIndex;
        private float _frameTimer;

        // ------------------------------------------------------------------
        // 构建
        // ------------------------------------------------------------------
        public void Build(RectTransform parent, Settings cfg)
        {
            _style = Normalize(cfg != null ? cfg.AnimStyle : null);

            _track = UiKit.Rect("AnimTrack", parent);
            _track.anchorMin = new Vector2(0f, 1f);
            _track.anchorMax = new Vector2(1f, 1f);
            _track.pivot = new Vector2(0.5f, 1f);
            _track.offsetMin = new Vector2(0f, -11f);
            _track.offsetMax = Vector2.zero;

            _trackBg = UiKit.Image(_track, new Color(1f, 1f, 1f, 0.18f));

            RectTransform fillRect = UiKit.Rect("Fill", _track);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(0f, 0f);
            _fill = UiKit.Image(fillRect, Color.white);

            RectTransform headRect = UiKit.Rect("Head", fillRect);
            headRect.anchorMin = new Vector2(1f, 0.5f);
            headRect.anchorMax = new Vector2(1f, 0.5f);
            headRect.pivot = new Vector2(0.5f, 0.5f);
            headRect.anchoredPosition = Vector2.zero;
            headRect.sizeDelta = new Vector2(46f, 46f);
            _head = UiKit.Image(headRect, new Color(1f, 1f, 1f, 0.85f), UiKit.MakeGlow(64, 2.6f));

            // 斜纹
            RectTransform flowRect = UiKit.Rect("Flow", _track);
            flowRect.anchorMin = Vector2.zero;
            flowRect.anchorMax = Vector2.one;
            flowRect.offsetMin = Vector2.zero;
            flowRect.offsetMax = Vector2.zero;
            _flow = flowRect.gameObject.AddComponent<RawImage>();
            _flow.texture = MakeStripeTexture(48, 48);
            _flow.raycastTarget = false;
            _flow.enabled = false;

            // 波浪切片
            _waveRoot = UiKit.Rect("Wave", _track);
            _waveRoot.anchorMin = Vector2.zero;
            _waveRoot.anchorMax = Vector2.one;
            _waveRoot.offsetMin = Vector2.zero;
            _waveRoot.offsetMax = Vector2.zero;
            for (int i = 0; i < 40; i++)
            {
                RectTransform slice = UiKit.Rect("S" + i, _waveRoot);
                slice.anchorMin = new Vector2(i / 40f, 0f);
                slice.anchorMax = new Vector2((i + 1) / 40f, 1f);
                slice.offsetMin = Vector2.zero;
                slice.offsetMax = Vector2.zero;
                Image image = UiKit.Image(slice, Color.white);
                image.enabled = false;
                _slices.Add(image);
            }
            _waveRoot.gameObject.SetActive(false);

            // 中央指示器
            _center = UiKit.Rect("AnimCenter", parent);
            _center.anchorMin = new Vector2(0.5f, 0.5f);
            _center.anchorMax = new Vector2(0.5f, 0.5f);
            _center.pivot = new Vector2(0.5f, 0.5f);
            _center.anchoredPosition = Vector2.zero;
            _center.sizeDelta = new Vector2(220f, 220f);

            RectTransform ringRect = UiKit.Rect("Ring", _center);
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(160f, 160f);
            _ring = UiKit.Image(ringRect, AccentRed, MakeRingSprite(160, 14f));
            _ring.enabled = false;

            for (int i = 0; i < 3; i++)
            {
                RectTransform dotRect = UiKit.Rect("Dot" + i, _center);
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(38f, 38f);
                dotRect.anchoredPosition = new Vector2((i - 1) * 70f, 0f);
                Image dot = UiKit.Image(dotRect, Color.white, UiKit.MakeGlow(64, 1.6f));
                dot.enabled = false;
                _dots.Add(dot);
            }

            RectTransform customRect = UiKit.Rect("Custom", _center);
            customRect.anchorMin = new Vector2(0.5f, 0.5f);
            customRect.anchorMax = new Vector2(0.5f, 0.5f);
            customRect.pivot = new Vector2(0.5f, 0.5f);
            customRect.sizeDelta = new Vector2(220f, 220f);
            _custom = UiKit.Image(customRect, Color.white);
            _custom.preserveAspect = true;
            _custom.enabled = false;

            BuildBoss(parent);
            ApplyLayout(cfg);
        }

        /// <summary>boss 动画：黑屏 → 字母 → 数字从下方升起 → 故障 → 淡出。</summary>
        private void BuildBoss(RectTransform parent)
        {
            _bossRoot = UiKit.Rect("BossRoot", parent);
            _bossRoot.anchorMin = Vector2.zero;
            _bossRoot.anchorMax = Vector2.one;
            _bossRoot.offsetMin = Vector2.zero;
            _bossRoot.offsetMax = Vector2.zero;
            _bossGroup = _bossRoot.gameObject.AddComponent<CanvasGroup>();
            _bossGroup.alpha = 0f;
            _bossGroup.blocksRaycasts = false;
            _bossGroup.interactable = false;

            RectTransform black = UiKit.Rect("Black", _bossRoot);
            UiKit.Stretch(black);
            _bossBlack = UiKit.Image(black, Color.black);

            RectTransform glitchA = UiKit.Rect("GlitchA", _bossRoot);
            glitchA.anchorMin = glitchA.anchorMax = new Vector2(0.5f, 0.5f);
            glitchA.pivot = new Vector2(0.5f, 0.5f);
            glitchA.sizeDelta = new Vector2(240f, 240f);
            glitchA.anchoredPosition = new Vector2(0f, 170f);
            _bossGlitchA = UiKit.Image(glitchA, new Color(1f, 0.2f, 0.3f, 0f));
            _bossGlitchA.preserveAspect = true;

            RectTransform glitchB = UiKit.Rect("GlitchB", _bossRoot);
            glitchB.anchorMin = glitchB.anchorMax = new Vector2(0.5f, 0.5f);
            glitchB.pivot = new Vector2(0.5f, 0.5f);
            glitchB.sizeDelta = new Vector2(240f, 240f);
            glitchB.anchoredPosition = new Vector2(0f, 170f);
            _bossGlitchB = UiKit.Image(glitchB, new Color(0.2f, 0.7f, 1f, 0f));
            _bossGlitchB.preserveAspect = true;

            RectTransform letter = UiKit.Rect("Letter", _bossRoot);
            letter.anchorMin = letter.anchorMax = new Vector2(0.5f, 0.5f);
            letter.pivot = new Vector2(0.5f, 0.5f);
            letter.sizeDelta = new Vector2(240f, 240f);
            letter.anchoredPosition = new Vector2(0f, 170f);
            _bossLetter = UiKit.Image(letter, Color.white);
            _bossLetter.preserveAspect = true;

            _bossGlitchTextA = MakeBossText("GlitchTextA", new Color(1f, 0.25f, 0.35f, 0f));
            _bossGlitchTextB = MakeBossText("GlitchTextB", new Color(0.25f, 0.75f, 1f, 0f));

            for (int i = 0; i < 2; i++)
            {
                _bossDigits[i] = MakeBossText("Digit" + i, Color.white);
                _bossDigits[i].gameObject.SetActive(false);
            }

            _bossRoot.gameObject.SetActive(false);
            _bossRoot.SetAsLastSibling();
        }

        private Text MakeBossText(string name, Color color)
        {
            RectTransform rt = UiKit.Rect(name, _bossRoot);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 340f);
            rt.anchoredPosition = new Vector2(0f, -70f);

            Text text = UiKit.Text(rt, "", 300, color, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            if (color.a <= 0.01f) color.a = 0f;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.enabled = true;
            UiKit.AddOutline(text, 2f);
            return text;
        }

        private static string DigitsOf(string mark)
        {
            if (string.IsNullOrEmpty(mark)) return "";
            int start = -1;
            int end = -1;
            for (int i = 0; i < mark.Length; i++)
            {
                if (char.IsDigit(mark[i]))
                {
                    if (start < 0) start = i;
                    end = i;
                }
            }
            if (start < 0) return "";
            return mark.Substring(start, end - start + 1);
        }

        /// <summary>告诉 boss 动画用哪张评级图（例如 P19）。</summary>
        public void SetMark(string mark)
        {
            if (string.IsNullOrEmpty(mark)) mark = RatingMarks.Unranked;
            if (mark == _bossMark) return;
            _bossMark = mark;

            _bossLetterSprite = null;
            _bossDigitsValue = "";

            try
            {
                string dir = Path.Combine(Main.ModPath, "Marks", "_split");
                string letter = Path.Combine(dir, mark + "_L.png");
                if (File.Exists(letter))
                {
                    _bossLetterSprite = UiKit.LoadSpriteFromFile(letter);
                    _bossDigitsValue = DigitsOf(mark);
                }
                if (_bossLetterSprite == null)
                {
                    // 没有拆分素材就退回整张图（数字已经画在图里，不再叠文字）
                    _bossLetterSprite = RatingMarks.Icon(mark);
                    _bossDigitsValue = "";
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            if (_bossLetter != null)
            {
                _bossLetter.sprite = _bossLetterSprite;
                _bossLetter.enabled = _bossLetterSprite != null;
            }
            if (_bossGlitchA != null) _bossGlitchA.sprite = _bossLetterSprite;
            if (_bossGlitchB != null) _bossGlitchB.sprite = _bossLetterSprite;

            int count = _bossDigitsValue.Length;
            for (int i = 0; i < 2; i++)
            {
                if (_bossDigits[i] == null) continue;
                bool used = i < count;
                _bossDigits[i].gameObject.SetActive(used);
                if (used) _bossDigits[i].text = _bossDigitsValue[i].ToString();
            }
        }

        public void ApplyLayout(Settings cfg)
        {
            if (cfg == null) return;

            if (_track != null)
            {
                float thickness = Mathf.Clamp(cfg.BarThickness, 1f, 60f);
                _track.offsetMin = new Vector2(0f, -thickness);
                _track.offsetMax = Vector2.zero;
            }

            if (_center != null)
            {
                _center.anchoredPosition = new Vector2(0f, cfg.AnimOffsetY);
                float scale = Mathf.Clamp(cfg.AnimScale, 0.2f, 4f);
                _center.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void SetStyle(string style)
        {
            string next = Normalize(style);
            if (next == _style) return;
            _style = next;
            _framesKey = null;
            _frameIndex = 0;
            _bossTime = 0f;
            if (_bossRoot != null) _bossRoot.gameObject.SetActive(false);
        }

        /// <summary>重新开始 boss 动画的计时（每次显示加载画面时调用）。</summary>
        public void Restart()
        {
            _bossTime = 0f;
            _time = 0f;
        }

        public void SetVisible(bool visible)
        {
            if (_bossRoot != null)
            {
                bool show = visible && _style == StyleBoss;
                _bossRoot.gameObject.SetActive(show);
                if (show) _bossRoot.SetAsLastSibling();
            }

            if (_style == StyleBoss)
            {
                if (_track != null) _track.gameObject.SetActive(false);
                if (_center != null) _center.gameObject.SetActive(false);
                return;
            }

            bool barStyle = _style == StyleSweep || _style == StylePulse || _style == StyleWave
                || _style == StyleFlow || (_style == StyleCustom && UseBarSlot());

            if (_track != null) _track.gameObject.SetActive(visible && barStyle);
            if (_center != null) _center.gameObject.SetActive(visible && !barStyle);
        }

        // ------------------------------------------------------------------
        // 每帧
        // ------------------------------------------------------------------
        public void Tick(float progress, float dt, Settings cfg)
        {
            _time += dt;
            float p = Mathf.Clamp01(progress);

            if (_style == StyleBoss)
            {
                if (_bossRoot != null) _bossRoot.gameObject.SetActive(true);
                if (_track != null) _track.gameObject.SetActive(false);
                if (_center != null) _center.gameObject.SetActive(false);
                TickBoss(dt);
                return;
            }

            if (_style == StyleCustom) EnsureFrames(cfg);

            bool barStyle = _style == StyleSweep || _style == StylePulse || _style == StyleWave
                || _style == StyleFlow || (_style == StyleCustom && UseBarSlot());

            if (_track != null && _track.gameObject.activeSelf != barStyle)
                _track.gameObject.SetActive(barStyle);
            if (_center != null && _center.gameObject.activeSelf == barStyle)
                _center.gameObject.SetActive(!barStyle);

            if (barStyle) TickBar(p, cfg);
            else TickCenter(p, dt, cfg);
        }

        // ------------------------------------------------------------------
        // boss 动画
        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        // boss 动画：黑屏 → 圆圈从中心放大 → 数字从下方飞进圆圈 → 故障 → 淡出
        // ------------------------------------------------------------------
        private const float BossCircleY = 40f;
        private const float BossCircleSize = 420f;

        private void TickBoss(float dt)
        {
            _bossTime += dt;
            float t = _bossTime;
            float H = _bossRoot != null ? _bossRoot.rect.height : 1080f;
            if (H <= 1f) H = 1080f;

            // 1) 黑屏淡入
            if (_bossBlack != null)
            {
                Color c = Color.black;
                c.a = Mathf.Clamp01(t / 0.22f);
                _bossBlack.color = c;
            }

            // 2) 圆圈：放大 + 居中
            float circleT = Mathf.Clamp01((t - 0.22f) / 0.55f);
            float circleEase = 1f - Mathf.Pow(1f - circleT, 3f);
            float circleScale = Mathf.Lerp(0.32f, 1f, circleEase);
            if (t > 3.4f) circleScale *= 1f + 0.02f * Mathf.Clamp01((t - 3.4f) / 0.6f);

            Vector2 circlePos = new Vector2(0f, BossCircleY);

            // 故障抖动
            bool burst = t > 2.0f && t < 3.6f && Mathf.Repeat(t - 2.0f, 0.5f) < 0.08f;
            if (burst) circlePos += new Vector2(UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(-3f, 3f));

            if (_bossLetter != null)
            {
                _bossLetter.rectTransform.anchoredPosition = circlePos;
                _bossLetter.rectTransform.sizeDelta = new Vector2(BossCircleSize, BossCircleSize);
                _bossLetter.rectTransform.localScale = new Vector3(circleScale, circleScale, 1f);
                Color c = Color.white;
                c.a = circleT;
                _bossLetter.color = c;
            }

            // 圆圈边缘的彩色残影（故障）
            if (_bossGlitchA != null && _bossGlitchB != null)
            {
                if (burst && _bossLetterSprite != null)
                {
                    float off = UnityEngine.Random.Range(8f, 20f);
                    _bossGlitchA.enabled = true;
                    _bossGlitchB.enabled = true;
                    _bossGlitchA.rectTransform.anchoredPosition = circlePos + new Vector2(-off, 0f);
                    _bossGlitchB.rectTransform.anchoredPosition = circlePos + new Vector2(off, 0f);
                    _bossGlitchA.rectTransform.localScale = new Vector3(circleScale, circleScale, 1f);
                    _bossGlitchB.rectTransform.localScale = new Vector3(circleScale, circleScale, 1f);
                    _bossGlitchA.color = new Color(1f, 0.25f, 0.35f, 0.45f * circleT);
                    _bossGlitchB.color = new Color(0.25f, 0.75f, 1f, 0.45f * circleT);
                }
                else
                {
                    _bossGlitchA.enabled = false;
                    _bossGlitchB.enabled = false;
                }
            }

            // 3) 数字：从下方飞进圆圈里
            float fade = 1f;
            if (t > 4.0f) fade = Mathf.Clamp01(1f - (t - 4.0f) / 0.55f);

            int count = Mathf.Min(2, _bossDigitsValue.Length);
            float finalFont = BossCircleSize * 0.40f;
            float startFont = finalFont * 1.9f;
            float gap = finalFont * 0.62f;
            float targetY = BossCircleY - BossCircleSize * 0.13f;

            for (int i = 0; i < count; i++)
            {
                Text digit = _bossDigits[i];
                if (digit == null) continue;

                float start = 0.80f + i * 0.26f;
                float p = Mathf.Clamp01((t - start) / 0.95f);
                float ease = 1f - Mathf.Pow(1f - p, 3f);

                float y = Mathf.Lerp(-H * 0.62f, targetY, ease);
                float x = (i - (count - 1) * 0.5f) * gap;
                if (burst) x += UnityEngine.Random.Range(-5f, 5f);

                float font = Mathf.Lerp(startFont, finalFont, ease);

                digit.rectTransform.anchoredPosition = new Vector2(x, y);
                digit.fontSize = Mathf.Max(8, Mathf.RoundToInt(font));

                Color c = Color.white;
                c.a = Mathf.Clamp01(p * 4f) * fade * (burst ? 0.75f : 1f);
                digit.color = c;
            }

            // 数字的彩色残影
            if (_bossGlitchTextA != null && _bossGlitchTextB != null)
            {
                if (burst && count > 0)
                {
                    _bossGlitchTextA.text = _bossDigitsValue;
                    _bossGlitchTextB.text = _bossDigitsValue;
                    _bossGlitchTextA.gameObject.SetActive(true);
                    _bossGlitchTextB.gameObject.SetActive(true);

                    float off = UnityEngine.Random.Range(4f, 10f);
                    Vector2 basePos = new Vector2(count > 1 ? -gap * 0.5f : 0f, targetY);
                    _bossGlitchTextA.rectTransform.anchoredPosition = basePos + new Vector2(-off, 0f);
                    _bossGlitchTextB.rectTransform.anchoredPosition = basePos + new Vector2(off, 0f);
                    _bossGlitchTextA.fontSize = Mathf.RoundToInt(finalFont);
                    _bossGlitchTextB.fontSize = Mathf.RoundToInt(finalFont);
                    _bossGlitchTextA.color = new Color(1f, 0.25f, 0.35f, 0.28f * fade);
                    _bossGlitchTextB.color = new Color(0.25f, 0.75f, 1f, 0.28f * fade);
                }
                else
                {
                    _bossGlitchTextA.gameObject.SetActive(false);
                    _bossGlitchTextB.gameObject.SetActive(false);
                }
            }

            if (_bossGroup != null) _bossGroup.alpha = Mathf.Clamp01(t / 0.2f);
        }
        private void TickBar(float p, Settings cfg)
        {
            float width = _track != null ? _track.rect.width : 0f;
            if (width <= 1f) return;

            switch (_style)
            {
                case StylePulse: TickPulse(p, width); break;
                case StyleWave: TickWave(p, width); break;
                case StyleFlow: TickFlow(p, width); break;
                case StyleCustom: TickCustomBar(); break;
                default: TickSweep(width, cfg); break;
            }
        }

        private void TickSweep(float width, Settings cfg)
        {
            SetBarVisible(true, false, false);

            float ratio = cfg != null ? Mathf.Clamp(cfg.BarSweepWidth, 0.05f, 1f) : 0.25f;
            float seconds = cfg != null ? Mathf.Clamp(cfg.BarSweepSeconds, 0.2f, 10f) : 1.1f;

            float segment = width * ratio;
            float cycle = _time / seconds;
            int index = Mathf.FloorToInt(cycle);
            float t = cycle - index;

            float x = Mathf.Lerp(-segment, width, t);

            RectTransform fill = _fill.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(segment, 0f);
            fill.anchoredPosition = new Vector2(x, 0f);

            Color color = (index % 2 == 0) ? AccentRed : AccentBlue;
            _fill.color = color;
            if (_head != null)
            {
                Color head = color;
                head.a = 0.9f;
                _head.color = head;
            }
        }

        private void TickPulse(float p, float width)
        {
            SetBarVisible(true, false, false);

            RectTransform fill = _fill.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Max(0.001f, p), 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            float pulse = 0.62f + 0.38f * Mathf.Sin(_time * 6.2f);
            Color color = Color.Lerp(AccentRed, AccentBlue, 0.5f + 0.5f * Mathf.Sin(_time * 3.1f));
            color.a = pulse;
            _fill.color = color;

            if (_head != null)
            {
                Color head = Color.Lerp(color, Color.white, 0.5f);
                head.a = pulse;
                _head.color = head;
            }
        }

        private void TickWave(float p, float width)
        {
            SetBarVisible(false, false, true);

            float filled = width * p;
            int count = _slices.Count;
            for (int i = 0; i < count; i++)
            {
                float centerX = (i + 0.5f) / count * width;
                bool on = centerX <= filled;
                Image slice = _slices[i];
                if (slice == null) continue;

                if (!on)
                {
                    if (slice.enabled) slice.enabled = false;
                    continue;
                }

                slice.enabled = true;
                float phase = _time * 5.5f - (i / (float)count) * 7.5f;
                float h = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(phase));

                RectTransform rt = slice.rectTransform;
                rt.anchorMin = new Vector2(i / (float)count, 0f);
                rt.anchorMax = new Vector2((i + 1) / (float)count, h);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Color color = Color.Lerp(AccentRed, AccentBlue, 0.5f + 0.5f * Mathf.Sin(phase * 0.5f));
                color.a = 0.9f;
                slice.color = color;
            }
        }

        private void TickFlow(float p, float width)
        {
            SetBarVisible(false, true, false);

            RectTransform flowRect = (RectTransform)_flow.transform;
            flowRect.anchorMin = new Vector2(0f, 0f);
            flowRect.anchorMax = new Vector2(Mathf.Max(0.001f, p), 1f);
            flowRect.offsetMin = Vector2.zero;
            flowRect.offsetMax = Vector2.zero;

            Color color = Color.Lerp(AccentRed, AccentBlue, 0.5f + 0.5f * Mathf.Sin(_time * 1.6f));
            color.a = 0.95f;
            _flow.color = color;

            Rect uv = _flow.uvRect;
            uv.width = Mathf.Max(1f, width * p / 48f);
            uv.height = 1f;
            uv.x = -_time * 1.4f;
            _flow.uvRect = uv;
        }

        private void TickCenter(float p, float dt, Settings cfg)
        {
            switch (_style)
            {
                case StyleSpinner: TickSpinner(p); break;
                case StyleDots: TickDots(p); break;
                default: TickCustomCenter(dt, cfg); break;
            }
        }

        private void TickSpinner(float p)
        {
            SetCenterVisible(true, false, false);

            if (_ring != null)
            {
                _ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -_time * 260f);
                Color color = Color.Lerp(AccentRed, AccentBlue, 0.5f + 0.5f * Mathf.Sin(_time * 2f));
                _ring.color = color;
            }
        }

        private void TickDots(float p)
        {
            SetCenterVisible(false, true, false);

            for (int i = 0; i < _dots.Count; i++)
            {
                Image dot = _dots[i];
                if (dot == null) continue;
                float phase = _time * 4.2f - i * 0.55f;
                float alpha = 0.35f + 0.65f * Mathf.Clamp01(Mathf.Sin(phase) * 0.5f + 0.5f);
                Color color = Color.Lerp(AccentRed, AccentBlue, i / 2f);
                color.a = alpha;
                dot.color = color;
                float scale = 0.8f + 0.35f * alpha;
                dot.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void TickCustomCenter(float dt, Settings cfg)
        {
            SetCenterVisible(false, false, true);
            StepFrames(dt, cfg);
        }

        private void TickCustomBar()
        {
            // 自定义素材放顶栏：铺在进度条区域里
            SetBarVisible(false, true, false);
            if (_flow != null)
            {
                _flow.texture = _custom != null && _custom.sprite != null ? _custom.sprite.texture : _flow.texture;
                RectTransform rt = (RectTransform)_flow.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _flow.color = Color.white;
                _flow.uvRect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        private void StepFrames(float dt, Settings cfg)
        {
            if (_frames == null || _frames.Length == 0) return;

            float fps = _frameFps > 0.1f ? _frameFps : 24f;
            if (cfg != null && cfg.AnimFps > 0.1f) fps = cfg.AnimFps;

            _frameTimer += dt;
            if (_frameTimer >= 1f / fps)
            {
                _frameTimer = 0f;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                if (_custom != null) _custom.sprite = _frames[_frameIndex];
            }
        }

        private bool UseBarSlot()
        {
            Settings cfg = Main.Config;
            return cfg != null && string.Equals(cfg.AnimSlot, "bar", StringComparison.OrdinalIgnoreCase);
        }

        private void SetBarVisible(bool fill, bool flow, bool wave)
        {
            if (_fill != null && _fill.enabled != fill) _fill.enabled = fill;
            if (_head != null && _head.enabled != fill) _head.enabled = fill;
            if (_flow != null && _flow.enabled != flow) _flow.enabled = flow;
            if (_waveRoot != null && _waveRoot.gameObject.activeSelf != wave) _waveRoot.gameObject.SetActive(wave);
            if (_trackBg != null) _trackBg.enabled = true;
        }

        private void SetCenterVisible(bool ring, bool dots, bool custom)
        {
            if (_ring != null && _ring.enabled != ring) _ring.enabled = ring;
            foreach (Image dot in _dots) if (dot != null && dot.enabled != dots) dot.enabled = dots;
            if (_custom != null && _custom.enabled != custom) _custom.enabled = custom;
        }

        // ------------------------------------------------------------------
        // 自定义素材
        // ------------------------------------------------------------------
        private void EnsureFrames(Settings cfg)
        {
            try
            {
                string root = Path.Combine(Main.ModPath, "Animations");
                if (!Directory.Exists(root)) return;

                string wanted = cfg != null ? cfg.AnimFolder : null;
                string dir = null;

                if (!string.IsNullOrEmpty(wanted))
                {
                    string candidate = Path.Combine(root, wanted);
                    if (Directory.Exists(candidate)) dir = candidate;
                }

                if (dir == null)
                {
                    string[] dirs = Directory.GetDirectories(root);
                    if (dirs.Length == 0) return;
                    dir = dirs[0];
                }

                if (dir == _framesKey) return;
                _framesKey = dir;
                _frames = null;
                _frameIndex = 0;
                _frameTimer = 0f;
                _frameFps = 24f;

                LoadFrames(dir);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private void LoadFrames(string dir)
        {
            // 1) sheet.png + anim.json
            string json = Path.Combine(dir, "anim.json");
            string sheet = Path.Combine(dir, "sheet.png");
            if (File.Exists(sheet))
            {
                int columns = 1;
                int frames = 0;
                float fps = 24f;

                if (File.Exists(json))
                {
                    try
                    {
                        JObject obj = JObject.Parse(File.ReadAllText(json));
                        columns = Mathf.Max(1, obj.Value<int?>("columns") ?? 1);
                        frames = obj.Value<int?>("frames") ?? 0;
                        fps = obj.Value<float?>("fps") ?? 24f;
                    }
                    catch (Exception e)
                    {
                        Main.Error(e);
                    }
                }

                Sprite sheetSprite = UiKit.LoadSpriteFromFile(sheet);
                if (sheetSprite == null) return;

                Texture2D tex = sheetSprite.texture;
                int frameWidth = tex.width / columns;
                int frameHeight = tex.height;
                if (frames <= 0) frames = columns;

                frames = Mathf.Clamp(frames, 1, 128);
                Sprite[] result = new Sprite[frames];
                for (int i = 0; i < frames; i++)
                {
                    Rect rect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight);
                    result[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                }

                _frames = result;
                _frameFps = fps;
                Main.Info("custom anim: " + Path.GetFileName(dir) + " sheet, " + frames + " frames @ " + fps + "fps");
                return;
            }

            // 2) PNG 序列
            string[] files = Directory.GetFiles(dir, "*.png");
            if (files.Length == 0) return;

            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            if (files.Length > 96) files = files[0..96];

            List<Sprite> list = new List<Sprite>();
            foreach (string file in files)
            {
                Sprite sprite = UiKit.LoadSpriteFromFile(file);
                if (sprite != null) list.Add(sprite);
            }

            if (list.Count == 0) return;

            _frames = list.ToArray();
            if (File.Exists(json))
            {
                try
                {
                    JObject obj = JObject.Parse(File.ReadAllText(json));
                    _frameFps = obj.Value<float?>("fps") ?? 24f;
                }
                catch (Exception e)
                {
                    Main.Error(e);
                }
            }

            Main.Info("custom anim: " + Path.GetFileName(dir) + " " + _frames.Length + " frames @ " + _frameFps + "fps");
        }

        // ------------------------------------------------------------------
        // 贴图
        // ------------------------------------------------------------------
        private static Texture2D MakeStripeTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int band = ((x + y) % 16 + 16) % 16;
                    float alpha = band < 8 ? 1f : 0.25f;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Sprite MakeRingSprite(int size, float thickness)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float center = size * 0.5f;
            float outer = center - 2f;
            float inner = outer - thickness;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;
                    if (dist <= outer && dist >= inner)
                    {
                        // 留一段缺口，旋转才看得出来
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        alpha = (angle > -90f && angle < 90f) ? 1f : 0.15f;
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static string Normalize(string style)
        {
            if (string.IsNullOrEmpty(style)) return StyleSweep;
            string value = style.Trim().ToLowerInvariant();
            foreach (string item in Styles)
            {
                if (item == value) return value;
            }
            return StyleSweep;
        }
    }
}

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
        private RectTransform _bossParent;
        private CanvasGroup _bossGroup;
        private Image _bossBlack;
        private Image _bossLetter;
        private Image _bossGlitchA;
        private Image _bossGlitchB;
        private readonly Text[] _bossDigits = new Text[2];
        private Text _bossGlitchTextA;
        private Text _bossGlitchTextB;
        private string _bossMark;
        private bool _bossGgMode;
        private string _bossGgValue = "";
        private Text _bossGgPrefix;
        private Text _bossGgRight;
        private Text _bossGlow;
        private Text _bossTrailA;
        private Text _bossTrailB;
        private RectTransform _rollRoot;
        private RectTransform _reelRoot;
        private readonly Text[,] _reelCell = new Text[5, 5];
        private readonly RectTransform[] _glitchBars = new RectTransform[4];
        private readonly Image[] _glitchImgs = new Image[4];
        private readonly RectTransform[] _reelCol = new RectTransform[5];
        private readonly Text[] _reelText = new Text[5];
        private readonly float[] _reelOffset = new float[5];
        private float _reelCycle = 3000f;
        private bool _reelSeeded;
        private int _reelFont = 380;
        private string _bossDigitsValue = "";
        private Sprite _bossLetterSprite;
        private float _bossTime;
        private float _bossRollTick;
        private string _bossRollValue = "";

        private BossKind _bossKind = BossKind.Classic;
        private Image _bossFlash;
        private Image _bossWave;
        private Image _bossSpinRing;

        private bool _visible = true;
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
        private void EnsureBoss()
        {
            if (_bossRoot != null) return;
            if (_bossParent == null) return;
            BuildBoss(_bossParent);
            _bossRoot.gameObject.SetActive(false);
        }

        private void BuildBoss(RectTransform parent)
        {
            _bossParent = parent;
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

            _bossGgPrefix = MakeBossText("GgPrefix", new Color(1f, 1f, 1f, 0.9f));
            _bossGgPrefix.text = "<Lv.";
            _bossGgPrefix.fontSize = 300;
            _bossGgPrefix.gameObject.SetActive(false);

            _bossGgRight = MakeBossText("GgRight", new Color(1f, 1f, 1f, 0.9f));
            _bossGgRight.text = ">";
            _bossGgRight.fontSize = 300;
            _bossGgRight.gameObject.SetActive(false);

            // 五位老虎机：每列 5 个数字格，按位置动态换字，滚动无缝
            RectTransform reelRoot = UiKit.Rect("ReelRoot", _bossRoot);
            reelRoot.anchorMin = reelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            reelRoot.pivot = new Vector2(0.5f, 0.5f);
            reelRoot.sizeDelta = new Vector2(1900f, 420f);
            reelRoot.anchoredPosition = Vector2.zero;
            _reelRoot = reelRoot;

            for (int i = 0; i < 5; i++)
            {
                RectTransform col = UiKit.Rect("ReelCol" + i, reelRoot);
                col.anchorMin = col.anchorMax = new Vector2(0.5f, 0.5f);
                col.pivot = new Vector2(0.5f, 0.5f);
                col.sizeDelta = new Vector2(400f, 900f);
                col.anchoredPosition = new Vector2((i - 2) * 385f, 0f);
                col.gameObject.AddComponent<RectMask2D>();
                _reelCol[i] = col;

                for (int k = 0; k < 5; k++)
                {
                    Text cell = MakeBossText("Cell" + i + "_" + k, Color.white);
                    cell.rectTransform.SetParent(col, false);
                    cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    cell.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    cell.rectTransform.sizeDelta = new Vector2(400f, 400f);
                    cell.rectTransform.anchoredPosition = Vector2.zero;
                    cell.fontSize = 380;
                    cell.fontStyle = FontStyle.Bold;
                    cell.alignment = TextAnchor.MiddleCenter;
                    cell.verticalOverflow = VerticalWrapMode.Overflow;
                    cell.horizontalOverflow = HorizontalWrapMode.Overflow;
                    cell.text = "0";
                    cell.gameObject.SetActive(false);
                    _reelCell[i, k] = cell;
                }
            }

            // 故障条（全屏横向切片）
            for (int i = 0; i < 4; i++)
            {
                GameObject barGo = new GameObject("GlitchBar" + i, typeof(RectTransform));
                RectTransform bar = (RectTransform)barGo.transform;
                bar.SetParent(_bossRoot, false);
                bar.anchorMin = new Vector2(0f, 0.5f);
                bar.anchorMax = new Vector2(1f, 0.5f);
                bar.pivot = new Vector2(0.5f, 0.5f);
                bar.sizeDelta = new Vector2(0f, 20f);
                Image img = barGo.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = false;
                _glitchBars[i] = bar;
                _glitchImgs[i] = img;
                barGo.transform.SetSiblingIndex(0);
            }

            _bossGlow = MakeBossText("Glow", new Color(1f, 1f, 1f, 0f));
            _bossTrailA = MakeBossText("TrailA", new Color(1f, 1f, 1f, 0f));
            _bossTrailB = MakeBossText("TrailB", new Color(1f, 1f, 1f, 0f));

            _bossGlitchTextA = MakeBossText("GlitchTextA", new Color(1f, 0.25f, 0.35f, 0f));
            _bossGlitchTextB = MakeBossText("GlitchTextB", new Color(0.25f, 0.75f, 1f, 0f));

            for (int i = 0; i < 2; i++)
            {
                _bossDigits[i] = MakeBossText("Digit" + i, Color.white);
                _bossDigits[i].gameObject.SetActive(false);
            }

            // 白闪
            RectTransform flash = UiKit.Rect("Flash", _bossRoot);
            UiKit.Stretch(flash);
            _bossFlash = UiKit.Image(flash, new Color(1f, 1f, 1f, 0f));

            // 冲击波
            RectTransform wave = UiKit.Rect("Wave", _bossRoot);
            wave.anchorMin = wave.anchorMax = new Vector2(0.5f, 0.5f);
            wave.pivot = new Vector2(0.5f, 0.5f);
            wave.sizeDelta = new Vector2(300f, 300f);
            wave.anchoredPosition = new Vector2(0f, BossCircleY);
            _bossWave = UiKit.Image(wave, new Color(1f, 1f, 1f, 0f), MakeRingSprite(128, 6f));
            _bossWave.enabled = false;

            // UJ 旋转环
            RectTransform spin = UiKit.Rect("SpinRing", _bossRoot);
            spin.anchorMin = spin.anchorMax = new Vector2(0.5f, 0.5f);
            spin.pivot = new Vector2(0.5f, 0.5f);
            spin.sizeDelta = new Vector2(460f, 460f);
            spin.anchoredPosition = new Vector2(0f, BossCircleY);
            _bossSpinRing = UiKit.Image(spin, new Color(1f, 0.3f, 0.4f, 0f), MakeRingSprite(128, 8f));
            _bossSpinRing.enabled = false;

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

        /// <summary>设置 boss 演出显示什么：GG 有值时用纯文字 Lv.XX，否则用 TUF 图标。</summary>
        public void SetRating(string mark, string gg)
        {
            EnsureBoss();
            _bossGgMode = !string.IsNullOrEmpty(gg);
            _bossGgValue = _bossGgMode ? FormatGG(gg) : "";
            SetMark(mark);
            ApplyRatingVisibility();
        }

        private static string RandomDigits(int length)
        {
            string value = "";
            for (int i = 0; i < length; i++) value += UnityEngine.Random.Range(0, 10).ToString();
            return value;
        }

        private static string FormatGG(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            raw = raw.Trim();

            int number;
            if (int.TryParse(raw, out number) && number >= 0 && number < 100) return number.ToString("00");
            return raw;   // 20.3 / -21+ 之类原样显示
        }

        private void ApplyRatingVisibility()
        {
            bool tuf = !_bossGgMode;

            if (_bossLetter != null) _bossLetter.enabled = tuf;
            if (_bossSpinRing != null) _bossSpinRing.enabled = false;
            if (_bossGgPrefix != null) _bossGgPrefix.gameObject.SetActive(!tuf);
            if (_bossGgRight != null) _bossGgRight.gameObject.SetActive(!tuf);

            if (_bossLetter != null) _bossLetter.enabled = true;
            if (_bossDigits[0] != null) _bossDigits[0].gameObject.SetActive(true);
            if (_bossGgPrefix != null) _bossGgPrefix.gameObject.SetActive(!tuf);
            if (_bossGgRight != null) _bossGgRight.gameObject.SetActive(!tuf);
            if (_bossDigits[1] != null) _bossDigits[1].gameObject.SetActive(_bossDigitsValue.Length > 1);
        }

        /// <summary>告诉 boss 动画用哪张评级图（例如 P19）。</summary>
        public void SetMark(string mark)
        {
            if (string.IsNullOrEmpty(mark)) mark = RatingMarks.Unranked;
            if (mark == _bossMark) return;
            _bossMark = mark;

            _bossLetterSprite = null;
            _bossDigitsValue = "";
            _bossKind = ResolveKind(mark);

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

        /// <summary>强制收起所有元素（退出转场时用，保证一个都不留）。</summary>
        /// <summary>调试用状态串。</summary>
        public string DebugState()
        {
            string s = "visible=" + _visible + " style=" + _style + " gg=" + _bossGgMode;
            s += " bossRoot=" + (_bossRoot != null ? (_bossRoot.gameObject.activeSelf ? "ON" : "off") : "null");
            if (_bossRoot != null) s += " grpAlpha=" + (_bossGroup != null ? _bossGroup.alpha.ToString("0.00") : "-");
            if (_bossGgPrefix != null) s += " ggL=" + (_bossGgPrefix.gameObject.activeSelf ? "ON" : "off") + "/" + _bossGgPrefix.color.a.ToString("0.00");
            if (_bossGgRight != null) s += " ggR=" + (_bossGgRight.gameObject.activeSelf ? "ON" : "off") + "/" + _bossGgRight.color.a.ToString("0.00");
            if (_bossDigits[0] != null) s += " d0=" + (_bossDigits[0].gameObject.activeSelf ? "ON" : "off");
            return s;
        }

        public void ForceHide()
        {
            _visible = false;

            if (_track != null) _track.gameObject.SetActive(false);
            if (_center != null) _center.gameObject.SetActive(false);

            // 直接销毁 boss 根节点，避免任何残留
            if (_bossRoot != null)
            {
                UnityEngine.Object.Destroy(_bossRoot.gameObject);
            }

            _bossRoot = null;
            _bossGroup = null;
            _bossBlack = null;
            _bossLetter = null;
            _bossGlitchA = null;
            _bossGlitchB = null;
            _bossFlash = null;
            _bossWave = null;
            _bossSpinRing = null;
            _bossGgPrefix = null;
            _bossGgRight = null;
            _bossGlitchTextA = null;
            _bossGlitchTextB = null;
            _bossGlow = null;
            _bossTrailA = null;
            _bossTrailB = null;
            _reelRoot = null;
            for (int i = 0; i < 5; i++) { _reelText[i] = null; _reelCol[i] = null; _reelOffset[i] = 0f; }
            _bossDigits[0] = null;
            _bossDigits[1] = null;
            _bossMark = null;
            _bossLetterSprite = null;
            _bossGgMode = false;
            _bossGgValue = "";
            _bossTime = 0f;
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (!visible)
            {
                if (_bossRoot != null) _bossRoot.gameObject.SetActive(false);
                if (_track != null) _track.gameObject.SetActive(false);
                if (_center != null) _center.gameObject.SetActive(false);
                return;
            }

            if (_style == StyleBoss) EnsureBoss();

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
            if (!_visible) return;

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
        // boss 动画：按评级分成 10 种不同的演出
        // ------------------------------------------------------------------
        private const float BossCircleY = 40f;
        private const float BossCircleSize = 420f;

        private enum BossKind
        {
            Simple,        // P1-P9
            Classic,       // P10-P20
            SideIn,        // G1-G9
            DropBounce,    // G10-G20
            Dash,          // U1-U9
            Arc,           // U10-U14
            HeavyGlitch,   // U15-U16
            DigitsFirst,   // U17-U19
            Ultimate,      // U20
            Spiral         // UJ
        }

        private enum EaseKind { Cubic, Back, Bounce, Linear }

        private struct BossPlan
        {
            public float Duration;
            public float CircleStart;
            public float CircleDur;
            public float CircleFromScale;
            public float CircleFromRot;
            public EaseKind CircleEase;
            public float DigitStart;
            public float DigitGap;
            public float DigitDur;
            public EaseKind DigitEase;
            public int DigitDir;        // 0 下方 1 上方 2 左右 3 四角 4 螺旋
            public float Glitch;        // 0..1
            public int GlitchCount;     // 故障触发次数（不循环）
            public bool DigitsFirst;
            public bool Flash;
            public bool Wave;
            public bool SpinRing;
        }

        private static BossKind ResolveKind(string mark)
        {
            if (string.IsNullOrEmpty(mark)) return BossKind.Classic;

            char letter = char.ToUpperInvariant(mark[0]);
            string digits = DigitsOf(mark);
            int value = 0;
            int.TryParse(digits, out value);
            bool hasJ = mark.Length > 0 && char.ToUpperInvariant(mark[mark.Length - 1]) == 'J';

            if (letter == 'P') return value <= 9 ? BossKind.Simple : BossKind.Classic;
            if (letter == 'G') return value <= 9 ? BossKind.SideIn : BossKind.DropBounce;
            if (letter == 'U')
            {
                if (hasJ) return BossKind.Spiral;
                if (value <= 9) return BossKind.Dash;
                if (value <= 14) return BossKind.Arc;
                if (value <= 16) return BossKind.HeavyGlitch;
                if (value <= 19) return BossKind.DigitsFirst;
                return BossKind.Ultimate;
            }
            return BossKind.Classic;
        }

        private static BossPlan PlanOf(BossKind kind)
        {
            BossPlan p = new BossPlan();
            p.CircleStart = 0.22f;
            p.CircleDur = 0.55f;
            p.CircleFromScale = 0.32f;
            p.CircleFromRot = 0f;
            p.CircleEase = EaseKind.Cubic;
            p.DigitStart = 0.80f;
            p.DigitGap = 0.26f;
            p.DigitDur = 0.95f;
            p.DigitEase = EaseKind.Cubic;
            p.DigitDir = 0;
            p.Glitch = 0.35f;
            p.GlitchCount = 2;
            p.DigitsFirst = false;
            p.Flash = false;
            p.Wave = false;
            p.SpinRing = false;

            switch (kind)
            {
                case BossKind.Simple:
                    p.Duration = 3.4f;
                    p.DigitStart = 0.55f;
                    p.DigitDur = 1.05f;
                    p.Glitch = 0f;
                    p.GlitchCount = 0;
                    break;

                case BossKind.Classic:
                    p.Duration = 4.6f;
                    break;

                case BossKind.SideIn:
                    p.Duration = 3.9f;
                    p.CircleFromRot = 160f;
                    p.CircleEase = EaseKind.Back;
                    p.DigitStart = 0.62f;
                    p.DigitDur = 0.9f;
                    p.DigitEase = EaseKind.Back;
                    p.DigitDir = 2;
                    p.Glitch = 0.18f;
                    p.GlitchCount = 1;
                    break;

                case BossKind.DropBounce:
                    p.Duration = 4.4f;
                    p.CircleFromScale = 0.15f;
                    p.CircleEase = EaseKind.Back;
                    p.DigitStart = 0.70f;
                    p.DigitDur = 1.1f;
                    p.DigitEase = EaseKind.Bounce;
                    p.DigitDir = 1;
                    p.Glitch = 0.25f;
                    p.GlitchCount = 1;
                    p.Flash = true;
                    break;

                case BossKind.Dash:
                    p.Duration = 3.6f;
                    p.CircleFromScale = 0.55f;
                    p.CircleDur = 0.35f;
                    p.DigitStart = 0.50f;
                    p.DigitDur = 0.55f;
                    p.DigitEase = EaseKind.Linear;
                    p.Glitch = 0.45f;
                    p.GlitchCount = 2;
                    break;

                case BossKind.Arc:
                    p.Duration = 4.2f;
                    p.CircleFromScale = 1.35f;
                    p.CircleEase = EaseKind.Cubic;
                    p.DigitStart = 0.62f;
                    p.DigitGap = 0.20f;
                    p.DigitDur = 1.0f;
                    p.DigitDir = 3;
                    p.Glitch = 0.2f;
                    p.GlitchCount = 1;
                    break;

                case BossKind.HeavyGlitch:
                    p.Duration = 4.8f;
                    p.DigitStart = 0.75f;
                    p.DigitDur = 1.35f;
                    p.Glitch = 0.95f;
                    p.GlitchCount = 3;
                    break;

                case BossKind.DigitsFirst:
                    p.Duration = 4.4f;
                    p.DigitsFirst = true;
                    p.CircleStart = 1.15f;
                    p.CircleFromScale = 2.1f;
                    p.CircleEase = EaseKind.Back;
                    p.DigitStart = 0.30f;
                    p.DigitDur = 1.1f;
                    p.Glitch = 0.3f;
                    p.GlitchCount = 2;
                    break;

                case BossKind.Ultimate:
                    p.Duration = 5.2f;
                    p.CircleStart = 0.45f;
                    p.CircleDur = 0.5f;
                    p.CircleFromScale = 6f;
                    p.CircleEase = EaseKind.Back;
                    p.DigitStart = 0.85f;
                    p.DigitGap = 0.22f;
                    p.DigitDur = 0.95f;
                    p.DigitEase = EaseKind.Bounce;
                    p.DigitDir = 1;
                    p.Glitch = 0.85f;
                    p.GlitchCount = 3;
                    p.Flash = true;
                    p.Wave = true;
                    break;

                case BossKind.Spiral:
                    p.Duration = 4.6f;
                    p.CircleStart = 0.2f;
                    p.CircleDur = 0.6f;
                    p.CircleFromScale = 0.1f;
                    p.CircleFromRot = -220f;
                    p.CircleEase = EaseKind.Back;
                    p.DigitStart = 0.70f;
                    p.DigitGap = 0.24f;
                    p.DigitDur = 1.2f;
                    p.DigitDir = 4;
                    p.Glitch = 0.3f;
                    p.GlitchCount = 2;
                    p.SpinRing = true;
                    break;
            }

            return p;
        }

        private static float Ease(EaseKind kind, float t)
        {
            t = Mathf.Clamp01(t);
            switch (kind)
            {
                case EaseKind.Back:
                {
                    float c = 1.70158f;
                    float u = t - 1f;
                    return 1f + (c + 1f) * u * u * u + c * u * u;
                }
                case EaseKind.Bounce:
                    return BounceOut(t);
                case EaseKind.Linear:
                    return t;
                default:
                    return 1f - Mathf.Pow(1f - t, 3f);
            }
        }

        private static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        /// <summary>当前 boss 演出在给定设置下的总时长（秒）。</summary>
        public float BossDuration(Settings cfg)
        {
            float speed = cfg != null ? Mathf.Clamp(cfg.BossSpeed, 0.4f, 2f) : 1f;
            float duration = _bossGgMode ? 10.4f : PlanOf(_bossKind).Duration;
            return duration / speed;
        }

        // ------------------------------------------------------------------
        // boss · GG：一排 <Lv.XX>
        //   一开始 <Lv.> 是合在一起的，数字从下方升起的同时括号张开
        // ------------------------------------------------------------------
        /// <summary>把 boss 演出的所有元素收起来（只保留黑底）。</summary>
        private void HideBossContent()
        {
            if (_bossBlack != null)
            {
                Color c = Color.black;
                c.a = 1f;
                _bossBlack.color = c;
            }

            if (_bossLetter != null) _bossLetter.enabled = false;
            if (_bossGlitchA != null) _bossGlitchA.enabled = false;
            if (_bossGlitchB != null) _bossGlitchB.enabled = false;
            if (_bossWave != null) _bossWave.enabled = false;
            if (_bossSpinRing != null) _bossSpinRing.enabled = false;

            for (int i = 0; i < 2; i++)
            {
                if (_bossDigits[i] != null && _bossDigits[i].gameObject.activeSelf) _bossDigits[i].gameObject.SetActive(false);
            }
            if (_bossGgPrefix != null && _bossGgPrefix.gameObject.activeSelf) _bossGgPrefix.gameObject.SetActive(false);
            if (_bossGgRight != null && _bossGgRight.gameObject.activeSelf) _bossGgRight.gameObject.SetActive(false);
            if (_reelRoot != null && _reelRoot.gameObject.activeSelf) _reelRoot.gameObject.SetActive(false);
            for (int i = 0; i < 5; i++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (_reelCell[i, k] != null && _reelCell[i, k].gameObject.activeSelf) _reelCell[i, k].gameObject.SetActive(false);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (_glitchImgs[i] != null) { Color gc = _glitchImgs[i].color; gc.a = 0f; _glitchImgs[i].color = gc; }
            }

            if (_bossGlow != null && _bossGlow.gameObject.activeSelf) _bossGlow.gameObject.SetActive(false);
            if (_bossTrailA != null && _bossTrailA.gameObject.activeSelf) _bossTrailA.gameObject.SetActive(false);
            if (_bossTrailB != null && _bossTrailB.gameObject.activeSelf) _bossTrailB.gameObject.SetActive(false);
            if (_bossGlitchTextA != null && _bossGlitchTextA.gameObject.activeSelf) _bossGlitchTextA.gameObject.SetActive(false);
            if (_bossGlitchTextB != null && _bossGlitchTextB.gameObject.activeSelf) _bossGlitchTextB.gameObject.SetActive(false);

            if (_bossFlash != null)
            {
                Color fc = _bossFlash.color;
                fc.a = 0f;
                _bossFlash.color = fc;
            }
        }

        private void TickBossGG(float dt)
        {
            _bossTime += dt;

            Settings cfg = Main.Config;
            float speed = cfg != null ? Mathf.Clamp(cfg.BossSpeed, 0.4f, 2f) : 1f;
            float sizeMul = cfg != null ? Mathf.Clamp(cfg.BossScale, 0.5f, 1.8f) : 1f;
            float glitchMul = cfg != null ? Mathf.Clamp(cfg.BossGlitch, 0f, 2f) : 1f;
            float numScale = cfg != null ? Mathf.Clamp(cfg.BossGgNumberSize, 0.3f, 6f) : 4f;
            float labelScale = cfg != null ? Mathf.Clamp(cfg.BossGgLabelSize, 0.1f, 3f) : 0.25f;

            float t = _bossTime * speed;
            const float total = 10.4f;

            if (t >= total)
            {
                HideBossContent();
                return;
            }

            float H = _bossRoot != null ? _bossRoot.rect.height : 1080f;
            float W = _bossRoot != null ? _bossRoot.rect.width : 1920f;
            if (H <= 1f) H = 1080f;
            if (W <= 1f) W = 1920f;

            if (_bossBlack != null)
            {
                Color c = Color.black;
                c.a = Mathf.Clamp01(t / 0.22f);
                _bossBlack.color = c;
            }

            if (_bossLetter != null && _bossLetter.enabled) _bossLetter.enabled = false;
            if (_bossSpinRing != null && _bossSpinRing.enabled) _bossSpinRing.enabled = false;
            if (_bossGlitchA != null) _bossGlitchA.enabled = false;
            if (_bossGlitchB != null) _bossGlitchB.enabled = false;
            if (_bossWave != null) _bossWave.enabled = false;

            if (_bossGgPrefix == null || _bossGgRight == null || _bossDigits[0] == null) return;

            int labelFont = Mathf.Max(12, Mathf.RoundToInt(250f * sizeMul * labelScale));
            int numberFont = Mathf.Max(24, Mathf.RoundToInt(250f * sizeMul * numScale));

            // ---------------- 时间轴 ----------------
            const float RiseStart = 0.22f;
            const float RiseEnd = 0.72f;
            const float HoldEnd = 1.60f;
            const float MorphEnd = 2.00f;      // 数字 → 卷轴 的过渡
            const float JoinStart = 6.40f;     // 其它三位进入
            const float GrowStart = 7.40f;     // 数字开始铺满全屏
            const float BracketFade = 9.00f;    // <> 开始淡出
            const float RollEnd = 9.10f;
            const float GlitchStart = 9.10f;
            const float CrtStart = 9.35f;
            const float CrtLineEnd = 9.65f;
            const float CrtDotEnd = 9.95f;

            bool rolling = t >= MorphEnd && t < RollEnd;
            bool crt = t >= CrtStart;
            bool displayed = t < MorphEnd;      // 数字展示阶段（含上升 + 保持 + 过渡）

            // ---------------- 数字上升 ----------------
            float rise = Mathf.Clamp01((t - RiseStart) / (RiseEnd - RiseStart));
            float riseEase = Ease(EaseKind.Cubic, rise);

            float morph = Mathf.Clamp01((t - HoldEnd) / (MorphEnd - HoldEnd));
            float morphEase = Ease(EaseKind.Cubic, morph);

            Text tens = _bossDigits[0];
            Text ones = _bossDigits[1];
            bool split = _bossGgValue.Length == 2;

            tens.fontSize = numberFont;
            tens.fontStyle = FontStyle.Bold;
            tens.text = split ? _bossGgValue[0].ToString() : _bossGgValue;

            if (ones != null)
            {
                ones.fontSize = numberFont;
                ones.fontStyle = FontStyle.Bold;
                ones.text = split ? _bossGgValue[1].ToString() : "";
            }

            float tensW = tens.preferredWidth;
            float onesW = split && ones != null ? ones.preferredWidth : 0f;
            float digitsW = tensW + onesW;

            _bossGgPrefix.fontSize = labelFont;
            _bossGgRight.fontSize = labelFont;
            _bossGgPrefix.fontStyle = FontStyle.Bold;
            _bossGgRight.fontStyle = FontStyle.Bold;
            _bossGgPrefix.text = "<Lv.";
            _bossGgRight.text = ">";

            float labelW = _bossGgPrefix.preferredWidth;
            float rightW = _bossGgRight.preferredWidth;
            float gap = labelFont * 0.30f;
            float totalW = labelW + gap * 2f + digitsW + rightW;
            float rowLeft = -totalW * 0.5f;
            float lift = numberFont * 0.07f;
            float labelY = lift - (numberFont - labelFont) * 0.16f;

            // 升起时的纵向位置
            float digY = Mathf.Lerp(-H * 0.34f, lift, riseEase);

            bool showNumber = displayed && rise > 0.02f && morph < 0.98f;
            tens.gameObject.SetActive(showNumber && !split);
            tens.gameObject.SetActive(showNumber);
            if (ones != null) ones.gameObject.SetActive(showNumber && split);

            float numberAlpha = (1f - morphEase) * Mathf.Clamp01(rise * 3f);
            float shrink = Mathf.Lerp(1f, 0.88f, morphEase);

            tens.rectTransform.localScale = new Vector3(shrink, shrink, 1f);
            tens.rectTransform.anchoredPosition = new Vector2(rowLeft + labelW + gap + tensW * 0.5f, digY);
            tens.color = new Color(1f, 1f, 1f, numberAlpha);

            if (split && ones != null)
            {
                ones.rectTransform.localScale = new Vector3(shrink, shrink, 1f);
                ones.rectTransform.anchoredPosition = new Vector2(rowLeft + labelW + gap + tensW + onesW * 0.5f, digY);
                ones.color = new Color(1f, 1f, 1f, numberAlpha);
            }

            float bracketFade = Mathf.Clamp01((t - BracketFade) / 0.35f);
            bool showLabels = rise > 0.02f && t < MorphEnd || (t >= MorphEnd && bracketFade < 1f && rise > 0.02f);
            _bossGgPrefix.gameObject.SetActive(showLabels);
            _bossGgRight.gameObject.SetActive(showLabels);

            float labelAlpha = (1f - bracketFade) * Mathf.Clamp01(rise * 3f);
            _bossGgPrefix.rectTransform.anchoredPosition = new Vector2(rowLeft + labelW * 0.5f, labelY);
            _bossGgRight.rectTransform.anchoredPosition = new Vector2(rowLeft + labelW + gap * 2f + digitsW + rightW * 0.5f, labelY);
            _bossGgPrefix.color = new Color(1f, 1f, 1f, labelAlpha * 0.85f);
            _bossGgRight.color = new Color(1f, 1f, 1f, labelAlpha * 0.85f);

            // 辉光
            if (_bossGlow != null)
            {
                bool glowOn = showNumber;
                _bossGlow.gameObject.SetActive(glowOn);
                if (glowOn)
                {
                    _bossGlow.fontStyle = FontStyle.Bold;
                    _bossGlow.text = tens.text;
                    _bossGlow.fontSize = Mathf.RoundToInt(numberFont * 1.06f);
                    _bossGlow.rectTransform.anchoredPosition = tens.rectTransform.anchoredPosition;
                    _bossGlow.rectTransform.localScale = tens.rectTransform.localScale;
                    _bossGlow.color = new Color(1f, 1f, 1f, 0.22f * numberAlpha);
                }
            }

            if (_bossTrailA != null) _bossTrailA.gameObject.SetActive(false);
            if (_bossTrailB != null) _bossTrailB.gameObject.SetActive(false);

            // ---------------- 卷轴 ----------------
            if (_reelRoot != null)
            {
                bool reelVisible = t >= HoldEnd && !crt;
                _reelRoot.gameObject.SetActive(reelVisible);

                if (reelVisible)
                {
                    bool twoDigits = _bossGgValue.Length >= 2;
                    int slotA = 2;
                    int slotB = twoDigits ? 3 : 2;
                    const float spacing = 360f;

                    if (!_reelSeeded)
                    {
                        _reelSeeded = true;
                        int dLast = _bossGgValue.Length > 0 ? (_bossGgValue[_bossGgValue.Length - 1] - '0') : 0;
                        int dPrev = _bossGgValue.Length > 1 ? (_bossGgValue[_bossGgValue.Length - 2] - '0') : 0;
                        _reelOffset[slotB] = (dLast + 2) * spacing;
                        _reelOffset[slotA] = ((twoDigits ? dPrev : 0) + 2) * spacing;
                        for (int k = 0; k < 5; k++)
                        {
                            if (k != slotA && k != slotB) _reelOffset[k] = spacing * UnityEngine.Random.Range(0, 10);
                        }
                    }

                    float pitch = 385f;
                    float rollProgress = Mathf.Clamp01((t - MorphEnd) / (RollEnd - MorphEnd));

                    float inT = Mathf.Clamp01((t - HoldEnd) / (MorphEnd - HoldEnd));
                    float inEase = Ease(EaseKind.Cubic, inT);

                    float join = Mathf.Clamp01((t - JoinStart) / 1.00f);
                    float joinEase = Ease(EaseKind.Cubic, join);

                    float centerOffset = twoDigits ? -0.5f : 0f;
                    float recenter = Mathf.Clamp01((t - 4.60f) / 3.00f);
                    // smoothstep 两边都平缓，看起来像"慢慢滑过去"
                    float recenterEase = recenter * recenter * (3f - 2f * recenter);
                    float rootX = Mathf.Lerp(centerOffset * pitch, 0f, recenterEase);
                    _reelRoot.anchoredPosition = new Vector2(rootX, 0f);

                    float growT = Mathf.Clamp01((t - GrowStart) / 2.2f);
                    float growEase = Ease(EaseKind.Cubic, growT);
                    float reelScale = Mathf.Lerp(Mathf.Lerp(0.9f, 1f, inEase), 1.15f, growEase);
                    _reelRoot.localScale = new Vector3(reelScale, reelScale, 1f);

                    float scrollSpeed = Mathf.Lerp(25f, 24000f, Mathf.Pow(rollProgress, 3.2f));

                    for (int i = 0; i < 5; i++)
                    {
                        RectTransform col = _reelCol[i];
                        if (col == null) continue;

                        bool ratingSlot = i == slotA || i == slotB;
                        bool present = ratingSlot || (join > 0.01f);

                        if (col.gameObject.activeSelf != present) col.gameObject.SetActive(present);
                        if (!present) continue;

                        // 遮罩从宽到窄：开始时能看到上下几个数字，后面收成一行
                        float winT = Mathf.Clamp01((t - 4.60f) / 3.00f);
                        float winH = Mathf.Lerp(380f, 780f, Ease(EaseKind.Cubic, winT));
                        Vector2 sz = col.sizeDelta;
                        if (Mathf.Abs(sz.y - winH) > 0.5f) col.sizeDelta = new Vector2(sz.x, winH);

                        float speedFactor = ratingSlot ? Mathf.Lerp(0f, 1f, inEase) : 1f;
                        _reelOffset[i] += scrollSpeed * dt * speedFactor * (1f + i * 0.05f);

                        float pos = _reelOffset[i] / spacing;
                        int baseIndex = Mathf.FloorToInt(pos);
                        float frac = pos - baseIndex;

                        float slide = ratingSlot ? 0f : -(1f - joinEase) * 1100f;
                        float alpha = ratingSlot ? inEase : joinEase;
                        float blur = Mathf.Clamp01((scrollSpeed - 3000f) / 12000f);

                        for (int k = 0; k < 5; k++)
                        {
                            Text cell = _reelCell[i, k];
                            if (cell == null) continue;

                            if (!cell.gameObject.activeSelf) cell.gameObject.SetActive(true);

                            int digit = ((baseIndex - k) % 10 + 10) % 10;
                            cell.text = digit.ToString();
                            cell.fontSize = 380;
                            cell.fontStyle = FontStyle.Bold;

                            float y = (k - 2f + frac) * spacing;
                            cell.rectTransform.anchoredPosition = new Vector2(slide, y);
                            cell.color = new Color(1f, 1f, 1f, alpha * (1f - blur * 0.25f) + blur * 0.25f);
                        }
                    }
                }
            }

            // ---------------- 故障 ----------------
            // 加速末段就开始偶发，正式故障段高强度闪烁，并延续到电视关机前
            bool glitchPhase = t >= (GlitchStart - 1.60f) && t < CrtStart;

            if (glitchPhase)
            {
                float intensity = t >= GlitchStart ? 1f : Mathf.InverseLerp(GlitchStart - 1.60f, GlitchStart, t) * 0.6f;

                for (int i = 0; i < 4; i++)
                {
                    Image img = _glitchImgs[i];
                    RectTransform bar = _glitchBars[i];
                    if (img == null || bar == null) continue;

                    bool on = UnityEngine.Random.value < (t >= GlitchStart ? 0.85f : 0.22f) * Mathf.Clamp01(glitchMul);

                    if (on)
                    {
                        float height = UnityEngine.Random.Range(8f, 26f + 120f * intensity);
                        bar.sizeDelta = new Vector2(0f, height);
                        bar.anchoredPosition = new Vector2(UnityEngine.Random.Range(-26f, 26f), UnityEngine.Random.Range(-H * 0.45f, H * 0.45f));

                        float pick = UnityEngine.Random.value;
                        Color c;
                        if (pick < 0.34f) c = new Color(1f, 0.25f, 0.35f, 0.55f);
                        else if (pick < 0.68f) c = new Color(0.25f, 0.75f, 1f, 0.55f);
                        else c = new Color(1f, 1f, 1f, 0.35f);

                        c.a *= Mathf.Clamp01(0.4f + glitchMul * 0.5f);
                        img.color = c;
                    }
                    else
                    {
                        Color c = img.color;
                        c.a = 0f;
                        img.color = c;
                    }
                }

                // 数字横向抖动
                if (_reelRoot != null)
                {
                    float amp = (t >= GlitchStart ? 26f : 8f) * Mathf.Clamp01(glitchMul);
                    _reelRoot.anchoredPosition += new Vector2(UnityEngine.Random.Range(-amp, amp), 0f);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (_glitchImgs[i] != null)
                    {
                        Color c = _glitchImgs[i].color;
                        c.a = 0f;
                        _glitchImgs[i].color = c;
                    }
                }
            }

            if (_bossGlitchTextA != null && _bossGlitchTextA.gameObject.activeSelf) _bossGlitchTextA.gameObject.SetActive(false);
            if (_bossGlitchTextB != null && _bossGlitchTextB.gameObject.activeSelf) _bossGlitchTextB.gameObject.SetActive(false);

            // ---------------- 电视关机 ----------------
            if (crt)
            {
                float lineT = Mathf.Clamp01((t - CrtStart) / (CrtLineEnd - CrtStart));
                float dotT = Mathf.Clamp01((t - CrtLineEnd) / (CrtDotEnd - CrtLineEnd));

                float sx = 1f;
                float sy = 1f;

                if (t < CrtLineEnd) sy = Mathf.Lerp(1f, 0.008f, lineT);
                else { sy = 0.008f; sx = Mathf.Lerp(1f, 0.004f, dotT); }

                if (_bossRoot != null) _bossRoot.localScale = new Vector3(sx, sy, 1f);

                // 收成点之后立刻清空，避免中心留一个像素点
                if (dotT >= 0.98f) HideBossContent();
            }
            else if (_bossRoot != null)
            {
                _bossRoot.localScale = Vector3.one;
            }

            if (_bossGroup != null) _bossGroup.alpha = Mathf.Clamp01(t / 0.2f);
        }

        private void TickBoss(float dt)
        {
            if (_bossGgMode)
            {
                TickBossGG(dt);
                return;
            }

            _bossTime += dt;

            Settings cfg = Main.Config;
            float speed = cfg != null ? Mathf.Clamp(cfg.BossSpeed, 0.4f, 2f) : 1f;
            float sizeMul = cfg != null ? Mathf.Clamp(cfg.BossScale, 0.5f, 1.8f) : 1f;
            float glitchMul = cfg != null ? Mathf.Clamp(cfg.BossGlitch, 0f, 2f) : 1f;
            float digitMul = cfg != null ? Mathf.Clamp(cfg.BossDigitScale, 0.6f, 1.6f) : 1f;
            float circleSize = BossCircleSize * sizeMul;

            float t = _bossTime * speed;
            float H = _bossRoot != null ? _bossRoot.rect.height : 1080f;
            float W = _bossRoot != null ? _bossRoot.rect.width : 1920f;
            if (H <= 1f) H = 1080f;
            if (W <= 1f) W = 1920f;

            BossPlan plan = PlanOf(_bossKind);
            float total = plan.Duration;

            // 演出结束：把内容全部收掉，只留黑底（避免淡出后残留）
            if (t >= total + 0.02f)
            {
                HideBossContent();
                return;
            }

            float fadeOutStart = total - 0.55f;
            float fade = t > fadeOutStart ? Mathf.Clamp01(1f - (t - fadeOutStart) / 0.55f) : 1f;

            // 1) 黑屏
            if (_bossBlack != null)
            {
                float black = Mathf.Clamp01(t / 0.22f);
                if (_bossKind == BossKind.Ultimate)
                {
                    // U20：开头闪两下
                    black = Mathf.Clamp01(t / 0.12f);
                    if (t > 0.16f && t < 0.22f) black = 0.35f;
                    if (t > 0.28f && t < 0.32f) black = 0.6f;
                }
                Color c = Color.black;
                c.a = black;
                _bossBlack.color = c;
            }

            // 2) 圆圈
            float circleT = Mathf.Clamp01((t - plan.CircleStart) / Mathf.Max(0.05f, plan.CircleDur));
            float circleEase = Ease(plan.CircleEase, circleT);
            float circleScale = Mathf.Lerp(plan.CircleFromScale, 1f, circleEase);
            float circleAlpha = Mathf.Clamp01(circleT * 2f);

            if (plan.DigitsFirst)
            {
                // 数字先出现，圆圈后飞过来把数字框住
                circleAlpha = Mathf.Clamp01(circleT * 2.5f);
            }

            float circleRot = Mathf.Lerp(plan.CircleFromRot, 0f, circleEase);

            // 故障：只在固定的几个瞬间触发，不循环
            plan.Glitch = Mathf.Clamp01(plan.Glitch * glitchMul);

            int digitCount0 = Mathf.Min(2, _bossDigitsValue.Length);
            float landTime = plan.DigitStart + Mathf.Max(0, digitCount0 - 1) * plan.DigitGap + plan.DigitDur;
            int glitchTimes = Mathf.Clamp(Mathf.RoundToInt(plan.GlitchCount * Mathf.Clamp(glitchMul, 0f, 2f)), 0, 3);
            float burstLength = 0.10f + 0.05f * plan.Glitch;

            bool burst = false;
            for (int gi = 0; gi < glitchTimes; gi++)
            {
                float bt = landTime + 0.02f + gi * (0.55f + 0.15f * plan.Glitch);
                if (t >= bt && t < bt + burstLength)
                {
                    burst = true;
                    break;
                }
            }

            Vector2 circlePos = new Vector2(0f, BossCircleY);
            if (burst) circlePos += new Vector2(UnityEngine.Random.Range(-8f, 8f) * plan.Glitch, UnityEngine.Random.Range(-4f, 4f) * plan.Glitch);

            if (_bossLetter != null)
            {
                _bossLetter.rectTransform.anchoredPosition = circlePos;
                _bossLetter.rectTransform.sizeDelta = new Vector2(circleSize, circleSize);
                _bossLetter.rectTransform.localRotation = Quaternion.Euler(0f, 0f, circleRot);
                _bossLetter.rectTransform.localScale = new Vector3(circleScale, circleScale, 1f);

                Color c = Color.white;
                c.a = circleAlpha * fade;
                _bossLetter.color = c;
            }

            // 字母残影（故障）
            if (_bossGlitchA != null && _bossGlitchB != null)
            {
                float ghostAlpha = 0.18f + 0.45f * plan.Glitch;
                if (burst && _bossLetterSprite != null)
                {
                    float off = UnityEngine.Random.Range(6f, 10f + 16f * plan.Glitch);
                    _bossGlitchA.enabled = true;
                    _bossGlitchB.enabled = true;
                    _bossGlitchA.rectTransform.anchoredPosition = circlePos + new Vector2(-off, 0f);
                    _bossGlitchB.rectTransform.anchoredPosition = circlePos + new Vector2(off, 0f);
                    _bossGlitchA.rectTransform.sizeDelta = new Vector2(circleSize, circleSize);
                    _bossGlitchB.rectTransform.sizeDelta = new Vector2(circleSize, circleSize);
                    _bossGlitchA.rectTransform.localScale = new Vector3(circleScale, circleScale, 1f);
                    _bossGlitchB.rectTransform.localScale = new Vector3(circleScale, circleScale, 1f);
                    _bossGlitchA.color = new Color(1f, 0.25f, 0.35f, ghostAlpha * fade);
                    _bossGlitchB.color = new Color(0.25f, 0.75f, 1f, ghostAlpha * fade);
                }
                else
                {
                    _bossGlitchA.enabled = false;
                    _bossGlitchB.enabled = false;
                }
            }

            // UJ：外圈旋转环
            if (_bossSpinRing != null)
            {
                bool on = plan.SpinRing;
                _bossSpinRing.enabled = on;
                if (on)
                {
                    _bossSpinRing.rectTransform.anchoredPosition = circlePos;
                    _bossSpinRing.rectTransform.sizeDelta = new Vector2(circleSize * 1.12f, circleSize * 1.12f);
                    _bossSpinRing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -t * 220f);
                    _bossSpinRing.rectTransform.localScale = new Vector3(circleScale, circleScale, 1f);
                    float hue = Mathf.Repeat(t * 0.35f, 1f);
                    Color ring = Color.HSVToRGB(hue, 0.65f, 1f);
                    ring.a = 0.85f * circleAlpha * fade;
                    _bossSpinRing.color = ring;
                }
            }

            // 3) 数字
            int count = Mathf.Min(2, _bossDigitsValue.Length);
            float finalFont = circleSize * 0.40f * digitMul;
            float startFont = finalFont * 1.9f;
            float gap = finalFont * 0.62f;
            float targetY = BossCircleY - circleSize * 0.13f;

            for (int i = 0; i < 2; i++)
            {
                Text digit = _bossDigits[i];
                if (digit == null) continue;

                if (i >= count)
                {
                    if (digit.gameObject.activeSelf) digit.gameObject.SetActive(false);
                    continue;
                }
                if (!digit.gameObject.activeSelf) digit.gameObject.SetActive(true);

                float start = plan.DigitStart + i * plan.DigitGap;
                float p = Mathf.Clamp01((t - start) / Mathf.Max(0.05f, plan.DigitDur));
                float ease = Ease(plan.DigitEase, p);

                float targetX = (i - (count - 1) * 0.5f) * gap;
                Vector2 from;
                Vector2 to = new Vector2(targetX, targetY);

                switch (plan.DigitDir)
                {
                    case 1: from = new Vector2(targetX, H * 0.66f); break;
                    case 2: from = new Vector2((i % 2 == 0 ? -1f : 1f) * W * 0.55f, targetY); break;
                    case 3: from = new Vector2((i % 2 == 0 ? -1f : 1f) * W * 0.45f, -H * 0.45f); break;
                    case 4:
                    {
                        float angle = Mathf.Lerp(200f, 0f, ease) * Mathf.Deg2Rad;
                        float radius = Mathf.Lerp(520f, 0f, ease);
                        from = new Vector2(targetX + Mathf.Cos(angle) * radius, targetY + Mathf.Sin(angle) * radius);
                        break;
                    }
                    default: from = new Vector2(targetX, -H * 0.62f); break;
                }

                Vector2 pos = new Vector2(Mathf.Lerp(from.x, to.x, ease), Mathf.Lerp(from.y, to.y, ease));
                if (burst) pos += new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f));

                digit.rectTransform.anchoredPosition = pos;
                digit.fontSize = Mathf.Max(8, Mathf.RoundToInt(Mathf.Lerp(startFont, finalFont, ease)));

                Color c = Color.white;
                c.a = Mathf.Clamp01(p * 4f) * fade;
                digit.color = c;
            }

            // 数字残影
            if (_bossGlitchTextA != null && _bossGlitchTextB != null)
            {
                if (burst && count > 0 && plan.Glitch > 0.1f)
                {
                    _bossGlitchTextA.text = _bossDigitsValue;
                    _bossGlitchTextB.text = _bossDigitsValue;
                    _bossGlitchTextA.gameObject.SetActive(true);
                    _bossGlitchTextB.gameObject.SetActive(true);

                    float off = UnityEngine.Random.Range(4f, 6f + 12f * plan.Glitch);
                    Vector2 basePos = new Vector2(count > 1 ? -gap * 0.5f : 0f, targetY);
                    _bossGlitchTextA.rectTransform.anchoredPosition = basePos + new Vector2(-off, 0f);
                    _bossGlitchTextB.rectTransform.anchoredPosition = basePos + new Vector2(off, 0f);
                    _bossGlitchTextA.fontSize = Mathf.RoundToInt(finalFont);
                    _bossGlitchTextB.fontSize = Mathf.RoundToInt(finalFont);
                    float ghost = 0.12f + 0.3f * plan.Glitch;
                    _bossGlitchTextA.color = new Color(1f, 0.25f, 0.35f, ghost * fade);
                    _bossGlitchTextB.color = new Color(0.25f, 0.75f, 1f, ghost * fade);
                }
                else
                {
                    _bossGlitchTextA.gameObject.SetActive(false);
                    _bossGlitchTextB.gameObject.SetActive(false);
                }
            }

            // 4) 白闪
            if (_bossFlash != null)
            {
                float flash = 0f;
                if (plan.Flash)
                {
                    if (t > plan.CircleStart + plan.CircleDur * 0.8f && t < plan.CircleStart + plan.CircleDur * 0.8f + 0.18f) flash = 0.7f;
                    if (_bossKind == BossKind.Ultimate)
                    {
                        if (t > plan.DigitStart + plan.DigitDur && t < plan.DigitStart + plan.DigitDur + 0.22f) flash = 0.9f;
                    }
                }
                Color c = _bossFlash.color;
                c.a = flash * fade;
                _bossFlash.color = c;
            }

            // 5) 冲击波
            if (_bossWave != null)
            {
                if (plan.Wave && t > plan.DigitStart + plan.DigitDur * 0.9f)
                {
                    float w = Mathf.Clamp01((t - (plan.DigitStart + plan.DigitDur * 0.9f)) / 1.1f);
                    _bossWave.enabled = true;
                    float size = Mathf.Lerp(360f, 1500f, w);
                    _bossWave.rectTransform.sizeDelta = new Vector2(size, size);
                    Color c = Color.white;
                    c.a = (1f - w) * 0.55f * fade;
                    _bossWave.color = c;
                }
                else
                {
                    _bossWave.enabled = false;
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

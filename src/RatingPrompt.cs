using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhigrosLoading
{
    /// <summary>
    /// 打完关卡退出时的「评级」弹窗：从 LevelLibrary 的 marks 里挑一个评级。
    /// </summary>
    public class RatingPrompt : MonoBehaviour
    {
        private static RatingPrompt _instance;

        private const float PanelWidth = 1560f;
        private const float PanelHeight = 780f;
        private const float CellSize = 108f;
        private const float CellGap = 10f;
        private const int Columns = 12;

        private static readonly Color PanelColor = new Color(0.07f, 0.075f, 0.10f, 0.97f);
        private static readonly Color CellColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color Accent = new Color(1f, 0.42f, 0.50f, 1f);

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _panel;
        private Text _title;
        private Text _subtitle;
        private Image _currentIcon;
        private Text _currentLabel;
        private RectTransform _content;
        private ScrollRect _scroll;

        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<Text> _tabTexts = new List<Text>();
        private readonly List<GameObject> _cells = new List<GameObject>();
        private readonly Dictionary<string, Image> _cellBackgrounds = new Dictionary<string, Image>();

        private GameObject _ownEventSystem;
        private string _levelPath;
        private string _currentMark;
        private string _suggestedMark;
        private double _suggestedStar;
        private string _selectedMark;
        private string _judge = "";
        private readonly Dictionary<string, Image> _judgeButtons = new Dictionary<string, Image>();
        private string _tab = "P";
        private Action _onClosed;
        private bool _open;

        // ------------------------------------------------------------------
        public static bool IsOpen
        {
            get { return _instance != null && _instance._open; }
        }

        public static void Show(string levelPath, string song, string artist, Action onClosed)
        {
            try
            {
                Ensure();
                if (_instance == null) return;
                _instance.ShowInternal(levelPath, song, artist, onClosed);
            }
            catch (Exception e)
            {
                Main.Error(e);
                if (onClosed != null) onClosed();
            }
        }

        /// <summary>UMM 关掉模组时调用：关掉弹窗（并放行被拦住的切场景）。</summary>
        public static void ForceClose()
        {
            if (_instance != null && _instance._open) _instance.Close(false);
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            GameObject go = new GameObject("PhigrosRatingPrompt", typeof(RectTransform));
            UnityEngine.Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<RatingPrompt>();
        }

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

        private void ShowInternal(string levelPath, string song, string artist, Action onClosed)
        {
            _levelPath = levelPath;
            _onClosed = onClosed;
            _currentMark = levelPath == null ? null : RatingMarks.GetMark(levelPath);

            // AutoRating 建议：没手动评级时给一个预选值
            _suggestedMark = null;
            _suggestedStar = 0.0;
            if (string.IsNullOrEmpty(_currentMark) && (Main.Config == null || Main.Config.AutoRatingSuggest))
            {
                string auto;
                double star;
                string gg;
                if (AutoRatingBridge.TryGetLive(out auto, out star, out gg))
                {
                    _suggestedMark = auto;
                    _suggestedStar = star;
                }
                else if (levelPath != null)
                {
                    _suggestedMark = AutoRatingBridge.GetCachedMark(levelPath);
                    _suggestedStar = AutoRatingBridge.GetCachedStar(levelPath);
                }
            }

            _judge = RatingMarks.GetJudge(_levelPath) ?? "";
            RefreshJudge();

            _selectedMark = string.IsNullOrEmpty(_currentMark) && !string.IsNullOrEmpty(_suggestedMark)
                ? _suggestedMark
                : _currentMark;
            _open = true;

            string displaySong = string.IsNullOrEmpty(song) ? Lang.T("未知曲目", "Unknown song") : song;
            _title.text = Lang.T("为这张谱面评级", "Rate this chart");
            _subtitle.text = string.IsNullOrEmpty(artist) ? displaySong : displaySong + "   ·   " + artist;

            string label = Lang.T("当前：", "Current: ") + (string.IsNullOrEmpty(_currentMark) ? RatingMarks.Unranked : _currentMark);
            if (!string.IsNullOrEmpty(_suggestedMark))
            {
                label += Lang.T("     AutoRating：", "     AutoRating: ") + _suggestedMark;
                if (_suggestedStar > 0.0) label += Lang.T("（★", " (★") + _suggestedStar.ToString("0.00") + Lang.T("）", ")");
            }
            SetText(_currentLabel, label);
            ApplyCurrentIcon();

            string tab = string.IsNullOrEmpty(_currentMark) && string.IsNullOrEmpty(_suggestedMark)
                ? "P"
                : RatingMarks.TabOf(string.IsNullOrEmpty(_currentMark) ? _suggestedMark : _currentMark);
            _tab = Array.IndexOf(RatingMarks.Tabs, tab) >= 0 ? tab : "P";
            RefreshTabs();
            RebuildGrid();

            EnsureInput();

            _group.alpha = 0f;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

        }

        private static void SetText(Text text, string value)
        {
            if (text != null) text.text = value ?? "";
        }

        private void ApplyCurrentIcon()
        {
            string shown = !string.IsNullOrEmpty(_currentMark) ? _currentMark : _suggestedMark;
            Sprite icon = RatingMarks.Icon(string.IsNullOrEmpty(shown) ? RatingMarks.Unranked : shown);
            _currentIcon.sprite = icon;
            _currentIcon.gameObject.SetActive(icon != null);
        }

        private void Update()
        {
            if (!_open) return;
            // 淡入
            if (_group.alpha < 1f) _group.alpha = Mathf.Min(1f, _group.alpha + Time.unscaledDeltaTime * 6f);


        }

        /// <summary>游戏里没有可用的 EventSystem 时，临时建一个（关闭时销毁，避免影响游戏自己的 UI）。</summary>
        private void EnsureInput()
        {
            if (EventSystem.current != null) return;
            if (_ownEventSystem != null) return;

            _ownEventSystem = new GameObject("PhigrosRatingEventSystem");
            UnityEngine.Object.DontDestroyOnLoad(_ownEventSystem);
            _ownEventSystem.AddComponent<EventSystem>();
            _ownEventSystem.AddComponent<StandaloneInputModule>();
            Main.Info("no EventSystem in scene, created a temporary one");
        }

        private void ReleaseInput()
        {
            if (_ownEventSystem == null) return;
            UnityEngine.Object.Destroy(_ownEventSystem);
            _ownEventSystem = null;
        }

        private void Close(bool save)
        {
            if (!_open) return;
            _open = false;

            try
            {
                if (save)
                {
                    if (!string.IsNullOrEmpty(_selectedMark)) RatingMarks.SetMark(_levelPath, _selectedMark);
                    RatingMarks.SetJudge(_levelPath, string.IsNullOrEmpty(_judge) ? null : _judge);
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            gameObject.SetActive(false);
            ReleaseInput();

            Action callback = _onClosed;
            _onClosed = null;
            if (callback != null)
            {
                try { callback(); }
                catch (Exception e) { Main.Error(e); }
            }
        }

        // ------------------------------------------------------------------
        // 交互
        // ------------------------------------------------------------------
        private void SelectMark(string mark)
        {
            _selectedMark = mark;
            SetText(_currentLabel, Lang.T("当前：", "Current: ") + (string.IsNullOrEmpty(mark) ? RatingMarks.Unranked : mark));
            _currentMark = mark;
            ApplyCurrentIcon();
            RefreshSelection();
        }

        private void RefreshJudge()
        {
            foreach (KeyValuePair<string, Image> pair in _judgeButtons)
            {
                if (pair.Value == null) continue;
                pair.Value.color = pair.Key == _judge ? Accent : CellColor;
            }
        }

        private void RefreshSelection()
        {
            foreach (KeyValuePair<string, Image> pair in _cellBackgrounds)
            {
                if (pair.Value == null) continue;
                bool selected = pair.Key == _selectedMark;
                pair.Value.color = selected ? Accent : CellColor;
            }
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool active = RatingMarks.Tabs[i] == _tab;
                Image image = _tabButtons[i].GetComponent<Image>();
                if (image != null) image.color = active ? Accent : CellColor;
                if (_tabTexts[i] != null) _tabTexts[i].color = active ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            }
        }

        private void SetTab(string tab)
        {
            if (_tab == tab) return;
            _tab = tab;
            RefreshTabs();
            RebuildGrid();
        }

        // ------------------------------------------------------------------
        // 构建
        // ------------------------------------------------------------------
        private void Build()
        {
            RectTransform root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32767;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            gameObject.AddComponent<GraphicRaycaster>();

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // 遮罩（拦截点击）
            RectTransform dim = UiKit.Rect("Dim", transform);
            UiKit.Stretch(dim);
            Image dimImage = UiKit.Image(dim, new Color(0f, 0f, 0f, 0.66f));
            dimImage.raycastTarget = true;

            Sprite rounded = UiKit.MakeRounded(96, 26, Color.white);

            // 面板
            RectTransform panelShadow = UiKit.Rect("PanelShadow", transform);
            UiKit.Anchor(panelShadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(PanelWidth + 10f, PanelHeight + 10f));
            Image shadowImage = UiKit.Image(panelShadow, new Color(0f, 0f, 0f, 0.55f), rounded);
            shadowImage.type = Image.Type.Sliced;

            _panel = UiKit.Rect("Panel", transform);
            UiKit.Anchor(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(PanelWidth, PanelHeight));
            Image panelImage = UiKit.Image(_panel, PanelColor, rounded);
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = true;

            // 标题
            RectTransform titleRect = UiKit.Rect("Title", _panel);
            UiKit.Anchor(titleRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -40f), new Vector2(900f, 60f));
            _title = UiKit.Text(titleRect, "为这张谱面评级", 40, Color.white, TextAnchor.MiddleLeft);
            _title.fontStyle = FontStyle.Bold;

            RectTransform subRect = UiKit.Rect("Subtitle", _panel);
            UiKit.Anchor(subRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -104f), new Vector2(1000f, 34f));
            _subtitle = UiKit.Text(subRect, "", 23, new Color(1f, 1f, 1f, 0.62f), TextAnchor.MiddleLeft);

            // 当前评级
            RectTransform currentIconRect = UiKit.Rect("CurrentIcon", _panel);
            UiKit.Anchor(currentIconRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -36f), new Vector2(92f, 92f));
            _currentIcon = UiKit.Image(currentIconRect, Color.white, null);
            _currentIcon.preserveAspect = true;

            RectTransform currentLabelRect = UiKit.Rect("CurrentLabel", _panel);
            UiKit.Anchor(currentLabelRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-180f, -62f), new Vector2(420f, 44f));
            _currentLabel = UiKit.Text(currentLabelRect, "", 26, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleRight);

            // 关闭
            RectTransform closeRect = UiKit.Rect("Close", _panel);
            UiKit.Anchor(closeRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -10f), new Vector2(52f, 52f));
            Image closeBg = UiKit.Image(closeRect, new Color(1f, 1f, 1f, 0.08f), UiKit.MakeRounded(64, 32, Color.white));
            closeBg.type = Image.Type.Sliced;
            closeBg.raycastTarget = true;
            Button closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeBg;
            closeButton.onClick.AddListener(delegate { Close(false); });
            RectTransform closeTextRect = UiKit.Rect("X", closeRect);
            UiKit.Stretch(closeTextRect);
            Text closeText = UiKit.Text(closeTextRect, "×", 38, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
            closeText.raycastTarget = false;

            // 标签页
            for (int i = 0; i < RatingMarks.Tabs.Length; i++)
            {
                string tab = RatingMarks.Tabs[i];
                RectTransform tabRect = UiKit.Rect("Tab_" + tab, _panel);
                UiKit.Anchor(tabRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f + i * 196f, -156f), new Vector2(184f, 56f));

                Image tabBg = UiKit.Image(tabRect, CellColor, rounded);
                tabBg.type = Image.Type.Sliced;
                tabBg.raycastTarget = true;
                Button tabButton = tabRect.gameObject.AddComponent<Button>();
                tabButton.targetGraphic = tabBg;
                tabButton.onClick.AddListener(delegate { SetTab(tab); });

                RectTransform tabTextRect = UiKit.Rect("Label", tabRect);
                UiKit.Stretch(tabTextRect);
                Text tabText = UiKit.Text(tabTextRect, RatingMarks.TabLabels[i], 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
                tabText.raycastTarget = false;

                _tabButtons.Add(tabButton);
                _tabTexts.Add(tabText);
            }

            // 网格
            RectTransform viewport = UiKit.Rect("Viewport", _panel);
            UiKit.Anchor(viewport, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -232f), new Vector2(PanelWidth - 96f, 372f));
            Image viewportImage = UiKit.Image(viewport, new Color(1f, 1f, 1f, 0.01f));
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiKit.Rect("Content", viewport);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 100f);

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _content;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 60f;
            _scroll.inertia = false;

            // 底部按钮
            MakeActionButton(_panel, "Unranked", Lang.T("未评级", "Unranked"), new Color(1f, 1f, 1f, 0.10f), delegate { SelectMark(RatingMarks.Unranked); Close(true); }, 220f, -480f);
            MakeActionButton(_panel, "Cancel", Lang.T("取消", "Cancel"), new Color(1f, 1f, 1f, 0.10f), delegate { Close(false); }, 200f, -264f);
            MakeActionButton(_panel, "Save", Lang.T("保存", "Save"), new Color(1f, 0.35f, 0.44f, 1f), delegate { Close(true); }, 200f, -48f);

            // 提示
            RectTransform hintRect = UiKit.Rect("Hint", _panel);
            UiKit.Anchor(hintRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 46f), new Vector2(760f, 30f));
            Text hint = UiKit.Text(hintRect, Lang.T("在对应分类里点一个图标 · 未评级 = Unranked", "Click an icon in a tab · Unranked = no rating"), 19, new Color(1f, 1f, 1f, 0.42f), TextAnchor.MiddleLeft);
            hint.raycastTarget = false;
        }

        private void MakeActionButton(RectTransform parent, string name, string label, Color color, UnityEngine.Events.UnityAction action, float width, float x)
        {
            RectTransform rect = UiKit.Rect(name, parent);
            // 注意：x 从右往左（负数）
            UiKit.Anchor(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(x, 44f), new Vector2(width, 66f));

            Image bg = UiKit.Image(rect, color, UiKit.MakeRounded(64, 20, Color.white));
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(action);

            RectTransform textRect = UiKit.Rect("Label", rect);
            UiKit.Stretch(textRect);
            Text text = UiKit.Text(textRect, label, 26, Color.white, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
        }

        private void RebuildGrid()
        {
            foreach (GameObject cell in _cells)
            {
                if (cell != null) UnityEngine.Object.Destroy(cell);
            }
            _cells.Clear();
            _cellBackgrounds.Clear();

            List<string> marks = RatingMarks.ListTab(_tab);
            if (marks == null) return;

            Sprite rounded = UiKit.MakeRounded(64, 20, Color.white);

            for (int i = 0; i < marks.Count; i++)
            {
                string mark = marks[i];
                int row = i / Columns;
                int col = i % Columns;

                RectTransform cell = UiKit.Rect("Cell_" + mark, _content);
                cell.anchorMin = new Vector2(0f, 1f);
                cell.anchorMax = new Vector2(0f, 1f);
                cell.pivot = new Vector2(0f, 1f);
                cell.anchoredPosition = new Vector2(col * (CellSize + CellGap), -row * (CellSize + CellGap));
                cell.sizeDelta = new Vector2(CellSize, CellSize);

                Image bg = UiKit.Image(cell, CellColor, rounded);
                bg.type = Image.Type.Sliced;
                bg.raycastTarget = true;
                Button button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;

                RectTransform iconRect = UiKit.Rect("Icon", cell);
                UiKit.Stretch(iconRect);
                iconRect.offsetMin = new Vector2(12f, 12f);
                iconRect.offsetMax = new Vector2(-12f, -12f);
                Image icon = UiKit.Image(iconRect, Color.white, RatingMarks.Icon(mark));
                icon.preserveAspect = true;

                string captured = mark;
                button.onClick.AddListener(delegate { SelectMark(captured); });

                _cells.Add(cell.gameObject);
                _cellBackgrounds[mark] = bg;
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(marks.Count / (float)Columns));
            _content.sizeDelta = new Vector2(0f, rows * (CellSize + CellGap) + 8f);
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;

            RefreshSelection();
        }
    }
}

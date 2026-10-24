using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// UMM 设置界面：仿 Iridium 的分区 / 折叠 / 双列布局。
    /// 全部用 IMGUI 自绘（生成纯色贴图当卡片背景），不依赖额外资源。
    /// </summary>
    public static class SettingsUI
    {
        private static readonly Color Bg = new Color(0.09f, 0.10f, 0.13f, 1f);
        private static readonly Color Card = new Color(0.14f, 0.15f, 0.19f, 1f);
        private static readonly Color Header = new Color(0.18f, 0.19f, 0.24f, 1f);
        private static readonly Color Accent = new Color(1f, 0.22f, 0.29f, 1f);
        private static readonly Color Text = new Color(0.90f, 0.91f, 0.94f, 1f);
        private static readonly Color Muted = new Color(0.60f, 0.64f, 0.72f, 1f);

        private static Texture2D _bgTex;
        private static Texture2D _cardTex;
        private static Texture2D _headerTex;
        private static Texture2D _accentTex;

        private static GUIStyle _card;
        private static GUIStyle _header;
        private static GUIStyle _label;
        private static GUIStyle _muted;
        private static GUIStyle _value;
        private static GUIStyle _title;
        private static GUIStyle _tabStyle;
        private static GUIStyle _tabActive;
        private static GUIStyle _button;

        private static int _tab;
        private static readonly Dictionary<string, bool> _fold = new Dictionary<string, bool>();
        private static Vector2 _scroll;

        private static readonly string[] Tabs = new string[] { "通用", "加载画面", "动画", "演出", "关于" };

        // ------------------------------------------------------------------
        private static Texture2D Solid(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static void EnsureStyles()
        {
            if (_label != null) return;

            _bgTex = Solid(Bg);
            _cardTex = Solid(Card);
            _headerTex = Solid(Header);
            _accentTex = Solid(Accent);

            _card = new GUIStyle(GUI.skin.box);
            _card.normal.background = _cardTex;
            _card.border = new RectOffset(6, 6, 6, 6);
            _card.margin = new RectOffset(0, 0, 4, 8);
            _card.padding = new RectOffset(10, 10, 8, 10);

            _header = new GUIStyle(GUI.skin.button);
            _header.normal.background = _headerTex;
            _header.hover.background = _headerTex;
            _header.active.background = _headerTex;
            _header.alignment = TextAnchor.MiddleLeft;
            _header.fontStyle = FontStyle.Bold;
            _header.fontSize = 13;
            _header.normal.textColor = Text;
            _header.hover.textColor = Text;
            _header.padding = new RectOffset(10, 8, 6, 6);
            _header.margin = new RectOffset(0, 0, 0, 2);

            _label = new GUIStyle(GUI.skin.label);
            _label.normal.textColor = Text;
            _label.fontSize = 12;
            _label.alignment = TextAnchor.MiddleLeft;

            _muted = new GUIStyle(_label);
            _muted.normal.textColor = Muted;
            _muted.fontSize = 11;

            _value = new GUIStyle(_label);
            _value.alignment = TextAnchor.MiddleRight;
            _value.normal.textColor = Accent;

            _title = new GUIStyle(_label);
            _title.fontSize = 16;
            _title.fontStyle = FontStyle.Bold;

            _tabStyle = new GUIStyle(GUI.skin.button);
            _tabStyle.normal.textColor = Muted;
            _tabStyle.hover.textColor = Text;
            _tabStyle.fontSize = 12;
            _tabStyle.margin = new RectOffset(0, 4, 0, 4);
            _tabStyle.padding = new RectOffset(12, 12, 6, 6);

            _tabActive = new GUIStyle(_tabStyle);
            _tabActive.normal.background = _accentTex;
            _tabActive.hover.background = _accentTex;
            _tabActive.normal.textColor = Color.white;
            _tabActive.hover.textColor = Color.white;
            _tabActive.fontStyle = FontStyle.Bold;

            _button = new GUIStyle(GUI.skin.button);
            _button.fontSize = 11;
            _button.padding = new RectOffset(8, 8, 4, 4);
            _button.margin = new RectOffset(2, 2, 0, 0);
        }

        // ------------------------------------------------------------------
        // 组件
        // ------------------------------------------------------------------
        private static bool Fold(string id, string title, bool defaultOpen = true)
        {
            bool open;
            if (!_fold.TryGetValue(id, out open)) open = defaultOpen;

            if (GUILayout.Button((open ? "▾  " : "▸  ") + title, _header))
            {
                open = !open;
                _fold[id] = open;
            }

            return open;
        }

        private static void Row(string label, Action control, string hint = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _label, GUILayout.Width(210f));
            control();
            if (!string.IsNullOrEmpty(hint)) GUILayout.Label(hint, _muted, GUILayout.Width(150f));
            GUILayout.EndHorizontal();
        }

        private static void ToggleRow(string label, ref bool value, string hint = null)
        {
            bool current = value;
            Row(label, delegate
            {
                if (GUILayout.Toggle(current, "", GUILayout.Width(24f)) != current) current = !current;
            }, hint);
            value = current;
        }

        private static void SliderRow(string label, ref float value, float min, float max, string format)
        {
            float current = value;
            Row(label, delegate
            {
                float next = GUILayout.HorizontalSlider(current, min, max, GUILayout.Width(190f));
                if (Math.Abs(next - current) > 0.0001f) current = next;
                GUILayout.Label(next.ToString(format), _value, GUILayout.Width(56f));
            });
            value = current;
        }

        private static void ButtonRow(string label, string button, Action onClick)
        {
            Row(label, delegate
            {
                if (GUILayout.Button(button, _button, GUILayout.Width(150f))) onClick();
            });
        }

        private static void ChoiceRow(string label, string[] options, ref string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _label, GUILayout.Width(210f));
            for (int i = 0; i < options.Length; i++)
            {
                bool active = string.Equals(value, options[i], StringComparison.OrdinalIgnoreCase);
                if (GUILayout.Button(active ? "● " + options[i] : options[i], active ? _tabActive : _button, GUILayout.Width(90f)))
                {
                    value = options[i];
                }
            }
            GUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------
        public static void Draw()
        {
            Settings cfg = Main.Config;
            if (cfg == null) return;

            EnsureStyles();

            GUILayout.BeginVertical(GUILayout.ExpandHeight(false));

            // 标题
            GUILayout.BeginHorizontal();
            GUILayout.Label("Adofai Loading Screen", _title);
            GUILayout.FlexibleSpace();
            GUILayout.Label("v" + (Main.VersionText ?? ""), _muted);
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // 标签页
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (GUILayout.Button(Tabs[i], i == _tab ? _tabActive : _tabStyle, GUILayout.Width(88f))) _tab = i;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(340f));
            switch (_tab)
            {
                case 0: DrawGeneral(cfg); break;
                case 1: DrawLoading(cfg); break;
                case 2: DrawAnimation(cfg); break;
                case 3: DrawBoss(cfg); break;
                default: DrawAbout(); break;
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("恢复默认", _button, GUILayout.Width(110f)))
            {
                cfg.Reset();
                Settings.Save();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("改完点 UMM 的 Save 写入 config.json · 改动实时生效", _muted);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // ------------------------------------------------------------------
        private static void DrawGeneral(Settings cfg)
        {
            GUILayout.BeginVertical(_card);

            if (Fold("general", "开关"))
            {
                ToggleRow("启用模组元素", ref cfg.Enabled);
                ToggleRow("离开关卡时弹评级窗", ref cfg.RatingPrompt);
                ToggleRow("已评级时跳过评级窗", ref cfg.SkipRatedPrompt);
                ToggleRow("退出关卡播放转场", ref cfg.ExitAnimation);
                ToggleRow("修复语言被锁成中文", ref cfg.FixLanguage);
            }

            if (Fold("lang", "语言与快捷键"))
            {
                ChoiceRow("界面语言", new string[] { "auto", "zh", "en" }, ref cfg.Language);

                GUILayout.BeginHorizontal();
                GUILayout.Label("局内改评级快捷键", _label, GUILayout.Width(210f));
                if (GUILayout.Button(string.IsNullOrEmpty(cfg.Hotkey) ? "未设置" : cfg.Hotkey, _button, GUILayout.Width(90f)))
                {
                    Main.BeginHotkeyCapture();
                }
                GUILayout.Label(Main.IsCapturingHotkey ? "请按一个键…" : "点按钮后按任意键", _muted, GUILayout.Width(180f));
                GUILayout.EndHorizontal();
            }

            if (Fold("exit", "退出动画"))
            {
                ChoiceRow("样式", new string[] { "auto", "fade", "overlay" }, ref cfg.ExitStyle);
                GUILayout.Label("auto = 进关用 boss 演出时纯淡出；fade = 总是退出动画；overlay = 旧曲图过渡", _muted);
            }

            GUILayout.EndVertical();
        }

        private static void DrawLoading(Settings cfg)
        {
            GUILayout.BeginVertical(_card);

            if (Fold("cover", "曲图"))
            {
                SliderRow("压暗强度", ref cfg.Dim, 0f, 1f, "0.00");
                ToggleRow("缓慢放大（Ken Burns）", ref cfg.KenBurns);
            }

            if (Fold("bar", "顶部加载条"))
            {
                SliderRow("高度", ref cfg.BarThickness, 1f, 60f, "0");
                ToggleRow("红蓝交替扫过", ref cfg.BarSweep);
                SliderRow("扫过一次时长(秒)", ref cfg.BarSweepSeconds, 0.3f, 5f, "0.00");
                SliderRow("色块宽度(占比)", ref cfg.BarSweepWidth, 0.1f, 0.6f, "0.00");
                SliderRow("百分比字号", ref cfg.PercentFont, 8f, 120f, "0");
                ToggleRow("显示百分比", ref cfg.ShowPercent);
            }

            if (Fold("info", "右侧曲目信息"))
            {
                SliderRow("离右边距", ref cfg.InfoRight, 0f, 700f, "0");
                SliderRow("垂直偏移", ref cfg.InfoY, -500f, 500f, "0");
                SliderRow("曲名字号", ref cfg.LoadTitleFont, 10f, 160f, "0");
                SliderRow("曲师字号", ref cfg.LoadArtistFont, 8f, 120f, "0");
                SliderRow("谱师字号", ref cfg.LoadChartFont, 8f, 120f, "0");
            }

            if (Fold("diff", "右下角评级图标"))
            {
                SliderRow("图标高度", ref cfg.DiffHeight, 40f, 800f, "0");
                SliderRow("离右边距", ref cfg.DiffRight, 0f, 800f, "0");
                SliderRow("离底边距", ref cfg.DiffBottom, 0f, 800f, "0");
                ToggleRow("显示谱面特效评级（T2/T3/T4）", ref cfg.ShowFxRating);
            }

            if (Fold("time", "时长"))
            {
                SliderRow("最短(秒)", ref cfg.MinLoadingSeconds, 0f, 5f, "0.00");
                SliderRow("最长(秒)", ref cfg.MaxLoadingSeconds, 10f, 300f, "0");
            }

            GUILayout.EndVertical();
        }

        private static void DrawAnimation(Settings cfg)
        {
            GUILayout.BeginVertical(_card);

            if (Fold("style", "样式"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("加载动画", _label, GUILayout.Width(210f));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(6f);
                for (int i = 0; i < LoadingAnim.Styles.Length; i++)
                {
                    string style = LoadingAnim.Styles[i];
                    bool active = string.Equals(cfg.AnimStyle, style, StringComparison.OrdinalIgnoreCase);
                    if (GUILayout.Button(active ? "● " + style : style, active ? _tabActive : _button, GUILayout.Width(96f)))
                    {
                        cfg.AnimStyle = style;
                    }
                    if (i == 3)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(6f);
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (Fold("custom", "自定义素材"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Animations 文件夹", _label, GUILayout.Width(210f));
                string[] folders = Main.ListAnimFolders();
                string current = string.IsNullOrEmpty(cfg.AnimFolder) ? (folders.Length > 0 ? folders[0] : "（空）") : cfg.AnimFolder;
                if (GUILayout.Button(current, _button, GUILayout.Width(200f)))
                {
                    cfg.AnimFolder = Main.NextAnimFolder(folders, cfg.AnimFolder);
                }
                if (GUILayout.Button("打开目录", _button, GUILayout.Width(100f))) Main.OpenAnimFolder();
                GUILayout.EndHorizontal();

                bool bar = string.Equals(cfg.AnimSlot, "bar", StringComparison.OrdinalIgnoreCase);
                ToggleRow("放在顶栏（否则居中）", ref bar);
                cfg.AnimSlot = bar ? "bar" : "center";
                SliderRow("帧率(0=用json)", ref cfg.AnimFps, 0f, 60f, "0");
                SliderRow("指示器大小", ref cfg.AnimScale, 0.2f, 4f, "0.00");
                SliderRow("指示器垂直偏移", ref cfg.AnimOffsetY, -400f, 400f, "0");
            }

            GUILayout.EndVertical();
        }

        private static void DrawBoss(Settings cfg)
        {
            GUILayout.BeginVertical(_card);

            if (Fold("source", "显示的评级"))
            {
                ChoiceRow("评级来源", new string[] { "tuf", "gg" }, ref cfg.BossRatingSource);
                GUILayout.Label("tuf = P/G/U 图标 + 分档演出；gg = 一排 <Lv.XX> 纯文字", _muted);

                SliderRow("数字大小", ref cfg.BossGgNumberSize, 0.3f, 6f, "0.00");
                SliderRow("括号大小", ref cfg.BossGgLabelSize, 0.1f, 3f, "0.00");
                ToggleRow("数字滚动（老虎机效果）", ref cfg.BossRoll);
            }

            if (Fold("tune", "演出参数"))
            {
                SliderRow("速度", ref cfg.BossSpeed, 0.4f, 2f, "0.00");
                SliderRow("圆圈大小", ref cfg.BossScale, 0.5f, 1.8f, "0.00");
                SliderRow("数字大小(TUF)", ref cfg.BossDigitScale, 0.6f, 1.6f, "0.00");
                SliderRow("故障强度(0=关)", ref cfg.BossGlitch, 0f, 2f, "0.00");
                ToggleRow("演出期间锁输入", ref cfg.LockInputDuringIntro);
            }

            if (Fold("tiers", "分档一览", false))
            {
                GUILayout.Label("P1-9 简易 / P10-20 标准 / G1-9 侧飞 / G10-20 坠落", _muted);
                GUILayout.Label("U1-9 冲刺 / U10-14 汇聚 / U15-16 重故障 / U17-19 数字先行", _muted);
                GUILayout.Label("U20 大招 / UJ 螺旋", _muted);
            }

            GUILayout.EndVertical();
        }

        private static void DrawAbout()
        {
            GUILayout.BeginVertical(_card);

            GUILayout.Label("Adofai Loading Screen", _title);
            GUILayout.Label("版本 " + (Main.VersionText ?? ""), _muted);
            GUILayout.Space(4f);
            GUILayout.Label("加载画面 / 评级徽章 / boss 演出", _label);
            GUILayout.Label("github.com/C4z-Rove/adofai-loading-screen", _muted);

            GUILayout.Space(6f);
            GUILayout.Label("评级图标放在 Marks\\ 下；boss 演出的字母素材在 Marks\\_split\\", _muted);
            GUILayout.Label("自定义动画放在 Animations\\<名字>\\（PNG 序列或 sheet.png + anim.json）", _muted);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("预览一次", _button, GUILayout.Width(120f))) Main.StartPreview();
            if (GUILayout.Button("打开模组目录", _button, GUILayout.Width(140f))) Main.OpenModFolder();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}

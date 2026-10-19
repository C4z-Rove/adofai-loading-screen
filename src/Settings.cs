using System;
using System.IO;
using Newtonsoft.Json;

namespace PhigrosLoading
{
    public class Settings
    {
        /// <summary>总开关</summary>
        public bool Enabled = true;

        /// <summary>曲图之上的压暗强度（0-1）</summary>
        public float Dim = 0.42f;

        /// <summary>是否在加载条右侧显示百分比</summary>
        public bool ShowPercent = true;

        /// <summary>曲图缓慢放大的“呼吸”效果</summary>
        public bool KenBurns = true;

        /// <summary>调试用：启动游戏后立刻预览一次加载画面（默认关闭）</summary>
        public bool PreviewOnStart = false;

        /// <summary>调试预览用哪个评级（默认 P19）</summary>
        public string PreviewMark = "P19";

        /// <summary>退出关卡时弹出评级窗口</summary>
        public bool RatingPrompt = true;

        /// <summary>已经评过级的谱面，退出时直接跳过评级窗</summary>
        public bool SkipRatedPrompt = true;

        /// <summary>局内改评级的快捷键（KeyCode 名字，例如 F7）</summary>
        public string Hotkey = "F7";

        /// <summary>启动后把语言按设置重新应用一次（修复被锁成中文的问题）</summary>
        public bool FixLanguage = true;

        /// <summary>通关时记录用的判定（wide/normal/strict）</summary>
        public bool RecordJudge = true;

        /// <summary>加载画面最短显示时间（秒）；加载本身更慢时不会额外延长</summary>
        public float MinLoadingSeconds = 1.2f;

        /// <summary>加载画面最长显示时间（秒）；超过就强制收掉，避免卡住</summary>
        public float MaxLoadingSeconds = 90f;

        /// <summary>离开关卡时也播放这一段动画（替代原版黑屏）</summary>
        public bool ExitAnimation = true;

        /// <summary>退出动画样式：auto（boss 进关→纯淡出）/ fade（总是纯淡出）/ overlay（总是曲图过渡）</summary>
        public string ExitStyle = "auto";

        /// <summary>游戏进行中在右下角显示评级文字</summary>
        public bool InGameDifficulty = true;

        /// <summary>评级弹窗里用 AutoRating 的结果作为建议并预选</summary>
        public bool AutoRatingSuggest = true;

        /// <summary>没有手动评级时，用 AutoRating 的结果代替 Unranked 显示</summary>
        public bool AutoRatingDisplay = true;

        /// <summary>自定义选关界面里，在完成度右上方显示评级图标</summary>
        public bool ClsBadge = true;

        /// <summary>选关评级图标的水平偏移（单位 = 一个图标的边长，负值往左）</summary>
        public float ClsBadgeOffsetX = -0.45f;

        /// <summary>选关评级图标的垂直偏移（单位 = 一个图标的边长）</summary>
        public float ClsBadgeOffsetY = 0f;

        /// <summary>选关判定图标大小（倍率，基于行高）</summary>
        public float ClsJudgeScale = 1.15f;

        /// <summary>选关判定图标水平偏移（单位 = 一个图标的边长）</summary>
        public float ClsJudgeOffsetX = 0.25f;

        /// <summary>选关判定图标垂直偏移（单位 = 一个图标的边长）</summary>
        public float ClsJudgeOffsetY = 0f;

        /// <summary>评级图标目录（留空 = 自动找 LevelLibrary\assets\marks）</summary>
        public string MarksPath = "";

        /// <summary>界面语言：auto / zh / en</summary>
        public string Language = "auto";

        // ---------------- 游戏内角落文字 ----------------
        /// <summary>左下角曲名：离左边距</summary>
        public float TitleX = 22f;
        /// <summary>左下角曲名：离底边距</summary>
        public float TitleY = 48f;
        /// <summary>左下角曲名字号</summary>
        public float TitleFont = 36f;

        /// <summary>右下角评级：离右边距</summary>
        public float RatingX = 22f;
        /// <summary>右下角评级：离底边距</summary>
        public float RatingY = 48f;
        /// <summary>右下角评级字号</summary>
        public float RatingFont = 36f;

        // ---------------- 加载画面 ----------------
        /// <summary>右侧信息块：离右边距</summary>
        public float InfoRight = 148f;
        /// <summary>右侧信息块：垂直偏移（正数向上）</summary>
        public float InfoY = 66f;
        /// <summary>加载画面曲名字号</summary>
        public float LoadTitleFont = 62f;
        /// <summary>加载画面曲师字号</summary>
        public float LoadArtistFont = 32f;
        /// <summary>加载画面谱师字号</summary>
        public float LoadChartFont = 26f;

        /// <summary>右下角评级图标高度</summary>
        public float DiffHeight = 240f;
        /// <summary>右下角评级图标离右边距</summary>
        public float DiffRight = 120f;
        /// <summary>右下角评级图标离底边距</summary>
        public float DiffBottom = 84f;

        /// <summary>顶部加载条高度</summary>
        public float BarThickness = 11f;

        /// <summary>Phigros 同款加载条：红/蓝色块交替从左扫到右</summary>
        public bool BarSweep = true;

        /// <summary>动画样式：sweep / pulse / wave / flow / spinner / dots / custom</summary>
        public string AnimStyle = "sweep";

        /// <summary>自定义动画文件夹名（Animations\ 下），空=用第一个</summary>
        public string AnimFolder = "";

        /// <summary>自定义动画放在哪：center / bar</summary>
        public string AnimSlot = "center";

        /// <summary>自定义动画帧率（0=用 anim.json 里的值）</summary>
        public float AnimFps = 0f;

        /// <summary>中央指示器缩放</summary>
        public float AnimScale = 1f;

        /// <summary>中央指示器垂直偏移</summary>
        public float AnimOffsetY = 0f;

        /// <summary>boss 演出速度倍率（1 = 原速）</summary>
        public float BossSpeed = 1f;

        /// <summary>boss 圆圈大小倍率</summary>
        public float BossScale = 1f;

        /// <summary>boss 故障强度倍率（0 = 关掉故障）</summary>
        public float BossGlitch = 1f;

        /// <summary>boss 数字大小倍率</summary>
        public float BossDigitScale = 1f;

        /// <summary>boss 演出显示哪种评级：tuf（P/G/U 图标）或 gg（Lv.XX 纯文字）</summary>
        public string BossRatingSource = "tuf";

        /// <summary>色块扫过一次的时长（秒）</summary>
        public float BarSweepSeconds = 1.1f;

        /// <summary>色块宽度（占整条的比例）</summary>
        public float BarSweepWidth = 0.25f;
        /// <summary>右上角百分比字号</summary>
        public float PercentFont = 24f;

        public void Reset()
        {
            Settings d = new Settings();
            Enabled = d.Enabled;
            Dim = d.Dim;
            ShowPercent = d.ShowPercent;
            KenBurns = d.KenBurns;
            RatingPrompt = d.RatingPrompt;
            SkipRatedPrompt = d.SkipRatedPrompt;
            Hotkey = d.Hotkey;
            FixLanguage = d.FixLanguage;
            RecordJudge = d.RecordJudge;
            MinLoadingSeconds = d.MinLoadingSeconds;
            MaxLoadingSeconds = d.MaxLoadingSeconds;
            ExitAnimation = d.ExitAnimation;
            ExitStyle = d.ExitStyle;
            InGameDifficulty = d.InGameDifficulty;
            AutoRatingSuggest = d.AutoRatingSuggest;
            AutoRatingDisplay = d.AutoRatingDisplay;
            ClsBadge = d.ClsBadge;
            ClsBadgeOffsetX = d.ClsBadgeOffsetX;
            ClsBadgeOffsetY = d.ClsBadgeOffsetY;
            ClsJudgeScale = d.ClsJudgeScale;
            ClsJudgeOffsetX = d.ClsJudgeOffsetX;
            ClsJudgeOffsetY = d.ClsJudgeOffsetY;
            MarksPath = d.MarksPath;
            Language = d.Language;

            TitleX = d.TitleX;
            TitleY = d.TitleY;
            TitleFont = d.TitleFont;
            RatingX = d.RatingX;
            RatingY = d.RatingY;
            RatingFont = d.RatingFont;

            InfoRight = d.InfoRight;
            InfoY = d.InfoY;
            LoadTitleFont = d.LoadTitleFont;
            LoadArtistFont = d.LoadArtistFont;
            LoadChartFont = d.LoadChartFont;
            DiffHeight = d.DiffHeight;
            DiffRight = d.DiffRight;
            DiffBottom = d.DiffBottom;
            BarThickness = d.BarThickness;
            BarSweep = d.BarSweep;
            AnimStyle = d.AnimStyle;
            AnimFolder = d.AnimFolder;
            AnimSlot = d.AnimSlot;
            AnimFps = d.AnimFps;
            AnimScale = d.AnimScale;
            AnimOffsetY = d.AnimOffsetY;
            BossSpeed = d.BossSpeed;
            BossScale = d.BossScale;
            BossGlitch = d.BossGlitch;
            BossDigitScale = d.BossDigitScale;
            BossRatingSource = d.BossRatingSource;
            BarSweepSeconds = d.BarSweepSeconds;
            BarSweepWidth = d.BarSweepWidth;
            PercentFont = d.PercentFont;
        }

        public static string Path => System.IO.Path.Combine(Main.ModPath, "config.json");

        public static void Save()
        {
            try
            {
                File.WriteAllText(Path, JsonConvert.SerializeObject(Main.Config, Formatting.Indented));
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        public static Settings Load(string path)
        {
            Settings settings = null;
            try
            {
                if (File.Exists(path))
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            if (settings == null) settings = new Settings();

            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            return settings;
        }
    }
}

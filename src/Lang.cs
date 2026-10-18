using System;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>界面文案：跟随游戏语言（可在设置里强制 中文 / English）。</summary>
    public static class Lang
    {
        public static bool Chinese
        {
            get
            {
                string mode = Main.Config != null ? (Main.Config.Language ?? "auto") : "auto";
                if (string.Equals(mode, "zh", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(mode, "en", StringComparison.OrdinalIgnoreCase)) return false;

                try
                {
                    SystemLanguage lang = RDString.language;
                    if (lang == SystemLanguage.Chinese || lang == SystemLanguage.ChineseSimplified
                        || lang == SystemLanguage.ChineseTraditional) return true;
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        public static string T(string zh, string en)
        {
            return Chinese ? zh : en;
        }
    }
}

using System;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 修复「设置里是英文、游戏里却是中文」：
    /// 游戏启动时 RDString.Setup() 读 Persistence.language 的那一刻，PlayerPrefs 里的
    /// language 还是默认值（跟随系统 = 中文），等真正的设置值读出来就已经晚了，
    /// 运行时语言被锁死。这里在启动后把设置里的语言重新应用一次，并持续盯着，
    /// 设置里改语言也能立刻生效（不用重启）。
    /// </summary>
    public class LanguageFixer : MonoBehaviour
    {
        private static LanguageFixer _instance;
        private float _next;
        private bool _loggedOnce;

        public static void Init()
        {
            if (_instance != null) return;
            try
            {
                GameObject go = new GameObject("PhigrosLanguageFixer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<LanguageFixer>();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;

            try
            {
                if (!Main.Active) return;
                if (Main.Config != null && !Main.Config.FixLanguage) return;

                SystemLanguage saved = Persistence.language;
                if (RDString.language == saved) return;

                if (!_loggedOnce)
                {
                    _loggedOnce = true;
                    Main.Info("language mismatch: saved=" + saved + ", runtime=" + RDString.language + " -> applying saved");
                }

                RDString.ChangeLanguage(saved);
                Main.Info("language applied: " + RDString.language);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }
    }
}

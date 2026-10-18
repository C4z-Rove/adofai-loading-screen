using System;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 通关时把这张谱面用的判定记下来（Lenient / Normal / Strict）。
    ///
    /// 游戏的判定是全局设置（GCS.difficulty，0/1/2），并不存在谱面文件里，
    /// 所以「通关时的判定」只能靠我们自己记录：一旦进入 States.Won，
    /// 就把当前 GCS.difficulty 写进 judges.txt，选关界面就能显示对应图标。
    /// </summary>
    public class JudgmentRecorder : MonoBehaviour
    {
        private static JudgmentRecorder _instance;

        private string _recordedPath;

        public static void Init()
        {
            if (_instance != null) return;
            try
            {
                GameObject go = new GameObject("PhigrosJudgmentRecorder");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<JudgmentRecorder>();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        /// <summary>GCS.difficulty → wide / normal / strict。</summary>
        public static string CurrentJudge()
        {
            switch ((int)GCS.difficulty)
            {
                case 0: return "wide";     // Lenient
                case 2: return "strict";   // Strict
                default: return "normal";  // Normal
            }
        }

        private void Update()
        {
            try
            {
                if (!Main.Active) return;
                if (Main.Config != null && !Main.Config.RecordJudge) return;

                if (!ADOBase.isScnGame || ADOBase.isLevelEditor)
                {
                    _recordedPath = null;
                    return;
                }

                scrController controller = scrController.instance;
                if (controller == null) return;

                if (controller.currentState != States.Won)
                {
                    if (controller.currentState == States.Start) _recordedPath = null;
                    return;
                }

                string path = Patch_LoadSceneWithTransition.CurrentLevelPath();
                if (string.IsNullOrEmpty(path) || path == _recordedPath) return;

                _recordedPath = path;

                string judge = CurrentJudge();
                if (RatingMarks.GetLocalJudge(path) == judge) return;

                RatingMarks.SetJudge(path, judge);
                Main.Info("judge recorded at clear: " + judge + " for " + path);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 调试用：启动后预览一次加载画面 + 右下角评级文字 + 评级弹窗
    /// （config.json 里 PreviewOnStart = true 时启用）。
    /// </summary>
    public class PreviewRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // 调试：PreviewMark = "exit:" 时直接模拟一次退出转场
            if (Main.Config != null && Main.Config.PreviewMark != null && Main.Config.PreviewMark.StartsWith("exit:"))
            {
                yield return new WaitForSecondsRealtime(3f);

                LoadingInfo exitInfo = new LoadingInfo();
                exitInfo.Key = "PreviewExit";
                exitInfo.IsExit = true;
                exitInfo.Title = "exit test";
                PhigrosOverlay.Show(exitInfo);

                yield return new WaitForSecondsRealtime(0.25f);
                Main.Info("exit-test: " + PhigrosOverlay.DebugState());
                yield return new WaitForSecondsRealtime(1.0f);
                Main.Info("exit-test later: " + PhigrosOverlay.DebugState());
                yield break;
            }

            yield return new WaitForSecondsRealtime(2f);

            InGameBadge.Preview("P19", "Adofai Loading Screen");

            yield return new WaitForSecondsRealtime(7f);
            InGameBadge.EndPreview();

            yield return new WaitForSecondsRealtime(0.4f);

            LoadingInfo info = new LoadingInfo();
            info.Key = "Preview";
            string previewMark = (Main.Config != null && !string.IsNullOrEmpty(Main.Config.PreviewMark)) ? Main.Config.PreviewMark : "P19";
            if (previewMark.StartsWith("gg:"))
            {
                info.GGText = previewMark.Substring(3);
                previewMark = "P19";
            }
            info.Mark = previewMark;
            info.Title = "Adofai Loading Screen";
            info.Artist = "A Dance of Fire and Ice";
            info.Author = "Adofai Loading Screen";
            info.Difficulty = 7;

            info.PreparePreview();
            info.Mark = (Main.Config != null && !string.IsNullOrEmpty(Main.Config.PreviewMark)) ? Main.Config.PreviewMark : "P19";
            PhigrosOverlay.Show(info);

            yield return new WaitForSecondsRealtime(7f);
            PhigrosOverlay.Complete();

            yield return new WaitForSecondsRealtime(1f);
            RatingPrompt.Show(null, "Adofai Loading Screen", "A Dance of Fire and Ice", null);

            yield return new WaitForSecondsRealtime(40f);
            Destroy(gameObject);
        }
    }
}

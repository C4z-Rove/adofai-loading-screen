using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>启动时查一次 GitHub 最新 Release，有新版就在设置面板顶部提示。</summary>
    public static class UpdateCheck
    {
        public const string RepoUrl = "https://github.com/C4z-Rove/adofai-loading-screen";
        public const string ApiUrl = "https://api.github.com/repos/C4z-Rove/adofai-loading-screen/releases/latest";

        public static string LatestTag { get; private set; }
        public static bool Checked { get; private set; }

        public static bool HasUpdate
        {
            get
            {
                if (!Checked || string.IsNullOrEmpty(LatestTag)) return false;
                string local = "v" + Main.VersionText;
                return !string.Equals(LatestTag, local, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void Check()
        {
            try
            {
                Thread thread = new Thread(new ThreadStart(delegate
                {
                    try
                    {
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("User-Agent", "adofai-loading-screen");
                            client.Headers.Add("Accept", "application/vnd.github+json");
                            string json = client.DownloadString(ApiUrl);
                            Match match = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                            if (match.Success) LatestTag = match.Groups[1].Value;
                        }
                    }
                    catch
                    {
                        // 断网/被墙都静默
                    }
                    finally
                    {
                        Checked = true;
                    }
                }));
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        public static void OpenReleases()
        {
            try
            {
                Application.OpenURL(RepoUrl + "/releases/latest");
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        public static void OpenRepo()
        {
            try
            {
                Application.OpenURL(RepoUrl);
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }
    }
}

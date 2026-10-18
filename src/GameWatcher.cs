using UnityEngine;

namespace PhigrosLoading
{
    /// <summary>
    /// 关卡级的状态：这一关是否已经弹过评级窗（每关只拦一次）。
    /// </summary>
    public class GameWatcher : MonoBehaviour
    {
        private static bool _prompted;

        public static bool PromptShown
        {
            get { return _prompted; }
            set { _prompted = value; }
        }

        public static void OnLevelStart()
        {
            _prompted = false;
        }
    }
}

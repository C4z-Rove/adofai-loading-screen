using System;
using UnityEngine;
using UnityEngine.UI;

namespace PhigrosLoading
{
    /// <summary>
    /// boss 进关演出：独立 canvas、独立对象树，播完（或关卡退出）立刻销毁。
    /// 完全不依赖加载画面那个 overlay，避免任何残留。
    /// </summary>
    public class BossStage : MonoBehaviour
    {
        public static bool Active { get; private set; }

        /// <summary>演出期间锁住谱面输入。</summary>
        public static bool InputLocked
        {
            get
            {
                return Active
                    && Main.Config != null
                    && Main.Config.LockInputDuringIntro
                    && Main.Config.AnimStyle != null
                    && (string.Equals(Main.Config.AnimStyle, "boss", StringComparison.OrdinalIgnoreCase));
            }
        }

        private LoadingAnim _anim;
        private CanvasGroup _group;
        private float _duration;
        private float _elapsed;

        public static void Play(string mark, string gg, string fxMark)
        {
            Disconnect();

            try
            {
                GameObject go = new GameObject("PhigrosBossStage");
                UnityEngine.Object.DontDestroyOnLoad(go);

                Canvas canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32766;

                CanvasScaler scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;

                RectTransform root = (RectTransform)go.transform;

                CanvasGroup group = go.AddComponent<CanvasGroup>();
                group.alpha = 1f;
                group.blocksRaycasts = false;
                group.interactable = false;

                BossStage stage = go.AddComponent<BossStage>();
                stage._group = group;
                stage._anim = new LoadingAnim();
                stage._anim.Build(root, Main.Config);
                stage._anim.SetRating(mark, gg);
                stage._anim.SetVisible(true);
                stage._duration = stage._anim.BossDuration(Main.Config) + 0.9f;


                Active = true;
                Main.Info("boss stage: start for " + mark + " (" + stage._duration.ToString("0.0") + "s)");
            }
            catch (Exception e)
            {
                Main.Error(e);
            }
        }

        /// <summary>立刻结束并销毁所有 boss 舞台。</summary>
        public static void Disconnect()
        {
            try
            {
                BossStage[] all = Resources.FindObjectsOfTypeAll<BossStage>();
                foreach (BossStage s in all)
                {
                    if (s == null) continue;
                    if (!s.gameObject.scene.IsValid()) continue;
                    UnityEngine.Object.DestroyImmediate(s.gameObject);
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
            }

            Active = false;
        }

        private void Update()
        {
            try
            {
                if (_anim == null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                    return;
                }

                float dt = Time.unscaledDeltaTime;
                _elapsed += dt;
                _anim.Tick(1f, dt, Main.Config);

                // 结尾 0.6 秒整体淡出，接到关卡画面
                float fadeStart = Mathf.Max(0f, _duration - 0.6f);
                if (_elapsed > fadeStart && _group != null)
                {
                    _group.alpha = Mathf.Clamp01(1f - (_elapsed - fadeStart) / 0.6f);
                }

                if (_elapsed >= _duration)
                {
                    Active = false;
                    Main.Info("boss stage: END after " + _elapsed.ToString("0.00") + "s");
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
            catch (Exception e)
            {
                Main.Error(e);
                Active = false;
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}

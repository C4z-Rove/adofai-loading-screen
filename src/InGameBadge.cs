using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PhigrosLoading;

public class InGameBadge : MonoBehaviour
{
	private const float ReferenceHeight = 900f;

	private const float MarginX = 22f;

	private const float MarginY = 18f;

	private static InGameBadge _instance;

	private static bool _preview;

	private static string _previewMark = "P19";

	private static string _previewTitle = "Adofai Loading Screen";

	private Canvas _canvas;

	private CanvasScaler _scaler;

	private CanvasGroup _group;

	private RectTransform _titleRect;

	private RectTransform _markRect;

	private Text _title;

	private Text _mark;

	private bool _layoutApplied;

	private float _lx;

	private float _ly;

	private float _lf;

	private float _rx;

	private float _ry;

	private float _rf;

	private bool _built;

	private string _shownMark;

	private string _shownTitle;

	private float _nextRefresh;

	private bool _targetVisible;

	private int _screenH;

	public static void Preview(string mark, string title)
	{
		Ensure();
		_previewMark = (string.IsNullOrEmpty(mark) ? "Unranked" : mark);
		_previewTitle = (string.IsNullOrEmpty(title) ? "Adofai Loading Screen" : title);
		_preview = true;
	}

	public static void Hide()
	{
		if ((Object)(object)_instance != (Object)null)
		{
			_instance.HideNow();
		}
	}

	public static void EndPreview()
	{
		_preview = false;
		if ((Object)(object)_instance != (Object)null)
		{
			_instance.HideNow();
		}
	}

	public static void Init()
	{
		Ensure();
	}

	private static void Ensure()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		if ((Object)(object)_instance != (Object)null)
		{
			return;
		}
		try
		{
			GameObject val = new GameObject("PhigrosInGameBadge", new Type[1] { typeof(RectTransform) });
			UnityEngine.Object.DontDestroyOnLoad((Object)val);
			_instance = val.AddComponent<InGameBadge>();
		}
		catch (Exception e)
		{
			Main.Error(e);
		}
	}

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
	}

	private void Build()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		RectTransform val = (RectTransform)((Component)this).transform;
		val.anchorMin = Vector2.zero;
		val.anchorMax = Vector2.one;
		val.offsetMin = Vector2.zero;
		val.offsetMax = Vector2.zero;
		_canvas = ((Component)this).gameObject.AddComponent<Canvas>();
		_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		_canvas.sortingOrder = 32000;
		_scaler = ((Component)this).gameObject.AddComponent<CanvasScaler>();
		_scaler.uiScaleMode = (CanvasScaler.ScaleMode)1;
		_scaler.referenceResolution = new Vector2(1920f, 900f);
		_scaler.screenMatchMode = (CanvasScaler.ScreenMatchMode)0;
		_scaler.matchWidthOrHeight = 1f;
		_screenH = Screen.height;
		_group = ((Component)this).gameObject.AddComponent<CanvasGroup>();
		_group.alpha = 0f;
		_group.blocksRaycasts = false;
		_group.interactable = false;
		RectTransform rt = (_titleRect = UiKit.Rect("SongTitle", ((Component)this).transform));
		UiKit.Anchor(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 18f), new Vector2(1100f, 46f));
		_title = UiKit.Text(rt, "", 36, new Color(1f, 1f, 1f, 0.9f), (TextAnchor)3);
		_title.horizontalOverflow = (HorizontalWrapMode)1;
		_title.verticalOverflow = (VerticalWrapMode)1;
		UiKit.AddOutline((Graphic)(object)_title, 1.4f);
		UiKit.AddShadow((Graphic)(object)_title, 2f);
		RectTransform rt2 = (_markRect = UiKit.Rect("Rating", ((Component)this).transform));
		UiKit.Anchor(rt2, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 18f), new Vector2(460f, 46f));
		_mark = UiKit.Text(rt2, "", 36, new Color(1f, 1f, 1f, 0.94f), (TextAnchor)5);
		_mark.fontStyle = (FontStyle)1;
		_mark.horizontalOverflow = (HorizontalWrapMode)1;
		_mark.verticalOverflow = (VerticalWrapMode)1;
		UiKit.AddOutline((Graphic)(object)_mark, 1.4f);
		UiKit.AddShadow((Graphic)(object)_mark, 2f);
		_built = true;
		_group.alpha = 0f;
	}

	private void Update()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Invalid comparison between Unknown and I4
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Invalid comparison between Unknown and I4
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Invalid comparison between Unknown and I4
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		if (!_built)
		{
			return;
		}
		if (!Main.Active)
		{
			HideNow();
			return;
		}
		ApplyLayoutIfNeeded();
		if (_preview)
		{
			SetTexts(_previewTitle, _previewMark);
			_targetVisible = true;
			Fade();
			return;
		}
		bool flag = false;
		if (Main.Config == null || Main.Config.InGameDifficulty)
		{
			try
			{
				if (ADOBase.isScnGame && !ADOBase.isLevelEditor)
				{
					scrController instance = scrController.instance;
					scnGame instance2 = scnGame.instance;
					if ((Object)(object)instance != (Object)null && !instance.paused && (Object)(object)instance2 != (Object)null && !instance2.isLoading)
					{
						States currentState = instance.currentState;
						if ((int)currentState == 1 || (int)currentState == 2 || (int)currentState == 3 || (int)currentState == 4)
						{
							flag = true;
						}
					}
				}
			}
			catch (Exception e)
			{
				Main.Error(e);
			}
		}
		if (flag && Time.unscaledTime >= _nextRefresh)
		{
			_nextRefresh = Time.unscaledTime + 0.5f;
			string mark = "Unranked";
			string title = "";
			try
			{
				scnGame instance3 = scnGame.instance;
				if ((Object)(object)instance3 != (Object)null)
				{
					if (!string.IsNullOrEmpty(instance3.levelPath))
					{
						AutoRatingBridge.CacheFor(instance3.levelPath);
						string text = null;
						if ((Main.Config == null || Main.Config.AutoRatingDisplay) && AutoRatingBridge.TryGetLive(out var mark2, out var _, out var gg))
						{
							text = AutoRatingBridge.Format(mark2, gg);
						}
						if (!string.IsNullOrEmpty(text))
						{
							mark = text;
						}
						else
						{
							string mark3 = RatingMarks.GetMark(instance3.levelPath);
							if (!string.IsNullOrEmpty(mark3))
							{
								mark = mark3;
							}
						}
					}
					if (instance3.levelData != null && !string.IsNullOrEmpty(instance3.levelData.song))
					{
						title = UiKit.StripRichText(instance3.levelData.song);
					}
				}
			}
			catch (Exception e2)
			{
				Main.Error(e2);
			}
			SetTexts(title, mark);
		}
		_targetVisible = flag;
		Fade();
	}

	private void ApplyLayoutIfNeeded()
	{
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		Settings config = Main.Config;
		if (config != null && (!_layoutApplied || _lx != config.TitleX || _ly != config.TitleY || _lf != config.TitleFont || _rx != config.RatingX || _ry != config.RatingY || _rf != config.RatingFont))
		{
			_layoutApplied = true;
			_lx = config.TitleX;
			_ly = config.TitleY;
			_lf = config.TitleFont;
			_rx = config.RatingX;
			_ry = config.RatingY;
			_rf = config.RatingFont;
			if ((Object)(object)_titleRect != (Object)null)
			{
				UiKit.Anchor(_titleRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Mathf.Max(0f, config.TitleX), Mathf.Max(0f, config.TitleY)), new Vector2(1100f, 46f));
			}
			if ((Object)(object)_title != (Object)null)
			{
				_title.fontSize = Mathf.Max(8, Mathf.RoundToInt(config.TitleFont));
			}
			if ((Object)(object)_markRect != (Object)null)
			{
				UiKit.Anchor(_markRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f - Mathf.Max(0f, config.RatingX), Mathf.Max(0f, config.RatingY)), new Vector2(460f, 46f));
			}
			if ((Object)(object)_mark != (Object)null)
			{
				_mark.fontSize = Mathf.Max(8, Mathf.RoundToInt(config.RatingFont));
			}
		}
	}

	private void SetTexts(string title, string mark)
	{
		if (string.IsNullOrEmpty(mark))
		{
			mark = "Unranked";
		}
		if (_shownTitle != title)
		{
			_shownTitle = title;
			if ((Object)(object)_title != (Object)null)
			{
				_title.text = title ?? "";
			}
		}
		if (_shownMark != mark)
		{
			_shownMark = mark;
			if ((Object)(object)_mark != (Object)null)
			{
				_mark.text = mark;
			}
		}
	}

	private void Fade()
	{
		if (!((Object)(object)_group == (Object)null))
		{
			float num = (_targetVisible ? 1f : 0f);
			_group.alpha = Mathf.MoveTowards(_group.alpha, num, Time.unscaledDeltaTime * 8f);
		}
	}

	private void HideNow()
	{
		_targetVisible = false;
		if ((Object)(object)_group != (Object)null)
		{
			_group.alpha = 0f;
		}
	}
}

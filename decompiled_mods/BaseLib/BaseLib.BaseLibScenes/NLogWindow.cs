using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using BaseLib.Config;
using BaseLib.ConsoleCommands;
using BaseLib.Extensions;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Logging;

namespace BaseLib.BaseLibScenes;

[GlobalClass]
[ScriptPath("res://BaseLibScenes/NLogWindow.cs")]
public class NLogWindow : Window
{
	public class MethodName : MethodName
	{
		public static readonly StringName AddLog = StringName.op_Implicit("AddLog");

		public static readonly StringName OpenOnErr = StringName.op_Implicit("OpenOnErr");

		public static readonly StringName SetDirty = StringName.op_Implicit("SetDirty");

		public static readonly StringName _EnterTree = StringName.op_Implicit("_EnterTree");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName ApplyMinSizeForScale = StringName.op_Implicit("ApplyMinSizeForScale");

		public static readonly StringName ApplyChromeFontSize = StringName.op_Implicit("ApplyChromeFontSize");

		public static readonly StringName OnSizeChanged = StringName.op_Implicit("OnSizeChanged");

		public static readonly StringName _Notification = StringName.op_Implicit("_Notification");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName UpdateFilter = StringName.op_Implicit("UpdateFilter");

		public static readonly StringName RegenText = StringName.op_Implicit("RegenText");

		public static readonly StringName Refresh = StringName.op_Implicit("Refresh");

		public static readonly StringName UpdateText = StringName.op_Implicit("UpdateText");

		public static readonly StringName ScrollToBottomAsync = StringName.op_Implicit("ScrollToBottomAsync");

		public static readonly StringName MatchesFilter = StringName.op_Implicit("MatchesFilter");

		public static readonly StringName OnScrollbarValueChanged = StringName.op_Implicit("OnScrollbarValueChanged");

		public static readonly StringName IsNearBottom = StringName.op_Implicit("IsNearBottom");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName ChangeFontSize = StringName.op_Implicit("ChangeFontSize");

		public static readonly StringName SetFontSize = StringName.op_Implicit("SetFontSize");

		public static readonly StringName RenderLine = StringName.op_Implicit("RenderLine");

		public static readonly StringName TryGetBracketLevel = StringName.op_Implicit("TryGetBracketLevel");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Limit = StringName.op_Implicit("Limit");

		public static readonly StringName _writeIndex = StringName.op_Implicit("_writeIndex");

		public static readonly StringName _scrollContainer = StringName.op_Implicit("_scrollContainer");

		public static readonly StringName _logLabel = StringName.op_Implicit("_logLabel");

		public static readonly StringName _logLevelLabel = StringName.op_Implicit("_logLevelLabel");

		public static readonly StringName _logLevelDropdown = StringName.op_Implicit("_logLevelDropdown");

		public static readonly StringName _filterInput = StringName.op_Implicit("_filterInput");

		public static readonly StringName _regexButton = StringName.op_Implicit("_regexButton");

		public static readonly StringName _inverseButton = StringName.op_Implicit("_inverseButton");

		public static readonly StringName _filterText = StringName.op_Implicit("_filterText");

		public static readonly StringName _settingChanged = StringName.op_Implicit("_settingChanged");

		public static readonly StringName _isFollowingLog = StringName.op_Implicit("_isFollowingLog");

		public static readonly StringName _currentFontSize = StringName.op_Implicit("_currentFontSize");

		public static readonly StringName _needsRefresh = StringName.op_Implicit("_needsRefresh");

		public static readonly StringName _timeSinceRefresh = StringName.op_Implicit("_timeSinceRefresh");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Lock _logLock = new Lock();

	private static ImmutableList<NLogWindow> _listeners = ImmutableList<NLogWindow>.Empty;

	private static bool _openedOnErr = false;

	private const int MaxBufferedLines = 8192;

	private const int TrimChunk = 1024;

	private static readonly List<string> _fullLog = new List<string>();

	private static int _logBaseIndex = 0;

	private static int _fullLogCount = 0;

	private int _writeIndex;

	private ScrollContainer? _scrollContainer;

	private RichTextLabel? _logLabel;

	private Label? _logLevelLabel;

	private OptionButton? _logLevelDropdown;

	private LineEdit? _filterInput;

	private Button? _regexButton;

	private Button? _inverseButton;

	private string _filterText = "";

	private Regex? _regex;

	private bool _settingChanged;

	private bool _isFollowingLog = true;

	private int _currentFontSize;

	private bool _needsRefresh;

	private double _timeSinceRefresh;

	private static readonly Color ErrorColor = Color.FromHtml("#ff6d6d".AsSpan());

	private static readonly Color WarnColor = Color.FromHtml("#ffd866".AsSpan());

	private static readonly Color DebugColor = Color.FromHtml("#7fdfff".AsSpan());

	public int Limit { get; private set; } = BaseLibConfig.LimitedLogSize;

	public static bool IsOpen => _listeners.Count > 0;

	public static void AddLog(string msg)
	{
		using (_logLock.EnterScope())
		{
			_fullLog.Add(msg);
			if (_fullLog.Count > 9216)
			{
				int num = _fullLog.Count - 8192;
				_fullLog.RemoveRange(0, num);
				_logBaseIndex += num;
			}
			_fullLogCount = _logBaseIndex + _fullLog.Count;
		}
		foreach (NLogWindow listener in _listeners)
		{
			listener.SetDirty();
		}
	}

	public static void OpenOnErr()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (BaseLibConfig.OpenLogWindowOnError && !IsOpen && !_openedOnErr)
		{
			_openedOnErr = true;
			Callable val = Callable.From((Action)delegate
			{
				OpenLogWindow.OpenWindow(stealFocus: true);
			});
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	private void SetDirty()
	{
		_needsRefresh = true;
	}

	public override void _EnterTree()
	{
		((Node)this)._EnterTree();
		ImmutableInterlocked.Update<ImmutableList<NLogWindow>>(ref _listeners, (ImmutableList<NLogWindow> list) => list.Add(this));
	}

	public override void _ExitTree()
	{
		((Node)this)._ExitTree();
		ImmutableInterlocked.Update<ImmutableList<NLogWindow>>(ref _listeners, (ImmutableList<NLogWindow> list) => list.Remove(this));
		if (_listeners.Count == 0)
		{
			_openedOnErr = false;
		}
	}

	public unsafe override void _Ready()
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Expected O, but got Unknown
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Expected O, but got Unknown
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Expected O, but got Unknown
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Expected O, but got Unknown
		((Viewport)this).OwnWorld3D = true;
		((Node)this)._Ready();
		_scrollContainer = ((Node)this).GetNode<ScrollContainer>(NodePath.op_Implicit("MainVBox/Scroll"));
		_logLabel = ((Node)this).GetNode<RichTextLabel>(NodePath.op_Implicit("MainVBox/Scroll/Log"));
		_logLevelLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("MainVBox/TopBarContainer/TopBarHBox/LogLevelLabel"));
		_logLevelDropdown = ((Node)this).GetNode<OptionButton>(NodePath.op_Implicit("MainVBox/TopBarContainer/TopBarHBox/LogLevelOption"));
		_filterInput = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("MainVBox/TopBarContainer/TopBarHBox/FilterText"));
		_regexButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("MainVBox/TopBarContainer/TopBarHBox/RegexButton"));
		_inverseButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("MainVBox/TopBarContainer/TopBarHBox/InverseButton"));
		((Control)_logLabel).AddThemeFontOverride(StringName.op_Implicit("normal_font"), ResourceLoader.Load<Font>("res://fonts/source_code_pro_medium.ttf", (string)null, (CacheMode)1));
		LogLevel[] values = Enum.GetValues<LogLevel>();
		for (int i = 0; i < values.Length; i++)
		{
			LogLevel val = values[i];
			_logLevelDropdown.AddItem(((object)(*(LogLevel*)(&val))/*cast due to .constrained prefix*/).ToString(), -1);
		}
		_logLevelDropdown.Selected = BaseLibConfig.LastLogLevel;
		((BaseButton)_regexButton).ButtonPressed = BaseLibConfig.LogUseRegex;
		((BaseButton)_inverseButton).ButtonPressed = BaseLibConfig.LogInvertFilter;
		_filterInput.Text = BaseLibConfig.LogLastFilter;
		_currentFontSize = BaseLibConfig.LogFontSize;
		_filterInput.TextChanged += (TextChangedEventHandler)delegate
		{
			_settingChanged = true;
			UpdateFilter();
		};
		((BaseButton)_regexButton).Toggled += (ToggledEventHandler)delegate
		{
			_settingChanged = true;
			UpdateFilter();
		};
		((BaseButton)_inverseButton).Toggled += (ToggledEventHandler)delegate
		{
			_settingChanged = true;
			RegenText();
			ScrollToBottomAsync();
		};
		_logLevelDropdown.ItemSelected += (ItemSelectedEventHandler)delegate
		{
			_settingChanged = true;
			RegenText();
			ScrollToBottomAsync();
		};
		((Viewport)this).SizeChanged += OnSizeChanged;
		((Window)this).CloseRequested += ((Node)this).QueueFree;
		_logLabel.Finished += delegate
		{
			if (_isFollowingLog)
			{
				ScrollToBottomAsync();
			}
		};
		((Range)_scrollContainer.GetVScrollBar()).ValueChanged += new ValueChangedEventHandler(OnScrollbarValueChanged);
		_isFollowingLog = true;
		SetFontSize(_currentFontSize, save: false);
		ApplyMinSizeForScale();
		UpdateFilter();
		((Node)this).ProcessMode = (ProcessModeEnum)3;
	}

	private void ApplyMinSizeForScale()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		float num = ((((Window)this).ContentScaleFactor > 0f) ? ((Window)this).ContentScaleFactor : 1f);
		((Window)this).MinSize = new Vector2I((int)(360f * num), (int)(66f * num));
	}

	private void ApplyChromeFontSize(int size)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		((Control)(object)_logLevelLabel)?.AddThemeFontSizeOverrideAll(size);
		((Control)(object)_logLevelDropdown)?.AddThemeFontSizeOverrideAll(size);
		((Control)(object)_filterInput)?.AddThemeFontSizeOverrideAll(size);
		((Control)(object)_regexButton)?.AddThemeFontSizeOverrideAll(size);
		((Control)(object)_inverseButton)?.AddThemeFontSizeOverrideAll(size);
		int num = Mathf.Max(28, (int)((float)size * 1.25f));
		if (_regexButton != null)
		{
			((Control)_regexButton).CustomMinimumSize = new Vector2((float)num, (float)num);
		}
		if (_inverseButton != null)
		{
			((Control)_inverseButton).CustomMinimumSize = new Vector2((float)num, (float)num);
		}
	}

	private void OnSizeChanged()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		BaseLibConfig.LogLastSizeX = ((Window)this).Size.X;
		BaseLibConfig.LogLastSizeY = ((Window)this).Size.Y;
		SetDirty();
		ModConfig.SaveDebounced<BaseLibConfig>();
	}

	public override void _Notification(int what)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this)._Notification(what);
		if ((long)what == 1012)
		{
			BaseLibConfig.LogLastPosX = ((Window)this).Position.X;
			BaseLibConfig.LogLastPosY = ((Window)this).Position.Y;
			ModConfig.SaveDebounced<BaseLibConfig>();
		}
	}

	public override void _Process(double delta)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I8
		((Node)this)._Process(delta);
		_timeSinceRefresh += delta;
		if (_needsRefresh && ((Window)this).Visible && (long)((Window)this).Mode != 1 && !(_timeSinceRefresh < 1.0 / 30.0))
		{
			_timeSinceRefresh = 0.0;
			_needsRefresh = false;
			if (BaseLibConfig.LimitedLogSize > Limit)
			{
				Limit = BaseLibConfig.LimitedLogSize;
				RegenText();
			}
			else
			{
				Limit = BaseLibConfig.LimitedLogSize;
				Refresh();
			}
		}
	}

	private void UpdateFilter()
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		LineEdit? filterInput = _filterInput;
		_filterText = ((filterInput != null) ? filterInput.Text : null) ?? "";
		Button? regexButton = _regexButton;
		if (regexButton == null || !((BaseButton)regexButton).ButtonPressed || string.IsNullOrEmpty(_filterText))
		{
			_regex = null;
		}
		else
		{
			try
			{
				_regex = new Regex(_filterText, RegexOptions.IgnoreCase);
				LineEdit? filterInput2 = _filterInput;
				if (filterInput2 != null)
				{
					((Control)filterInput2).RemoveThemeColorOverride(StringName.op_Implicit("font_color"));
				}
			}
			catch
			{
				LineEdit? filterInput3 = _filterInput;
				if (filterInput3 != null)
				{
					((Control)filterInput3).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color(1f, 0.4f, 0.4f, 1f));
				}
			}
		}
		RegenText();
		if (!_isFollowingLog)
		{
			ScrollToBottomAsync();
		}
	}

	public void RegenText()
	{
		RichTextLabel? logLabel = _logLabel;
		if (logLabel != null)
		{
			logLabel.Clear();
		}
		int num = 0;
		using (_logLock.EnterScope())
		{
			_writeIndex = _logBaseIndex;
			for (int num2 = _fullLog.Count - 1; num2 >= 0; num2--)
			{
				if (MatchesFilter(_fullLog[num2]))
				{
					num++;
					if (num >= BaseLibConfig.LimitedLogSize)
					{
						_writeIndex = _logBaseIndex + num2;
						break;
					}
				}
			}
		}
		Refresh();
	}

	public void Refresh()
	{
		if (((Node)this).IsNodeReady())
		{
			UpdateText();
			if (_settingChanged)
			{
				_settingChanged = false;
				BaseLibConfig.LastLogLevel = _logLevelDropdown.Selected;
				BaseLibConfig.LogInvertFilter = ((BaseButton)_inverseButton).ButtonPressed;
				BaseLibConfig.LogUseRegex = ((BaseButton)_regexButton).ButtonPressed;
				BaseLibConfig.LogLastFilter = _filterText;
				ModConfig.SaveDebounced<BaseLibConfig>();
			}
		}
	}

	private void UpdateText()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if (!((Node)this).IsNodeReady() || _logLabel == null || _scrollContainer == null || _logLevelDropdown == null)
		{
			return;
		}
		_isFollowingLog = _isFollowingLog || IsNearBottom();
		LogLevel minLevel = (LogLevel)_logLevelDropdown.Selected;
		while (_writeIndex < _fullLogCount)
		{
			string line;
			using (_logLock.EnterScope())
			{
				if (_writeIndex < _logBaseIndex)
				{
					_writeIndex = _logBaseIndex;
				}
				if (_writeIndex >= _fullLogCount)
				{
					break;
				}
				line = _fullLog[_writeIndex - _logBaseIndex];
				goto IL_009b;
			}
			IL_009b:
			if (MatchesFilter(line))
			{
				RenderLine(line, minLevel, _logLabel);
			}
			_writeIndex++;
		}
		int num = Math.Max(1, BaseLibConfig.LimitedLogSize);
		int num2 = 64;
		while (_logLabel.GetParagraphCount() > num && num2 > 0)
		{
			_logLabel.RemoveParagraph(0);
			num2--;
		}
		if (_isFollowingLog)
		{
			ScrollToBottomAsync();
		}
	}

	private async void ScrollToBottomAsync()
	{
		try
		{
			await ((GodotObject)this).ToSignal((GodotObject)(object)((Node)this).GetTree(), SignalName.ProcessFrame);
			if (_scrollContainer != null)
			{
				VScrollBar vScrollBar = _scrollContainer.GetVScrollBar();
				_scrollContainer.ScrollVertical = (int)((Range)vScrollBar).MaxValue;
				_isFollowingLog = true;
			}
		}
		catch (Exception)
		{
		}
	}

	private bool MatchesFilter(string line)
	{
		if (string.IsNullOrEmpty(_filterText))
		{
			return true;
		}
		bool flag = _regex?.IsMatch(line) ?? line.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
		Button? inverseButton = _inverseButton;
		if (inverseButton == null || !((BaseButton)inverseButton).ButtonPressed)
		{
			return flag;
		}
		return !flag;
	}

	private void OnScrollbarValueChanged(double value)
	{
		if (_scrollContainer != null)
		{
			_isFollowingLog = IsNearBottom(_scrollContainer.GetVScrollBar(), value);
		}
	}

	private bool IsNearBottom()
	{
		if (_scrollContainer == null)
		{
			return true;
		}
		VScrollBar vScrollBar = _scrollContainer.GetVScrollBar();
		return IsNearBottom(vScrollBar, ((Range)vScrollBar).Value);
	}

	private static bool IsNearBottom(VScrollBar scrollbar, double value)
	{
		return ((Range)scrollbar).MaxValue - ((Range)scrollbar).Page - value <= 8.0;
	}

	public override void _Input(InputEvent @event)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I8
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I8
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I8
		InputEventMouseButton val = (InputEventMouseButton)(object)((@event is InputEventMouseButton) ? @event : null);
		if (val != null && ((InputEventWithModifiers)val).CtrlPressed && ((long)val.ButtonIndex == 4 || (long)val.ButtonIndex == 5) && ((InputEvent)val).IsReleased())
		{
			ChangeFontSize(((long)val.ButtonIndex == 4) ? 1 : (-1));
			((Node)this).GetViewport().SetInputAsHandled();
		}
	}

	private void ChangeFontSize(int deltaPx)
	{
		SetFontSize(Math.Clamp(BaseLibConfig.LogFontSize + deltaPx, 8, 48));
	}

	private void SetFontSize(int newSize, bool save = true)
	{
		((Control)(object)_logLabel)?.AddThemeFontSizeOverrideAll(newSize);
		ApplyChromeFontSize(newSize);
		_currentFontSize = newSize;
		ScrollToBottomAsync();
		if (save)
		{
			BaseLibConfig.LogFontSize = newSize;
			ModConfig.SaveDebounced<BaseLibConfig>();
		}
	}

	private static void RenderLine(string line, LogLevel minLevel, RichTextLabel? label)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (label != null && TryGetBracketLevel(line) >= minLevel)
		{
			label.AddText(line);
		}
	}

	private static LogLevel TryGetBracketLevel(string line)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (!line.StartsWith('['))
		{
			return (LogLevel)3;
		}
		int num = line.IndexOf(']');
		if (num <= 1)
		{
			return (LogLevel)3;
		}
		if (!Enum.TryParse<LogLevel>(line.Substring(1, num - 1), ignoreCase: true, out LogLevel result))
		{
			return (LogLevel)5;
		}
		return result;
	}

	private static Color? GetColorForLine(string line)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected I4, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		LogLevel val = TryGetBracketLevel(line);
		return (val - 3) switch
		{
			2 => ErrorColor, 
			1 => WarnColor, 
			0 => null, 
			_ => DebugColor, 
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0420: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b4: Expected O, but got Unknown
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0501: Unknown result type (might be due to invalid IL or missing references)
		//IL_0529: Unknown result type (might be due to invalid IL or missing references)
		//IL_0534: Expected O, but got Unknown
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0560: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Unknown result type (might be due to invalid IL or missing references)
		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0603: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
		//IL_064d: Unknown result type (might be due to invalid IL or missing references)
		//IL_066e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_069f: Expected O, but got Unknown
		//IL_069a: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fa: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(25)
		{
			new MethodInfo(MethodName.AddLog, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("msg"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OpenOnErr, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetDirty, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._EnterTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyMinSizeForScale, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyChromeFontSize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("size"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnSizeChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Notification, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("what"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateFilter, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RegenText, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Refresh, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateText, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ScrollToBottomAsync, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.MatchesFilter, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("line"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnScrollbarValueChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.IsNearBottom, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.IsNearBottom, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("scrollbar"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("VScrollBar"), false),
				new PropertyInfo((Type)3, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ChangeFontSize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("deltaPx"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetFontSize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("newSize"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)1, StringName.op_Implicit("save"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RenderLine, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("line"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("minLevel"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)24, StringName.op_Implicit("label"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("RichTextLabel"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.TryGetBracketLevel, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("line"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_0437: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_047b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0484: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.AddLog && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			AddLog(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OpenOnErr && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OpenOnErr();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetDirty && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SetDirty();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._EnterTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._EnterTree();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyMinSizeForScale && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyMinSizeForScale();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyChromeFontSize && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ApplyChromeFontSize(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnSizeChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnSizeChanged();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Notification && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((GodotObject)this)._Notification(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateFilter && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateFilter();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RegenText && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RegenText();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Refresh && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Refresh();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateText && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateText();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ScrollToBottomAsync && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ScrollToBottomAsync();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.MatchesFilter && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = MatchesFilter(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.OnScrollbarValueChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnScrollbarValueChanged(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.IsNearBottom && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			bool flag2 = IsNearBottom();
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
			return true;
		}
		if ((ref method) == MethodName.IsNearBottom && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			bool flag3 = IsNearBottom(VariantUtils.ConvertTo<VScrollBar>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<bool>(ref flag3);
			return true;
		}
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ChangeFontSize && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ChangeFontSize(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetFontSize && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			SetFontSize(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RenderLine && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			RenderLine(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<LogLevel>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<RichTextLabel>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.TryGetBracketLevel && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			LogLevel val = TryGetBracketLevel(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<LogLevel>(ref val);
			return true;
		}
		return ((Window)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.AddLog && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			AddLog(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OpenOnErr && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OpenOnErr();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.IsNearBottom && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			bool flag = IsNearBottom(VariantUtils.ConvertTo<VScrollBar>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.RenderLine && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			RenderLine(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<LogLevel>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<RichTextLabel>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.TryGetBracketLevel && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			LogLevel val = TryGetBracketLevel(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<LogLevel>(ref val);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.AddLog)
		{
			return true;
		}
		if ((ref method) == MethodName.OpenOnErr)
		{
			return true;
		}
		if ((ref method) == MethodName.SetDirty)
		{
			return true;
		}
		if ((ref method) == MethodName._EnterTree)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyMinSizeForScale)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyChromeFontSize)
		{
			return true;
		}
		if ((ref method) == MethodName.OnSizeChanged)
		{
			return true;
		}
		if ((ref method) == MethodName._Notification)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateFilter)
		{
			return true;
		}
		if ((ref method) == MethodName.RegenText)
		{
			return true;
		}
		if ((ref method) == MethodName.Refresh)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateText)
		{
			return true;
		}
		if ((ref method) == MethodName.ScrollToBottomAsync)
		{
			return true;
		}
		if ((ref method) == MethodName.MatchesFilter)
		{
			return true;
		}
		if ((ref method) == MethodName.OnScrollbarValueChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.IsNearBottom)
		{
			return true;
		}
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName.ChangeFontSize)
		{
			return true;
		}
		if ((ref method) == MethodName.SetFontSize)
		{
			return true;
		}
		if ((ref method) == MethodName.RenderLine)
		{
			return true;
		}
		if ((ref method) == MethodName.TryGetBracketLevel)
		{
			return true;
		}
		return ((Window)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.Limit)
		{
			Limit = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._writeIndex)
		{
			_writeIndex = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._scrollContainer)
		{
			_scrollContainer = VariantUtils.ConvertTo<ScrollContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._logLabel)
		{
			_logLabel = VariantUtils.ConvertTo<RichTextLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._logLevelLabel)
		{
			_logLevelLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._logLevelDropdown)
		{
			_logLevelDropdown = VariantUtils.ConvertTo<OptionButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._filterInput)
		{
			_filterInput = VariantUtils.ConvertTo<LineEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._regexButton)
		{
			_regexButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._inverseButton)
		{
			_inverseButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._filterText)
		{
			_filterText = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._settingChanged)
		{
			_settingChanged = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isFollowingLog)
		{
			_isFollowingLog = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentFontSize)
		{
			_currentFontSize = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._needsRefresh)
		{
			_needsRefresh = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._timeSinceRefresh)
		{
			_timeSinceRefresh = VariantUtils.ConvertTo<double>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Limit)
		{
			int limit = Limit;
			value = VariantUtils.CreateFrom<int>(ref limit);
			return true;
		}
		if ((ref name) == PropertyName._writeIndex)
		{
			value = VariantUtils.CreateFrom<int>(ref _writeIndex);
			return true;
		}
		if ((ref name) == PropertyName._scrollContainer)
		{
			value = VariantUtils.CreateFrom<ScrollContainer>(ref _scrollContainer);
			return true;
		}
		if ((ref name) == PropertyName._logLabel)
		{
			value = VariantUtils.CreateFrom<RichTextLabel>(ref _logLabel);
			return true;
		}
		if ((ref name) == PropertyName._logLevelLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _logLevelLabel);
			return true;
		}
		if ((ref name) == PropertyName._logLevelDropdown)
		{
			value = VariantUtils.CreateFrom<OptionButton>(ref _logLevelDropdown);
			return true;
		}
		if ((ref name) == PropertyName._filterInput)
		{
			value = VariantUtils.CreateFrom<LineEdit>(ref _filterInput);
			return true;
		}
		if ((ref name) == PropertyName._regexButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref _regexButton);
			return true;
		}
		if ((ref name) == PropertyName._inverseButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref _inverseButton);
			return true;
		}
		if ((ref name) == PropertyName._filterText)
		{
			value = VariantUtils.CreateFrom<string>(ref _filterText);
			return true;
		}
		if ((ref name) == PropertyName._settingChanged)
		{
			value = VariantUtils.CreateFrom<bool>(ref _settingChanged);
			return true;
		}
		if ((ref name) == PropertyName._isFollowingLog)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isFollowingLog);
			return true;
		}
		if ((ref name) == PropertyName._currentFontSize)
		{
			value = VariantUtils.CreateFrom<int>(ref _currentFontSize);
			return true;
		}
		if ((ref name) == PropertyName._needsRefresh)
		{
			value = VariantUtils.CreateFrom<bool>(ref _needsRefresh);
			return true;
		}
		if ((ref name) == PropertyName._timeSinceRefresh)
		{
			value = VariantUtils.CreateFrom<double>(ref _timeSinceRefresh);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, PropertyName.Limit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._writeIndex, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._scrollContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._logLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._logLevelLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._logLevelDropdown, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._filterInput, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._regexButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._inverseButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName._filterText, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._settingChanged, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isFollowingLog, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._currentFontSize, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._needsRefresh, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._timeSinceRefresh, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName limit = PropertyName.Limit;
		int limit2 = Limit;
		info.AddProperty(limit, Variant.From<int>(ref limit2));
		info.AddProperty(PropertyName._writeIndex, Variant.From<int>(ref _writeIndex));
		info.AddProperty(PropertyName._scrollContainer, Variant.From<ScrollContainer>(ref _scrollContainer));
		info.AddProperty(PropertyName._logLabel, Variant.From<RichTextLabel>(ref _logLabel));
		info.AddProperty(PropertyName._logLevelLabel, Variant.From<Label>(ref _logLevelLabel));
		info.AddProperty(PropertyName._logLevelDropdown, Variant.From<OptionButton>(ref _logLevelDropdown));
		info.AddProperty(PropertyName._filterInput, Variant.From<LineEdit>(ref _filterInput));
		info.AddProperty(PropertyName._regexButton, Variant.From<Button>(ref _regexButton));
		info.AddProperty(PropertyName._inverseButton, Variant.From<Button>(ref _inverseButton));
		info.AddProperty(PropertyName._filterText, Variant.From<string>(ref _filterText));
		info.AddProperty(PropertyName._settingChanged, Variant.From<bool>(ref _settingChanged));
		info.AddProperty(PropertyName._isFollowingLog, Variant.From<bool>(ref _isFollowingLog));
		info.AddProperty(PropertyName._currentFontSize, Variant.From<int>(ref _currentFontSize));
		info.AddProperty(PropertyName._needsRefresh, Variant.From<bool>(ref _needsRefresh));
		info.AddProperty(PropertyName._timeSinceRefresh, Variant.From<double>(ref _timeSinceRefresh));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.Limit, ref val))
		{
			Limit = ((Variant)(ref val)).As<int>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._writeIndex, ref val2))
		{
			_writeIndex = ((Variant)(ref val2)).As<int>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._scrollContainer, ref val3))
		{
			_scrollContainer = ((Variant)(ref val3)).As<ScrollContainer>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._logLabel, ref val4))
		{
			_logLabel = ((Variant)(ref val4)).As<RichTextLabel>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._logLevelLabel, ref val5))
		{
			_logLevelLabel = ((Variant)(ref val5)).As<Label>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._logLevelDropdown, ref val6))
		{
			_logLevelDropdown = ((Variant)(ref val6)).As<OptionButton>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._filterInput, ref val7))
		{
			_filterInput = ((Variant)(ref val7)).As<LineEdit>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._regexButton, ref val8))
		{
			_regexButton = ((Variant)(ref val8)).As<Button>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._inverseButton, ref val9))
		{
			_inverseButton = ((Variant)(ref val9)).As<Button>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._filterText, ref val10))
		{
			_filterText = ((Variant)(ref val10)).As<string>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._settingChanged, ref val11))
		{
			_settingChanged = ((Variant)(ref val11)).As<bool>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._isFollowingLog, ref val12))
		{
			_isFollowingLog = ((Variant)(ref val12)).As<bool>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentFontSize, ref val13))
		{
			_currentFontSize = ((Variant)(ref val13)).As<int>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._needsRefresh, ref val14))
		{
			_needsRefresh = ((Variant)(ref val14)).As<bool>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._timeSinceRefresh, ref val15))
		{
			_timeSinceRefresh = ((Variant)(ref val15)).As<double>();
		}
	}
}

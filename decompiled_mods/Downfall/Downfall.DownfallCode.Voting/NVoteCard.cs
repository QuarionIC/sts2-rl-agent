using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Downfall.DownfallCode.Voting;

[ScriptPath("res://DownfallCode/Voting/NVoteCard.cs")]
public class NVoteCard : PanelContainer
{
	[Signal]
	public delegate void CardClickedEventHandler(string imagePath);

	[Signal]
	public delegate void ScoreChangedEventHandler();

	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName OnImageGuiInput = StringName.op_Implicit("OnImageGuiInput");

		public static readonly StringName Vote = StringName.op_Implicit("Vote");

		public static readonly StringName OpenReportPopup = StringName.op_Implicit("OpenReportPopup");

		public static readonly StringName UpdateReportHighlight = StringName.op_Implicit("UpdateReportHighlight");

		public static readonly StringName UpdateVoteHighlight = StringName.op_Implicit("UpdateVoteHighlight");

		public static readonly StringName Refresh = StringName.op_Implicit("Refresh");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Score = StringName.op_Implicit("Score");

		public static readonly StringName _authorLabel = StringName.op_Implicit("_authorLabel");

		public static readonly StringName _count = StringName.op_Implicit("_count");

		public static readonly StringName _down = StringName.op_Implicit("_down");

		public static readonly StringName _downButton = StringName.op_Implicit("_downButton");

		public static readonly StringName _image = StringName.op_Implicit("_image");

		public static readonly StringName _imagePath = StringName.op_Implicit("_imagePath");

		public static readonly StringName _myVote = StringName.op_Implicit("_myVote");

		public static readonly StringName _reportButton = StringName.op_Implicit("_reportButton");

		public static readonly StringName _submissionId = StringName.op_Implicit("_submissionId");

		public static readonly StringName _up = StringName.op_Implicit("_up");

		public static readonly StringName _upButton = StringName.op_Implicit("_upButton");
	}

	public class SignalName : SignalName
	{
		public static readonly StringName CardClicked = StringName.op_Implicit("CardClicked");

		public static readonly StringName ScoreChanged = StringName.op_Implicit("ScoreChanged");
	}

	internal static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

	private static readonly (string reason, string label)[] ReportReasons = new(string, string)[5]
	{
		("ai", "AI-generated"),
		("stolen", "Stolen / copyright"),
		("inappropriate", "NSFW / inappropriate"),
		("offtopic", "Off-topic"),
		("other", "Other")
	};

	private static readonly Color UpColor = new Color(1f, 0.6f, 0.2f, 1f);

	private static readonly Color DownColor = new Color(0.3f, 0.55f, 1f, 1f);

	private readonly HashSet<string> _myFlags = new HashSet<string>();

	private Label _authorLabel;

	private Label _count;

	private int _down;

	private Button _downButton;

	private TextureRect _image;

	private string _imagePath = "";

	private int _myVote;

	private ArtEntry? _pending;

	private Button _reportButton;

	private long _submissionId;

	private int _up;

	private Button _upButton;

	private CardClickedEventHandler backing_CardClicked;

	private ScoreChangedEventHandler backing_ScoreChanged;

	public int Score => _up - _down;

	public event CardClickedEventHandler CardClicked
	{
		add
		{
			backing_CardClicked = (CardClickedEventHandler)Delegate.Combine(backing_CardClicked, value);
		}
		remove
		{
			backing_CardClicked = (CardClickedEventHandler)Delegate.Remove(backing_CardClicked, value);
		}
	}

	public event ScoreChangedEventHandler ScoreChanged
	{
		add
		{
			backing_ScoreChanged = (ScoreChangedEventHandler)Delegate.Combine(backing_ScoreChanged, value);
		}
		remove
		{
			backing_ScoreChanged = (ScoreChangedEventHandler)Delegate.Remove(backing_ScoreChanged, value);
		}
	}

	public override void _Ready()
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		_image = ((Node)this).GetNode<TextureRect>(NodePath.op_Implicit("MarginContainer/VBoxContainer/Image"));
		_authorLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("MarginContainer/VBoxContainer/AuthorLabel"));
		_upButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("MarginContainer/VBoxContainer/VoteRow/UpButton"));
		_count = ((Node)this).GetNode<Label>(NodePath.op_Implicit("MarginContainer/VBoxContainer/VoteRow/CountLabel"));
		_downButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("MarginContainer/VBoxContainer/VoteRow/DownButton"));
		_reportButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("MarginContainer/VBoxContainer/VoteRow/ReportButton"));
		((BaseButton)_upButton).Pressed += delegate
		{
			Vote(1);
		};
		((BaseButton)_downButton).Pressed += delegate
		{
			Vote(-1);
		};
		((BaseButton)_reportButton).Pressed += OpenReportPopup;
		((Control)_image).GuiInput += new GuiInputEventHandler(OnImageGuiInput);
		((Control)_image).MouseFilter = (MouseFilterEnum)0;
		if (_pending != null)
		{
			Apply(_pending);
		}
	}

	private void OnImageGuiInput(InputEvent e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I8
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		InputEventMouseButton val = (InputEventMouseButton)(object)((e is InputEventMouseButton) ? e : null);
		if (val != null && val.Pressed && (long)val.ButtonIndex == 1)
		{
			((GodotObject)this).EmitSignal(SignalName.CardClicked, (Variant[])(object)new Variant[1] { Variant.op_Implicit(_imagePath) });
		}
	}

	public void SetEntry(ArtEntry entry)
	{
		_pending = entry;
		if (((Node)this).IsNodeReady())
		{
			Apply(entry);
		}
	}

	private void Apply(ArtEntry entry)
	{
		_submissionId = entry.Id;
		_authorLabel.Text = entry.Author;
		_imagePath = entry.ImagePath;
		_up = entry.Upvotes;
		_down = entry.Downvotes;
		_myVote = entry.MyVote;
		_myFlags.Clear();
		foreach (string myFlag in entry.MyFlags)
		{
			_myFlags.Add(myFlag);
		}
		Refresh();
		UpdateVoteHighlight();
		UpdateReportHighlight();
		LoadImageAsync(entry.ImagePath);
	}

	private void Vote(int value)
	{
		int myVote = ((_myVote != value) ? value : 0);
		switch (_myVote)
		{
		case 1:
			_up--;
			break;
		case -1:
			_down--;
			break;
		}
		_myVote = myVote;
		switch (_myVote)
		{
		case 1:
			_up++;
			break;
		case -1:
			_down++;
			break;
		}
		Refresh();
		UpdateVoteHighlight();
		if (_myVote != 0)
		{
			VotingApi.Instance.CastVote(_submissionId, _myVote);
		}
		else
		{
			VotingApi.Instance.ClearVote(_submissionId);
		}
	}

	private void OpenReportPopup()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Expected O, but got Unknown
		HashSet<string> draft = new HashSet<string>(_myFlags);
		PopupPanel popup = new PopupPanel();
		VBoxContainer val = new VBoxContainer();
		((Node)popup).AddChild((Node)(object)val, false, (InternalMode)0);
		((Node)val).AddChild((Node)new Label
		{
			Text = "Report this submission:"
		}, false, (InternalMode)0);
		(string, string)[] reportReasons = ReportReasons;
		for (int i = 0; i < reportReasons.Length; i++)
		{
			(string, string) tuple = reportReasons[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			CheckBox val2 = new CheckBox
			{
				Text = item2,
				ButtonPressed = draft.Contains(item)
			};
			string r = item;
			((BaseButton)val2).Toggled += (ToggledEventHandler)delegate(bool on)
			{
				if (on)
				{
					draft.Add(r);
				}
				else
				{
					draft.Remove(r);
				}
			};
			((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		}
		Button val3 = new Button
		{
			Text = "Send report"
		};
		((BaseButton)val3).Pressed += delegate
		{
			SubmitReport(draft);
			((Window)popup).Hide();
		};
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)this).AddChild((Node)(object)popup, false, (InternalMode)0);
		((Window)popup).PopupCentered((Vector2I?)null);
		((Popup)popup).PopupHide += delegate
		{
			((Node)popup).QueueFree();
		};
	}

	private void SubmitReport(HashSet<string> draft)
	{
		foreach (string item in draft)
		{
			if (!_myFlags.Contains(item))
			{
				VotingApi.Instance.ToggleFlag(_submissionId, item, on: true);
			}
		}
		foreach (string myFlag in _myFlags)
		{
			if (!draft.Contains(myFlag))
			{
				VotingApi.Instance.ToggleFlag(_submissionId, myFlag, on: false);
			}
		}
		_myFlags.Clear();
		foreach (string item2 in draft)
		{
			_myFlags.Add(item2);
		}
		UpdateReportHighlight();
	}

	private void UpdateReportHighlight()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)_reportButton).Modulate = (Color)((_myFlags.Count > 0) ? new Color(1f, 0.6f, 0.3f, 1f) : Colors.White);
	}

	private void UpdateVoteHighlight()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)_upButton).Modulate = ((_myVote == 1) ? UpColor : Colors.White);
		((CanvasItem)_downButton).Modulate = ((_myVote == -1) ? DownColor : Colors.White);
	}

	private async Task LoadImageAsync(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		if (TextureCache.TryGetValue(path, out Texture2D value))
		{
			_image.Texture = value;
			return;
		}
		Texture2D val = await ResolveTexture(path);
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)this))
		{
			TextureCache[path] = val;
			if (_imagePath == path)
			{
				_image.Texture = val;
			}
		}
	}

	private async Task<Texture2D?> ResolveTexture(string path)
	{
		if (path.StartsWith("res://"))
		{
			return ResourceLoader.Exists(path, "") ? GD.Load<Texture2D>(path) : null;
		}
		if (path.StartsWith("http://") || path.StartsWith("https://"))
		{
			return await Download(path);
		}
		if (!FileAccess.FileExists(path))
		{
			return null;
		}
		Image val = new Image();
		return (Texture2D?)(object)(((int)val.Load(path) == 0) ? ImageTexture.CreateFromImage(val) : null);
	}

	private async Task<Texture2D?> Download(string url)
	{
		HttpRequest http = new HttpRequest();
		((Node)this).AddChild((Node)(object)http, false, (InternalMode)0);
		if ((int)http.Request(url, (string[])null, (Method)0, "") != 0)
		{
			((Node)http).QueueFree();
			return null;
		}
		Variant[] obj = await ((GodotObject)this).ToSignal((GodotObject)(object)http, SignalName.RequestCompleted);
		((Node)http).QueueFree();
		byte[] array = ((Variant)(ref obj[3])).AsByteArray();
		Image val = new Image();
		if ((int)val.LoadPngFromBuffer(array) != 0 && (int)val.LoadJpgFromBuffer(array) != 0 && (int)val.LoadWebpFromBuffer(array) != 0)
		{
			return null;
		}
		return (Texture2D?)(object)ImageTexture.CreateFromImage(val);
	}

	private void Refresh()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		_count.Text = (_up - _down).ToString();
		((GodotObject)this).EmitSignal(SignalName.ScoreChanged, Array.Empty<Variant>());
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(7)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnImageGuiInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("e"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Vote, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OpenReportPopup, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateReportHighlight, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateVoteHighlight, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Refresh, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnImageGuiInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnImageGuiInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Vote && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Vote(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OpenReportPopup && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OpenReportPopup();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateReportHighlight && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateReportHighlight();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateVoteHighlight && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateVoteHighlight();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Refresh && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Refresh();
			ret = default(godot_variant);
			return true;
		}
		return ((PanelContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.OnImageGuiInput)
		{
			return true;
		}
		if ((ref method) == MethodName.Vote)
		{
			return true;
		}
		if ((ref method) == MethodName.OpenReportPopup)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateReportHighlight)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateVoteHighlight)
		{
			return true;
		}
		if ((ref method) == MethodName.Refresh)
		{
			return true;
		}
		return ((PanelContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._authorLabel)
		{
			_authorLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._count)
		{
			_count = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._down)
		{
			_down = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._downButton)
		{
			_downButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._image)
		{
			_image = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._imagePath)
		{
			_imagePath = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._myVote)
		{
			_myVote = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._reportButton)
		{
			_reportButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._submissionId)
		{
			_submissionId = VariantUtils.ConvertTo<long>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._up)
		{
			_up = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._upButton)
		{
			_upButton = VariantUtils.ConvertTo<Button>(ref value);
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
		if ((ref name) == PropertyName.Score)
		{
			int score = Score;
			value = VariantUtils.CreateFrom<int>(ref score);
			return true;
		}
		if ((ref name) == PropertyName._authorLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _authorLabel);
			return true;
		}
		if ((ref name) == PropertyName._count)
		{
			value = VariantUtils.CreateFrom<Label>(ref _count);
			return true;
		}
		if ((ref name) == PropertyName._down)
		{
			value = VariantUtils.CreateFrom<int>(ref _down);
			return true;
		}
		if ((ref name) == PropertyName._downButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref _downButton);
			return true;
		}
		if ((ref name) == PropertyName._image)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _image);
			return true;
		}
		if ((ref name) == PropertyName._imagePath)
		{
			value = VariantUtils.CreateFrom<string>(ref _imagePath);
			return true;
		}
		if ((ref name) == PropertyName._myVote)
		{
			value = VariantUtils.CreateFrom<int>(ref _myVote);
			return true;
		}
		if ((ref name) == PropertyName._reportButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref _reportButton);
			return true;
		}
		if ((ref name) == PropertyName._submissionId)
		{
			value = VariantUtils.CreateFrom<long>(ref _submissionId);
			return true;
		}
		if ((ref name) == PropertyName._up)
		{
			value = VariantUtils.CreateFrom<int>(ref _up);
			return true;
		}
		if ((ref name) == PropertyName._upButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref _upButton);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._authorLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._count, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._down, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._downButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._image, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName._imagePath, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._myVote, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._reportButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._submissionId, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._up, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._upButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.Score, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._authorLabel, Variant.From<Label>(ref _authorLabel));
		info.AddProperty(PropertyName._count, Variant.From<Label>(ref _count));
		info.AddProperty(PropertyName._down, Variant.From<int>(ref _down));
		info.AddProperty(PropertyName._downButton, Variant.From<Button>(ref _downButton));
		info.AddProperty(PropertyName._image, Variant.From<TextureRect>(ref _image));
		info.AddProperty(PropertyName._imagePath, Variant.From<string>(ref _imagePath));
		info.AddProperty(PropertyName._myVote, Variant.From<int>(ref _myVote));
		info.AddProperty(PropertyName._reportButton, Variant.From<Button>(ref _reportButton));
		info.AddProperty(PropertyName._submissionId, Variant.From<long>(ref _submissionId));
		info.AddProperty(PropertyName._up, Variant.From<int>(ref _up));
		info.AddProperty(PropertyName._upButton, Variant.From<Button>(ref _upButton));
		info.AddSignalEventDelegate(SignalName.CardClicked, (Delegate)backing_CardClicked);
		info.AddSignalEventDelegate(SignalName.ScoreChanged, (Delegate)backing_ScoreChanged);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._authorLabel, ref val))
		{
			_authorLabel = ((Variant)(ref val)).As<Label>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._count, ref val2))
		{
			_count = ((Variant)(ref val2)).As<Label>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._down, ref val3))
		{
			_down = ((Variant)(ref val3)).As<int>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._downButton, ref val4))
		{
			_downButton = ((Variant)(ref val4)).As<Button>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._image, ref val5))
		{
			_image = ((Variant)(ref val5)).As<TextureRect>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._imagePath, ref val6))
		{
			_imagePath = ((Variant)(ref val6)).As<string>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._myVote, ref val7))
		{
			_myVote = ((Variant)(ref val7)).As<int>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._reportButton, ref val8))
		{
			_reportButton = ((Variant)(ref val8)).As<Button>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._submissionId, ref val9))
		{
			_submissionId = ((Variant)(ref val9)).As<long>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._up, ref val10))
		{
			_up = ((Variant)(ref val10)).As<int>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._upButton, ref val11))
		{
			_upButton = ((Variant)(ref val11)).As<Button>();
		}
		CardClickedEventHandler cardClickedEventHandler = default(CardClickedEventHandler);
		if (info.TryGetSignalEventDelegate<CardClickedEventHandler>(SignalName.CardClicked, ref cardClickedEventHandler))
		{
			backing_CardClicked = cardClickedEventHandler;
		}
		ScoreChangedEventHandler scoreChangedEventHandler = default(ScoreChangedEventHandler);
		if (info.TryGetSignalEventDelegate<ScoreChangedEventHandler>(SignalName.ScoreChanged, ref scoreChangedEventHandler))
		{
			backing_ScoreChanged = scoreChangedEventHandler;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotSignalList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(SignalName.CardClicked, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("imagePath"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(SignalName.ScoreChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	protected void EmitSignalCardClicked(string imagePath)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).EmitSignal(SignalName.CardClicked, (Variant[])(object)new Variant[1] { Variant.op_Implicit(imagePath) });
	}

	protected void EmitSignalScoreChanged()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).EmitSignal(SignalName.ScoreChanged, Array.Empty<Variant>());
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RaiseGodotClassSignalCallbacks(in godot_string_name signal, NativeVariantPtrArgs args)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if ((ref signal) == SignalName.CardClicked && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			backing_CardClicked?.Invoke(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
		}
		else if ((ref signal) == SignalName.ScoreChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			backing_ScoreChanged?.Invoke();
		}
		else
		{
			((GodotObject)this).RaiseGodotClassSignalCallbacks(ref signal, args);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassSignal(in godot_string_name signal)
	{
		if ((ref signal) == SignalName.CardClicked)
		{
			return true;
		}
		if ((ref signal) == SignalName.ScoreChanged)
		{
			return true;
		}
		return ((PanelContainer)this).HasGodotClassSignal(ref signal);
	}
}

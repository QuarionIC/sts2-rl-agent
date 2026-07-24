using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Voting;

[ScriptPath("res://DownfallCode/Voting/NArtVotingContainer.cs")]
public class NArtVotingContainer : Control
{
	[Signal]
	public delegate void EntryClickedEventHandler(string imagePath);

	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SortByScore = StringName.op_Implicit("SortByScore");

		public static readonly StringName ClearCards = StringName.op_Implicit("ClearCards");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _content = StringName.op_Implicit("_content");

		public static readonly StringName _fillGeneration = StringName.op_Implicit("_fillGeneration");

		public static readonly StringName _title = StringName.op_Implicit("_title");

		public static readonly StringName _voteCardScene = StringName.op_Implicit("_voteCardScene");
	}

	public class SignalName : SignalName
	{
		public static readonly StringName EntryClicked = StringName.op_Implicit("EntryClicked");
	}

	private const string VoteCardScenePath = "res://Downfall/scenes/voting/art_row.tscn";

	private readonly Dictionary<string, List<ArtEntry>> _entryCache = new Dictionary<string, List<ArtEntry>>();

	private HFlowContainer _content;

	private int _fillGeneration;

	private MegaRichTextLabel _title;

	private PackedScene _voteCardScene;

	private EntryClickedEventHandler backing_EntryClicked;

	public event EntryClickedEventHandler EntryClicked
	{
		add
		{
			backing_EntryClicked = (EntryClickedEventHandler)Delegate.Combine(backing_EntryClicked, value);
		}
		remove
		{
			backing_EntryClicked = (EntryClickedEventHandler)Delegate.Remove(backing_EntryClicked, value);
		}
	}

	public override void _Ready()
	{
		_title = ((Node)this).GetNode<MegaRichTextLabel>(NodePath.op_Implicit("SubmissionsTitle"));
		_content = ((Node)this).GetNode<HFlowContainer>(NodePath.op_Implicit("SubmissionsScrollContainer/Mask/Content"));
		_voteCardScene = PreloadManager.Cache.GetScene("res://Downfall/scenes/voting/art_row.tscn");
		ClearCards();
	}

	public async void Fill(ArtData artData)
	{
		int gen = ++_fillGeneration;
		ClearCards();
		CardModel card = artData.Card;
		if (card == null)
		{
			GD.PrintErr($"No card model for {artData.ModelId} — skipping");
			_title.Text = "?";
			return;
		}
		_title.Text = card.Title;
		List<ArtEntry> list = await GetEntriesFor(artData);
		if (gen != _fillGeneration || list == null)
		{
			return;
		}
		foreach (ArtEntry item in list)
		{
			NVoteCard nVoteCard = _voteCardScene.Instantiate<NVoteCard>((GenEditState)0);
			((Node)_content).AddChild((Node)(object)nVoteCard, false, (InternalMode)0);
			nVoteCard.SetEntry(item);
			nVoteCard.ScoreChanged += SortByScore;
			nVoteCard.CardClicked += delegate(string path)
			{
				//IL_000f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_0019: Unknown result type (might be due to invalid IL or missing references)
				((GodotObject)this).EmitSignal(SignalName.EntryClicked, (Variant[])(object)new Variant[1] { Variant.op_Implicit(path) });
			};
		}
		SortByScore();
	}

	private void SortByScore()
	{
		List<NVoteCard> list = (from c in ((IEnumerable)((Node)_content).GetChildren(false)).OfType<NVoteCard>()
			orderby c.Score descending
			select c).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			((Node)_content).MoveChild((Node)(object)list[num], num);
		}
	}

	private async Task<List<ArtEntry>?> GetEntriesFor(ArtData artData)
	{
		List<ArtEntry> entries = artData.Entries;
		if (entries != null && entries.Count > 0)
		{
			return artData.Entries;
		}
		string key = artData.Id ?? ((object)artData.ModelId).ToString();
		if (_entryCache.TryGetValue(key, out List<ArtEntry> value))
		{
			return value;
		}
		List<ArtEntry> list = await FetchFromDatabase(key);
		if (list == null)
		{
			return list;
		}
		_entryCache[key] = list;
		artData.Entries = list;
		return list;
	}

	private async Task<List<ArtEntry>?> FetchFromDatabase(string categoryId)
	{
		return await VotingApi.Instance.GetSubmissions(categoryId);
	}

	private void ClearCards()
	{
		foreach (Node child in ((Node)_content).GetChildren(false))
		{
			((Node)_content).RemoveChild(child);
			child.QueueFree();
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SortByScore, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ClearCards, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SortByScore && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SortByScore();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ClearCards && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ClearCards();
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.SortByScore)
		{
			return true;
		}
		if ((ref method) == MethodName.ClearCards)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._content)
		{
			_content = VariantUtils.ConvertTo<HFlowContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fillGeneration)
		{
			_fillGeneration = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._title)
		{
			_title = VariantUtils.ConvertTo<MegaRichTextLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._voteCardScene)
		{
			_voteCardScene = VariantUtils.ConvertTo<PackedScene>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._content)
		{
			value = VariantUtils.CreateFrom<HFlowContainer>(ref _content);
			return true;
		}
		if ((ref name) == PropertyName._fillGeneration)
		{
			value = VariantUtils.CreateFrom<int>(ref _fillGeneration);
			return true;
		}
		if ((ref name) == PropertyName._title)
		{
			value = VariantUtils.CreateFrom<MegaRichTextLabel>(ref _title);
			return true;
		}
		if ((ref name) == PropertyName._voteCardScene)
		{
			value = VariantUtils.CreateFrom<PackedScene>(ref _voteCardScene);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._content, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._fillGeneration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._title, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._voteCardScene, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._content, Variant.From<HFlowContainer>(ref _content));
		info.AddProperty(PropertyName._fillGeneration, Variant.From<int>(ref _fillGeneration));
		info.AddProperty(PropertyName._title, Variant.From<MegaRichTextLabel>(ref _title));
		info.AddProperty(PropertyName._voteCardScene, Variant.From<PackedScene>(ref _voteCardScene));
		info.AddSignalEventDelegate(SignalName.EntryClicked, (Delegate)backing_EntryClicked);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._content, ref val))
		{
			_content = ((Variant)(ref val)).As<HFlowContainer>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._fillGeneration, ref val2))
		{
			_fillGeneration = ((Variant)(ref val2)).As<int>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._title, ref val3))
		{
			_title = ((Variant)(ref val3)).As<MegaRichTextLabel>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._voteCardScene, ref val4))
		{
			_voteCardScene = ((Variant)(ref val4)).As<PackedScene>();
		}
		EntryClickedEventHandler entryClickedEventHandler = default(EntryClickedEventHandler);
		if (info.TryGetSignalEventDelegate<EntryClickedEventHandler>(SignalName.EntryClicked, ref entryClickedEventHandler))
		{
			backing_EntryClicked = entryClickedEventHandler;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotSignalList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(SignalName.EntryClicked, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("imagePath"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	protected void EmitSignalEntryClicked(string imagePath)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).EmitSignal(SignalName.EntryClicked, (Variant[])(object)new Variant[1] { Variant.op_Implicit(imagePath) });
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RaiseGodotClassSignalCallbacks(in godot_string_name signal, NativeVariantPtrArgs args)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if ((ref signal) == SignalName.EntryClicked && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			backing_EntryClicked?.Invoke(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
		}
		else
		{
			((GodotObject)this).RaiseGodotClassSignalCallbacks(ref signal, args);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassSignal(in godot_string_name signal)
	{
		if ((ref signal) == SignalName.EntryClicked)
		{
			return true;
		}
		return ((Control)this).HasGodotClassSignal(ref signal);
	}
}

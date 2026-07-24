using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Voting;

[ScriptPath("res://DownfallCode/Voting/NArtVotingCardContainer.cs")]
public class NArtVotingCardContainer : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName UpdateImage = StringName.op_Implicit("UpdateImage");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _card = StringName.op_Implicit("_card");

		public static readonly StringName _currentImagePath = StringName.op_Implicit("_currentImagePath");
	}

	public class SignalName : SignalName
	{
	}

	private NCard? _card;

	private string _currentImagePath = "";

	public async void Fill(ArtData artData)
	{
		if (_card != null)
		{
			((Node)this).RemoveChild((Node)(object)_card);
			((Node)_card).QueueFree();
			_card = null;
		}
		_currentImagePath = "";
		CardModel card = artData.Card;
		if (card == null)
		{
			return;
		}
		_card = NCard.Create(card, (ModelVisibility)1);
		if (_card != null)
		{
			((Node)this).AddChild((Node)(object)_card, false, (InternalMode)0);
			((Control)_card).Position = NCard.defaultSize / 2f + new Vector2(100f, 100f);
			if (!((Node)_card).IsNodeReady())
			{
				await ((GodotObject)this).ToSignal((GodotObject)(object)_card, SignalName.Ready);
			}
			_card.UpdateVisuals((PileType)6, (CardPreviewMode)1);
		}
	}

	public async void UpdateImage(string imagePath)
	{
		if (_card != null && !string.IsNullOrEmpty(imagePath) && !(imagePath == _currentImagePath))
		{
			_currentImagePath = imagePath;
			Texture2D val = await LoadTexture(imagePath);
			if (val != null && _card != null && !(_currentImagePath != imagePath))
			{
				((Node)_card).GetNode<TextureRect>(NodePath.op_Implicit("%Portrait")).Texture = val;
			}
		}
	}

	private async Task<Texture2D?> LoadTexture(string path)
	{
		if (NVoteCard.TextureCache.TryGetValue(path, out Texture2D value))
		{
			return value;
		}
		Texture2D val;
		if (path.StartsWith("res://"))
		{
			val = (ResourceLoader.Exists(path, "") ? GD.Load<Texture2D>(path) : null);
		}
		else if (path.StartsWith("http://") || path.StartsWith("https://"))
		{
			val = await Download(path);
		}
		else if (FileAccess.FileExists(path))
		{
			Image val2 = new Image();
			val = (Texture2D)(object)(((int)val2.Load(path) == 0) ? ImageTexture.CreateFromImage(val2) : null);
		}
		else
		{
			val = null;
		}
		if (val != null)
		{
			NVoteCard.TextureCache[path] = val;
		}
		return val;
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

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName.UpdateImage, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("imagePath"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.UpdateImage && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateImage(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.UpdateImage)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._card)
		{
			_card = VariantUtils.ConvertTo<NCard>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentImagePath)
		{
			_currentImagePath = VariantUtils.ConvertTo<string>(ref value);
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
		if ((ref name) == PropertyName._card)
		{
			value = VariantUtils.CreateFrom<NCard>(ref _card);
			return true;
		}
		if ((ref name) == PropertyName._currentImagePath)
		{
			value = VariantUtils.CreateFrom<string>(ref _currentImagePath);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._card, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName._currentImagePath, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._card, Variant.From<NCard>(ref _card));
		info.AddProperty(PropertyName._currentImagePath, Variant.From<string>(ref _currentImagePath));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._card, ref val))
		{
			_card = ((Variant)(ref val)).As<NCard>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentImagePath, ref val2))
		{
			_currentImagePath = ((Variant)(ref val2)).As<string>();
		}
	}
}

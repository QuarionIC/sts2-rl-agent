using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using Downfall.DownfallCode.Localization;
using Downfall.DownfallCode.Patches;
using Downfall.DownfallCode.Utils;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Guardian.GuardianCode.Cards;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using Guardian.GuardianCode.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Guardian.GuardianCode;

[ModInitializer("Initialize")]
[ScriptPath("res://GuardianCode/GuardianMainFile.cs")]
public class GuardianMainFile : Node
{
	public class MethodName : MethodName
	{
		public static readonly StringName Initialize = StringName.op_Implicit("Initialize");

		public static readonly StringName RegisterGemSave = StringName.op_Implicit("RegisterGemSave");
	}

	public class PropertyName : PropertyName
	{
	}

	public class SignalName : SignalName
	{
	}

	public const string ModId = "Guardian";

	public static Logger Logger { get; } = new Logger("Guardian", (LogType)0);

	public static void Initialize()
	{
		CustomLocTableManager.Register("gems");
		RegisterGemSave();
		CardDescriptionRegistry.Register<GuardianCardModel>(DescriptionInjectionPoint.BelowMainText, (IExtraDescriptionSource)new GemDescriptionSource());
		BundledSubmodLocRegistry.Register("Guardian");
		TranscendenceHooks.OnTransformed += CopyGemsToTranscendence;
		CombatUiHooks.Register(GuardianCombatModel.SetupGuardianCombatUi);
	}

	private static void CopyGemsToTranscendence(CardModel starter, CardModel result)
	{
		if (starter is IGemSocketCard gemSocketCard && result is IGemSocketCard gemSocketCard2 && gemSocketCard.Gems.Count != 0)
		{
			List<GemModel> gems = (from gem in gemSocketCard.Gems.Take(gemSocketCard2.GemSlots)
				select gem.CreateClone()).ToList();
			gemSocketCard2.AddGems(gems);
		}
	}

	private static void RegisterGemSave()
	{
		ExtendedSaveTypes.RegisterListSaveType<ModelId>();
		ExtendedSaveHandlers<CardModel, SerializableCard>.RegisterSave<List<ModelId>>("GuardianGems", (Func<CardModel, List<ModelId>>)delegate(CardModel card)
		{
			List<GemModel> list = CardModifier.DirectModifiers(card).OfType<GemModel>().ToList();
			return (list.Count <= 0) ? null : list.Select((GemModel g) => ((AbstractModel)g).Id).ToList();
		}, (Action<CardModel, List<ModelId>>)delegate(CardModel card, List<ModelId>? gemIds)
		{
			if (gemIds == null)
			{
				return;
			}
			HashSet<ModelId> hashSet = (from g in CardModifier.DirectModifiers(card).OfType<GemModel>()
				select ((AbstractModel)g).Id).ToHashSet();
			foreach (ModelId gemId in gemIds)
			{
				if (!hashSet.Contains(gemId) && ModelDb.GetById<CardModifier>(gemId) is GemModel gemModel)
				{
					GemModel gemModel2 = gemModel.ToMutable();
					CardModifier.AddModifier(card, (CardModifier)(object)gemModel2);
				}
			}
		}, (Action<List<ModelId>, PacketWriter>)delegate(List<ModelId> gemIds, PacketWriter writer)
		{
			if (gemIds == null)
			{
				writer.WriteInt(0, 32);
				return;
			}
			writer.WriteInt(gemIds.Count, 32);
			foreach (ModelId gemId2 in gemIds)
			{
				PacketWriterExtensions.WriteModelEntry(writer, gemId2);
			}
		}, (Func<PacketReader, List<ModelId>>)delegate(PacketReader reader)
		{
			int num = reader.ReadInt(32);
			if (num <= 0)
			{
				return (List<ModelId>)null;
			}
			List<ModelId> list = new List<ModelId>(num);
			for (int i = 0; i < num; i++)
			{
				list.Add(PacketReaderExtensions.ReadModelIdAssumingType<CardModifier>(reader));
			}
			return list;
		});
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName.Initialize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RegisterGemSave, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Initialize && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Initialize();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RegisterGemSave && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RegisterGemSave();
			ret = default(godot_variant);
			return true;
		}
		return ((Node)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Initialize && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Initialize();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RegisterGemSave && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RegisterGemSave();
			ret = default(godot_variant);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Initialize)
		{
			return true;
		}
		if ((ref method) == MethodName.RegisterGemSave)
		{
			return true;
		}
		return ((Node)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
	}
}

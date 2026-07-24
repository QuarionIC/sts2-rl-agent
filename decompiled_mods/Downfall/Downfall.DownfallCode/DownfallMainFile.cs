using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BaseLib.Config;
using BaseLib.Patches.Features;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Config;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Localization;
using Downfall.DownfallCode.Nodes;
using Downfall.DownfallCode.Patches;
using Downfall.DownfallCode.Utils;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Downfall.DownfallCode;

[ModInitializer("Initialize")]
[ScriptPath("res://DownfallCode/DownfallMainFile.cs")]
public class DownfallMainFile : Node
{
	public class MethodName : MethodName
	{
		public static readonly StringName Initialize = StringName.op_Implicit("Initialize");

		public static readonly StringName LogRegisteredCounts = StringName.op_Implicit("LogRegisteredCounts");
	}

	public class PropertyName : PropertyName
	{
	}

	public class SignalName : SignalName
	{
	}

	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static MetricsUploadHook _003C0_003E__OnMetricsUpload;
	}

	public const string ModId = "Downfall";

	public static Logger Logger { get; } = new Logger("Downfall", (LogType)0);

	public static void Initialize()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		CustomLocTableManager.Register("card_modifiers");
		CustomLocTableManager.Register("artists");
		ExtendedSaveTypes.RegisterListSaveType<SerializableCard>();
		ModConfigRegistry.Register("Downfall", (ModConfig)(object)new DownfallConfig());
		ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
		DownfallPatchManager.HarmonyPatches();
		NCustomCardHolder.InitPool();
		object obj = _003C_003EO._003C0_003E__OnMetricsUpload;
		if (obj == null)
		{
			MetricsUploadHook val = OnMetricsUpload;
			_003C_003EO._003C0_003E__OnMetricsUpload = val;
			obj = (object)val;
		}
		ModManager.OnMetricsUpload += (MetricsUploadHook)obj;
		CardTitleHooks.Register(delegate(CardModel card, string title)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			if (!card.IsEcho())
			{
				return title;
			}
			LocString val2 = new LocString("card_keywords", "DOWNFALL-ECHO.card_title");
			val2.Add("card", title);
			return val2.GetFormattedText();
		});
		PostInitRegistry.Register(delegate
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			CustomTargetType.RegisterMultiTargetType(DownfallTargetType.MeAndEnemies, (Func<Creature, Player, bool>)((Creature target, Player player) => (target != null && target.IsAlive && !target.IsPet && target.IsEnemy) || target == player.Creature));
			LogRegisteredCounts();
			CustomPowerInstanceType.RegisterAll();
		});
		LocFormatterRegistry.Register(new PowerIconFormatter(), new PreviewPluralFormatter(), new PreviewValueFormatter(), new PlusIfUpgradedFormatter());
	}

	private static void OnMetricsUpload(SerializableRun run, bool isVictory, ulong localPlayerId)
	{
		if (DownfallConfig.UploadMetrics && !run.Players.All((SerializablePlayer e) => e.CharacterId == (ModelId)null || !(ModelDb.GetById<CharacterModel>(e.CharacterId) is DownfallCharacterModel)))
		{
			SendToServer(JsonSerializer.Serialize<SerializableRun>(run.Anonymized()));
		}
	}

	private static async Task SendToServer(string json)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		using HttpClient client = new HttpClient();
		client.Timeout = TimeSpan.FromSeconds(15L);
		ByteArrayContent byteArrayContent = new ByteArrayContent(bytes);
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		try
		{
			HttpResponseMessage httpResponseMessage = await client.PutAsync("http://localhost:3000/runs", byteArrayContent);
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				Logger.Info("Upload successful!", 1);
				return;
			}
			Logger.Warn($"Upload failed: {httpResponseMessage.StatusCode}", 1);
		}
		catch (HttpRequestException ex)
		{
			Logger.Warn("Upload failed due to network error: " + ex.Message, 1);
		}
		catch (TaskCanceledException ex2)
		{
			Logger.Warn("Upload timed out: " + ex2.Message, 1);
		}
	}

	private static void LogRegisteredCounts()
	{
		Assembly modAssembly = typeof(DownfallMainFile).Assembly;
		foreach (CharacterModel character in from c in ModelDb.AllCharacters.Where((CharacterModel c) => ((object)c).GetType().Assembly == modAssembly).ToList()
			orderby ((AbstractModel)c).Id.Entry
			select c)
		{
			string name = ((object)character).GetType().Name;
			int value = ModelDb.AllCards.Count((CardModel c) => c.Pool == character.CardPool);
			int value2 = ModelDb.AllRelics.Count((RelicModel r) => r.Pool == character.RelicPool);
			int value3 = ModelDb.AllPotions.Count((PotionModel p) => p.Pool == character.PotionPool);
			Logger.Info($"{name}: {value} cards, {value2} relics, {value3} potions", 1);
		}
		int value4 = ModelDb.AllPowers.Count((PowerModel p) => ((object)p).GetType().Assembly == modAssembly);
		Logger.Info($"Powers: {value4}", 1);
		foreach (DownfallCharacterModel item in ModelDb.AllCharacters.OfType<DownfallCharacterModel>())
		{
			ModSoundEffect characterSelectSfxEntry = item.CharacterSelectSfxEntry;
			if (characterSelectSfxEntry != null)
			{
				SfxOverrideRegistry.Register(((CharacterModel)item).CharacterSelectSfx, characterSelectSfxEntry);
			}
		}
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
			new MethodInfo(MethodName.LogRegisteredCounts, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null)
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
		if ((ref method) == MethodName.LogRegisteredCounts && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			LogRegisteredCounts();
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
		if ((ref method) == MethodName.LogRegisteredCounts && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			LogRegisteredCounts();
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
		if ((ref method) == MethodName.LogRegisteredCounts)
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

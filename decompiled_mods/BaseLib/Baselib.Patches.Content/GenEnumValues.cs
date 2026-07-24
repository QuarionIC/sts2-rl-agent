using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Extensions;
using BaseLib.Patches.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(ModelDb), "Init")]
internal class GenEnumValues
{
	[HarmonyPrefix]
	private static void FindAndGenerate()
	{
		//IL_0582: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Invalid comparison between Unknown and I4
		//IL_05d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d7: Invalid comparison between Unknown and I4
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_060d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		Type[] modTypes = ReflectionHelper.ModTypes;
		BaseLibMain.Logger.Info($"Starting custom enum generation for {modTypes.Length} modded types", 1);
		List<FieldInfo> list = new List<FieldInfo>();
		Type[] array = modTypes;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (FieldInfo item in from field in array[i].GetFields()
				where Attribute.IsDefined(field, typeof(CustomEnumAttribute))
				select field)
			{
				if (!item.FieldType.IsEnum)
				{
					throw new Exception($"Field {item.DeclaringType?.FullName}.{item.Name} should be an enum type for CustomEnum");
				}
				if (!item.IsStatic)
				{
					throw new Exception($"Field {item.DeclaringType?.FullName}.{item.Name} should be static for CustomEnum");
				}
				if (!(item.DeclaringType == null))
				{
					list.Add(item);
				}
			}
		}
		list.Sort(delegate(FieldInfo a, FieldInfo b)
		{
			int num = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
			return (num == 0) ? string.Compare(a.DeclaringType?.Name, b.DeclaringType?.Name, StringComparison.Ordinal) : num;
		});
		foreach (FieldInfo item2 in list)
		{
			CustomEnumAttribute customAttribute = item2.GetCustomAttribute<CustomEnumAttribute>();
			object obj = CustomEnums.GenerateKey(item2);
			Type declaringType = item2.DeclaringType;
			if (declaringType == null)
			{
				continue;
			}
			BaseLibMain.Logger.Debug($"Generated value {Convert.ChangeType(obj, TypeCode.Int64)} for field {item2.Name} of enum {declaringType.FullName}", 1);
			item2.SetValue(null, obj);
			if (!CustomEnums.GeneratedCustomEnumEntries.TryGetValue(item2.FieldType, out Dictionary<int, (string, string)> value))
			{
				value = (CustomEnums.GeneratedCustomEnumEntries[item2.FieldType] = new Dictionary<int, (string, string)>());
			}
			value.Add((int)obj, (declaringType.GetPrefix(), item2.Name));
			if (item2.FieldType == typeof(CardKeyword))
			{
				string key = declaringType.GetPrefix() + (customAttribute?.Name ?? item2.Name).ToUpperInvariant();
				KeywordPropertiesAttribute customAttribute2 = item2.GetCustomAttribute<KeywordPropertiesAttribute>();
				AutoKeywordPosition autoKeywordPosition = customAttribute2?.Position ?? AutoKeywordPosition.None;
				switch (autoKeywordPosition)
				{
				case AutoKeywordPosition.Before:
					AutoKeywordText.AdditionalBeforeKeywords.Add((CardKeyword)obj);
					break;
				case AutoKeywordPosition.After:
					AutoKeywordText.AdditionalAfterKeywords.Add((CardKeyword)obj);
					break;
				}
				CustomKeywords.KeywordIDs.Add((int)obj, new CustomKeywords.KeywordInfo(key)
				{
					AutoPosition = autoKeywordPosition,
					RichKeyword = (customAttribute2?.RichKeyword ?? true)
				});
				continue;
			}
			if (item2.FieldType == typeof(PileType) && declaringType.IsAssignableTo(typeof(CustomPile)))
			{
				ConstructorInfo constructor = declaringType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Array.Empty<Type>()) ?? throw new Exception("CustomPile " + declaringType.FullName + " with custom PileType does not have an accessible no-parameter constructor");
				PileType? val = (PileType?)item2.GetValue(null);
				if (!val.HasValue)
				{
					throw new Exception("Failed to be set up custom PileType in " + declaringType.FullName);
				}
				CustomPiles.RegisterCustomPile(val.Value, () => (CustomPile)constructor.Invoke(null));
				CustomPile customPile = (CustomPile)constructor.Invoke(null);
				if (customPile != null)
				{
					string iconPath = customPile.IconPath;
					if (iconPath != null)
					{
						LocString name = customPile.Name;
						if (name != null)
						{
							MultiPileCardSelect.RegisterPileIndicator(val.Value, iconPath, name);
						}
					}
				}
			}
			if (!(item2.FieldType == typeof(RewardType)) || !declaringType.IsAssignableTo(typeof(CustomReward)))
			{
				continue;
			}
			if (!(AccessToolsExtensions.CreateInstance(declaringType) is CustomReward customReward))
			{
				BaseLibMain.Logger.Error($"Reward instance creation for type {declaringType} from {declaringType.Assembly} failed during Initialize", 1);
				continue;
			}
			BaseLibMain.Logger.Debug($"Initializing CustomReward inheriting class {((object)customReward).GetType()}", 1);
			SerializableReward val2 = ((Reward)customReward).ToSerializable();
			if ((int)val2.RewardType == 0)
			{
				throw new InvalidOperationException($"CustomReward {((object)customReward).GetType()}'s RewardType is None, or doesn't set RewardType in it's ToSerializable override");
			}
			RewardType rewardType = val2.RewardType;
			if ((int)rewardType >= 0 && (int)rewardType <= 6)
			{
				throw new InvalidOperationException($"$CustomReward {((object)customReward).GetType()}'s RewardType is basegame type {val2.RewardType} rather than a custom type");
			}
			customReward.Initialize();
		}
	}
}

using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(DustyTome), "SetupForPlayer")]
internal class DustyTomePatch
{
	private static bool _initialized = false;

	private static readonly Dictionary<ModelId, List<ModelId>> _customTome = new Dictionary<ModelId, List<ModelId>>();

	private static Dictionary<ModelId, List<ModelId>> CustomTome
	{
		get
		{
			if (_initialized)
			{
				return _customTome;
			}
			_initialized = true;
			int num = 0;
			foreach (CardModel allCard in ModelDb.AllCards)
			{
				if (allCard is ITomeCard tomeCard)
				{
					if (!_customTome.TryGetValue(((AbstractModel)tomeCard.TomeCharacter).Id, out List<ModelId> value))
					{
						value = new List<ModelId>();
						_customTome[((AbstractModel)tomeCard.TomeCharacter).Id] = value;
					}
					value.Add(((AbstractModel)allCard).Id);
					num++;
				}
			}
			BaseLibMain.Logger.Info($"Initialized DustyTome dictionary; found {num} ITomeCard implementations", 1);
			return _customTome;
		}
	}

	[HarmonyPrefix]
	private static bool DustyTomeCardOverride(DustyTome __instance, Player player)
	{
		if (CustomTome.TryGetValue(((AbstractModel)player.Character).Id, out List<ModelId> value))
		{
			__instance.AncientCard = player.PlayerRng.Rewards.NextItem<ModelId>((IEnumerable<ModelId>)value);
			return false;
		}
		return true;
	}
}

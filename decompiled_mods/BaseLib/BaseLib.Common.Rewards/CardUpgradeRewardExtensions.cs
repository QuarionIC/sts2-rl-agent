using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace BaseLib.Common.Rewards;

internal static class CardUpgradeRewardExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_002417FE65D7FCD1FE422A3D4E29C1FE8F01
	{
		[SpecialName]
		public static class _003CM_003E_0024A0C79FC3C0AC9B189888D2E608DEB92A
		{
		}

		[ExtensionMarker("<M>$A0C79FC3C0AC9B189888D2E608DEB92A")]
		public Task<bool> DoCardUpgrade(Player player, int amount = 1)
		{
			throw null;
		}
	}

	public static async Task<bool> DoCardUpgrade(this RewardSynchronizer rewardSynchronizer, Player player, int amount = 1)
	{
		CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, amount);
		((CardSelectorPrefs)(ref val)).set_Cancelable(true);
		((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
		CardSelectorPrefs val2 = val;
		List<CardModel> list = (await CardSelectCmd.FromDeckForUpgrade(player, val2)).ToList();
		CardCmd.Upgrade((IEnumerable<CardModel>)list, (CardPreviewStyle)4);
		return list.Count > 0;
	}
}

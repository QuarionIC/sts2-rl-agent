using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace BaseLib.Common.Rewards;

internal static class TransformRewardSynchronizerPatches
{
	[SpecialName]
	public sealed class _003CG_003E_002417FE65D7FCD1FE422A3D4E29C1FE8F01
	{
		[SpecialName]
		public static class _003CM_003E_0024A0C79FC3C0AC9B189888D2E608DEB92A
		{
		}

		[ExtensionMarker("<M>$A0C79FC3C0AC9B189888D2E608DEB92A")]
		public Task<bool> DoUnsyncedCardTransform(Player player, int amount = 1, bool upgrade = false)
		{
			throw null;
		}
	}

	public static async Task<bool> DoUnsyncedCardTransform(this RewardSynchronizer rewardSynchronizer, Player player, int amount = 1, bool upgrade = false)
	{
		LocString val = (LocString)(upgrade ? ((object)CardSelectorPrefsExtensions.TransformAndUpgradeSelectionPrompt) : ((object)CardSelectorPrefs.TransformSelectionPrompt));
		CardSelectorPrefs val2 = new CardSelectorPrefs(val, 1, amount);
		((CardSelectorPrefs)(ref val2)).set_Cancelable(true);
		((CardSelectorPrefs)(ref val2)).set_RequireManualConfirmation(true);
		CardSelectorPrefs val3 = val2;
		List<CardModel> cards = (await CardSelectCmd.FromDeckForTransformation(player, val3, (Func<CardModel, CardTransformation>)null)).ToList();
		BaseLibMain.Logger.Debug($"Current combat state for transform rewards is: IsEnding={CombatManager.Instance.IsEnding}", 1);
		foreach (CardModel card in cards)
		{
			CardModel newCard = CardFactory.CreateRandomCardForTransform(card, false, player.RunState.Rng.Niche);
			if (upgrade)
			{
				CardCmd.Upgrade(newCard, (CardPreviewStyle)1);
			}
			await CardCmd.Transform(card, newCard, (CardPreviewStyle)4);
			BaseLibMain.Logger.Debug($"Player {player.NetId} transformed {((AbstractModel)card).Id} in their deck into {((AbstractModel)newCard).Id}" + (upgrade ? " and upgraded it." : "."), 1);
		}
		return cards.Count > 0;
	}
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Nodes;
using Guardian.GuardianCode.Cards.Abstract;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace Guardian.GuardianCode.RestSiteOptions;

public class GemRestSiteOption : CustomRestSiteOption
{
	public const string Id = "DOWNFALL-GEM";

	private CardModel? _gem;

	private CardModel? _gemHolder;

	public override string OptionId => "DOWNFALL-GEM";

	public override string CustomIconPath => "rest_site_option_gem.png".RestSitePath<Guardian.GuardianCode.Core.Guardian>();

	public override bool IsEnabled
	{
		get
		{
			if (((RestSiteOption)this).Owner.GetDeck().Any((CardModel c) => c is IGemCard))
			{
				return ((RestSiteOption)this).Owner.GetDeck().Any((CardModel c) => c is IGemSocketCard gemSocketCard && gemSocketCard.FreeSlots > 0);
			}
			return false;
		}
	}

	public GemRestSiteOption(Player owner)
		: base(owner)
	{
	}

	public override async Task<bool> OnSelect()
	{
		if (!((RestSiteOption)this).IsEnabled)
		{
			return false;
		}
		CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
		((CardSelectorPrefs)(ref val)).set_Cancelable(true);
		((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(false);
		CardSelectorPrefs prefs = val;
		uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(((RestSiteOption)this).Owner);
		List<CardModel> list;
		if (CardSelectCmd.ShouldSelectLocalCard(((RestSiteOption)this).Owner))
		{
			IReadOnlyList<CardModel> deck = ((RestSiteOption)this).Owner.GetDeck((CardModel c) => c is IGemCard);
			IReadOnlyList<CardModel> deck2 = ((RestSiteOption)this).Owner.GetDeck((CardModel c) => c is IGemSocketCard gemSocketCard && gemSocketCard.FreeSlots > 0);
			if (NOverlayStack.Instance == null)
			{
				return false;
			}
			list = (await NGemUpgradeSelectScreen.ShowScreen(deck, deck2, prefs).CardsSelected()).ToList();
			RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(((RestSiteOption)this).Owner, choiceId, PlayerChoiceResult.FromMutableDeckCards((IEnumerable<CardModel>)list));
		}
		else
		{
			list = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(((RestSiteOption)this).Owner, choiceId)).AsDeckCards().ToList();
		}
		CardSelectCmd.LogChoice(((RestSiteOption)this).Owner, (IEnumerable<CardModel>)list);
		if (list.Count != 2)
		{
			return false;
		}
		_gem = list.First();
		_gemHolder = list.Last();
		if (_gem == null || _gemHolder == null)
		{
			return false;
		}
		await GuardianCmd.PutGemIn(_gem, _gemHolder);
		NRestSiteRoom instance = NRestSiteRoom.Instance;
		NRestSiteButton val2 = ((instance != null) ? instance.GetButtonForOption((RestSiteOption)(object)this) : null);
		if (val2 == null)
		{
			return false;
		}
		val2.Reload();
		val2._isUnclickable = !((RestSiteOption)this).IsEnabled;
		return false;
	}
}

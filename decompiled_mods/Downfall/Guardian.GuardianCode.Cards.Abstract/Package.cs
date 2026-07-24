using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Cards.Abstract;

public abstract class Package<T1, T2, T3> : GuardianCardModel, IPackageCard, IModfyCardDescription where T1 : CardModel where T2 : CardModel where T3 : CardModel
{
	protected Package(int cost)
		: base(cost, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithUpgradingCardTip<T1>((Action<T1, CardModel>)null);
		((ConstructedCardModel)this).WithUpgradingCardTip<T2>((Action<T2, CardModel>)null);
		((ConstructedCardModel)this).WithUpgradingCardTip<T3>((Action<T3, CardModel>)null);
	}

	public LocString ModifyDescription(LocString locString)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return new LocString("cards", "GUARDIAN-PACKAGE.description");
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		CardModel val = ((CardModel)ModelDb.Card<T1>()).ToMutable();
		CardModel val2 = ((CardModel)ModelDb.Card<T2>()).ToMutable();
		CardModel val3 = ((CardModel)ModelDb.Card<T3>()).ToMutable();
		if (((CardModel)this).IsUpgraded)
		{
			val.UpgradeInternal();
			val2.UpgradeInternal();
			val3.UpgradeInternal();
		}
		description.Add("card1", val.Title);
		description.Add("card2", val2.Title);
		description.Add("card3", val3.Title);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await DownfallCardCmd.GiveCard<T1>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1);
		await DownfallCardCmd.GiveCard<T2>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1);
		await DownfallCardCmd.GiveCard<T3>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1);
	}
}

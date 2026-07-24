using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Hermit.HermitCode.Cards.Basic;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hermit.HermitCode.Cards.Common;

public sealed class HighCaliber : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public HighCaliber()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithUpgradingCardTip<StrikeHermit>((Action<StrikeHermit, CardModel>)WithModifiers);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun1();
			return Task.CompletedTask;
		})
			.Execute(ctx);
		await DownfallCardCmd.GiveCard<StrikeHermit>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<StrikeHermit>?)delegate(StrikeHermit card)
		{
			WithModifiers(card, (CardModel)(object)this);
		}, (Player?)null);
	}

	private static void WithModifiers(StrikeHermit strike, CardModel card)
	{
		DownfallCardCmd.ForceUpgrade((CardModel)(object)strike, 2);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		int num = ((CardModel)this).CurrentUpgradeLevel + 2;
		if (num <= 1)
		{
			switch (num)
			{
			case 0:
				description.Add("UpgradeAmount", "");
				break;
			case 1:
				description.Add("UpgradeAmount", "+");
				break;
			}
		}
		else
		{
			description.Add("UpgradeAmount", $"+{num}");
		}
	}
}

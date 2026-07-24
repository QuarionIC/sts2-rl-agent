using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Cards.Ancient;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Cards.Basic;

[Pool(typeof(GuardianCardPool))]
public class TwinSlam : GuardianCardModel, ITranscendenceCard, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public int GemSlots
	{
		get
		{
			if (!((CardModel)this).IsUpgraded)
			{
				return 1;
			}
			return 2;
		}
	}

	public TwinSlam()
		: base(1, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 0);
		((ConstructedCardModel)this).WithUpgradingCardTip<SecondSlam>((Action<SecondSlam, CardModel>)Action);
	}

	public CardModel GetTranscendenceTransformedCard()
	{
		return (CardModel)(object)ModelDb.Card<BaubleBurst>();
	}

	private static void Action(SecondSlam secondSlam, CardModel card)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		if (card is IGemSocketCard gemSocketCard)
		{
			((IGemSocketCard)secondSlam)?.AddGems(gemSocketCard.Gems.Select((GemModel e) => e.CreateClone()));
		}
		if (card.Enchantment != null)
		{
			EnchantmentModel val = (EnchantmentModel)((AbstractModel)card.Enchantment).MutableClone();
			CardCmd.Enchant(val, (CardModel)(object)secondSlam, (decimal)val.Amount);
			NCard obj = NCard.FindOnTable((CardModel)(object)secondSlam, (PileType?)null);
			if (obj != null)
			{
				obj.ReloadOverlay();
			}
		}
	}

	private void Action(SecondSlam secondSlam)
	{
		Action(secondSlam, (CardModel)(object)this);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		NCard obj = NCard.FindOnTable((CardModel)(object)(await DownfallCardCmd.GiveCard<SecondSlam>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<SecondSlam>?)Action, (Player?)null)), (PileType?)null);
		if (obj != null)
		{
			obj.ReloadOverlay();
		}
	}
}

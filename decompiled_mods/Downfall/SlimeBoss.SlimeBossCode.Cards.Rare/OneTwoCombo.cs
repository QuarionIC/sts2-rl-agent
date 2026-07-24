using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.Extensions;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class OneTwoCombo : SlimeBossCardModel, IAfterSplit
{
	public OneTwoCombo()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(4, 3);
		((ConstructedCardModel)(object)this).WithCommand(1m);
	}

	public async Task AfterSplit(Player player, SlimeModel slime)
	{
		if (player == ((CardModel)this).Owner)
		{
			CardPile pile = ((CardModel)this).Pile;
			if (pile != null && (int)pile.Type == 3)
			{
				await CardPileCmd.Add((CardModel)(object)this, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await SlimeBossCmd.Command(ctx, (CardModel)(object)this, (ValueProp)8);
	}
}

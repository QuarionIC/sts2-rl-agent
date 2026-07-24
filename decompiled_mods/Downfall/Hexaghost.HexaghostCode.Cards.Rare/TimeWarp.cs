using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class TimeWarp : HexaghostCardModel, IWheelMoved
{
	public TimeWarp()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HexaghostKeyword.Advance));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HexaghostKeyword.Retract));
	}

	public async Task AfterWheelAdvance(PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		if (((CardModel)this).Pile != null && player == ((CardModel)this).Owner && (int)((CardModel)this).Pile.Type == 3)
		{
			await CardPileCmd.Add((CardModel)(object)this, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}

	public async Task AfterWheelRetract(PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		if (((CardModel)this).Pile != null && player == ((CardModel)this).Owner && (int)((CardModel)this).Pile.Type == 3)
		{
			await CardPileCmd.Add((CardModel)(object)this, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}

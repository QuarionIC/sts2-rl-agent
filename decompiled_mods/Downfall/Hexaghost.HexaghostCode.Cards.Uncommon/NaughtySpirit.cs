using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class NaughtySpirit : HexaghostCardModel, IModifyCardPlayResultLocation
{
	public NaughtySpirit()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithPower<SoulBurnPower>(3, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HexaghostKeyword.Retract));
	}

	public CardLocationCompatiblity ModifyCardPlayResultLocationCompability(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocationCompatiblity cardLocation)
	{
		if ((object)this != card || !HexaghostCmd.IsIgnited(card.Owner))
		{
			return cardLocation;
		}
		return new CardLocationCompatiblity(card.Owner, (PileType)2, (CardPilePosition)1);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if ((int)cardPlay.ResultPile == 2 && (object)this == cardPlay.Card)
		{
			await HexaghostCmd.Retract(ctx, ((CardModel)this).Owner, (AbstractModel?)(object)this);
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<SoulBurnPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}

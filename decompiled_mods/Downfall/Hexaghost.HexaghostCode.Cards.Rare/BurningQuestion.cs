using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class BurningQuestion : HexaghostCardModel
{
	public override bool CanBeGeneratedInCombat => false;

	public BurningQuestion()
		: base(3, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<IntensityPower>(3, 1);
		((ConstructedCardModel)this).WithPower<MetallicizePower>(6, 2);
		((ConstructedCardModel)(object)this).WithPower<RoyaltiesPower>(30, 5, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel[] array = (CardModel[])(object)new CardModel[3]
		{
			(CardModel)BurningQuestionChoice1.Create(((CardModel)this).Owner),
			(CardModel)BurningQuestionChoice2.Create(((CardModel)this).Owner),
			(CardModel)BurningQuestionChoice3.Create(((CardModel)this).Owner)
		};
		if (((CardModel)this).IsUpgraded)
		{
			CardModel[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].UpgradeInternal();
			}
		}
		if (await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)array, ((CardModel)this).Owner, false) is BurningQuestionChoiceBase burningQuestionChoiceBase)
		{
			await burningQuestionChoiceBase.OnSelect(ctx, cardPlay);
		}
	}
}

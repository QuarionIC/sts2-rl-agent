using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Cards.Abstract;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class CompilePackage : GuardianCardModel
{
	public CompilePackage()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Package));
		((ConstructedCardModel)this).WithCards(3, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (GuardianCmd.CanPutIntoStasis(((CardModel)this).Owner) && ((CardModel)this).CombatState != null)
		{
			List<CardModel> list = IEnumerableExtensions.TakeRandom<IPackageCard>(((CardPoolModel)ModelDb.CardPool<TokenCardPool>()).AllCards.OfType<IPackageCard>(), ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).CombatState.RunState.Rng.CombatCardGeneration).Cast<CardModel>().Select(Select)
				.ToList();
			CardModel card = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, false);
			if (card != null)
			{
				await CardPileCmd.AddGeneratedCardToCombat(card, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
				await GuardianCmd.PutIntoStasis(card, ctx, (AbstractModel)(object)this);
			}
		}
	}

	private CardModel Select(CardModel cardModel)
	{
		if (((CardModel)this).RunState == null)
		{
			throw new InvalidOperationException();
		}
		CardModel val = ((CardModel)this).CombatState.CreateCard(cardModel, ((CardModel)this).Owner);
		if (((CardModel)this).IsUpgraded)
		{
			CardCmd.Upgrade(val, (CardPreviewStyle)1);
		}
		return val;
	}
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class ComboTackle : SlimeBossCardModel
{
	public ComboTackle()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithDamage(12, 7);
		((ConstructedCardModel)(object)this).WithSelfDamage(3);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)SlimeBossTag.Tackle });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await MyCommonActions.SelfDamage(ctx, (AbstractModel)(object)this);
		IEnumerable<CardModel> enumerable = from e in ((CardModel)this).Pool.GetUnlockedCards(((CardModel)this).Owner.UnlockState, ((CardModel)this).Owner.RunState.CardMultiplayerConstraint)
			where e.Tags.Contains(SlimeBossTag.Tackle)
			select e;
		CardModel val = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, enumerable, 1, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
		if (val != null)
		{
			val.SetToFreeThisTurn();
			await CardPileCmd.AddGeneratedCardToCombat(val, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Cards.Affliction;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public class TornPage : HermitCardModel
{
	public TornPage()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(9, 3);
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel _) => HoverTipFactory.FromAffliction<Necromantic>(1)));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		IEnumerable<CardModel> enumerable = from e in ((CardModel)this).Owner.GetHand()
			where (int)e.Type == 5
			select e;
		foreach (CardModel item in enumerable)
		{
			await CardCmd.Afflict<Necromantic>(item, 1m);
		}
	}
}

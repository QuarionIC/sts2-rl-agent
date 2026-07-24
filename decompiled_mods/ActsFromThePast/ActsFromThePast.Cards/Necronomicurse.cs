using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Necronomicurse : CustomCardModel
{
	public override bool CanBeGeneratedByModifiers => false;

	public override int MaxUpgradeLevel => 0;

	public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[2]
	{
		(CardKeyword)4,
		(CardKeyword)7
	};

	public Necronomicurse()
		: base(-1, (CardType)5, (CardRarity)9, (TargetType)0, true, true)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if ((object)card == this)
		{
			RelicModel necronomicon = ((IEnumerable<RelicModel>)((CardModel)this).Owner.Relics).FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => r is Necronomicon));
			if (necronomicon != null)
			{
				necronomicon.Flash();
			}
			await CardPileCmd.Add((CardModel)(object)this, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}

	public override void AfterTransformedTo()
	{
		AFTPModAudio.Play("relics", "necronomicon");
	}
}

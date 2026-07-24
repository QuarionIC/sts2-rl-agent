using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Affliction;

public class Necromantic : DownfallAfflictionModel<Hermit.HermitCode.Core.Hermit>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromKeyword((CardKeyword)1));

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (card == ((AfflictionModel)this).Card)
		{
			await CardPileCmd.Add(card, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}

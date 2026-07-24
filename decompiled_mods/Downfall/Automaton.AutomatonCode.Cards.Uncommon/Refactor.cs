using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Refactor : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Refactor()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(4, 2);
		((ConstructedCardModel)(object)this).WithScry(4);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		ScryResult val = await ScryCmd.Execute(ctx, (CardModel)(object)this);
		List<CardModel> statuses = ((ScryResult)(ref val)).Discarded.Where((CardModel c) => (int)c.Type == 4).ToList();
		foreach (CardModel item in statuses)
		{
			await CardCmd.Exhaust(ctx, item, false, false);
		}
		if (statuses.Count > 0)
		{
			await CreatureCmd.GainBlock(((CardModel)this).Owner.Creature, (decimal)(((DynamicVar)((CardModel)this).DynamicVars.Block).IntValue * statuses.Count), ((CardModel)this).DynamicVars.Block.Props, cardPlay, false);
		}
	}
}

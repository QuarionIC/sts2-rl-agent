using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Common;

public class Spectre : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<DawnablesAwakened>();

	public Spectre()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)2));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		if (((CardModel)this).CombatState != null)
		{
			CardModel val = (CardModel)((!((CardModel)this).IsUpgraded) ? ((object)((CardModel)this).CombatState.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)((CardModel)this).Owner.GetHand((CardModel e) => (object)e != this))) : ((object)(await DownfallCardCmd.SelectFromHand(ctx, DownfallCardSelectorPrefs.AddEtherealSelectionPrompt, (CardModel)(object)this)).FirstOrDefault()));
			if (val != null)
			{
				val.AddKeyword((CardKeyword)2);
			}
		}
	}
}

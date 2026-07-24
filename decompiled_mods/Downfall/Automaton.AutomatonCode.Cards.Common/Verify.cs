using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Common;

[Pool(typeof(AutomatonCardPool))]
public class Verify : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Verify()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Stash));
		((ConstructedCardModel)this).WithBlock(6, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		CardModel val = (await CardSelectCmd.FromCombatPile(ctx, PileTypeExtensions.GetPile((PileType)3, ((CardModel)this).Owner), ((CardModel)this).Owner, new CardSelectorPrefs(StashCmd.StashSelectionPrompt, 1))).FirstOrDefault();
		if (val != null)
		{
			await StashCmd.Stash(val);
		}
	}
}

using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Return : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public Return()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithEnergy(1, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel val = (await DownfallCardCmd.SelectFromCards(ctx, ((CardModel)this).Owner.GetDiscard(), DownfallCardSelectorPrefs.ToTopSelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
		if (val != null)
		{
			await CardPileCmd.Add(val, (PileType)1, (CardPilePosition)2, (AbstractModel)null, false);
		}
		await CommonActions.ApplySelf<EnergyNextTurnPower>(ctx, (CardModel)(object)this, ((DynamicVar)((CardModel)this).DynamicVars.Energy).BaseValue, false);
	}
}

using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class Assembly : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public Assembly()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCards(4, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Encode));
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		foreach (CardModel item in (await CommonActions.Draw((CardModel)(object)this, ctx)).Where(AutomatonCmd.IsEncodable))
		{
			await AutomatonCmd.EncodeCard(item, ctx);
		}
	}
}

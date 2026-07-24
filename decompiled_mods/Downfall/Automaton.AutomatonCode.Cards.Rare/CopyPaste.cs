using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class CopyPaste : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public CopyPaste()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
		((ConstructedCardModel)(object)this).WithPower<CopyPastePower>(1, showTooltip: false);
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<CopyPastePower>(ctx, (CardModel)(object)this, false);
	}
}

using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class Carrionmaker : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Carrionmaker()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)4)
	{
		((ConstructedCardModel)this).WithDamage(9, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry s) => ((CombatHistoryEntry)s).HappenedThisTurn(((CardModel)this).CombatState) && s.CardPlay.Card is ISpell);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1 + num, (string)null, (string)null, (string)null).Execute(ctx);
	}
}

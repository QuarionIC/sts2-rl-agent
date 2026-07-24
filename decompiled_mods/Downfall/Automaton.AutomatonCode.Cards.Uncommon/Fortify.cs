using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Fortify : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Hermitfan69>();

	public Fortify()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(8, 3);
		((ConstructedCardModel)this).WithBlock(8, 3);
		WithStash(2, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
		await StashCmd.StashFromHand((CardModel)(object)this, ctx);
	}
}

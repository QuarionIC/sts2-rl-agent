using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Multiplayer;

[Pool(typeof(ChampCardPool))]
public class Huddle : ChampCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public Huddle()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)7)
	{
		((ConstructedCardModel)this).WithPower<VigorPower>(6, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		foreach (Creature item in from c in ((CardModel)this).CombatState.GetTeammatesOf(((CardModel)this).Owner.Creature)
			where c != null && c.IsAlive && c.IsPlayer
			select c)
		{
			await CommonActions.Apply<VigorPower>(ctx, item, (CardModel)(object)this, false);
		}
	}
}

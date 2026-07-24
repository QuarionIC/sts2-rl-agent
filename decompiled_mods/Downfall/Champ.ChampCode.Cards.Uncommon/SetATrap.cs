using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Interfaces;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class SetATrap : ChampCardModel, IDefensiveComboCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public SetATrap()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithBlock(8, 2);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 1);
	}

	public async Task DefensiveComboEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			await CommonActions.Apply<WeakPower>(ctx, (IEnumerable<Creature>)((CardModel)this).CombatState.HittableEnemies, DynamicVarSource.op_Implicit((CardModel)(object)this), false);
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}

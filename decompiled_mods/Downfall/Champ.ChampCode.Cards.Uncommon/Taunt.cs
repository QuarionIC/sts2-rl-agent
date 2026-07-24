using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class Taunt : ChampCardModel
{
	public override TargetType TargetType
	{
		get
		{
			if (((CardModel)this).IsUpgraded)
			{
				return (TargetType)3;
			}
			return (TargetType)2;
		}
	}

	public Taunt()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<VulnerablePower>(1, 0);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).IsUpgraded)
		{
			if (((CardModel)this).CombatState != null)
			{
				IReadOnlyList<Creature> enemies = ((CardModel)this).CombatState.HittableEnemies;
				await CommonActions.Apply<VulnerablePower>(ctx, (IEnumerable<Creature>)enemies, DynamicVarSource.op_Implicit((CardModel)(object)this), false);
				await CommonActions.Apply<WeakPower>(ctx, (IEnumerable<Creature>)enemies, DynamicVarSource.op_Implicit((CardModel)(object)this), false);
			}
		}
		else if (cardPlay.Target != null)
		{
			await CommonActions.Apply<VulnerablePower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
			await CommonActions.Apply<WeakPower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
		}
	}
}

using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class Nihil : AwakenedCardModel, IChantable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public bool HasChanted { get; set; }

	public Nihil()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<ManaburnPower>(13, 4);
	}

	public async Task PlayChantEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		foreach (Creature enemy in ((CardModel)this).CombatState.Enemies)
		{
			int powerAmount = enemy.GetPowerAmount<ManaburnPower>();
			if (powerAmount > 0)
			{
				await DownfallCreatureCmd.Damage(ctx, enemy, powerAmount, (ValueProp)6, (CardModel)(object)this, cardPlay);
			}
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<ManaburnPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}

using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Virtue : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Virtue()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)0);
		((ConstructedCardModel)this).WithVar("Reduce", 1, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		int reduceBy = ((CardModel)this).DynamicVars["Reduce"].IntValue;
		foreach (PowerModel item in (from p in ((CardModel)this).Owner.Creature.Powers.Where(Exclude)
			where (int)p.StackType == 1
			where (int)p.TypeForCurrentAmount == 2
			select p).ToList())
		{
			int num = ((item.Amount > 0) ? (-Math.Min(reduceBy, item.Amount)) : Math.Min(reduceBy, Math.Abs(item.Amount)));
			await PowerCmd.ModifyAmount(ctx, item, (decimal)num, ((CardModel)this).Owner.Creature, (CardModel)null, false);
		}
	}

	private static bool Exclude(PowerModel powerModel)
	{
		bool flag = ((powerModel is ChainsOfBindingPower || powerModel is ConfusedPower) ? true : false);
		return !flag;
	}
}

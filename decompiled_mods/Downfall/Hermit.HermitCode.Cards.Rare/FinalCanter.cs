using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class FinalCanter : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public FinalCanter()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(10, 3);
		((ConstructedCardModel)this).WithCalculatedVar("CalculatedHits", 0, (Func<CardModel, Creature, decimal>)CountCursesInHand, 0, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	private static decimal CountCursesInHand(CardModel card, Creature? _)
	{
		return card.Owner.GetHand().Count((CardModel c) => (int)c.Type == 5);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		int num = (int)((CalculatedVar)((CardModel)this).DynamicVars["CalculatedHits"]).Calculate(play.Target);
		if (num > 0)
		{
			await CommonActions.CardAttack((CardModel)(object)this, play, num, (string)null, (string)null, (string)null).WithHermitFireHitFx().Execute(ctx);
		}
	}
}

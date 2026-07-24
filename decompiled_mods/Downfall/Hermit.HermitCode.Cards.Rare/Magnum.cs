using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class Magnum : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Magnum()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 2);
		((ConstructedCardModel)this).WithCards(6, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		int count = ((CardModel)this).Owner.GetHand().Count;
		int num = Math.Min(((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, count);
		if (num == 0)
		{
			return;
		}
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.DiscardSelectionPrompt, num, num);
		List<CardModel> selected = (await CardSelectCmd.FromHandForDiscard(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).ToList();
		if (selected.Count != 0)
		{
			await CardCmd.Discard(ctx, (IEnumerable<CardModel>)selected);
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
			await CommonActions.CardAttack((CardModel)(object)this, play, selected.Count, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
			{
				HermitSfx.PlayGun1();
				return Task.CompletedTask;
			})
				.Execute(ctx);
		}
	}
}

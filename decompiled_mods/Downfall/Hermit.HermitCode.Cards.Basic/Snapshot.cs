using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.Cards.Ancient;
using Hermit.HermitCode.Powers;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Cards.Basic;

public sealed class Snapshot : HermitCardModel, IHasDeadOnEffect, ITranscendenceCard
{
	private AttackCommand? _result;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public override bool GainsBlock => true;

	public Snapshot()
		: base(1, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 3);
	}

	public async Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay play)
	{
		if (_result != null)
		{
			int unblockedDamage = _result.Results.SelectMany((List<DamageResult> e) => e).Sum((DamageResult e) => e.TotalDamage);
			int hasSnipe = ((!((CardModel)this).Owner.Creature.HasPower<SnipePower>()) ? 1 : 2);
			for (int i = 0; i < hasSnipe; i++)
			{
				await CreatureCmd.GainBlock(((CardModel)this).Owner.Creature, (decimal)unblockedDamage, (ValueProp)8, play, false);
			}
			_result = null;
		}
	}

	public CardModel GetTranscendenceTransformedCard()
	{
		return (CardModel)(object)ModelDb.Card<Crackshot>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		_result = await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun1();
			return Task.CompletedTask;
		})
			.Execute(ctx);
	}
}

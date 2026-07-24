using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Powers;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Cards.Common;

public class Headshot : HermitCardModel, IHasDeadOnEffect, IModifyDamageMultiplicative
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Headshot()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 2);
	}

	public Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	public decimal ModifyDamageMultiplicativeCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (this == null)
		{
			return 1m;
		}
		if ((object)cardSource != this || dealer != ((CardModel)this).Owner.Creature || !ValuePropExtensions.IsPoweredAttack(props) || !((IHasDeadOnEffect)this).IsDeadOn)
		{
			return 1m;
		}
		return ((CardModel)this).Owner.Creature.HasPower<SnipePower>() ? 4 : 2;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		HermitSfx.PlayGun2();
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().Execute(ctx);
	}
}

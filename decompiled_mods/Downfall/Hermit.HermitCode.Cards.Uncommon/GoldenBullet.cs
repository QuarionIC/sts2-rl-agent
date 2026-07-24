using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class GoldenBullet : HermitCardModel
{
	private int _currentCost = 3;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	[SavedProperty]
	public int CurrentCost
	{
		get
		{
			return _currentCost;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_currentCost = value;
			((CardModel)this).EnergyCost.SetCustomBaseCost(_currentCost);
		}
	}

	public GoldenBullet()
		: base(3, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(18, 6);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)6));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		bool shouldTriggerFatal = play.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
		AttackCommand val = await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun1();
			return Task.CompletedTask;
		})
			.Execute(ctx);
		if (shouldTriggerFatal && val.Results.SelectMany((List<DamageResult> r) => r).Any((DamageResult r) => r.WasTargetKilled))
		{
			BuffFromPlay();
			(((CardModel)this).DeckVersion as GoldenBullet)?.BuffFromPlay();
		}
	}

	protected override void AfterDowngraded()
	{
		UpdateCost();
	}

	private void BuffFromPlay()
	{
		CurrentCost = Math.Max(0, CurrentCost - 1);
		UpdateCost();
	}

	private void UpdateCost()
	{
		((CardModel)this).EnergyCost.SetCustomBaseCost(CurrentCost);
	}
}

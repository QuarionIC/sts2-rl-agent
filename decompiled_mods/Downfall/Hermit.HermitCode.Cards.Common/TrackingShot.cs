using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Hermit.HermitCode.Cards.Common;

public sealed class TrackingShot : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public TrackingShot()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)(object)this).WithRepeat(2);
		((ConstructedCardModel)this).WithKeyword(HermitKeywords.Concentrate, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 2, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			if (Rng.Chaotic.NextInt(2) == 0)
			{
				HermitSfx.PlayGun3();
			}
			else
			{
				HermitSfx.PlayGun1();
			}
			return Task.CompletedTask;
		})
			.Execute(ctx);
	}
}

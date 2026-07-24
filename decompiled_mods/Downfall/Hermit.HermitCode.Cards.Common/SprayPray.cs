using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hermit.HermitCode.Cards.Common;

public sealed class SprayPray : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public SprayPray()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)4)
	{
		((ConstructedCardModel)this).WithDamage(5, 1);
		((ConstructedCardModel)(object)this).WithRepeat(3);
		((ConstructedCardModel)(object)this).WithTip<Doubt>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun3();
			return Task.CompletedTask;
		})
			.Execute(ctx);
		await DownfallCardCmd.GiveCard<Doubt>(((CardModel)this).Owner, (PileType)1, (CardPilePosition)3, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Doubt>?)null, (Player?)null);
	}
}

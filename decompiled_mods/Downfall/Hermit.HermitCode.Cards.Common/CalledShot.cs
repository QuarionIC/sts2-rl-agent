using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.History;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Common;

public sealed class CalledShot : HermitCardModel
{
	private const int DrawAmount = 1;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	protected override bool ShouldGlowGoldInternal => LastPlayTriggeredDeadOn();

	public CalledShot()
		: base(0, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(5, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HermitKeywords.DeadOn));
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)1);
	}

	private bool LastPlayTriggeredDeadOn()
	{
		CardPlay val = (from e in CombatManager.Instance.History.Entries.OfType<DeadOnEntry>()
			where ((CombatHistoryEntry)e).HappenedThisTurn(((CardModel)this).CombatState) && ((CombatHistoryEntry)e).Actor == ((CardModel)this).Owner.Creature
			select e.CardPlay).LastOrDefault();
		CardPlay val2 = (from e in CombatManager.Instance.History.CardPlaysFinished
			where ((CombatHistoryEntry)e).HappenedThisTurn(((CardModel)this).CombatState) && ((CombatHistoryEntry)e).Actor == ((CardModel)this).Owner.Creature
			select e.CardPlay).LastOrDefault();
		if (val2 != null && val != null)
		{
			return val2 == val;
		}
		return false;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitGunHitFx().BeforeDamage((Func<Task>)delegate
		{
			HermitSfx.PlayGun2();
			return Task.CompletedTask;
		})
			.Execute(ctx);
		if (LastPlayTriggeredDeadOn())
		{
			await CardPileCmd.Draw(ctx, 1m, ((CardModel)this).Owner, false);
		}
	}
}

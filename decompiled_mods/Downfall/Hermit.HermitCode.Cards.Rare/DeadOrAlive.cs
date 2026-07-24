using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class DeadOrAlive : HermitCardModel
{
	private const int MonsterGoldAmount = 15;

	private const int EliteGoldAmount = 40;

	private const int BossGoldAmount = 100;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	protected override bool HasEnergyCostX => true;

	public override bool CanBeGeneratedInCombat => false;

	public DeadOrAlive()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(8, 3);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HermitKeywords.Bounty));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)6));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		int times = ((CardModel)this).ResolveEnergyXValue();
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		AttackCommand obj = await CommonActions.CardAttack((CardModel)(object)this, play, times, (string)null, (string)null, (string)null).WithHermitBluntLightHitFx().Execute(ctx);
		bool flag = play.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
		if (obj.Results.SelectMany((List<DamageResult> e) => e).Any((DamageResult e) => e.WasTargetKilled) && flag)
		{
			ICombatState combatState = ((CardModel)this).Owner.Creature.CombatState;
			AbstractRoom obj2 = ((combatState != null) ? combatState.RunState.CurrentRoom : null);
			await PlayerCmd.GainGold((decimal)(((obj2 != null) ? new RoomType?(obj2.RoomType) : ((RoomType?)null)) switch
			{
				(RoomType)0L => 15, 
				(RoomType)1L => 40, 
				(RoomType)2L => 100, 
				_ => 15, 
			}), ((CardModel)this).Owner, false);
		}
	}
}

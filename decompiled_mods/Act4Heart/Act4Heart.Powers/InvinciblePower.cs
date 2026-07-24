using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dolso;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Act4Heart.Powers;

internal class InvinciblePower : A4hPowerModel
{
	private sealed class Data
	{
		private readonly Dictionary<Creature, decimal>? damage_recieved_this_turn_split = (split ? new Dictionary<Creature, decimal>() : null);

		private decimal damage_recieved_this_turn_shared;

		internal ref decimal this[Creature? dealer]
		{
			get
			{
				if (damage_recieved_this_turn_split == null)
				{
					return ref damage_recieved_this_turn_shared;
				}
				if (dealer == null)
				{
					dealer = MainPlayer();
				}
				else
				{
					if (dealer.IsPet)
					{
						dealer = dealer.PetOwner.Creature;
					}
					if (!dealer.IsPlayer)
					{
						log.warning("InvinciblePower" + $": dealer '{dealer}' is not a player");
						dealer = MainPlayer();
					}
				}
				bool exists;
				return ref CollectionsMarshal.GetValueRefOrAddDefault(damage_recieved_this_turn_split, dealer, out exists);
			}
		}

		public Data(bool split)
		{
		}

		internal void Reset()
		{
			if (damage_recieved_this_turn_split != null)
			{
				damage_recieved_this_turn_split.Clear();
			}
			else
			{
				damage_recieved_this_turn_shared = default(decimal);
			}
		}

		private static Creature MainPlayer()
		{
			return RunManager.Instance.State.Players[0].Creature;
		}
	}

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public override bool ShouldScaleInMultiplayer => false;

	public override int DisplayAmount => (int)Math.Max(0m, (decimal)((PowerModel)this).Amount - data[LocalContext.GetMe(((PowerModel)this).CombatState).Creature]);

	internal int null_amount => (int)Math.Max(0m, (decimal)((PowerModel)this).Amount - data[null]);

	private Data data => ((PowerModel)this).GetInternalData<Data>();

	public override object InitInternalData()
	{
		return new Data(RunManager.Instance.State.Players.Count > 1 && ModMain.current_config.multiplayer_heart_split_invincible_pool);
	}

	public override decimal ModifyHpLostAfterOstyLate(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card_source)
	{
		if (target != ((PowerModel)this).Owner || amount == 0m)
		{
			return amount;
		}
		return Math.Min(amount, (decimal)((PowerModel)this).Amount - data[dealer]);
	}

	public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? card_source)
	{
		if (target == ((PowerModel)this).Owner && !result.WasFullyBlocked)
		{
			ref decimal reference = ref data[dealer];
			reference += (decimal)result.UnblockedDamage;
			((PowerModel)this).InvokeDisplayAmountChanged();
			if (reference >= (decimal)((PowerModel)this).Amount)
			{
				((PowerModel)this).Flash();
				((PowerModel)this).Owner.HpDisplay = (HpDisplay)1;
			}
		}
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			data.Reset();
			((PowerModel)this).InvokeDisplayAmountChanged();
			((PowerModel)this).Owner.HpDisplay = (HpDisplay)0;
		}
		return Task.CompletedTask;
	}
}

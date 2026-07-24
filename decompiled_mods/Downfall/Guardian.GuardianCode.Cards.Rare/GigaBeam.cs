using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Godot;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class GigaBeam : GuardianCardModel, IModifyDamageAdditive
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public GigaBeam()
		: base(3, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(36, 4);
		((ConstructedCardModel)this).WithVar("StrengthEffect", 2, 2);
		((ConstructedCardModel)(object)this).WithPower<NextTurnStunnedPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
	}

	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		if ((object)cardSource == this && ValuePropExtensions.IsPoweredAttack(props))
		{
			return (((dealer != null) ? new int?(dealer.GetPowerAmount<StrengthPower>()) : ((int?)null)) * (((CardModel)this).DynamicVars["StrengthEffect"].IntValue - 1)).GetValueOrDefault();
		}
		return 0m;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState != null)
		{
			await BetaMainCompatibility.FromCardCompatibility(DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue), (CardModel)(object)this, cardPlay).TargetingAllOpponents(((CardModel)this).CombatState).WithAttackerAnim("Cast", 0.5f, (Creature)null)
				.BeforeDamage((Func<Task>)BeforeDamageAction)
				.Execute(ctx);
			await CommonActions.ApplySelf<NextTurnStunnedPower>(ctx, (CardModel)(object)this, false);
		}
	}

	private async Task BeforeDamageAction()
	{
		ICombatState combatState = ((CardModel)this).CombatState;
		List<Creature> enemies = ((combatState != null) ? combatState.Enemies.Where((Creature e) => e.IsAlive).ToList() : null);
		if (enemies != null)
		{
			NHyperbeamVfx val = NHyperbeamVfx.Create(((CardModel)this).Owner.Creature, enemies.Last());
			if (val != null)
			{
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)val);
				}
			}
		}
		await Cmd.Wait(0.5f, false);
		if (enemies == null)
		{
			return;
		}
		foreach (NHyperbeamImpactVfx item in enemies.Select((Creature enemy) => NHyperbeamImpactVfx.Create(((CardModel)this).Owner.Creature, enemy)).OfType<NHyperbeamImpactVfx>())
		{
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)item);
			}
		}
	}
}

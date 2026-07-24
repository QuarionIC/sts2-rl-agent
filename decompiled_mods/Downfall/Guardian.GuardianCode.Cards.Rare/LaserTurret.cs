using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class LaserTurret : GuardianCardModel, ITickCard, ICustomTickDuration
{
	public int TickDuration => 4;

	public LaserTurret()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(5, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Tick));
	}

	public async Task OnTick(PlayerChoiceContext ctx)
	{
		ICombatState combatState = ((CardModel)this).CombatState;
		Creature val = ((combatState != null) ? combatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((CardModel)this).CombatState.HittableEnemies) : null);
		if (val != null)
		{
			await CreatureCmd.Damage(ctx, val, ((CardModel)this).DynamicVars.Damage, ((CardModel)this).Owner.Creature);
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await GuardianCmd.PutIntoStasis((CardModel)(object)this, ctx, (AbstractModel)(object)this);
	}
}

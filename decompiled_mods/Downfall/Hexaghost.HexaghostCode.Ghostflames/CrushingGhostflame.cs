using System.Collections.Generic;
using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using Hexaghost.HexaghostCode.Ghostflames.Intents;
using Hexaghost.HexaghostCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Ghostflames;

public class CrushingGhostflame : GhostflameModel
{
	public override AbstractIntent Intent => (AbstractIntent)(object)new CustomAttackIntent(() => 3 + base.Intensity, () => 2 * (1 + Repeat(GhostflameRepeatType.Damage)));

	protected override int IgnitionRequirement => 2;

	public override FireColor FireColor => FireColor.Pink;

	public override async Task OnIgnite(PlayerChoiceContext ctx)
	{
		if (base.Owner.Creature.CombatState == null)
		{
			return;
		}
		SfxCmd.Play("event:/sfx/characters/attack_fire", 1f);
		int hitCount = 2 + Repeat(GhostflameRepeatType.Damage);
		int damage = 3 + base.Intensity;
		for (int i = 0; i < hitCount; i++)
		{
			Creature val = base.CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)base.CombatState.HittableEnemies);
			if (val == null)
			{
				break;
			}
			SpawnVfx(val);
			if (val.IsHittable)
			{
				await CreatureCmd.Damage(ctx, val, (decimal)damage, (ValueProp)12, base.Owner.Creature);
			}
		}
	}

	protected override async Task BeforeCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (base.IsActive && cardPlay.Card.Owner == base.Owner)
		{
			bool flag = HexaghostHook.GhostflameConditionOverwrites(base.CombatState, base.Owner, this, cardPlay);
			if (((int)cardPlay.Card.Type == 2 || flag) && TryProgress())
			{
				await Ignite(ctx);
			}
		}
	}
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using Hexaghost.HexaghostCode.Ghostflames.Intents;
using Hexaghost.HexaghostCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Hexaghost.HexaghostCode.Ghostflames;

public class SearingGhostflame : GhostflameModel
{
	protected override int IgnitionRequirement => 2;

	public override FireColor FireColor => FireColor.Green;

	public override AbstractIntent Intent => (AbstractIntent)(object)new MultiStatusIntent<SoulBurnPower>(() => 3 + base.Intensity, 2 * (1 + Repeat(GhostflameRepeatType.Soulburn)));

	public override async Task OnIgnite(PlayerChoiceContext ctx)
	{
		Creature target = base.CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)base.CombatState.HittableEnemies);
		if (target != null && base.Owner.Creature.CombatState != null)
		{
			int intensity = base.Intensity;
			int repeat = 2 + Repeat(GhostflameRepeatType.Soulburn);
			SfxCmd.Play("event:/sfx/characters/attack_fire", 1f);
			SpawnVfx(target);
			for (int i = 0; i < repeat; i++)
			{
				await PowerCmd.Apply<SoulBurnPower>(ctx, target, (decimal)(3 + intensity), base.Owner.Creature, (CardModel)null, false);
			}
		}
	}

	protected override async Task BeforeCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (base.IsActive && cardPlay.Card.Owner == base.Owner)
		{
			bool flag = HexaghostHook.GhostflameConditionOverwrites(base.CombatState, base.Owner, this, cardPlay);
			if (((int)cardPlay.Card.Type == 1 || flag) && TryProgress())
			{
				await Ignite(ctx);
			}
		}
	}
}

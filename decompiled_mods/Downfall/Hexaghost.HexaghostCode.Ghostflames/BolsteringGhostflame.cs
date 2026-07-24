using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using Hexaghost.HexaghostCode.Ghostflames.Intents;
using Hexaghost.HexaghostCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Ghostflames;

public class BolsteringGhostflame : GhostflameModel
{
	public override AbstractIntent Intent => (AbstractIntent)(object)new BolsteringIntent();

	protected override int IgnitionRequirement => 1;

	public override FireColor FireColor => FireColor.Blue;

	public override async Task OnIgnite(PlayerChoiceContext ctx)
	{
		if (base.Owner.Creature.CombatState != null)
		{
			SfxCmd.Play("event:/sfx/characters/attack_fire", 1f);
			int repeat = 1 + Repeat(GhostflameRepeatType.Block);
			int block = base.Intensity;
			for (int i = 0; i < repeat; i++)
			{
				await CreatureCmd.GainBlock(base.Owner.Creature, (decimal)(4 + block), (ValueProp)12, (CardPlay)null, false);
			}
			await PowerCmd.Apply<StrengthPower>(ctx, base.Owner.Creature, 1m, base.Owner.Creature, (CardModel)null, false);
		}
	}

	protected override async Task BeforeCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (base.IsActive && cardPlay.Card.Owner == base.Owner && LocalContext.NetId.HasValue)
		{
			bool flag = HexaghostHook.GhostflameConditionOverwrites(base.CombatState, base.Owner, this, cardPlay);
			if (((int)cardPlay.Card.Type == 3 || flag) && TryProgress())
			{
				await Ignite(ctx);
			}
		}
	}
}

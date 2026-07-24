using System.Collections.Generic;
using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Champ.ChampCode.DynamicVars;
using Champ.ChampCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Stance;

public class ChampDefensiveStance : ChampStanceModel
{
	public override bool ShouldReceiveCombatHooks => true;

	public override bool HasFinisher => true;

	public override string ChargeIconPath => "res://Champ/images/ui/stance_charge_defensive.png";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>((DynamicVar[])(object)new DynamicVar[2]
	{
		new DefensiveSkillVar(2m),
		new DefensiveFinisherVar(6m)
	});

	public override async Task SkillBonus(PlayerChoiceContext ctx)
	{
		int num = (int)((DefensiveSkillVar)(object)base.DynamicVars["DefensiveSkill"]).Calculate();
		await PowerCmd.Apply<CounterPower>(ctx, base.Owner.Creature, (decimal)num, base.Owner.Creature, (CardModel)null, false);
	}

	public override async Task Finisher(PlayerChoiceContext ctx)
	{
		int num = (int)((DefensiveFinisherVar)(object)base.DynamicVars["DefensiveFinisher"]).Calculate();
		await CreatureCmd.GainBlock(base.Owner.Creature, (decimal)num, (ValueProp)4, (CardPlay)null, false);
	}
}

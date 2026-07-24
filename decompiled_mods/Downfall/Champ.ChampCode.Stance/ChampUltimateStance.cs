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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Stance;

public class ChampUltimateStance : ChampStanceModel
{
	public override bool ShouldReceiveCombatHooks => true;

	public override bool HasFinisher => true;

	public override string ChargeIconPath => "res://Champ/images/ui/stance_charge_ultimate.png";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>((DynamicVar[])(object)new DynamicVar[4]
	{
		new BerserkerSkillVar(2m),
		new DefensiveSkillVar(2m),
		new BerserkerFinisherVar(1m),
		new DefensiveFinisherVar(6m)
	});

	public override async Task SkillBonus(PlayerChoiceContext ctx)
	{
		int num = (int)((BerserkerSkillVar)(object)base.DynamicVars["BerserkerSkill"]).Calculate();
		await PowerCmd.Apply<VigorPower>(ctx, base.Owner.Creature, (decimal)num, base.Owner.Creature, (CardModel)null, false);
		int num2 = (int)((DefensiveSkillVar)(object)base.DynamicVars["DefensiveSkill"]).Calculate();
		await PowerCmd.Apply<CounterPower>(ctx, base.Owner.Creature, (decimal)num2, base.Owner.Creature, (CardModel)null, false);
	}

	public override async Task Finisher(PlayerChoiceContext ctx)
	{
		int num = (int)((BerserkerFinisherVar)(object)base.DynamicVars["BerserkerFinisher"]).Calculate();
		await PowerCmd.Apply<StrengthPower>(ctx, base.Owner.Creature, (decimal)num, base.Owner.Creature, (CardModel)null, false);
		int num2 = (int)((DefensiveFinisherVar)(object)base.DynamicVars["DefensiveFinisher"]).Calculate();
		await CreatureCmd.GainBlock(base.Owner.Creature, (decimal)num2, (ValueProp)4, (CardPlay)null, false);
	}
}

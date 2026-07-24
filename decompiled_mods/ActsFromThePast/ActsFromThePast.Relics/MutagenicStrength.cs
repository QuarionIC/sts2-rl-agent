using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class MutagenicStrength : CustomRelicModel
{
	public override RelicRarity Rarity => (RelicRarity)6;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new PowerVar<StrengthPower>(3m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<StrengthPower>((int?)null) };

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is CombatRoom)
		{
			((RelicModel)this).Flash();
			await PowerCmd.Apply<MutagenicStrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, ((DynamicVar)((RelicModel)this).DynamicVars.Strength).BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
		}
	}

	public MutagenicStrength()
		: base(true)
	{
	}
}

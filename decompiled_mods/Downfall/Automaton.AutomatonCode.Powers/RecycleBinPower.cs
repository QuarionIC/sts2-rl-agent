using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Powers;

public class RecycleBinPower : AutomatonPowerModel
{
	public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner))
		{
			int? num = ((PowerModel)this).Owner.Player?.GetHand().Count(delegate(CardModel e)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
				//IL_000b: Invalid comparison between Unknown and I4
				CardType type = e.Type;
				return type - 4 <= 1;
			});
			if ((num ?? 0) != 0 || 1 == 0)
			{
				((PowerModel)this).Flash();
				await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)(num.Value * ((PowerModel)this).Amount), (ValueProp)4, (CardPlay)null, false);
			}
		}
	}

	public RecycleBinPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}

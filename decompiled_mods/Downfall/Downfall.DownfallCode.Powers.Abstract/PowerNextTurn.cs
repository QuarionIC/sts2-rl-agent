using System.Threading.Tasks;
using BaseLib.Patches.Localization;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Powers.Abstract;

public abstract class PowerNextTurn<T> : DownfallPowerModel, IAddDumbVariablesToPowerDescription where T : PowerModel
{
	public override bool AllowNegative => ((PowerModel)ModelDb.Power<T>()).AllowNegative;

	public override PowerType Type => ((PowerModel)ModelDb.Power<T>()).Type;

	public override PowerStackType StackType => ((PowerModel)ModelDb.Power<T>()).StackType;

	public override PowerInstanceType InstanceType => ((PowerModel)ModelDb.Power<T>()).InstanceType;

	protected PowerNextTurn()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithTips((PowerModel e) => ((PowerModel)ModelDb.Power<T>()).HoverTips);
	}

	public void AddDumbVariablesToPowerDescription(LocString description)
	{
		description.Add("NAmount", (decimal)(-((PowerModel)this).Amount));
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
			await PowerCmd.Apply<T>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Applier, (CardModel)null, false);
		}
	}
}

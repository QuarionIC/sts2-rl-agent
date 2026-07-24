using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Powers;

public sealed class SporeCloudPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<VulnerablePower>((int?)null) };

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || creature != ((PowerModel)this).Owner)
		{
			return;
		}
		IEnumerable<Creature> players = ((PowerModel)this).CombatState.PlayerCreatures.Where((Creature c) => c.IsAlive);
		if (!players.Any())
		{
			return;
		}
		((PowerModel)this).Flash();
		AFTPModAudio.Play("fungi_beast", "spore_cloud_release");
		foreach (Creature player in players)
		{
			await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player, (decimal)((PowerModel)this).Amount, (Creature)null, (CardModel)null, false);
		}
	}
}

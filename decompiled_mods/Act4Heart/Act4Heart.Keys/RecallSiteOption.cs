using System.Linq;
using System.Threading.Tasks;
using Dolso;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace Act4Heart.Keys;

internal class RecallSiteOption : RestSiteOption
{
	public override string OptionId => "RECALL";

	public RecallSiteOption(Player owner)
		: base(owner)
	{
	}

	public override async Task<bool> OnSelect()
	{
		Player val = ((RestSiteOption)this).Owner;
		if (val.GetRelic<RubyKey>() != null)
		{
			IRunState runState = val.RunState;
			Player[] array = ((IPlayerCollection)runState).Players.Where((Player a) => a.GetRelic<RubyKey>() == null).ToArray();
			if (array.Length == 0)
			{
				log.info("blocking recall as all players already have red key");
				return false;
			}
			int num = new Rng(val, ((AbstractModel)ModelDb.Relic<RubyKey>()).Id, (ulong)(uint)runState.TotalFloor).NextInt(0, array.Length);
			val = array[num];
			log.info($"player {((IPlayerCollection)runState).GetPlayerSlotIndex(((RestSiteOption)this).Owner)} is recalling for random player {((IPlayerCollection)runState).GetPlayerSlotIndex(val)}");
		}
		await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<RubyKey>()).ToMutable(), val, -1);
		return true;
	}
}

using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class NlothsHungryFace : CustomRelicModel
{
	private int _treasureRoomsEntered;

	public override RelicRarity Rarity => (RelicRarity)6;

	public override bool IsUsedUp => _treasureRoomsEntered >= 1;

	[SavedProperty]
	public int TreasureRoomsEntered
	{
		get
		{
			return _treasureRoomsEntered;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_treasureRoomsEntered = value;
			if (((RelicModel)this).IsUsedUp)
			{
				((RelicModel)this).Status = (RelicStatus)2;
			}
		}
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is TreasureRoom)
		{
			int treasureRoomsEntered = TreasureRoomsEntered + 1;
			TreasureRoomsEntered = treasureRoomsEntered;
		}
		return Task.CompletedTask;
	}

	public override bool ShouldGenerateTreasure(Player player)
	{
		if (player != ((RelicModel)this).Owner || TreasureRoomsEntered > 1)
		{
			return true;
		}
		SilverCrucible val = ((RelicModel)this).Owner.Relics.OfType<SilverCrucible>().FirstOrDefault();
		if (val != null && !((AbstractModel)val).ShouldGenerateTreasure(player))
		{
			return true;
		}
		return false;
	}

	public NlothsHungryFace()
		: base(true)
	{
	}
}

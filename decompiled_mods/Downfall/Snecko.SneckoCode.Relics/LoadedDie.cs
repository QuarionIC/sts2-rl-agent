using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class LoadedDie : SneckoRelicModel, IAfterCardMuddled
{
	public LoadedDie()
		: base((RelicRarity)2)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(SneckoKeywords.Muddle);
		WithBlock(1);
	}

	public async Task AfterCardMuddled(PlayerChoiceContext ctx, CardModel card, AbstractModel? source)
	{
		if (card.Owner == ((RelicModel)this).Owner)
		{
			await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars.Block, (CardPlay)null, false);
		}
	}
}

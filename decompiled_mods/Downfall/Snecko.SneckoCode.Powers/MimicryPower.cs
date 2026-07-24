using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Powers;

public class MimicryPower : SneckoPowerModel
{
	public MimicryPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedPowerModel)this).WithTip<StrengthPower>();
		WithTip(SneckoTip.Offclass);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner && SneckoCmd.IsOffclass(cardPlay.Card))
		{
			await PowerCmd.Apply<MimicryPowerPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}

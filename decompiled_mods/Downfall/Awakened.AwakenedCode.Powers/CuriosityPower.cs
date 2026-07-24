using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Powers;

public class CuriosityPower : AwakenedPowerModel
{
	private bool _isFirstTriggered = true;

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((PowerModel)this).Owner.Player == cardPlay.Card.Owner && (int)cardPlay.Card.Type == 3)
		{
			if (_isFirstTriggered)
			{
				_isFirstTriggered = false;
			}
			else
			{
				await PowerCmd.Apply<StrengthPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
			}
		}
	}

	public CuriosityPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}

using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Patches;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode;

public interface IHasDeadOnEffect
{
	bool IsDeadOn
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Invalid comparison between Unknown and I4
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Invalid comparison between Unknown and I4
			CardPile pile = ((CardModel)this).Pile;
			if (pile == null || (int)pile.Type != 2 || !IsDeadOnInHand)
			{
				CardPile pile2 = ((CardModel)this).Pile;
				if (pile2 != null && (int)pile2.Type == 5)
				{
					return WasThisPlayedDeadOn;
				}
				return false;
			}
			return true;
		}
	}

	bool IsDeadOnInHand => HermitCmd.IsDeadOnInCurrentHandState((CardModel)this);

	bool WasThisPlayedDeadOn
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between O and Unknown
			if ((object)DeadOnPatch.LastPlayed == (object)(CardModel)this)
			{
				return DeadOnPatch.LastWasDeadOn;
			}
			return false;
		}
	}

	Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay cardPlay);
}

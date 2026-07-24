using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class MergePower : AutomatonPowerModel, IOnEncode
{
	public async Task OnCardEncoded(PlayerChoiceContext ctx, CardModel encodedCard)
	{
		if (encodedCard.Owner == ((PowerModel)this).Owner.Player && ((PowerModel)this).Amount > 0)
		{
			int copies = ((PowerModel)this).Amount;
			await PowerCmd.Remove((PowerModel)(object)this);
			for (int i = 0; i < copies; i++)
			{
				await AutomatonCmd.EncodeCard(encodedCard.CreateClone(), ctx);
			}
		}
	}

	public MergePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}

using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Common;

public sealed class BodyArmor : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public BodyArmor()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(5, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.DiscardSelectionPrompt, 1);
		CardModel selected = (await CardSelectCmd.FromHandForDiscard(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.CardBlock((CardModel)(object)this, play);
		if (selected != null)
		{
			await CardCmd.Discard(ctx, selected);
			if ((int)selected.Type != 1)
			{
				await CommonActions.CardBlock((CardModel)(object)this, play);
			}
		}
	}
}
